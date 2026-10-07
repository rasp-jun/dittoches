using UnityEngine;

public sealed partial class MultiLauncher
{
    int onlineDragSlot=-1,onlineDragStar;
    string onlineDragArea="",onlineDragId="";
    bool onlineDragging;
    Vector2 onlineDragStart;
    Rect OnlineSellZone { get { return new Rect(310,814,945,108); } }
    bool OnlineSaleVisible { get { return onlineDragging&&onlineDragSlot>=0; } }
    void ResetOnlineDrag(){onlineDragSlot=-1;onlineDragArea="";onlineDragId="";onlineDragging=false;}
    Unit OnlineDraggedUnit(Player me)
    {
        if(me==null||onlineDragSlot<0)return null;
        var unit=At(onlineDragArea=="board"?me.board:me.bench,onlineDragSlot);
        return unit!=null&&unit.id==onlineDragId&&unit.star==onlineDragStar?unit:null;
    }
    void HandleOnlineDrag(Room room,Player me,bool editable,int cell,int seat)
    {
        Event e=Event.current;
        if(!editable||busy||!GUI.enabled||onlineItem>=0){ResetOnlineDrag();return;}
        if((e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape)||(e.type==EventType.MouseDown&&e.button==1))
        {ResetOnlineDrag();return;}
        if(e.type==EventType.MouseDown&&e.button==0)
        {
            ResetOnlineDrag();
            string area=seat>=0?"bench":cell>=28&&room.phase=="prepare"?"board":"";
            int slot=seat>=0?seat:cell-28;
            var unit=area==""?null:At(area=="bench"?me.bench:me.board,slot);
            if(unit!=null){onlineDragSlot=slot;onlineDragArea=area;onlineDragId=unit.id;onlineDragStar=unit.star;onlineDragStart=e.mousePosition;}
        }
        if(onlineDragSlot>=0&&OnlineDraggedUnit(me)==null){ResetOnlineDrag();return;}
        if(e.type==EventType.MouseDrag&&onlineDragSlot>=0&&Vector2.Distance(e.mousePosition,onlineDragStart)>8)
        {onlineDragging=true;arenaPointer.Reset();e.Use();}
        if(e.type==EventType.MouseUp&&e.button==0)
        {
            if(onlineDragging&&OnlineDraggedUnit(me)!=null)
            {
                if(OnlineSellZone.Contains(e.mousePosition))
                    Send("/action",new Command{action="sell",area=onlineDragArea,slot=onlineDragSlot});
                else if(seat>=0||room.phase=="prepare"&&cell>=28)
                {
                    string target=seat>=0?"bench":"board";int slot=seat>=0?seat:cell-28;
                    if(target!=onlineDragArea||slot!=onlineDragSlot)
                        Send("/action",new Command{action="move",area=onlineDragArea,slot=onlineDragSlot,targetArea=target,targetSlot=slot});
                }
                ClearOnlineUnitSelection();e.Use();
            }
            ResetOnlineDrag();
        }
    }
    void DrawOnlineSaleTarget(Player me)
    {
        var unit=OnlineDraggedUnit(me);if(!OnlineSaleVisible||unit==null)return;
        int price=Def(unit.id).cost*(int)Mathf.Pow(3,unit.star-1);
        bool over=OnlineSellZone.Contains(Event.current.mousePosition);
        Card(OnlineSellZone,over?new Color(.35f,.12f,.07f):new Color(.12f,.09f,.04f),gold);
        Portrait(new Rect(325,824,82,88),unit.id);
        GUI.Label(new Rect(427,826,810,32),Def(unit.id).name+"  ·  판매 +"+price+" G",text);
        GUI.Label(new Rect(427,863,810,25),"상점 영역에 놓아 판매 · 장비 "+(unit.items==null?0:unit.items.Length)+"개 반환 · 밖에 놓거나 ESC로 취소",small);
        GUI.Label(new Rect(427,892,810,23),RecruitmentAdvice.Bank(me.gold+price),small);
        if(!over)
        {
            Vector2 p=Event.current.mousePosition;
            Card(new Rect(p.x+16,p.y-48,210,56),surface,gold);
            GUI.Label(new Rect(p.x+25,p.y-40,195,44),Def(unit.id).name+"\n상점으로 드래그 · "+price+"G",small);
        }
    }
    bool OnlineManagementAllowed(Room room,Player me)
    {return room!=null&&me!=null&&((room.phase=="battle"&&state!=null&&state.combatActions>=1)||(room.phase=="prepare"&&!me.ready));}
    bool OnlineConnectionFresh()
    {return !connectionError&&!recoveringLogin&&Time.unscaledTime-receivedAt<6;}
    bool CanSellOnline(Room room)
    {return selectedArea=="bench"||(room.phase=="prepare"&&selectedArea=="board");}
    void ClearOnlineUnitSelection()
    {selectedSlot=-1;selectedArea="";arenaPointer.Reset();ResetOnlineDrag();}
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
