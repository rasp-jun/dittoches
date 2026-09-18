using System.Collections.Generic;
using UnityEngine;

public sealed partial class TacticalArena
{
    // Reused across casts: no materials or GameObjects are allocated per projectile per frame.
    const int EffectBudget=1024;
    readonly List<MeshRenderer> effectPool=new List<MeshRenderer>();
    int effectCursor;
    Mesh effectSphere;
    void BeginEffects(){effectCursor=0;}
    void EndEffects(){for(int i=effectCursor;i<effectPool.Count;i++)effectPool[i].enabled=false;}
    public int EffectCount { get { return effectCursor; } }
    void Effect(Mesh mesh,Vector3 position,Vector3 scale,Color tint,Quaternion rotation)
    {
        if(effectCursor>=EffectBudget||scale.sqrMagnitude<.000001f)return;
        MeshRenderer renderer;
        if(effectCursor==effectPool.Count)
        {renderer=Shape("Skill effect",mesh,glow,position,scale);effectPool.Add(renderer);}
        else renderer=effectPool[effectCursor];
        effectCursor++;renderer.enabled=true;
        renderer.GetComponent<MeshFilter>().sharedMesh=mesh;
        renderer.transform.localPosition=position;renderer.transform.localScale=scale;
        renderer.transform.localRotation=rotation;Tint(renderer,tint);
    }
    Mesh EffectSphere()
    {
        if(effectSphere!=null)return effectSphere;
        const int lat=8,lon=12;var vertices=new Vector3[(lat+1)*(lon+1)];var triangles=new List<int>();
        for(int y=0;y<=lat;y++)for(int x=0;x<=lon;x++)
        {
            float a=y*Mathf.PI/lat,b=x*Mathf.PI*2/lon;
            vertices[y*(lon+1)+x]=new Vector3(Mathf.Sin(a)*Mathf.Cos(b),Mathf.Cos(a),Mathf.Sin(a)*Mathf.Sin(b));
            if(y==lat||x==lon)continue;int n=y*(lon+1)+x;
            triangles.Add(n);triangles.Add(n+lon+1);triangles.Add(n+1);
            triangles.Add(n+1);triangles.Add(n+lon+1);triangles.Add(n+lon+2);
        }
        effectSphere=Own(new Mesh{name="Skill sphere",vertices=vertices,triangles=triangles.ToArray()});return effectSphere;
    }
    void Orb(Vector3 p,float size,Color c)
    {
        Effect(EffectSphere(),p,Vector3.one*size,c,Quaternion.identity);
        Effect(EffectSphere(),p-camera.transform.forward*size*.8f,Vector3.one*size*.42f,Color.Lerp(c,Color.white,.78f),Quaternion.identity);
    }
    void Stroke(Vector3 a,Vector3 b,float width,Color color)
    {
        Vector3 delta=b-a;if(delta.sqrMagnitude<.00001f)return;
        Effect(cube,(a+b)*.5f,new Vector3(width,width,delta.magnitude),color,Quaternion.LookRotation(delta));
    }
    void Halo(Vector3 p,float radius,Color c,bool vertical=false)
    {Effect(ringMesh,p,Vector3.one*radius,c,vertical?camera.transform.rotation*Quaternion.Euler(90,0,0):Quaternion.identity);}
    static Vector3 Side(Vector3 direction)
    {Vector3 side=Vector3.Cross(Vector3.up,direction);return side.sqrMagnitude<.001f?Vector3.right:side.normalized;}
    public static float DigimonHeadHeight(string id,int star=1)
    {var s=DigimonSkillCatalog.Find(id);return s==null?1.42f:1.3f*s.size*(1+.055f*(star-1))+s.hover+.15f;}

    /// <summary>Use an authored skeletal model where available; legacy units retain their existing art.</summary>
    public void PoseDigimon(object key,string id,float speed,float direction,float time,float castAge=-1,float attack=0,float death=0,int star=1)
    {
        Actor actor;if(!actors.TryGetValue(key,out actor))return;
        var s=DigimonSkillCatalog.Find(id);if(s==null){PoseCombatActor(key,speed,direction,time,death);return;}
        if(PoseModel(actor,id,speed,time,s,castAge,attack,death,star))return;
        float dt=actor.poseTime<0?0:Mathf.Clamp(time-actor.poseTime,0,.1f);bool first=actor.poseTime<0;actor.poseTime=time;
        float blend=first?1:1-Mathf.Exp(-dt*14);
        actor.poseMove=Mathf.Lerp(actor.poseMove,Mathf.Clamp01(speed),blend);
        actor.posePhase+=dt*(3.2f+actor.poseMove*4.8f);
        float size=s.size*(1+.055f*(star-1)),move=actor.poseMove,phase=actor.posePhase;
        float bob=s.hover>0?s.hover+Mathf.Sin(phase*.65f)*.035f:Mathf.Abs(Mathf.Sin(phase))*move*.065f;
        float lean=Mathf.Sin(phase)*move*3f,sx=1,sy=1+Mathf.Sin(phase*.42f)*.013f;
        float facing=direction<-.05f?-1:1;
        if(castAge>=0&&castAge<s.Duration)
        {
            float charge=Mathf.Clamp01(castAge/s.windup);
            float release=Mathf.Clamp01((castAge-s.windup)/Mathf.Max(.1f,s.travel));
            float pulse=castAge<s.windup?Mathf.Sin(charge*Mathf.PI*.5f):Mathf.Sin(release*Mathf.PI);
            switch(s.motion)
            {
                case "spit":sx+=pulse*.1f;sy-=pulse*.08f;lean=(castAge<s.windup?-7:8)*pulse*facing;break;
                case "inflate":sx+=pulse*.22f;sy+=pulse*.1f;bob+=pulse*.08f;break;
                case "breath":lean=(castAge<s.windup?-10:12)*pulse*facing;sx+=pulse*.055f;break;
                case "wings":bob+=pulse*.22f;sx+=Mathf.Sin(castAge*15f)*.045f;lean=Mathf.Sin(castAge*12f)*3;break;
                case "spin":lean=Mathf.Sin(castAge*24f)*12;sx*=.65f+.35f*Mathf.Abs(Mathf.Cos(castAge*12f));break;
                case "slash":case "lash":lean=Mathf.Sin(castAge*19f)*17*facing;bob+=pulse*.07f;break;
                case "charge":lean=18*pulse*facing;break;
                case "punch":lean=(castAge<s.windup?-12:18)*pulse*facing;break;
                case "raise":bob+=pulse*.16f;sy+=pulse*.06f;break;
                case "horn":lean=-8*pulse*facing;bob+=pulse*.06f;break;
                case "recoil":lean=-Mathf.Max(0,Mathf.Sin((castAge-s.windup)*20))*10*facing;break;
            }
        }
        else if(attack>0)lean+=Mathf.Sin((1-Mathf.Clamp01(attack/.35f))*Mathf.PI)*9*facing;
        // Blend pose transitions so starting/stopping, a cast ending, or a target change cannot snap the body.
        actor.poseLean=Mathf.LerpAngle(actor.poseLean,lean,blend);
        actor.poseX=Mathf.Lerp(actor.poseX,sx,blend);actor.poseY=Mathf.Lerp(actor.poseY,sy,blend);actor.poseBob=Mathf.Lerp(actor.poseBob,bob,blend);
        lean=actor.poseLean;sx=actor.poseX;sy=actor.poseY;bob=actor.poseBob;
        Texture texture=actor.portrait.sharedMaterial.mainTexture;
        float aspect=texture!=null?Mathf.Clamp((float)texture.width/texture.height,.68f,1.45f):1;
        actor.portrait.transform.localScale=new Vector3(size*sx*aspect,size*sy,1);
        actor.portrait.transform.localPosition=camera.transform.up*(.65f*size*sy)+Vector3.up*bob;
        if(castAge>=0&&s.motion=="charge")
        {
            float dash=Mathf.Sin(Mathf.Clamp01((castAge-s.windup)/s.travel)*Mathf.PI);
            actor.portrait.transform.localPosition+=new Vector3(facing*.36f,0,0)*dash;
        }
        actor.portrait.transform.rotation=camera.transform.rotation*Quaternion.Euler(0,0,death>0?-55*death:lean);
        actor.contactShadow.transform.localScale=new Vector3(.43f*size,.01f,.30f*size)*(1-Mathf.Clamp(bob,0,.5f)*.45f);
    }
    public void DrawSkill(DigimonSkillCatalog.Entry s,Vector3 origin,Vector3 target,float age)
    {
        if(s==null||age<0||age>=s.Duration)return;
        Color color=s.Tint;Vector3 delta=target-origin,side=Side(delta);
        Vector3 start=origin+Vector3.up*(.75f*s.size+s.hover),end=target+Vector3.up*.65f;
        start=ModelMuzzle(s.id,origin,start);
        float charge=Mathf.Clamp01(age/s.windup);
        if(age<s.windup)
        {
            Halo(origin+Vector3.up*.07f,.3f+charge*.25f,color);
            if(s.visual=="seven")for(int i=0;i<7;i++)
            {float a=i*Mathf.PI*2/7;Orb(start+camera.transform.right*Mathf.Cos(a)*.75f+Vector3.up*(.55f+Mathf.Sin(a)*.65f),.13f*charge,color);}
            else if(s.visual=="sun")Orb(start+Vector3.up*1.1f,.65f*charge,color);
            else if(s.visual=="flower")for(int i=0;i<5;i++)
            {float a=i*Mathf.PI*2/5;Orb(start+side*Mathf.Cos(a)*.24f+Vector3.up*Mathf.Sin(a)*.24f,.09f*charge,color);}
            else if(s.visual!="gate"&&s.visual!="needles")Orb(start,.08f+charge*.12f,color);
            return;
        }
        if(s.visual=="gate")
        {
            float open=Mathf.Clamp01((age-s.windup)/s.travel),close=1-Mathf.Clamp01((age-s.Impact)/s.recovery);
            Vector3 center=end+Vector3.up*.45f;float r=.85f*open*close;
            Halo(center,r,color,true);Halo(center,r*.76f,new Color(1,.95f,.65f),true);
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4+age*2;Vector3 p=center+(camera.transform.right*Mathf.Cos(a)+camera.transform.up*Mathf.Sin(a))*r;
                Orb(p,.045f*close,Color.white);
            }
            Stroke(center-camera.transform.up*r*.8f,center+camera.transform.up*r*.8f,.05f*close,color);
            return;
        }
        for(int shot=0;shot<s.shots;shot++)
        {
            float t=(age-s.windup-shot*s.interval)/s.travel;
            if(t<0&&s.visual=="seven")
            {
                float a=shot*Mathf.PI*2/7;
                Orb(start+camera.transform.right*Mathf.Cos(a)*.75f+Vector3.up*(.55f+Mathf.Sin(a)*.65f),.13f,color);
            }
            if(t>=0&&t<1)
            {
                Vector3 launch=start;
                if(s.visual=="sun")launch+=Vector3.up*1.1f;
                if(s.visual=="missiles")launch+=side*(shot%2==0?-.28f:.28f);
                if(s.visual=="seven")
                {float a=shot*Mathf.PI*2/7;launch+=camera.transform.right*Mathf.Cos(a)*.75f+Vector3.up*(.55f+Mathf.Sin(a)*.65f);}
                Vector3 p=Vector3.Lerp(launch,end,t);
                if(s.visual=="sun"||s.visual=="meteors"||s.visual=="missiles")p+=Vector3.up*Mathf.Sin(t*Mathf.PI)*(s.visual=="sun"?.45f:.7f);
                DrawFlight(s,launch,end,p,side,t,age,color);
            }
            float impactAge=age-s.Impact-shot*s.interval;
            if(impactAge>=0&&impactAge<.3f)
            {
                float fade=1-impactAge/.3f;
                Vector3 center=s.shape=="radial"?origin:target;
                Halo(center+Vector3.up*.09f,(.2f+impactAge*3)*Mathf.Min(1.5f,s.radius),Color.Lerp(color,Color.white,fade*.4f));
                if(s.visual=="ice")for(int j=0;j<5;j++)
                {
                    float a=j*Mathf.PI*2/5;Vector3 offset=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.4f;
                    Effect(hex,center+offset+Vector3.up*.3f,new Vector3(.12f,.7f*fade,.12f),color,Quaternion.Euler(0,a*57,15));
                }
                else Orb(center+Vector3.up*.6f,.24f*fade,color);
            }
        }
    }
    void DrawFlight(DigimonSkillCatalog.Entry s,Vector3 start,Vector3 end,Vector3 p,Vector3 side,float t,float age,Color color)
    {
        switch(s.visual)
        {
            case "bubble":case "air":
                Halo(p,s.visual=="air"?.24f:.13f,color,true);
                if(s.visual=="air")Halo(Vector3.Lerp(start,p,.83f),.32f,color,true);
                else Orb(p+camera.transform.up*.065f-camera.transform.right*.04f,.035f,Color.white);
                break;
            case "vine":case "whip":
                Vector3 previous=start;
                for(int j=1;j<=12;j++)
                {
                    float f=j/12f;Vector3 next=Vector3.Lerp(start,end,Mathf.Min(1,t*2)*f)+side*Mathf.Sin(f*12-age*18)*.12f;
                    Stroke(previous,next,.065f,s.visual=="vine"?new Color(.25f,.65f,.22f):color);
                    if(j%3==0)Stroke(next,next+Vector3.up*.13f,.035f,color);previous=next;
                }
                break;
            case "water":
                Stroke(start,p,.18f,color);
                for(int j=0;j<5;j++)Halo(Vector3.Lerp(start,p,j/5f),.13f,color,true);
                Orb(p,.16f,Color.white);break;
            case "darkclaw":
                Stroke(start,p,.09f,color);
                for(int j=0;j<4;j++)
                {
                    Vector3 claw=p+side*(j-1.5f)*.08f;
                    Stroke(claw,claw+Vector3.up*.2f+(end-start).normalized*.15f,.035f,color);
                }
                break;
            case "lightning":case "thunderbeam":
                Vector3 prev=start;int segments=s.visual=="thunderbeam"?14:8;
                for(int j=1;j<=segments;j++)
                {
                    float f=j/(float)segments;Vector3 next=Vector3.Lerp(start,end,f)+side*Mathf.Sin(j*17+Mathf.Floor(age*22))*.16f*Mathf.Sin(f*Mathf.PI);
                    Stroke(prev,next,s.visual=="thunderbeam"?.13f:.06f,color);prev=next;
                }
                if(s.visual=="thunderbeam")Stroke(start,end,.045f,Color.white);
                break;
            case "needles":
                for(int j=0;j<16;j++)
                {
                    float a=j*Mathf.PI/8;Vector3 d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                    Vector3 tip=start+d*t*s.radius*1.2f;Stroke(tip-d*.28f,tip,.035f,color);
                }
                break;
            case "claws":
                for(int j=0;j<3;j++)
                {
                    Vector3 mid=Vector3.Lerp(start,end,t)+side*(j-1)*.16f;
                    Stroke(mid+Vector3.up*.25f-side*.13f,mid-Vector3.up*.22f+side*.12f,.055f,color);
                }
                break;
            case "phoenix":
                Orb(p,.16f,color);
                for(int j=-1;j<=1;j+=2)
                {
                    Stroke(p,p+side*j*.65f+Vector3.up*.25f,.12f,color);
                    Stroke(p+side*j*.65f+Vector3.up*.25f,p+side*j*.95f-Vector3.up*.08f,.08f,color);
                    Stroke(p,p+(start-end).normalized*.6f+side*j*.2f,.065f,color);
                }
                break;
            case "starlight":
                for(int j=0;j<14;j++)
                {
                    float a=j*2.39996f,r=Mathf.Sqrt((j+.5f)/14)*s.radius;
                    Vector3 point=end+new Vector3(Mathf.Cos(a)*r,(1-t)*2,Mathf.Sin(a)*r);
                    Stroke(point,point+Vector3.up*.19f,.045f,color);Orb(point,.045f,color);
                }
                break;
            case "ice":
                for(int j=0;j<7;j++)
                {
                    Vector3 point=Vector3.Lerp(start,end,(t+j/7f)%1)+side*Mathf.Sin(j*3.7f)*t*.4f;
                    Effect(hex,point,new Vector3(.10f,.32f,.1f),color,Quaternion.Euler(30,age*180+j*40,65));
                }
                Stroke(start,p,.09f,color);
                break;
            case "spiral":
                for(int j=0;j<7;j++)
                {float a=age*19+j*.8f;Orb(p+side*Mathf.Cos(a)*.16f+Vector3.up*Mathf.Sin(a)*.16f-(end-start).normalized*j*.075f,.08f,color);}
                break;
            case "missiles":
                Vector3 direction=(end-start).normalized;
                Stroke(p-direction*.25f,p+direction*.12f,.14f,new Color(.8f,.85f,.87f));
                Orb(p-direction*.35f,.14f,color);break;
            case "horn":
                Stroke(start,p,.12f,color);Halo(p,.23f,color,true);break;
            default:
                float radius=s.visual=="sun"?.65f:s.visual=="electricorb"?.27f:s.visual=="flower"?.22f:.16f;
                Orb(p,radius,color);
                Stroke(Vector3.Lerp(start,p,.76f),p,radius*.8f,color);
                if(s.visual=="fire"||s.visual=="bluefire")for(int j=1;j<=3;j++)
                    Orb(p-(end-start).normalized*j*.13f+Vector3.up*Mathf.Sin(age*28+j)*.07f,radius*(1-j*.18f),color);
                if(s.visual=="flower"||s.visual=="electricorb")Halo(p,radius*1.45f,color,true);
                break;
        }
    }
}
