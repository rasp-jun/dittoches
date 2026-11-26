using System.Linq;
using UnityEngine;

public sealed partial class MultiLauncher
{
    string onlineSkillId="",onlineSkillArea="";int onlineSkillSlot=-1;
    GUIStyle skillTypeSummary;
    void OpenOnlineSkill(Unit unit,string area)
    {onlineSkillId=unit.id;onlineSkillArea=area;onlineSkillSlot=unit.slot;onlineItemGuide=-1;arenaPointer.Reset();}
    Unit SelectedOnlineUnit(Player me)
    {return selectedSlot<0?null:At(selectedArea=="board"?me.board:me.bench,selectedSlot);}
    DigimonSkillCatalog.Stats OnlineStats(Unit unit,string area)
    {DigimonBuildCatalog.Bonus bonus;Fighter live;return OnlineStats(unit,area,out bonus,out live);}
    static int OnlineInspectionStar(Unit unit,Fighter live){return live!=null?live.star:unit.star;}
    DigimonSkillCatalog.Stats OnlineStats(Unit unit,string area,out DigimonBuildCatalog.Bonus bonus,out Fighter live)
    {
        bool scouting=area.StartsWith("scout-");var me=scouting?ScoutedPlayer:OnlineMe;var ids=me!=null&&(area=="board"||area=="scout-board")?me.board.Select(BuildMember):Enumerable.Empty<DigimonBuildCatalog.Member>();
        bonus=DigimonBuildCatalog.Resolve(unit.id,ids,unit.items??new int[0]);
        live=scouting?null:OnlineLiveFighter(unit,area);
        var stats=new DigimonSkillCatalog.Stats(DigimonSkillCatalog.Find(unit.id),OnlineInspectionStar(unit,live),bonus);
        if(live!=null)
        {
            stats.speed=DigimonCombatMath.AttackSpeed(DigimonSkillCatalog.Find(unit.id).attackSpeed,bonus.speed,bonus.rampSpeed,live.combatAge);
            if(live.combatStatsVersion>=1){stats.attack=live.attackDamage;stats.abilityPower=live.abilityPower;stats.health=live.maxHp;stats.armor=live.armor;stats.magicResist=live.magicResist;stats.range=live.attackRange;stats.speed=live.attackSpeed;}
        }
        return stats;
    }
    void DrawOnlineSkillStats(Unit unit)
    {
        DigimonBuildCatalog.Bonus bonus;Fighter live;
        var stats=OnlineStats(unit,selectedArea,out bonus,out live);var skill=DigimonSkillCatalog.Find(unit.id);int star=OnlineInspectionStar(unit,live);
        Card(new Rect(1305,490,270,430),surface,new Color(.2f,.38f,.43f));
        if(skillTypeSummary==null)skillTypeSummary=new GUIStyle(small){fontSize=12,wordWrap=true};
        GUI.Label(new Rect(1320,501,240,36),"스킬 계수: "+skill.ScalingStats(star)+"\n평타 물리 · 스킬 "+skill.DamageLabel,skillTypeSummary);
        if(live!=null)
        {
            float time=CombatTime(state.room,Mathf.Max(0,state.room.remaining-(Time.unscaledTime-receivedAt)));
            string status=live.hp<=0?"전투 불능":live.stun>0?"기절 "+live.stun.ToString("0.0")+"초":ActiveCastAge(state.room,live,time)>=0?"스킬 시전":live.mana>=live.maxMana?"스킬 준비":"전투 중";
            CombatReportUI.Vitals(new Rect(1320,539,240,90),live.hp,live.maxHp,live.mana,live.maxMana,live.shield,status);
            GUI.Label(new Rect(1320,638,240,57),"공격력 "+stats.attack.ToString("0.#")+" · 주문력 "+stats.abilityPower.ToString("0.#")+"\n방어 "+stats.armor.ToString("0")+" · 마저 "+stats.magicResist.ToString("0")+"\n공속 "+stats.speed.ToString("0.00")+" · 사거리 "+stats.range+"칸",new GUIStyle(small){fontSize=13});
        }
        else GUI.Label(new Rect(1320,543,240,139),"공격력 "+stats.attack.ToString("0.#")+" · 주문력 "+stats.abilityPower.ToString("0.#")+"\n최대 체력 "+stats.health.ToString("0")+"\n방어 "+stats.armor.ToString("0")+" · 마저 "+stats.magicResist.ToString("0")+"\n공속 "+stats.speed.ToString("0.00")+" · 사거리 "+stats.range+"칸\n시작 마나 "+stats.startMana.ToString("0")+" / "+stats.maxMana.ToString("0"),small);
        CombatStatusUI.Draw(new Rect(1320,700,240,84),CombatStatusUI.Describe(bonus,live!=null?live.combatAge:0,live!=null?live.attacks:0,live!=null&&live.lowShieldUsed,live!=null,live!=null&&live.hp<=0,live==null?null:new CombatStatusUI.State{burnRemaining=live.burnRemaining,shredRemaining=live.shredRemaining,battleAge=live.battleAge,synergyUsed=live.synergyUsed,revived=live.revived}));
        if(DigimonSkillUI.DrawIcon(new Rect(1320,795,54,54),skill))OpenOnlineSkill(unit,selectedArea);
        GUI.Label(new Rect(1385,793,170,38),skill.name,small);
        GUI.Label(new Rect(1385,831,170,22),"클릭 → 성급·스킬 정보",new GUIStyle(small){fontSize=12});
        if(GUI.Button(new Rect(1381,792,181,61),new GUIContent("",skill.name+" · 성급 비교와 스킬 상세"),GUIStyle.none))OpenOnlineSkill(unit,selectedArea);
        GUI.Label(new Rect(1320,863,240,50),"계산 "+skill.Damage(star,stats.attack,stats.abilityPower).ToString("0.#")+" 피해 · "+skill.DamageLabel+"\n"+(live!=null&&star!=unit.star?"전투 "+new string('★',star)+" · 다음 전투 "+new string('★',unit.star):selectedArea=="board"?"장비 + 전장 시너지":"대기석 · 장비만 적용"),small);
    }
    void DrawOnlineSkillDetails()
    {
        if(string.IsNullOrEmpty(onlineSkillId))return;
        Unit unit;string context;
        if(onlineSkillArea==""||onlineSkillArea=="shop"){unit=new Unit{id=onlineSkillId,star=1,items=new int[0]};context=(onlineSkillArea=="shop"?"상점":"도감")+" · 1성 기본 능력치 (장비·시너지 없음)";}
        else
        {
            bool scout=onlineSkillArea.StartsWith("scout-");var me=scout?ScoutedPlayer:OnlineMe;unit=me==null?null:At(onlineSkillArea.EndsWith("board")?me.board:me.bench,onlineSkillSlot);
            if(unit==null||unit.id!=onlineSkillId){onlineSkillId="";return;}
            context=onlineSkillArea=="scout-board"?"상대 장비 + 상대 전장 시너지 적용":onlineSkillArea=="board"?"현재 장비 + 전장 시너지 적용":"대기석 · 장비만 적용 (시너지 미포함)";
        }
        Panel(new Rect(0,0,1600,1000),new Color(0,0,0,.5f));
        var owner=onlineSkillArea.StartsWith("scout-")?ScoutedPlayer:OnlineMe;
        string collection=owner==null?"":StarUpgradeUI.Collection(RecruitCopies(owner,unit.id,1),RecruitCopies(owner,unit.id,2),RecruitCopies(owner,unit.id,3));
        if(state!=null&&state.room!=null&&state.room.phase=="battle"&&owner!=null&&owner==OnlineMe)collection=StarUpgradeUI.Progress(RecruitCopies(owner,unit.id,1),RecruitCopies(owner,unit.id,2),RecruitCopies(owner,unit.id,3))+"\n전투 중 승급은 다음 전투부터 적용 · 장비는 즉시 적용";
        DigimonBuildCatalog.Bonus bonus;Fighter live;var stats=OnlineStats(unit,onlineSkillArea,out bonus,out live);int star=OnlineInspectionStar(unit,live);
        if(live!=null&&star!=unit.star)context="현재 전투 "+new string('★',star)+" 능력치 · 다음 전투부터 "+new string('★',unit.star)+" 적용";
        if(!DigimonSkillUI.DrawDetails(new Rect(475,100,650,770),DigimonSkillCatalog.Find(unit.id),star,stats,context,collection))onlineSkillId="";
        if(onlineSkillId.Length>0&&onlineSkillArea=="shop"&&OnlineMe!=null)
            PurchaseForecastUI.Draw(new Rect(18,120,436,590),PreviewOnlinePurchase(OnlineMe,unit.id,state.room),unit.id,Def(unit.id).name,state.room.phase=="battle");
    }
}
