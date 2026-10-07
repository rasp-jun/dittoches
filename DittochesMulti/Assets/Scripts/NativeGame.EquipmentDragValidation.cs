#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    void EquipmentGesture(EventType type,Vector2 point,KeyCode key=KeyCode.None,int button=0)
    {Event.current=new Event{type=type,mousePosition=point,keyCode=key,button=button};DrawGame();}
    void ValidateEquipmentDragInGUI()
    {
        var oldBoard=(Unit[])board.Clone();var oldBench=(Unit[])bench.Clone();var oldBag=inventory.ToArray();int oldRound=round;
        try
        {
            Array.Clear(board,0,board.Length);Array.Clear(bench,0,bench.Length);inventory.Clear();inventory.AddRange(new[]{8,12,14});
            selectedItem=selectedBoard=selectedBench=-1;inventoryPage=0;bench[0]=new Unit(RosterById["agumon"]);
            Vector2 start=InventorySlotRect(0).center,target=arena.Project(TacticalArena.BenchWorld(0)),outside=new Vector2(280,1050);
            EquipmentGesture(EventType.MouseDown,start);EquipmentGesture(EventType.MouseUp,start);
            Require(selectedItem==0&&inventory.Count==3,"equipment click still selects without consuming");
            EquipmentGesture(EventType.MouseDown,start);EquipmentGesture(EventType.MouseUp,start);
            Require(selectedItem==-1,"equipment click still toggles selection");
            EquipmentGesture(EventType.MouseDown,start);EquipmentGesture(EventType.MouseDrag,target);
            Require(equipmentDrag.Dragging&&selectedItem==0&&!draggingUnit,"gear drag cannot start unit drag");
            EquipmentGesture(EventType.KeyDown,target,KeyCode.Escape);EquipmentGesture(EventType.MouseUp,target);
            Require(inventory.Count==3&&bench[0].items.Count==0&&selectedItem==-1,"Escape cancels gear without delayed drop");
            EquipmentGesture(EventType.MouseDown,start);EquipmentGesture(EventType.MouseDrag,target);
            EquipmentGesture(EventType.MouseDown,target,button:1);EquipmentGesture(EventType.MouseUp,target);
            Require(inventory.Count==3&&bench[0].items.Count==0&&!equipmentDrag.Dragging,"right click cancels gear");
            EquipmentGesture(EventType.MouseDown,start);EquipmentGesture(EventType.MouseDrag,outside);EquipmentGesture(EventType.MouseUp,outside);
            Require(inventory.Count==3&&bench[0].items.Count==0,"empty shop drop never consumes gear or buys");
            EquipmentGesture(EventType.MouseDown,start);EquipmentGesture(EventType.MouseDrag,target);EquipmentGesture(EventType.MouseUp,target);
            Require(bench[0].items.SequenceEqual(new[]{8})&&inventory.SequenceEqual(new[]{12,14}),"bench drop equips exactly once");
            EquipmentGesture(EventType.MouseUp,target);Require(inventory.Count==2,"duplicate mouse release does not equip again");
            bench[0].items.Add(10);EquipmentGesture(EventType.MouseDown,start);EquipmentGesture(EventType.MouseDrag,target);EquipmentGesture(EventType.MouseUp,target);
            Require(inventory.SequenceEqual(new[]{12,14})&&bench[0].items.SequenceEqual(new[]{8,10}),"full slots reject drop without loss");
            EquipmentGesture(EventType.MouseDown,start);EquipmentGesture(EventType.MouseDrag,target);inventory.Insert(0,1);EquipmentGesture(EventType.MouseUp,target);
            Require(inventory.SequenceEqual(new[]{1,12,14}),"inventory change cancels stale slot");
            EquipmentGesture(EventType.MouseDown,start);EquipmentGesture(EventType.MouseDrag,target);round++;EquipmentGesture(EventType.MouseUp,target);
            Require(inventory.Count==3&&!equipmentDrag.Dragging,"round transition cancels stale gear");
            EquipmentGesture(EventType.MouseDown,start);EquipmentGesture(EventType.MouseDrag,target);showTeamPlan=true;
            EquipmentGesture(EventType.MouseUp,target);showTeamPlan=false;
            Require(inventory.Count==3&&!equipmentDrag.Dragging,"modal opening cancels gear");
            inventory.Clear();inventory.AddRange(new[]{0,1});
            foreach(int slot in new[]{0,1}){var p=InventorySlotRect(slot).center;EquipmentGesture(EventType.MouseDown,p);EquipmentGesture(EventType.MouseUp,p);}
            Require(inventory.SequenceEqual(new[]{5}),"two material clicks still combine once");
            inventory.Clear();inventory.Add(8);board[3]=new Unit(RosterById["agumon"]);SetupBattle();battling=true;
            var live=fighters.First(f=>!f.enemy);live.renderPos=new Vector2(3,5);float max=live.maxHp;live.hp=max*.5f;
            Vector2 head=arena.Project(FighterWorld(live)+Vector3.up*TacticalArena.DigimonHeadHeight(live.unit.def.id,live.unit.star)),feet=arena.Project(FighterWorld(live));
            target=Vector2.Lerp(head,feet,.5f);
            EquipmentGesture(EventType.MouseDown,start);EquipmentGesture(EventType.MouseDrag,target);EquipmentGesture(EventType.MouseUp,target);
            Require(board[3].items.SequenceEqual(new[]{8})&&live.maxHp>max&&Mathf.Abs(live.hp/live.maxHp-.5f)<.001f,"combat drag immediately changes live stats without healing");
            inventory.Add(12);live.mana=7;PrepareCombatLabels();target=fighterLabels[live].rect.center;
            var projection=DigimonEquipmentPreview.Create(live.unit.def.id,live.unit.star,fighters.Where(f=>!f.enemy).Select(f=>f.unit.def.id),live.unit.items,12);
            DigimonEquipmentPreview.SetCombatState(projection,live.hp,live.maxHp,live.mana);
            Require(projection.combat&&projection.currentMana==7&&projection.after.startMana>projection.before.startMana,"combat preview separates current mana from opening mana");
            EquipmentGesture(EventType.MouseDown,start);EquipmentGesture(EventType.MouseDrag,target);EquipmentGesture(EventType.MouseUp,target);
            Require(board[3].items.SequenceEqual(new[]{8,12})&&live.mana==7,"combat health label accepts drag without opening mana replay");
            Require(Mathf.Abs(live.hp-projection.projectedHealth)<.01f,"combat preview matches applied current health");
            inventory.Add(14);PrepareCombatLabels();target=fighterLabels[live].rect.center;
            projection=DigimonEquipmentPreview.Create(live.unit.def.id,live.unit.star,fighters.Where(f=>!f.enemy).Select(f=>f.unit.def.id),live.unit.items,14);
            DigimonEquipmentPreview.SetCombatState(projection,live.hp,live.maxHp,live.mana);
            EquipmentGesture(EventType.MouseDown,start);EquipmentGesture(EventType.MouseUp,start);EquipmentGesture(EventType.MouseDown,target);
            Require(board[3].items.Count==0&&inventory.Count==2,"click and drag share health label target and extractor returns gear");
            Require(Mathf.Abs(live.hp-projection.projectedHealth)<.01f&&live.mana==7,"extractor preview preserves live health ratio and mana");
            string blocked;var enemy=fighters.First(f=>f.enemy);PrepareCombatLabels();Event.current=new Event{mousePosition=fighterLabels[enemy].rect.center};
            Require(EquipmentHoverTarget(out blocked)==null&&blocked.Length>0&&equipmentTargetRect.width>0,"enemy health label blocks gear and retains red feedback bounds");
            live.dead=true;Event.current=new Event{mousePosition=fighterLabels[live].rect.center};
            Require(EquipmentHoverTarget(out blocked)!=live.unit,"stale health label never targets a dead unit");
            live.dead=false;board[3]=null;
            Require(EquipmentHoverTarget(out blocked)==null&&blocked.Contains("합성"),"merged-away source cannot receive misleading equipment preview");
        }
        finally
        {
            equipmentDrag.Reset();fighters.Clear();battling=false;round=oldRound;showTeamPlan=false;
            Array.Copy(oldBoard,board,board.Length);Array.Copy(oldBench,bench,bench.Length);inventory.Clear();inventory.AddRange(oldBag);
            selectedItem=selectedBoard=selectedBench=-1;dragSource=-1;draggingUnit=false;arenaPointer.Reset();inspectedUnit=null;
        }
    }
}
#endif
