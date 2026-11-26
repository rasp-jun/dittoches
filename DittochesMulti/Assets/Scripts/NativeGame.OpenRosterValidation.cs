#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    static readonly string[] OpenRosterTripleIds={"agumon","greymon","monochromon","metalgreymon","metalgarurumon","plotmon","tailmon","skullgreymon","machinedramon"};
    void ValidateOpenRosterRules()
    {
        int before=validationChecks;artPack=0;battling=trialsMode=false;
        string[] added={"kuwagamon","shellmon","devimon","monochromon","ogremon","meramon","leomon","etemon","andromon","skullgreymon","piedmon","machinedramon"};
        foreach(string id in added)
        {
            var d=RosterById[id];
            Require(d.cost==(int)DigimonVisualScale.Stage(id),"evolution stage determines cost "+id);
            Require(DigimonBuildCatalog.ForUnit(id).Count()==(OpenRosterTripleIds.Contains(id)?3:2),"complete native traits "+id);
            Array.Clear(board,0,board.Length);Array.Clear(bench,0,bench.Length);Array.Clear(shop,0,shop.Length);inventory.Clear();fighters.Clear();
            gold=100;level=9;RebuildPool();int supply=pool[id];
            for(int copy=0;copy<9;copy++)
            {
                shop[0]=d;pool[id]--;int costBefore=gold;
                Require(Buy(0)&&gold==costBefore-d.cost&&shop[0]==null,"direct purchase charged once "+id+":"+copy);
                Require(!Buy(0)&&gold==costBefore-d.cost,"empty slot rejects repeated purchase "+id);
                if(copy==0){var u=bench.First(b=>b!=null);u.items.AddRange(new[]{0,3,8});}
            }
            var merged=bench.Single(u=>u!=null);
            Require(merged.def.id==id&&merged.star==3&&merged.items.SequenceEqual(new[]{0,3,8}),"nine copies retain identity and all three equipment slots "+id);
            Require(pool[id]==supply-9,"nine copies deducted from matching price pool "+id);
            Save();Require(Load()&&bench.Single(u=>u!=null).def.id==id&&bench.Single(u=>u!=null).star==3,"save reload preserves promoted enemy champion "+id);
            Require(pool[id]==supply-9,"save rebuild preserves new champion supply "+id);
            Array.Clear(board,0,board.Length);Array.Clear(bench,0,bench.Length);inventory.Clear();
            board[0]=new Unit(d);board[0].items.Add(0);board[1]=new Unit(d);board[1].items.Add(3);
            for(int i=0;i<bench.Length;i++)bench[i]=new Unit(RosterById["koromon"]);
            gold=10;shop[0]=d;Require(Buy(0)&&board[0].star==2&&board[1]==null&&board[0].items.SequenceEqual(new[]{0,3}),"full bench supports same-species merge "+id);
            for(int star=1;star<=3;star++)
            {
                fighters.Clear();skillCasts.Clear();
                var caster=CreateFighter(new Unit(d){star=star},false,new Vector2(3,4));
                var target=CreateFighter(new Unit(RosterById["koromon"]),true,new Vector2(3,3));
                fighters.Add(caster);fighters.Add(target);InitializeBuildBonuses();
                target.maxHp=target.hp=100000;target.shield=0;target.build=new DigimonBuildCatalog.Bonus();
                var skill=DigimonSkillCatalog.Find(id);float expected=skill.Damage(star,AttackDamage(caster),100);
                Require(StartDigimonSkill(caster,target),"recruited enemy skill starts as allied caster "+id);
                UpdateDigimonSkills(skill.windup+.001f);Require(target.hp==100000,"allied skill waits for projectile travel "+id);
                UpdateDigimonSkills(skill.Duration);
                Require(Mathf.Abs(100000-target.hp-expected)<.04f&&Mathf.Abs(caster.skillDamageDone-expected)<.04f,"allied skill impact matches stats and report "+id+":"+star);
            }
        }
        foreach(var trait in DigimonBuildCatalog.Data.traits)
        {
            Require(trait.members.Distinct().Count()==trait.members.Length&&trait.members.All(RosterById.ContainsKey),"trait has unique recruitable members "+trait.id);
            for(int count=0;count<=trait.members.Length;count++)Require(trait.Level(count)==trait.tiers.Count(t=>count>=t.count),"mixed family threshold "+trait.id+":"+count);
        }
        Array.Clear(board,0,board.Length);Array.Clear(bench,0,bench.Length);Array.Clear(shop,0,shop.Length);fighters.Clear();skillCasts.Clear();combatPopups.Clear();inventory.Clear();
        Debug.Log("OPEN ROSTER RULES COMPLETE: "+(validationChecks-before)+" checks / 12 champions / direct purchase, 3-star merge, 3-slot gear, save, allied skill, 13 trait thresholds");
    }
}
#endif
