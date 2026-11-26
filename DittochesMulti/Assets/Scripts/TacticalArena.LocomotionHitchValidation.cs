#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Reflection;
using UnityEngine;

public static class LocomotionHitchValidation
{
    public static void Validate(Action<bool,string> require)
    {
        int checks=0;Action<bool,string> counted=(ok,message)=>{require(ok,message);checks++;};
        using(var review=new TacticalArena(new Rect(0,0,1280,720)))review.ValidateLocomotionHitches(counted);
        Debug.Log("LOCOMOTION HITCH COMPLETE: "+checks+" checks / actual board and tamer paths / 30-144 FPS / 100-250 ms / wrapped stride and combat transitions");
    }
    internal static float Float(FaithfulModelActor actor,string field)
    {return (float)typeof(FaithfulModelActor).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(actor);}
    internal static Vector3[] Velocities(FaithfulModelActor actor)
    {return (Vector3[])typeof(FaithfulModelActor).GetField("velocity",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(actor);}
}

public sealed partial class TacticalArena
{
    static bool HitchPhase(float actual,float expected)
    {return Mathf.Abs(Mathf.DeltaAngle(actual*360,expected*360))<.06f;}
    static Vector3 HitchPoint(float time,int side)
    {
        Vector3 point=CellWorld(2+.7f*time,2.7f+.8f*time);
        return side==0?point:new Vector3(-point.x,point.y,-point.z);
    }
    void PoseHitchBoard(object key,string id,int star,Vector3 point,WorldMotionTracker<int>.Motion movement,float time)
    {
        SetActor(key,point,null,Color.cyan);
        if(movement.speed>.025f)FaceActor(key,new Vector3(movement.x,0,movement.z));
        PoseDigimon(key,id,movement.speed,movement.x,time,star:star);
    }
    public void ValidateLocomotionHitches(Action<bool,string> require)
    {
        SetField(0);
        float[][] intervals={new[]{1f/30},new[]{1f/60},new[]{1f/144},new[]{.25f},new[]{.1f,.125f,.167f,.2f,.249f}};
        foreach(string id in new[]{"koromon","agumon","kuwagamon"})foreach(int star in new[]{1,3})foreach(int side in new[]{0,1})foreach(var steps in intervals)
        {
            object key=new object();var tracker=new WorldMotionTracker<int>();float time=0,expected=0;
            Vector3 previous=HitchPoint(time,side);var first=tracker.At(0,previous.x,previous.z,time);
            PoseHitchBoard(key,id,star,previous,first,time);var model=actors[key].faithful;
            float stride=Mathf.Max(.15f,DigimonVisualScale.Height(id,star)*.42f);
            int frames=steps.Length==1?Mathf.RoundToInt(1/steps[0]):steps.Length*3;
            for(int frame=0;frame<frames;frame++)
            {
                time+=steps[frame%steps.Length];Vector3 point=HitchPoint(time,side);
                var motion=tracker.At(0,point.x,point.z,time);
                PoseHitchBoard(key,id,star,point,motion,time);
                // Distance is independent of the actor's elapsed-time calculation.
                expected=Mathf.Repeat(expected+Vector3.Distance(point,previous)/stride,1);previous=point;
                string context=id+" / "+star+" / "+side+" / "+steps[frame%steps.Length];
                require(!motion.reset&&HitchPhase(model.LocomotionPhase,expected),"actual board stride preserves displayed distance across frame intervals / "+context);
                float held=model.LocomotionPhase;
                PoseHitchBoard(key,id,star,point,tracker.At(0,point.x,point.z,time),time);
                require(model.LocomotionPhase==held,"repeated board timestamp cannot double a recovered hitch stride / "+context);
            }
            float phase=model.LocomotionPhase;
            var stopped=tracker.At(0,previous.x,previous.z,time+.2f);time+=.2f;
            PoseHitchBoard(key,id,star,previous,stopped,time);
            require(model.Motion=="Idle"&&model.LocomotionPhase==phase,"post-hitch stop adds no filtered-speed travel / "+id);
            Vector3 corrected=previous+Vector3.right*.02f;
            PoseHitchBoard(key,id,star,corrected,tracker.At(0,corrected.x,corrected.z,time),time);
            require(model.LocomotionPhase==phase,"same-time position correction adds no stride / "+id);
            Vector3 teleport=corrected+Vector3.right*4;time+=.12f;
            var jumped=tracker.At(0,teleport.x,teleport.z,time);PoseHitchBoard(key,id,star,teleport,jumped,time);
            require(jumped.reset&&model.Motion=="Idle"&&model.LocomotionPhase==phase,"implausible displacement is not a recovered hitch / "+id);
            Vector3 paused=teleport+Vector3.right*.2f;time+=.251f;
            var gap=tracker.At(0,paused.x,paused.z,time);PoseHitchBoard(key,id,star,paused,gap,time);
            require(gap.reset&&model.LocomotionPhase==phase,"sample gap beyond 250 ms cannot invent past travel / "+id);
            Vector3 resumed=paused+Vector3.right*.04f;time+=.02f;
            var resumedMotion=tracker.At(0,resumed.x,resumed.z,time);PoseHitchBoard(key,id,star,resumed,resumedMotion,time);
            require(HitchPhase(model.LocomotionPhase,phase+Vector3.Distance(paused,resumed)/stride),"normal stride resumes from the gap baseline / "+id);
            var rewind=tracker.At(0,0,0,0);PoseHitchBoard(key,id,star,Vector3.zero,rewind,0);
            require(model.LocomotionPhase==0&&model.Motion=="Idle","rewind resets accumulated hitch stride / "+id);
            Release(actors[key].root);actors.Remove(key);
        }
        ValidateHitchTamerPaths(require);
        ValidateHitchTransitionBounds(require);
    }
    void ValidateHitchTamerPaths(Action<bool,string> require)
    {
        foreach(int choice in new[]{1,2,3})
        {
            object key=new object();Vector3 point=new Vector3(-1,.19f,0),velocity=new Vector3(.8f,0,-.6f);
            float time=3600,expected=0;SetTamer(key,choice,point,Vector3.zero,0,time);var model=actors[key].faithful;
            foreach(float step in new[]{.1f,.125f,.167f,.2f,.249f,.25f,.016f})
            {
                Vector3 previous=point;point+=velocity*step;time+=step;
                SetTamer(key,choice,point,Vector3.zero,1,time);
                expected=Mathf.Repeat(expected+Vector3.Distance(previous,point)/(.72f*1.08f*.42f),1);
                require(HitchPhase(model.LocomotionPhase,expected),"tamer stride at final world height survives hitches and long-running float timestamps / "+choice+" / "+step);
            }
            Release(actors[key].root);actors.Remove(key);
        }
    }
    void ValidateHitchTransitionBounds(Action<bool,string> require)
    {
        foreach(string id in new[]{"koromon","agumon","kuwagamon"})
        {
            object key=new object();SetActor(key,new Vector3(0,.19f,0),null,Color.cyan);PoseDigimon(key,id,4,0,0);
            var model=actors[key].faithful;float stride=Mathf.Max(.15f,DigimonVisualScale.Height(id)*.42f);
            require(model.LocomotionPhase==0,"first positive-speed sample cannot invent earlier travel / "+id);
            model.Pose(.25f,4,Vector3.forward,null,-1,0,0);
            require(HitchPhase(model.LocomotionPhase,1/stride),"250 ms sample can wrap multiple authored cycles without losing distance / "+id);
            float held=model.LocomotionPhase;
            model.Pose(.55f,4,Vector3.forward,null,-1,0,0);
            require(model.LocomotionPhase==held,"direct actor long gap freezes distance phase even without a tracker / "+id);
            model.Pose(.75f,0,Vector3.left,null,-1,.3f,0,1);
            require(model.Motion=="Attack"&&model.LocomotionPhase==held,"basic attack remains authoritative over locomotion after hitch / "+id);
            require(Mathf.Abs(LocomotionHitchValidation.Float(model,"blendAge")-.1f)<.0001f&&
                Mathf.Abs(LocomotionHitchValidation.Float(model,"velocityDelta")-.1f)<.0001f,"attack blend and outgoing velocity retain capped transition time / "+id);
            RequireHitchVelocities(model,require,id+" wrapped gait to attack");
            model.Pose(1.1f,0,Vector3.left,null,-1,0,0,1,hit:.18f,hitTime:1.1f);
            require(model.Motion=="Hit"&&model.LocomotionPhase==held,"timed hit keeps its event clock and frozen stride / "+id);
            require(Mathf.Abs(LocomotionHitchValidation.Float(model,"blendAge")-.1f)<.0001f,"hit blend still caps a long frame at 100 ms / "+id);
            model.Pose(1.25f,0,Vector3.left,null,-1,0,0,1,hit:0,hitTime:1.1f);
            require(Mathf.Abs(model.MotionTime-model.Data.clips["Hit"].duration*.15f/.22f)<.001f,"hit clip continues to use event age rather than locomotion elapsed / "+id);
            var skill=DigimonSkillCatalog.Find(id);float castAge=skill.windup*.5f;
            model.Pose(1.45f,4,Vector3.left,skill,castAge,0,0,1);
            require(model.Motion=="Skill"&&model.LocomotionPhase==held,"casting wins over a positive hitch movement sample / "+id);
            require(Mathf.Abs(model.MotionTime-model.Data.clips["Skill"].duration*model.Data.technique.skillRelease*.5f)<.001f,"skill release mapping remains unchanged by cadence recovery / "+id);
            require(Mathf.Abs(LocomotionHitchValidation.Float(model,"blendAge")-.1f)<.0001f,"skill pose transition retains the 100 ms cap / "+id);
            RequireHitchVelocities(model,require,id+" hit to skill");
            model.Pose(1.7f,0,Vector3.left,null,-1,0,0,1);
            require(model.Motion=="Idle"&&model.LocomotionPhase==held,"stop after cast does not spend deferred hitch distance / "+id);
            Bounds bounds=model.GeometryBounds();float size=bounds.size.sqrMagnitude;
            require(!float.IsNaN(size)&&!float.IsInfinity(size)&&size>0,"wrapped-cycle transitions retain finite skinned geometry / "+id);
            model.Pose(0,4,Vector3.forward,null,-1,0,0);
            require(model.LocomotionPhase==0,"direct actor rewind clears wrapped phase / "+id);
            Release(actors[key].root);actors.Remove(key);
        }
    }
    static void RequireHitchVelocities(FaithfulModelActor model,Action<bool,string> require,string context)
    {
        foreach(Vector3 velocity in LocomotionHitchValidation.Velocities(model))
            require(!float.IsNaN(velocity.sqrMagnitude)&&!float.IsInfinity(velocity.sqrMagnitude)&&velocity.magnitude<=1.5001f,
                "wrapped phase cannot amplify outgoing pose velocity past its existing bound / "+context);
        foreach(Transform node in model.Nodes)
            require(!float.IsNaN(node.localPosition.sqrMagnitude)&&!float.IsInfinity(node.localPosition.sqrMagnitude),
                "hitch transition leaves finite joint positions / "+context);
    }
}
#endif
