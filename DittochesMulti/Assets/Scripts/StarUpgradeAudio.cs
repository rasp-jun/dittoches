#if DITTOCHES_PORTABLE_PREVIEW
using PlayerPrefs = PortablePreviewPrefs;
#endif
using UnityEngine;

public sealed class StarUpgradeAudio : MonoBehaviour
{
    public const string VolumeKey="dittoches.upgradeAudio.volume.v1",MuteKey="dittoches.upgradeAudio.mute.v1";
    public const int DefaultVolume=35;
    static StarUpgradeAudio instance;
    static bool loaded,muted;
    static int volume;
    static readonly AudioClip[] clips=new AudioClip[2];
    readonly StarUpgradeSoundQueue queue=new StarUpgradeSoundQueue();
    readonly AudioSource[] voices=new AudioSource[2];
    AudioListener ownListener;
    int current=-1,playingStar;
    float playedAt;
    static void Load(){if(loaded)return;volume=Mathf.Clamp(PlayerPrefs.GetInt(VolumeKey,DefaultVolume),0,100);muted=PlayerPrefs.GetInt(MuteKey,0)!=0;loaded=true;}
    public static int Volume {get{Load();return volume;}}
    public static bool Muted {get{Load();return muted;}}
    public static bool Audible {get{return !Muted&&Volume>0;}}
    static bool Validation
    {
        get{
#if DITTOCHES_PORTABLE_PREVIEW
            return PortablePreviewPrefs.IsValidation;
#else
            return false;
#endif
        }
    }
    public static void Configure(int percent,bool silence)
    {
        Load();percent=Mathf.Clamp(percent,0,100);if(volume==percent&&muted==silence)return;
        volume=percent;muted=silence;PlayerPrefs.SetInt(VolumeKey,volume);PlayerPrefs.SetInt(MuteKey,muted?1:0);PlayerPrefs.Save();
        if(!Audible)Cancel();else if(instance!=null)instance.RefreshVolume();CombatAudio.RefreshSettings();
    }
    public static bool Request(int star)
    {
        if((star!=2&&star!=3)||!Audible||!Validation&&!Application.isFocused)return false;
        EnsureInstance();
#if DITTOCHES_PORTABLE_PREVIEW
        RequestedCount++;
#endif
        return instance.queue.Add(star,Time.unscaledTime);
    }
    static void EnsureInstance()
    {
        if(instance==null)
        {
            var root=new GameObject("Star upgrade audio"){hideFlags=HideFlags.HideInHierarchy};DontDestroyOnLoad(root);
            instance=root.AddComponent<StarUpgradeAudio>();
        }
    }
    public static void EnsureSharedListener(){EnsureInstance();instance.EnsureListener();}
    public static bool ChimePlaying {get{return instance!=null&&instance.current>=0&&instance.voices[instance.current].isPlaying;}}
    public static void Preview(int star){Cancel();Request(star);}
    public static void Cancel()
    {
        if(instance==null)return;instance.queue.Clear();
        foreach(var source in instance.voices)if(source!=null)source.Stop();instance.current=-1;
    }
    static AudioClip Clip(int star)
    {
        int index=star-2;if(clips[index]!=null)return clips[index];var samples=StarUpgradeChime.Samples(star);
        var clip=AudioClip.Create(star+" star promotion",samples.Length,1,StarUpgradeChime.SampleRate,false);clip.SetData(samples,0);clips[index]=clip;return clip;
    }
    void Awake()
    {
        for(int i=0;i<voices.Length;i++)
        {
            var source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=false;source.spatialBlend=0;source.dopplerLevel=0;
            source.bypassReverbZones=true;source.mute=Validation;voices[i]=source;
        }
    }
    void EnsureListener()
    {
        bool other=false;foreach(var listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            if(listener!=ownListener&&listener.isActiveAndEnabled){other=true;break;}
        if(ownListener==null&&!other)ownListener=gameObject.AddComponent<AudioListener>();
        if(ownListener!=null)ownListener.enabled=!other;
    }
    void Update()
    {
        int star=queue.Take(Time.unscaledTime);
        if(star>0&&Audible)
        {
            // Let a recent three-star flourish finish instead of cutting it with a lesser cue.
            if(!(star==2&&playingStar==3&&current>=0&&voices[current].isPlaying&&Time.unscaledTime-playedAt<.65f))
            {
                EnsureListener();current=(current+1)%voices.Length;voices[current].Stop();voices[current].clip=Clip(star);
                playedAt=Time.unscaledTime;playingStar=star;voices[current].volume=Volume/100f;voices[current].Play();
#if DITTOCHES_PORTABLE_PREVIEW
                PlayedCount++;LastPlayedStar=star;
#endif
            }
        }
        RefreshVolume();
    }
    void RefreshVolume()
    {
        float gain=Audible?Volume/100f:0;
        for(int i=0;i<voices.Length;i++)
        {
            float blend=Mathf.Clamp01((Time.unscaledTime-playedAt)/.045f),fade=i==current?blend:1-blend;
            voices[i].volume=gain*fade;if(i!=current&&fade<=0&&voices[i].isPlaying)voices[i].Stop();
        }
    }
    void OnApplicationFocus(bool focused){if(!focused&&!Validation)Cancel();}
    void OnDestroy()
    {
        if(instance!=this)return;instance=null;
        for(int i=0;i<clips.Length;i++)if(clips[i]!=null){Destroy(clips[i]);clips[i]=null;}
    }
#if DITTOCHES_PORTABLE_PREVIEW
    public static int RequestedCount,PlayedCount,LastPlayedStar;
    public static string Diagnostic {get{return "requested="+RequestedCount+", played="+PlayedCount+", last="+LastPlayedStar+", pending="+PendingStar+", playing="+IsPlaying+", volume="+Volume+", muted="+Muted+", audioTime="+AudioSettings.dspTime+", active="+(instance!=null&&instance.isActiveAndEnabled)+", listenerPause="+AudioListener.pause+(instance==null?"":", current="+instance.current+", age="+(Time.unscaledTime-instance.playedAt));}}
    public static int PendingStar {get{return instance==null?0:instance.queue.Star;}}
    public static int VoiceCount {get{return instance==null?0:instance.voices.Length;}}
    public static bool IsPlaying {get{return instance!=null&&instance.current>=0&&instance.voices[instance.current].isPlaying;}}
    public static float VoiceGain {get{float gain=0;if(instance!=null)foreach(var v in instance.voices)if(v.isPlaying)gain+=v.volume;return gain;}}
    public static AudioClip ReviewClip(int star){return Clip(star);}
    public static void ReloadSettings(){loaded=false;Load();Cancel();}
#endif
}
