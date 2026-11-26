using System.Collections.Generic;
using UnityEngine;

// A sound is observed once at its visual impact. Never queue a backlog on reconnect/seek.
public sealed class CombatAudioTimeline
{
    readonly Dictionary<long,float> seen=new Dictionary<long,float>();
    readonly List<long> expired=new List<long>();
    bool active;float time,floor,pruned;
    public void Clear(){active=false;seen.Clear();expired.Clear();}
    public bool Begin(float now)
    {
        bool reset=!active||now<time-.001f||now-time>.5f;
        if(reset){seen.Clear();floor=!active&&now<=.25f?-.0001f:now+.0001f;pruned=now;}
        active=true;time=now;
        if(now-pruned>=.5f){expired.Clear();foreach(var pair in seen)if(pair.Value<now-.5f)expired.Add(pair.Key);foreach(long key in expired)seen.Remove(key);pruned=now;}
        return reset;
    }
    public bool Observe(long key,float impact)
    {
        if(!active||float.IsNaN(impact)||float.IsInfinity(impact)||impact<floor||time<impact||time-impact>.12f||seen.ContainsKey(key))return false;
        seen.Add(key,impact);return true;
    }
    public int Count {get{return seen.Count;}}
}
