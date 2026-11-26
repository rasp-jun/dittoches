using System;
using System.Linq;
using UnityEngine;

public sealed class StarMergeArrival
{
    public string id;
    public int star,slot;
    public bool board;
    public float started;
    public Vector3[] origins;
    public float Duration {get{return star>=3?1.45f:1.1f;}}
    public bool Active(float now){return now>=started&&now<started+Duration;}
    public static StarMergeArrival Create(PurchaseForecast forecast,Func<PurchaseForecast.Source,Vector3> position,Vector3 incoming,bool battle,float now)
    {
        if(forecast==null||!forecast.available||forecast.result==null||forecast.result.star<2||battle&&forecast.result.board)return null;
        var origins=forecast.sources.Where(s=>!s.recipient&&s.star>0).Select(s=>position(s)+Vector3.up*(TacticalArena.DigimonHeadHeight(forecast.result.id,s.star)*(s.board?1:.85f)*.42f)).ToList();
        if(forecast.sources.Any(s=>s.recipient&&s.star>0))origins.Add(incoming);
        if(origins.Count==0)return null;
        return new StarMergeArrival{id=forecast.result.id,star=forecast.result.star,slot=forecast.result.slot,board=forecast.result.board,started=now,origins=origins.Take(8).ToArray()};
    }
    public static Vector3 Curve(Vector3 from,Vector3 to,float progress)
    {
        float t=Mathf.Clamp01(progress),smooth=t*t*(3-2*t);
        return Vector3.Lerp(from,to,smooth)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*(.45f+Mathf.Min(1.1f,Vector3.Distance(from,to)*.14f)));
    }
}

public sealed partial class TacticalArena
{
    public void DrawMergeArrival(StarMergeArrival flight,Vector3 target,float now)
    {
        if(flight==null||!flight.Active(now))return;
        float progress=(now-flight.started)/flight.Duration,fade=1-Mathf.Clamp01((progress-.86f)/.14f);
        if(fade<=.001f)return;
        Color tint=StarUpgradeUI.Tint(flight.star);
        for(int i=0;i<flight.origins.Length;i++)
        {
            float t=Mathf.Clamp01((progress-i*.02f)/.8f);if(t<=0||t>=1)continue;
            Vector3 from=flight.origins[i],head=StarMergeArrival.Curve(from,target,t);
            Color color=Color.Lerp(tint,Color.white,.2f*(1-t));
            Orb(head,(flight.star>=3?.18f:.14f)*fade,color);
            for(int tail=0;tail<4;tail++)
            {
                float a=Mathf.Max(0,t-tail*.045f),b=Mathf.Max(0,t-(tail+1)*.045f);
                Stroke(StarMergeArrival.Curve(from,target,a),StarMergeArrival.Curve(from,target,b),(.045f-tail*.008f)*fade,color*(1-tail*.13f));
            }
            if(t<.4f)Halo(from-Vector3.up*.08f,.2f+t*.25f,color*(1-t/.4f));
        }
        if(progress>.55f)
        {
            float spread=(progress-.55f)/.45f;
            Halo(target,.18f+spread*.75f,tint*fade,true);
            Halo(target-Vector3.up*.2f,.14f+spread*.56f,tint*fade);
        }
    }
}
