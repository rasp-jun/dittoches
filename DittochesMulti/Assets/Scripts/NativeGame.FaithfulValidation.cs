#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    [Serializable] sealed class FaithfulCombatReport
    {public bool passed;public int models,attacks,casts,dead,frames,fixedLaunches;public float seconds,averageFps;public string[] motions;}
    public void BeginFaithfulCombatSmoke(){StartCoroutine(FaithfulCombatSmoke());}
    IEnumerator FaithfulCombatSmoke()
    {
        Application.runInBackground=true;Application.targetFrameRate=60;yield return null;
        artPack=0;lobby=false;round=12;level=7;DigimonModelLibrary.PreviewEnabled=false;
        ValidateBuildRules();ValidateScalingRules();ValidateTacticalRules();ValidateReportRules();ValidateFormationRules();ValidateCombatLabels();
        fighters.Clear();skillCasts.Clear();Array.Clear(board,0,board.Length);
        string[] ids={"agumon","greymon","metalgreymon","wargreymon","garurumon","rosemon","seraphimon"};
        for(int i=0;i<ids.Length;i++)
        {
            var def=RosterById[ids[i]];board[i]=new Unit(def);
            fighters.Add(CreateFighter(board[i],false,new Vector2(i,5)));
            fighters.Add(CreateFighter(new Unit(def),true,new Vector2(i,2)));
        }
        InitializeBuildBonuses();foreach(Fighter fighter in fighters)fighter.mana=fighter.maxMana;
        arena=new TacticalArena(new Rect(0,0,1280,800),true);battling=true;
        string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"..","FaithfulCombatValidation"));Directory.CreateDirectory(folder);
        var seen=new HashSet<string>();var launchPoints=new Dictionary<int,Vector3>();float started=Time.unscaledTime;int frames=0,shot=0;
        while(Time.unscaledTime-started<18)
        {
            float dt=Mathf.Min(Time.deltaTime,.1f);UpdateCombat(dt);
            arena.BeginFrame(-1,-1,-1,false);
            foreach(Fighter f in fighters)RenderArenaFighter(f);
            DrawDigimonSkills();arena.Render();frames++;
            foreach(SkillCast cast in skillCasts)
            {
                Vector3 launch,previous;
                if(!arena.SkillLaunch(cast.presentationId,out launch))continue;
                if(launchPoints.TryGetValue(cast.presentationId,out previous))Require((launch-previous).sqrMagnitude<1e-8f,"Released projectile followed recoil");
                else launchPoints.Add(cast.presentationId,launch);
            }
            foreach(string motion in arena.FaithfulMotions())seen.Add(motion);
            if(Time.unscaledTime-started>1+shot*3&&shot<6)
            {
                var previous=RenderTexture.active;RenderTexture.active=arena.Texture;
                var image=new Texture2D(arena.Texture.width,arena.Texture.height,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,image.width,image.height),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(folder,"combat-"+shot+".png"),image.EncodeToPNG());Destroy(image);RenderTexture.active=previous;shot++;
            }
            yield return null;
        }
        var report=new FaithfulCombatReport{models=ids.Length*2,attacks=fighters.Sum(f=>f.attacks),casts=fighters.Sum(f=>f.casts),dead=fighters.Count(f=>f.dead),frames=frames,seconds=Time.unscaledTime-started,motions=seen.ToArray()};
        report.fixedLaunches=launchPoints.Count;
        report.averageFps=frames/report.seconds;report.passed=report.attacks>0&&report.casts>0&&seen.Contains("Skill")&&seen.Contains("Attack")&&seen.Contains("Down")&&report.fixedLaunches>=3;
        File.WriteAllText(Path.Combine(folder,"combat-report.json"),JsonUtility.ToJson(report,true));
        Debug.Log("FAITHFUL COMBAT RESULT "+report.passed+" attacks="+report.attacks+" casts="+report.casts+" fps="+report.averageFps);
        battling=false;Application.Quit(report.passed?0:1);
    }
}
#endif
