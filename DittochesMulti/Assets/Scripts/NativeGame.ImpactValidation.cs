#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.IO;
using UnityEngine;

public sealed partial class NativeGame
{
    static void ImpactPose(FaithfulModelActor actor,float time,float hit,DigimonSkillCatalog.Entry skill=null,float death=0)
    {
        actor.Pose(1+time,0,Vector3.back,skill,skill==null?-1:time,
            Mathf.Max(0,.35f-time),death,1,-1,false,hit);
    }
    void ValidateImpactMotion()
    {
        int before=validationChecks;
        foreach(string id in DigimonModelLibrary.Roster)
        {
            var parent=new GameObject("Impact validation "+id);parent.transform.position=Vector3.one*10000;
            var actor=new GameObject("Hit").AddComponent<FaithfulModelActor>();actor.transform.SetParent(parent.transform,false);actor.Initialize(id,0);
            var reference=new GameObject("Reference").AddComponent<FaithfulModelActor>();reference.transform.SetParent(parent.transform,false);reference.Initialize(id,0);
            try
            {
                foreach(int star in new[]{1,3})
                foreach(bool cast in new[]{false,true})
                {
                    float size=DigimonVisualScale.Height(id,star);
                    actor.SetSize(size);reference.SetSize(size);
                    var scale=actor.transform.localScale;var anchor=actor.transform.localPosition;
                    var skill=cast?DigimonSkillCatalog.Find(id):null;
                    // Rewind explicitly, then compare with a no-impact actor at identical clip times.
                    ImpactPose(actor,-.1f,0);ImpactPose(reference,-.1f,0);
                    ImpactPose(actor,0,.18f,skill);ImpactPose(reference,0,0,skill);
                    Require((actor.transform.localPosition-anchor).sqrMagnitude<.000001f,"impact starts without teleport "+id);
                    ImpactPose(actor,.04f,.14f,skill);ImpactPose(reference,.04f,0,skill);
                    Require(actor.Motion==reference.Motion&&Mathf.Abs(actor.MotionTime-reference.MotionTime)<.00001f,"impact preserves attack/skill timeline "+id);
                    Require(actor.Motion==(cast&&skill!=null?"Skill":"Attack"),"impact does not interrupt committed action "+id);
                    float shift=(actor.transform.localPosition-anchor).magnitude;
                    Require(shift>.001f&&shift<.16f,"bounded visible recoil at each evolution size "+id);
                    Require(actor.transform.localScale==scale&&parent.transform.position==Vector3.one*10000,"impact preserves scale and board parent "+id);
                    for(int n=0;n<actor.Nodes.Length;n++)
                        Require((actor.Nodes[n].localPosition-reference.Nodes[n].localPosition).sqrMagnitude<.000001f&&Quaternion.Angle(actor.Nodes[n].localRotation,reference.Nodes[n].localRotation)<.08f,"root layer leaves authored skeleton intact "+id+" star="+star+" cast="+cast+" node="+n+" dp="+(actor.Nodes[n].localPosition-reference.Nodes[n].localPosition).magnitude+" dq="+Quaternion.Angle(actor.Nodes[n].localRotation,reference.Nodes[n].localRotation));
                    Vector3 peak=actor.transform.localPosition;Quaternion rotation=actor.transform.localRotation;
                    for(int duplicate=0;duplicate<3;duplicate++){actor.SetSize(size);ImpactPose(actor,.04f,.14f,skill);}
                    Require((actor.transform.localPosition-peak).sqrMagnitude<.000001f&&Quaternion.Angle(actor.transform.localRotation,rotation)<.02f,"same-time repaint cannot accumulate impact "+id);
                    ImpactPose(actor,.041f,.18f,skill);
                    Require((actor.transform.localPosition-peak).magnitude<.003f,"rapid second hit keeps position continuous "+id);
                    ImpactPose(actor,.4f,0,skill);
                    Require((actor.transform.localPosition-anchor).sqrMagnitude<.000001f&&Quaternion.Angle(actor.transform.localRotation,Quaternion.identity)<.02f,"impact expires after skipped frames "+id);
                    ImpactPose(actor,0,.18f,skill);ImpactPose(actor,.12f,.06f,skill);
                    Vector3 late=actor.transform.localPosition;Quaternion lateRotation=actor.transform.localRotation;
                    // A replay first seeing an older hit must show its current age, not restart it.
                    ImpactPose(reference,-.1f,0);ImpactPose(reference,.12f,.06f,skill);
                    Require((reference.transform.localPosition-late).sqrMagnitude<.000001f&&Quaternion.Angle(reference.transform.localRotation,lateRotation)<.02f,"late hit agrees with continuously sampled playback "+id);
                    ImpactPose(actor,.13f,.05f,skill,.1f);
                    Require(actor.Motion=="Down"&&(actor.transform.localPosition-anchor).sqrMagnitude<.000001f,"death owns pose and clears recoil "+id);
                    ImpactPose(actor,-.1f,0);
                    Require((actor.transform.localPosition-anchor).sqrMagnitude<.000001f,"rewind clears previous impact "+id);
                }
                actor.SetSize(DigimonVisualScale.Height(id));var resting=actor.transform.localPosition;
                foreach(Vector3 facing in new[]{Vector3.forward,Vector3.back,Vector3.left,Vector3.right})
                {
                    actor.Pose(0,0,facing,null,-1,0,0,0);
                    actor.Pose(.1f,0,facing,null,-1,0,0,0,hit:.18f);
                    actor.Pose(.14f,0,facing,null,-1,0,0,0,hit:.14f);
                    Vector3 contact=actor.transform.localPosition-actor.transform.localRotation*resting;
                    Require(actor.Motion=="Hit"&&Vector3.Dot(contact,-facing)>.001f&&Mathf.Abs(contact.y)<.00001f,"idle hit preserves authored clip and horizontal ground contact "+id);
                    actor.Pose(2,0,facing,null,-1,0,0,0,hit:.18f);
                    Require((actor.transform.localPosition-resting).sqrMagnitude<.000001f,"new hit after a long frame cannot inherit expired recoil "+id);
                }
                actor.Pose(0,0,Vector3.back,null,-1,0,0,0,hitTime:-10);
                actor.Pose(.2f,0,Vector3.back,null,-1,0,0,0,hitTime:.1f);
                Vector3 firstTimed=actor.transform.localPosition;
                actor.Pose(.5f,0,Vector3.back,null,-1,0,0,0,hitTime:.4f);
                Require((actor.transform.localPosition-firstTimed).sqrMagnitude<.000001f&&(firstTimed-resting).magnitude>.001f,"equal-strength consecutive online hits each react "+id);
                actor.Pose(.8f,0,Vector3.back,null,-1,0,0,0,hitTime:.4f);
                Require((actor.transform.localPosition-resting).sqrMagnitude<.000001f,"repeated snapshot never restarts old hit "+id);
                actor.Pose(.9f,0,Vector3.back,null,-1,0,0,0,hitTime:.71f);
                Require((actor.transform.localPosition-resting).magnitude>.00001f,"late snapshot retains final recoil after flash has ended "+id);
                actor.Pose(1.2f,0,Vector3.back,null,-1,0,0,0,hitTime:2);
                Require((actor.transform.localPosition-resting).sqrMagnitude<.000001f,"future hit is never presented early "+id);
                Vector3 frameRateReference=Vector3.zero;
                foreach(int fps in new[]{30,60,144})
                {
                    ImpactPose(actor,-.1f,0);ImpactPose(actor,0,.18f);
                    for(int frame=1;frame<fps*.1f;frame++)ImpactPose(actor,frame/(float)fps,.18f-frame/(float)fps);
                    ImpactPose(actor,.1f,.08f);
                    if(fps==30)frameRateReference=actor.transform.localPosition;
                    else Require((actor.transform.localPosition-frameRateReference).sqrMagnitude<.000001f,"impact amplitude independent of render FPS "+id);
                }
            }
            finally{DestroyImmediate(parent);}
        }
        Debug.Log("IMPACT MOTION COMPLETE: "+(validationChecks-before)+" checks / 34 models / 1 and 3 stars / attack and skill");
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"--impact-capture")>=0)CaptureImpactReview();
    }
    void CaptureImpactReview()
    {
        string folder=Path.Combine(Application.dataPath,"../ImpactCaptures");Directory.CreateDirectory(folder);
        string[] ids={"agumon","metalgreymon","wargreymon","metalgarurumon"};
        using(var review=new TacticalArena(new Rect(0,0,1440,900)))
        {
            var picture=new Texture2D(review.Texture.width,review.Texture.height,TextureFormat.RGB24,false);
            try
            {
                for(int frame=0;frame<60;frame++)
                {
                    float time=frame/30f,hitAge=time-.35f;
                    if(time>=1.35f)hitAge=time-1.35f;else if(time>=.85f)hitAge=time-.85f;
                    float hit=hitAge>=0?Mathf.Max(0,.18f-hitAge):0;
                    review.BeginFrame(-1,-1,-1,false);
                    for(int i=0;i<ids.Length;i++)
                    {
                        review.SetActor(i,TacticalArena.CellWorld(1.5f+(i%2)*3,2+(i/2)*3),null,i<2?Color.red:Color.cyan);
                        review.FaceActor(i,new Vector3(-.3f,0,-1));review.CombatMotion(i,1,-1,false);
                        review.DecorateActor(i,false,0,hit/.18f);
                        review.PoseDigimon(i,ids[i],0,0,time,castAge:time,star:1);
                    }
                    review.Render();var old=RenderTexture.active;
                    try
                    {
                        RenderTexture.active=review.Texture;picture.ReadPixels(new Rect(0,0,picture.width,picture.height),0,0);picture.Apply();
                        File.WriteAllBytes(Path.Combine(folder,frame.ToString("D3")+".png"),picture.EncodeToPNG());
                    }
                    finally{RenderTexture.active=old;}
                }
            }
            finally{Destroy(picture);}
        }
    }
}
#endif
