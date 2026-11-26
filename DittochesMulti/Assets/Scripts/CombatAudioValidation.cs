#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

public static class CombatAudioValidation
{
    public static void Rules(Action<bool,string> require)
    {
        string folder=Path.Combine(Application.dataPath,"../CombatAudioCaptures");Directory.CreateDirectory(folder);
        foreach(CombatSound.Kind kind in Enum.GetValues(typeof(CombatSound.Kind)))foreach(bool skill in new[]{false,true})
        {
            var data=CombatSound.Samples(kind,skill);var clip=CombatAudio.ReviewClip(kind,skill);var actual=new float[data.Length];clip.GetData(actual,0);
            double peak=data.Max(x=>Math.Abs(x)),rms=Math.Sqrt(data.Average(x=>(double)x*x)),mean=data.Average(x=>(double)x);
            require(data.All(x=>!float.IsNaN(x)&&!float.IsInfinity(x))&&peak<.54&&rms>.012&&rms<.25&&Math.Abs(mean)<.00001,"bounded finite waveform "+kind+"/"+skill+" peak="+peak+" rms="+rms+" dc="+mean);
            require(data[0]==0&&data[data.Length-1]==0&&data.Skip(1).Select((x,i)=>Math.Abs(x-data[i])).Max()<.35,"silent endpoints and bounded transitions "+kind+"/"+skill);
            require(clip.frequency==48000&&clip.channels==1&&actual.SequenceEqual(data)&&ReferenceEquals(clip,CombatAudio.ReviewClip(kind,skill)),"exact cached Unity PCM "+kind+"/"+skill);
            using(var writer=new BinaryWriter(File.Create(Path.Combine(folder,kind.ToString().ToLowerInvariant()+(skill?"-skill":"-basic")+".wav"))))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(data.Length*2+36);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(48000);writer.Write(96000);writer.Write((short)2);writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(data.Length*2);
                // Match one unobstructed live voice at default master volume, including its 5ms onset.
                for(int i=0;i<data.Length;i++)writer.Write((short)Mathf.RoundToInt(data[i]*.35f*.6f*(skill?1.3f:.85f)/2*Mathf.Clamp01(i/(48000f*.005f))*32767));
            }
            Debug.Log("COMBAT PCM: "+kind+" skill="+skill+" samples="+data.Length+" peak="+peak+" rms="+rms);
        }
        var clock=new CombatAudioTimeline();clock.Begin(0);require(!clock.Observe(1,.1f),"future sound stays silent");clock.Begin(.1f);
        require(clock.Observe(1,.1f)&&!clock.Observe(1,.1f)&&clock.Observe(-1,.1f),"same GUI frame sounds once and skill/basic keys differ");
        clock.Begin(.4f);require(!clock.Observe(2,.1f),"stale sound never queues");clock.Begin(2);require(!clock.Observe(3,1.99f),"hitch does not replay a backlog");
        clock.Begin(2.04f);require(clock.Observe(4,2.03f),"new attack after hitch resumes");clock.Begin(1);require(!clock.Observe(5,.99f),"rewind remains silent at landing");
        clock.Clear();clock.Begin(8);require(!clock.Observe(6,7.99f),"late join discards historical attacks");
        clock.Clear();clock.Begin(0);
        for(int i=1;i<=1000;i++){float t=i*.02f;clock.Begin(t);require(clock.Observe(i,t)&&!clock.Observe(i,t),"continuous event consumed once "+i);require(clock.Count<55,"clock retains bounded recent events "+i);}
        foreach(string id in DigimonModelLibrary.Roster){require(Enum.IsDefined(typeof(CombatSound.Kind),CombatSound.Basic(id))&&Enum.IsDefined(typeof(CombatSound.Kind),CombatSound.Skill(DigimonSkillCatalog.Find(id).visual)),"all roster attacks mapped "+id);}
        require(CombatSound.Basic("garurumon")==CombatSound.Kind.Bite&&CombatSound.Basic("togemon")==CombatSound.Kind.Impact&&CombatSound.Skill("swords")==CombatSound.Kind.Slash&&CombatSound.Skill("claws")==CombatSound.Kind.Slash,"melee identities mapped to their intended timbre");
        Debug.Log("COMBAT AUDIO RULES COMPLETE: 28 cached PCM clips, 47 basic/skill mappings, bounded clocks, repaint/hitch/seek/reconnect behavior");
    }
    public static IEnumerator Playback(Action<bool,string> require)
    {
        using(var saved=new StarSoundValidation.Preferences())
        {
            StarUpgradeAudio.Configure(35,false);StarUpgradeAudio.Cancel();CombatAudio.Configure(true);CombatAudio.Cancel();
            int before=CombatAudio.PlayedCount;
            bool first=CombatAudio.Request(CombatSound.Kind.Holy,true,.9f),second=CombatAudio.Request(CombatSound.Kind.Holy,true);
            require(first&&!second,"same family burst suppressed: first="+first+" second="+second+" enabled="+CombatAudio.Enabled+" audible="+CombatAudio.Audible+" validation="+PortablePreviewPrefs.IsValidation+" time="+Time.unscaledTime+" played="+CombatAudio.PlayedCount);
            yield return new WaitForSecondsRealtime(.02f);
            require(CombatAudio.ActiveVoices==1&&CombatAudio.PlayedCount==before+1&&CombatAudio.LastPan==.5f&&CombatAudio.Gain>0&&CombatAudio.Gain<=.2101f,"actual Unity source playing with clamped pan and default-volume budget");
            CombatAudio.Cancel();
            for(int i=0;i<14;i++){CombatAudio.Request((CombatSound.Kind)i,false);CombatAudio.Request((CombatSound.Kind)i,true);yield return new WaitForSecondsRealtime(.048f);require(CombatAudio.ActiveVoices<=6&&CombatAudio.Gain<=.2101f,"crowded volley keeps voice and gain limits "+i);}
            CombatAudio.Cancel();
            foreach(var kind in new[]{CombatSound.Kind.Holy,CombatSound.Kind.Water,CombatSound.Kind.Fire,CombatSound.Kind.Shadow}){CombatAudio.Request(kind,false);yield return new WaitForSecondsRealtime(.04f);}
            require(CombatAudio.ActiveVoices==4,"basic volley occupies four fixed voices");
            require(!CombatAudio.Request(CombatSound.Kind.Fire,false)&&CombatAudio.Request(CombatSound.Kind.Shadow,true),"two voices remain reserved for skills under basic saturation");
            StarUpgradeAudio.Request(3);yield return new WaitForSecondsRealtime(.07f);
            require(StarUpgradeAudio.ChimePlaying&&CombatAudio.Gain<=.0946f,"upgrade flourish ducks combat volume");
            StarUpgradeAudio.Configure(50,true);require(CombatAudio.ActiveVoices==0&&!CombatAudio.Request(CombatSound.Kind.Fire,true),"master mute immediately stops combat too");
            StarUpgradeAudio.Configure(35,false);CombatAudio.Configure(false);CombatAudio.ReloadSettings();
            require(!CombatAudio.Enabled&&!CombatAudio.Request(CombatSound.Kind.Fire,true)&&StarUpgradeAudio.Request(2),"combat toggle persists independently from promotion sounds");
            CombatAudio.Configure(true);CombatAudio.Preview(CombatSound.Kind.Fire,false);CombatAudio.Cancel();yield return new WaitForSecondsRealtime(.05f);
            require(CombatAudio.ActiveVoices==0,"cancelled sounds never resume from a queue");
            require(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(x=>x.isActiveAndEnabled)==1,"combat and promotion share one active listener");
        }
        Debug.Log("COMBAT AUDIO PLAYBACK COMPLETE: six real muted validation AudioSources, reserved skills, gain budget, ducking, persistence, cancellation and one listener");
    }
}
public sealed class CombatAudioPlayerValidation : MonoBehaviour
{
    int checks;
    void Require(bool ok,string message){if(!ok){Debug.LogError("COMBAT AUDIO SMOKE FAILED: "+message);Application.Quit(2);throw new InvalidOperationException(message);}checks++;}
    IEnumerator Start(){Application.runInBackground=true;Application.targetFrameRate=60;CombatAudioValidation.Rules(Require);yield return CombatAudioValidation.Playback(Require);Debug.Log("COMBAT AUDIO SMOKE COMPLETE: "+checks+" checks");Application.Quit();}
}
#endif
