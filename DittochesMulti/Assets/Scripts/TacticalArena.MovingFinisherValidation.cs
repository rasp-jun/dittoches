#if DITTOCHES_PORTABLE_PREVIEW
using System;
using UnityEngine;

public sealed partial class TacticalArena
{
    void ValidateMovingFinishers(Action<bool,string> require)
    {
        for(int choice=0;choice<3;choice++)foreach(int direction in new[]{1,-1})
        {
            Vector3 own=new Vector3(-2,.19f,-3.2f*direction),rival=new Vector3(2,.19f,3.2f*direction),launch=Vector3.zero,land=Vector3.zero;
            float hit=FinisherImpactTime(choice);
            for(int frame=0;frame<96;frame++)
            {
                float age=frame/30f;
                Vector3 source=own+Vector3.right*Mathf.Sin(age)*1.2f,target=rival+Vector3.left*Mathf.Sin(age*1.6f)*2;
                BeginFrame(-1,-1,-1,false);DrawFinisher(choice,source,target,age,true);EndEffects();
                if(age>=FinisherLaunchTime&&age-1/30f<FinisherLaunchTime)launch=finisherSource;
                if(age>=FinisherLaunchTime)require(finisherSource==launch,"moving winner cannot drag a released projectile");
                if(age<hit)require(finisherDestination==target,"flight tracks the moving loser in either field orientation");
                else if(age-1/30f<hit+.00001f)land=finisherDestination;
                else require(finisherDestination==land,"impact remains at arrival position while tamers continue moving");
                require(SkillEffectsHealthy()&&effectCursor<128,"moving finisher preserves valid bounded pooled geometry");
            }
        }
    }
}
#endif
