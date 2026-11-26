#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.IO;
using UnityEngine;

public sealed partial class NativeGame
{
    static void LocomotionPose(FaithfulModelActor actor,float time,float speed)
    {actor.Pose(time,speed,Vector3.back,null,-1,0,0,0);}
    void RequireLocomotionPose(FaithfulModelActor actor,FaithfulModelActor reference,string reason)
    {
        for(int i=0;i<actor.Nodes.Length;i++)
        {
            var actual=actor.Nodes[i];var expected=reference.Nodes[i];
            Require((actual.localPosition-expected.localPosition).sqrMagnitude<.000001f&&
                Quaternion.Angle(actual.localRotation,expected.localRotation)<.09f&&
                (actual.localScale-expected.localScale).sqrMagnitude<.000001f,reason+" / "+actor.Data.id+" / "+i);
        }
    }
    void ValidateLocomotion()
    {
        int before=validationChecks;
        foreach(string id in DigimonModelLibrary.Roster)
        {
            var parent=new GameObject("Locomotion validation "+id);parent.transform.position=Vector3.one*10000;
            var actor=new GameObject("Continuous gait").AddComponent<FaithfulModelActor>();actor.transform.SetParent(parent.transform,false);actor.Initialize(id,0);
            var walk=new GameObject("Authored walk").AddComponent<FaithfulModelActor>();walk.transform.SetParent(parent.transform,false);walk.Initialize(id,0);
            var run=new GameObject("Authored run").AddComponent<FaithfulModelActor>();run.transform.SetParent(parent.transform,false);run.Initialize(id,0);
            try
            {
                foreach(int star in new[]{1,3})
                {
                    float size=DigimonVisualScale.Height(id,star);actor.SetSize(size);walk.SetSize(size);run.SetSize(size);
                    foreach(float speed in new[]{.35f,.55f,.70f,.85f,1.1f})
                    {
                        LocomotionPose(actor,0,speed);
                        for(int frame=1;frame<=24;frame++)LocomotionPose(actor,frame/60f,speed);
                        float phase=actor.LocomotionPhase,weight=actor.LocomotionBlend;
                        Require(phase>=0&&phase<1&&weight>=0&&weight<=1,"gait phase and blend bounded / "+id);
                        Require(speed<=.45f?weight==0:speed>=.95f?weight==1:weight>0&&weight<1,"speed chooses authored or blended gait / "+id);
                        walk.Sample("Walk",phase*walk.Data.clips["Walk"].duration,true);
                        run.Sample("Run",phase*run.Data.clips["Run"].duration,true);
                        for(int n=0;n<actor.Nodes.Length;n++)
                        {
                            var node=actor.Nodes[n];Vector3 position=Vector3.LerpUnclamped(walk.Nodes[n].localPosition,run.Nodes[n].localPosition,weight);
                            Quaternion rotation=Quaternion.SlerpUnclamped(walk.Nodes[n].localRotation,run.Nodes[n].localRotation,weight);
                            Require((node.localPosition-position).sqrMagnitude<.000001f&&Quaternion.Angle(node.localRotation,rotation)<.09f,
                                "walk/run share the same stride without transition restart / "+id+" / "+n);
                        }
                        // GeometryBounds is in original actor-local source units;
                        // Kuwagamon's unscaled authored diagonal already exceeds 100.
                        // Compare actual deformations to both authored poses and
                        // report display size after the species' SetSize scale.
                        Bounds geometry=actor.GeometryBounds(),walkGeometry=walk.GeometryBounds(),runGeometry=run.GeometryBounds();
                        Vector3 displayedSize=geometry.size*actor.transform.localScale.x;
                        Bounds authoredEnvelope=walkGeometry;authoredEnvelope.Encapsulate(runGeometry);
                        float tolerance=actor.Data.restBounds.size.magnitude*.15f;
                        Vector3 low=authoredEnvelope.min-Vector3.one*tolerance,high=authoredEnvelope.max+Vector3.one*tolerance;
                        string skinContext=id+" star="+star+" speed="+speed+" raw="+geometry.size+" display="+displayedSize;
                        Require(!float.IsNaN(displayedSize.sqrMagnitude)&&!float.IsInfinity(displayedSize.sqrMagnitude)&&displayedSize.y>size*.1f,
                            "blended skinned geometry remains finite at presentation scale / "+skinContext);
                        Require(geometry.min.x>=low.x&&geometry.min.y>=low.y&&geometry.min.z>=low.z&&
                            geometry.max.x<=high.x&&geometry.max.y<=high.y&&geometry.max.z<=high.z,
                            "blended surface stays near both authored gait surfaces / "+skinContext);
                        if(weight==0||weight==1)
                        {
                            Bounds exact=weight==0?walkGeometry:runGeometry;
                            float exactTolerance=Mathf.Max(.001f,actor.Data.restBounds.size.magnitude*.002f);
                            Require((geometry.min-exact.min).magnitude<exactTolerance&&(geometry.max-exact.max).magnitude<exactTolerance,
                                "unblended skin matches its authored gait at original source scale / "+skinContext);
                        }
                        if(id=="kuwagamon")Debug.Log("LOCOMOTION SKIN: "+skinContext);
                        LocomotionPose(actor,.41f,0);
                        Require(actor.Motion=="Idle"&&actor.LocomotionPhase==phase,"stop begins settling without filtered phantom steps / "+id);
                        LocomotionPose(actor,.8f,0);
                        Require(actor.Motion=="Idle"&&actor.LocomotionPhase==phase,"standing still cannot advance feet / "+id);
                    }

                    float expectedPhase=0;
                    foreach(int fps in new[]{30,60,144})
                    {
                        LocomotionPose(actor,0,.72f);
                        for(int frame=1;frame<=fps;frame++)LocomotionPose(actor,frame/(float)fps,.72f);
                        if(fps==30)expectedPhase=actor.LocomotionPhase;
                        else Require(Mathf.Abs(Mathf.DeltaAngle(actor.LocomotionPhase*360,expectedPhase*360))<.01f,
                            "stride follows distance across render rates / "+id+" / "+fps);
                    }
                    LocomotionPose(actor,0,.35f);float previousBlend=0;
                    for(int frame=1;frame<=60;frame++)
                    {
                        LocomotionPose(actor,frame/60f,.35f+.75f*frame/60f);
                        Require(actor.LocomotionBlend>=previousBlend,"acceleration cannot reverse gait blend / "+id);
                        previousBlend=actor.LocomotionBlend;
                        if(frame%6!=0)continue;
                        float phase=actor.LocomotionPhase,weight=actor.LocomotionBlend;
                        walk.Sample("Walk",phase*walk.Data.clips["Walk"].duration,true);
                        run.Sample("Run",phase*run.Data.clips["Run"].duration,true);
                        for(int n=0;n<actor.Nodes.Length;n++)
                        {
                            Quaternion expected=Quaternion.SlerpUnclamped(walk.Nodes[n].localRotation,run.Nodes[n].localRotation,weight);
                            Require(Quaternion.Angle(actor.Nodes[n].localRotation,expected)<.09f,
                                "crossing walk/run threshold never restarts the transition / "+id+" / "+n);
                        }
                    }
                    // Duplicate GUI samples must preserve outgoing velocity when a
                    // moving actor begins its next attack.
                    LocomotionPose(actor,0,.72f);LocomotionPose(walk,0,.72f);
                    for(int frame=1;frame<=20;frame++)
                    {
                        LocomotionPose(actor,frame/60f,.72f);LocomotionPose(walk,frame/60f,.72f);
                        for(int repaint=0;repaint<3;repaint++)LocomotionPose(actor,frame/60f,.72f);
                    }
                    actor.Pose(.35f,0,Vector3.back,null,-1,.35f,0,1);
                    walk.Pose(.35f,0,Vector3.back,null,-1,.35f,0,1);
                    RequireLocomotionPose(actor,walk,"duplicate repaint preserves movement-to-attack momentum");
                    LocomotionPose(actor,0,.72f);
                    Require(actor.LocomotionPhase==0&&actor.LocomotionBlend>0&&actor.LocomotionBlend<1,"rewind clears gait history / "+id);
                    LocomotionPose(actor,.1f,float.NaN);
                    Require(actor.Motion=="Idle"&&actor.LocomotionPhase==0,"invalid speed cannot corrupt gait / "+id);
                }
            }
            finally{DestroyImmediate(parent);}
        }
        Debug.Log("LOCOMOTION COMPLETE: "+(validationChecks-before)+" checks / 39 models / 1 and 3 stars / shared stride and repaint stability");
    }
    System.Collections.IEnumerator CaptureLocomotionReview()
    {
        string folder=Path.Combine(Application.dataPath,"../LocomotionCaptures");Directory.CreateDirectory(folder);
        string[] ids={"agumon","garurumon","wargreymon","patamon","metalgreymon","metalgarurumon"};
        using(var review=new TacticalArena(new Rect(0,0,1280,720)))
        {
            var picture=new Texture2D(review.Texture.width,review.Texture.height,TextureFormat.RGB24,false);
            try
            {
                float distance=0;
                for(int frame=0;frame<150;frame++)
                {
                    float time=frame/30f,speed=time<3.5f?.25f+.9f*Mathf.Sin(time/3.5f*Mathf.PI):0;
                    distance+=speed/30f;review.BeginFrame(-1,-1,-1,false);
                    for(int i=0;i<ids.Length;i++)
                    {
                        Vector3 origin=TacticalArena.CellWorld(i%3*2+1,i/3*3+1)+Vector3.back*distance;
                        review.SetActor(i,origin,null,i<3?Color.cyan:Color.yellow);
                        review.FaceActor(i,Vector3.back);review.PoseDigimon(i,ids[i],speed,0,time);
                    }
                    yield return null;
                    review.Render();var old=RenderTexture.active;
                    try{RenderTexture.active=review.Texture;picture.ReadPixels(new Rect(0,0,picture.width,picture.height),0,0);picture.Apply();File.WriteAllBytes(Path.Combine(folder,frame.ToString("D4")+".png"),picture.EncodeToPNG());}
                    finally{RenderTexture.active=old;}
                }
            }
            finally{Destroy(picture);}
        }
    }
}
#endif
