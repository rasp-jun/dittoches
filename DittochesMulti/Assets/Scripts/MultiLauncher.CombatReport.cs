using System;
using System.Linq;
using UnityEngine;

public sealed partial class MultiLauncher
{
    bool onlineReport=true;int onlineReportMetric;
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
        var frame=VisibleCombatFrame(room,remaining);
        var source=frame!=null?frame.units:room.lastCombat??new Fighter[0];
        if(source.Length==0)return false;
        if(onlineItem<0&&Btn(new Rect(1305,265,270,22),onlineReport?"유닛 정보 · 시너지 보기":"전투 기록 보기"))onlineReport=!onlineReport;
        if(!onlineReport)return false;
        var rows=source.Where(f=>f.side==room.side).Select(f=>new CombatReportUI.Row{
            key=f,name=Def(f.id).name,portrait=LobbyPortrait(f.id),dead=f.hp<=0,
            damage=f.damageDone,basic=f.basicDamageDone,skill=f.skillDamageDone,taken=f.damageTaken,absorbed=f.shieldAbsorbed,
            healing=f.healingDone,shielding=f.shieldingDone,casts=f.casts}).ToArray();
        var clicked=CombatReportUI.Draw(new Rect(1305,290,270,638),rows,ref onlineReportMetric,frame!=null?"전투 기록 · "+room.round:"지난 전투 · "+room.reportRound);
        // Historical rows are immutable results, not handles to possibly moved/sold units.
        if(clicked!=null&&frame!=null){var fighter=(Fighter)clicked.key;selectedArea="board";selectedSlot=fighter.slot;onlineReport=false;}
        return true;
    }
}
