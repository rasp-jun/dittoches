using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    readonly CombatReportUI.Table tacticalReportRows=new CombatReportUI.Table();
    int tacticalReportMetric;
    Fighter InspectedFighter(Unit unit)
    {return (battling?fighters:lastBattleReport).FirstOrDefault(f=>f.unit==unit);}
    string InspectionOrigin(Unit unit,Fighter live)
    {
        if(trialsMode&&live!=null&&live.enemy)
            return CurrentTrial.boss&&fighters.FirstOrDefault(f=>f.enemy)==live?"보스":"시험 적";
        return RosterById.ContainsKey(unit.def.id)?unit.def.cost+" G":"야생";
    }
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
        if(HudButton(new Rect(x+13,695,192,34),TrialsMode?"시험 정보":"테이머 목록"))showCombatReport=false;
    }
    void DrawTacticalUnitDetails(Unit unit,float x)
    {
        DigimonBuildCatalog.Bonus statusBonus;Fighter live;
        var meta=Meta(unit.def.id);var skill=DigimonSkillCatalog.Find(unit.def.id);var stats=InspectStats(unit,out statusBonus,out live);
        ArenaPresentationUI.SectionHeading(new Rect(x+14,120,152,27),live!=null&&!battling?"전투 기록":"유닛 정보","");
        if(HudButton(new Rect(x+170,116,33,30),"×"))inspectedUnit=null;
        CharacterCardArt.Portrait(new Rect(x+14,155,190,112),unit.def.id,unit.def.cost,true);
        BattleHudTheme.Badge(new Rect(x+23,163,61,24),new string('★',unit.star),BattleHudTheme.Gold,14);
        BattleHudTheme.Badge(new Rect(x+134,163,61,24),InspectionOrigin(unit,live),BattleHudTheme.Gold,13);
        BattleHudTheme.FitLabel(new Rect(x+14,274,190,29),UnitName(unit.def),21);
        BattleHudTheme.Label(new Rect(x+14,308,190,19),DigimonVisualScale.StageName(unit.def.id)+" · "+meta.attr,12,BattleHudTheme.Muted);
        BattleHudTheme.FitLabel(new Rect(x+14,333,190,20),BuildTags(unit.def.id,unit.items),13,BattleHudTheme.Teal,false);
        if(live!=null)
        {
            string status=live.dead?"전투 불능":!battling?"전투 종료":live.stun>0?"기절 "+live.stun.ToString("0.0")+"초":live.skillCast!=null?"스킬 시전":live.mana>=live.maxMana?"스킬 준비":"전투 중";
            CombatReportUI.Vitals(new Rect(x+14,358,190,89),live.hp,live.maxHp,live.mana,live.maxMana,live.shield,status);
        }
        else
        {
            BattleHudTheme.Round(new Rect(x+14,363,190,79),BattleHudTheme.Raised);
            BattleHudTheme.Label(new Rect(x+22,368,113,23),"최대 체력",12,BattleHudTheme.Muted);
            BattleHudTheme.Label(new Rect(x+127,368,68,23),stats.health.ToString("0"),17,BattleHudTheme.Teal,true,TextAnchor.MiddleRight);
            BattleHudTheme.Label(new Rect(x+22,395,89,21),"시작 마나",12,BattleHudTheme.Muted);
            BattleHudTheme.Label(new Rect(x+97,395,98,21),stats.startMana.ToString("0")+" / "+stats.maxMana.ToString("0"),13,null,true,TextAnchor.MiddleRight);
            BattleHudTheme.FitLabel(new Rect(x+22,420,173,17),StatContext(unit,live),11,BattleHudTheme.Muted,false);
            GUI.Label(new Rect(x+14,363,190,79),new GUIContent("",StatContext(unit,live)),GUIStyle.none);
        }
        BattleHudTheme.Round(new Rect(x+14,451,190,81),BattleHudTheme.Raised);
        DrawTacticalStatPair(new Rect(x+17,453,184,22),"공격",stats.attack.ToString("0.#"),"주문",stats.abilityPower.ToString("0.#"));
        DrawTacticalStatPair(new Rect(x+17,475,184,22),"방어",stats.armor.ToString("0"),"마저",stats.magicResist.ToString("0"));
        DrawTacticalStatPair(new Rect(x+17,497,184,22),"공속",stats.speed.ToString("0.00"),"거리",stats.range+"칸");
        BattleHudTheme.Label(new Rect(x+20,517,176,16),"평타 물리 · 스킬 "+skill.DamageLabel,10,BattleHudTheme.Muted);
        GUI.Label(new Rect(x+14,451,190,81),new GUIContent("",StatContext(unit,live)),GUIStyle.none);
        CombatStatusUI.Draw(new Rect(x+14,541,190,84),CombatStatusUI.Describe(statusBonus,live!=null?live.combatAge:0,live!=null?live.attacks:0,live!=null&&live.lowShieldUsed,live!=null,live!=null&&(live.dead||!battling),live==null?null:new CombatStatusUI.State{burnRemaining=live.burnRemaining,shredRemaining=live.shredRemaining,battleAge=live.battleAge,synergyUsed=live.synergyUsed,revived=live.revived}));
        BattleHudTheme.Round(new Rect(x+14,628,190,79),BattleHudTheme.Raised);
        if(DigimonSkillUI.DrawIcon(new Rect(x+14,632,50,50),skill))OpenSkillDetails(unit);
        BattleHudTheme.FitLabel(new Rect(x+74,634,123,25),skill.name,15);
        BattleHudTheme.Label(new Rect(x+74,660,123,21),"기술 상세 보기",11,BattleHudTheme.Muted);
        if(GUI.Button(new Rect(x+71,632,133,50),new GUIContent("",skill.name+" · 기술 상세 보기"),GUIStyle.none))OpenSkillDetails(unit);
        BattleHudTheme.Label(new Rect(x+21,684,178,20),"계산 "+skill.Damage(unit.star,stats.attack,stats.abilityPower).ToString("0.#")+" "+skill.DamageLabel+" 피해",12,BattleHudTheme.Gold);
        BattleHudTheme.Label(new Rect(x+14,710,190,19),"장착 장비  "+unit.items.Count+" / 3",11,BattleHudTheme.Muted);
        for(int i=0;i<DigimonBuildCatalog.EquipmentSlots;i++)
        {
            Rect slot=new Rect(x+14+i*65,733,60,49);BattleHudTheme.Round(slot,BattleHudTheme.Raised);
            if(i<unit.items.Count)
            {
                int item=unit.items[i];DigimonEquipmentArt.Draw(new Rect(slot.x+11,slot.y+5,38,38),item);
                if(DigimonEquipmentUI.RightClick(slot)||GUI.Button(slot,new GUIContent("",DigimonEquipmentArt.Tooltip(item)),GUIStyle.none))OpenEquipmentGuide(item);
            }
            else BattleHudTheme.Label(slot,"빈 슬롯",10,BattleHudTheme.Muted,false,TextAnchor.MiddleCenter);
        }
        if((battling||lastBattleReport.Count>0)&&HudButton(new Rect(x+14,790,190,22),"전투 기록 보기")){inspectedUnit=null;showCombatReport=true;}
    }
    static void DrawTacticalStatPair(Rect r,string left,string leftValue,string right,string rightValue)
    {
        float half=r.width*.5f;
        BattleHudTheme.Label(new Rect(r.x+5,r.y,39,r.height),left,11,BattleHudTheme.Muted);
        BattleHudTheme.Label(new Rect(r.x+40,r.y,half-47,r.height),leftValue,13,null,true,TextAnchor.MiddleRight);
        BattleHudTheme.Label(new Rect(r.x+half+5,r.y,39,r.height),right,11,BattleHudTheme.Muted);
        BattleHudTheme.Label(new Rect(r.x+half+40,r.y,half-47,r.height),rightValue,13,null,true,TextAnchor.MiddleRight);
    }
}
