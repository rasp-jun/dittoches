using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class NativeGame
{
    float synergyBattleAge;
    float EffectiveArmor(Fighter f){return f.build.armor*(1-f.armorShred);}
    void ApplyBurn(Fighter source,Fighter target)
    {
        if(target.dead||source.build.burn<=0||source.build.burn<target.burnRate)return;
        if(target.burnRemaining<=0)target.burnClock=0;
        target.burnRate=source.build.burn;target.burnRemaining=3;target.burnSource=source;
    }
    void ApplySkillTraits(Fighter source,Fighter target)
    {
        if(artPack!=0||target.dead)return;
        if(source.build.armorShred>0&&source.build.armorShred>=target.armorShred)
        {target.armorShred=source.build.armorShred;target.shredRemaining=4;}
        ApplyBurn(source,target);
    }
    void SynergyDamage(Fighter source,Fighter target,float amount,string type="physical")
    {
        bool previous=applyingSkillDamage;applyingSkillDamage=true;
        try{DealDamage(source,target,amount,type);}finally{applyingSkillDamage=previous;}
    }
    void TickSynergyEffects(float dt)
    {
        if(artPack!=0)return;
        synergyBattleAge+=dt;
        foreach(var f in fighters)
        {
            f.battleAge=synergyBattleAge;
            if(f.dead){f.burnRemaining=f.burnClock=f.burnRate=f.shredRemaining=f.armorShred=0;continue;}
            f.shredRemaining=Mathf.Max(0,f.shredRemaining-dt);
            if(f.shredRemaining<.00001f)f.armorShred=0;
            f.burnClock+=Mathf.Min(dt,f.burnRemaining);f.burnRemaining=Mathf.Max(0,f.burnRemaining-dt);
            while(f.burnClock+.00001f>=1)
            {f.burnClock-=1;if(f.burnSource!=null)SynergyDamage(f.burnSource,f,f.maxHp*f.burnRate,"magic");}
            if(f.burnRemaining<.00001f)f.burnRate=f.burnClock=f.burnRemaining=0;
        }
    }
    void ApplyArtillerySplash(Fighter source,Fighter target)
    {
        if(artPack!=0||source.attacks<=0||source.attacks%3!=0||source.build.thirdSplash<=0)return;
        var victims=fighters.Where(f=>!f.dead&&f.enemy!=source.enemy&&f!=target&&DigimonCombatMath.HexDistance(f.pos.x,f.pos.y,target.pos.x,target.pos.y)<=1.00001f)
            .OrderBy(f=>DigimonCombatMath.HexDistance(f.pos.x,f.pos.y,target.pos.x,target.pos.y)).Take(2).ToArray();
        foreach(var victim in victims)SynergyDamage(source,victim,AttackDamage(source)*source.build.thirdSplash);
    }
    sealed class TeamCrisis
    {public Fighter source;public Fighter[] team;public int bit,kind;public float value;}
    void ResolveTeamCrisis()
    {
        if(artPack!=0)return;
        // A dead caster must never resume an old windup after being revived.
        foreach(var cast in skillCasts.Where(c=>!c.released&&c.caster.dead)){cast.cancelled=true;cast.caster.skillCast=null;}
        skillCasts.RemoveAll(c=>c.cancelled);
        var pending=new List<TeamCrisis>();
        foreach(bool enemy in new[]{false,true})
        {
            var team=fighters.Where(f=>f.enemy==enemy).ToArray();
            if(!team.Any(f=>!f.dead&&f.hp>0)||team.Sum(f=>f.hp)>team.Sum(f=>f.maxHp)*(.4f+1e-7f))continue;
            foreach(int bit in new[]{1,4,2})
            {
                if(team.Any(f=>(f.synergyUsed&bit)!=0))continue;
                var source=team.Where(f=>CrisisValue(f,bit)>0).OrderBy(f=>f.dead).ThenByDescending(f=>CrisisValue(f,bit)).FirstOrDefault();
                if(source==null)continue;int kind=bit==1?3:bit==2?4:5;
                foreach(var f in team){f.synergyUsed|=bit;f.synergyAt=synergyBattleAge;f.synergyKind=kind;}
                pending.Add(new TeamCrisis{source=source,team=team,bit=bit,kind=kind,value=CrisisValue(source,bit)});
            }
        }
        // Both teams latch at the same health boundary; rescue effects precede blasts.
        foreach(var effect in pending.OrderBy(e=>e.bit==2))
        {
            var source=effect.source;var team=effect.team;
            if(effect.bit==1)
            {
                foreach(var f in team.Where(f=>!f.dead&&f.build.teamRally>0))
                {f.stun=0;Heal(source,f,f.maxHp*effect.value);Shield(source,f,f.maxHp*effect.value);f.mana=Mathf.Min(f.maxMana,f.mana+30);}
            }
            else if(effect.bit==4)
            {
                var revived=team.Where(f=>f.dead).OrderByDescending(f=>f.unit.def.cost).ThenByDescending(f=>f.unit.star).FirstOrDefault();
                if(revived!=null)
                {
                    revived.dead=false;revived.hp=revived.maxHp*effect.value;revived.shield=revived.mana=revived.stun=0;
                    revived.cooldown=.25f;revived.target=null;revived.skillCast=null;revived.diedAt=0;revived.revived=true;revived.revivedAt=synergyBattleAge;
                    revived.attackFlash=revived.skillFlash=0;revived.healFlash=.5f;revived.renderPos=revived.pos;revived.renderVelocity=Vector2.zero;
                    revived.burnRemaining=revived.burnClock=revived.burnRate=revived.armorShred=revived.shredRemaining=0;revived.burnSource=null;
                    source.healingDone+=revived.hp;
                }
                foreach(var f in team.Where(f=>!f.dead))Shield(source,f,f.maxHp*.15f);
            }
            else
            {
                foreach(var f in fighters.Where(f=>!f.dead&&f.enemy!=source.enemy).ToArray())
                {SynergyDamage(source,f,f.maxHp*effect.value,"magic");ApplyBurn(source,f);}
            }
        }
        foreach(var team in pending.Select(e=>e.team).Distinct())
        {
            int kind=team[0].synergyKind;foreach(var f in team)f.synergyKind=0;
            var hero=team.Where(f=>!f.dead).OrderByDescending(f=>f.revived).ThenByDescending(f=>f.unit.def.cost).ThenByDescending(f=>f.unit.star).FirstOrDefault();
            if(hero!=null)hero.synergyKind=kind;
        }
    }
    static float CrisisValue(Fighter f,int bit){return bit==1?f.build.teamRally:bit==2?f.build.teamInferno:f.build.teamSalvation;}
}
