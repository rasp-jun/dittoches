using UnityEngine;

public sealed partial class NativeGame
{
    private void RenderArenaFighter(Fighter f)
    {
        if(f.dead&&Time.unscaledTime-f.diedAt>=(artPack==0&&FaithfulModelData.Available(f.unit.def.id)?2.2f:.45f))return;
        float death=f.dead?Mathf.Max(.0001f,Mathf.Clamp01((Time.unscaledTime-f.diedAt)/(artPack==0&&FaithfulModelData.Available(f.unit.def.id)?2.2f:.45f))):0;
        bool skeletal=artPack==0&&(DigimonModelLibrary.HasModel(f.unit.def.id)||FaithfulModelData.Available(f.unit.def.id));
        arena.SetActor(f,FighterWorld(f)-Vector3.up*(skeletal?0:death*.15f),Tex(f.unit.def.id=="apocalymon"&&(f.attackFlash>0||f.skillFlash>0)?"Apocalymon_Attack":UnitSprite(f.unit.def)),f.enemy?new Color(1,.35f,.28f):new Color(.25f,1,.62f),(1+(artPack==0?0:f.skillFlash*.16f))*(skeletal?1:1-death*.88f),f.hitFlash*2);
        if(skeletal)
        {
            Vector3 facing=f.renderVelocity.sqrMagnitude>.01f?new Vector3(f.renderVelocity.x,0,-f.renderVelocity.y):
                f.target!=null?TacticalArena.CellWorld(f.target.renderPos.x,f.target.renderPos.y)-FighterWorld(f):new Vector3(0,0,f.enemy?-1:1);
            arena.FaceActor(f,facing);
        }
        bool gearTarget=!f.dead&&!f.enemy&&selectedItem>=0;
        arena.CombatMotion(f,f.attacks,f.cooldown,f.mana<f.maxMana&&f.target!=null&&!f.target.dead&&DigimonCombatMath.InAttackRange(f.pos.x,f.pos.y,f.target.pos.x,f.target.pos.y,Meta(f.unit.def.id).range));
        arena.DecorateActor(f,!f.dead&&inspectedUnit==f.unit||gearTarget,0,f.hitFlash/.18f,f.healFlash/.28f,f.shieldFlash/.35f,gearTarget&&!CanEquipSelected(f.unit));
        if(artPack==0)arena.PoseDigimon(f,f.unit.def.id,f.renderVelocity.magnitude,f.attackTarget.x-f.renderPos.x,Time.unscaledTime,
            f.skillCast!=null?f.skillCast.age:-1,f.attackFlash,death,f.unit.star);
        else arena.PoseCombatActor(f,f.renderVelocity.magnitude,f.renderVelocity.x,Time.unscaledTime,death);
    }
}
