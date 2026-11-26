using System.Collections.Generic;
using UnityEngine;

// Visual events only. Damage, shields, death and board positions remain simulation-owned.
public struct CombatHitPresentation
{
    public const float Duration=.22f,FlashDuration=.18f;
    public float time,availableAt,occurredAt;
    public Vector3 direction;
    public static CombatHitPresentation Empty {get{return new CombatHitPresentation{time=-100};}}
    public float Flash(float now)
    {return now>=time&&time>=0?Mathf.Clamp01(1-(now-time)/FlashDuration):0;}
    public static Vector3 Horizontal(Vector3 value)
    {
        if(float.IsNaN(value.x)||float.IsNaN(value.z)||float.IsInfinity(value.x)||float.IsInfinity(value.z))return Vector3.zero;
        value.y=0;return value.sqrMagnitude>.000001f?value.normalized:Vector3.zero;
    }
}

// Ordered on receipt; binary-search sampling supports seeks and repeated repaints.
public sealed class CombatHitTrack
{
    readonly List<CombatHitPresentation> events=new List<CombatHitPresentation>();
    public int Count {get{return events.Count;}}
    public void Add(float time,Vector3 direction,float availableAt=0,float occurredAt=float.NaN)
    {
        if(time<0||float.IsNaN(time)||float.IsInfinity(time))return;
        direction=CombatHitPresentation.Horizontal(direction);
        if(float.IsNaN(occurredAt))occurredAt=time;
        int lo=0,hi=events.Count;
        while(lo<hi){int mid=(lo+hi)/2;if(events[mid].time<=time)lo=mid+1;else hi=mid;}
        if(lo>0&&events[lo-1].time==time&&events[lo-1].occurredAt==occurredAt&&events[lo-1].availableAt==availableAt)
        {var same=events[lo-1];same.direction+=direction;events[lo-1]=same;return;}
        events.Insert(lo,new CombatHitPresentation{time=time,direction=direction,availableAt=availableAt,occurredAt=occurredAt});
    }
    public CombatHitPresentation At(float time,float since=-1)
    {
        if(float.IsNaN(time)||float.IsInfinity(time))return CombatHitPresentation.Empty;
        int lo=0,hi=events.Count;
        while(lo<hi){int mid=(lo+hi)/2;if(events[mid].time<=time)lo=mid+1;else hi=mid;}
        while(lo>0)
        {
            var hit=events[--lo];
            if(time-hit.time>=CombatHitPresentation.Duration)break;
            if(hit.availableAt>time||hit.occurredAt<since)continue;
            hit.direction=CombatHitPresentation.Horizontal(hit.direction);return hit;
        }
        return CombatHitPresentation.Empty;
    }
    public void Prune(float now)
    {
        int count=0;while(count<events.Count&&events[count].time<now-CombatHitPresentation.Duration)count++;
        if(count>0)events.RemoveRange(0,count);
    }
}
