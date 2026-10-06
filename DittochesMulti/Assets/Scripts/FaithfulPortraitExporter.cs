using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Deterministic, offline portrait export; no camera or mesh work in the shop.</summary>
public sealed class FaithfulPortraitExporter : MonoBehaviour
{
    [Serializable] public sealed class Entry {public string id,sourceHash;public float bodyCoverage,bustCoverage;}
    [Serializable] public sealed class Report {public bool passed;public string error;public int size=768;public List<Entry> models=new List<Entry>();}
    public static bool TryBoot()
    {
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"--export-portraits")<0)return false;
        new GameObject("Portrait studio").AddComponent<FaithfulPortraitExporter>();return true;
    }
    Camera view;RenderTexture target;
    List<Vector3> Vertices(FaithfulModelActor actor)
    {
        var result=new List<Vector3>();
        foreach(var renderer in actor.Renderers)
        {
            var skin=renderer as SkinnedMeshRenderer;
            if(skin==null)
            {
                foreach(var p in renderer.GetComponent<MeshFilter>().sharedMesh.vertices)result.Add(renderer.transform.TransformPoint(p));
                continue;
            }
            // Compute world skin positions explicitly: BakeMesh applies transform scale differently
            // across the imported native rigs. Do not apply that scale a second time when framing.
            var mesh=skin.sharedMesh;var binds=mesh.bindposes;var bones=skin.bones;
            var matrices=new Matrix4x4[bones.Length];
            for(int b=0;b<bones.Length;b++)matrices[b]=bones[b].localToWorldMatrix*binds[b];
            var vertices=mesh.vertices;var weights=mesh.boneWeights;
            for(int i=0;i<vertices.Length;i++)
            {
                var w=weights[i];var p=vertices[i];
                result.Add(matrices[w.boneIndex0].MultiplyPoint3x4(p)*w.weight0+
                    matrices[w.boneIndex1].MultiplyPoint3x4(p)*w.weight1+
                    matrices[w.boneIndex2].MultiplyPoint3x4(p)*w.weight2+
                    matrices[w.boneIndex3].MultiplyPoint3x4(p)*w.weight3);
            }
        }
        return result;
    }

    float Render(string file)
    {
        view.Render();var old=RenderTexture.active;RenderTexture.active=target;
        var image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
        try
        {
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
            int visible=0;foreach(var pixel in image.GetPixels32())if(pixel.a>32)visible++;
            float coverage=visible/(float)(target.width*target.height);
            if(coverage<.035f||coverage>.93f)throw new InvalidOperationException("Portrait framing failed: "+file+" "+coverage);
            File.WriteAllBytes(file,image.EncodeToPNG());return coverage;
        }
        finally{RenderTexture.active=old;Destroy(image);}
    }
    IEnumerator Start()
    {
        Application.runInBackground=true;Application.targetFrameRate=60;
        string output=Path.Combine(Application.streamingAssetsPath,"FaithfulPortraits");Directory.CreateDirectory(output);
        var report=new Report();
        view=gameObject.AddComponent<Camera>();view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=Color.clear;
        view.orthographic=true;view.nearClipPlane=.01f;view.farClipPlane=30;
        target=new RenderTexture(768,768,24,RenderTextureFormat.ARGB32){antiAliasing=4};target.Create();view.targetTexture=target;
        var light=new GameObject("Portrait key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(30,-35,0);
        RenderSettings.ambientLight=new Color(.65f,.70f,.78f);
        foreach(string id in DigimonModelLibrary.Roster)
        {
            FaithfulModelActor actor=null;
            try
            {
                actor=new GameObject(id).AddComponent<FaithfulModelActor>();actor.Initialize(id,0);actor.SetSize(1.8f);actor.Sample("Idle",.18f,true);
                foreach(var material in actor.Data.materials)if(material.HasProperty("_Unlit"))material.SetFloat("_Unlit",.26f);
                // A shallow three-quarter view keeps both eyes and the broad shoulder silhouette readable.
                Vector3 direction=new Vector3(.38f,.16f,1).normalized;
                view.transform.rotation=Quaternion.LookRotation(-direction,Vector3.up);
                var points=Vertices(actor);Vector3 right=view.transform.right,up=view.transform.up;
                float loX=float.MaxValue,hiX=float.MinValue,loY=float.MaxValue,hiY=float.MinValue;
                foreach(var p in points){float x=Vector3.Dot(p,right),y=Vector3.Dot(p,up);loX=Mathf.Min(loX,x);hiX=Mathf.Max(hiX,x);loY=Mathf.Min(loY,y);hiY=Mathf.Max(hiY,y);}
                Debug.Log("PORTRAIT FRAME "+id+" x="+loX+":"+hiX+" y="+loY+":"+hiY+" scale="+actor.transform.localScale);
                Vector3 center=right*((loX+hiX)*.5f)+up*((loY+hiY)*.5f);
                view.transform.position=center+direction*8;view.orthographicSize=Mathf.Max(hiX-loX,hiY-loY)*.55f;
                var row=new Entry{id=id,sourceHash=actor.Data.sourceHash};
                row.bodyCoverage=Render(Path.Combine(output,id+".png"));
                // Crop the torso while preserving heads/horns. Native rigs without a named Head use an upper-body focus.
                Vector3 focus=new Vector3(0,1.22f,0);bool headFound=false;
                foreach(var bone in actor.Nodes)if(bone.name.ToLowerInvariant().Contains("head")){focus=bone.position+Vector3.up*.12f;headFound=true;break;}
                if(id=="angemon")focus.y-=.22f;
                float crop=.68f;
                if(Array.IndexOf(new[]{"angemon","holyangemon","rosemon","lilimon","seraphimon","devimon","etemon","wargreymon","weregarurumon"},id)>=0){if(!headFound)focus.y=1.40f;crop=.55f;}
                if(id=="greymon"||id=="metalgreymon"){if(!headFound)focus.y=1.30f;crop=.68f;}
                if(id=="birdramon"||id=="hououmon"||id=="garudamon"){if(!headFound)focus.y=1.22f;crop=.70f;}
                if(id=="garurumon"||id=="metalgarurumon"){if(!headFound)focus.y=1.08f;crop=.70f;}
                if(Array.IndexOf(new[]{"koromon","tsunomon","mochimon","tanemon","pyocomon","tokomon","patamon"},id)>=0){focus=center;crop=Mathf.Max(hiX-loX,hiY-loY)*.53f;}
                view.orthographicSize=crop;
                view.transform.position=focus+direction*8;
                row.bustCoverage=Render(Path.Combine(output,id+"-bust.png"));
                report.models.Add(row);Debug.Log("PORTRAIT EXPORTED "+id);
            }
            catch(Exception error){report.error=error.ToString();Debug.LogException(error);}
            if(actor!=null)Destroy(actor.gameObject);yield return null;
            if(report.error!=null)break;
        }
        report.passed=report.error==null&&report.models.Count==DigimonModelLibrary.Roster.Length;
        File.WriteAllText(Path.Combine(output,"manifest.json"),JsonUtility.ToJson(report,true));
        view.targetTexture=null;target.Release();Destroy(target);Debug.Log("PORTRAIT EXPORT RESULT "+report.passed);Application.Quit(report.passed?0:1);
    }
}
