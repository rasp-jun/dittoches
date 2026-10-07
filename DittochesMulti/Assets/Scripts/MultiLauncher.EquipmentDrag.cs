using System.Linq;
using UnityEngine;

public sealed partial class MultiLauncher
{
    readonly EquipmentDrag onlineEquipmentDrag=new EquipmentDrag();
    void OnApplicationFocus(bool focused){if(!focused){onlineEquipmentDrag.Reset();ResetEquipmentSelection();ResetOnlineDrag();arenaPointer.Reset();}}
    void OnDisable(){onlineEquipmentDrag.Reset();ResetEquipmentSelection();ResetOnlineDrag();arenaPointer.Reset();}
    Rect OnlineInventoryRect(int slot){return new Rect(39+slot%4*57,695+slot/4*43,39,39);}
    Rect onlineEquipmentTargetRect;
    Rect OnlinePieceRect(string id,int star,Vector3 ground)
    {
        Vector2 head=arena.Project(ground+Vector3.up*TacticalArena.DigimonHeadHeight(id,star)),feet=arena.Project(ground);
        return new Rect(feet.x-36,head.y,72,Mathf.Max(24,feet.y-head.y+8));
    }
    Rect OnlineCombatBody(Fighter f)
    {
        var room=state.room;float remaining=Mathf.Max(0,room.remaining-(Time.unscaledTime-receivedAt));
        float progress=CombatProgress(room,remaining);int frame=Mathf.FloorToInt(progress),next=Mathf.Min(frame+1,room.frames.Length-1);
        var to=System.Array.Find(room.frames[next].units,u=>u.key==f.key)??f;
        float x=Mathf.Lerp(f.x,to.x,progress-frame),y=Mathf.Lerp(f.y,to.y,progress-frame);
        if(room.side==1){x=6-x;y=7-y;}
        return OnlinePieceRect(f.id,f.star,TacticalArena.CellWorld(x,y));
    }
    Unit OnlineCombatEquipmentOwner(Fighter f,out string blocked)
    {
        blocked="";
        if(f.side!=state.room.side){blocked="상대 유닛에는 장착할 수 없습니다";return null;}
        var owner=At(OnlineMe.board,f.slot);
        if(owner==null||owner.id!=f.id){blocked="합성으로 회수된 유닛입니다";return null;}
        return owner;
    }
    Unit OnlineEquipmentTarget(out string area,out int slot,out Fighter live,out string blocked)
    {
        area="";slot=-1;live=null;blocked="";onlineEquipmentTargetRect=new Rect();var me=OnlineMe;var room=state.room;
        Vector2 p=Event.current.mousePosition;int seat=arena.HitBench(p),cell=arena.HitCell(p);
        var current=room.phase=="battle"?VisibleCombatFrame(room,Mathf.Max(0,room.remaining-(Time.unscaledTime-receivedAt))):null;
        if(current!=null)foreach(var row in onlineCombatLabels)
        {
            if(!row.rect.Contains(p))continue;
            var f=System.Array.Find(current.units,u=>u.key==((Fighter)row.key).key&&u.hp>0);if(f==null)continue;
            onlineEquipmentTargetRect=row.rect;area="board";slot=f.slot;live=f;return OnlineCombatEquipmentOwner(f,out blocked);
        }
        foreach(var unit in me.bench.OrderByDescending(u=>u.slot))
        {
            var r=OnlinePieceRect(unit.id,unit.star,TacticalArena.BenchWorld(unit.slot));
            if(!r.Contains(p))continue;
            onlineEquipmentTargetRect=r;area="bench";slot=unit.slot;return unit;
        }
        if(seat>=0){area="bench";slot=seat;var unit=At(me.bench,seat);if(unit!=null)onlineEquipmentTargetRect=OnlinePieceRect(unit.id,unit.star,TacticalArena.BenchWorld(seat));return unit;}
        if(room.phase=="prepare")
        {
            foreach(bool own in new[]{true,false})foreach(var unit in room.players[own?room.side:1-room.side].board.OrderByDescending(u=>own?u.slot/7:3-u.slot/7))
            {
                int x=own?unit.slot%7:6-unit.slot%7,y=own?unit.slot/7+4:3-unit.slot/7;
                var r=OnlinePieceRect(unit.id,unit.star,TacticalArena.CellWorld(x,y));if(!r.Contains(p))continue;
                onlineEquipmentTargetRect=r;
                if(!own){blocked="상대 유닛에는 장착할 수 없습니다";return null;}
                area="board";slot=unit.slot;return unit;
            }
            if(cell>=28){area="board";slot=cell-28;var unit=At(me.board,slot);if(unit!=null)onlineEquipmentTargetRect=OnlinePieceRect(unit.id,unit.star,TacticalArena.CellWorld(slot%7,slot/7+4));return unit;}
            if(cell>=0)blocked="상대 유닛에는 장착할 수 없습니다";
            return null;
        }
        if(current==null)return null;
        foreach(var f in current.units.Where(u=>u.hp>0).OrderByDescending(u=>OnlineCombatBody(u).yMax))
        {
            var r=OnlineCombatBody(f);if(!r.Contains(p))continue;
            onlineEquipmentTargetRect=r;area="board";slot=f.slot;live=f;return OnlineCombatEquipmentOwner(f,out blocked);
        }
        return null;
    }
    void HandleOnlineEquipmentClick(bool editable)
    {
        Event e=Event.current;if(artPack!=0||!editable||!GUI.enabled||busy||onlineItem<0||e.type!=EventType.MouseDown||e.button!=0)return;
        string area,blocked;int slot;Fighter live;var unit=OnlineEquipmentTarget(out area,out slot,out live,out blocked);
        if(unit!=null){EquipOnlineSelection(area,slot,unit);arenaPointer.Reset();e.Use();}
        else if(blocked.Length>0){notice=blocked;e.Use();}
    }
    void HandleOnlineEquipmentDrag(Room room,Player me,bool editable)
    {
        if(artPack!=0)return;
        Event e=Event.current;int hovered=-1;
        for(int i=0;i<12;i++)if(OnlineInventoryRect(i).Contains(e.mousePosition))hovered=onlineItemPage*12+i;
        var result=onlineEquipmentDrag.Update(e,hovered,me.inventory,room.id+":"+room.round+":"+room.phase+":"+me.inventoryRevision,
            editable&&!busy&&GUI.enabled);
        if(onlineEquipmentDrag.Dragging)
        {onlineItem=onlineEquipmentDrag.Slot;onlineItemRevision=me.inventoryRevision;ClearOnlineUnitSelection();arenaPointer.Reset();}
        if(result==EquipmentDrag.Result.Click)SelectOnlineItem(onlineEquipmentDrag.Slot);
        else if(result==EquipmentDrag.Result.Drop)
        {
            string area,blocked;int slot;Fighter live;var unit=OnlineEquipmentTarget(out area,out slot,out live,out blocked);
            if(unit!=null)
            {
                var preview=DigimonBuildCatalog.PreviewEquipment(unit.items??new int[0],me.inventory[onlineItem],unit.id);
                if(preview.allowed)EquipOnlineSelection(area,slot,unit);else notice=preview.message;
            }
            else notice=blocked.Length>0?blocked:"장착 취소 · 장비는 보관함에 있습니다";
            ResetEquipmentSelection();
        }
        else if(result==EquipmentDrag.Result.Cancel)ResetEquipmentSelection();
        if(result!=EquipmentDrag.Result.None)onlineEquipmentDrag.Reset();
    }
    void DrawOnlineEquipmentGhost()
    {
        if(artPack!=0)return;
        var me=OnlineMe;if(onlineItem<0||onlineItem>=me.inventory.Length||!GUI.enabled)return;
        string area,blocked;int slot;Fighter live;var target=OnlineEquipmentTarget(out area,out slot,out live,out blocked);
        bool allowed=target!=null&&DigimonBuildCatalog.PreviewEquipment(target.items??new int[0],me.inventory[onlineItem],target.id).allowed;
        EquipmentTargetFeedback.Draw(onlineEquipmentTargetRect,allowed);
        if(!onlineEquipmentDrag.Dragging)return;
        Vector2 p=Event.current.mousePosition;Rect r=new Rect(Mathf.Clamp(p.x+20,8,1282),Mathf.Clamp(p.y-72,85,850),310,66);
        Card(r,surface,allowed?new Color(.35f,.95f,.65f):target!=null||blocked.Length>0?new Color(1f,.38f,.3f):gold);DigimonEquipmentArt.Draw(new Rect(r.x+8,r.y+10,44,44),me.inventory[onlineItem]);
        string hint=target==null?(blocked.Length>0?blocked:"아군에게 놓기 · ESC 취소"):DigimonBuildCatalog.PreviewEquipment(target.items??new int[0],me.inventory[onlineItem],target.id).message;
        GUI.Label(new Rect(r.x+62,r.y+5,242,24),DigimonBuildCatalog.Data.items[me.inventory[onlineItem]].name,small);
        GUI.Label(new Rect(r.x+62,r.y+29,240,32),hint,new GUIStyle(small){fontSize=12,wordWrap=true});
    }
}
