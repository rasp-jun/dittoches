#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Collections;
using System.IO;
using UnityEngine;

public sealed partial class NativeGame
{
    void ValidateImpactAccents()
    {
        int before=validationChecks;using(var review=new TacticalArena(new Rect(0,0,640,360)))review.ValidateImpactAccents(Require);
        Debug.Log("IMPACT ACCENTS RULES COMPLETE: "+(validationChecks-before)+" checks / transparent flare mesh, actual launch anchors, eight impact elements, timing, directions and warmed resource reuse");
    }
    IEnumerator CaptureImpactAccents()
    {
        string folder=Path.Combine(Application.dataPath,"../ImpactAccentCaptures");Directory.CreateDirectory(folder);
        string[] ids={"ogremon","agumon","herakle","metalgarurumon","rosemon","ophanimon","devimon","shellmon"};
        using(var review=new TacticalArena(new Rect(0,0,960,640)))
        {
            review.UseModelStudio(Vector3.zero);var texture=new Texture2D(review.Texture.width,review.Texture.height,TextureFormat.RGB24,false);int serial=0;
            try
            {
                foreach(string id in ids)foreach(bool basic in new[]{true,false})foreach(string phase in new[]{"release","contact","spread","fade"})
                {
                    var skill=DigimonSkillCatalog.Find(id);float travel=basic?BasicAttackPresentation.Travel(id):skill.travel;
                    float impact=basic?travel:skill.Impact,release=basic?0:skill.windup;
                    float age=phase=="release"?release+.015f:impact+(phase=="contact"?.018f:phase=="spread"?.09f:.175f);
                    float height=Mathf.Max(DigimonVisualScale.Height(id),DigimonVisualScale.Height("greymon"));
                    Vector3 from=new Vector3(-1.1f,.12f,0),to=new Vector3(1.1f,.12f,0);serial++;
                    review.SetView(new Vector3(-.3f,height*1.05f+1.2f,-6.5f-height*.6f),new Vector3(0,height*.42f,0),38);
                    review.BeginFrame(-1,-1,-1,false);review.SetActor(0,from,null,Color.cyan);review.FaceActor(0,Vector3.right);review.CombatMotion(0,serial,-1,false);
                    review.PoseDigimon(0,id,0,0,100+age,basic?-1:age,basic?Mathf.Max(.001f,.35f-age):0);
                    review.SetActor(1,to,null,Color.red);review.FaceActor(1,Vector3.left);review.PoseDigimon(1,"greymon",0,0,100+age);
                    float targetHeight=CombatContactPresentation.Height("greymon"),surface=review.ContactSurface(1,targetHeight,CombatContactPresentation.Surface("greymon"));
                    if(basic)review.DrawBasicAttack(0,id,to+Vector3.up*targetHeight,age,serial,targetSurface:surface);
                    else review.DrawSkill(skill,from,to,age,serial,"greymon",1,surface);
                    yield return null;review.Render();Require(review.SkillEffectsHealthy(),"accent capture remains finite "+id+"/"+phase);
                    var old=RenderTexture.active;
                    try{RenderTexture.active=review.Texture;texture.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(folder,id+(basic?"-basic-":"-skill-")+phase+".png"),texture.EncodeToPNG());}
                    finally{RenderTexture.active=old;}
                }
            }
            finally{Destroy(texture);}
        }
        Debug.Log("IMPACT ACCENTS CAPTURES COMPLETE: 8 species / basic and skill / four phases / 64 actual frames");
    }
}
public sealed partial class TacticalArena
{
    public void ValidateImpactAccents(Action<bool,string> require)
    {
        var mesh=ImpactFlareMesh();var vertices=mesh.vertices;var colors=mesh.colors;var triangles=mesh.triangles;
        require(vertices.Length==33&&triangles.Length==96&&colors[0].a==1,"one shared flare has a bright center and 32 triangles");
        for(int i=1;i<vertices.Length;i++)require(vertices[i].sqrMagnitude>0&&vertices[i].sqrMagnitude<=1.001f&&colors[i].a==0,"all flare tips are bounded and transparent "+i);
        for(int i=0;i<triangles.Length;i+=3)require(Vector3.Cross(vertices[triangles[i+1]]-vertices[triangles[i]],vertices[triangles[i+2]]-vertices[triangles[i]]).z>0,"flare triangle winding remains consistent "+i);
        int resources=0,pool=0,peak=0;
        for(int pass=0;pass<2;pass++)
        {
            foreach(CombatEffectStyle.Element element in Enum.GetValues(typeof(CombatEffectStyle.Element)))foreach(bool major in new[]{false,true})
            {
                foreach(Vector3 direction in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.up,camera.transform.forward,Vector3.zero})
                {
                    Vector3 source=new Vector3(.3f,.8f,-.2f);
                    BeginFrame(-1,-1,-1,false);CombatLaunch(element,source,direction,.015f,.16f,Color.cyan,major);Render();
                    require(EffectCount>=1&&SkillEffectsHealthy()&&(effectPool[0].transform.localPosition-(source-camera.transform.forward*.025f)).magnitude<.001f,"launch glow stays exactly on the emission joint for each direction and element");
                    BeginFrame(-1,-1,-1,false);CombatLaunch(element,source,direction,-.01f,.16f,Color.cyan,major);CombatLaunch(element,source,direction,major?.095f:.07f,.16f,Color.cyan,major);Render();require(EffectCount==0,"launch cannot appear early or linger after its short window");
                }
                foreach(float time in new[]{0f,.05f,.3f,.75f,.999f})
                {
                    BeginFrame(-1,-1,-1,false);for(int i=0;i<14;i++)CombatImpact(element,new Vector3(i%7-3,.8f,i/7),Vector3.right,time,.24f,i,Color.cyan,major,.5f);
                    Render();peak=Mathf.Max(peak,EffectCount);require((time>=.95f||EffectCount>0)&&EffectCount<EffectBudget&&SkillEffectsHealthy(),"fourteen impact accents remain finite and bounded across decay "+element+"/"+time);
                }
            }
            if(pass==0){resources=OwnedResourceCount;pool=SkillPoolSize;}
            else require(resources==OwnedResourceCount&&pool==SkillPoolSize,"warm accents reuse meshes materials and renderers without pool growth");
        }
        BeginFrame(-1,-1,-1,false);foreach(CombatEffectStyle.Element element in Enum.GetValues(typeof(CombatEffectStyle.Element)))CombatImpact(element,Vector3.zero,Vector3.right,1,.24f,1,Color.cyan,true);Render();require(EffectCount==0,"all impact families clear completely at their end time");
        BeginFrame(-1,-1,-1,false);CombatImpactAccent(CombatEffectStyle.Element.Force,Vector3.zero,Vector3.right,.6f,.2f,1,Color.white,true);CombatFlare(Vector3.zero,Vector3.right,0,Color.white);CombatFlare(Vector3.zero,Vector3.right,1,Color.clear);Render();
        require(EffectCount==0&&SkillEffectsHealthy(),"expired/empty accents leave no geometry");require(ReferenceEquals(mesh,ImpactFlareMesh()),"flare is cached for the arena lifetime");
    }
}
#endif
