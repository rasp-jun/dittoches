#if DITTOCHES_PORTABLE_PREVIEW
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    void ValidateNativeCombatAudio()
    {
        bool wasBattle=battling,wasLobby=lobby;int wasScout=scoutedRival,wasArt=artPack;var oldArena=arena;var traces=battleTraces.ToArray();var casts=skillCasts.ToArray();
        using(var prefs=new StarSoundValidation.Preferences())using(var review=new TacticalArena(new Rect(0,0,640,360)))
        {
            try
            {
                StarUpgradeAudio.Configure(35,false);CombatAudio.Configure(true);battling=true;lobby=false;scoutedRival=-1;artPack=0;arena=review;
                foreach(string id in DigimonModelLibrary.Roster)
                {
                    var source=new Fighter{unit=new Unit(RosterById[id]),attacks=1};var target=new Fighter{unit=new Unit(RosterById["koromon"]),renderPos=new Vector2(2,3)};
                    battleTraces.Clear();RecordAttackTrace(source,target);var trace=battleTraces.Last();float travel=BasicAttackPresentation.Travel(id);int n=CombatAudio.RequestedCount;
                    review.BeginFrame(-1,-1,-1,false);trace.started=Time.unscaledTime-travel-.01f;DrawBasicAttacks();DrawBasicAttacks();
                    Require(CombatAudio.RequestedCount==n+1&&source.audioAttackSerial==1,"actual native basic impact routes once "+id);
                    RecordAttackTrace(source,target);battleTraces.Last().started=trace.started;DrawBasicAttacks();Require(CombatAudio.RequestedCount==n+1,"splash traces cannot multiply the same attack sound "+id);
                    var s=DigimonSkillCatalog.Find(id);var cast=new SkillCast{caster=source,target=target,skill=s,aim=new Vector2(2,3),age=s.Impact-.01f};skillCasts.Clear();skillCasts.Add(cast);DrawDigimonSkills();
                    Require(!cast.soundPlayed&&CombatAudio.RequestedCount==n+1,"native skill waits for impact "+id);cast.age=s.Impact+.01f;DrawDigimonSkills();DrawDigimonSkills();
                    Require(cast.soundPlayed&&CombatAudio.RequestedCount==n+2,"actual native skill impact routes once "+id);
                    cast.soundPlayed=false;cast.age=s.Impact+.2f;DrawDigimonSkills();Require(cast.soundPlayed&&CombatAudio.RequestedCount==n+2,"late native skill never queues historical sound "+id);
                    source.attacks=2;RecordAttackTrace(source,target);battleTraces.Last().started=Time.unscaledTime-travel-.01f;StarUpgradeAudio.Configure(35,true);DrawBasicAttacks();StarUpgradeAudio.Configure(35,false);DrawBasicAttacks();
                    Require(source.audioAttackSerial==2&&CombatAudio.RequestedCount==n+2,"muted impact is consumed before unmute "+id);
                }
                nativeAudioActive=true;battling=false;SyncNativeCombatAudio();Require(CombatAudio.ActiveVoices==0&&!nativeAudioActive,"native battle exit cancels current combat sounds");
            }
            finally{arena=oldArena;battling=wasBattle;lobby=wasLobby;scoutedRival=wasScout;artPack=wasArt;nativeAudioActive=false;battleTraces.Clear();battleTraces.AddRange(traces);skillCasts.Clear();skillCasts.AddRange(casts);}
        }
        Debug.Log("COMBAT AUDIO NATIVE COMPLETE: 47 actual basic/skill draw paths, splash dedupe, timing, stale and muted consumption, battle exit");
    }
}
public sealed partial class MultiLauncher
{
    void ValidateOnlineCombatAudio()
    {
        int savedArt=artPack;
        using(var prefs=new StarSoundValidation.Preferences())
        {
            try
            {
                artPack=0;StarUpgradeAudio.Configure(35,false);CombatAudio.Configure(true);
                foreach(string id in DigimonModelLibrary.Roster)foreach(int side in new[]{0,1})
                {
                    ClearOnlineCombatAudio();var skill=DigimonSkillCatalog.Find(id);var source=new Fighter{key=1,id=id,attacks=1,attackAt=.1f,target=2,hp=10};var target=new Fighter{key=2,id="koromon",x=1,y=3,hp=10};
                    var room=new Room{id="audio",round=1,side=side,phase="battle",battleDuration=10,frames=new[]{new Frame{time=0,units=new[]{target}},new Frame{time=.1f,units=new[]{source,target}}},skillEvents=new[]{new SkillEvent{serial=1,caster=1,target=2,id=id,started=.1f,tx=1,ty=3}}};
                    int n=CombatAudio.RequestedCount;ObserveOnlineCombatAudio(room,10);
                    float end=.1f+Mathf.Max(skill.Impact,BasicAttackPresentation.Travel(id))+.15f;
                    for(float t=.025f;t<end;t+=.025f){ObserveOnlineCombatAudio(room,10-t);ObserveOnlineCombatAudio(room,10-t);}
                    OnlineRequire(CombatAudio.RequestedCount==n+2,"online actual event observer emits one basic and one skill across both sides "+id+"/"+side);
                    ObserveOnlineCombatAudio(room,9.95f);OnlineRequire(CombatAudio.RequestedCount==n+2,"online rewind does not repeat heard events "+id);
                    ClearOnlineCombatAudio();ObserveOnlineCombatAudio(room,10-end);OnlineRequire(CombatAudio.RequestedCount==n+2,"online reconnect skips historical impacts "+id);
                    room.phase="prepare";ObserveOnlineCombatAudio(room,10);OnlineRequire(!onlineAudioActive&&CombatAudio.ActiveVoices==0,"online preparation clears battle audio "+id);
                }
            }
            finally{ClearOnlineCombatAudio();artPack=savedArt;}
        }
        Debug.Log("COMBAT AUDIO ONLINE COMPLETE: 47 species both sides, first impact, repeated repaint, seek/reconnect and phase transition");
    }
}
#endif
