using System.Collections.Generic;
using UnityEngine;

/// <summary>Blended skeletal locomotion and combat poses. Animation never scales a portrait.</summary>
public sealed class DigimonRig
{
    public readonly GameObject root;
    public readonly SkinnedMeshRenderer renderer;
    public readonly DigimonModelLibrary.Model model;
    readonly Transform[] bones;
    readonly Vector3[] rest;
    readonly Dictionary<string,int> index=new Dictionary<string,int>();
    Vector3 previousPosition;
    float lastTime=-1,phase,moveBlend,yaw,desiredYaw=155;
    bool facingSet;
    public int BoneCount { get { return bones.Length; } }
    public Vector3 Mouth { get { return bones[index["Jaw"]].TransformPoint(new Vector3(0,.005f,.39f)); } }
    public DigimonRig(DigimonModelLibrary.Model model,Transform parent,Material material,int layer)
    {
        this.model=model;
        root=new GameObject(model.id+" / skeletal character"){layer=layer,hideFlags=HideFlags.HideAndDontSave};
        root.transform.SetParent(parent,false);
        var definition=model.skeleton;bones=new Transform[definition.names.Count];rest=new Vector3[bones.Length];
        for(int i=0;i<bones.Length;i++)
        {
            var bone=new GameObject(definition.names[i]){layer=layer,hideFlags=HideFlags.HideAndDontSave};
            int parentIndex=definition.parents[i];
            bone.transform.SetParent(parentIndex<0?root.transform:bones[parentIndex],false);
            rest[i]=definition.positions[i]-(parentIndex<0?Vector3.zero:definition.positions[parentIndex]);
            bone.transform.localPosition=rest[i];bones[i]=bone.transform;index.Add(definition.names[i],i);
        }
        renderer=root.AddComponent<SkinnedMeshRenderer>();renderer.sharedMesh=model.mesh;renderer.sharedMaterial=material;
        renderer.bones=bones;renderer.rootBone=bones[0];renderer.quality=SkinQuality.Bone2;
        renderer.localBounds=new Bounds(Vector3.up*.8f,new Vector3(3,3,3));
        renderer.updateWhenOffscreen=false;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        yaw=desiredYaw;
    }
    public void Face(Vector3 worldDirection)
    {
        worldDirection.y=0;if(worldDirection.sqrMagnitude<.0001f)return;
        desiredYaw=Mathf.Atan2(worldDirection.x,worldDirection.z)*Mathf.Rad2Deg;facingSet=true;
    }
    void Rotate(string name,float x,float y=0,float z=0)
    {int i;if(index.TryGetValue(name,out i))bones[i].localRotation=Quaternion.Euler(x,y,z);}
    static float Ease(float value){value=Mathf.Clamp01(value);return value*value*(3-2*value);}
    void Leg(string suffix,float cycle,float movement,float crouch)
    {
        float u=Mathf.Repeat(cycle/(Mathf.PI*2),1);
        // Long support phase, shorter lifted return. Feet move backwards relative to the advancing body.
        float forward=u<.62f?Mathf.Lerp(.15f,-.15f,u/.62f):Mathf.Lerp(-.15f,.15f,Ease((u-.62f)/.38f));
        float lift=u<.62f?0:Mathf.Sin((u-.62f)/.38f*Mathf.PI)*.105f;
        int h=index["Thigh"+suffix],k=index["Shin"+suffix],f=index["Foot"+suffix];
        Vector3 hip=root.transform.InverseTransformPoint(bones[h].position);
        Vector3 foot=new Vector3(suffix=="L"?-.235f:.235f,.11f+lift*movement,.025f+forward*movement);
        Vector3 d=foot-hip;float upper=rest[k].magnitude,lower=rest[f].magnitude;
        float distance=Mathf.Clamp(d.magnitude,.025f,upper+lower-.001f);d.Normalize();
        float along=(upper*upper-lower*lower+distance*distance)/(2*distance);
        float outwards=Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
        Vector3 bend=(Vector3.forward-d*Vector3.Dot(Vector3.forward,d)).normalized;
        Vector3 knee=hip+d*along+bend*outwards;
        bones[h].localRotation=Quaternion.FromToRotation(rest[k],bones[h].parent.InverseTransformVector(root.transform.TransformVector(knee-hip)));
        bones[k].localRotation=Quaternion.FromToRotation(rest[f],bones[k].parent.InverseTransformVector(root.transform.TransformVector(hip+d*distance-knee)));
        bones[f].rotation=root.transform.rotation;
    }
    public void Pose(Vector3 position,float speed,float time,DigimonSkillCatalog.Entry skill,float castAge,float attack,float death,int star)
    {
        float dt=lastTime<0?0:Mathf.Clamp(time-lastTime,0,.1f);
        Vector3 delta=lastTime<0?Vector3.zero:position-previousPosition;delta.y=0;
        if(time<lastTime){phase=0;moveBlend=0;}
        if(delta.sqrMagnitude>.00001f&&!facingSet)Face(delta);
        float actual=dt>0?delta.magnitude/dt:0;
        float wanted=Mathf.Clamp01(Mathf.Max(speed,actual)*.8f);
        moveBlend=Mathf.Lerp(moveBlend,wanted,1-Mathf.Exp(-dt*12));
        // Distance controls stride length; a viewer can supply speed for an in-place walk.
        phase+=delta.magnitude>.00001f?delta.magnitude*11f:dt*7f*moveBlend;
        yaw=Mathf.LerpAngle(yaw,desiredYaw,1-Mathf.Exp(-dt*12));facingSet=false;
        for(int i=0;i<bones.Length;i++){bones[i].localPosition=rest[i];bones[i].localRotation=Quaternion.identity;}
        float breath=Mathf.Sin(time*2.5f)*.010f,idle=Mathf.Sin(time*1.55f)*1.5f;
        float charge=0,release=0,settle=0,jaw=0;
        if(skill!=null&&castAge>=0&&castAge<skill.Duration)
        {
            if(castAge<skill.windup)charge=Ease(castAge/skill.windup);
            else
            {
                release=1-Ease((castAge-skill.windup)/Mathf.Max(.14f,skill.travel));
                settle=1-Ease((castAge-skill.Impact)/Mathf.Max(.1f,skill.recovery));
            }
            jaw=castAge<skill.windup?charge*12:release*32+settle*8;
        }
        float strike=attack>0?Mathf.Sin(Ease(1-Mathf.Clamp01(attack/.35f))*Mathf.PI):0;
        float crouch=charge*.035f+death*.25f;
        bones[0].localPosition+=new Vector3(0,breath-Mathf.Cos(phase*2)*.012f*moveBlend-crouch,charge*-.035f);
        Rotate("Hips",charge*-7+release*10,0,Mathf.Sin(phase)*2*moveBlend+death*76);
        Rotate("Spine",charge*-9+release*13+strike*8,Mathf.Sin(phase)*4*moveBlend+strike*18,idle*(1-moveBlend));
        Rotate("Head",charge*-11-release*6,Mathf.Sin(time*1.3f)*2*(1-moveBlend)-strike*12);
        Rotate("Jaw",jaw);
        float armSwing=Mathf.Sin(phase)*26*moveBlend;
        Rotate("UpperArmL",armSwing-charge*20+release*22,0,charge*-16-strike*25);
        Rotate("UpperArmR",-armSwing-charge*20+release*22-strike*65,0,charge*16);
        Rotate("ForearmL",-12-charge*20,0,-strike*25);
        Rotate("ForearmR",-12-charge*20-strike*35);
        Rotate("Tail",-charge*8,Mathf.Sin(phase-.65f)*12*moveBlend+Mathf.Sin(time*1.8f)*5);
        Rotate("TailMid",Mathf.Sin(time*2-.6f)*3,Mathf.Sin(phase-1.2f)*16*moveBlend+Mathf.Sin(time*1.8f-.7f)*7);
        Rotate("TailTip",0,Mathf.Sin(phase-1.8f)*20*moveBlend+Mathf.Sin(time*1.8f-1.3f)*9);
        Leg("L",phase,moveBlend,crouch);Leg("R",phase+Mathf.PI,moveBlend,crouch);
        root.transform.localRotation=Quaternion.Euler(0,yaw,0);
        root.transform.localPosition=new Vector3(0,-.045f,0);
        root.transform.localScale=Vector3.one*(.83f*(1+.055f*(star-1)));
        previousPosition=position;lastTime=time;
    }
}
