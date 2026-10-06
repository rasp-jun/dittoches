using System;
using UnityEngine;

/// <summary>Allocation-free skeletal playback of the reviewed roster, driven by combat time.</summary>
public sealed class FaithfulModelActor : MonoBehaviour
{
    public FaithfulModelData Data {get;private set;}
    public Transform[] Nodes {get;private set;}
    public Renderer[] Renderers {get;private set;}
    public string Motion {get;private set;}
    public float MotionTime {get;private set;}
    Vector3[] p,s,fromP,fromS,previousP,velocity;
    Quaternion[] q,fromQ,previousQ,spin;
    Vector3[] emitterP,emitterS;Quaternion[] emitterQ;
    readonly System.Collections.Generic.Dictionary<string,Transform> bindings=new System.Collections.Generic.Dictionary<string,Transform>();
    float velocityDelta=.016f;
    float blendAge,blendDuration,lastTime=-1,attackAge=100,hitAge=100,phase,moveSpeed,yaw;
    float lastAttack,lastHit,lastCast=-1;int lastSerial=-1;
    float height=1,ground,modelScale=1;
    float hitStartedAt=-100,impactWeight,impactStart,lastHitAt=float.NaN;
    Vector3 impactBack=Vector3.back;
    static int nextPhase;
    readonly float idleOffset=(nextPhase++%257)*.031f;
    public Transform Mouth {get;private set;}

    public void Initialize(string id,int layer)
    {
        Data=FaithfulModelData.Acquire(id);int count=Data.nodes.Length;
        Nodes=new Transform[count];p=new Vector3[count];s=new Vector3[count];q=new Quaternion[count];
        fromP=new Vector3[count];fromS=new Vector3[count];fromQ=new Quaternion[count];previousP=new Vector3[count];velocity=new Vector3[count];previousQ=new Quaternion[count];spin=new Quaternion[count];
        for(int i=0;i<count;i++)spin[i]=Quaternion.identity;
        emitterP=new Vector3[count];emitterS=new Vector3[count];emitterQ=new Quaternion[count];
        for(int i=0;i<count;i++)Nodes[i]=new GameObject(Data.nodes[i].name){layer=layer}.transform;
        for(int i=0;i<count;i++)
        {
            var node=Data.nodes[i];Nodes[i].SetParent(node.parent<0?transform:Nodes[node.parent],false);
            Nodes[i].localPosition=node.p;Nodes[i].localRotation=node.q;Nodes[i].localScale=node.s;
            if(node.name=="Head"||node.name=="Jaw")Mouth=Nodes[i];
            bindings[node.name]=Nodes[i];
        }
        Renderers=new Renderer[Data.parts.Length];
        for(int i=0;i<Data.parts.Length;i++)
        {
            var part=Data.parts[i];var obj=new GameObject("Surface "+i){layer=layer};obj.transform.SetParent(Nodes[part.node],false);
            Renderer renderer;
            if(part.joints.Length>0)
            {
                var skin=obj.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=part.mesh;
                var bones=new Transform[part.joints.Length];for(int j=0;j<bones.Length;j++)bones[j]=Nodes[part.joints[j]];
                skin.bones=bones;skin.rootBone=transform;skin.updateWhenOffscreen=true;skin.quality=SkinQuality.Bone4;
                skin.localBounds=new Bounds(Vector3.zero,Vector3.one*20);renderer=skin;
            }
            else{obj.AddComponent<MeshFilter>().sharedMesh=part.mesh;renderer=obj.AddComponent<MeshRenderer>();}
            renderer.sharedMaterial=Data.materials[Mathf.Max(0,part.material)];
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;renderer.receiveShadows=true;Renderers[i]=renderer;
        }
        Sample("Idle",0,true);
        // Bake actual rest geometry; source meshes can have non-identity ancestors.
        Bounds bounds=Data.hasBounds?Data.restBounds:GeometryBounds();
        if(!Data.hasBounds){Data.restBounds=bounds;Data.hasBounds=true;}
        height=Mathf.Max(.1f,bounds.size.y);ground=bounds.min.y;
        yaw=180;transform.localRotation=Quaternion.Euler(0,yaw,0);
    }
    public void SetSize(float wantedHeight)
    {
        modelScale=wantedHeight/height;
        transform.localScale=Vector3.one*modelScale;
        transform.localPosition=Vector3.up*(-ground*modelScale);
    }
    static float ImpactEnvelope(float age,float start)
    {return age<.035f?Mathf.Lerp(start,1,Smooth(age/.035f)):1-Smooth((age-.035f)/.185f);}
    void ApplyImpact(float death)
    {
        // A visual root layer leaves skeletal attack/skill timing and board coordinates intact.
        // Rotate about ground contact, not the source mesh origin (which differs by species).
        impactWeight=death>0?0:ImpactEnvelope(hitAge,impactStart);
        Quaternion facing=Quaternion.Euler(0,yaw,0);
        Quaternion recoil=Quaternion.AngleAxis(2.5f*impactWeight,Vector3.Cross(Vector3.up,impactBack));
        transform.localRotation=recoil*facing;
        float distance=Mathf.Clamp(height*modelScale*.032f,.012f,.065f)*impactWeight;
        transform.localPosition=impactBack*distance-transform.localRotation*(Vector3.up*(ground*modelScale));
    }
    /// <summary>Actual skinned geometry in actor-local space, independent of board/bench parent scale.</summary>
    public Bounds GeometryBounds()
    {
        var result=new Bounds();bool first=true;Matrix4x4 local=transform.worldToLocalMatrix;
        foreach(var renderer in Renderers)
        {
            var skin=renderer as SkinnedMeshRenderer;Mesh mesh=skin!=null?skin.sharedMesh:renderer.GetComponent<MeshFilter>().sharedMesh;
            var vertices=mesh.vertices;Matrix4x4[] matrices=null;BoneWeight[] weights=null;
            if(skin!=null)
            {
                var bones=skin.bones;var binds=mesh.bindposes;matrices=new Matrix4x4[bones.Length];weights=mesh.boneWeights;
                for(int b=0;b<bones.Length;b++)matrices[b]=local*bones[b].localToWorldMatrix*binds[b];
            }
            Matrix4x4 rigid=local*renderer.localToWorldMatrix;
            for(int i=0;i<vertices.Length;i++)
            {
                Vector3 point;
                if(skin==null)point=rigid.MultiplyPoint3x4(vertices[i]);
                else
                {
                    var w=weights[i];var v=vertices[i];
                    point=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+
                        matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;
                }
                if(first){result=new Bounds(point,Vector3.zero);first=false;}else result.Encapsulate(point);
            }
        }
        return result;
    }
    void OnDestroy(){if(Data!=null){Data.Release();Data=null;}}
    Vector3 Joint(string name,Vector3 fallback){Transform bone;return bindings.TryGetValue(name,out bone)?bone.position:fallback;}
    Vector3 LiveEmitter()
    {
        float h=height*transform.lossyScale.y;Vector3 front=-transform.forward;
        Vector3 chest=Joint("Spine",transform.position+Vector3.up*h*.6f),head=Joint("Head",chest+Vector3.up*h*.2f);
        Vector3 hands=(Joint("HandL",chest)+Joint("HandR",chest))*.5f;
        switch(Data.technique.emitter)
        {
            case "chest":return chest+front*h*.08f;
            case "hands":case "overhead":return hands;
            case "leftHand":return Joint("HandL",chest);
            case "rightHand":case "sword":return Joint("HandR",chest);
            case "wings":return (Joint("WingL",chest)+Joint("WingR",chest))*.5f;
            case "head":return head;
            default:return head+front*h*.12f;
        }
    }
    public Vector3 SkillEmitter(bool atRelease)
    {
        if(!atRelease)return LiveEmitter();
        // Sample the authored release, then restore the displayed pose exactly.
        for(int i=0;i<Nodes.Length;i++)
        {
            emitterP[i]=Nodes[i].localPosition;emitterQ[i]=Nodes[i].localRotation;emitterS[i]=Nodes[i].localScale;
            Nodes[i].localPosition=Data.nodes[i].p;Nodes[i].localRotation=Data.nodes[i].q;Nodes[i].localScale=Data.nodes[i].s;
        }
        var clip=Data.clips["Skill"];
        foreach(var track in clip.tracks)
        {
            Vector4 value=track.Sample(clip.duration*Data.technique.skillRelease);var node=Nodes[track.node];
            if(track.kind==0)node.localPosition=value;else if(track.kind==1)node.localRotation=new Quaternion(value.x,value.y,value.z,value.w);else node.localScale=value;
        }
        Vector3 point=LiveEmitter();
        for(int i=0;i<Nodes.Length;i++){Nodes[i].localPosition=emitterP[i];Nodes[i].localRotation=emitterQ[i];Nodes[i].localScale=emitterS[i];}
        return point;
    }
    static float Smooth(float u){u=Mathf.Clamp01(u);return u*u*u*(u*(u*6-15)+10);}
    public void Sample(string name,float time,bool immediate=false,float delta=0)
    {
        FaithfulModelData.Clip clip;if(!Data.clips.TryGetValue(name,out clip))clip=Data.clips["Idle"];
        if(Motion!=clip.name||immediate)
        {
            for(int i=0;i<Nodes.Length;i++){fromP[i]=Nodes[i].localPosition;fromQ[i]=Nodes[i].localRotation;fromS[i]=Nodes[i].localScale;}
            Motion=clip.name;blendAge=0;
            blendDuration=immediate?0:name=="Hit"?.09f:name=="Dodge"?.12f:name=="Attack"?.16f:name=="Idle"?.28f:.22f;
            if(immediate)Array.Clear(velocity,0,velocity.Length);
        }
        MotionTime=clip.Loop?Mathf.Repeat(time,clip.duration):Mathf.Clamp(time,0,clip.duration);
        for(int i=0;i<Nodes.Length;i++){p[i]=Data.nodes[i].p;q[i]=Data.nodes[i].q;s[i]=Data.nodes[i].s;previousP[i]=Nodes[i].localPosition;previousQ[i]=Nodes[i].localRotation;if(immediate)spin[i]=Quaternion.identity;}
        foreach(var track in clip.tracks)
        {
            Vector4 value=track.Sample(MotionTime);int i=track.node;
            if(track.kind==0)p[i]=value;else if(track.kind==1)q[i]=new Quaternion(value.x,value.y,value.z,value.w);else s[i]=value;
        }
        blendAge+=Mathf.Max(0,delta);float u=blendDuration>0?Mathf.Clamp01(blendAge/blendDuration):1,w=Smooth(u);
        for(int i=0;i<Nodes.Length;i++)
        {
            float travel=blendDuration*(u-u*u+u*u*u/3);
            Nodes[i].localPosition=Vector3.LerpUnclamped(fromP[i]+velocity[i]*travel,p[i],w);
            float angle;Vector3 axis;spin[i].ToAngleAxis(out angle,out axis);if(angle>180)angle-=360;
            Quaternion projected=fromQ[i];
            if(axis.sqrMagnitude>.01f&&!float.IsInfinity(axis.x))projected*=Quaternion.AngleAxis(Mathf.Clamp(angle*travel/velocityDelta,-17,17),axis);
            Nodes[i].localRotation=Quaternion.SlerpUnclamped(projected,q[i],w);
            Nodes[i].localScale=Vector3.LerpUnclamped(fromS[i],s[i],w);
            // A seek/first sample has no outgoing velocity from the pose before the reset.
            if(immediate){previousP[i]=Nodes[i].localPosition;previousQ[i]=Nodes[i].localRotation;}
        }
    }
    public void Pose(float time,float speed,Vector3 facing,DigimonSkillCatalog.Entry skill,float castAge,float attack,float death,int serial=-1,float cooldown=-1,bool inRange=false,float hit=0,float hitTime=float.NaN)
    {
        bool first=lastTime<0||time<lastTime;float dt=first?0:Mathf.Clamp(time-lastTime,0,.1f);lastTime=time;
        if(first){phase=0;attackAge=100;hitAge=100;hitStartedAt=-100;lastHitAt=float.NaN;impactWeight=impactStart=0;lastSerial=-1;lastCast=-1;lastHit=0;lastAttack=0;}
        if(serial>=0?(serial!=lastSerial&&serial>0&&(!first||attack>0)):(attack>lastAttack+.0001f))attackAge=Mathf.Clamp(.35f-attack,0,.35f);else attackAge+=dt;
        bool timedHit=!float.IsNaN(hitTime);
        bool newHit=timedHit?hitTime>=0&&hitTime<=time&&(float.IsNaN(lastHitAt)||hitTime>lastHitAt+.0001f):hit>lastHit+.0001f;
        if(newHit)
        {
            // Online uses the actual event stamp: consecutive snapshots can have equal flash time.
            // Local fallback is remaining flash seconds, not a normalized strength.
            float started=timedHit?hitTime:time-Mathf.Clamp(.18f-hit,0,.18f);
            impactStart=ImpactEnvelope(Mathf.Max(0,started-hitStartedAt),impactStart);
            hitStartedAt=started;if(timedHit)lastHitAt=hitTime;
        }
        hitAge=Mathf.Max(0,time-hitStartedAt);
        lastSerial=serial;lastAttack=attack;lastHit=hit;
        moveSpeed=Mathf.Lerp(moveSpeed,speed,first?1:1-Mathf.Exp(-dt*14));
        if((death<=0||first)&&facing.sqrMagnitude>.001f){float wanted=180+Mathf.Atan2(facing.x,facing.z)*Mathf.Rad2Deg;yaw=first?wanted:Mathf.LerpAngle(yaw,wanted,1-Mathf.Exp(-dt*14));}
        if(newHit)impactBack=Quaternion.Euler(0,yaw,0)*Vector3.forward;
        string motion="Idle";float age=time+idleOffset;
        bool casting=castAge>=0&&skill!=null&&castAge<skill.Duration;
        if(death>0){motion="Down";age=death*Data.clips["Down"].duration;}
        else if(casting)
        {
            motion="Skill";float duration=Data.clips[motion].duration;
            float release=Data.technique.skillRelease;
            age=castAge<skill.windup?duration*release*castAge/Mathf.Max(.01f,skill.windup):
                duration*(release+(1-release)*Mathf.Clamp01((castAge-skill.windup)/Mathf.Max(.01f,skill.Duration-skill.windup)));
            attackAge=100;
        }
        else if(attackAge<.35f)
        {motion="Attack";age=Data.clips[motion].duration*(Data.technique.attackRelease+(1-Data.technique.attackRelease)*attackAge/.35f);}
        else if(inRange&&cooldown>=0&&cooldown<.18f)
        {motion="Attack";age=Data.clips[motion].duration*Data.technique.attackRelease*(1-cooldown/.18f);}
        else if(hitAge<.22f){motion="Hit";age=Data.clips[motion].duration*hitAge/.22f;}
        else if(moveSpeed>.025f)
        {
            motion=moveSpeed>(Motion=="Run"?.60f:.78f)?"Run":"Walk";
            phase+=dt*moveSpeed/Mathf.Max(.15f,height*transform.localScale.x*.42f);
            age=phase*Data.clips[motion].duration;
        }
        if(first||casting&&(lastCast<0||castAge<lastCast))Motion=null;
        lastCast=castAge;
        // Capture outgoing velocity only when switching, never allocate per tick.
        if(Motion!=motion&&dt>0)
        {
            velocityDelta=dt;
            for(int i=0;i<Nodes.Length;i++){velocity[i]=Vector3.ClampMagnitude((Nodes[i].localPosition-previousP[i])/dt,1.5f);spin[i]=Quaternion.Inverse(previousQ[i])*Nodes[i].localRotation;}
        }
        Sample(motion,age,first,dt);
        ApplyImpact(death);
    }
}
