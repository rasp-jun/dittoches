#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    void ValidateContactFinish()
    {
        int before=validationChecks;var previousArena=arena;var casts=skillCasts.ToArray();var traces=battleTraces.ToArray();int previousArt=artPack;
        using(var review=new TacticalArena(new Rect(0,0,640,360)))
        {
            review.ValidateContactFinish(Require);arena=review;skillCasts.Clear();battleTraces.Clear();artPack=0;
            try
            {
                var skill=DigimonSkillCatalog.Find("agumon");
                foreach(string id in DigimonModelLibrary.Roster)foreach(int star in new[]{1,3})
                {
                    var target=new Fighter{unit=new Unit(RosterById[id]){star=star},renderPos=new Vector2(3,3)};
                    var source=new Fighter{unit=new Unit(RosterById["agumon"]),renderPos=new Vector2(2,4),attacks=1};
                    var cast=new SkillCast{caster=source,target=target,skill=skill,origin=new Vector2(2,4),aim=new Vector2(3,3),age=skill.windup+skill.travel*.5f,presentationId=301};
                    skillCasts.Clear();skillCasts.Add(cast);review.BeginFrame(-1,-1,-1,false);DrawDigimonSkills();review.Render();
                    review.RequireFireContact(TacticalArena.CellWorld(2,4),TacticalArena.CellWorld(3,3),DigimonVisualScale.Height(id,star)*.55f,Require);
                    RecordAttackTrace(source,target);var trace=battleTraces.Last();float depth=CombatContactPresentation.Surface(id,star);
                    Require(trace.targetSurface==depth&&Mathf.Abs(trace.to.y-(TacticalArena.CellWorld(3,3).y+DigimonVisualScale.Height(id,star)*.55f))<.001f,"native basic records original target size and torso height "+id);
                    target.unit.star=2;target.renderPos=new Vector2(4,1);
                    Require(trace.targetSurface==depth&&trace.to==TacticalArena.CellWorld(3,3)+Vector3.up*DigimonVisualScale.Height(id,star)*.55f,"released native basic cannot change surface depth or endpoint after movement "+id);
                }
            }
            finally{arena=previousArena;skillCasts.Clear();skillCasts.AddRange(casts);battleTraces.Clear();battleTraces.AddRange(traces);artPack=previousArt;}
        }
        Debug.Log("CONTACT FINISH RULES COMPLETE: "+(validationChecks-before)+" checks / 47 target species, 1 and 3 stars, native host aim, immutable basic surfaces and five melee identities");
    }
    IEnumerator CaptureTargetContact()
    {
        string folder=Path.Combine(Application.dataPath,"../ContactTargetCaptures");Directory.CreateDirectory(folder);
        string[] ids={"koromon","agumon","greymon","metalgreymon","herakle","hououmon"};
        using(var review=new TacticalArena(new Rect(0,0,960,640)))
        {
            review.UseModelStudio(Vector3.zero);var texture=new Texture2D(review.Texture.width,review.Texture.height,TextureFormat.RGB24,false);int serial=0;
            try
            {
                foreach(string id in ids)foreach(int star in new[]{1,3})foreach(bool impact in new[]{false,true})
                {
                    var s=DigimonSkillCatalog.Find("gabumon");float age=impact?s.Impact+.06f:s.windup+s.travel*.9f;
                    float height=Mathf.Max(DigimonVisualScale.Height(id,star),DigimonVisualScale.Height("gabumon"));Vector3 from=new Vector3(-1.1f,.12f,0),to=new Vector3(1.1f,.12f,0);
                    review.SetView(new Vector3(-.3f,height+1.1f,-6.5f-height*.5f),new Vector3(0,height*.42f,0),38);
                    review.BeginFrame(-1,-1,-1,false);review.SetActor(0,from,null,Color.cyan);review.FaceActor(0,Vector3.right);review.PoseDigimon(0,"gabumon",0,0,10+age,age);
                    review.SetActor(1,to,null,Color.red);review.FaceActor(1,Vector3.left);review.PoseDigimon(1,id,0,0,10+age,star:star);
                    review.DrawSkill(s,from,to,age,++serial,id,star,review.ContactSurface(1,CombatContactPresentation.Height(id,star),CombatContactPresentation.Surface(id,star)));yield return null;review.Render();Require(review.SkillEffectsHealthy(),"actual target height capture "+id+":"+star);
                    var old=RenderTexture.active;
                    try{RenderTexture.active=review.Texture;texture.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(folder,id+"-"+star+(impact?"-impact":"-flight")+".png"),texture.EncodeToPNG());}
                    finally{RenderTexture.active=old;}
                }
            }
            finally{Destroy(texture);}
        }
        Debug.Log("CONTACT TARGET CAPTURES COMPLETE: 6 targets / 1 and 3 stars / 24 actual flight and impact frames");
    }
}
public sealed partial class TacticalArena
{
    public void RequireFireContact(Vector3 origin,Vector3 target,float height,Action<bool,string> require)
    {
        var skill=DigimonSkillCatalog.Find("agumon");Vector3 expected=Vector3.Lerp(origin+Vector3.up*(.75f*skill.size+skill.hover),target+Vector3.up*height,.5f);
        require(EffectCount>0&&(effectPool[0].transform.localPosition-expected).magnitude<.001f,"actual flame mesh path uses the original target's torso height");
    }
    public void ValidateContactFinish(Action<bool,string> require)
    {
        var skill=DigimonSkillCatalog.Find("agumon");Vector3 origin=Vector3.left,target=Vector3.right;
        foreach(string id in DigimonModelLibrary.Roster)foreach(int star in new[]{1,3})
        {
            float height=DigimonVisualScale.Height(id,star)*.55f,surface=CombatContactPresentation.Surface(id,star);
            require(Mathf.Abs(CombatContactPresentation.Height(id,star)-height)<.0001f&&surface>=.14f&&surface<=.5f,"contact profile follows roster stage/star within bounded depth "+id);
            BeginFrame(-1,-1,-1,false);DrawSkill(skill,origin,target,skill.windup+skill.travel*.5f,22,id,star);Render();RequireFireContact(origin,target,height,require);
            BeginFrame(-1,-1,-1,false);DrawSkill(skill,origin,target,skill.Impact+.06f,22,id,star);Render();
            float offset=Mathf.Max(surface,Mathf.Max(.34f,Mathf.Clamp(skill.radius*.22f,.20f,.37f)*1.4f));Vector3 torso=target+Vector3.up*height;
            Vector3 expected=torso+(root.transform.InverseTransformPoint(camera.transform.position)-torso).normalized*offset-camera.transform.forward*.025f;
            require(EffectCount>1&&(effectPool[1].transform.localPosition-expected).magnitude<.001f,"actual impact glow meets target torso on visible surface "+id);
            require((Project(torso)-Project(ContactPoint(torso,offset))).sqrMagnitude<.001f,"moving an impact toward the camera cannot shift it off the target on screen "+id);
        }
        require(CombatContactPresentation.Height(null)==.65f&&CombatContactPresentation.Surface("missing")==.28f,"unknown legacy targets retain a stable visual fallback");
        foreach(string id in DigimonModelLibrary.Roster)foreach(int star in new[]{1,3})
        {
            BeginFrame(-1,-1,-1,false);SetActor("surface",Vector3.zero,null,Color.red);FaceActor("surface",Vector3.left);PoseDigimon("surface",id,0,0,1,star:star);
            float height=CombatContactPresentation.Height(id,star),fallback=CombatContactPresentation.Surface(id,star),depth=ContactSurface("surface",height,fallback);
            require(depth>=fallback&&depth<=3&&!float.IsNaN(depth),"actual model bounds give finite camera-facing contact depth "+id);
            require(ContactSurface("missing",height,fallback)==fallback,"missing actor keeps bounded species fallback");
            if(id=="hououmon")require(depth>fallback,"wide wings require actual model depth instead of the generic offset");
            BeginFrame(-1,-1,-1,false);Render();require(ContactSurface("surface",height,fallback)==fallback,"stale prior-frame model cannot change the contact surface");
        }
        int resources=0,pool=0;
        for(int pass=0;pass<2;pass++)
        {
            foreach(string id in new[]{"garurumon","togemon","atlur","mochimon","rosemon","devimon"})foreach(int star in new[]{1,3})foreach(float age in new[]{0f,.07f,.18f})
            {
                BeginFrame(-1,-1,-1,false);
                for(int i=0;i<14;i++)DrawBasicShape(id,new Vector3(i%7-3,.6f,i/7*3-1.5f),Vector3.up*.6f,age,i,star,.5f);
                Render();require(EffectCount>0&&SkillEffectsHealthy(),"fourteen distinct melee contact effects stay visible and bounded "+id);
            }
            if(pass==0){resources=OwnedResourceCount;pool=SkillPoolSize;}
            else require(resources==OwnedResourceCount&&pool==SkillPoolSize,"melee contacts reuse existing meshes and warmed renderers");
        }
        BeginFrame(-1,-1,-1,false);SetActor(0,Vector3.zero,null,Color.cyan);PoseDigimon(0,"agumon",0,0,1);DrawBasicAttack(0,"agumon",Vector3.right,.03f,1,1,.5f);Render();
        BeginFrame(-1,-1,-1,false);SetActor(0,Vector3.zero,null,Color.cyan);PoseDigimon(0,"agumon",0,0,1.04f);DrawBasicAttack(0,"agumon",Vector3.left,.07f,1,1,.14f);Render();
        require(actors[0].basicTo==Vector3.right&&actors[0].basicSurface==.5f,"online cached basic preserves target surface alongside original destination");
        BeginFrame(-1,-1,-1,false);Render();require(EffectCount==0&&SkillEffectsHealthy(),"melee contact effects clear after their frame");
    }
}
public sealed partial class MultiLauncher
{
    void ValidateContactReplay()
    {
        var prior=arena;int before=onlineChecks;
        using(var review=new TacticalArena(new Rect(0,0,640,360)))
        {
            arena=review;
            try
            {
                var skill=DigimonSkillCatalog.Find("agumon");float age=skill.windup+skill.travel*.5f;int serial=70;
                foreach(string id in new[]{"koromon","agumon","greymon","metalgreymon","herakle","hououmon"})foreach(int star in new[]{1,3})foreach(int side in new[]{0,1})
                {
                    var target=new Fighter{key=2,id=id,star=star,hp=100};var cast=new SkillEvent{id="agumon",serial=++serial,caster=1,target=2,started=0,sx=2,sy=4,tx=3,ty=3};
                    var room=new Room{side=side,battleDuration=8,frames=new[]{new Frame{time=0,units=new[]{target}},new Frame{time=1,units=new[]{new Fighter{key=2,id="tokomon",star=1,hp=0}}}},skillEvents=new[]{cast}};
                    review.BeginFrame(-1,-1,-1,false);DrawMatchSkills(room,8-age);review.Render();
                    review.RequireFireContact(TacticalArena.CellWorld(side==0?2:4,side==0?4:3),TacticalArena.CellWorld(3,side==0?3:4),DigimonVisualScale.Height(id,star)*.55f,OnlineRequire);
                    OnlineRequire(target.hp==100&&target.id==id&&cast.tx==3&&cast.ty==3,"contact replay does not alter fighter state or server target coordinates");
                    cast.target=99;review.BeginFrame(-1,-1,-1,false);DrawMatchSkills(room,8-age);review.Render();
                    review.RequireFireContact(TacticalArena.CellWorld(side==0?2:4,side==0?4:3),TacticalArena.CellWorld(3,side==0?3:4),.65f,OnlineRequire);
                }
            }
            finally{arena=prior;}
        }
        Debug.Log("CONTACT REPLAY COMPLETE: "+(onlineChecks-before)+" checks / original target profile, both board orientations and missing target fallback");
    }
}
#endif
