#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Linq;
using UnityEngine;

public sealed partial class MultiLauncher
{
    Vector2 FormationDragProbePoint(string area,int slot)
    {return arena.Project(area=="bench"?TacticalArena.BenchWorld(slot):TacticalArena.CellWorld(slot%7,slot/7+4));}
    void FormationDragProbeInput(EventType type,Vector2 point,KeyCode key=KeyCode.None)
    {
        Event.current=new Event{type=type,button=0,mousePosition=point,keyCode=key};
        var room=state.room;var me=OnlineMe;
        DrawPerspectiveMatch(room,me,room.players[1-room.side],OnlineManagementAllowed(room,me)&&OnlineConnectionFresh(),room.remaining);
    }
    FormationForecast.Plan FormationDragProbePlan(Vector2 target,bool editable=true)
    {Event.current=new Event{type=EventType.Repaint,mousePosition=target};return OnlineFormationForecast(OnlineMe,editable);}
    void StartFormationDragProbe(string area,int slot,Vector2 target)
    {
        ResetOnlineDrag();arenaPointer.Reset();Vector2 start=FormationDragProbePoint(area,slot);
        OnlineRequire(area=="bench"?arena.HitBench(start)==slot:arena.HitCell(start)==slot+28,"formation drag probe starts on the exact source cell");
        FormationDragProbeInput(EventType.MouseDown,start);
        OnlineRequire(onlineDragArea==area&&onlineDragSlot==slot&&!onlineDragging,"real mouse down captures the drag candidate without activating it");
        FormationDragProbeInput(EventType.MouseDrag,target);
        OnlineRequire(onlineDragging&&onlineDragArea==area&&onlineDragSlot==slot,"real mouse drag activates the captured source");
    }
    void CompareFormationDragProbe(FormationForecast.Plan actual,string area,int slot,string targetArea,int targetSlot,string label)
    {
        var me=OnlineMe;var boardIds=new string[28];var benchIds=new string[9];
        var members=new DigimonBuildCatalog.Member[28];var reserves=new DigimonBuildCatalog.Member[9];
        foreach(var unit in me.board){boardIds[unit.slot]=unit.id;members[unit.slot]=BuildMember(unit);}
        foreach(var unit in me.bench){benchIds[unit.slot]=unit.id;reserves[unit.slot]=BuildMember(unit);}
        var expected=FormationForecast.Preview(boardIds,benchIds,area=="board",slot,targetArea=="board",targetSlot,OnlineFormationLimit(me),members,reserves);
        OnlineRequire(actual!=null,label+" forecast exists");
        OnlineRequire(actual.allowed==expected.allowed&&actual.message==expected.message&&actual.Changed==expected.Changed,label+" result and message match the actual drag source");
        OnlineRequire(actual.board.SequenceEqual(expected.board)&&actual.bench.SequenceEqual(expected.bench),label+" exact slot swap matches the actual drag source");
        OnlineRequire(actual.traits.Length==expected.traits.Length,label+" trait count matches");
        for(int i=0;i<expected.traits.Length;i++)
            OnlineRequire(ReferenceEquals(actual.traits[i].trait,expected.traits[i].trait)&&actual.traits[i].before==expected.traits[i].before&&actual.traits[i].after==expected.traits[i].after,label+" trait order and gear-aware counts match");
    }
    // Call from an existing portable validation callback inside OnGUI, after
    // arena/catalog/styles exist. Events reach the production arena/drag path.
    // No release event is sent, so these probes cannot issue a server mutation.
    // Invoke only between gestures; pointer/equipment capture is reset on exit.
    void ValidateOnlineFormationDragInGUI()
    {
        OnlineRequire(Event.current!=null&&arena!=null,"formation drag validation runs in GUI with an existing arena");
        OnlineRequire(!arenaPointer.HasPress&&onlineEquipmentDrag.Slot<0&&!onlineDragging&&onlineDragSlot<0,"formation drag validation starts between pointer and equipment gestures");
        var savedState=state;var savedEvent=new Event(Event.current);bool savedEnabled=GUI.enabled;
        int savedArt=artPack,savedSlot=selectedSlot,savedItem=onlineItem,savedScoutSide=scoutSide;
        int savedItemRevision=onlineItemRevision,savedItemPage=onlineItemPage;
        var savedInventory=OnlineMe==null||OnlineMe.inventory==null?new int[0]:OnlineMe.inventory;
        string savedArea=selectedArea;bool savedBusy=busy,savedError=connectionError,savedRecovery=recoveringLogin,savedReport=onlineReport;
        float savedReceived=receivedAt;int savedDragSlot=onlineDragSlot,savedDragStar=onlineDragStar;
        string savedDragArea=onlineDragArea,savedDragId=onlineDragId;bool savedDragging=onlineDragging;Vector2 savedDragStart=onlineDragStart;
        try
        {
            var me=new Player{level=3,inventory=new int[0],board=new[]{
                new Unit{id="agumon",star=1,slot=3,items=new[]{0}},new Unit{id="greymon",star=1,slot=4,items=new int[0]}},
                bench=new[]{new Unit{id="gabumon",star=1,slot=0,items=new[]{18}},new Unit{id="patamon",star=1,slot=1,items=new int[0]}}};
            var enemy=new Player{level=3,inventory=new int[0],board=new Unit[0],bench=new Unit[0]};
            var room=new Room{id="formation-drag-probe",round=4,side=0,phase="prepare",remaining=30,players=new[]{me,enemy}};
            state=new State{room=room,combatActions=1};artPack=0;busy=connectionError=recoveringLogin=false;receivedAt=Time.unscaledTime;
            onlineItem=-1;scoutSide=-1;selectedSlot=-1;selectedArea="";GUI.enabled=true;onlineEquipmentDrag.Reset();arenaPointer.Reset();
            Vector2 empty=FormationDragProbePoint("board",10),occupied=FormationDragProbePoint("board",3),seat=FormationDragProbePoint("bench",0);

            StartFormationDragProbe("bench",0,empty);
            OnlineRequire(selectedSlot==-1,"dragging a new unit does not require an earlier click selection");
            CompareFormationDragProbe(FormationDragProbePlan(empty),"bench",0,"board",10,"unselected bench deployment");

            selectedArea="board";selectedSlot=3;StartFormationDragProbe("bench",0,occupied);
            OnlineRequire(selectedArea=="board"&&selectedSlot==3,"previous selection remains different from the dragged source");
            CompareFormationDragProbe(FormationDragProbePlan(occupied),"bench",0,"board",3,"drag overrides an old board selection");

            selectedArea="bench";selectedSlot=1;StartFormationDragProbe("board",3,seat);
            CompareFormationDragProbe(FormationDragProbePlan(seat),"board",3,"bench",0,"board withdrawal overrides an old bench selection");
            Vector2 boardSwap=FormationDragProbePoint("board",4);StartFormationDragProbe("board",3,boardSwap);
            CompareFormationDragProbe(FormationDragProbePlan(boardSwap),"board",3,"board",4,"board to board drag");
            Vector2 benchSwap=FormationDragProbePoint("bench",1);StartFormationDragProbe("bench",0,benchSwap);
            CompareFormationDragProbe(FormationDragProbePlan(benchSwap),"bench",0,"bench",1,"bench to bench drag");

            ResetOnlineDrag();selectedArea="bench";selectedSlot=0;
            CompareFormationDragProbe(FormationDragProbePlan(empty),"bench",0,"board",10,"ordinary click selection stays unchanged");
            FormationDragProbeInput(EventType.MouseDown,seat);
            FormationDragProbeInput(EventType.MouseDrag,seat+Vector2.right*4);
            OnlineRequire(!onlineDragging,"movement below the drag threshold retains click semantics");
            CompareFormationDragProbe(FormationDragProbePlan(empty),"bench",0,"board",10,"pending mouse press retains click selection");

            selectedArea="";selectedSlot=-1;StartFormationDragProbe("bench",0,empty);
            var original=me.bench[0];me.bench[0]=new Unit{id="palmon",star=1,slot=0,items=new int[0]};
            OnlineRequire(FormationDragProbePlan(empty)==null,"replaced drag species cannot fall back to another selected unit");
            me.bench[0]=new Unit{id=original.id,star=2,slot=0,items=original.items};
            OnlineRequire(FormationDragProbePlan(empty)==null,"upgraded drag source invalidates the captured star");
            me.bench[0]=new Unit{id=original.id,star=1,slot=0,items=new[]{17}};
            CompareFormationDragProbe(FormationDragProbePlan(empty),"bench",0,"board",10,"equal source identity accepts fresh snapshot and current emblem");

            GUI.enabled=false;OnlineRequire(FormationDragProbePlan(empty)==null,"modal-disabled GUI suppresses a held forecast");GUI.enabled=true;
            OnlineRequire(FormationDragProbePlan(empty,false)==null,"caller permission cannot be bypassed by an active drag");
            busy=true;OnlineRequire(FormationDragProbePlan(empty)==null,"pending server action suppresses drag forecast");busy=false;
            onlineItem=0;OnlineRequire(FormationDragProbePlan(empty)==null,"equipment selection suppresses unit forecast");onlineItem=-1;
            scoutSide=1;OnlineRequire(FormationDragProbePlan(empty)==null,"scouting never exposes an own drag forecast");scoutSide=-1;
            room.phase="battle";OnlineRequire(FormationDragProbePlan(empty)==null,"battle bench management cannot show a preparation forecast");room.phase="finished";
            OnlineRequire(FormationDragProbePlan(empty)==null,"finished match suppresses forecast");room.phase="prepare";
            me.ready=true;OnlineRequire(FormationDragProbePlan(empty,OnlineManagementAllowed(room,me)&&OnlineConnectionFresh())==null,"ready state removes caller edit permission");me.ready=false;
            receivedAt=Time.unscaledTime-7;OnlineRequire(FormationDragProbePlan(empty,OnlineManagementAllowed(room,me)&&OnlineConnectionFresh())==null,"stale snapshot removes caller edit permission");receivedAt=Time.unscaledTime;
            OnlineRequire(FormationDragProbePlan(arena.Project(TacticalArena.CellWorld(3,1)))==null,"enemy field is not a placement target");
            OnlineRequire(FormationDragProbePlan(new Vector2(-100,-100))==null,"outside field is not a placement target");

            me.level=2;var full=FormationDragProbePlan(empty);OnlineRequire(full!=null&&!full.allowed,"real drag still respects an empty target capacity limit");
            me.inventory=new[]{26};CompareFormationDragProbe(FormationDragProbePlan(empty),"bench",0,"board",10,"owned capacity changes apply immediately");
            OnlineRequire(FormationDragProbePlan(empty).allowed,"capacity artifact allows the same held deployment");
            FormationDragProbeInput(EventType.KeyDown,empty,KeyCode.Escape);
            OnlineRequire(!onlineDragging&&selectedSlot<0&&FormationDragProbePlan(empty)==null,"Escape through the arena path cancels drag and preview");
            OnlineRequire(!busy,"formation probe never sends a move or purchase");
            Debug.Log("FORMATION DRAG PREVIEW: production input source, swaps and permission boundaries verified");
        }
        finally
        {
            state=savedState;Event.current=savedEvent;GUI.enabled=savedEnabled;artPack=savedArt;selectedArea=savedArea;selectedSlot=savedSlot;
            onlineItem=savedItem;onlineItemRevision=savedItemRevision;onlineItemPage=savedItemPage;
            onlineInventoryView.Sync(savedInventory,ref onlineItemPage);
            scoutSide=savedScoutSide;busy=savedBusy;connectionError=savedError;recoveringLogin=savedRecovery;onlineReport=savedReport;receivedAt=savedReceived;
            onlineDragSlot=savedDragSlot;onlineDragStar=savedDragStar;onlineDragArea=savedDragArea;onlineDragId=savedDragId;onlineDragging=savedDragging;onlineDragStart=savedDragStart;
            arenaPointer.Reset();onlineEquipmentDrag.Reset();
        }
    }
}
#endif
