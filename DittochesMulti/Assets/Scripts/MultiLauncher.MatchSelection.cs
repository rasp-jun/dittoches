using UnityEngine;

public sealed partial class MultiLauncher
{
    void ClearOnlineUnitSelection()
    {selectedSlot=-1;selectedArea="";arenaPointer.Reset();}
    static Player MatchPlayer(Room room)
    {return room==null||room.players==null||room.side<0||room.side>=room.players.Length?null:room.players[room.side];}
    Unit SelectedInRoom(Room room)
    {
        var me=MatchPlayer(room);
        if(me==null||selectedSlot<0||(selectedArea!="board"&&selectedArea!="bench"))return null;
        var units=selectedArea=="board"?me.board:me.bench;return units==null?null:At(units,selectedSlot);
    }
    void ReconcileMatchSelection(Room previous,Room next)
    {
        bool contextChanged=previous==null||next==null||previous.id!=next.id||previous.side!=next.side||previous.phase!=next.phase||previous.round!=next.round;
        if(contextChanged)
        {
            ClearOnlineUnitSelection();ResetEquipmentSelection();onlineHistoricalFighter=null;onlineReport=true;
            return;
        }
        if(next.phase=="finished"){ClearOnlineUnitSelection();return;}
        if(selectedSlot>=0)
        {
            var before=SelectedInRoom(previous);var after=SelectedInRoom(next);
            if(before==null||after==null||before.id!=after.id||before.star!=after.star)ClearOnlineUnitSelection();
        }
        var priorMe=MatchPlayer(previous);var nextMe=MatchPlayer(next);
        if(priorMe==null||nextMe==null||priorMe.ready!=nextMe.ready)arenaPointer.Reset();
    }
    int OnlinePreparationCell(Room room)
    {return room.phase=="prepare"&&selectedArea=="board"&&selectedSlot>=0?selectedSlot+28:-1;}
    string OnlineRangeHint(Room room,Player me,float remaining)
    {
        var unit=SelectedOnlineUnit(me);if(unit==null)return "";
        int range=DigimonSkillCatalog.Find(unit.id).attackRange;
        if(room.phase=="battle")
        {
            if(selectedArea!="board")return "";
            var frame=VisibleCombatFrame(room,remaining);Fighter live=null;
            if(frame!=null)foreach(var f in frame.units)if(f.side==room.side&&f.slot==unit.slot&&f.id==unit.id){live=f;break;}
            if(live==null)return "";
            if(live.hp<=0)return Def(unit.id).name+" · 전투 불능 · 오른쪽에서 전투 정보 확인";
            if(live.attackRange>0)range=live.attackRange;
        }
        if(room.phase=="finished")return "";
        return Def(unit.id).name+" · 기본 공격 "+range+"칸  /  하늘색: 공격 범위";
    }
    string OnlineCombatSummary(Room room,float remaining)
    {
        var frame=VisibleCombatFrame(room,remaining);if(frame==null)return "전투 정보 수신 중";
        int own=0,ownTotal=0,enemy=0,enemyTotal=0;
        foreach(var f in frame.units)
        {if(f.side==room.side){ownTotal++;if(f.hp>0)own++;}else{enemyTotal++;if(f.hp>0)enemy++;}}
        return "아군 "+own+" / "+ownTotal+"  ·  상대 "+enemy+" / "+enemyTotal+"  ·  아군 클릭: 전투 정보";
    }
}
