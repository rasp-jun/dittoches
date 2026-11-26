using UnityEngine;

public sealed partial class NativeGame
{
    bool nativeAudioActive;
    bool NativeAudioVisible {get{return battling&&artPack==0&&!lobby&&scoutedRival<0;}}
    void SyncNativeCombatAudio()
    {
        bool active=NativeAudioVisible;if(nativeAudioActive&&!active)CombatAudio.Cancel();nativeAudioActive=active;
    }
    void ObserveNativeBasicSound(BattleTrace trace,float age)
    {
        float travel=BasicAttackPresentation.Travel(trace.id);
        if(!NativeAudioVisible||trace.source==null||trace.serial<=trace.source.audioAttackSerial||age<travel)return;
        trace.source.audioAttackSerial=trace.serial;
        if(age-travel<=.12f)CombatAudio.Request(CombatSound.Basic(trace.id),false,trace.to.x/12f);
    }
    void ObserveNativeSkillSound(SkillCast cast)
    {
        if(!NativeAudioVisible||cast.cancelled||cast.soundPlayed||cast.age<cast.skill.Impact)return;
        cast.soundPlayed=true;
        if(cast.age-cast.skill.Impact<=.12f)CombatAudio.Request(CombatSound.Skill(cast.skill.visual),true,(cast.aim.x-3)/6f);
    }
}
