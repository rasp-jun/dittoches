#if DITTOCHES_PORTABLE_PREVIEW
using PlayerPrefs=PortablePreviewPrefs;
#endif
using UnityEngine;

public sealed class CombatAudio : MonoBehaviour
{
    public const string EnabledKey="dittoches.combatAudio.enabled.v1";
    public const int VoiceLimit=6;
    static CombatAudio instance;
    static bool loaded,enabledSetting;
    static readonly AudioClip[] clips=new AudioClip[28];
    readonly AudioSource[] voices=new AudioSource[VoiceLimit];
    readonly float[] weights=new float[VoiceLimit],started=new float[VoiceLimit],familyAt=new float[28];
    float basicAt=-100,skillAt=-100;
    public static bool Enabled {get{if(!loaded){enabledSetting=PlayerPrefs.GetInt(EnabledKey,1)!=0;loaded=true;}return enabledSetting;}}
    public static bool Audible {get{return Enabled&&StarUpgradeAudio.Audible;}}
    static bool Validation {get{
#if DITTOCHES_PORTABLE_PREVIEW
        return PortablePreviewPrefs.IsValidation;
#else
        return false;
#endif
    }}
    public static void Configure(bool value){if(Enabled==value)return;enabledSetting=value;PlayerPrefs.SetInt(EnabledKey,value?1:0);PlayerPrefs.Save();if(!value)Cancel();}
    public static void RefreshSettings(){if(!Audible)Cancel();else if(instance!=null)instance.RefreshVolume();}
    public static void Cancel()
    {
        if(instance==null)return;foreach(var source in instance.voices)source.Stop();
        instance.basicAt=instance.skillAt=-100;for(int i=0;i<instance.familyAt.Length;i++)instance.familyAt[i]=-100;
    }
    static AudioClip Clip(CombatSound.Kind kind,bool skill)
    {
        int index=(int)kind*2+(skill?1:0);if(clips[index]!=null)return clips[index];
        var samples=CombatSound.Samples(kind,skill);var clip=AudioClip.Create("Combat "+kind+(skill?" skill":" basic"),samples.Length,1,CombatSound.SampleRate,false);
        clip.SetData(samples,0);return clips[index]=clip;
    }
    public static bool Request(CombatSound.Kind kind,bool skill,float pan=0)
    {
        if((int)kind<0||(int)kind>13||!Audible||!Validation&&!Application.isFocused)return false;
#if DITTOCHES_PORTABLE_PREVIEW
        RequestedCount++;
#endif
        if(instance==null){var root=new GameObject("Combat audio"){hideFlags=HideFlags.HideInHierarchy};DontDestroyOnLoad(root);instance=root.AddComponent<CombatAudio>();}
        return instance.Play(kind,skill,pan);
    }
    public static void Preview(CombatSound.Kind kind,bool skill){Cancel();Request(kind,skill);}
    void Awake()
    {
        for(int i=0;i<voices.Length;i++)
        {
            var source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=false;source.spatialBlend=0;source.dopplerLevel=0;
            source.bypassReverbZones=true;source.mute=Validation;source.Stop();voices[i]=source;
        }
        for(int i=0;i<familyAt.Length;i++)familyAt[i]=-100;
        // Warm all short clips once; attacks reuse the fixed cache.
        for(int i=0;i<14;i++){Clip((CombatSound.Kind)i,false);Clip((CombatSound.Kind)i,true);}
    }
    bool Play(CombatSound.Kind kind,bool skill,float pan)
    {
        float now=Time.unscaledTime;int family=(int)kind*2+(skill?1:0);
        if(now-familyAt[family]<(skill?.09f:.065f)||now-(skill?skillAt:basicAt)<(skill?.045f:.035f))return false;
        // Two voices stay available to skills even during a crowded basic-attack volley.
        int slot=-1;for(int i=0;i<(skill?VoiceLimit:VoiceLimit-2);i++)if(!voices[i].isPlaying){slot=i;break;}
        if(slot<0)return false;
        StarUpgradeAudio.EnsureSharedListener();var source=voices[slot];source.clip=Clip(kind,skill);source.panStereo=float.IsNaN(pan)?0:Mathf.Clamp(pan,-.5f,.5f);
        weights[slot]=skill?1.3f:.85f;started[slot]=now;source.volume=0;source.Play();familyAt[family]=now;if(skill)skillAt=now;else basicAt=now;
        RefreshVolume();
#if DITTOCHES_PORTABLE_PREVIEW
        PlayedCount++;LastKind=kind;LastSkill=skill;LastPan=source.panStereo;
#endif
        return true;
    }
    void Update(){RefreshSettings();}
    void RefreshVolume()
    {
        float total=0;for(int i=0;i<voices.Length;i++)if(voices[i].isPlaying)total+=weights[i];
        float budget=StarUpgradeAudio.Volume/100f*.6f*(StarUpgradeAudio.ChimePlaying?.45f:1);
        for(int i=0;i<voices.Length;i++)if(voices[i].isPlaying)voices[i].volume=budget*weights[i]/Mathf.Max(2,total)*Mathf.Clamp01((Time.unscaledTime-started[i])/.005f);
    }
    void OnApplicationFocus(bool focused){if(!focused&&!Validation)Cancel();}
    void OnDestroy(){if(instance!=this)return;instance=null;for(int i=0;i<clips.Length;i++)if(clips[i]!=null){Destroy(clips[i]);clips[i]=null;}}
#if DITTOCHES_PORTABLE_PREVIEW
    public static int RequestedCount,PlayedCount;public static CombatSound.Kind LastKind;public static bool LastSkill;public static float LastPan;
    public static int ActiveVoices {get{int count=0;if(instance!=null)foreach(var s in instance.voices)if(s.isPlaying)count++;return count;}}
    public static float Gain {get{float sum=0;if(instance!=null)foreach(var s in instance.voices)if(s.isPlaying)sum+=s.volume;return sum;}}
    public static AudioClip ReviewClip(CombatSound.Kind kind,bool skill){return Clip(kind,skill);}
    public static void ReloadSettings(){loaded=false;Cancel();}
#endif
}
