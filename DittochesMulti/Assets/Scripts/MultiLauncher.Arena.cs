using UnityEngine;

public sealed partial class MultiLauncher
{
    int DrawOnlineCombatSignals(Room room,float remaining)
    {
        var current=VisibleCombatFrame(room,remaining);if(current==null)return 0;
        float progress=CombatProgress(room,remaining),fraction=progress-Mathf.Floor(progress),time=CombatTime(room,remaining);
        int next=Mathf.Min(Mathf.FloorToInt(progress)+1,room.frames.Length-1),count=0;
        foreach(var f in current.units)
        {
            var signal=OnlineCombatSignal(f,time);if(signal.kind==0)continue;
            var to=System.Array.Find(room.frames[next].units,u=>u.key==f.key)??f;
            float x=Mathf.Lerp(f.x,to.x,fraction),y=Mathf.Lerp(f.y,to.y,fraction);
            if(room.side==1){x=6-x;y=7-y;}
            arena.DrawCombatSignal(TacticalArena.CellWorld(x,y),signal,TacticalArena.DigimonHeadHeight(f.id,f.star));count++;
        }
        return count;
    }

    TacticalArena arena;
    readonly ArenaPointer arenaPointer=new ArenaPointer();
    void DrawPerspectiveMatch(Room room, Player me, Player enemy, bool editable, float remaining)
    {
        if(arena==null)arena=new TacticalArena(TacticalArena.MultiViewport,artPack==0);
        bool combat=room.phase=="battle";
        Event e=Event.current;
        int hit=arena.HitCell(e.mousePosition),seat=arena.HitBench(e.mousePosition);
        HandleOnlineEquipmentDrag(room,me,editable);
        HandleOnlineEquipmentClick(editable);
        if(artPack==0)HandleOnlineDrag(room,me,editable,hit,seat);
        if(artPack==0&&GUI.enabled&&!busy&&e.type==EventType.MouseDown&&e.button==1&&hit>=28&&room.phase!="finished")
        {selectedSlot=-1;selectedArea="";ResetEquipmentSelection();arenaPointer.Reset();MoveOnlineTamer(hit);e.Use();}
        if(GUI.enabled&&((e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape)
            ||(e.type==EventType.MouseDown&&e.button==1&&TacticalArena.MultiViewport.Contains(e.mousePosition))))
        {selectedSlot=-1;selectedArea="";ResetEquipmentSelection();arenaPointer.Reset();e.Use();}
        arenaPointer.Update(seat>=0?56+seat:hit,e.type==EventType.MouseDown&&e.button==0,
            e.type==EventType.MouseUp&&e.button==0,onlineDragging,!editable||busy||!GUI.enabled);
        if(Event.current.type==EventType.Repaint)
        {
            if(artPack==0)arena.SetField(me.field);
            arena.BeginFrame(OnlinePreparationCell(room),editable&&!combat?hit:-1,selectedArea=="bench"?selectedSlot:-1,editable&&selectedSlot>=0);
            Unit selected=SelectedOnlineUnit(me);
            if(artPack==0&&room.phase=="prepare"&&selected!=null)
            {
                int rangeCell=selectedArea=="board"?selectedSlot+28:-1;
                if(editable&&GUI.enabled&&!busy&&seat<0&&hit>=28&&(selectedArea=="board"||At(me.board,hit-28)!=null||me.board.Length<OnlineFormationLimit(me)))rangeCell=hit;
                if(rangeCell>=28)arena.HighlightHexAttackRange(rangeCell%7,rangeCell/7,DigimonSkillCatalog.Find(selected.id).attackRange);
            }
            if(editable&&GUI.enabled&&!busy&&selectedSlot>=0&&(!combat||selectedArea=="bench"))
                arena.HighlightDestination(hit,seat,seat>=0||(!combat&&hit>=28&&(selectedArea=="board"||At(me.board,hit-28)!=null||me.board.Length<OnlineFormationLimit(me))));
            if(combat&&room.frames!=null&&room.frames.Length>0)
            {
                float progress=CombatProgress(room,remaining);
                int frame=Mathf.FloorToInt(progress),next=Mathf.Min(frame+1,room.frames.Length-1);
                foreach(Fighter f in room.frames[frame].units)RenderOnlineFighter(room,f,frame,next,progress,remaining);
            }
            else
            {
                for(int row=0;row<8;row++)for(int col=0;col<7;col++)
                {
                    bool own=row>=4;int slot=own?(row-4)*7+col:(3-row)*7+6-col;
                    Unit unit=At(own?me.board:enemy.board,slot);
                    if(unit!=null)
                    {
                        string key=(own?"own:":"enemy:")+slot;
                        arena.SetActor(key,TacticalArena.CellWorld(col,row),LobbyPortrait(unit.id),own?Color.green:Color.red);
                        if(artPack==0){arena.FaceActor(key,own?Vector3.forward:Vector3.back);arena.PoseDigimon(key,unit.id,0,0,Time.unscaledTime,star:unit.star);arena.DecorateActor(key,false,own?OnlinePromotion(unit,"board"):0);}
                    }
                }
            }
            for(int i=0;i<9;i++){Unit unit=At(me.bench,i);if(unit!=null){arena.SetActor("bench:"+i,TacticalArena.BenchWorld(i),LobbyPortrait(unit.id),gold,.85f);if(artPack==0){arena.PoseDigimon("bench:"+i,unit.id,0,0,Time.unscaledTime,star:unit.star);arena.DecorateActor("bench:"+i,false,combat?0:OnlinePromotion(unit,"bench"));}}}
            if(combat&&artPack==0){DrawMatchSkills(room,remaining);DrawOnlineCombatSignals(room,remaining);}
            if(artPack==0)DrawMatchTamers(room,me,enemy);
            arena.Render();
        }
        GUI.DrawTexture(TacticalArena.MultiViewport,arena.Texture,ScaleMode.StretchToFill,false);
        if(artPack==0)
        {
            string rangeHint=OnlineRangeHint(room,me,remaining);
            if(rangeHint.Length>0)GUI.Label(new Rect(350,176,880,26),rangeHint,centered);
            if(combat)GUI.Label(new Rect(350,143,880,28),OnlineCombatSummary(room,remaining),centered);
        }
        if(!combat)GUI.Label(new Rect(350,143,880,28),me.ready?"준비 완료 · 상대 테이머를 기다리는 중":onlineItem>=0?"장비를 받을 아군 선택 · ESC / 우클릭 취소":"유닛 선택 → 이동할 칸 선택  ·  우클릭: 테이머 이동  ·  ESC: 취소",centered);
        if(combat)DrawCombat(room,remaining);
        else for(int row=0;row<8;row++)for(int col=0;col<7;col++)
        {
            bool own=row>=4;int slot=own?(row-4)*7+col:(3-row)*7+6-col;
            Unit unit=At(own?me.board:enemy.board,slot);
            if(unit!=null){GUI.Label(arena.LabelRect(TacticalArena.CellWorld(col,row),3),new string('★',unit.star),centered);DrawUnitItemBadges(unit,TacticalArena.CellWorld(col,row));}
            if(own&&editable&&!busy&&GUI.enabled&&arenaPointer.Released==row*7+col&&Event.current.type==EventType.MouseUp&&Event.current.button==0)
            {ClickSlot("board",slot,unit);Event.current.Use();}
        }
        for(int i=0;i<9;i++)
        {
            Rect rect=arena.BenchRect(i);Unit unit=At(me.bench,i);
            GUI.Label(new Rect(rect.x,rect.yMax+2,rect.width,20),unit==null?(i+1).ToString():new string('★',unit.star),centered);
            if(unit!=null)DrawUnitItemBadges(unit,TacticalArena.BenchWorld(i));
            if(editable&&!busy&&GUI.enabled&&arenaPointer.Released==56+i&&Event.current.type==EventType.MouseUp&&Event.current.button==0){ClickSlot("bench",i,unit);Event.current.Use();}
        }
    }
}
