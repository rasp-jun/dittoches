#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed partial class MultiLauncher
{
    [Serializable] sealed class FaithfulReplayReport {public bool passed;public int frames,events;public string[] motions;}
    public void BeginFaithfulReplaySmoke(){StartCoroutine(FaithfulReplaySmoke());}
    IEnumerator FaithfulReplaySmoke()
    {
        yield return null;artPack=0;
        string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"..","FaithfulReplayValidation"));
        Room room=JsonUtility.FromJson<Room>(File.ReadAllText(Path.Combine(folder,"fixture.json")));
        arena=new TacticalArena(new Rect(0,0,1280,800),true);var seen=new HashSet<string>();int frames=0;
        for(int side=0;side<2;side++)
        {
            room.side=side;arena.Dispose();arena=new TacticalArena(new Rect(0,0,1280,800),true);
            for(float time=0;time<room.battleDuration;time+=1f/30)
            {
                float remaining=room.battleDuration-time,progress=CombatProgress(room,remaining);
                int frame=Mathf.FloorToInt(progress),next=Mathf.Min(frame+1,room.frames.Length-1);
                arena.BeginFrame(-1,-1,-1,false);
                foreach(Fighter f in room.frames[frame].units)RenderOnlineFighter(room,f,frame,next,progress,remaining);
                DrawMatchSkills(room,remaining);arena.Render();frames++;
                foreach(string motion in arena.FaithfulMotions())seen.Add(motion);
                if(Mathf.Abs(time-3)<.016f)
                {
                    var old=RenderTexture.active;RenderTexture.active=arena.Texture;var image=new Texture2D(arena.Texture.width,arena.Texture.height,TextureFormat.RGB24,false);
                    image.ReadPixels(new Rect(0,0,image.width,image.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,"side-"+side+".png"),image.EncodeToPNG());Destroy(image);RenderTexture.active=old;
                }
                yield return null;
            }
        }
        var report=new FaithfulReplayReport{frames=frames,events=room.skillEvents.Length,motions=new List<string>(seen).ToArray(),passed=seen.Contains("Attack")&&seen.Contains("Skill")&&seen.Contains("Down")};
        File.WriteAllText(Path.Combine(folder,"replay-report.json"),JsonUtility.ToJson(report,true));
        Debug.Log("FAITHFUL REPLAY RESULT "+report.passed+" / "+frames+" frames / "+report.events+" events");Application.Quit(report.passed?0:1);
    }
}
#endif
