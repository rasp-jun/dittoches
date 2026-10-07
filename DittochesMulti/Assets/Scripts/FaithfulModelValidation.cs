using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Exercise the actual loader and skinner for the entire roster in a player.</summary>
public sealed class FaithfulModelValidation : MonoBehaviour
{
    Camera view;RenderTexture target;string output;int checks,clips;
    [Serializable] public sealed class Row {public string id,sha256,shader;public int clips;public float transitionJump;}
    [Serializable] public sealed class Report {public bool passed;public int checks,clips;public string error;public List<Row> models=new List<Row>();}
    readonly Report report=new Report();
    public static bool TryBoot()
    {
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"--faithful-smoke")<0)return false;
        new GameObject("Faithful runtime validation").AddComponent<FaithfulModelValidation>();return true;
    }
    void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);checks++;}
    static float Difference(Vector3[] a,Vector3[] b){float max=0;for(int i=0;i<a.Length;i++)max=Mathf.Max(max,(a[i]-b[i]).magnitude);return max;}
    Vector3[] Capture(FaithfulModelActor actor)
    {
        var points=new List<Vector3>();var baked=new Mesh();
        foreach(var renderer in actor.Renderers)
        {
            Mesh mesh;var skin=renderer as SkinnedMeshRenderer;
            if(skin!=null){skin.BakeMesh(baked);mesh=baked;}else mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
            Vector3[] vertices=mesh.vertices;int step=Mathf.Max(1,vertices.Length/100);
            for(int i=0;i<vertices.Length;i+=step)
            {
                Vector3 point=renderer.transform.TransformPoint(vertices[i]);
                Require(!float.IsNaN(point.sqrMagnitude)&&!float.IsInfinity(point.sqrMagnitude)&&point.magnitude<100,"Invalid skinned surface: "+actor.Data.id);
                points.Add(point);
            }
        }
        Destroy(baked);return points.ToArray();
    }
    void Image(string name)
    {
        view.Render();var old=RenderTexture.active;RenderTexture.active=target;
        var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
        File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());Destroy(image);RenderTexture.active=old;
    }
    IEnumerator Start()
    {
        Application.runInBackground=true;Application.targetFrameRate=60;
        output=Path.GetFullPath(Path.Combine(Application.dataPath,"..","FaithfulValidation"));Directory.CreateDirectory(output);
        view=gameObject.AddComponent<Camera>();view.backgroundColor=new Color(.12f,.16f,.21f);view.clearFlags=CameraClearFlags.SolidColor;
        view.transform.position=new Vector3(3.1f,2.1f,4.3f);view.transform.LookAt(new Vector3(0,.85f,0));view.fieldOfView=32;
        target=new RenderTexture(960,720,24){antiAliasing=2};target.Create();view.targetTexture=target;
        var light=new GameObject("Key light").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(45,-35,0);light.intensity=1.3f;
        RenderSettings.ambientLight=new Color(.6f,.65f,.72f);
        foreach(string id in DigimonModelLibrary.Roster)
        {
            FaithfulModelActor actor=null;
            try
            {
                Require(FaithfulModelData.Available(id),"Missing model: "+id);
                actor=new GameObject(id).AddComponent<FaithfulModelActor>();actor.Initialize(id,0);actor.SetSize(1.8f);
                var row=new Row{id=id,sha256=actor.Data.sourceHash,shader=actor.Data.materials[0].shader.name};
                foreach(var pair in actor.Data.clips)
                {
                    var clip=pair.Value;actor.Sample(pair.Key,0,true);var first=Capture(actor);
                    actor.Sample(pair.Key,clip.duration*.37f,true);float change=Difference(first,Capture(actor));
                    actor.Sample(pair.Key,clip.duration*.71f,true);change=Mathf.Max(change,Difference(first,Capture(actor)));
                    Require(change>.000001f,"No actual vertex motion: "+id+" / "+pair.Key);
                    clips++;row.clips++;
                }
                actor.Sample("Attack",actor.Data.clips["Attack"].duration*.51f,true);
                Vector3[] before=Capture(actor);actor.Sample("Walk",.3f,false,0);
                row.transitionJump=Difference(before,Capture(actor));Require(row.transitionJump<.00001f,"Transition snap "+id);
                for(int i=0;i<30;i++)actor.Sample("Walk",.3f,false,0);
                Require(Difference(before,Capture(actor))<.00001f,"Paused blend drift "+id);
                var spec=DigimonSkillCatalog.Find(id);
                if(spec!=null)
                {
                    actor.Pose(0,0,Vector3.forward,spec,0,0,0,0);
                    actor.Pose(.1f,0,Vector3.forward,spec,spec.windup,0,0,0);
                    Require(actor.Motion=="Skill"&&Mathf.Abs(actor.MotionTime/actor.Data.clips["Skill"].duration-actor.Data.technique.skillRelease)<.00001f,"Skill release not aligned with combat windup: "+id);
                    var emitterPose=Capture(actor);Vector3 emitter=actor.SkillEmitter(true);
                    Require(Difference(emitterPose,Capture(actor))<.00001f&&!float.IsNaN(emitter.sqrMagnitude),"Release emitter changed visible pose: "+id);
                    actor.Pose(.2f,0,Vector3.forward,spec,-1,0,1,0);
                    Require(actor.Motion=="Down"&&Mathf.Abs(actor.MotionTime-actor.Data.clips["Down"].duration)<.00001f,"Death did not hold final frame: "+id);
                    actor.Pose(0,0,Vector3.forward,spec,-1,0,0,0);
                    Require(actor.Motion=="Idle","Replay reset retained death: "+id);
                }
                actor.Sample("Idle",0,true);Image(id+"-Idle");
                actor.Sample("Skill",actor.Data.clips["Skill"].duration*.57f,true);Image(id+"-Skill");
                report.models.Add(row);Debug.Log("FAITHFUL RUNTIME PASS "+id+" "+row.clips+" clips");
            }
            catch(Exception error){report.error=error.ToString();Debug.LogException(error);}
            if(actor!=null)Destroy(actor.gameObject);
            yield return null;
            if(report.error!=null)break;
        }
        report.passed=report.error==null&&report.models.Count==34&&clips==352;report.checks=checks;report.clips=clips;
        File.WriteAllText(Path.Combine(output,"runtime-report.json"),JsonUtility.ToJson(report,true));
        Debug.Log("FAITHFUL RUNTIME RESULT "+report.passed+" "+clips+" clips / "+checks+" checks");
        Application.Quit(report.passed?0:1);
    }
}
