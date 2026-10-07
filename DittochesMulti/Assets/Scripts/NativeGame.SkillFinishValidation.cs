#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.IO;
using UnityEngine;

public sealed partial class NativeGame
{
    void ValidateSkillFinish()
    {
        int first=validationChecks;string folder=Path.Combine(Application.dataPath,"../SkillFinishCaptures");Directory.CreateDirectory(folder);
        using(var review=new TacticalArena(new Rect(0,0,960,720)))
        {
            int resources=0,pool=0;
            for(int pass=0;pass<2;pass++)
            {
                int id=0;
                foreach(var skill in DigimonSkillCatalog.All)
                {
                    foreach(float age in new[]{-.1f,0,skill.windup*.5f,skill.windup+skill.travel*.4f,skill.Impact+.02f,skill.Impact+.15f,skill.Duration-.001f,skill.Duration+.01f})
                    {
                        review.BeginFrame(-1,-1,-1,false);review.DrawSkill(skill,new Vector3(-1,0,0),new Vector3(1,0,0),age,1000+id);review.Render();
                        Require(review.SkillEffectsHealthy(),"finite bounded pooled skill effects "+skill.id+" at "+age);
                        if(age<0||age>=skill.Duration)Require(review.EffectCount==0,"no effects outside real skill timeline "+skill.id);
                    }
                    id++;
                }
                // A full 7v7 burst stays within the existing visual budget.
                review.BeginFrame(-1,-1,-1,false);
                string[] burst={"seraphimon","hououmon","togemon","metalgreymon","metalgarurumon","herakle","wargreymon"};
                for(int i=0;i<14;i++)
                {
                    var skill=DigimonSkillCatalog.Find(burst[i%7]);
                    review.DrawSkill(skill,new Vector3(i%7-3,0,i/7*3-1.5f),new Vector3(0,0,0),skill.windup+skill.travel*.6f,2000+i);
                }
                review.Render();Require(review.SkillEffectsHealthy(),"fourteen simultaneous skills remain within effect budget");
                if(pass==0){resources=review.OwnedResourceCount;pool=review.SkillPoolSize;}
                else Require(resources==review.OwnedResourceCount&&pool==review.SkillPoolSize,"warmed-up effects reuse arena resources and renderers");
            }
            Require(review.SkillSphereFacesOutward(),"energy sphere normals face outward for correct volume shading");
            Require(review.SkillFinishResourcesValid,"actual player supports alpha skill materials");
            review.BeginFrame(-1,-1,-1,false);review.Render();
            Require(review.EffectCount==0&&review.SkillEffectsHealthy(),"all pooled effects hide after casts end");
            Debug.Log("SKILL FINISH RESOURCES "+resources+" / pooled renderers "+pool);
        }
        foreach(string id in new[]{"greymon","metalgreymon","metalgarurumon","wargreymon"})
        foreach(bool quality in new[]{false,true})
        using(var review=new TacticalArena(new Rect(0,0,960,720),true,quality))
        {
            review.UseModelStudio(Vector3.zero);review.SetView(new Vector3(-.2f,2.8f,-6.8f),new Vector3(0,.8f,0),40);
            var skill=DigimonSkillCatalog.Find(id);
            foreach(bool impact in new[]{false,true})
            {
                float age=impact?skill.Impact+.09f:skill.windup+skill.travel*.6f;
                Vector3 origin=new Vector3(-1.2f,.12f,0),target=new Vector3(1.2f,.12f,0);
                review.BeginFrame(-1,-1,-1,false);
                review.SetActor(0,origin,null,Color.green);review.FaceActor(0,Vector3.right);review.PoseDigimon(0,id,0,0,age,castAge:age);
                review.SetActor(1,target,null,Color.red);review.FaceActor(1,Vector3.left);review.PoseDigimon(1,"devimon",0,0,age);
                review.DrawSkill(skill,origin,target,age,100);review.Render();
                Require(review.SkillEffectsHealthy(),"signature review renders "+id);
                var old=RenderTexture.active;var texture=new Texture2D(review.Texture.width,review.Texture.height,TextureFormat.RGB24,false);
                try
                {
                    RenderTexture.active=review.Texture;texture.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);texture.Apply();
                    File.WriteAllBytes(Path.Combine(folder,id+(impact?"-impact":"-flight")+(quality?"-enhanced":"-baseline")+".png"),texture.EncodeToPNG());
                }
                finally{RenderTexture.active=old;Destroy(texture);}
            }
        }
        Debug.Log("SKILL FINISH COMPLETE: "+(validationChecks-first)+" checks / 34 techniques / four rendered comparisons");
    }
}
#endif
