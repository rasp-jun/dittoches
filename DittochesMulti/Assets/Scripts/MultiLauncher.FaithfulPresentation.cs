using UnityEngine;

public sealed partial class MultiLauncher
{
    private void RenderOnlineFighter(Room room,Fighter f,int frame,int next,float progress,float remaining)
    {
        float death=0;
        if(f.hp<=0)
        {
            if(artPack!=0||!FaithfulModelData.Available(f.id))return;
            int deathFrame=frame;
            while(deathFrame>0)
            {
                Fighter prior=System.Array.Find(room.frames[deathFrame-1].units,u=>u.key==f.key);
                if(prior==null||prior.hp>0)break;deathFrame--;
            }
            float since=CombatTime(room,remaining)-room.frames[deathFrame].time;
            if(since>=2.2f)return;death=Mathf.Max(.0001f,since/2.2f);
        }
        Fighter to=System.Array.Find(room.frames[next].units,u=>u.key==f.key)??f;
        float x=Mathf.Lerp(f.x,to.x,progress-frame),y=Mathf.Lerp(f.y,to.y,progress-frame);
        if(room.side==1){x=6-x;y=7-y;}
        bool focused=f.hp>0&&f.side==room.side&&selectedArea=="board"&&f.slot==selectedSlot;
        if(artPack==0&&focused)arena.HighlightHexAttackRange(x,y,f.attackRange>0?f.attackRange:DigimonSkillCatalog.Find(f.id).attackRange);
        arena.SetActor("fighter:"+f.key,TacticalArena.CellWorld(x,y),LobbyPortrait(f.id),f.side==room.side?Color.green:Color.red);
        if(artPack==0)
        {
            float playhead=CombatTime(room,remaining),age=ActiveCastAge(room,f.key,playhead);
            Fighter aim=System.Array.Find(room.frames[frame].units,u=>u.key==f.target);
            Vector3 facing=new Vector3(to.x-f.x,0,-(to.y-f.y));
            if(facing.sqrMagnitude<.001f&&aim!=null)facing=new Vector3(aim.x-f.x,0,-(aim.y-f.y));
            if(room.side==1)facing=-facing;
            arena.FaceActor("fighter:"+f.key,facing);
            arena.CombatMotion("fighter:"+f.key,f.attacks,-1,false,f.hitAt);
            arena.DecorateActor("fighter:"+f.key,focused,0,Mathf.Clamp01(1-(playhead-f.hitAt)/.18f));
            arena.PoseDigimon("fighter:"+f.key,f.id,Vector2.Distance(new Vector2(f.x,f.y),new Vector2(to.x,to.y))*5,
                (to.x-f.x)*(room.side==1?-1:1),playhead,age,Mathf.Max(0,.35f-(playhead-f.attackAt)),death,f.star);
        }
    }
}
