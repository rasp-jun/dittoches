using UnityEngine;

public sealed partial class TacticalArena
{
    Mesh skillBeam;
    Mesh SkillBeamMesh()
    {
        if(skillBeam!=null)return skillBeam;
        const int rows=5,columns=5;var vertices=new Vector3[rows*columns];var colors=new Color[vertices.Length];var triangles=new int[(rows-1)*(columns-1)*6];
        for(int x=0;x<columns;x++)for(int y=0;y<rows;y++)
        {
            float t=x/(float)(columns-1),across=(y-2)*.5f,profile=.82f+.18f*Mathf.Sin(t*Mathf.PI);int n=x*rows+y;
            vertices[n]=new Vector3(t-.5f,across*.5f*profile,0);
            colors[n]=new Color(1,1,1,y==0||y==4?0:y==2?1:.3f);
            if(x==columns-1||y==rows-1)continue;int k=(x*(rows-1)+y)*6;
            triangles[k]=n;triangles[k+1]=n+rows;triangles[k+2]=n+1;triangles[k+3]=n+1;triangles[k+4]=n+rows;triangles[k+5]=n+rows+1;
        }
        skillBeam=Own(new Mesh{name="Soft-edged energy filament",vertices=vertices,triangles=triangles});skillBeam.colors=colors;return skillBeam;
    }
    void SkillBeam(Vector3 from,Vector3 to,float width,Color color)
    {
        Vector3 delta=to-from;float length=delta.magnitude;
        if(length<.002f||width<.001f||color.a<=.001f||!EnsureSkillFinish())return;
        Vector3 direction=delta/length,across=Vector3.Cross(direction,camera.transform.forward);
        if(across.sqrMagnitude<.001f)across=camera.transform.right;across.Normalize();
        Effect(SkillBeamMesh(),(from+to)*.5f,new Vector3(length,width*2,1),color,Quaternion.LookRotation(Vector3.Cross(direction,across),across),transparentSkillMaterial);
    }
    void RibbonLightning(Vector3 start,Vector3 end,float age,Color color,bool heavy,bool basic=false)
    {
        Vector3 direction=(end-start).normalized,side=Side(direction),up=camera.transform.up,previous=start;
        int segments=basic?6:heavy?10:8;float width=basic?.025f:heavy?.08f:.045f,spread=basic?.06f:heavy?.18f:.12f;
        float tick=Mathf.Floor(age*24);Color light=Color.Lerp(color,Color.white,.72f);
        for(int i=1;i<=segments;i++)
        {
            float f=i/(float)segments,envelope=Mathf.Sin(f*Mathf.PI);
            Vector3 next=Vector3.Lerp(start,end,f)+(side*Mathf.Sin(i*17+tick)+up*Mathf.Cos(i*8-tick)*.45f)*spread*envelope;
            SkillBeam(previous,next,width,light);
            if(!basic&&i%3==0&&i<segments)
            {
                Vector3 branch=next+side*(i%2==0?1:-1)*.23f+up*.12f;
                Color c=color;c.a=.6f;SkillBeam(next,branch,width*.42f,c);SkillBeam(branch,branch+direction*.13f+up*.06f,width*.22f,c);
            }
            previous=next;
        }
        if(heavy){Color aura=color;aura.a=.38f;SkillBeam(start,end,.17f,aura);SkillBeam(start,end,.025f,light);}
        SoftSkillGlow(start,basic?.06f:.12f,color,.55f);SoftSkillGlow(end,basic?.065f:.14f,light,.65f);
    }
    void RibbonWater(Vector3 start,Vector3 end,float age,Color color,bool basic)
    {
        Vector3 direction=(end-start).normalized;
        Color mist=color;mist.a=.45f;
        if(basic)CombatTrail(end,direction,Mathf.Min(.36f,(end-start).magnitude),.075f,mist);
        else
        {
            SkillBeam(start,end,.13f,mist);Color core=Color.Lerp(color,Color.white,.6f);core.a=.65f;SkillBeam(start,end,.035f,core);
            for(int i=0;i<3;i++)
            {
                float f=Mathf.Repeat(age*2+i/3f,1);Vector3 at=Vector3.Lerp(start,end,f);
                CombatSweep(at,direction,.11f,age*190+i*120,color,.65f);
            }
        }
        SoftSkillGlow(end,basic?.10f:.16f,Color.Lerp(color,Color.white,.5f),.65f);
        for(int i=0;i<(basic?2:4);i++)
        {
            Vector3 at=end-direction*(.10f+i*.045f)+camera.transform.up*Mathf.Sin(age*24+i*2.3f)*.04f;
            CombatTrail(at,direction,.10f,.018f,Color.Lerp(color,Color.white,.3f));
        }
    }
    void RibbonAir(Vector3 p,Vector3 direction,float age,float size,Color color,bool bubble)
    {
        Color rim=color;rim.a=.75f;
        if(bubble)
        {
            for(int i=0;i<3;i++)CombatSweep(p,direction,size,age*60+i*120,rim);
            SoftSkillGlow(p-camera.transform.right*size*.34f+camera.transform.up*size*.4f,size*.28f,Color.white,.8f);
            Color fill=color;fill.a=.18f;SoftSkillGlow(p,size*.85f,fill,.7f);
        }
        else
        {
            for(int i=0;i<3;i++)
            {
                rim.a=.65f-i*.13f;CombatSweep(p-direction*i*.11f,direction,size*(1+i*.23f),age*260+i*110,rim,.8f);
            }
            CombatTrail(p,direction,.30f,size*.24f,color);
        }
    }
    bool DrawRibbonFlight(DigimonSkillCatalog.Entry s,Vector3 start,Vector3 end,Vector3 p,Vector3 side,float t,float age,Color color)
    {
        Vector3 forward=(end-start).normalized;if(forward.sqrMagnitude<.001f)forward=Vector3.forward;
        switch(s.visual)
        {
            case "lightning":case "thunderbeam":
                RibbonLightning(start,end,age,color,s.visual=="thunderbeam");return true;
            case "holybeam":
                Color aura=color;aura.a=.6f;SkillBeam(start,p,.12f,aura);SkillBeam(start,p,.032f,Color.Lerp(color,Color.white,.8f));
                SoftSkillGlow(start,.14f,color,.45f);SoftSkillGlow(p,.17f,color,.7f);
                CombatSweep(p,forward,.18f,age*130,color);return true;
            case "water":RibbonWater(start,p,age,color,false);return true;
            case "vine":case "whip":
                Vector3 previous=start;float reach=Mathf.Clamp01(t*2),sway=(s.visual=="vine"?.16f:.23f)*Mathf.Sin(t*Mathf.PI);
                Color stem=s.visual=="vine"?new Color(.24f,.68f,.24f):color;
                for(int i=1;i<=18;i++)
                {
                    float f=i/18f;Vector3 next=Vector3.Lerp(start,end,reach*f)+(side*Mathf.Sin(f*9-age*14)+camera.transform.up*Mathf.Cos(f*6-age*12)*.4f)*Mathf.Sin(f*Mathf.PI)*sway;
                    SkillBeam(previous,next,.048f*(1-f*.65f),stem);
                    if(i%4==0)
                    {
                        Vector3 thorn=(camera.transform.up*(i%8==0?1:-1)+side*.35f).normalized;
                        Effect(SkillTip(),next,new Vector3(.026f,.11f*(1-f*.4f),.023f),Color.Lerp(stem,Color.white,.2f),Quaternion.FromToRotation(Vector3.up,thorn),stone);
                    }
                    previous=next;
                }
                CombatSweep(previous,forward,.15f,t*210,color,.55f);return true;
            case "needles":
                for(int i=0;i<16;i++)
                {
                    float a=i*Mathf.PI/8;Vector3 d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),tip=start+d*t*s.radius*1.2f;
                    Effect(SkillTip(),tip-d*.22f,new Vector3(.04f,.29f,.035f),Color.Lerp(color,Color.white,.3f),Quaternion.FromToRotation(Vector3.up,d),stone);
                    Color wake=color;wake.a=.65f;CombatTrail(tip-d*.16f,d,Mathf.Min(.28f,t*s.radius),.024f,wake);
                }
                return true;
            case "spiral":
                Vector3 orbitUp=Vector3.Cross(forward,side).normalized;
                for(int strand=0;strand<2;strand++)
                {
                    Vector3 last=p;Color c=Color.Lerp(color,Color.white,strand*.35f);c.a=.8f;
                    for(int i=1;i<=24;i++)
                    {
                        float f=i/24f,a=age*15-f*8+strand*Mathf.PI,r=.12f*Mathf.Sin(f*Mathf.PI);
                        Vector3 next=p-forward*f*.70f+(side*Mathf.Cos(a)+orbitUp*Mathf.Sin(a))*r;
                        SkillBeam(last,next,.045f*(1-f*.65f),c);last=next;
                    }
                }
                SoftSkillGlow(p,.15f,color,.7f);CombatTrail(p,forward,.5f,.08f,color);return true;
            case "horn":
                CombatTrail(p,forward,Mathf.Min(.65f,(p-start).magnitude),.07f,color);
                Effect(SkillTip(),p-forward*.30f,new Vector3(.12f,.42f,.10f),Color.Lerp(color,Color.white,.55f),Quaternion.FromToRotation(Vector3.up,forward),transparentSkillMaterial);
                CombatSweep(p,forward,.24f,age*160,color,.65f);SoftSkillGlow(p,.12f,color,.55f);return true;
            case "bubble":case "air":
                RibbonAir(p,forward,age,s.visual=="bubble"?.15f:.24f,color,s.visual=="bubble");return true;
            default:return false;
        }
    }
    bool DrawRibbonBasic(BasicAttackPresentation.Kind kind,Vector3 start,Vector3 p,Vector3 direction,float age,float size,Color color)
    {
        if(!EnsureSkillFinish())return false;
        switch(kind)
        {
            case BasicAttackPresentation.Kind.Electric:RibbonLightning(start,p,age,color,false,true);return true;
            case BasicAttackPresentation.Kind.Water:RibbonWater(start,p,age,color,true);return true;
            case BasicAttackPresentation.Kind.Bubble:RibbonAir(p,direction,age,size*.85f,color,true);return true;
            case BasicAttackPresentation.Kind.Air:RibbonAir(p,direction,age,size,color,false);return true;
            case BasicAttackPresentation.Kind.Fire:
                CombatTrail(p,direction,Mathf.Min((p-start).magnitude,.28f),size*.55f,color);
                SoftSkillGlow(p,size*.7f,color,.7f);SoftSkillGlow(p,size*.24f,Color.Lerp(color,Color.white,.8f),.8f);
                for(int i=1;i<=2;i++)SoftSkillGlow(p-direction*i*.065f+camera.transform.up*Mathf.Sin(age*29+i)*.02f,size*(.55f-i*.12f),color,.45f);
                return true;
            default:return false;
        }
    }
}
