#if DITTOCHES_PORTABLE_PREVIEW
using System;
using UnityEngine;

public sealed partial class TacticalArena
{
    public void ValidateFinisherFraming(Action<bool,string> require)
    {
        Color[] crown=ImpactCorona().colors;
        require(crown[0].a==0&&crown[1].a==1&&crown[2].a==0,"impact crown retains transparent inner and outer edges in the shipped player");
        Color[] feather=FinisherFeather().colors;
        require(feather[0].a<.3f&&feather[12].a>.99f,"luminous feathers retain their tapered alpha after mesh ownership");
        foreach(float z in new[]{-3.8f,0,1.8f,3.8f})foreach(int choice in new[]{1,2})
        {
            Vector3 origin=new Vector3(0,.23f,-z),impact=new Vector3(0,.23f,z);
            float initialScale=0;
            foreach(float age in new[]{.05f,.3f,.55f,.9f,1.13f,1.6f,2.2f})
            {
                BeginFrame(-1,-1,-1,false);DrawFinisher(choice,origin,impact,age);EndEffects();
                if(age==.05f)initialScale=finisherHeight;
                require(finisherHeight==initialScale&&finisherHeight>0&&finisherHeight<=1,"finisher framing remains fixed over the shot");
                bool checkedCore=false;
                for(int i=0;i<effectCursor;i++)
                {
                    var slot=effectPool[i];
                    bool meteor=choice==1&&age<FinisherImpactTime(1)&&(slot.mesh==meteorCore||slot.mesh==meteorCrust)&&slot.material==roundedSkillMaterial;
                    bool gate=choice==2&&slot.mesh==cube;
                    if(!meteor&&!gate)continue;checkedCore=true;
                    foreach(Vector3 vertex in slot.mesh.vertices)
                    {
                        Vector3 p=camera.WorldToViewportPoint(slot.transform.TransformPoint(vertex));
                        require(p.z>0&&p.y>.015f&&p.y<.98f,"meteor or gate core remains inside field camera: effect="+choice+" z="+z+" age="+age+" viewport="+p);
                    }
                }
                if(choice==2||age<FinisherImpactTime(1))require(checkedCore,"framing checks actual drawn geometry");
            }
            BeginFrame(-1,-1,-1,false);DrawFinisher(choice,origin,impact,0);float scale=finisherHeight;
            BeginFrame(-1,-1,-1,false);DrawFinisher(choice,origin+Vector3.right*2,impact+Vector3.forward*2,.2f);
            require(finisherSource==origin&&finisherDestination==impact&&finisherHeight==scale,"moving target cannot drag or resize an active finisher");
        }
        BeginFrame(-1,-1,-1,false);
        foreach(float invalid in new[]{-1,float.NaN,float.PositiveInfinity,FinisherDuration})DrawFinisher(1,Vector3.zero,Vector3.zero,invalid);
        EndEffects();require(effectCursor==0,"invalid or expired finisher cannot leave effects visible");
        ValidateFinisherReplayResources(require);
        ValidateMovingFinishers(require);
        foreach(int choice in new[]{0,1,2})foreach(float z in new[]{-3.8f,3.8f})
        {
            Vector3 from=new Vector3(-1,.23f,-z),to=new Vector3(1,.30f,z);
            require((FlightPoint(from,to,choice,0,1)-(from+Vector3.up*.65f)).sqrMagnitude<.00001f,"every projectile launches at the winning tamer");
            require((FlightPoint(from,to,choice,1,1)-(to+Vector3.up*.55f)).sqrMagnitude<.00001f,"every projectile arrives at the losing tamer");
            Vector3 midpoint=FlightPoint(from,to,choice,.5f,1);
            require(Mathf.Abs(midpoint.z)<.001f&&midpoint.y>1,"flight visibly crosses the field on an elevated arc in either direction");
        }
        UnityEngine.Debug.Log("FINISHER FRAMING: near / middle / far meteor and holy gate silhouettes verified");
    }
    void ValidateFinisherReplayResources(Action<bool,string> require)
    {
        int warmResources=0,warmPool=0,peak=0;
        for(int pass=0;pass<2;pass++)
        {
            for(int choice=0;choice<3;choice++)for(int frame=0;frame<=Mathf.CeilToInt(FinisherDuration*30);frame++)
            {
                BeginFrame(-1,-1,-1,false);DrawFinisher(choice,new Vector3(0,.23f,-3.8f),new Vector3(0,.23f,3.8f),frame/30f);EndEffects();
                peak=Mathf.Max(peak,effectCursor);
                require(SkillEffectsHealthy(),"finisher replay has finite bounded geometry and no stale pooled effects");
                require(effectCursor<128,"finisher remains below 128 active effects throughout its complete timeline");
            }
            if(pass==0){warmResources=OwnedResourceCount;warmPool=effectPool.Count;}
            else require(OwnedResourceCount==warmResources&&effectPool.Count==warmPool,"replaying all finishers reuses the warmed mesh/material/effect pool");
        }
        Debug.Log("FINISHER REPLAY: peak="+peak+" / 128, pooled="+warmPool+", owned="+warmResources);
    }
}
#endif
