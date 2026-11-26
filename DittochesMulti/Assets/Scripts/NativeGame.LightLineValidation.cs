#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    void ValidateLightLineRules()
    {
        int start=validationChecks;artPack=0;
        Require(Roster.Length==47&&DigimonSkillCatalog.All.Count()==47,"47 player definitions and 47 skills include the light line");
        for(int cost=1;cost<=5;cost++)Require(ActiveRoster.Count(u=>u.cost==cost)==new[]{0,7,7,14,10,9}[cost],"evolution-priced units at cost "+cost);
        artPack=1;Require(ActiveRoster.Count()==30&&!ActiveRoster.Any(u=>LightLineIds.Contains(u.id)),"original roster is preserved");artPack=0;
        foreach(var trait in DigimonBuildCatalog.Data.traits)Require(trait.members.Length>=trait.tiers.Last().count&&trait.tiers.Select(t=>t.count).Distinct().Count()==trait.tiers.Length,"balanced trait membership "+trait.id);
        var light=DigimonBuildCatalog.Find("light");
        for(int count=1;count<=9;count++)
        {
            var ids=light.members.Take(count).ToArray();var bonus=DigimonBuildCatalog.Resolve(ids[0],ids,new int[0]);
            float expected=count<3?0:count<6?.035f:count<9?.07f:.12f;
            Require(Mathf.Abs(bonus.castAllyShield-expected)<.0001f,"light threshold "+count);
            Require(DigimonBuildCatalog.Count(light,ids.Concat(ids))==count,"light duplicate species excluded");
        }
        foreach(string id in LightLineIds)
        {
            var loaded=LoadUnit(new UnitSave{id=id,star=2,items=new[]{8,12}});
            Require(loaded!=null&&loaded.def.id==id&&loaded.star==2&&loaded.items.SequenceEqual(new[]{8,12}),"new saved unit loads with gear "+id);
            for(int star=1;star<=3;star++)
            {
                fighters.Clear();skillCasts.Clear();
                var caster=CreateFighter(new Unit(RosterById[id]){star=star},false,new Vector2(3,4));
                var target=CreateFighter(new Unit(RosterById["koromon"]),true,new Vector2(3,3));
                fighters.Add(caster);fighters.Add(target);InitializeBuildBonuses();
                target.maxHp=target.hp=100000;target.build=new DigimonBuildCatalog.Bonus();target.shield=0;
                var skill=DigimonSkillCatalog.Find(id);float expected=skill.Damage(star,AttackDamage(caster),100);
                Require(StartDigimonSkill(caster,target),"new technique starts "+id);
                UpdateDigimonSkills(skill.windup+.001f);Require(target.hp==100000,"new technique waits for travel "+id);
                UpdateDigimonSkills(skill.Duration);Require(Mathf.Abs((100000-target.hp)-expected)<.02f,"new technique actual damage at star "+star+" "+id);
                Require(Mathf.Abs(caster.skillDamageDone-expected)<.02f&&caster.basicDamageDone==0,"new technique report totals "+id);
            }
        }
        fighters.Clear();skillCasts.Clear();
        var source=CreateFighter(new Unit(RosterById["ophanimon"]),false,new Vector2(2,4));
        var ally=CreateFighter(new Unit(RosterById["plotmon"]),false,new Vector2(3,4));
        var enemy=CreateFighter(new Unit(RosterById["agumon"]),true,new Vector2(3,3));
        fighters.Add(source);fighters.Add(ally);fighters.Add(enemy);InitializeBuildBonuses();
        source.build=new DigimonBuildCatalog.Bonus{castAllyShield=.11f,healPower=5};source.maxHp=source.hp=1000;source.shield=0;
        ally.maxHp=2000;ally.hp=600;ally.shield=990;enemy.hp=1;enemy.shield=0;
        OnBuildCast(source);Require(ally.shield==1000&&source.shield==0&&enemy.shield==0&&source.shieldingDone==10,"light chooses live ally fraction and caps exact credit");
        OnBuildCast(source);Require(source.shieldingDone==10,"light capped shield grants no duplicate credit");
        ally.dead=true;ally.hp=0;ally.shield=0;OnBuildCast(source);
        Require(source.shield==110&&ally.shield==0,"light can protect self and cannot revive");
        source.dead=true;OnBuildCast(source);Require(source.shield==110,"dead caster grants no light shield");
        fighters.Clear();skillCasts.Clear();combatPopups.Clear();Array.Clear(board,0,board.Length);
        Debug.Log("LIGHT LINE RULES COMPLETE: "+(validationChecks-start)+" checks / 47 players / 47 techniques / light thresholds, impact, shields and save compatibility");
    }
}
#endif
