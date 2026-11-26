using UnityEngine;

public sealed partial class MultiLauncher
{
    readonly CombatAudioTimeline combatAudioTimeline=new CombatAudioTimeline();
    bool onlineAudioActive;string audioRoom;int audioRound,audioSide;
    void ClearOnlineCombatAudio()
    {
        if(onlineAudioActive)CombatAudio.Cancel();onlineAudioActive=false;combatAudioTimeline.Clear();
    }
    void SyncOnlineCombatAudio()
    {
        if(onlineAudioActive&&(solo||artPack!=0||state==null||state.room==null||state.room.phase!="battle"||OnlineScouting))ClearOnlineCombatAudio();
    }
    void ObserveOnlineCombatAudio(Room room,float remaining)
    {
        if(room==null||room.phase!="battle"||artPack!=0||OnlineScouting||room.frames==null||room.frames.Length==0){ClearOnlineCombatAudio();return;}
        int side=BattleSide(room);float time=CombatTime(room,remaining);
        if(!onlineAudioActive||audioRoom!=room.id||audioRound!=room.round||audioSide!=side)
        {ClearOnlineCombatAudio();audioRoom=room.id;audioRound=room.round;audioSide=side;onlineAudioActive=true;}
        if(combatAudioTimeline.Begin(time))CombatAudio.Cancel();
        // Skills first: simultaneous basic volleys cannot occupy their reserved voices.
        if(room.skillEvents!=null)foreach(var cast in room.skillEvents)
        {
            var skill=DigimonSkillCatalog.Find(cast.id);if(skill==null)continue;
            if(combatAudioTimeline.Observe(-1L-(uint)cast.serial,cast.started+skill.Impact))
                CombatAudio.Request(CombatSound.Skill(skill.visual),true,((side==1?6-cast.tx:cast.tx)-3)/6f);
        }
        int frame=Mathf.FloorToInt(CombatProgress(room,remaining));var units=room.frames[frame].units;if(units==null)return;
        foreach(var fighter in units)
        {
            if(fighter==null||fighter.attacks<=0)continue;
            long key=((long)(uint)fighter.key<<32)|(uint)fighter.attacks;
            if(!combatAudioTimeline.Observe(key,fighter.attackAt+BasicAttackPresentation.Travel(fighter.id)))continue;
            var target=BasicAttackTarget(room,fighter,frame);if(target==null)continue;
            CombatAudio.Request(CombatSound.Basic(fighter.id),false,((side==1?6-target.x:target.x)-3)/6f);
        }
    }
}
