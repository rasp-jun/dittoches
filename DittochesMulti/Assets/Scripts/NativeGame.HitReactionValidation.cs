#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    void ValidateHitReactions()
    {
        int before=validationChecks;int wasArt=artPack;bool wasSkill=applyingSkillDamage;
        try
        {
            artPack=0;applyingSkillDamage=false;
            var track=new CombatHitTrack();track.Add(1.2f,Vector3.right);track.Add(1.1f,Vector3.forward);
            Require(track.At(1).time<0&&track.At(1).Flash(1)==0,"queued projectile cannot flash before arrival");
            Require(track.At(1.15f).direction==Vector3.forward&&track.At(1.21f).direction==Vector3.right,"immediate skill and delayed projectile retain independent arrival order");
            Require(track.At(3).time<0&&track.At(1.21f).time==1.2f,"sampling expires and rewinds without consuming replay events");
            for(int i=0;i<5;i++)Require(track.At(1.21f).time==1.2f,"same-time IMGUI repaint preserves hit stamp");
            track.Add(float.NaN,Vector3.one);track.Add(float.PositiveInfinity,Vector3.one);track.Add(-1,Vector3.one);
            Require(track.Count==2,"invalid hit clocks are rejected");
            track.Prune(2);Require(track.Count==0,"native history prunes finished reactions");
            track.Add(3,Vector3.right,3.2f);Require(track.At(3.1f).time<0&&track.At(3.21f).time==3,"online cannot reveal a hit before its snapshot");
            track.Add(4,Vector3.right);track.Add(4,Vector3.left);Require(track.At(4).direction==Vector3.zero,"opposite simultaneous sources use neutral fallback without NaN");
            foreach(string id in DigimonModelLibrary.Roster)
            {
                var source=new Fighter{unit=new Unit(RosterById[id]),renderPos=new Vector2(1,3),hp=100};
                var target=new Fighter{unit=new Unit(RosterById["greymon"]),renderPos=new Vector2(4,3),hp=100};
                float travel=BasicAttackPresentation.Travel(id);RecordHitReaction(source,target,10);
                Require(NativeHitReaction(target,10+travel-.005f).time<0,"native waits for actual basic impact "+id);
                var hit=NativeHitReaction(target,10+travel+.04f);
                Require(Mathf.Abs(hit.time-(10+travel))<.0001f&&hit.direction.x>.99f&&hit.Flash(10+travel+.04f)>.7f,"native impact direction and flash follow source "+id);
                source.renderPos=new Vector2(6,3);Require(NativeHitReaction(target,10+travel+.04f).direction.x>.99f,"later source motion cannot rotate a released hit "+id);
                Require(target.hp==100&&target.Snapshot().hitReactions==null,"presentation does not change HP or leak histories into snapshots "+id);
                applyingSkillDamage=true;RecordHitReaction(source,target,11);applyingSkillDamage=false;
                Require(NativeHitReaction(target,11.04f).time==11&&NativeHitReaction(target,11.04f).direction.x<-.99f,"skill or status damage already arrives at its authoritative impact "+id);
                target.dead=true;Require(NativeHitReaction(target,11.04f).time<0,"death owns the model "+id);
                target.dead=false;target.hp=0;RecordHitReaction(source,target,11.05f);target.hp=100;
                Require(NativeHitReaction(target,11.06f).time<0,"lethal damage clears pending recoil before revival "+id);
            }
            using(var review=new TacticalArena(new Rect(0,0,640,360)))
            {
                var old=arena;
                try
                {
                    arena=review;var f=new Fighter{unit=new Unit(RosterById["greymon"]),hp=100,renderPos=new Vector2(3,3),hitFlash=.18f,hitReactions=new CombatHitTrack()};
                    f.hitReactions.Add(Time.unscaledTime+.1f,Vector3.right);
                    review.BeginFrame(-1,-1,-1,false);RenderArenaFighter(f);review.RequireHitReaction(f,-100,Vector3.zero,0,Require);
                    f.hitReactions.Add(Time.unscaledTime-.04f,Vector3.left);
                    RenderArenaFighter(f);review.RequireHitReaction(f,Time.unscaledTime-.04f,Vector3.left,1-.04f/.18f,Require);
                }
                finally{arena=old;}
            }
        }
        finally{artPack=wasArt;applyingSkillDamage=wasSkill;}
        Debug.Log("HIT REACTIONS NATIVE COMPLETE: "+(validationChecks-before)+" checks / all 47 basic arrivals, incoming directions, native renderer, skills, death and seek-safe tracks");
    }
    IEnumerator CaptureHitReactions()
    {
        string folder=Path.Combine(Application.dataPath,"../HitReactionCaptures");Directory.CreateDirectory(folder);
        string[] ids={"gabumon","tentomon","metalgreymon","ogremon","garurumon","agumon"};
        int wasArt=artPack;bool wasSkill=applyingSkillDamage;artPack=0;
        try
        {
            using(var review=new TacticalArena(new Rect(0,0,960,640)))
            {
                review.UseModelStudio(Vector3.zero);var texture=new Texture2D(review.Texture.width,review.Texture.height,TextureFormat.RGB24,false);
                try
                {
                    for(int n=0;n<ids.Length;n++)
                    {
                        string id=ids[n];bool basic=id!="agumon";var skill=DigimonSkillCatalog.Find(id);float start=.25f,impact=start+(basic?BasicAttackPresentation.Travel(id):skill.Impact);
                        var source=new Fighter{unit=new Unit(RosterById[id]),hp=100,renderPos=new Vector2(1,3)};
                        var target=new Fighter{unit=new Unit(RosterById["greymon"]),hp=100,renderPos=new Vector2(4,3)};
                        applyingSkillDamage=!basic;RecordHitReaction(source,target,5+(basic?start:impact));
                        Vector3 from=new Vector3(-1.1f,.12f,0),to=new Vector3(1.1f,.12f,0);float height=Mathf.Max(DigimonVisualScale.Height(id),DigimonVisualScale.Height("greymon"));
                        review.SetView(new Vector3(-.3f,height*1.05f+1.2f,-6.5f-height*.6f),new Vector3(0,height*.42f,0),38);
                        for(int frame=0;frame<48;frame++)
                        {
                            float time=frame/47f*1.4f,age=time-start;var hit=NativeHitReaction(target,5+time);
                            review.BeginFrame(-1,-1,-1,false);review.SetActor(n*2,from,null,Color.cyan);review.FaceActor(n*2,Vector3.right);
                            review.CombatMotion(n*2,age>=0?1:0,age<0?-age:-1,age<0);review.PoseDigimon(n*2,id,0,0,5+time,basic?-1:age,basic&&age>=0?Mathf.Max(0,.35f-age):0);
                            review.SetActor(n*2+1,to,null,Color.red);review.FaceActor(n*2+1,Vector3.back);review.CombatMotion(n*2+1,0,-1,false,hit.time,hit.direction);
                            review.DecorateActor(n*2+1,false,0,hit.Flash(5+time));review.PoseDigimon(n*2+1,"greymon",0,0,5+time);
                            float targetHeight=CombatContactPresentation.Height("greymon"),surface=review.ContactSurface(n*2+1,targetHeight,CombatContactPresentation.Surface("greymon"));
                            if(age>=0){if(basic)review.DrawBasicAttack(n*2,id,to+Vector3.up*targetHeight,age,1,targetSurface:surface);else review.DrawSkill(skill,from,to,age,n+1,"greymon",1,surface);}
                            yield return null;review.Render();Require(review.SkillEffectsHealthy(),"reaction capture finite "+id+"/"+frame);
                            var old=RenderTexture.active;
                            try{RenderTexture.active=review.Texture;texture.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(folder,id+"-"+frame.ToString("D2")+".png"),texture.EncodeToPNG());}
                            finally{RenderTexture.active=old;}
                        }
                        File.WriteAllText(Path.Combine(folder,id+".json"),"{\"id\":\""+id+"\",\"basic\":"+(basic?"true":"false")+",\"frames\":48,\"seconds\":1.4,\"impact\":"+impact.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");
                    }
                }
                finally{Destroy(texture);}
            }
        }
        finally{artPack=wasArt;applyingSkillDamage=wasSkill;}
        Debug.Log("HIT REACTIONS CAPTURES COMPLETE: 6 sequences / 288 actual Unity frames");
    }
}
public sealed partial class TacticalArena
{
    public void RequireHitReaction(object key,float time,Vector3 direction,float flash,Action<bool,string> require)
    {
        Actor actor;require(actors.TryGetValue(key,out actor),"host renders target actor");
        require(Mathf.Abs(actor.hitTime-time)<.001f&&(actor.hitDirection-direction).magnitude<.001f,"host passes the resolved impact timestamp and incoming direction");
        require(Mathf.Abs(actor.hitAmount/.18f-flash)<.001f,"host flash follows resolved impact instead of raw damage release");
    }
}
#endif
