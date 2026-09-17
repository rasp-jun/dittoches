using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    string[] buildItemNames,buildItemIcons,buildItemDescriptions;
    string[] ItemNames { get { return artPack==0?(buildItemNames??(buildItemNames=DigimonBuildCatalog.Data.items.Select(i=>i.name).ToArray())):LegacyItemNames; } }
    string[] ItemIcons { get { return artPack==0?(buildItemIcons??(buildItemIcons=DigimonBuildCatalog.Data.items.Select(i=>i.icon).ToArray())):LegacyItemIcons; } }
    string[] ItemDescriptions { get { return artPack==0?(buildItemDescriptions??(buildItemDescriptions=DigimonBuildCatalog.Data.items.Select(i=>i.description+(i.id<4?" · 재료 2개로 합성":"")).ToArray())):LegacyItemDescriptions; } }
    Rect TraitGuideRect { get { return new Rect(270,145,artPack==0?610:475,artPack==0?360:155); } }
    Rect RecipeGuideRect { get { return artPack==0?new Rect(270,410,650,recipeFocus<=3?402:260):new Rect(270,470,470,recipeFocus<=3?270:150); } }
    string BuildTags(string id){return string.Join(" · ",DigimonBuildCatalog.ForUnit(id).Select(t=>t.name).ToArray());}
    List<TraitEntry> BuildTraits()
    {
        string[] ids=board.Where(u=>u!=null).Select(u=>u.def.id).ToArray();
        return DigimonBuildCatalog.Data.traits.Select(t=>new TraitEntry(t.category,t.name,DigimonBuildCatalog.Count(t,ids)))
            .Where(t=>t.count>0).OrderByDescending(t=>TraitTier(t.category,t.count)).ThenByDescending(t=>t.count).ThenBy(t=>t.name).ToList();
    }
    string BuildTraitText(string key,int tier)
    {
        var t=DigimonBuildCatalog.Find(key);if(t==null)return "";
        return tier==0?"2종 배치 시 활성 · "+t.tiers[0].text:t.tiers[Mathf.Clamp(tier-1,0,2)].text;
    }
    void DrawBuildTraitGuide()
    {
        var t=DigimonBuildCatalog.Find(traitFocus.key);if(t==null)return;
        int count=TraitCount(t.category,t.name),tier=t.Level(count);Rect r=TraitGuideRect;
        HudPanel(r);GUI.Label(new Rect(r.x+20,r.y+12,530,30),t.category+" · "+t.name+"  "+count+" / "+t.Target(count),header);
        if(HudButton(new Rect(r.xMax-48,r.y+12,32,28),"×")){traitFocus=null;return;}
        GUI.Label(new Rect(r.x+20,r.y+51,r.width-40,49),t.description,hudWrap);
        for(int i=0;i<3;i++)
        {
            float y=r.y+109+i*49;DrawRect(new Rect(r.x+18,y,r.width-36,44),tier==i+1?new Color(.19f,.17f,.09f):panel);
            GUI.Label(new Rect(r.x+28,y+6,42,30),"("+t.tiers[i].count+")",label);
            GUI.Label(new Rect(r.x+73,y+4,r.width-98,39),t.tiers[i].text,hudWrap);
        }
        GUI.Label(new Rect(r.x+20,r.y+264,r.width-40,56),string.Join(" · ",t.members.Select(id=>RosterById[id].name).ToArray()),hudWrap);
        GUI.Label(new Rect(r.x+20,r.y+328,r.width-40,23),"서로 다른 종류만 계산 · 대기석 제외 · 효과는 전투 시작 시 확정",hudSmall);
    }
    void DrawBuildRecipeGuide()
    {
        if(recipeFocus<0||recipeFocus>=ItemNames.Length)return;
        Rect r=RecipeGuideRect;var item=DigimonBuildCatalog.Data.items[recipeFocus];HudPanel(r);
        GUI.Label(new Rect(r.x+20,r.y+12,r.width-85,30),item.name+" · "+(recipeFocus<4?"기본 장비":recipeFocus<14?"완성 장비":"소모품"),header);
        if(HudButton(new Rect(r.xMax-48,r.y+12,32,28),"×")){showRecipeGuide=false;return;}
        GUI.Label(new Rect(r.x+20,r.y+51,r.width-40,60),item.description,hudWrap);
        if(recipeFocus<4)
        {
            for(int i=0;i<4;i++)
            {
                int result=DigimonBuildCatalog.Combine(recipeFocus,i);var made=DigimonBuildCatalog.Data.items[result];float y=r.y+116+i*61;
                DrawRect(new Rect(r.x+18,y,r.width-36,57),panel);
                GUI.Label(new Rect(r.x+28,y+3,r.width-56,24),"+ "+ItemNames[i]+"  →  "+made.name,label);
                GUI.Label(new Rect(r.x+28,y+28,r.width-56,27),made.description,hudSmall);
            }
            GUI.Label(new Rect(r.x+20,r.yMax-31,r.width-40,25),"보관함에서 재료 2개 클릭 또는 같은 유닛에 장착해 합성",hudSmall);
        }
        else
        {
            string recipe=item.recipe.Length==2?string.Join(" + ",item.recipe.Select(i=>ItemNames[i]).ToArray()):"합성 대상이 아닌 회수 도구";
            GUI.Label(new Rect(r.x+20,r.y+126,r.width-40,48),recipe,hudWrap);
            GUI.Label(new Rect(r.x+20,r.y+183,r.width-40,52),string.IsNullOrEmpty(item.owner)?"1회 사용 후 소모됩니다.":"원작 장비 사용자: "+item.owner+"\n합성식과 전투 수치는 게임용으로 재구성했습니다.",hudWrap);
        }
    }
    void InitializeBuildBonuses()
    {
        if(artPack!=0)return;
        foreach(bool enemy in new[]{false,true})
        {
            Fighter[] team=fighters.Where(f=>f.enemy==enemy).ToArray();string[] ids=team.Select(f=>f.unit.def.id).ToArray();
            foreach(Fighter f in team)
            {
                f.build=DigimonBuildCatalog.Resolve(f.unit.def.id,ids,f.unit.items);f.attacks=0;f.lowShieldUsed=false;
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
        f.mana=Mathf.Min(f.maxMana,f.mana+f.build.manaRegen*dt);
        // Tick once a second to keep healing popups readable and independent of frame rate.
        f.regenClock+=dt;
        while(f.regenClock>=1){f.regenClock-=1;if(f.build.regen>0)Heal(f,f,f.maxHp*f.build.regen);}
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
        if(artPack!=0||f.dead||f.lowShieldUsed||f.build.lowShield<=0||f.hp>f.maxHp*.35f)return;
        f.lowShieldUsed=true;Shield(f,f,f.maxHp*f.build.lowShield);
    }
}
