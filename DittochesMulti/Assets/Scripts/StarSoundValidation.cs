#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using PlayerPrefs=PortablePreviewPrefs;

public static class StarSoundValidation
{
    public sealed class Preferences : IDisposable
    {
        readonly bool combatExists=PlayerPrefs.HasKey(CombatAudio.EnabledKey);
        readonly int combatEnabled=PlayerPrefs.GetInt(CombatAudio.EnabledKey);
        readonly bool volumeExists=PlayerPrefs.HasKey(StarUpgradeAudio.VolumeKey),muteExists=PlayerPrefs.HasKey(StarUpgradeAudio.MuteKey);
        readonly int volume=PlayerPrefs.GetInt(StarUpgradeAudio.VolumeKey),mute=PlayerPrefs.GetInt(StarUpgradeAudio.MuteKey);
        public void Dispose()
        {
            StarUpgradeAudio.Cancel();
            if(volumeExists)PlayerPrefs.SetInt(StarUpgradeAudio.VolumeKey,volume);else PlayerPrefs.DeleteKey(StarUpgradeAudio.VolumeKey);
            if(muteExists)PlayerPrefs.SetInt(StarUpgradeAudio.MuteKey,mute);else PlayerPrefs.DeleteKey(StarUpgradeAudio.MuteKey);
            if(combatExists)PlayerPrefs.SetInt(CombatAudio.EnabledKey,combatEnabled);else PlayerPrefs.DeleteKey(CombatAudio.EnabledKey);
            PlayerPrefs.Save();StarUpgradeAudio.ReloadSettings();CombatAudio.ReloadSettings();
        }
    }
    public static void Rules(Action<bool,string> require)
    {
        var queue=new StarUpgradeSoundQueue();require(!queue.Add(1,0)&&!queue.Add(4,0)&&queue.Star==0,"ordinary purchases and invalid ranks cannot queue audio");
        queue.Add(2,1);queue.Add(3,1.01f);queue.Add(2,1.02f);
        require(queue.Take(1.03f)==0&&queue.Take(1.05f)==3&&queue.Take(2)==0,"chain upgrades emit the highest rank once after the transaction window");
        queue.Add(3,3);queue.Clear();require(queue.Take(10)==0,"cancelled upgrade cues never replay later");
        require(StarUpgradeChime.Samples(1).Length==0,"no cue synthesized for ordinary purchase");
        string folder=Path.Combine(Application.dataPath,"../AudioCaptures");Directory.CreateDirectory(folder);
        foreach(int star in new[]{2,3})
        {
            var samples=StarUpgradeChime.Samples(star);var clip=StarUpgradeAudio.ReviewClip(star);var actual=new float[samples.Length];clip.GetData(actual,0);
            require(clip.channels==1&&clip.frequency==48000&&clip.samples==samples.Length&&actual.SequenceEqual(samples),"cached Unity clip contains the exact generated PCM "+star);
            require(ReferenceEquals(clip,StarUpgradeAudio.ReviewClip(star)),"repeated upgrade reuses cached audio clip "+star);
            double peak=samples.Max(v=>Math.Abs(v)),rms=Math.Sqrt(samples.Average(v=>(double)v*v)),mean=samples.Average(v=>(double)v);
            float jump=samples.Skip(1).Select((v,i)=>Mathf.Abs(v-samples[i])).Max();
            require(samples.All(v=>!float.IsNaN(v)&&!float.IsInfinity(v))&&peak<=.62001&&rms>.03&&rms<.25&&Math.Abs(mean)<.002,"cue is finite, unclipped and within RMS/DC bounds "+star);
            require(samples[0]==0&&samples[samples.Length-1]==0&&jump<.15f,"cue has silent endpoints and smooth sample transitions "+star);
            using(var writer=new BinaryWriter(File.Create(Path.Combine(folder,"star-"+star+"-default-volume.wav"))))
            {
                int bytes=samples.Length*2;writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(bytes+36);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(48000);writer.Write(96000);writer.Write((short)2);writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(bytes);
                for(int i=0;i<samples.Length;i++)writer.Write((short)Mathf.RoundToInt(samples[i]*(StarUpgradeAudio.DefaultVolume/100f)*Mathf.Clamp01(i/(48000f*.045f))*32767));
            }
            Debug.Log("STAR SOUND PCM: rank="+star+", samples="+samples.Length+", peak="+peak.ToString("0.000000")+", rms="+rms.ToString("0.000000")+", dc="+mean.ToString("0.000000")+", jump="+jump.ToString("0.000000"));
        }
        require(StarUpgradeAudio.ReviewClip(3).length>StarUpgradeAudio.ReviewClip(2).length,"three-star flourish is distinct and longer than two-star cue");
        Debug.Log("STAR SOUND RULES COMPLETE: generated PCM, clip cache, waveform bounds and transactional highest-rank queue");
    }
    public static IEnumerator Playback(Action<bool,string> require)
    {
        using(var saved=new Preferences())
        {
            StarUpgradeAudio.Configure(35,false);StarUpgradeAudio.Cancel();int before=StarUpgradeAudio.PlayedCount;
            StarUpgradeAudio.Request(2);StarUpgradeAudio.Request(3);StarUpgradeAudio.Request(2);
            yield return new WaitForSecondsRealtime(.12f);
            require(StarUpgradeAudio.PlayedCount==before+1&&StarUpgradeAudio.LastPlayedStar==3&&StarUpgradeAudio.IsPlaying,"real Unity player emits one final-rank cue for a chain merge (before="+before+"; "+StarUpgradeAudio.Diagnostic+")");
            require(StarUpgradeAudio.VoiceCount==2&&StarUpgradeAudio.VoiceGain<=.3501f,"audio uses two fixed voices with bounded crossfade gain");
            StarUpgradeAudio.Request(2);yield return new WaitForSecondsRealtime(.10f);
            require(StarUpgradeAudio.PlayedCount==before+1,"recent three-star cue is not interrupted by a weaker merge cue");
            StarUpgradeAudio.Configure(50,true);require(!StarUpgradeAudio.Request(3)&&!StarUpgradeAudio.IsPlaying&&StarUpgradeAudio.PendingStar==0,"mute stops current and pending audio");
            StarUpgradeAudio.ReloadSettings();require(StarUpgradeAudio.Muted&&StarUpgradeAudio.Volume==50,"saved volume and mute survive reload");
            StarUpgradeAudio.Configure(0,false);require(!StarUpgradeAudio.Request(2),"zero volume is silent even without mute");
            StarUpgradeAudio.Configure(200,false);require(StarUpgradeAudio.Volume==100,"corrupt or oversized volume is clamped");
            StarUpgradeAudio.Request(2);StarUpgradeAudio.Cancel();yield return new WaitForSecondsRealtime(.1f);
            require(StarUpgradeAudio.PlayedCount==before+1&&!StarUpgradeAudio.IsPlaying,"cancelled pending cue cannot leak across mode changes");
        }
        Debug.Log("STAR SOUND PLAYBACK COMPLETE: live AudioSource, two-voice limit, priority, mute/zero volume, persistence and cancellation; validation output muted");
    }
}
public sealed class StarSoundPlayerValidation : MonoBehaviour
{
    int checks;
    void Require(bool ok,string message){if(!ok){Debug.LogError("SOUND SMOKE FAILED: "+message);Application.Quit(2);throw new InvalidOperationException(message);}checks++;}
    IEnumerator Start()
    {
        Application.runInBackground=true;Application.targetFrameRate=60;
        StarSoundValidation.Rules(Require);yield return StarSoundValidation.Playback(Require);
        Debug.Log("SOUND SMOKE COMPLETE: "+checks+" checks");Application.Quit();
    }
}
#endif
