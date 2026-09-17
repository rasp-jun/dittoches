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
        var me=OnlineMe;var ids=me!=null&&area=="board"?me.board.Select(u=>u.id):Enumerable.Empty<string>();
        var bonus=DigimonBuildCatalog.Resolve(unit.id,ids,unit.items??new int[0]);
        return new DigimonSkillCatalog.Stats(DigimonSkillCatalog.Find(unit.id),unit.star,bonus);
    }
    void DrawOnlineSkillStats(Unit unit)
    {
        var stats=OnlineStats(unit,selectedArea);var skill=DigimonSkillCatalog.Find(unit.id);
        Card(new Rect(1305,490,270,430),surface,new Color(.2f,.38f,.43f));
        GUI.Label(new Rect(1320,504,240,28),skill.ScalingRole+" · "+skill.DamageLabel+" 스킬",eyebrow);
        GUI.Label(new Rect(1320,543,240,139),"공격력 "+stats.attack.ToString("0.#")+" · 주문력 "+stats.abilityPower.ToString("0.#")+"\n최대 체력 "+stats.health.ToString("0")+"\n방어 "+stats.armor.ToString("0")+" · 마저 "+stats.magicResist.ToString("0")+"\n공속 "+stats.speed.ToString("0.00")+" · 사거리 "+stats.range+"칸\n마나 "+stats.startMana.ToString("0")+" / "+stats.maxMana.ToString("0"),small);
        if(DigimonSkillUI.DrawIcon(new Rect(1320,694,64,64),skill))OpenOnlineSkill(unit,selectedArea);
        GUI.Label(new Rect(1395,693,158,54),skill.name,small);
        GUI.Label(new Rect(1395,748,158,27),"우클릭 → 스킬 정보",new GUIStyle(small){fontSize=12});
        GUI.Label(new Rect(1320,794,240,96),"현재 스킬 "+skill.Damage(unit.star,stats.attack,stats.abilityPower).ToString("0.#")+" 피해\n"+DigimonSkillUI.Formula(skill,unit.star)+"\n"+(selectedArea=="board"?"장비 + 전장 시너지 적용":"대기석 · 장비만 적용"),small);
    }
    void DrawOnlineSkillDetails()
    {
        if(string.IsNullOrEmpty(onlineSkillId))return;
        Unit unit;string context;
        if(onlineSkillArea==""){unit=new Unit{id=onlineSkillId,star=1,items=new int[0]};context="도감 · 1성 기본 능력치 (장비·시너지 없음)";}
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
