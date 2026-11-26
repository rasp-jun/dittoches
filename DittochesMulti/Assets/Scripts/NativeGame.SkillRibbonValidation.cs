#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    void ValidateSkillRibbons()
    {
        int before=validationChecks;
        using(var arena=new TacticalArena(new Rect(0,0,640,360)))arena.ValidateSkillRibbons(Require);
        Debug.Log("SKILL RIBBON RULES COMPLETE: "+(validationChecks-before)+" checks / soft geometry, directional endpoints, transparent edges, temporal cleanup and warmed resource reuse");
    }
}
public sealed partial class TacticalArena
{
    public void ValidateSkillRibbons(Action<bool,string> require)
    {
        var mesh=SkillBeamMesh();var vertices=mesh.vertices;var colors=mesh.colors;
        require(vertices.Length==25&&mesh.triangles.Length==96,"beam uses one shared smooth 5 by 5 surface");
        for(int i=0;i<vertices.Length;i++)require(!float.IsNaN(vertices[i].sqrMagnitude)&&colors[i].a>=0&&colors[i].a<=1,"beam vertices and alpha remain finite and bounded");
        for(int x=0;x<5;x++)require(colors[x*5].a==0&&colors[x*5+4].a==0&&colors[x*5+2].a>0,"both beam edges disappear while the center remains visible");
        foreach(Vector3 direction in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back,Vector3.up,camera.transform.forward,new Vector3(1,1,1).normalized})
        {
            BeginFrame(-1,-1,-1,false);Vector3 from=new Vector3(.3f,.7f,-.2f),to=from+direction*2;
            SkillBeam(from,to,.1f,Color.cyan);Render();require(EffectCount==1&&SkillEffectsHealthy(),"beam renders in each horizontal, vertical and camera-aligned direction");
            var transform=effectPool[0].transform;
            require((root.transform.InverseTransformPoint(transform.TransformPoint(Vector3.left*.5f))-from).magnitude<.001f&&(root.transform.InverseTransformPoint(transform.TransformPoint(Vector3.right*.5f))-to).magnitude<.001f,"ribbon endpoints match the actual attack path in every direction");
        }
        BeginFrame(-1,-1,-1,false);SkillBeam(Vector3.zero,Vector3.zero,.1f,Color.white);SkillBeam(Vector3.zero,Vector3.right,0,Color.white);SkillBeam(Vector3.zero,Vector3.right,.1f,Color.clear);Render();
        require(EffectCount==0&&SkillEffectsHealthy(),"empty or transparent beams cannot leave invalid or lingering geometry");
        string[] ids={"tentomon","herakle","machinedramon","leomon","shellmon","palmon","rosemon","togemon","piyomon","atlur","koromon","patamon"};int resources=0,pool=0,peak=0;
        for(int pass=0;pass<2;pass++)
        {
            foreach(string id in ids)
            {
                var skill=DigimonSkillCatalog.Find(id);
                foreach(float progress in new[]{0f,.1f,.5f,.95f})
                {
                    BeginFrame(-1,-1,-1,false);float age=skill.windup+skill.travel*progress;
                    for(int i=0;i<14;i++)DrawSkill(skill,new Vector3(i%7-3,0,i/7*3-1.5f),Vector3.zero,age,10000+i);
                    Render();peak=Mathf.Max(peak,EffectCount);require(SkillEffectsHealthy()&&EffectCount>0,"fourteen ribbon skill casts remain visible and below cap "+id+":"+progress);
                }
                BeginFrame(-1,-1,-1,false);DrawSkill(skill,Vector3.left,Vector3.right,skill.Duration,10000);Render();require(EffectCount==0&&SkillEffectsHealthy(),"ribbon skill clears at its actual lifetime "+id);
            }
            foreach(string id in new[]{"tentomon","kabuterimon","shellmon","tsunomon","patamon","gabumon","piyomon"})foreach(int star in new[]{1,3})foreach(float progress in new[]{0f,.5f,.95f})
            {
                BeginFrame(-1,-1,-1,false);
                for(int i=0;i<14;i++)DrawBasicShape(id,new Vector3(i%7-3,.6f,i/7*3-1.5f),Vector3.up*.6f,BasicAttackPresentation.Travel(id)*progress,i,star);
                Render();require(SkillEffectsHealthy()&&EffectCount>0,"fourteen refined ranged basics remain bounded across travel and star sizes "+id+":"+star+":"+progress);
            }
            if(pass==0){resources=OwnedResourceCount;pool=SkillPoolSize;}
            else require(resources==OwnedResourceCount&&pool==SkillPoolSize,"all new ribbon families reuse warmed meshes materials and renderers");
        }
        require(peak<EffectBudget,"ribbon presentation stays inside the shared effect cap");
    }
}
#endif
