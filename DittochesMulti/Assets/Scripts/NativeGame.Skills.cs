using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    private sealed class SkillCast
    {
        public Fighter caster,target;
        public DigimonSkillCatalog.Entry skill;
        public Vector2 origin,aim;
        public float age,power;
        public int hits;
        public bool released,cancelled;
    }
    private readonly List<SkillCast> skillCasts=new List<SkillCast>();
    private bool applyingSkillDamage;
    private bool StartDigimonSkill(Fighter caster,Fighter target)
    {
        if(artPack!=0)return false;
        var skill=DigimonSkillCatalog.Find(caster.unit.def.id);
        if(skill==null)return false;
        var cast=new SkillCast{caster=caster,target=target,skill=skill,origin=caster.pos,aim=target.pos,
            power=skill.Damage(caster.unit.star,AttackDamage(caster),DigimonCombatMath.AbilityPower(caster.build.abilityPower)*(caster.enemy?caster.attackScale:1f))};
        skillCasts.Add(cast);caster.skillCast=cast;
        caster.mana=0;caster.casts++;caster.cooldown=skill.Duration;
        caster.attackTarget=target.pos;caster.skillFlash=skill.Duration;OnBuildCast(caster);
        return true;
    }
    private void UpdateDigimonSkills(float dt,bool applyDamage=true)
    {
        for(int i=skillCasts.Count-1;i>=0;i--)
        {
            SkillCast cast=skillCasts[i];var skill=cast.skill;
            if(!cast.released&&(cast.caster.dead||cast.caster.stun>0))
            {
                cast.cancelled=true;cast.caster.skillFlash=0;cast.caster.skillCast=null;
                skillCasts.RemoveAt(i);continue;
            }
            cast.age+=dt;
            if(!cast.released)
            {
                if(cast.target.dead)cast.target=FindCombatTarget(cast.caster);
                if(cast.target==null){cast.caster.skillCast=null;cast.caster.skillFlash=0;skillCasts.RemoveAt(i);continue;}
                cast.origin=cast.caster.pos;cast.aim=cast.target.pos;
                cast.caster.attackTarget=cast.aim;
                if(cast.age>=skill.windup)cast.released=true;
            }
            // Homing shots follow only the locked target; line/cone directions stay fixed after release.
            if(cast.hits==0&&cast.target!=null&&!cast.target.dead&&(skill.shape=="single"||skill.shape=="splash"))cast.aim=cast.target.pos;
            int due=SkillGeometry.DueHits(cast.age,skill.windup,skill.travel,skill.interval,skill.shots);
            while(cast.hits<due){if(applyDamage)ResolveDigimonHit(cast);cast.hits++;}
            if(cast.age>=skill.Duration)
            {if(cast.caster.skillCast==cast)cast.caster.skillCast=null;skillCasts.RemoveAt(i);}
        }
    }
    private void ResolveDigimonHit(SkillCast cast)
    {
        var s=cast.skill;
        IEnumerable<Fighter> victims=fighters.Where(f=>!f.dead&&f.enemy!=cast.caster.enemy);
        victims=s.shape=="single"?victims.Where(f=>f==cast.target):victims.Where(f=>SkillGeometry.Contains(s.shape,
            cast.origin.x,cast.origin.y,cast.aim.x,cast.aim.y,f.pos.x,f.pos.y,s.radius,s.reach));
        applyingSkillDamage=true;
        try
        {
            foreach(Fighter victim in victims.OrderBy(f=>Vector2.Distance(f.pos,cast.aim)).Take(s.targets).ToArray())
            {
                DealDamage(cast.caster,victim,cast.power/s.shots,s.damageType);
                if(!victim.dead)victim.stun=Mathf.Max(victim.stun,s.stun);
                if(!victim.dead&&s.visual=="gate")victim.pos=Vector2.MoveTowards(victim.pos,cast.aim,.45f);
            }
        }
        finally{applyingSkillDamage=false;}
    }
    private void DrawDigimonSkills()
    {
        foreach(SkillCast cast in skillCasts)if(!cast.cancelled)
            arena.DrawSkill(cast.skill,TacticalArena.CellWorld(cast.origin.x,cast.origin.y),
                TacticalArena.CellWorld(cast.aim.x,cast.aim.y),cast.age);
    }
}
