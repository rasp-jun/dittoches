using System;
using UnityEngine;

public sealed partial class MultiLauncher
{
    Fighter BasicAttackTarget(Room room,Fighter fighter,int frame)
    {
        int originFrame=frame;
        while(originFrame>0)
        {
            var previous=FindFighter(room.frames[originFrame-1],fighter.key);
            if(previous==null||previous.attacks!=fighter.attacks)break;
            originFrame--;
        }
        var source=FindFighter(room.frames[originFrame],fighter.key);
        return source==null?null:FindFighter(room.frames[originFrame],source.target);
    }
    bool UpcomingBasicAttack(Room room,Fighter current,int frame,float time,out float cooldown,out Fighter target)
    {
        cooldown=-1;target=null;
        if(current.hp<=0||current.stun>0)return false;
        float window=BasicAttackPresentation.Windup(current.id);
        for(int i=frame+1;i<room.frames.Length&&room.frames[i].time<=time+window+.21f;i++)
        {
            var next=FindFighter(room.frames[i],current.key);
            if(next==null||next.hp<=0)return false;
            if(next.attacks<=current.attacks)continue;
            float until=next.attackAt-time;
            if(until<=0||until>window)return false;
            cooldown=until;target=FindFighter(room.frames[frame],next.target,true);
            return target!=null;
        }
        return false;
    }
    void DrawOnlineBasicAttacks(Room room,float remaining)
    {
        if(room.frames==null||room.frames.Length==0)return;
        float time=CombatTime(room,remaining);int frame=Mathf.FloorToInt(CombatProgress(room,remaining));
        foreach(var fighter in room.frames[frame].units)
        {
            float age=time-fighter.attackAt;
            if(fighter.attacks<=0||age<0||age>=BasicAttackPresentation.Lifetime(fighter.id))continue;
            // Target and endpoint come from the event's snapshot, not a subsequent retarget.
            var target=BasicAttackTarget(room,fighter,frame);
            if(target==null)continue;
            float x=target.x,y=target.y;if(BattleSide(room)==1){x=6-x;y=7-y;}
            float height=CombatContactPresentation.Height(target.id,target.star),surface=arena.ContactSurface("fighter:"+target.key,height,CombatContactPresentation.Surface(target.id,target.star));
            arena.DrawBasicAttack("fighter:"+fighter.key,fighter.id,TacticalArena.CellWorld(x,y)+Vector3.up*height,age,fighter.attacks,fighter.star,surface);
        }
    }
#if DITTOCHES_PORTABLE_PREVIEW
    void ValidateBasicReplay()
    {
        var waiting=new Fighter{key=1,id="agumon",hp=100,attacks=0,attackAt=-10,target=2};
        var released=new Fighter{key=1,id="agumon",hp=100,attacks=1,attackAt=.15f,target=2};
        var retargeted=new Fighter{key=1,id="agumon",hp=100,attacks=1,attackAt=.15f,target=3};
        var enemy=new Fighter{key=2,id="gabumon",hp=100,x=3,y=2};
        var room=new Room{frames=new[]{new Frame{time=0,units=new[]{waiting,enemy}},new Frame{time=.2f,units=new[]{released,enemy}},new Frame{time=.4f,units=new[]{retargeted,enemy,new Fighter{key=3,id="patamon",hp=100}}}}};
        float cooldown;Fighter target;
        OnlineRequire(UpcomingBasicAttack(room,waiting,0,0,out cooldown,out target)&&Mathf.Abs(cooldown-.15f)<.001f&&target.key==2,"replay prepares next basic attack without altering current HP or attack count");
        OnlineRequire(waiting.attacks==0&&enemy.hp==100,"anticipation remains presentation only");
        OnlineRequire(!UpcomingBasicAttack(room,released,1,.2f,out cooldown,out target),"old snapshot does not repeat windup");
        OnlineRequire(BasicAttackTarget(room,retargeted,2).key==2,"released effect retains original target after retargeting");
        waiting.stun=.3f;OnlineRequire(!UpcomingBasicAttack(room,waiting,0,0,out cooldown,out target),"stunned attacker cannot anticipate");
        waiting.stun=0;waiting.hp=0;OnlineRequire(!UpcomingBasicAttack(room,waiting,0,0,out cooldown,out target),"dead attacker cannot anticipate");
        waiting.hp=100;released.attackAt=.8f;OnlineRequire(!UpcomingBasicAttack(room,waiting,0,0,out cooldown,out target),"distant future attack stays hidden");
    }
#endif
}
