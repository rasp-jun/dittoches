using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    int tacticalReportMetric;
    Fighter InspectedFighter(Unit unit)
    {return (battling?fighters:lastBattleReport).FirstOrDefault(f=>f.unit==unit);}
    void DrawTacticalReport(float x)
    {
        var rows=(battling?fighters:lastBattleReport).Where(f=>!f.enemy).Select(f=>new CombatReportUI.Row{
            key=f,name=UnitName(f.unit.def),portrait=Tex(UnitSprite(f.unit.def)),dead=f.dead,
            damage=f.damageDone,basic=f.basicDamageDone,skill=f.skillDamageDone,taken=f.damageTaken,absorbed=f.shieldAbsorbed,
            healing=f.healingDone,shielding=f.shieldingDone,casts=f.casts}).ToArray();
        var clicked=CombatReportUI.Draw(new Rect(x,106,218,581),rows,ref tacticalReportMetric,(battling?"전투 기록 · ":"지난 전투 · ")+lastReportRound);
        if(clicked!=null)inspectedUnit=((Fighter)clicked.key).unit;
        if(HudButton(new Rect(x+13,695,192,34),"테이머 목록"))showCombatReport=false;
    }
    void DrawTacticalUnitDetails(Unit unit,float x)
    {
        var meta=Meta(unit.def.id);var skill=DigimonSkillCatalog.Find(unit.def.id);var stats=InspectStats(unit);var live=InspectedFighter(unit);
        GUI.Label(new Rect(x+14,120,152,30),live!=null&&!battling?"지난 전투 정보":"유닛 정보",hudName);
        if(HudButton(new Rect(x+170,116,33,30),"×"))inspectedUnit=null;
        Portrait(new Rect(x+45,158,128,128),UnitSprite(unit.def));
        GUI.Label(new Rect(x+14,295,190,38),UnitName(unit.def)+"  "+new string('★',unit.star),hudWrap);
        GUI.Label(new Rect(x+14,338,190,39),BuildTags(unit.def.id)+"\n"+meta.attr+" · "+unit.def.cost+" G",hudWrap);
        if(live!=null)
        {
            string status=live.dead?"전투 불능":!battling?"전투 종료":live.stun>0?"기절 "+live.stun.ToString("0.0")+"초":live.skillCast!=null?"스킬 시전":live.mana>=live.maxMana?"스킬 준비":"전투 중";
            CombatReportUI.Vitals(new Rect(x+14,382,190,89),live.hp,live.maxHp,live.mana,live.maxMana,live.shield,status);
        }
        else GUI.Label(new Rect(x+14,382,190,77),"최대 체력 "+stats.health.ToString("0")+"\n시작 마나 "+stats.startMana.ToString("0")+" / "+stats.maxMana.ToString("0")+"\n"+StatContext(unit),hudWrap);
        GUI.Label(new Rect(x+14,486,190,84),"공격력 "+stats.attack.ToString("0.#")+" · 주문력 "+stats.abilityPower.ToString("0.#")+"\n방어 "+stats.armor.ToString("0")+" · 마저 "+stats.magicResist.ToString("0")+"\n공속 "+stats.speed.ToString("0.00")+" · 사거리 "+stats.range+"칸\n"+skill.ScalingRole,hudWrap);
        if(DigimonSkillUI.DrawIcon(new Rect(x+14,585,60,60),skill))OpenSkillDetails(unit);
        GUI.Label(new Rect(x+84,587,120,48),skill.name,hudWrap);
        GUI.Label(new Rect(x+14,657,190,44),"우클릭 → 계수·스킬 정보\n계산 "+skill.Damage(unit.star,stats.attack,stats.abilityPower).ToString("0.#")+" "+skill.DamageLabel+" 피해",hudWrap);
        for(int i=0;i<unit.items.Count&&i<2;i++)
        {int item=unit.items[i];GUI.Box(new Rect(x+14+i*95,710,88,33),new GUIContent(ItemIcons[item],ItemNames[item]+"\n"+ItemDescriptions[item]),card);}
        if((battling||lastBattleReport.Count>0)&&HudButton(new Rect(x+14,745,190,20),"전투 기록 보기")){inspectedUnit=null;showCombatReport=true;}
    }
}
