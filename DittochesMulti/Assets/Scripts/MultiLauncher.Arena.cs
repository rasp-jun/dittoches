using UnityEngine;

public sealed partial class MultiLauncher
{
    TacticalArena arena;
    readonly ArenaPointer arenaPointer=new ArenaPointer();
    void DrawPerspectiveMatch(Room room, Player me, Player enemy, bool editable, float remaining)
    {
        if(arena==null)arena=new TacticalArena(TacticalArena.MultiViewport,artPack==0);
        bool combat=room.phase=="battle";
        Event e=Event.current;
        int hit=arena.HitCell(e.mousePosition),seat=arena.HitBench(e.mousePosition);
        if(GUI.enabled&&((e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape)
            ||(e.type==EventType.MouseDown&&e.button==1&&TacticalArena.MultiViewport.Contains(e.mousePosition))))
        {selectedSlot=-1;selectedArea="";arenaPointer.Reset();e.Use();}
        arenaPointer.Update(seat>=0?56+seat:hit,e.type==EventType.MouseDown&&e.button==0,
            e.type==EventType.MouseUp&&e.button==0,false,!editable||busy||!GUI.enabled);
        if(Event.current.type==EventType.Repaint)
        {
            arena.BeginFrame(selectedArea=="board"&&selectedSlot>=0?selectedSlot+28:-1,editable?hit:-1,selectedArea=="bench"?selectedSlot:-1,editable&&selectedSlot>=0);
            if(editable&&GUI.enabled&&!busy&&selectedSlot>=0)
                arena.HighlightDestination(hit,seat,seat>=0||(hit>=28&&(selectedArea=="board"||At(me.board,hit-28)!=null||me.board.Length<me.level)));
            if(combat&&room.frames!=null&&room.frames.Length>0)
            {
                float progress=CombatProgress(room,remaining);
                int frame=Mathf.FloorToInt(progress),next=Mathf.Min(frame+1,room.frames.Length-1);
                foreach(Fighter f in room.frames[frame].units)
                {
                    if(f.hp<=0)continue;
                    Fighter to=System.Array.Find(room.frames[next].units,u=>u.key==f.key)??f;
                    float x=Mathf.Lerp(f.x,to.x,progress-frame),y=Mathf.Lerp(f.y,to.y,progress-frame);
                    if(room.side==1){x=6-x;y=7-y;}
                    arena.SetActor("fighter:"+f.key,TacticalArena.CellWorld(x,y),LobbyPortrait(f.id),f.side==room.side?Color.green:Color.red);
                    if(artPack==0)
                    {
                        float playhead=CombatTime(room,remaining),age=ActiveCastAge(room,f.key,playhead);
                        Fighter aim=System.Array.Find(room.frames[frame].units,u=>u.key==f.target);
                        Vector3 facing=new Vector3(to.x-f.x,0,-(to.y-f.y));
                        if(facing.sqrMagnitude<.001f&&aim!=null)facing=new Vector3(aim.x-f.x,0,-(aim.y-f.y));
                        if(room.side==1)facing=-facing;
                        arena.FaceActor("fighter:"+f.key,facing);
                        arena.PoseDigimon("fighter:"+f.key,f.id,Vector2.Distance(new Vector2(f.x,f.y),new Vector2(to.x,to.y))*5,
                            (to.x-f.x)*(room.side==1?-1:1),playhead,age,Mathf.Max(0,.35f-(playhead-f.attackAt)),0,f.star);
                        arena.DecorateActor("fighter:"+f.key,false,0,Mathf.Clamp01(1-(playhead-f.hitAt)/.18f));
                    }
                }
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
                        if(artPack==0){arena.FaceActor(key,own?Vector3.forward:Vector3.back);arena.PoseDigimon(key,unit.id,0,0,Time.unscaledTime,star:unit.star);}
                    }
                }
            }
            for(int i=0;i<9;i++){Unit unit=At(me.bench,i);if(unit!=null){arena.SetActor("bench:"+i,TacticalArena.BenchWorld(i),LobbyPortrait(unit.id),gold,.85f);if(artPack==0)arena.PoseDigimon("bench:"+i,unit.id,0,0,Time.unscaledTime,star:unit.star);}}
            if(combat&&artPack==0)DrawMatchSkills(room,remaining);
            arena.Render();
        }
        GUI.DrawTexture(TacticalArena.MultiViewport,arena.Texture,ScaleMode.StretchToFill,false);
        if(!combat)GUI.Label(new Rect(350,143,880,28),me.ready?"준비 완료 · 상대 테이머를 기다리는 중":"유닛 선택 → 이동할 칸 선택  ·  ESC / 우클릭 취소",centered);
        if(combat)DrawCombat(room,remaining);
        else for(int row=0;row<8;row++)for(int col=0;col<7;col++)
        {
            bool own=row>=4;int slot=own?(row-4)*7+col:(3-row)*7+6-col;
            Unit unit=At(own?me.board:enemy.board,slot);
            if(unit!=null)GUI.Label(arena.LabelRect(TacticalArena.CellWorld(col,row),3),new string('★',unit.star),centered);
            if(own&&editable&&!busy&&GUI.enabled&&arenaPointer.Released==row*7+col&&Event.current.type==EventType.MouseUp&&Event.current.button==0)
            {ClickSlot("board",slot,unit);Event.current.Use();}
        }
        for(int i=0;i<9;i++)
        {
            Rect rect=arena.BenchRect(i);Unit unit=At(me.bench,i);
            GUI.Label(new Rect(rect.x,rect.yMax+2,rect.width,20),unit==null?(i+1).ToString():new string('★',unit.star),centered);
            if(editable&&!busy&&GUI.enabled&&arenaPointer.Released==56+i&&Event.current.type==EventType.MouseUp&&Event.current.button==0){ClickSlot("bench",i,unit);Event.current.Use();}
        }
    }
}
