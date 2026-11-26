#if DITTOCHES_PORTABLE_PREVIEW || WORLDMOTION_CHECK
using System;

/// <summary>Numerical checks also run without an Editor or graphics player.</summary>
public static class WorldMotionValidation
{
    struct Point {public float x,z;public Point(float x,float z){this.x=x;this.z=z;}}
    static Point Cell(float x,float y)
    {
        int a=Math.Max(0,Math.Min(7,(int)Math.Floor(y))),b=Math.Min(7,a+1);
        float t=Math.Max(0,Math.Min(1,y-a)),offset=(a%2)*.65f*(1-t)+(b%2)*.65f*t;
        return new Point((x-3)*1.3f+offset-.325f,(3.5f-y)*1.125f);
    }
    static float Distance(Point a,Point b){float x=b.x-a.x,z=b.z-a.z;return (float)Math.Sqrt(x*x+z*z);}
    static bool Near(float a,float b,float tolerance=.0002f){return Math.Abs(a-b)<tolerance;}
    public static int Validate(Action<bool,string> require)
    {
        int checks=0;Action<bool,string> check=(ok,message)=>{checks++;require(ok,"world movement: "+message);};
        var tracker=new WorldMotionTracker<int>();tracker.EnsureContext("match",1);
        var first=tracker.At(0,0,0,0);check(first.reset&&first.speed==0,"first point does not invent travel");
        var moved=tracker.At(0,.13f,0,.1f);check(Near(moved.speed,1.3f)&&Near(moved.x,1.3f)&&moved.z==0,"speed is measured in displayed arena units");
        for(int repaint=0;repaint<5;repaint++)
        {
            var duplicate=tracker.At(0,.13f,0,.1f);
            check(duplicate.repeated&&duplicate.speed==moved.speed&&duplicate.x==moved.x&&tracker.Count==1,"repaint preserves the previous velocity without new state");
        }
        var corrected=tracker.At(0,.16f,0,.1f);check(corrected.reset&&corrected.speed==0,"same-time position correction resets the origin");
        check(Near(tracker.At(0,.29f,0,.2f).speed,1.3f),"motion resumes from corrected origin");
        check(tracker.At(0,2,0,.21f).reset,"teleport is not fast locomotion");
        check(Near(tracker.At(0,2.13f,0,.31f).speed,1.3f),"normal motion resumes after teleport");
        check(tracker.At(0,3,0,.8f).reset,"hidden or paused views do not catch up old travel");
        check(tracker.At(0,0,0,0).reset,"replay rewind discards the previous direction");
        check(tracker.At(0,float.NaN,0,.1f).reset&&tracker.Count==0,"invalid position drops poisoned history");
        check(tracker.At(0,0,0,.2f).reset,"valid position after invalid sample starts fresh");
        tracker.EnsureContext("match",1);check(tracker.Count==1,"ordinary polls keep the current match history");
        tracker.EnsureContext("match",2);check(tracker.Count==0,"new round or viewing side resets history");
        tracker.At(0,0,0,0);tracker.EnsureContext("other match",2);check(tracker.Count==0,"new room releases the prior match samples");
        object battle=new object();tracker.EnsureContext(battle,1);tracker.At(0,0,0,0);
        tracker.EnsureContext(new object(),1);check(tracker.Count==0,"restarting the same solo round releases old fighters");

        // Deliberately cross an odd-row seam halfway through each path. Both
        // perspectives must cover equal distances with opposite world directions.
        for(int path=0;path<3;path++)foreach(int fps in new[]{30,60,144})
        {
            var own=new WorldMotionTracker<int>();var other=new WorldMotionTracker<int>();
            Point previous=Cell(2,2.5f);float length=0,integrated=0,oldGridLength=0;
            own.At(0,previous.x,previous.z,0);other.At(0,-previous.x,-previous.z,0);
            float lastTime=0;
            for(int frame=1;frame<=fps;frame++)
            {
                float t=frame/(float)fps,x=2+(path==1?0:path==0?1:.8f)*t,y=2.5f+(path==0?0:1)*t;
                Point point=Cell(x,y),opposite=Cell(6-x,7-y);
                var a=own.At(0,point.x,point.z,t);var b=other.At(0,opposite.x,opposite.z,t);
                check(!a.reset&&!b.reset&&Near(a.speed,b.speed)&&Near(a.x,-b.x)&&Near(a.z,-b.z),"both seats preserve speed and mirror direction / "+path+" / "+fps);
                float distance=Distance(previous,point);length+=distance;integrated+=a.speed*(t-lastTime);
                check(Near(a.speed*(t-lastTime),distance),"gait input follows actual displayed distance / "+path+" / "+fps);
                var repeat=own.At(0,point.x,point.z,t);check(repeat.speed==a.speed,"duplicate GUI sample does not zero a moving unit");
                oldGridLength+=(path==0||path==1?1:(float)Math.Sqrt(1+.8*.8))*(t-lastTime);
                previous=point;lastTime=t;
            }
            check(Near(length,integrated,.00002f),"integrated gait distance equals rendered path");
            check(Math.Abs(length-oldGridLength)>.02f,"fixture catches the former grid/world unit mismatch");
            check(own.At(0,previous.x,previous.z,1+1f/fps).speed==0,"stationary display immediately stops generating steps");
        }
        // The replay interpolator already uses server timestamps. Measuring its
        // displayed output must therefore work equally with short final snapshots.
        float[] times={0,.2f,.25f,.65f,.8f,1};
        foreach(int fps in new[]{30,60,144})
        {
            var replay=new WorldMotionTracker<int>();Point start=Cell(2,2.2f);replay.At(0,start.x,start.z,0);
            for(int frame=1;frame<=fps;frame++)
            {
                float time=frame/(float)fps;int interval=0;
                while(interval<times.Length-2&&times[interval+1]<=time)interval++;
                float duration=times[interval+1]-times[interval],u=(time-times[interval])/duration;
                float from=2+1.5f*times[interval],to=2+1.5f*times[interval+1];Point point=Cell(from+(to-from)*u,2.2f);
                var motion=replay.At(0,point.x,point.z,time);
                check(Near(motion.speed,1.95f,.002f),"irregular online snapshots retain the real velocity / "+fps+" / "+frame);
            }
        }
        return checks;
    }
}
#endif
