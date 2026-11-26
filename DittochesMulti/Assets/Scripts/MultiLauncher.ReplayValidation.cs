#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class MultiLauncher
{
    // Retain the previous algorithms only as differential test oracles.
    float ReferenceCombatProgress(Room room,float remaining)
    {
        if(room.frames==null||room.frames.Length<2)return 0;
        int last=room.frames.Length-1;
        if(room.frames[last].time<=0)return Mathf.Clamp01(1-remaining/8f)*last;
        float time=CombatTime(room,remaining);
        for(int i=0;i<last;i++)if(time<room.frames[i+1].time)
            return i+Mathf.InverseLerp(room.frames[i].time,room.frames[i+1].time,time);
        return last;
    }
    static Fighter ReferenceBasicTarget(Room room,Fighter fighter,int frame)
    {
        int origin=frame;
        while(origin>0)
        {
            var previous=Array.Find(room.frames[origin-1].units,u=>u.key==fighter.key);
            if(previous==null||previous.attacks!=fighter.attacks)break;
            origin--;
        }
        var source=Array.Find(room.frames[origin].units,u=>u.key==fighter.key);
        return source==null?null:Array.Find(room.frames[origin].units,u=>u.key==source.target);
    }
    static bool ReferenceUpcomingAttack(Room room,Fighter current,int frame,float time,out float cooldown,out Fighter target)
    {
        cooldown=-1;target=null;
        if(current.hp<=0||current.stun>0)return false;
        float window=BasicAttackPresentation.Windup(current.id);
        for(int i=frame+1;i<room.frames.Length&&room.frames[i].time<=time+window+.21f;i++)
        {
            var next=Array.Find(room.frames[i].units,u=>u.key==current.key);
            if(next==null||next.hp<=0)return false;
            if(next.attacks<=current.attacks)continue;
            float until=next.attackAt-time;
            if(until<=0||until>window)return false;
            cooldown=until;target=Array.Find(room.frames[frame].units,u=>u.key==next.target&&u.hp>0);
            return target!=null;
        }
        return false;
    }
    void CheckReplayProgress(Room room,float remaining,string context)
    {
        float expected=ReferenceCombatProgress(room,remaining),actual=CombatProgress(room,remaining);
        OnlineRequire(Mathf.Abs(expected-actual)<.00001f,"replay frame lookup matches previous interpolation / "+context);
        int last=room.frames==null?0:Mathf.Max(0,room.frames.Length-1);
        OnlineRequire(actual>=0&&actual<=last,"replay frame lookup remains inside snapshot / "+context);
    }
    void CheckReplayRoom(Room room,string context)
    {
        var times=new List<float>{-1,0,.00001f,room.battleDuration,room.battleDuration+1};
        if(room.frames!=null)foreach(var frame in room.frames)
        {
            times.Add(frame.time-.00001f);times.Add(frame.time);times.Add(frame.time+.00001f);
        }
        if(room.frames!=null)for(int i=1;i<room.frames.Length;i++)times.Add((room.frames[i-1].time+room.frames[i].time)*.5f);
        times.Sort();float duration=room.battleDuration>0?room.battleDuration:8;
        for(int direction=0;direction<2;direction++)for(int i=0;i<times.Count;i++)
            CheckReplayProgress(room,duration-times[direction==0?i:times.Count-1-i],context);
        if(room.frames==null)return;
        for(int frame=0;frame<room.frames.Length;frame++)
        {
            var snapshot=room.frames[frame];
            OnlineRequire(FindFighter(snapshot,int.MinValue)==null,"missing replay fighter returns no target / "+context);
            foreach(var fighter in snapshot.units)
            {
                int key=fighter.key;
                OnlineRequire(ReferenceEquals(FindFighter(snapshot,key),Array.Find(snapshot.units,u=>u.key==key)),"replay lookup retains exact snapshot object / "+context);
                OnlineRequire(ReferenceEquals(FindFighter(snapshot,key,true),Array.Find(snapshot.units,u=>u.key==key&&u.hp>0)),"replay lookup preserves living-target predicate / "+context);
                OnlineRequire(ReferenceEquals(BasicAttackTarget(room,fighter,frame),ReferenceBasicTarget(room,fighter,frame)),"basic effect keeps original attack target / "+context);
                for(int point=0;point<3;point++)
                {
                    float time=snapshot.time+point*.075f,expectedCooldown,actualCooldown;Fighter expectedTarget,actualTarget;
                    bool expected=ReferenceUpcomingAttack(room,fighter,frame,time,out expectedCooldown,out expectedTarget);
                    bool actual=UpcomingBasicAttack(room,fighter,frame,time,out actualCooldown,out actualTarget);
                    OnlineRequire(actual==expected&&actualCooldown==expectedCooldown&&ReferenceEquals(actualTarget,expectedTarget),"attack anticipation keeps cooldown and target / "+context);
                }
                if(fighter.hp<=0)
                {
                    int deathFrame=frame;
                    while(deathFrame>0)
                    {
                        var prior=Array.Find(room.frames[deathFrame-1].units,u=>u.key==key);
                        if(prior==null||prior.hp>0)break;deathFrame--;
                    }
                    OnlineRequire(FighterDeathFrame(room.frames,frame,key)==deathFrame,"death animation retains original start frame / "+context);
                }
            }
        }
    }
    void ValidateReplayLookup(Room actualReplay=null)
    {
        if(actualReplay!=null)
        {
            CheckReplayRoom(actualReplay,"server fixture");
            MeasureReplayLookup(actualReplay);return;
        }
        OnlineRequire(FindFighter(null,1)==null&&FindFighter(new Frame(),1)==null,"empty replay snapshot has no fighter");
        Fighter dead=new Fighter{key=9,id="agumon",hp=0},alive=new Fighter{key=9,id="agumon",hp=1};
        var duplicate=new Frame{units=new[]{dead,alive}};
        OnlineRequire(ReferenceEquals(FindFighter(duplicate,9),dead)&&ReferenceEquals(FindFighter(duplicate,9,true),alive),"replay target lookup preserves first matching predicate");
        CheckReplayRoom(new Room(),"missing frames");
        CheckReplayRoom(new Room{frames=new Frame[0]},"empty frames");
        CheckReplayRoom(new Room{frames=new[]{new Frame{time=3,units=new Fighter[0]}}},"single frame");
        var room=new Room{id="lookup-regression",round=4,battleDuration=2,frames=new Frame[9]};
        float[] times={0,.1f,.2f,.2f,.37f,.6f,.8f,1.1f,1.5f};
        for(int i=0;i<times.Length;i++)
        {
            var a=new Fighter{key=1,id="agumon",hp=i>=6?0:100,attacks=i<2?0:i<5?1:2,attackAt=i<2?-10:i<5?.15f:.55f,target=i<4?2:3,stun=i==1?.1f:0};
            var b=new Fighter{key=2,id="gabumon",hp=i>=4?0:100,x=i*.1f};
            var c=new Fighter{key=3,id="patamon",hp=100,y=i*.2f};
            room.frames[i]=new Frame{time=times[i],units=i==3?new[]{c,b}:new[]{a,b,c}};
        }
        CheckReplayRoom(room,"irregular timestamps and repeated boundary");
        var original=room.frames;room.frames=new[]{new Frame{time=.25f,units=new[]{alive}},new Frame{time=.9f,units=new[]{dead}},new Frame{time=1.7f,units=new[]{dead}}};
        CheckReplayRoom(room,"same room and round with replaced frames");
        room.frames[1].time=1.2f;room.frames[1].units=new[]{new Fighter{key=9,hp=25,id="agumon"}};
        CheckReplayRoom(room,"existing snapshot corrected in place");
        room.frames=original;CheckReplayRoom(room,"return to earlier replay after replacement");
        foreach(var frame in room.frames)frame.time=0;
        CheckReplayRoom(room,"legacy server without timestamps");
    }
    void MeasureReplayLookup(Room room)
    {
        if(room.frames==null||room.frames.Length<2)return;
        const int count=32768;var remaining=new float[257];var keys=new int[257];
        for(int i=0;i<remaining.Length;i++)
        {
            remaining[i]=room.battleDuration*(1-i/256f);
            var frame=room.frames[i%room.frames.Length];keys[i]=frame.units.Length==0?int.MinValue:frame.units[i%frame.units.Length].key;
        }
        var before=new double[3];var after=new double[3];double expectedSum=0,actualSum=0;
        for(int warm=0;warm<remaining.Length;warm++){ReferenceCombatProgress(room,remaining[warm]);CombatProgress(room,remaining[warm]);}
        for(int trial=0;trial<3;trial++)for(int pass=0;pass<2;pass++)
        {
            bool baseline=((trial+pass)%2)==0;double sum=0;var watch=System.Diagnostics.Stopwatch.StartNew();
            for(int i=0;i<count;i++)
            {
                int sample=i%remaining.Length,key=keys[sample];float progress=baseline?ReferenceCombatProgress(room,remaining[sample]):CombatProgress(room,remaining[sample]);
                var frame=room.frames[Mathf.FloorToInt(progress)];
                var fighter=baseline?ReferenceReplayFighter(frame,key):FindFighter(frame,key);
                sum+=progress+(fighter==null?0:fighter.hp);
            }
            watch.Stop();if(baseline){before[trial]=watch.Elapsed.TotalMilliseconds;expectedSum=sum;}else{after[trial]=watch.Elapsed.TotalMilliseconds;actualSum=sum;}
        }
        Array.Sort(before);Array.Sort(after);
        OnlineRequire(expectedSum==actualSum,"replay lookup benchmark consumes identical snapshots");
        Debug.Log("REPLAY LOOKUP BENCHMARK: "+count+" progress and fighter queries, "+room.frames.Length+" frames; median "+before[1].ToString("0.###")+" -> "+after[1].ToString("0.###")+" ms (CPU lookup batch, not total FPS)");
    }
    // Keep the reference closure outside the optimized timing path.
    static Fighter ReferenceReplayFighter(Frame frame,int key)
    {return Array.Find(frame.units,u=>u.key==key);}
}
#endif
