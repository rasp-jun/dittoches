#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    void ValidateReportRules()
    {
        artPack=0;battling=false;fighters.Clear();lastBattleReport.Clear();
        var source=CreateFighter(new Unit(RosterById["agumon"]),false,new Vector2(3,4));
        var target=CreateFighter(new Unit(RosterById["koromon"]),true,new Vector2(3,3));
        source.build=new DigimonBuildCatalog.Bonus();target.build=new DigimonBuildCatalog.Bonus{armor=100};
        target.hp=target.maxHp=100;target.shield=40;
        DealDamage(source,target,120);
        Require(source.damageDone==20&&source.basicDamageDone==20&&source.skillDamageDone==0,"basic report counts post-resistance health damage");
        Require(target.damageTaken==20&&target.shieldAbsorbed==40,"received report separates health and shield");
        applyingSkillDamage=true;try{DealDamage(source,target,500,"magic");}finally{applyingSkillDamage=false;}
        Require(source.damageDone==100&&source.basicDamageDone==20&&source.skillDamageDone==80,"skill report excludes overkill");
        DealDamage(source,target,500);Require(source.damageDone==100,"dead target adds no report damage");
        source.maxHp=100;source.hp=90;source.shield=0;Heal(source,source,100);Heal(source,source,100);
        Shield(source,source,100);Shield(source,source,100);
        Require(source.healingDone==10&&source.shieldingDone==50,"recovery report excludes capped overflow");
        source.unit.items.Add(3);var snapshot=source.Snapshot();lastBattleReport.Add(snapshot);
        float recorded=InspectStats(snapshot.unit).abilityPower;
        source.unit.items.Clear();source.unit.star=3;source.damageDone=999;
        Require(snapshot.unit.star==1&&snapshot.unit.items.SequenceEqual(new[]{3})&&snapshot.damageDone==100,"completed report freezes unit and counters");
        Require(InspectedFighter(snapshot.unit)==snapshot&&InspectStats(snapshot.unit).abilityPower==recorded,"historical selection resolves historical fighter");
        var row=new CombatReportUI.Row{damage=100,basic=20,skill=80,taken=20,absorbed=40,healing=10,shielding=50};
        Require(row.Value(0)==100&&row.Value(1)==60&&row.Value(2)==10&&row.Value(3)==50,"report tabs use explicit accounting");
        lastBattleReport.Clear();combatPopups.Clear();battleTraces.Clear();
    }
    void ValidateTacticalRules()
    {
        artPack=0;battling=false;fighters.Clear();
        var source=CreateFighter(new Unit(RosterById["agumon"]),false,new Vector2(3,4));
        var near=CreateFighter(new Unit(RosterById["koromon"]),true,new Vector2(3,3));
        var far=CreateFighter(new Unit(RosterById["koromon"]),true,new Vector2(3,0));far.hp=1;
        fighters.Add(source);fighters.Add(far);fighters.Add(near);source.target=far;
        Require(SelectTarget(source)==near,"reachable enemy replaces distant weak target");
        far.pos=new Vector2(3,3.5f);Require(SelectTarget(source)==near,"reachable target stays locked");
        near.pos=new Vector2(3,0);far.pos=new Vector2(3,2);
        Require(SelectTarget(source)==near,"stable chase when neither target is reachable");
        near.dead=true;Require(SelectTarget(source)==far,"dead target replaced");
        far.enemy=false;Require(SelectTarget(source)==null,"allies excluded");fighters.Clear();
        foreach(string id in new[]{"agumon","weregarurumon","wargreymon"})
        {
            var unit=new Unit(RosterById[id]){star=2};unit.items.Add(0);unit.items.Add(3);
            Array.Clear(board,0,board.Length);board[0]=unit;board[1]=new Unit(RosterById["koromon"]);
            int[] old=unit.items.ToArray();var team=board.Where(u=>u!=null).Select(u=>u.def.id);
            var preview=DigimonEquipmentPreview.Create(id,unit.star,team,unit.items,1);
            Require(preview.change.allowed&&unit.items.SequenceEqual(old),"preview never mutates gear "+id);
            inventory.Clear();inventory.Add(1);selectedItem=0;Equip(unit);var actual=InspectStats(unit);
            Require(unit.items.SequenceEqual(preview.change.items),"craft preview equals applied equipment "+id);
            Require(Mathf.Abs(actual.attack-preview.after.attack)<.001f&&actual.abilityPower==preview.after.abilityPower&&actual.health==preview.after.health,"preview equals actual gear plus synergy stats "+id);
            var skill=DigimonSkillCatalog.Find(id);
            Require(Mathf.Abs(skill.Damage(unit.star,actual.attack,actual.abilityPower)-preview.damageAfter)<.001f,"skill preview matches equipped damage "+id);
            Require(!DigimonEquipmentPreview.Create(id,2,team,unit.items,8).change.allowed,"full slots reject before consumption");
            var remove=DigimonEquipmentPreview.Create(id,2,team,unit.items,14);
            Require(remove.change.allowed&&remove.change.removed&&remove.change.items.Length==0,"remover preview clears both slots");
        }
        Require(!DigimonBuildCatalog.PreviewEquipment(new int[0],14).allowed,"empty remover preview rejects");
        Array.Clear(board,0,board.Length);inventory.Clear();selectedItem=-1;placementNoticeUntil=0;
    }
    void ValidateScalingRules()
    {
        artPack=0;battling=false;Array.Clear(board,0,board.Length);Array.Clear(bench,0,bench.Length);fighters.Clear();skillCasts.Clear();
        foreach(var d in Roster)
        {
            var s=DigimonSkillCatalog.Find(d.id);
            Require(s.adRatio.Length==3&&s.apRatio.Length==3,"three explicit coefficient tiers "+d.id);
            Require(Meta(d.id).range==s.attackRange,"native range from catalog "+d.id);
            Require(DigimonSkillUI.Icon(s).width==96,"skill icon generated "+d.id);
            var unit=new Unit(d);board[0]=unit;var before=InspectStats(unit);
            unit.items.Add(3);var after=InspectStats(unit);
            Require(after.abilityPower==before.abilityPower+12&&after.attack==before.attack,"AP component does not add AD "+d.id);
            if(s.apRatio[0]>0)Require(s.Damage(1,after.attack,after.abilityPower)>s.Damage(1,before.attack,before.abilityPower),"AP changes this skill "+d.id);
            else Require(s.Damage(1,after.attack,after.abilityPower)==s.Damage(1,before.attack,before.abilityPower),"AD skill ignores AP "+d.id);
        }
        foreach(string id in new[]{"agumon","weregarurumon","wargreymon"})
        {
            Array.Clear(board,0,board.Length);var unit=new Unit(RosterById[id]);unit.items.Add(id=="weregarurumon"?0:3);board[0]=unit;
            var source=CreateFighter(unit,false,new Vector2(3,4));var target=CreateFighter(new Unit(RosterById["koromon"]),true,new Vector2(3,3));
            fighters.Clear();fighters.Add(source);fighters.Add(target);InitializeBuildBonuses();
            target.build=new DigimonBuildCatalog.Bonus{armor=100,magicResist=50};target.maxHp=target.hp=10000;target.shield=0;
            var stats=InspectStats(unit);var s=DigimonSkillCatalog.Find(id);float predicted=s.Damage(1,stats.attack,stats.abilityPower);
            Require(StartDigimonSkill(source,target),"native skill starts "+id);
            Require(Mathf.Abs(source.skillCast.power-predicted)<.001f,"tooltip and cast use same calculation "+id);
            source.build.abilityPower=9999;source.build.attack=99;UpdateDigimonSkills(s.Duration+.01f);
            float actual=10000-target.hp,expected=predicted/(s.damageType=="physical"?2:1.5f);
            Require(Mathf.Abs(actual-expected)<.01f,"actual frozen typed damage "+id);
        }
        Require(DigimonCombatMath.InAttackRange(3,2,2,1,1)&&!DigimonCombatMath.InAttackRange(3,2,1,1,1),"hex range distinguishes adjacent and distant cells");
        fighters.Clear();skillCasts.Clear();combatPopups.Clear();Array.Clear(board,0,board.Length);
    }
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
