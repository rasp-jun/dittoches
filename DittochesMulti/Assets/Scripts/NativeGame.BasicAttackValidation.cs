#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.IO;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    public void BeginBasicAttackSmoke(){StartCoroutine(BasicAttackSmoke());}
    System.Collections.IEnumerator BasicAttackSmoke()
    {
        yield return null;ValidateBasicAttacks();
        if(PortablePreview.HasArgument("--basic-attack-capture"))yield return CaptureBasicAttackReview();
        Debug.Log("BASIC ATTACK SMOKE COMPLETE: "+validationChecks+" checks");Application.Quit();
    }
    void ValidateBasicAttacks()
    {
        int before=validationChecks;
        var ids=DigimonModelLibrary.Roster.ToArray();
        Require(ids.Select(BasicAttackPresentation.For).Distinct().Count()>=18,"roster uses distinct contact and ranged attack families");
        foreach(string id in new[]{"agumon","togemon","garurumon","palmon","angemon","holyangemon","rosemon","wargreymon","devimon"})
            Require(BasicAttackPresentation.Contact(BasicAttackPresentation.For(id)),"weapon/contact attacks never spawn a travelling orb: "+id);
        using(var arena=new TacticalArena(new Rect(0,0,960,540)))
        {
            foreach(string id in ids)
            {
                var node=new GameObject("Basic attack validation");var actor=node.AddComponent<FaithfulModelActor>();actor.Initialize(id,30);actor.SetSize(DigimonVisualScale.Height(id));
                try
                {
                    float windup=BasicAttackPresentation.Windup(id);Vector3 forward=Vector3.forward;
                    actor.Pose(0,0,forward,null,-1,0,0,0);
                    actor.Pose(.02f,0,forward,null,-1,0,0,0,windup*.8f,true);
                    Require(actor.Motion=="Attack","windup starts before damage event: "+id);
                    float earlier=actor.MotionTime;
                    actor.Pose(.08f,0,forward,null,-1,0,0,0,.015f,true);
                    Require(actor.MotionTime>earlier,"windup advances toward authored release: "+id);
                    actor.Pose(.10f,0,forward,null,-1,.35f,0,1);
                    float release=actor.Data.clips["Attack"].duration*actor.Data.technique.attackRelease;
                    Require(Mathf.Abs(actor.MotionTime-release)<.001f,"attack event aligns to authored release: "+id);
                    actor.Pose(.20f,0,forward,null,-1,.25f,0,1);
                    Require(actor.MotionTime>release&&actor.Motion=="Attack","swing has follow through: "+id);
                    actor.Pose(.60f,0,forward,null,-1,0,0,1);
                    Require(actor.Motion=="Idle","recovery settles without repeating old attack: "+id);
                    actor.Pose(.61f,0,forward,null,-1,0,.1f,1);
                    Require(actor.Motion=="Down","death owns the animation: "+id);
                    Require(actor.Nodes.All(n=>!float.IsNaN(n.localPosition.sqrMagnitude)&&!float.IsInfinity(n.localPosition.sqrMagnitude)),"all attack bones remain finite: "+id);
                }
                finally{DestroyImmediate(node);}
                foreach(float age in new[]{-.01f,0f,.05f,.12f,.23f,.45f})
                {
                    arena.BeginFrame(-1,-1,-1,false);
                    arena.DrawBasicShape(id,Vector3.up,Vector3.up+Vector3.forward*1.5f,age,1,3);arena.Render();
                    Require(arena.SkillEffectsHealthy(),"pooled effect valid: "+id+" / "+age);
                    if(age<0||age>=BasicAttackPresentation.Lifetime(id))Require(arena.EffectCount==0,"future/expired attack emits nothing: "+id);
                    else Require(arena.EffectCount>0,"active attack has a visible signature: "+id);
                }
            }
            int allocated=arena.OwnedResourceCount,pool=arena.SkillPoolSize;
            for(int i=0;i<80;i++)
            {arena.BeginFrame(-1,-1,-1,false);arena.DrawBasicShape("wargreymon",Vector3.up,Vector3.up+Vector3.forward,.06f,i);arena.Render();}
            Require(arena.OwnedResourceCount==allocated&&arena.SkillPoolSize==pool,"repeated attacks reuse meshes materials and renderers");
        }
        Debug.Log("BASIC ATTACKS COMPLETE: "+(validationChecks-before)+" checks / "+ids.Length+" models");
    }
    System.Collections.IEnumerator CaptureBasicAttackReview()
    {
        string folder=Path.Combine(Application.dataPath,"../BasicAttackCaptures");Directory.CreateDirectory(folder);
        string[][] groups={new[]{"agumon","togemon","garurumon","holyangemon","rosemon","herakle"},
            new[]{"gabumon","tentomon","birdramon","metalgreymon","metalgarurumon","seraphimon"}};
        using(var review=new TacticalArena(new Rect(0,0,1280,720)))
        {
            var picture=new Texture2D(review.Texture.width,review.Texture.height,TextureFormat.RGB24,false);
            try
            {
                for(int group=0;group<groups.Length;group++)for(int frame=0;frame<90;frame++)
                {
                    float time=frame/30f;review.BeginFrame(-1,-1,-1,false);
                    for(int i=0;i<6;i++)
                    {
                        string id=groups[group][i];float cycle=time%1.4f,attackAge=cycle-.4f;int serial=cycle<.4f?(int)(time/1.4f):(int)(time/1.4f)+1;
                        Vector3 origin=TacticalArena.CellWorld(i%3*2+1,i/3*3+2),toward=Vector3.back;
                        review.SetActor(i,origin,null,i<3?Color.cyan:Color.yellow);
                        review.FaceActor(i,toward);review.CombatMotion(i,serial,cycle<.4f?.4f-cycle:-1,cycle<.4f);
                        review.PoseDigimon(i,id,0,0,group*10+time,attack:attackAge>=0?Mathf.Max(0,.35f-attackAge):0);
                        Vector3 target=origin+toward*1.1f+Vector3.up*DigimonVisualScale.Height(id)*.5f;
                        review.DrawBasicAttack(i,id,target,attackAge,serial);
                    }
                    // Unity updates skinning between engine frames; multiple Camera.Render
                    // calls in one frame would capture frozen skinned geometry.
                    yield return null;
                    review.Render();var old=RenderTexture.active;
                    try{RenderTexture.active=review.Texture;picture.ReadPixels(new Rect(0,0,picture.width,picture.height),0,0);picture.Apply();File.WriteAllBytes(Path.Combine(folder,(group*90+frame).ToString("D4")+".png"),picture.EncodeToPNG());}
                    finally{RenderTexture.active=old;}
                }
            }
            finally{Destroy(picture);}
        }
    }
}
#endif
