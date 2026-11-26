using UnityEngine;

public sealed partial class NativeGame
{
    readonly WorldMotionTracker<Fighter> faithfulWorldMotion=new WorldMotionTracker<Fighter>();
    bool NativeBasicAttackInRange(Fighter f)
    {return f.mana<f.maxMana&&f.target!=null&&!f.target.dead&&DigimonCombatMath.InAttackRange(f.pos.x,f.pos.y,f.target.pos.x,f.target.pos.y,Meta(f.unit.def.id).range);}
    Vector3 NativeFighterFacing(Fighter f,Vector3 world,WorldMotionTracker<Fighter>.Motion movement,bool attackInRange)
    {
        if(f.attackFlash>0||f.skillCast!=null)return TacticalArena.CellWorld(f.attackTarget.x,f.attackTarget.y)-world;
        if(attackInRange&&f.cooldown>=0&&f.cooldown<BasicAttackPresentation.Windup(f.unit.def.id))
            return TacticalArena.CellWorld(f.target.renderPos.x,f.target.renderPos.y)-world;
        if(movement.speed>.1f)return new Vector3(movement.x,0,movement.z);
        return f.target!=null?TacticalArena.CellWorld(f.target.renderPos.x,f.target.renderPos.y)-world:new Vector3(0,0,f.enemy?-1:1);
    }
    private void RenderArenaFighter(Fighter f)
    {
        if(f.dead&&Time.unscaledTime-f.diedAt>=(artPack==0&&FaithfulModelData.Available(f.unit.def.id)?2.2f:.45f))return;
        float death=f.dead?Mathf.Max(.0001f,Mathf.Clamp01((Time.unscaledTime-f.diedAt)/(artPack==0&&FaithfulModelData.Available(f.unit.def.id)?2.2f:.45f))):0;
        bool skeletal=artPack==0&&(DigimonModelLibrary.HasModel(f.unit.def.id)||FaithfulModelData.Available(f.unit.def.id));
        Vector3 world=FighterWorld(f);
        faithfulWorldMotion.EnsureContext(fighters.Count>0?fighters[0]:f,round);
        var movement=faithfulWorldMotion.At(f,world.x,world.z,Time.unscaledTime);
        bool attackInRange=NativeBasicAttackInRange(f);
        arena.SetActor(f,world-Vector3.up*(skeletal?0:death*.15f),Tex(f.unit.def.id=="apocalymon"&&(f.attackFlash>0||f.skillFlash>0)?"Apocalymon_Attack":UnitSprite(f.unit.def)),f.enemy?new Color(1,.35f,.28f):new Color(.25f,1,.62f),(1+(artPack==0?0:f.skillFlash*.16f))*(skeletal?1:1-death*.88f),f.hitFlash*2);
        if(skeletal)arena.FaceActor(f,NativeFighterFacing(f,world,movement,attackInRange));
        bool gearTarget=!f.dead&&!f.enemy&&selectedItem>=0;
        var hit=NativeHitReaction(f,Time.unscaledTime);
        arena.CombatMotion(f,f.attacks,f.cooldown,attackInRange,hit.time,hit.direction);
        arena.DecorateActor(f,!f.dead&&inspectedUnit==f.unit||gearTarget,0,artPack==0?hit.Flash(Time.unscaledTime):f.hitFlash/.18f,f.healFlash/.28f,f.shieldFlash/.35f,gearTarget&&!CanEquipSelected(f.unit));
        if(artPack==0)arena.PoseDigimon(f,f.unit.def.id,movement.speed,movement.x,Time.unscaledTime,
            f.skillCast!=null?f.skillCast.age:-1,f.attackFlash,death,f.unit.star);
        else arena.PoseCombatActor(f,f.renderVelocity.magnitude,f.renderVelocity.x,Time.unscaledTime,death);
    }
}
