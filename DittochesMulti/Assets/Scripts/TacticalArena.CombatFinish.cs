using UnityEngine;

public sealed partial class TacticalArena
{
    Mesh combatRibbon,combatStreak;
    Mesh CombatRibbonMesh()
    {
        if(combatRibbon!=null)return combatRibbon;
        const int segments=24,rows=3;var vertices=new Vector3[(segments+1)*rows];var colors=new Color[vertices.Length];var triangles=new int[segments*12];
        for(int i=0;i<=segments;i++)
        {
            float t=i/(float)segments,a=Mathf.Lerp(-1.32f,1.32f,t),taper=i==0||i==segments?0:Mathf.Sin(t*Mathf.PI);
            for(int j=0;j<rows;j++)
            {
                float r=1-(2-j)*.14f*taper;int k=i*rows+j;
                vertices[k]=new Vector3(Mathf.Sin(a)*r,Mathf.Cos(a)*r,0);
                colors[k]=new Color(1,1,1,(j==1?.9f:0)*taper);
            }
            if(i==segments)continue;
            for(int j=0;j<2;j++)
            {int n=i*3+j,k=i*12+j*6;triangles[k]=n;triangles[k+1]=n+3;triangles[k+2]=n+1;triangles[k+3]=n+1;triangles[k+4]=n+3;triangles[k+5]=n+4;}
        }
        combatRibbon=Own(new Mesh{name="Tapered combat crescent",vertices=vertices,triangles=triangles});
        // Own applies baked preview lighting; translucent ribbons retain their authored alpha.
        combatRibbon.colors=colors;return combatRibbon;
    }
    void CombatSweep(Vector3 center,Vector3 forward,float radius,float roll,Color color,float aspect=1)
    {
        if(radius<.005f||color.a<=.002f||!EnsureSkillFinish())return;
        if(forward.sqrMagnitude<.0001f)forward=Vector3.forward;
        // A camera-facing sweep remains legible when the attack travels across the board.
        float facing=Mathf.Atan2(Vector3.Dot(forward,camera.transform.up),Vector3.Dot(forward,camera.transform.right))*Mathf.Rad2Deg;
        Effect(CombatRibbonMesh(),center,new Vector3(radius,radius*aspect,1),color,camera.transform.rotation*Quaternion.Euler(0,0,roll+facing),transparentSkillMaterial);
    }
    Mesh CombatStreakMesh()
    {
        if(combatStreak!=null)return combatStreak;
        var vertices=new Vector3[12];var colors=new Color[12];var triangles=new int[36];
        for(int i=0;i<4;i++)for(int j=0;j<3;j++)
        {
            float t=i/3f;int n=i*3+j;vertices[n]=new Vector3(t-1,(j-1)*t*.5f,0);
            colors[n]=new Color(1,1,1,j==1?t*t*.8f:0);
            if(i==3||j==2)continue;int k=i*12+j*6;
            triangles[k]=n;triangles[k+1]=n+3;triangles[k+2]=n+1;triangles[k+3]=n+1;triangles[k+4]=n+3;triangles[k+5]=n+4;
        }
        combatStreak=Own(new Mesh{name="Continuous fading projectile wake",vertices=vertices,triangles=triangles});combatStreak.colors=colors;return combatStreak;
    }
    void CombatTrail(Vector3 p,Vector3 direction,float length,float width,Color color)
    {
        if(!EnsureSkillFinish()||direction.sqrMagnitude<.0001f||length<.005f)return;
        direction.Normalize();Vector3 across=Vector3.Cross(direction,camera.transform.forward);
        if(across.sqrMagnitude<.001f)across=camera.transform.right;across.Normalize();
        Effect(CombatStreakMesh(),p,new Vector3(length,width*2,1),color,Quaternion.LookRotation(Vector3.Cross(direction,across),across),transparentSkillMaterial);
    }
    void CombatImpact(CombatEffectStyle.Element element,Vector3 center,Vector3 forward,float t,float size,int seed,Color color,bool major,float surface=0)
    {
        if(t<0||t>=1||!EnsureSkillFinish())return;
        // Hit flashes sit on the visible surface, rather than disappearing inside an opaque body.
        center=ContactPoint(center,Mathf.Max(surface,Mathf.Max(major?.34f:.28f,size*1.4f)));
        float fade=(1-t)*(1-t),spread=1-Mathf.Pow(1-t,2),radius=size*(.45f+spread*2.3f);Color light=Color.Lerp(color,Color.white,.58f);light.a=fade;
        Vector3 side=camera.transform.right,spreadUp=camera.transform.up,up=Vector3.up;int count=major?6:4;
        SoftSkillGlow(center,size*(1.3f+t),color,fade*(major?.46f:.28f));
        CombatImpactAccent(element,center,forward,t,size,seed,color,major);
        if(element==CombatEffectStyle.Element.Force||element==CombatEffectStyle.Element.Shadow)
        {
            Color edge=color;edge.a=fade*.8f;
            CombatSweep(center,forward,radius*1.35f,seed*37+t*95,edge,.6f);
            if(major)CombatSweep(center,forward,radius*1.15f,seed*37+170-t*75,light,.65f);
        }
        if(element==CombatEffectStyle.Element.Water)
        {
            Color ripple=color;ripple.a=fade*.7f;Halo(center,radius,ripple,true);
            if(major)Halo(center-forward*.08f,radius*.72f,ripple,true);
        }
        for(int i=0;i<count;i++)
        {
            float angle=i*Mathf.PI*2/count+seed*.71f;
            Vector3 d=(side*Mathf.Cos(angle)+spreadUp*Mathf.Sin(angle)).normalized;
            Vector3 at=center+d*radius-up*(element==CombatEffectStyle.Element.Flame?-.1f:t*t*size*.7f);
            switch(element)
            {
                case CombatEffectStyle.Element.Flame:
                    SoftSkillGlow(at,size*(.36f-.22f*t),i%2==0?color:light,fade*.7f);
                    CombatSpark(at+d*size*.22f,d+up*.35f,size*(.6f-.3f*t),major?.033f:.023f,light);break;
                case CombatEffectStyle.Element.Storm:
                    Vector3 bend=center+d*radius*.6f+side*((i%2==0?1:-1)*size*.23f);
                    SkillBeam(center+d*size*.15f,bend,.014f+(major?.012f:0),light);
                    SkillBeam(bend,at,.012f+(major?.009f:0),color*new Color(1,1,1,fade));break;
                case CombatEffectStyle.Element.Frost:
                    Effect(SkillTip(),at,new Vector3(size*.18f,size*(.75f-t*.32f),size*.13f),light,Quaternion.FromToRotation(up,d),transparentSkillMaterial);break;
                case CombatEffectStyle.Element.Nature:
                    Effect(CombatRibbonMesh(),at,Vector3.one*(size*(.52f-t*.23f)),light,camera.transform.rotation*Quaternion.Euler(0,0,angle*Mathf.Rad2Deg+t*130),transparentSkillMaterial);break;
                case CombatEffectStyle.Element.Light:
                    CombatSpark(at+d*size*.28f,d,size*.62f,major?.03f:.022f,light);
                    if(i%2==0)SkillBeam(at-side*size*.15f,at+side*size*.15f,.013f,light);break;
                case CombatEffectStyle.Element.Water:
                    SoftSkillGlow(at,size*.20f,color,fade*.65f);break;
                default:
                    CombatSpark(at+d*size*.16f,d,size*(.62f-.25f*t),major?.031f:.023f,light);break;
            }
        }
    }
    void CombatCharge(DigimonSkillCatalog.Entry skill,Vector3 origin,Vector3 muzzle,float t,Color color)
    {
        if(!EnsureSkillFinish())return;
        var element=CombatEffectStyle.Skill(skill.visual);Color c=color;c.a=.16f+.28f*t;
        if(element==CombatEffectStyle.Element.Force||skill.visual=="darkclaw")
        {CombatSweep(muzzle,Vector3.up,.20f+.17f*t,t*110,c);return;}
        for(int i=0;i<3;i++)
        {
            float a=i*Mathf.PI*2/3+t*1.7f,r=(1-t)*.30f+.08f;
            Vector3 at=muzzle+(camera.transform.right*Mathf.Cos(a)+camera.transform.up*Mathf.Sin(a))*r;
            SoftSkillGlow(at,.025f+.015f*t,color,.25f+t*.35f);
        }
    }
    bool DrawCombatFlight(DigimonSkillCatalog.Entry s,Vector3 start,Vector3 end,Vector3 p,Vector3 side,float t,float age,Color color)
    {
        Vector3 forward=(end-start).normalized;if(forward.sqrMagnitude<.001f)forward=Vector3.forward;
        switch(s.visual)
        {
            case "claws":case "darkclaw":
                for(int i=0;i<3;i++)
                {
                    Color c=i==1?Color.Lerp(color,Color.white,.25f):color;c.a=.9f;
                    CombatSweep(p+side*(i-1)*.13f,forward,.43f,-65+t*130+i*9,c,.85f);
                }
                if(s.visual=="darkclaw")CombatTrail(p,forward,.6f,.06f,color);
                return true;
            case "impact":
                CombatSweep(p,forward,.48f,-70+t*150,color,.7f);CombatTrail(p,forward,.5f,.08f,color);return true;
            case "swords":
                Quaternion blade=Quaternion.FromToRotation(Vector3.up,forward);
                Effect(SkillTip(),p-forward*.43f,new Vector3(.08f,.55f,.028f),Color.Lerp(color,Color.white,.7f),blade,stone);
                BasicLine(p-forward*.36f-side*.15f,p-forward*.36f+side*.15f,.032f,color);
                BasicLine(p-forward*.37f,p-forward*.59f,.047f,new Color(.92f,.73f,.32f));
                CombatTrail(p-forward*.5f,forward,.42f,.05f,color);return true;
            case "phoenix":
                Orb(p,.14f,color);
                for(int sign=-1;sign<=1;sign+=2)
                {
                    CombatSweep(p+side*sign*.25f,forward,.65f,sign*(55+Mathf.Sin(t*Mathf.PI)*18),color,.50f);
                    for(int i=0;i<3;i++)CombatTrail(p+side*sign*(.28f+i*.16f),forward,.22f+i*.07f,.035f,color);
                }
                CombatTrail(p,forward,.65f,.13f,color);return true;
            case "flower":
                for(int i=0;i<5;i++)CombatSweep(p,forward,.22f,age*160+i*72,color,.6f);
                SoftSkillGlow(p,.12f,Color.white,.5f);CombatTrail(p,forward,.5f,.075f,color);return true;
            case "darkorb":case "electricorb":case "sun":
                float radius=s.visual=="sun"?.5f:s.visual=="electricorb"?.23f:.18f;
                Orb(p,radius,color);
                for(int i=0;i<3;i++)CombatSweep(p,forward,radius*1.35f,age*190+i*120,color,.8f);
                CombatTrail(p,forward,radius*2.2f,radius*.45f,color);return true;
            case "meteors":
                Effect(EffectSphere(),p,Vector3.one*.13f,Color.Lerp(color,new Color(.22f,.12f,.10f),.6f),Quaternion.Euler(age*130,30,0),stone);
                SoftSkillGlow(p,.24f,color,.7f);CombatTrail(p,forward,.65f,.12f,color);return true;
            case "seven":case "holy":
                Orb(p,.13f,color);CombatTrail(p,forward,.6f,.05f,color);
                CombatSweep(p,forward,.17f,age*130,color,.65f);return true;
            case "starlight":
                for(int i=0;i<8;i++)
                {
                    float a=i*2.39996f,r=Mathf.Sqrt((i+.5f)/8)*s.radius;
                    Vector3 at=end+new Vector3(Mathf.Cos(a)*r,(1-t)*2,Mathf.Sin(a)*r);
                    BasicLine(at,at+Vector3.up*.3f,.022f,color);SoftSkillGlow(at,.075f,color,.6f);
                }
                return true;
            default:return false;
        }
    }
#if DITTOCHES_PORTABLE_PREVIEW
    public bool CombatRibbonHealthy()
    {
        var mesh=CombatRibbonMesh();var streak=CombatStreakMesh();return mesh.vertexCount==75&&mesh.triangles.Length==288&&mesh.colors[0].a==0&&mesh.colors[72].a==0&&streak.vertexCount==12&&streak.colors[1].a==0&&streak.colors[10].a>.7f;
    }
#endif
}
