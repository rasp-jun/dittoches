#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    public void BeginCombatFinishSmoke(){StartCoroutine(CombatFinishSmoke());}
    IEnumerator CombatFinishSmoke()
    {
        Application.runInBackground=true;
        yield return null;ValidateBasicAttacks();ValidateSkillFinish();ValidateCombatFinishRules();ValidateSkillRibbons();ValidateContactFinish();ValidateImpactAccents();ValidateHitReactions();ValidateImpactMotion();
        yield return CaptureCombatFinish();
        yield return CaptureAttackTiming(contact:true);
        yield return CaptureTargetContact();
        yield return CaptureImpactAccents();
        yield return CaptureHitReactions();
        Debug.Log("COMBAT FINISH SMOKE COMPLETE: "+validationChecks+" checks");Application.Quit();
    }
    void ValidateCombatFinishRules()
    {
        int before=validationChecks,peak=0;
        var skills=DigimonSkillCatalog.All.ToArray();
        Require(skills.Length==47&&skills.Select(s=>CombatEffectStyle.Skill(s.visual)).Distinct().Count()==8,"all 47 techniques cover eight visual impact identities");
        using(var review=new TacticalArena(new Rect(0,0,640,360)))
        {
            int resources=0,pool=0;
            for(int pass=0;pass<2;pass++)
            {
                foreach(var s in skills)
                {
                    foreach(float age in new[]{s.windup*.6f,s.windup+s.travel*.65f,s.Impact+.06f,s.Duration-.01f})
                    {
                        review.BeginFrame(-1,-1,-1,false);
                        for(int i=0;i<14;i++)review.DrawSkill(s,new Vector3(i%7-3,0,i/7*3-1.5f),new Vector3(0,0,0),age,3000+i);
                        review.Render();peak=Mathf.Max(peak,review.EffectCount);
                        Require(review.SkillEffectsHealthy()&&(age>=s.Impact+(s.shots-1)*s.interval+.3f||review.EffectCount>0),"fourteen concurrent "+s.id+" casts stay finite, visible and below the shared effect cap at "+age);
                    }
                    foreach(int star in new[]{1,3})
                    {
                        review.BeginFrame(-1,-1,-1,false);
                        for(int i=0;i<14;i++)review.DrawBasicShape(s.id,new Vector3(i%7-3,.6f,i/7*3-1.5f),new Vector3(0,.6f,0),BasicAttackPresentation.Travel(s.id)+.05f,i,star);
                        review.Render();peak=Mathf.Max(peak,review.EffectCount);
                        Require(review.SkillEffectsHealthy()&&review.EffectCount>0,"fourteen concurrent basic impacts "+s.id+" star "+star+" retain bounded visible geometry");
                    }
                }
                Require(review.CombatRibbonHealthy(),"continuous tapered ribbon has finite shared geometry and transparent endpoints");
                if(pass==0){resources=review.OwnedResourceCount;pool=review.SkillPoolSize;}
                else Require(resources==review.OwnedResourceCount&&pool==review.SkillPoolSize,"all warmed-up attack families reuse exact mesh/material/renderer allocations");
            }
            review.BeginFrame(-1,-1,-1,false);review.Render();Require(review.EffectCount==0&&review.SkillEffectsHealthy(),"no lingering pooled attack geometry after combat");
        }
        Debug.Log("COMBAT FINISH RULES COMPLETE: "+(validationChecks-before)+" checks / 47 species / 8 impact elements / 14 simultaneous identical casts / peak "+peak+" effects");
    }
    IEnumerator CaptureCombatFinish()
    {
        string folder=Path.Combine(Application.dataPath,"../CombatFinishCaptures");Directory.CreateDirectory(folder);
        using(var review=new TacticalArena(new Rect(0,0,960,640)))
        {
            review.UseModelStudio(Vector3.zero);var texture=new Texture2D(review.Texture.width,review.Texture.height,TextureFormat.RGB24,false);
            try
            {
                int serial=0;
                foreach(var skill in DigimonSkillCatalog.All)
                {
                    float height=Mathf.Max(DigimonVisualScale.Height(skill.id),DigimonVisualScale.Height("greymon"));
                    review.SetView(new Vector3(-.3f,height*1.05f+1.2f,-6.5f-height*.6f),new Vector3(0,height*.42f,0),38);
                    foreach(bool basic in new[]{true,false})foreach(bool impact in new[]{false,true})
                    {
                        serial++;
                        float travel=basic?BasicAttackPresentation.Travel(skill.id):skill.travel;
                        float age=basic?(impact?travel+.065f:Mathf.Max(.025f,travel*.6f)):(impact?skill.Impact+.09f:skill.windup+travel*.6f);
                        Vector3 origin=new Vector3(-1.1f,.12f,0),target=new Vector3(1.1f,.12f,0);
                        review.BeginFrame(-1,-1,-1,false);
                        review.SetActor(0,origin,null,Color.cyan);review.FaceActor(0,Vector3.right);
                        review.CombatMotion(0,serial,-1,false);
                        review.PoseDigimon(0,skill.id,0,0,10+age,basic?-1:age,basic?Mathf.Max(.001f,.35f-age):0);
                        review.SetActor(1,target,null,Color.red);review.FaceActor(1,Vector3.left);review.PoseDigimon(1,"greymon",0,0,10+age);
                        if(basic)review.DrawBasicAttack(0,skill.id,target+Vector3.up*DigimonVisualScale.Height("greymon")*.55f,age,serial,targetSurface:review.ContactSurface(1,CombatContactPresentation.Height("greymon"),CombatContactPresentation.Surface("greymon")));
                        else review.DrawSkill(skill,origin,target,age,100,"greymon",1,review.ContactSurface(1,CombatContactPresentation.Height("greymon"),CombatContactPresentation.Surface("greymon")));
                        yield return null;review.Render();Require(review.SkillEffectsHealthy(),"actual models and attack capture "+skill.id);
                        var old=RenderTexture.active;
                        try
                        {
                            RenderTexture.active=review.Texture;texture.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);texture.Apply();
                            File.WriteAllBytes(Path.Combine(folder,skill.id+(basic?"-basic":"-skill")+(impact?"-impact":"-flight")+".png"),texture.EncodeToPNG());
                        }
                        finally{RenderTexture.active=old;}
                    }
                }
            }
            finally{Destroy(texture);}
        }
        Debug.Log("COMBAT FINISH CAPTURES COMPLETE: 47 species, 188 actual model/attack phase captures");
    }
}
#endif
