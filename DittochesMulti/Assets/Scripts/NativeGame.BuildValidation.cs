#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    void ValidateBuildRules()
    {
        artPack=0;round=12;
        Require(DigimonBuildCatalog.Data.traits.Length==11,"eleven new traits");
        foreach(var unit in Roster)Require(DigimonBuildCatalog.ForUnit(unit.id).Count()==2,"two tags "+unit.id);
        foreach(var trait in DigimonBuildCatalog.Data.traits)
        {
            for(int count=0;count<=trait.members.Length;count++)
            {
                Array.Clear(board,0,board.Length);Array.Clear(bench,0,bench.Length);
                for(int i=0;i<count;i++)board[i]=new Unit(RosterById[trait.members[i]]);
                bench[0]=new Unit(RosterById[trait.members[0]]){star=3};
                int expected=trait.tiers.Count(t=>count>=t.count);
                Require(TraitCount(trait.category,trait.name)==count,"bench excluded "+trait.name);
                Require(TraitLevel(trait.category,trait.name)==expected,"threshold "+trait.name+":"+count);
                if(count>0){board[20]=new Unit(RosterById[trait.members[0]]){star=3};Require(TraitCount(trait.category,trait.name)==count,"duplicate excluded");}
            }
        }
        for(int a=0;a<4;a++)for(int b=0;b<4;b++)
        {
            int result=DigimonBuildCatalog.Combine(a,b);
            Require(result==ItemRecipes[a,b]&&result==DigimonBuildCatalog.Combine(b,a),"recipe parity "+a+":"+b);
            inventory.Clear();inventory.Add(a);inventory.Add(b);selectedItem=-1;
            SelectInventoryItem(0);SelectInventoryItem(1);
            Require(inventory.SequenceEqual(new[]{result}),"inventory combine consumes exactly two");
            Unit equipped=new Unit(Roster[0]);equipped.items.Add(a);equipped.items.Add(4);
            inventory.Clear();inventory.Add(b);selectedItem=0;Equip(equipped);
            Require(equipped.items.SequenceEqual(new[]{result,4})&&inventory.Count==0,"auto combine on full equipment slots");
        }
        Unit original=LoadUnit(new UnitSave{id="agumon",star=2,items=new[]{12,13}});
        Require(original.items.SequenceEqual(new[]{12,13})&&!ItemNames[12].Contains("캡슐")&&!ItemNames[13].Contains("캡슐"),"legacy capsule ids map to equipment");
        inventory.Clear();inventory.Add(0);selectedItem=0;Equip(original);
        Require(inventory.Count==1&&original.items.Count==2,"full slots reject without consuming");
        inventory.Clear();inventory.Add(14);selectedItem=0;Equip(original);
        Require(original.items.Count==0&&inventory.SequenceEqual(new[]{12,13}),"remover returns equipment");
        Array.Clear(board,0,board.Length);board[0]=new Unit(RosterById["agumon"]);board[1]=new Unit(RosterById["koromon"]);
        fighters.Clear();var attacker=CreateFighter(board[0],false,new Vector2(3,4));var target=CreateFighter(new Unit(RosterById["gabumon"]),true,new Vector2(3,3));
        fighters.Add(attacker);fighters.Add(CreateFighter(board[1],false,new Vector2(2,4)));fighters.Add(target);InitializeBuildBonuses();
        Require(Mathf.Abs(attacker.build.attack-.22f)<.0001f&&target.build.attack==0,"side independent courage and fighter effects");
        attacker.build=new DigimonBuildCatalog.Bonus{lifesteal=.5f};attacker.hp=attacker.maxHp*.5f;attacker.attacks=1;
        float before=attacker.hp;target.hp=20;target.shield=100;target.build=new DigimonBuildCatalog.Bonus();
        DealDamage(attacker,target,100);Require(attacker.hp==before,"no lifesteal from shields");
        DealDamage(attacker,target,1000);Require(Mathf.Abs(attacker.hp-before-10)<.001f,"lifesteal uses actual health, excludes overkill");
        Require(target.dead,"damage kills");Heal(attacker,target,99999);Shield(attacker,target,99999);Require(target.hp==0&&target.shield==0,"no accidental resurrection");
        target.dead=false;target.maxHp=target.hp=1000;target.shield=0;target.build=new DigimonBuildCatalog.Bonus{lowShield=.25f};
        DealDamage(attacker,target,700);Require(target.hp==300&&target.shield==250,"low health shield triggers");
        DealDamage(attacker,target,260);Require(target.hp==290&&target.shield==0,"shield triggers only once");
        attacker.build=new DigimonBuildCatalog.Bonus{thirdStun=.4f,castHeal=.1f,castShield=.12f,healPower=.2f};attacker.attacks=2;
        target.stun=0;DealDamage(attacker,target,1);Require(target.stun==0,"second basic no stun");
        attacker.attacks=3;DealDamage(attacker,target,1);Require(target.stun==.4f,"third basic stuns");
        target.stun=0;applyingSkillDamage=true;DealDamage(attacker,target,1);applyingSkillDamage=false;Require(target.stun==0,"skill does not proc basic equipment");
        Fighter ally=fighters[1];ally.hp=1;float allyBefore=ally.hp;OnBuildCast(attacker);
        Require(Mathf.Abs(ally.hp-allyBefore-ally.maxHp*.12f)<.01f,"cast heals weakest ally with amplification");
        Require(Mathf.Abs(attacker.shield-attacker.maxHp*.12f)<.01f,"cast shields caster");
        attacker.build=new DigimonBuildCatalog.Bonus{regen=.01f,healPower=.2f,manaRegen=3};attacker.hp=attacker.maxHp*.5f;attacker.mana=0;before=attacker.hp;
        TickBuild(attacker,1);Require(Mathf.Abs(attacker.hp-before-attacker.maxHp*.012f)<.01f&&attacker.mana==3,"regeneration and mana tick");
        inventory.Clear();inventory.Add(0);selectedItem=0;battling=true;Equip(board[0]);battling=false;
        Require(inventory.Count==1&&board[0].items.Count==0,"board equipment frozen in combat");
        Save();Require(Load(),"version two save roundtrip");
        var legacy=JsonUtility.FromJson<SoloSave>(PortablePreviewPrefs.GetString(SaveKey));legacy.version=1;
        PortablePreviewPrefs.SetString(SaveKey,JsonUtility.ToJson(legacy));Require(Load(),"version one save still loads");
        artPack=1;Require(ItemNames[12]==LegacyItemNames[12],"original art equipment preserved");artPack=0;
        fighters.Clear();selectedItem=-1;combatPopups.Clear();placementNoticeUntil=0;
        Debug.Log("BUILD RULES CHECKED: "+validationChecks);
    }
}
#endif
