using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    readonly CombatReportUI.Table tacticalReportRows=new CombatReportUI.Table();
    int tacticalReportMetric;
    Fighter InspectedFighter(Unit unit)
    {return (battling?fighters:lastBattleReport).FirstOrDefault(f=>f.unit==unit);}
    void DrawTacticalReport(float x)
    {
        var rows=tacticalReportRows;rows.Clear();
        foreach(var f in (battling?fighters:lastBattleReport))
        {
            if(f.enemy)continue;var row=rows.Add();
            row.key=f;row.name=UnitName(f.unit.def);row.portrait=FaithfulPortraits.Get(f.unit.def.id,true)??Tex(UnitSprite(f.unit.def));row.dead=f.dead;
            row.damage=f.damageDone;row.basic=f.basicDamageDone;row.skill=f.skillDamageDone;row.taken=f.damageTaken;row.absorbed=f.shieldAbsorbed;
            row.healing=f.healingDone;row.shielding=f.shieldingDone;row.casts=f.casts;
        }
        var clicked=CombatReportUI.Draw(new Rect(x,106,218,581),rows,ref tacticalReportMetric,(battling?"전투 기록 · ":"지난 전투 · ")+lastReportRound);
        if(clicked!=null)inspectedUnit=((Fighter)clicked.key).unit;
        if(HudButton(new Rect(x+13,695,192,34),"테이머 목록"))showCombatReport=false;
    }
    void DrawTacticalUnitDetails(Unit unit,float x)
    {
        var meta=Meta(unit.def.id);var skill=DigimonSkillCatalog.Find(unit.def.id);var stats=InspectStats(unit);var live=InspectedFighter(unit);
        GUI.Label(new Rect(x+14,120,152,30),live!=null&&!battling?"지난 전투 정보":"유닛 정보",hudName);
        if(HudButton(new Rect(x+170,116,33,30),"×"))inspectedUnit=null;
        CharacterCardArt.Portrait(new Rect(x+14,155,190,112),unit.def.id,unit.def.cost,true);
        GUI.Label(new Rect(x+14,274,190,32),UnitName(unit.def)+"  "+new string('★',unit.star),hudWrap);
        GUI.Label(new Rect(x+14,310,190,43),DigimonVisualScale.StageName(unit.def.id)+" · "+BuildTags(unit.def.id)+"\n"+meta.attr+" · "+unit.def.cost+" G",hudWrap);
        if(live!=null)
        {
            string status=live.dead?"전투 불능":!battling?"전투 종료":live.stun>0?"기절 "+live.stun.ToString("0.0")+"초":live.skillCast!=null?"스킬 시전":live.mana>=live.maxMana?"스킬 준비":"전투 중";
            CombatReportUI.Vitals(new Rect(x+14,358,190,89),live.hp,live.maxHp,live.mana,live.maxMana,live.shield,status);
        }
        else GUI.Label(new Rect(x+14,358,190,77),"최대 체력 "+stats.health.ToString("0")+"\n시작 마나 "+stats.startMana.ToString("0")+" / "+stats.maxMana.ToString("0")+"\n"+StatContext(unit),hudWrap);
        GUI.Label(new Rect(x+14,454,190,79),"공격력 "+stats.attack.ToString("0.#")+" · 주문력 "+stats.abilityPower.ToString("0.#")+"\n방어 "+stats.armor.ToString("0")+" · 마저 "+stats.magicResist.ToString("0")+"\n공속 "+stats.speed.ToString("0.00")+" · 사거리 "+stats.range+"칸\n"+skill.ScalingRole,hudWrap);
        var statusBonus=live!=null?live.build:DigimonBuildCatalog.Resolve(unit.def.id,board.Contains(unit)?board.Where(u=>u!=null).Select(u=>u.def.id):Enumerable.Empty<string>(),unit.items);
        CombatStatusUI.Draw(new Rect(x+14,541,190,84),CombatStatusUI.Describe(statusBonus,live!=null?live.combatAge:0,live!=null?live.attacks:0,live!=null&&live.lowShieldUsed,live!=null,live!=null&&(live.dead||!battling)));
        if(DigimonSkillUI.DrawIcon(new Rect(x+14,632,50,50),skill))OpenSkillDetails(unit);
        GUI.Label(new Rect(x+84,632,120,48),skill.name+"\n우클릭 → 정보",new GUIStyle(hudWrap){fontSize=12});
        GUI.Label(new Rect(x+14,685,190,23),"계산 "+skill.Damage(unit.star,stats.attack,stats.abilityPower).ToString("0.#")+" "+skill.DamageLabel+" 피해",hudWrap);
        for(int i=0;i<unit.items.Count&&i<2;i++)
        {int item=unit.items[i];if(DigimonEquipmentArt.Button(new Rect(x+14+i*95,709,32,32),item))OpenEquipmentGuide(item);GUI.Label(new Rect(x+60+i*95,709,44,33),"상세",hudSmall);}
        if((battling||lastBattleReport.Count>0)&&HudButton(new Rect(x+14,745,190,20),"전투 기록 보기")){inspectedUnit=null;showCombatReport=true;}
    }
}
