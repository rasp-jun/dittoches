using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    static DigimonBuildCatalog.Member BuildMember(Unit u){return u==null?null:new DigimonBuildCatalog.Member(u.def.id,u.items);}
    IEnumerable<DigimonBuildCatalog.Member> BoardBuildMembers(){return board.Where(u=>u!=null).Select(BuildMember);}
    IEnumerable<DigimonBuildCatalog.Member> CombatBuildMembers(bool enemy){return fighters.Where(f=>f.enemy==enemy).Select(f=>BuildMember(f.unit));}
    int FormationLimit { get {return artPack==0?Mathf.Min(board.Length,level+inventory.Concat(board.Concat(bench).Where(u=>u!=null).SelectMany(u=>u.items)).Sum(i=>DigimonBuildCatalog.Data.items[i].teamSize)):level;} }
    void AssignMergedEquipment(Unit unit,IEnumerable<int> equipment)
    {
        unit.items.Clear();var granted=new HashSet<string>();
        foreach(int item in equipment)
        {
            string trait=artPack==0?DigimonBuildCatalog.Data.items[item].grantsTrait:null;
            if(unit.items.Count>=2||!string.IsNullOrEmpty(trait)&&granted.Contains(trait))inventory.Add(item);
            else{unit.items.Add(item);if(!string.IsNullOrEmpty(trait))granted.Add(trait);}
        }
    }
    string[] buildItemNames,buildItemIcons,buildItemDescriptions;
    string[] ItemNames { get { return artPack==0?(buildItemNames??(buildItemNames=DigimonBuildCatalog.Data.items.Select(i=>i.name).ToArray())):LegacyItemNames; } }
    string[] ItemIcons { get { return artPack==0?(buildItemIcons??(buildItemIcons=DigimonBuildCatalog.Data.items.Select(i=>i.icon).ToArray())):LegacyItemIcons; } }
    string[] ItemDescriptions { get { return artPack==0?(buildItemDescriptions??(buildItemDescriptions=DigimonBuildCatalog.Data.items.Select(i=>i.description+(i.id<4?" · 재료 2개로 합성":"")).ToArray())):LegacyItemDescriptions; } }
    Rect TraitGuideRect { get { return new Rect(270,145,artPack==0?610:475,artPack==0?360:155); } }
    Rect RecipeGuideRect { get { return artPack==0?new Rect(360,150,920,654):new Rect(270,470,470,recipeFocus<=3?270:150); } }
    string BuildTags(string id,IEnumerable<int> gear=null){return string.Join(" · ",DigimonBuildCatalog.ForUnit(id,gear).Select(t=>t.name).ToArray());}
    List<TraitEntry> BuildTraits()
    {
        var ids=BoardBuildMembers().ToArray();
        return DigimonBuildCatalog.Data.traits.Select(t=>new TraitEntry(t.category,t.name,DigimonBuildCatalog.Count(t,ids)))
            .Where(t=>t.count>0).OrderByDescending(t=>DigimonBuildCatalog.Find(t.key).Level(t.count)).ThenByDescending(t=>t.count).ThenBy(t=>t.name).ToList();
    }
    string BuildTraitText(string key,int tier)
    {
        var t=DigimonBuildCatalog.Find(key);if(t==null)return "";
        return tier==0?t.tiers[0].count+"종 배치 시 활성 · "+t.tiers[0].text:t.tiers[Mathf.Clamp(tier-1,0,t.tiers.Length-1)].text;
    }
    void DrawBuildTraitGuide()
    {
        string focus=traitFocus.key;Matrix4x4 previous=GUI.matrix;GUI.matrix=previous*Matrix4x4.Scale(new Vector3(1.2f,1.2f,1));
        bool open=DigimonTraitUI.DrawGuide(new Rect(0,0,1600,900),ref focus,board.Where(u=>u!=null).Select(u=>u.def.id).ToArray(),bench.Where(u=>u!=null).Select(u=>u.def.id).ToArray(),BoardBuildMembers().ToArray());
        GUI.matrix=previous;
        var trait=DigimonBuildCatalog.Find(focus);
        traitFocus=open?new TraitEntry(trait.category,trait.name,DigimonBuildCatalog.Count(trait,BoardBuildMembers())):null;
    }
    void OpenTraitGuide(string id)
    {
        var t=DigimonBuildCatalog.Find(id)??DigimonBuildCatalog.Data.traits[0];
        traitFocus=new TraitEntry(t.category,t.name,TraitCount(t.category,t.name));traitGuideUntil=float.PositiveInfinity;
        showRecipeGuide=false;skillDetailUnit=null;showTeamPlan=false;draggingUnit=false;dragSource=-1;arenaPointer.Reset();
    }

    void OpenEquipmentGuide(int id)
    {
        traitFocus=null;recipeFocus=id;showRecipeGuide=true;recipeGuideUntil=float.PositiveInfinity;
        dragSource=-1;draggingUnit=false;arenaPointer.Reset();
    }
    void DrawBuildRecipeGuide()
    {
        DrawRect(new Rect(0,84,1920,996),new Color(0,0,0,.62f));
        if(!DigimonEquipmentUI.DrawGuide(RecipeGuideRect,ref recipeFocus,inventory))showRecipeGuide=false;
    }
    void InitializeBuildBonuses()
    {
        if(artPack!=0)return;
        foreach(bool enemy in new[]{false,true})
        {
            Fighter[] team=fighters.Where(f=>f.enemy==enemy).ToArray();var ids=team.Select(f=>BuildMember(f.unit)).ToArray();
            foreach(Fighter f in team)
            {
                f.build=DigimonBuildCatalog.Resolve(f.unit.def.id,ids,f.unit.items);f.attacks=0;f.lowShieldUsed=false;f.combatAge=0;f.crisisAt=-1;
                // Flat equipment health is already included before star/enemy scaling in CreateFighter.
                f.maxHp*=1+f.build.hp;f.hp=f.maxHp;
                f.mana=Mathf.Min(f.maxMana,f.mana+f.build.startMana);
                Shield(f,f,f.maxHp*f.build.startShield);
            }
        }
    }
    void TickBuild(Fighter f,float dt)
    {
        if(artPack!=0||f.dead)return;
        f.combatAge+=dt;
        f.mana=Mathf.Min(f.maxMana,f.mana+f.build.manaRegen*dt);
        // Tick once a second to keep healing popups readable and independent of frame rate.
        f.regenClock+=dt;
        while(f.regenClock>=1){f.regenClock-=1;if(f.build.regen>0)Heal(f,f,f.maxHp*f.build.regen);}
    }
    void ApplyLiveEquipment(Fighter f,IEnumerable<int> items)
    {
        int[] equipment=items.ToArray();
        f.unit.items.Clear();f.unit.items.AddRange(equipment);
        foreach(var ally in fighters.Where(u=>u.enemy==f.enemy))RefreshLiveBuild(ally);
    }
    void RefreshLiveBuild(Fighter f)
    {
        if(f.dead)return;
        float fraction=f.hp/Mathf.Max(1,f.maxHp);
        float oldSpeed=DigimonCombatMath.AttackSpeed(Meta(f.unit.def.id).speed,f.build.speed,f.build.rampSpeed,f.combatAge);
        f.build=DigimonBuildCatalog.Resolve(f.unit.def.id,CombatBuildMembers(f.enemy),f.unit.items);
        f.maxHp=(Meta(f.unit.def.id).hp+f.unit.items.Sum(ItemHealth))*Mathf.Pow(1.8f,f.unit.star-1)*f.attackScale*(1+f.build.hp);
        f.hp=f.maxHp*fraction;f.shield=Mathf.Min(f.shield,f.maxHp*.5f);
        float speed=DigimonCombatMath.AttackSpeed(Meta(f.unit.def.id).speed,f.build.speed,f.build.rampSpeed,f.combatAge);
        if(f.skillCast==null)f.cooldown*=oldSpeed/speed;
        // Opening mana/shields, crisis triggers and regeneration timers are never reset.
    }
    void SyncCombatEquipment()
    {
        if(artPack!=0||!battling)return;
        foreach(var f in fighters.Where(f=>!f.enemy&&f.sourceSlot>=0))
        {
            var owner=board[f.sourceSlot];
            var items=owner!=null&&owner.def.id==f.unit.def.id?owner.items.ToArray():new int[0];
            f.unit.items.Clear();f.unit.items.AddRange(items);
        }
        foreach(var f in fighters.Where(f=>!f.enemy))RefreshLiveBuild(f);
    }
    void OnBuildCast(Fighter caster)
    {
        if(artPack!=0||caster.dead)return;
        if(caster.build.castHeal>0)
        {
            Fighter target=fighters.Where(f=>!f.dead&&f.enemy==caster.enemy).OrderBy(f=>f.hp/f.maxHp).FirstOrDefault();
            if(target!=null)Heal(caster,target,target.maxHp*caster.build.castHeal);
        }
        if(caster.build.castShield>0)Shield(caster,caster,caster.maxHp*caster.build.castShield);
    }
    void OnBuildDamaged(Fighter f)
    {
        if(artPack!=0||f.dead||f.lowShieldUsed||(f.build.lowShield<=0&&f.build.lowHeal<=0)||f.hp>f.maxHp*(.35f+1e-7f))return;
        f.lowShieldUsed=true;f.crisisAt=f.combatAge;Shield(f,f,f.maxHp*f.build.lowShield);Heal(f,f,f.maxHp*f.build.lowHeal);
    }
}
