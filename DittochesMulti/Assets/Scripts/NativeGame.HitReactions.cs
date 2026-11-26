using UnityEngine;

public sealed partial class NativeGame
{
    void RecordHitReaction(Fighter source,Fighter target,float now)
    {
        if(artPack!=0)return;
        if(target.hp<=0){target.hitReactions=null;return;}
        if(target.hitReactions==null)target.hitReactions=new CombatHitTrack();
        target.hitReactions.Prune(now);
        float delay=applyingSkillDamage?0:BasicAttackPresentation.Travel(source.unit.def.id);
        Vector3 direction=TacticalArena.CellWorld(target.renderPos.x,target.renderPos.y)-TacticalArena.CellWorld(source.renderPos.x,source.renderPos.y);
        target.hitReactions.Add(now+delay,direction);
    }
    static CombatHitPresentation NativeHitReaction(Fighter fighter,float now)
    {return fighter.dead||fighter.hitReactions==null?CombatHitPresentation.Empty:fighter.hitReactions.At(now);}
}
