using UnityEngine;

public sealed partial class MultiLauncher
{
    TacticalArena arena;
    void DrawPerspectiveMatch(Room room, Player me, Player enemy, bool editable, float remaining)
    {
        if(arena==null)arena=new TacticalArena(TacticalArena.MultiViewport);
        bool combat=room.phase=="battle";
        int hit=arena.HitCell(Event.current.mousePosition);
        if(Event.current.type==EventType.Repaint)
        {
            arena.BeginFrame(selectedArea=="board"&&selectedSlot>=0?selectedSlot+28:-1,editable?hit:-1,selectedArea=="bench"?selectedSlot:-1,editable&&selectedSlot>=0);
            if(combat&&room.frames!=null&&room.frames.Length>0)
            {
                float progress=Mathf.Clamp01(1-remaining/8f)*(room.frames.Length-1);
                int frame=Mathf.FloorToInt(progress),next=Mathf.Min(frame+1,room.frames.Length-1);
                foreach(Fighter f in room.frames[frame].units)
                {
                    if(f.hp<=0)continue;
                    Fighter to=System.Array.Find(room.frames[next].units,u=>u.key==f.key)??f;
                    float x=Mathf.Lerp(f.x,to.x,progress-frame),y=Mathf.Lerp(f.y,to.y,progress-frame);
                    if(room.side==1){x=6-x;y=7-y;}
                    arena.SetActor("fighter:"+f.key,TacticalArena.CellWorld(x,y),LobbyPortrait(f.id),f.side==room.side?Color.green:Color.red);
                }
            }
            else
            {
                for(int row=0;row<8;row++)for(int col=0;col<7;col++)
                {
                    bool own=row>=4;int slot=own?(row-4)*7+col:(3-row)*7+6-col;
                    Unit unit=At(own?me.board:enemy.board,slot);
                    if(unit!=null)arena.SetActor((own?"own:":"enemy:")+slot,TacticalArena.CellWorld(col,row),LobbyPortrait(unit.id),own?Color.green:Color.red);
                }
            }
            for(int i=0;i<9;i++){Unit unit=At(me.bench,i);if(unit!=null)arena.SetActor("bench:"+i,TacticalArena.BenchWorld(i),LobbyPortrait(unit.id),gold,.85f);}
            arena.Render();
        }
        GUI.DrawTexture(TacticalArena.MultiViewport,arena.Texture,ScaleMode.StretchToFill,false);
        if(combat)DrawCombat(room,remaining);
        else for(int row=0;row<8;row++)for(int col=0;col<7;col++)
        {
            bool own=row>=4;int slot=own?(row-4)*7+col:(3-row)*7+6-col;
            Unit unit=At(own?me.board:enemy.board,slot);
            if(unit!=null)GUI.Label(arena.LabelRect(TacticalArena.CellWorld(col,row),3),new string('★',unit.star),centered);
            if(own&&editable&&!busy&&GUI.enabled&&hit==row*7+col&&Event.current.type==EventType.MouseUp&&Event.current.button==0)
            {ClickSlot("board",slot,unit);Event.current.Use();}
        }
        for(int i=0;i<9;i++)
        {
            Rect rect=arena.BenchRect(i);Unit unit=At(me.bench,i);
            GUI.Label(new Rect(rect.x,rect.yMax+2,rect.width,20),unit==null?(i+1).ToString():new string('★',unit.star),centered);
            if(editable&&!busy&&GUI.enabled&&arena.HitBench(Event.current.mousePosition)==i&&Event.current.type==EventType.MouseUp&&Event.current.button==0){ClickSlot("bench",i,unit);Event.current.Use();}
        }
    }
}
