#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed partial class TacticalArena
{
    public float ValidatedFaithfulHeight(object key)
    {
        Actor actor;if(!actors.TryGetValue(key,out actor)||actor.faithful==null)throw new InvalidOperationException("Missing production actor");
        actor.faithful.Sample("Idle",0,true);
        return actor.faithful.GeometryBounds().size.y*actor.faithful.transform.localScale.y;
    }
    public void EvolutionReviewCamera()
    {UseModelStudio(new Vector3(0,-.12f,0));camera.transform.localPosition=new Vector3(0,3.8f,-13);camera.transform.LookAt(root.transform.TransformPoint(new Vector3(0,.9f,0)));camera.fieldOfView=32;}
}

public sealed class EvolutionScaleValidation : MonoBehaviour
{
    [Serializable] public sealed class Row {public string id,stage;public float oneStar,threeStar;}
    [Serializable] public sealed class Report {public bool passed;public int checks;public string error;public List<Row> models=new List<Row>();}
    public static bool TryBoot()
    {
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"--evolution-scale-smoke")<0)return false;
        new GameObject("Evolution scale validation").AddComponent<EvolutionScaleValidation>();return true;
    }
    Report report=new Report();TacticalArena arena;
    void Require(bool condition,string message){report.checks++;if(!condition)throw new InvalidOperationException(message);}
    IEnumerator Start()
    {
        Application.runInBackground=true;
        string output=Path.GetFullPath(Path.Combine(Application.dataPath,"..","EvolutionScaleValidation"));Directory.CreateDirectory(output);
        arena=new TacticalArena(new Rect(0,0,1440,720),true);arena.EvolutionReviewCamera();
        foreach(string id in DigimonModelLibrary.Roster)
        {
            try
            {
                Require(DigimonVisualScale.Stage(id)!=DigimonVisualScale.Evolution.Unknown,"Unclassified evolution: "+id);
                arena.BeginFrame(-1,-1,-1,false);arena.SetActor(id,Vector3.zero,null,Color.cyan,.85f);arena.PoseDigimon(id,id,0,0,0,star:1);
                Require(Mathf.Abs(arena.ValidatedFaithfulHeight(id)-DigimonVisualScale.Height(id))<.002f,"Bench-first model normalization: "+id);
                arena.SetActor(id,Vector3.zero,null,Color.cyan,1f);arena.PoseDigimon(id,id,0,0,0,star:1);
                float one=arena.ValidatedFaithfulHeight(id);
                arena.PoseDigimon(id,id,0,0,0,star:3);float three=arena.ValidatedFaithfulHeight(id);
                Require(Mathf.Abs(one-DigimonVisualScale.Height(id))<.002f,"Production height mismatch: "+id);
                Require(Mathf.Abs(three/one-1.06f)<.002f,"Star size mismatch: "+id);
                Require(Mathf.Abs(TacticalArena.DigimonHeadHeight(id,3)-three-.15f)<.002f,"Label height mismatch: "+id);
                report.models.Add(new Row{id=id,stage=DigimonVisualScale.StageName(id),oneStar=one,threeStar=three});arena.Render();
            }
            catch(Exception e){report.error=e.ToString();break;}
            yield return null;
        }
        if(report.error==null)try
        {
            foreach(var a in report.models)foreach(var b in report.models)
                if(DigimonVisualScale.Stage(a.id)<DigimonVisualScale.Stage(b.id))Require(a.threeStar<b.oneStar,"Evolution hierarchy inverted: "+a.id+" / "+b.id);
        }
        catch(Exception e){report.error=e.ToString();}
        string[][] lines={new[]{"koromon","agumon","greymon","metalgreymon","wargreymon"},new[]{"tsunomon","gabumon","garurumon","weregarurumon","metalgarurumon"},new[]{"mochimon","tentomon","kabuterimon","atlur","herakle"},new[]{"tanemon","palmon","togemon","lilimon","rosemon"},new[]{"pyocomon","piyomon","birdramon","garudamon","hououmon"},new[]{"tokomon","patamon","angemon","holyangemon","seraphimon"}};
        if(report.error==null)for(int line=0;line<lines.Length;line++)
        {
            arena.BeginFrame(-1,-1,-1,false);
            for(int i=0;i<5;i++)
            {
                string id=lines[line][i];arena.SetActor(id,new Vector3((i-2)*2.05f,0,0),FaithfulPortraits.Get(id),ArenaInterface.Rarity(i+1));arena.FaceActor(id,Vector3.back);arena.PoseDigimon(id,id,0,0,0);
            }
            arena.Render();var old=RenderTexture.active;RenderTexture.active=arena.Texture;var image=new Texture2D(arena.Texture.width,arena.Texture.height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,image.width,image.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,"line-"+line+".png"),image.EncodeToPNG());Destroy(image);RenderTexture.active=old;yield return null;
        }
        report.passed=report.error==null&&report.models.Count==34;
        File.WriteAllText(Path.Combine(output,"scale-report.json"),JsonUtility.ToJson(report,true));arena.Dispose();
        Debug.Log("EVOLUTION SCALE RESULT "+report.passed+" / "+report.checks+" checks "+report.error);Application.Quit(report.passed?0:1);
    }
}
#endif
