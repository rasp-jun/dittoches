#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    void ValidateScoutingInGUI()
    {
        var oldBoard=(Unit[])board.Clone();var oldBench=(Unit[])bench.Clone();var oldFighters=fighters.ToArray();
        int oldRound=round,bank=gold;string oldOpponent=currentOpponent;bool oldBattle=battling;
        try
        {
            round=20;battling=false;skillDetailUnit=null;traitFocus=null;showRecipeGuide=false;
            var random=UnityEngine.Random.state;var formation=RivalFormation(NextRivalIndex);
            Require(random.Equals(UnityEngine.Random.state),"scouting never consumes shop/combat random state");
            Require(ReferenceEquals(formation,RivalFormation(NextRivalIndex)),"same round scouting reuses actual rival formation");
            fighters.Clear();SetupRivalTeam(2+(round-4)/7,1);InitializeBuildBonuses();
            foreach(var f in fighters)
            {
                int index=(3-(int)f.pos.y)*7+6-(int)f.pos.x;var planned=formation[index];
                Require(planned!=null&&planned.def.id==f.unit.def.id&&planned.star==f.unit.star&&planned.items.SequenceEqual(f.unit.items),"scouted slots/stars/equipment match actual mirrored battle lineup");
                Require(!ReferenceEquals(planned,f.unit),"battle units cannot mutate cached scout units");
            }
            selectedBoard=3;selectedItem=0;ShowRivalBoard(NextRivalIndex);
            Require(scoutedRival==NextRivalIndex&&selectedBoard<0&&selectedItem<0&&!equipmentDrag.Dragging&&!draggingUnit,"scout entry cancels own management gesture");
            var first=scoutBoard.First(u=>u!=null);var live=fighters.First(f=>f.unit.def.id==first.def.id);
            Require(Mathf.Abs(InspectStats(first).health-live.maxHp)<.05f,"scout stats use rival synergy and difficulty scaling");
            int slot=Array.IndexOf(scoutBoard,first);var pointer=FormationPieceRect(first,TacticalArena.CellWorld(slot%7,slot/7+4),76).center;
            EquipmentGesture(EventType.MouseDown,pointer,button:1);
            Require(inspectedUnit!=null&&scoutBoard.Contains(inspectedUnit)&&selectedBoard<0,"right click in scout field inspects opponent without selecting own unit");
            var ownSignature=string.Join(",",board.Concat(bench).Select(u=>u==null?"-":u.def.id+u.star));
            foreach(KeyCode key in new[]{KeyCode.W,KeyCode.E})EquipmentGesture(EventType.KeyDown,pointer,key);
            Require(gold==bank&&ownSignature==string.Join(",",board.Concat(bench).Select(u=>u==null?"-":u.def.id+u.star)),"scout W/E cannot move or sell own unit in the same slot");
            EquipmentGesture(EventType.KeyDown,pointer,KeyCode.Space);
            Require(scoutedRival<0&&!battling&&Event.current.type==EventType.Used,"Space returns home without starting battle");
            EquipmentGesture(EventType.KeyDown,pointer,KeyCode.R);Require(scoutedRival==0,"R starts scouting first rival");
            EquipmentGesture(EventType.KeyDown,pointer,KeyCode.Q);Require(scoutedRival<0,"Q cycles back home");
            EquipmentGesture(EventType.KeyDown,pointer,KeyCode.Q);Require(scoutedRival==6,"Q wraps from home to last rival");
            EquipmentGesture(EventType.KeyDown,pointer,KeyCode.R);Require(scoutedRival<0,"R wraps from last rival to home");
            battling=true;ShowRivalBoard(2);Require(scoutedRival==2,"scouting available during ongoing battle");ReturnToOwnField();battling=false;
            Array.Clear(board,0,board.Length);Array.Clear(bench,0,bench.Length);board[3]=new Unit(RosterById["agumon"]);selectedBoard=-1;
            pointer=FormationPieceRect(board[3],TacticalArena.CellWorld(3,4),76).center;
            EquipmentGesture(EventType.KeyDown,pointer,KeyCode.W);Require(board[3]==null&&bench[0]!=null,"W benches hovered unit without prior click or sale");
            selectedBench=0;
            foreach(KeyCode key in new[]{KeyCode.E,KeyCode.W})EquipmentGesture(EventType.KeyDown,pointer,key);
            Require(bench[0]!=null&&board.All(u=>u==null)&&gold==bank,"empty field W/E never acts on previously selected bench unit");
            EquipmentGesture(EventType.KeyDown,Vector2.zero,KeyCode.W);Require(bench[0]==null&&board[0]!=null&&gold==bank,"W outside field deploys explicitly selected bench unit");
            for(int i=0;i<bench.Length;i++)bench[i]=new Unit(RosterById["gabumon"]);
            selectedBoard=0;selectedBench=-1;EquipmentGesture(EventType.KeyDown,Vector2.zero,KeyCode.W);
            Require(board[0]!=null&&bench.All(u=>u!=null)&&placementNotice.Contains("대기석이 가득"),"full bench preserves units and explains swap alternative");
            Array.Clear(board,0,board.Length);for(int i=0;i<FormationLimit;i++)board[i]=new Unit(RosterById["agumon"]);
            selectedBoard=-1;selectedBench=0;EquipmentGesture(EventType.KeyDown,Vector2.zero,KeyCode.W);
            Require(bench[0]!=null&&board.Count(u=>u!=null)==FormationLimit&&placementNotice.Contains("배치 한도"),"full formation preserves units and explains cap");
            battling=true;EquipmentGesture(EventType.KeyDown,Vector2.zero,KeyCode.W);
            Require(bench[0]!=null&&placementNotice.Contains("준비 단계"),"battle W explains why deployment is unavailable");battling=false;
            ShowRivalBoard(1);var prior=scoutBoard.First(u=>u!=null);round++;ShowRivalBoard(1);
            Require(!scoutBoard.Contains(prior),"new round refreshes cached opponent formation");
        }
        finally
        {
            ReturnToOwnField();Array.Copy(oldBoard,board,board.Length);Array.Copy(oldBench,bench,bench.Length);
            fighters.Clear();fighters.AddRange(oldFighters);round=oldRound;battling=oldBattle;gold=bank;currentOpponent=oldOpponent;
        }
    }
}
#endif
