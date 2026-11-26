#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    struct BoneState {public Vector3 position,scale;public Quaternion rotation;}
    static BoneState[] Bones(FaithfulModelActor a){return a.Nodes.Select(n=>new BoneState{position=n.localPosition,scale=n.localScale,rotation=n.localRotation}).ToArray();}
    static bool SameBones(FaithfulModelActor a,BoneState[] expected)
    {
        for(int i=0;i<a.Nodes.Length;i++)
            if((a.Nodes[i].localPosition-expected[i].position).sqrMagnitude>1e-8f||(a.Nodes[i].localScale-expected[i].scale).sqrMagnitude>1e-8f||Quaternion.Angle(a.Nodes[i].localRotation,expected[i].rotation)>.08f)return false;
        return true;
    }
    public void BeginAttackTimingSmoke(){StartCoroutine(AttackTimingSmoke());}
    IEnumerator AttackTimingSmoke()
    {
        Application.runInBackground=true;yield return null;
        ValidateAttackTiming();ValidateBasicAttacks();ValidateImpactMotion();
        yield return CaptureAttackTiming();
        Debug.Log("ATTACK TIMING SMOKE COMPLETE: "+validationChecks+" checks");Application.Quit();
    }
    void ValidateAttackTiming()
    {
        int before=validationChecks;float previousWindup=-1,previousRecovery=-1;
        for(int i=0;i<=100;i++)
        {
            float t=i/100f,w=BasicAttackPresentation.WindupPhase(t),r=BasicAttackPresentation.RecoveryPhase(t,1);
            Require(w>=previousWindup&&r>=previousRecovery&&w>=0&&r<=1,"preparation and follow-through phases are bounded and monotonic");previousWindup=w;previousRecovery=r;
        }
        Require(BasicAttackPresentation.WindupPhase(0)==0&&BasicAttackPresentation.WindupPhase(1)==1&&BasicAttackPresentation.RecoveryPhase(0,1)==0&&BasicAttackPresentation.RecoveryPhase(1,1)==1,"timing preserves exact authored endpoints");
        Require(BasicAttackPresentation.WindupPhase(.5f)<.5f&&BasicAttackPresentation.RecoveryPhase(.1f,1)<.05f,"preparation accelerates into the hit and gives the release a short visual accent");
        foreach(string id in DigimonModelLibrary.Roster)
        {
            var root=new GameObject("Attack timing "+id);var a=new GameObject("display").AddComponent<FaithfulModelActor>();var reference=new GameObject("reference").AddComponent<FaithfulModelActor>();
            a.transform.SetParent(root.transform,false);reference.transform.SetParent(root.transform,false);a.Initialize(id,0);reference.Initialize(id,0);
            try
            {
                foreach(int star in new[]{1,3})
                {
                    float size=DigimonVisualScale.Height(id,star);a.SetSize(size);reference.SetSize(size);
                    a.Pose(0,0,Vector3.forward,null,-1,0,0,0);reference.Pose(0,0,Vector3.forward,null,-1,0,0,0);
                    float attackRelease=a.Data.clips["Attack"].duration*a.Data.technique.attackRelease;
                    reference.Sample("Attack",attackRelease,true);var authored=Bones(reference);Vector3 launch=reference.BasicEmitter();
                    a.Pose(.1f,0,Vector3.forward,null,-1,.35f,0,1);
                    Require(a.Motion=="Attack"&&SameBones(a,authored),"actual skeleton reaches authored attack release on the damage event without an idle blend "+id+":"+star);
                    a.Pose(.18f,0,Vector3.forward,null,-1,.27f,0,1);var displayed=Bones(a);float time=a.MotionTime;Vector3 rootPosition=a.transform.localPosition;Quaternion rootRotation=a.transform.localRotation;
                    Require((a.BasicEmitter(true)-launch).magnitude<.001f,"late basic projectile uses the original release joint instead of the current follow-through "+id+":"+star);
                    Require(SameBones(a,displayed)&&a.MotionTime==time&&a.transform.localPosition==rootPosition&&a.transform.localRotation==rootRotation,"release lookup preserves every displayed bone and root "+id);
                    a.Pose(.20f,0,Vector3.forward,null,-1,.35f,0,2);Require(SameBones(a,authored),"rapid second attack restarts at its own release instead of blending from the old swing "+id);
                    a.Pose(.34f,0,Vector3.forward,null,-1,.23f,0,3);
                    float expected=a.Data.clips["Attack"].duration*(a.Data.technique.attackRelease+(1-a.Data.technique.attackRelease)*BasicAttackPresentation.RecoveryPhase(.12f,BasicAttackPresentation.Recovery(id)));
                    reference.Sample("Attack",expected,true);Require(SameBones(a,Bones(reference)),"first observation after a skipped frame uses current event age "+id);
                    var skill=DigimonSkillCatalog.Find(id);float skillRelease=a.Data.clips["Skill"].duration*a.Data.technique.skillRelease;
                    a.Pose(0,0,Vector3.forward,null,-1,0,0,0);
                    a.Pose(.04f,0,Vector3.forward,skill,skill.windup-.02f,0,0,0);
                    a.Pose(.06f,0,Vector3.forward,skill,skill.windup,0,0,0);reference.Sample("Skill",skillRelease,true);
                    Require(a.Motion=="Skill"&&SameBones(a,Bones(reference)),"skill release aligns actual bones even when the windup blend was still running "+id);
                    Vector3 skillLaunch=reference.SkillEmitter(false);
                    a.Pose(0,0,Vector3.forward,null,-1,0,0,0);
                    a.Pose(.1f,0,Vector3.forward,skill,skill.windup+.07f,0,0,0);
                    expected=a.Data.clips["Skill"].duration*(a.Data.technique.skillRelease+(1-a.Data.technique.skillRelease)*(.07f/(skill.Duration-skill.windup)));
                    reference.Sample("Skill",expected,true);Require(SameBones(a,Bones(reference)),"late skill observation displays current authored follow-through "+id);
                    displayed=Bones(a);time=a.MotionTime;
                    Require((a.SkillEmitter(true)-skillLaunch).magnitude<.001f&&SameBones(a,displayed)&&a.MotionTime==time,"late skill emitter stays at canonical launch while preserving visible pose "+id);
                    a.Pose(.1f,0,Vector3.forward,skill,skill.windup+.07f,0,0,0);Require(SameBones(a,displayed),"same timestamp repaint cannot restart or drift a committed action "+id);
                    a.Pose(.2f,0,Vector3.forward,skill,skill.windup+.1f,0,.2f,0);Require(a.Motion=="Down","death still owns the pose over release synchronization "+id);
                    a.Pose(0,0,Vector3.forward,null,-1,0,0,0);Require(a.Motion=="Idle","rewind clears committed attack/skill state "+id);
                }
            }
            finally{DestroyImmediate(root);}
        }
        using(var arena=new TacticalArena(new Rect(0,0,640,360)))arena.ValidateReleaseAnchors(Require);
        Debug.Log("ATTACK TIMING RULES COMPLETE: "+(validationChecks-before)+" checks / 47 species / stars 1 and 3 / actual authored bone parity, skipped frames, chained attacks and immutable launch anchors");
    }
    IEnumerator CaptureAttackTiming(bool ribbons=false,bool contact=false)
    {
        string folder=Path.Combine(Application.dataPath,contact?"../ContactMotionCaptures":ribbons?"../SkillRibbonCaptures":"../AttackTimingCaptures");Directory.CreateDirectory(folder);
        string[] ids=contact?new[]{"garurumon","togemon","atlur","mochimon","rosemon","agumon"}:ribbons?new[]{"herakle","rosemon","togemon","piyomon","shellmon","leomon"}:new[]{"agumon","ogremon","weregarurumon","gabumon","metalgreymon","seraphimon"};
        using(var arena=new TacticalArena(new Rect(0,0,640,420)))
        {
            arena.UseModelStudio(Vector3.zero);var texture=new Texture2D(arena.Texture.width,arena.Texture.height,TextureFormat.RGB24,false);int clip=0;
            try
            {
                foreach(string id in ids)foreach(bool basic in new[]{true,false})
                {
                    var skill=DigimonSkillCatalog.Find(id);float windup=basic?BasicAttackPresentation.Windup(id):skill.windup,window=basic?windup+.6f:skill.Duration+.25f;
                    float height=Mathf.Max(DigimonVisualScale.Height(id),DigimonVisualScale.Height("greymon"));
                    arena.SetView(new Vector3(-.2f,height+.8f,-6.3f-height*.4f),new Vector3(0,height*.45f,0),36);
                    clip++;string name=id+(basic?"-basic":"-skill");
                    for(int frame=0;frame<48;frame++)
                    {
                        float time=window*frame/47f,age=time-windup;int serial=age>=0?clip:clip-1;
                        Vector3 origin=new Vector3(-1.0f,.12f,0),target=new Vector3(1.1f,.12f,0);
                        arena.BeginFrame(-1,-1,-1,false);arena.SetActor(0,origin,null,Color.cyan);arena.FaceActor(0,Vector3.right);
                        arena.CombatMotion(0,serial,age<0?-age:-1,age<0);
                        arena.PoseDigimon(0,id,0,0,clip*10+time,basic?-1:(time<skill.Duration?time:-1),basic&&age>=0?Mathf.Max(0,.35f-age):0);
                        arena.SetActor(1,target,null,Color.red);arena.FaceActor(1,Vector3.left);arena.PoseDigimon(1,"greymon",0,0,clip*10+time);
                        if(basic)arena.DrawBasicAttack(0,id,target+Vector3.up*DigimonVisualScale.Height("greymon")*.55f,age,clip,targetSurface:arena.ContactSurface(1,CombatContactPresentation.Height("greymon"),CombatContactPresentation.Surface("greymon")));
                        else arena.DrawSkill(skill,origin,target,time,5000+clip,"greymon",1,arena.ContactSurface(1,CombatContactPresentation.Height("greymon"),CombatContactPresentation.Surface("greymon")));
                        yield return null;arena.Render();var old=RenderTexture.active;
                        try{RenderTexture.active=arena.Texture;texture.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(folder,name+"-"+frame.ToString("D2")+".png"),texture.EncodeToPNG());}
                        finally{RenderTexture.active=old;}
                        Require(arena.SkillEffectsHealthy(),"live sequence keeps healthy pooled effects "+name+":"+frame);
                    }
                    File.WriteAllText(Path.Combine(folder,name+".json"),"{\"id\":\""+id+"\",\"basic\":"+(basic?"true":"false")+",\"frames\":48,\"seconds\":"+window.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");
                }
            }
            finally{Destroy(texture);}
        }
        Debug.Log((contact?"CONTACT MOTION":ribbons?"SKILL RIBBON":"ATTACK TIMING")+" CAPTURES COMPLETE: 12 motion sequences / 576 actual Unity frames");
    }
}
public sealed partial class TacticalArena
{
    public void ValidateReleaseAnchors(Action<bool,string> require)
    {
        foreach(string id in DigimonModelLibrary.Roster)
        {
            BeginFrame(-1,-1,-1,false);SetActor(0,Vector3.zero,null,Color.cyan);FaceActor(0,Vector3.right);CombatMotion(0,1,-1,false);PoseDigimon(0,id,0,0,1,attack:.27f);
            Vector3 canonical,live;require(BasicEmitter(0,out canonical)&&BasicEmitter(0,out live,false),"native basic emitter resolves current actor "+id);
            DrawBasicAttack(0,id,Vector3.right+Vector3.up,.08f,1);Render();
            require((actors[0].basicFrom-canonical).sqrMagnitude<1e-8f,"online first observed launch uses canonical attack joint "+id);
            BeginFrame(-1,-1,-1,false);SetActor(0,Vector3.right*.4f,null,Color.cyan);FaceActor(0,Vector3.right);CombatMotion(0,1,-1,false);PoseDigimon(0,id,0,0,1.04f,attack:.23f);
            DrawBasicAttack(0,id,Vector3.right*2+Vector3.up,.12f,1);Render();
            require((actors[0].basicFrom-canonical).sqrMagnitude<1e-8f&&actors[0].basicTo==Vector3.right+Vector3.up,"a released projectile does not follow movement, recoil or retargeting "+id);
        }
    }
}
#endif
