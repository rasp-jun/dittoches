#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.IO;
using UnityEngine;

public sealed partial class NativeGame
{
    void ValidateWorldMotion()
    {
        int before=validationChecks;WorldMotionValidation.Validate(Require);
        ValidateWorldFacingPriority();
        foreach(string id in new[]{"koromon","agumon","garurumon","metalgreymon","wargreymon","kuwagamon"})
        {
            var parent=new GameObject("World movement validation "+id);parent.transform.position=Vector3.one*10000;
            var actor=new GameObject(id).AddComponent<FaithfulModelActor>();actor.transform.SetParent(parent.transform,false);actor.Initialize(id,0);
            try
            {
                foreach(int star in new[]{1,3})foreach(int side in new[]{0,1})foreach(int fps in new[]{30,60,144})
                {
                    float size=DigimonVisualScale.Height(id,star);actor.SetSize(size);
                    var tracker=new WorldMotionTracker<int>();float accumulated=0;
                    Vector3 initial=TacticalArena.CellWorld(2,2.5f);if(side==1)initial=new Vector3(-initial.x,initial.y,-initial.z);
                    tracker.At(0,initial.x,initial.z,0);actor.Pose(0,0,side==0?Vector3.back:Vector3.forward,null,-1,0,0,0);
                    for(int frame=1;frame<=fps;frame++)
                    {
                        float time=frame/(float)fps;Vector3 point=TacticalArena.CellWorld(2+.8f*time,2.5f+time);
                        if(side==1)point=new Vector3(-point.x,point.y,-point.z);
                        var motion=tracker.At(0,point.x,point.z,time);float dt=time-(frame-1)/(float)fps;
                        actor.Pose(time,motion.speed,new Vector3(motion.x,0,motion.z),null,-1,0,0,0);
                        accumulated=Mathf.Repeat(accumulated+motion.speed*dt/Mathf.Max(.15f,size*.42f),1);
                        Require(Mathf.Abs(Mathf.DeltaAngle(actor.LocomotionPhase*360,accumulated*360))<.05f,
                            "actual actor stride follows arena distance / "+id+" / "+side+" / "+fps);
                        float phase=actor.LocomotionPhase;
                        actor.Pose(time,tracker.At(0,point.x,point.z,time).speed,new Vector3(motion.x,0,motion.z),null,-1,0,0,0);
                        Require(actor.LocomotionPhase==phase,"repaint cannot advance world-distance stride / "+id);
                        if(frame==fps)Require(Vector3.Angle(-actor.transform.forward,new Vector3(motion.x,0,motion.z))<1,
                            "body finishes turning along the displayed world path / "+id+" / "+side);
                    }
                    Vector3 final=TacticalArena.CellWorld(2.8f,3.5f);if(side==1)final=new Vector3(-final.x,final.y,-final.z);
                    var stopped=tracker.At(0,final.x,final.z,1+1f/fps);float held=actor.LocomotionPhase;
                    actor.Pose(1+1f/fps,stopped.speed,Vector3.zero,null,-1,0,0,0);
                    Require(actor.Motion=="Idle"&&Mathf.Abs(Mathf.DeltaAngle(actor.LocomotionPhase*360,held*360))<.001f,"world-distance actor settles at destination / "+id);
                    tracker.At(0,final.x+3,final.z,1+.02f+1f/fps);
                    var afterSeek=tracker.At(0,initial.x,initial.z,0);
                    Require(afterSeek.reset&&afterSeek.speed==0,"world movement history clears on backward replay / "+id);
                }
            }
            finally{DestroyImmediate(parent);}
        }
        Debug.Log("WORLD MOTION COMPLETE: "+(validationChecks-before)+" checks / 6 body sizes / both seats / irregular snapshots / frame rates");
    }
    void ValidateWorldFacingPriority()
    {
        var source=new Fighter{unit=new Unit(RosterById["agumon"]),pos=new Vector2(3,5),renderPos=new Vector2(3,5),maxMana=100};
        var target=new Fighter{unit=new Unit(RosterById["gabumon"]),pos=new Vector2(3,0),renderPos=new Vector2(3,0)};
        source.target=target;Vector3 origin=TacticalArena.CellWorld(3,5);
        var movement=new WorldMotionTracker<Fighter>.Motion{x=1,z=.2f,speed=1.02f};Vector3 travel=new Vector3(movement.x,0,movement.z);
        foreach(float cooldown in new[]{-.5f,.05f})
        {
            source.cooldown=cooldown;bool inRange=NativeBasicAttackInRange(source);
            Require(!inRange&&NativeFighterFacing(source,origin,movement,inRange)==travel,
                "distant target cannot override actual pursuit direction even with an expired cooldown");
        }
        target.pos=target.renderPos=new Vector2(3,4);source.cooldown=.05f;
        bool ready=NativeBasicAttackInRange(source);Vector3 aim=TacticalArena.CellWorld(target.renderPos.x,target.renderPos.y)-origin;
        Require(ready&&NativeFighterFacing(source,origin,movement,ready)==aim,"in-range basic windup aims at the actual target");
        source.mana=source.maxMana;bool fullMana=NativeBasicAttackInRange(source);
        Require(!fullMana&&NativeFighterFacing(source,origin,movement,fullMana)==travel,"full mana cannot start a basic-attack aiming pose");
        source.mana=0;target.dead=true;
        Require(!NativeBasicAttackInRange(source)&&NativeFighterFacing(source,origin,movement,false)==travel,"dead target cannot override pursuit direction");
        target.dead=false;source.cooldown=-.1f;
        Require(NativeFighterFacing(source,origin,movement,NativeBasicAttackInRange(source))==travel,"uncommitted overdue attack is not a windup pose");
        source.attackTarget=new Vector2(1,2);Vector3 committed=TacticalArena.CellWorld(1,2)-origin;
        source.attackFlash=.2f;
        Require(NativeFighterFacing(source,origin,movement,false)==committed,"committed basic attack keeps its locked aim while moving");
        source.attackFlash=0;source.skillCast=new SkillCast();
        Require(NativeFighterFacing(source,origin,movement,false)==committed,"active skill keeps its locked aim while moving");
        source.skillCast=null;movement=new WorldMotionTracker<Fighter>.Motion();
        Require(NativeFighterFacing(source,origin,movement,false)==aim,"stationary unit still watches its target");
    }
    System.Collections.IEnumerator CaptureWorldMotionReview()
    {
        string folder=Path.Combine(Application.dataPath,"../WorldMotionCaptures");Directory.CreateDirectory(folder);
        string[] ids={"agumon","garurumon","wargreymon","patamon","metalgreymon","kuwagamon"};
        var movement=new WorldMotionTracker<int>();var facings=new Vector3[ids.Length];
        for(int i=0;i<facings.Length;i++)facings[i]=i<3?Vector3.back:Vector3.forward;
        using(var review=new TacticalArena(new Rect(0,0,1280,720)))
        {
            review.SetField(0);
            var picture=new Texture2D(review.Texture.width,review.Texture.height,TextureFormat.RGB24,false);
            try
            {
                for(int frame=0;frame<150;frame++)
                {
                    float time=frame/30f,p=Mathf.Clamp01(time/3.5f);p=p*p*(3-2*p);
                    review.BeginFrame(-1,-1,-1,false);
                    for(int i=0;i<ids.Length;i++)
                    {
                        int path=i%3;float x=path==0?.4f+1.1f*p:path==1?2.5f:4.5f+1.2f*p,y=path==0?1.5f:1+1.5f*p;
                        Vector3 point=TacticalArena.CellWorld(x,y);if(i>=3)point=new Vector3(-point.x,point.y,-point.z);
                        var motion=movement.At(i,point.x,point.z,time);
                        review.SetActor(i,point,null,i<3?Color.cyan:Color.yellow);
                        if(motion.speed>.025f)facings[i]=new Vector3(motion.x,0,motion.z);
                        review.FaceActor(i,facings[i]);review.PoseDigimon(i,ids[i],motion.speed,motion.x,time);
                    }
                    yield return null;review.Render();var old=RenderTexture.active;
                    try{RenderTexture.active=review.Texture;picture.ReadPixels(new Rect(0,0,picture.width,picture.height),0,0);picture.Apply();File.WriteAllBytes(Path.Combine(folder,frame.ToString("D4")+".png"),picture.EncodeToPNG());}
                    finally{RenderTexture.active=old;}
                }
            }
            finally{Destroy(picture);}
        }
    }
}
#endif
