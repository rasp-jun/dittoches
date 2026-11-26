using UnityEngine;

public sealed partial class MultiLauncher
{
    readonly WorldMotionTracker<int> faithfulWorldMotion=new WorldMotionTracker<int>();
    static Vector3 OnlineMotionWorld(Room room,float x,float y)
    {return BattleSide(room)==1?TacticalArena.CellWorld(6-x,7-y):TacticalArena.CellWorld(x,y);}
    private void RenderOnlineFighter(Room room,Fighter f,int frame,int next,float progress,float remaining)
    {
        float death=0;
        if(f.hp<=0)
        {
            if(artPack!=0||!FaithfulModelData.Available(f.id))return;
            int deathFrame=FighterDeathFrame(room.frames,frame,f.key);
            float since=CombatTime(room,remaining)-room.frames[deathFrame].time;
            if(since>=2.2f)return;death=Mathf.Max(.0001f,since/2.2f);
        }
        Fighter to=FindFighter(room.frames[next],f.key)??f;
        float x=Mathf.Lerp(f.x,to.x,progress-frame),y=Mathf.Lerp(f.y,to.y,progress-frame);
        if(BattleSide(room)==1){x=6-x;y=7-y;}
        bool focused=f.hp>0&&f.side==room.side&&selectedArea=="board"&&f.slot==selectedSlot;
        if(artPack==0&&focused)arena.HighlightHexAttackRange(x,y,f.attackRange>0?f.attackRange:DigimonSkillCatalog.Find(f.id).attackRange);
        string actorKey="fighter:"+f.key;
        Vector3 world=TacticalArena.CellWorld(x,y);
        arena.SetActor(actorKey,world,LobbyPortrait(f.id),f.side==room.side?Color.green:Color.red);
        if(artPack==0)
        {
            float playhead=CombatTime(room,remaining),age=ActiveCastAge(room,f,playhead);
            faithfulWorldMotion.EnsureContext(room.id,room.round*2+room.side);
            var movement=faithfulWorldMotion.At(f.key,world.x,world.z,playhead);
            Fighter aim=FindFighter(room.frames[frame],f.target);
            if(f.attacks>0&&playhead>=f.attackAt&&playhead-f.attackAt<BasicAttackPresentation.Recovery(f.id))aim=BasicAttackTarget(room,f,frame)??aim;
            float cooldown;Fighter upcoming;
            bool winding=UpcomingBasicAttack(room,f,frame,playhead,out cooldown,out upcoming);
            if(winding)aim=upcoming;
            Vector3 facing=movement.speed>.025f?new Vector3(movement.x,0,movement.z):OnlineMotionWorld(room,to.x,to.y)-world;
            if(aim!=null&&(winding||age>=0||playhead-f.attackAt<.35f||facing.sqrMagnitude<.001f))facing=OnlineMotionWorld(room,aim.x,aim.y)-world;
            arena.FaceActor(actorKey,facing);
            var hit=OnlineHitReaction(room,f,playhead);
            arena.CombatMotion(actorKey,f.attacks,cooldown,winding,hit.time,hit.direction);
            arena.DecorateActor(actorKey,focused,0,hit.Flash(playhead));
            arena.PoseDigimon(actorKey,f.id,movement.speed,movement.x,playhead,age,Mathf.Max(0,.35f-(playhead-f.attackAt)),death,f.star);
        }
    }
}
