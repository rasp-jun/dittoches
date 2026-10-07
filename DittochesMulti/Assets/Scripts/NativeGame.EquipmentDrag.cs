using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    readonly EquipmentDrag equipmentDrag=new EquipmentDrag();
    Rect InventorySlotRect(int slot){return new Rect(30+slot%4*47,550+slot/4*46,40,40);}
    void DrawEquipmentTargetFeedback()
    {
        if(artPack!=0||!GUI.enabled||selectedItem<0||selectedItem>=inventory.Count)return;
        string blocked;var target=EquipmentHoverTarget(out blocked);
        EquipmentTargetFeedback.Draw(equipmentTargetRect,target!=null&&CanEquipSelected(target));
    }
    void HandleEquipmentClick(bool allowed)
    {
        Event e=Event.current;if(artPack!=0||!allowed||!GUI.enabled||selectedItem<0||e.type!=EventType.MouseDown||e.button!=0)return;
        string blocked;var target=EquipmentHoverTarget(out blocked);
        if(target!=null){Equip(target);arenaPointer.Reset();e.Use();}
        else if(blocked.Length>0){NotifyPlacement(blocked);e.Use();}
    }
    void HandleEquipmentDrag(bool allowed)
    {
        if(artPack!=0)return;
        Event e=Event.current;int hovered=-1;
        for(int i=0;i<12;i++)if(InventorySlotRect(i).Contains(e.mousePosition))hovered=inventoryPage*12+i;
        var result=equipmentDrag.Update(e,hovered,inventory.ToArray(),round+":"+battling,allowed&&GUI.enabled&&hp>0&&scoutedRival<0);
        if(equipmentDrag.Dragging){selectedItem=equipmentDrag.Slot;selectedBoard=selectedBench=-1;dragSource=-1;draggingUnit=false;arenaPointer.Reset();}
        if(result==EquipmentDrag.Result.Click)SelectInventoryItem(equipmentDrag.Slot);
        else if(result==EquipmentDrag.Result.Drop)
        {
            string blocked;Unit target=EquipmentHoverTarget(out blocked);
            if(target!=null)Equip(target);
            else NotifyPlacement(blocked.Length>0?blocked:"장착 취소 · 장비는 보관함에 있습니다");
            selectedItem=-1;
        }
        else if(result==EquipmentDrag.Result.Cancel)selectedItem=-1;
        if(result!=EquipmentDrag.Result.None)equipmentDrag.Reset();
    }
}
