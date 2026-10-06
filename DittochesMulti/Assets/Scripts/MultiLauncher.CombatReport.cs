using System;
using System.Linq;
using UnityEngine;

public sealed partial class MultiLauncher
{
    readonly CombatReportUI.Table onlineReportRows=new CombatReportUI.Table();
    bool onlineReport=true;int onlineReportMetric;
    Fighter onlineHistoricalFighter;int onlineHistoricalRound;string onlineHistoricalRoom;
    void OpenHistoricalCombat(Fighter fighter,Room room)
    {
        onlineHistoricalFighter=fighter.Snapshot();onlineHistoricalRound=room.reportRound;onlineHistoricalRoom=room.id;
        selectedArea="";selectedSlot=-1;ResetEquipmentSelection();onlineReport=true;
    }
    void DrawHistoricalCombat()
    {
        var f=onlineHistoricalFighter;
        Rect r=new Rect(1305,290,270,638);Card(r,surface,new Color(.2f,.38f,.43f));
        GUI.Label(new Rect(1320,303,240,30),"지난 전투 · "+onlineHistoricalRound,eyebrow);
        FaithfulPortraits.Draw(new Rect(1320,344,79,77),f.id,true);
        GUI.Label(new Rect(1410,345,147,51),Def(f.id).name,new GUIStyle(text){fontSize=16});
        GUI.Label(new Rect(1410,395,147,26),new string('★',f.star)+" · "+(f.hp>0?"생존":"전투 불능"),small);
        CombatReportUI.Vitals(new Rect(1320,432,240,89),f.hp,f.maxHp,f.mana,f.maxMana,f.shield,"최종 상태");
        var body=new GUIStyle(small){fontSize=13,wordWrap=true};
        string stats=f.combatStatsVersion>0?"공격력 "+f.attackDamage.ToString("0.#")+" · 주문력 "+f.abilityPower.ToString("0.#")+
            "\n방어 "+f.armor.ToString("0")+" · 마저 "+f.magicResist.ToString("0")+"\n공속 "+f.attackSpeed.ToString("0.00")+" · 사거리 "+f.attackRange+"칸":"이 기록에는 세부 능력치가 없습니다.";
        GUI.Label(new Rect(1320,535,240,74),stats,body);
        GUI.Label(new Rect(1320,618,240,86),"가한 피해 "+f.damageDone.ToString("N0")+"\n기본 공격 "+f.basicDamageDone.ToString("N0")+" · 기술 "+f.skillDamageDone.ToString("N0")+
            "\n받은 피해 "+f.damageTaken.ToString("N0")+" · 흡수 "+f.shieldAbsorbed.ToString("N0"),body);
        GUI.Label(new Rect(1320,711,240,84),"회복 "+f.healingDone.ToString("N0")+" · 보호막 생성 "+f.shieldingDone.ToString("N0")+
            "\n기본 공격 "+f.attacks+"회 · 기술 "+f.casts+"회"+(f.lowShieldUsed?"\n위기 효과 사용 완료":""),body);
        GUI.Label(new Rect(1320,811,240,52),"전투 종료 시 저장된 기록\n현재 배치·장비를 변경해도 유지됩니다.",new GUIStyle(body){fontSize=12});
        if(Btn(new Rect(1320,874,240,36),"전투 기록 목록으로"))onlineHistoricalFighter=null;
    }
    Frame VisibleCombatFrame(Room room,float remaining)
    {
        if(room==null||room.phase!="battle"||room.frames==null||room.frames.Length==0)return null;
        // Counters come from the current frame, never the precomputed final frame.
        return room.frames[Mathf.Clamp(Mathf.FloorToInt(CombatProgress(room,remaining)),0,room.frames.Length-1)];
    }
    Fighter OnlineLiveFighter(Unit unit,string area)
    {
        if(area!="board"||state==null||state.room==null)return null;
        var room=state.room;var frame=VisibleCombatFrame(room,Mathf.Max(0,room.remaining-(Time.unscaledTime-receivedAt)));
        return frame==null?null:Array.Find(frame.units,f=>f.side==room.side&&f.slot==unit.slot&&f.id==unit.id);
    }
    bool DrawOnlineReport(Room room,float remaining)
    {
        if(onlineHistoricalFighter!=null&&(onlineHistoricalRoom!=room.id||onlineHistoricalRound!=room.reportRound))onlineHistoricalFighter=null;
        var frame=VisibleCombatFrame(room,remaining);
        var source=frame!=null?frame.units:room.lastCombat??new Fighter[0];
        if(source.Length==0)return false;
        if(onlineItem<0&&Btn(new Rect(1305,265,270,22),onlineReport?"유닛 정보 · 시너지 보기":"전투 기록 보기")){onlineReport=!onlineReport;onlineHistoricalFighter=null;}
        if(!onlineReport)return false;
        if(onlineHistoricalFighter!=null){DrawHistoricalCombat();return true;}
        var rows=onlineReportRows;rows.Clear();
        foreach(var f in source)
        {
            if(f.side!=room.side)continue;var row=rows.Add();
            row.key=f;row.name=Def(f.id).name;row.portrait=FaithfulPortraits.Get(f.id,true)??LobbyPortrait(f.id);row.dead=f.hp<=0;
            row.damage=f.damageDone;row.basic=f.basicDamageDone;row.skill=f.skillDamageDone;row.taken=f.damageTaken;row.absorbed=f.shieldAbsorbed;
            row.healing=f.healingDone;row.shielding=f.shieldingDone;row.casts=f.casts;
        }
        var clicked=CombatReportUI.Draw(new Rect(1305,290,270,638),rows,ref onlineReportMetric,frame!=null?"전투 기록 · "+room.round:"지난 전투 · "+room.reportRound);
        if(clicked!=null)
        {
            var fighter=(Fighter)clicked.key;
            if(frame!=null){selectedArea="board";selectedSlot=fighter.slot;onlineReport=false;}
            else OpenHistoricalCombat(fighter,room);
        }
        return true;
    }
}
