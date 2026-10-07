using System.Linq;
using UnityEngine;

public sealed partial class MultiLauncher
{
    string onlineSkillId="",onlineSkillArea="";int onlineSkillSlot=-1;
    void OpenOnlineSkill(Unit unit,string area)
    {onlineSkillId=unit.id;onlineSkillArea=area;onlineSkillSlot=unit.slot;onlineItemGuide=-1;arenaPointer.Reset();}
    Unit SelectedOnlineUnit(Player me)
    {return selectedSlot<0?null:At(selectedArea=="board"?me.board:me.bench,selectedSlot);}
    DigimonSkillCatalog.Stats OnlineStats(Unit unit,string area)
    {
        var me=OnlineMe;var ids=me!=null&&area=="board"?me.board.Select(BuildMember):Enumerable.Empty<DigimonBuildCatalog.Member>();
        var bonus=DigimonBuildCatalog.Resolve(unit.id,ids,unit.items??new int[0]);
        var stats=new DigimonSkillCatalog.Stats(DigimonSkillCatalog.Find(unit.id),unit.star,bonus);
        var live=OnlineLiveFighter(unit,area);
        if(live!=null)
        {
            stats.speed=DigimonCombatMath.AttackSpeed(DigimonSkillCatalog.Find(unit.id).attackSpeed,bonus.speed,bonus.rampSpeed,live.combatAge);
            if(live.combatStatsVersion>=1){stats.attack=live.attackDamage;stats.abilityPower=live.abilityPower;stats.health=live.maxHp;stats.armor=live.armor;stats.magicResist=live.magicResist;stats.range=live.attackRange;stats.speed=live.attackSpeed;}
        }
        return stats;
    }
    void DrawOnlineSkillStats(Unit unit)
    {
        var stats=OnlineStats(unit,selectedArea);var skill=DigimonSkillCatalog.Find(unit.id);
        var live=OnlineLiveFighter(unit,selectedArea);
        Card(new Rect(1305,490,270,430),surface,new Color(.2f,.38f,.43f));
        GUI.Label(new Rect(1320,504,240,28),skill.ScalingRole+" · "+skill.DamageLabel+" 스킬",eyebrow);
        if(live!=null)
        {
            float time=CombatTime(state.room,Mathf.Max(0,state.room.remaining-(Time.unscaledTime-receivedAt)));
            string status=live.hp<=0?"전투 불능":live.stun>0?"기절 "+live.stun.ToString("0.0")+"초":ActiveCastAge(state.room,live.key,time)>=0?"스킬 시전":live.mana>=live.maxMana?"스킬 준비":"전투 중";
            CombatReportUI.Vitals(new Rect(1320,539,240,90),live.hp,live.maxHp,live.mana,live.maxMana,live.shield,status);
            GUI.Label(new Rect(1320,638,240,57),"공격력 "+stats.attack.ToString("0.#")+" · 주문력 "+stats.abilityPower.ToString("0.#")+"\n방어 "+stats.armor.ToString("0")+" · 마저 "+stats.magicResist.ToString("0")+"\n공속 "+stats.speed.ToString("0.00")+" · 사거리 "+stats.range+"칸",new GUIStyle(small){fontSize=13});
        }
        else GUI.Label(new Rect(1320,543,240,139),"공격력 "+stats.attack.ToString("0.#")+" · 주문력 "+stats.abilityPower.ToString("0.#")+"\n최대 체력 "+stats.health.ToString("0")+"\n방어 "+stats.armor.ToString("0")+" · 마저 "+stats.magicResist.ToString("0")+"\n공속 "+stats.speed.ToString("0.00")+" · 사거리 "+stats.range+"칸\n시작 마나 "+stats.startMana.ToString("0")+" / "+stats.maxMana.ToString("0"),small);
        var me=OnlineMe;var ids=me!=null&&selectedArea=="board"?me.board.Select(BuildMember):Enumerable.Empty<DigimonBuildCatalog.Member>();
        var bonus=DigimonBuildCatalog.Resolve(unit.id,ids,unit.items??new int[0]);
        CombatStatusUI.Draw(new Rect(1320,700,240,84),CombatStatusUI.Describe(bonus,live!=null?live.combatAge:0,live!=null?live.attacks:0,live!=null&&live.lowShieldUsed,live!=null,live!=null&&live.hp<=0));
        if(DigimonSkillUI.DrawIcon(new Rect(1320,795,54,54),skill))OpenOnlineSkill(unit,selectedArea);
        GUI.Label(new Rect(1385,793,170,38),skill.name,small);
        GUI.Label(new Rect(1385,831,170,22),"우클릭 → 스킬 정보",new GUIStyle(small){fontSize=12});
        GUI.Label(new Rect(1320,863,240,50),"계산 "+skill.Damage(unit.star,stats.attack,stats.abilityPower).ToString("0.#")+" 피해 · "+skill.DamageLabel+"\n"+(selectedArea=="board"?"장비 + 전장 시너지":"대기석 · 장비만 적용"),small);
    }
    void DrawOnlineSkillDetails()
    {
        if(string.IsNullOrEmpty(onlineSkillId))return;
        Unit unit;string context;
        if(onlineSkillArea==""||onlineSkillArea=="shop"){unit=new Unit{id=onlineSkillId,star=1,items=new int[0]};context=(onlineSkillArea=="shop"?"상점":"도감")+" · 1성 기본 능력치 (장비·시너지 없음)";}
        else
        {
            var me=OnlineMe;unit=me==null?null:At(onlineSkillArea=="board"?me.board:me.bench,onlineSkillSlot);
            if(unit==null||unit.id!=onlineSkillId){onlineSkillId="";return;}
            context=onlineSkillArea=="board"?"현재 장비 + 전장 시너지 적용":"대기석 · 장비만 적용 (시너지 미포함)";
        }
        Panel(new Rect(0,0,1600,1000),new Color(0,0,0,.5f));
        if(!DigimonSkillUI.DrawDetails(new Rect(475,170,650,640),DigimonSkillCatalog.Find(unit.id),unit.star,OnlineStats(unit,onlineSkillArea),context))onlineSkillId="";
    }
}
