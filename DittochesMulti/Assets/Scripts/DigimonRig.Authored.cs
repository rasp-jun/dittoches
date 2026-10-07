using UnityEngine;

public sealed partial class DigimonRig
{
    Vector3[] posePositions,secondPositions,transitionPositions;
    Quaternion[] poseRotations,secondRotations,transitionRotations;
    string activeClip;
    float transitionAt;
    float authoredAttackAge=100,lastAttackFlash;
    void PoseAuthored(Vector3 position,float speed,float time,DigimonSkillCatalog.Entry skill,float castAge,float attack,float death,int star)
    {
        if(posePositions==null)
        {
            posePositions=new Vector3[bones.Length];secondPositions=new Vector3[bones.Length];transitionPositions=new Vector3[bones.Length];
            poseRotations=new Quaternion[bones.Length];secondRotations=new Quaternion[bones.Length];transitionRotations=new Quaternion[bones.Length];
        }
        float dt=lastTime<0?0:Mathf.Clamp(time-lastTime,0,.1f);
        bool reset=lastTime<0||time<lastTime;
        bool inspection=!string.IsNullOrEmpty(DigimonModelLibrary.InspectionClip);
        if(reset){phase=0;moveBlend=0;}
        Vector3 delta=reset?Vector3.zero:position-previousPosition;delta.y=0;
        if(delta.sqrMagnitude>.00001f&&!facingSet)Face(delta);
        float actual=dt>0?delta.magnitude/dt:0;
        float movement=Mathf.Clamp01(Mathf.Max(speed,actual)/.10f);
        moveBlend=reset?movement:Mathf.Lerp(moveBlend,movement,1-Mathf.Exp(-dt*12));
        // Inspection rotation must remain responsive when the motion clock is paused.
        yaw=reset||inspection?desiredYaw:Mathf.LerpAngle(yaw,desiredYaw,1-Mathf.Exp(-dt*12));facingSet=false;
        float modelScale=.66f*(1+.055f*(star-1));
        float attackDuration=model.authored.clips["Attack"].duration;
        bool casting=castAge>=0&&skill!=null&&castAge<skill.Duration;
        if(reset)authoredAttackAge=attack>0?(1-Mathf.Clamp01(attack/.35f))*attackDuration:attackDuration;
        else if(!inspection)
        {
            if(attack>0&&(lastAttackFlash<=0||attack>lastAttackFlash+.0001f))authoredAttackAge=0;
            else authoredAttackAge+=dt;
        }
        if(casting||death>0)authoredAttackAge=attackDuration;
        lastAttackFlash=attack;
        string clip=death>0?"Defeat":casting?"PepperBreath":authoredAttackAge<attackDuration?"Attack":"Locomotion";
        float age=death>0?death*1.2f:clip=="PepperBreath"?castAge:clip=="Attack"?authoredAttackAge:time;
        // The viewer owns replay/hold/seek; sample its requested time without a second timeline.
        if(inspection){clip=DigimonModelLibrary.InspectionClip;age=time;}
        if(clip!=activeClip||reset)
        {
            for(int i=0;i<bones.Length;i++){transitionPositions[i]=bones[i].localPosition;transitionRotations[i]=bones[i].localRotation;}
            transitionAt=time;activeClip=clip;
        }
        if(clip=="Locomotion")
        {
            model.authored.clips["Idle"].Sample(time,posePositions,poseRotations);
            // Walk covers .30/.62 model units per cycle. Advance by travelled
            // distance, accounting for star scale and the blended stride.
            // The combat speed hint controls blending, not distance travelled.
            float cycleDistance=model.walkCycleDistance*modelScale;
            phase=reset?0:phase+delta.magnitude/cycleDistance*model.authored.clips["Walk"].duration/Mathf.Max(.1f,moveBlend);
            model.authored.clips["Walk"].Sample(phase,secondPositions,secondRotations);
            for(int i=0;i<bones.Length;i++)
            {
                posePositions[i]=Vector3.Lerp(posePositions[i],secondPositions[i],moveBlend);
                poseRotations[i]=Quaternion.Slerp(poseRotations[i],secondRotations[i],moveBlend);
            }
        }
        else
        {
            AuthoredModelData.Clip motion;
            if(!model.authored.clips.TryGetValue(clip,out motion))motion=model.authored.clips["Idle"];
            motion.Sample(age,posePositions,poseRotations);
        }
        float blend=reset?1:Mathf.SmoothStep(0,1,Mathf.Clamp01((time-transitionAt)/.12f));
        for(int i=0;i<bones.Length;i++)
        {
            bones[i].localPosition=Vector3.Lerp(transitionPositions[i],posePositions[i],blend);
            bones[i].localRotation=Quaternion.Slerp(transitionRotations[i],poseRotations[i],blend);
        }
        root.transform.localPosition=Vector3.zero;
        root.transform.localRotation=Quaternion.Euler(0,yaw,0);
        root.transform.localScale=Vector3.one*modelScale;
        previousPosition=position;lastTime=time;
    }
}
