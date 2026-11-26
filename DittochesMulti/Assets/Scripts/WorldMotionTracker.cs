using System;
using System.Collections.Generic;

/// <summary>Presentation velocity in arena units, sampled once per distinct displayed time.</summary>
public sealed class WorldMotionTracker<TKey>
{
    public struct Motion
    {
        public float x,z,speed;
        public bool reset,repeated;
    }
    sealed class Sample
    {
        public float x,z,time;
        public Motion motion;
    }
    readonly Dictionary<TKey,Sample> samples=new Dictionary<TKey,Sample>();
    object context;int version;bool hasContext;
    // Native presentation is capped at seven grid units/sec. Even the longest
    // CellWorld shear is below 1.592, so twelve arena units/sec covers real travel.
    const float MaximumSpeed=12,MaximumGap=.25f;
    public int Count {get{return samples.Count;}}
    public void EnsureContext(object value,int revision)
    {
        if(hasContext&&Equals(context,value)&&version==revision)return;
        samples.Clear();context=value;version=revision;hasContext=true;
    }
    static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
    static Motion Rebase(Sample sample,float x,float z,float time)
    {
        sample.x=x;sample.z=z;sample.time=time;sample.motion=new Motion{reset=true};return sample.motion;
    }
    public Motion At(TKey key,float x,float z,float time)
    {
        if(!Finite(x)||!Finite(z)||!Finite(time))
        {samples.Remove(key);return new Motion{reset=true};}
        Sample sample;
        if(!samples.TryGetValue(key,out sample))
        {sample=new Sample();samples.Add(key,sample);return Rebase(sample,x,z,time);}
        float dt=time-sample.time,dx=x-sample.x,dz=z-sample.z;
        if(dt==0)
        {
            // Repainting cannot erase movement or apply it twice. A corrected
            // snapshot at the same timestamp instead establishes a new origin.
            if(dx==0&&dz==0){Motion repeat=sample.motion;repeat.repeated=true;return repeat;}
            return Rebase(sample,x,z,time);
        }
        if(dt<0||dt>MaximumGap)return Rebase(sample,x,z,time);
        float distanceSquared=dx*dx+dz*dz,allowed=MaximumSpeed*dt+.002f;
        if(!Finite(distanceSquared)||distanceSquared>allowed*allowed)return Rebase(sample,x,z,time);
        var motion=new Motion{x=dx/dt,z=dz/dt,speed=(float)Math.Sqrt(distanceSquared)/dt};
        sample.x=x;sample.z=z;sample.time=time;sample.motion=motion;return motion;
    }
}
