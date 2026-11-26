using UnityEngine;

public sealed partial class TacticalArena
{
    Mesh shockwaveMesh,finisherSigil;
    Mesh ShockwaveMesh()
    {
        if(shockwaveMesh!=null)return shockwaveMesh;
        const int sides=64;var vertices=new Vector3[sides*3];var colors=new Color[sides*3];var triangles=new int[sides*12];
        for(int i=0;i<sides;i++)
        {
            float a=i*Mathf.PI*2/sides;var ray=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
            for(int j=0;j<3;j++){vertices[i*3+j]=ray*(.72f+j*.14f);colors[i*3+j]=new Color(1,1,1,j==1?1:0);}
            for(int j=0;j<2;j++)
            {
                int n=i*3+j,next=(i+1)%sides*3+j,t=i*12+j*6;
                triangles[t]=n;triangles[t+1]=next;triangles[t+2]=n+1;triangles[t+3]=n+1;triangles[t+4]=next;triangles[t+5]=next+1;
            }
        }
        shockwaveMesh=Own(new Mesh{name="Soft expanding impact wave",vertices=vertices,triangles=triangles});shockwaveMesh.colors=colors;return shockwaveMesh;
    }
    void ImpactWave(Vector3 point,float radius,Color color,float alpha)
    {
        if(alpha<=.002f)return;color.a*=alpha;
        Effect(ShockwaveMesh(),point,new Vector3(radius,1,radius),color,Quaternion.identity,transparentSkillMaterial);
    }
    Mesh FinisherSigil()
    {
        if(finisherSigil!=null)return finisherSigil;
        const int count=24;var vertices=new Vector3[count*4];var triangles=new int[count*6];
        for(int i=0;i<count;i++)
        {
            float a=i*Mathf.PI*2/count;Vector3 ray=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),side=new Vector3(-ray.z,0,ray.x)*.018f;
            float start=i%3==0?.68f:.85f;int n=i*4,t=i*6;
            vertices[n]=ray*start-side;vertices[n+1]=ray-side;vertices[n+2]=ray+side;vertices[n+3]=ray*start+side;
            triangles[t]=n;triangles[t+1]=n+1;triangles[t+2]=n+2;triangles[t+3]=n;triangles[t+4]=n+2;triangles[t+5]=n+3;
        }
        finisherSigil=Own(new Mesh{name="Radial finisher inscription",vertices=vertices,triangles=triangles});return finisherSigil;
    }
    void DrawFinisherAccent(int choice,Vector3 source,Vector3 impact,float age,float hit,Color color,float fade)
    {
        if(!EnsureSkillFinish())return;
        float after=age-hit,charge=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/(hit*.75f)));
        float inscription=after<0?charge*.22f:fade*fade*.55f;
        Color ink=color;ink.a=inscription;
        Effect(FinisherSigil(),impact+Vector3.up*.025f,Vector3.one*(choice==1?1.35f:1.15f),ink,Quaternion.Euler(0,choice==0?age*32:-age*12,0),transparentSkillMaterial);
        if(after<0)
        {
            ImpactWave(impact+Vector3.up*.01f,1.5f-.6f*charge,color,.16f*charge);
            float flight=Mathf.Clamp01((age-FinisherLaunchTime)/(hit-FinisherLaunchTime));
            for(int i=0;i<12;i++)
            {
                float t=Mathf.Clamp01(flight-i*.025f);Vector3 p=FlightTrailPoint(source,impact,choice,t,finisherHeight);
                SoftSkillGlow(p,(choice==1?.25f:.16f)+i*.006f,color,(1-i/12f)*flight*.5f);
            }
            return;
        }
        float burst=1-Mathf.Exp(-after*5);
        ImpactWave(impact+Vector3.up*.035f,.35f+burst*(choice==1?3.0f:2.45f),Color.Lerp(color,Color.white,.25f),Mathf.Exp(-after*2.5f)*.9f);
        if(after>.10f)ImpactWave(impact+Vector3.up*.07f,.2f+(1-Mathf.Exp(-(after-.10f)*4))*2.1f,color,fade*fade*.45f);
        if(choice==1)
        {
            GroundRadiance(impact+Vector3.up*.012f,1.55f,new Color(.08f,.035f,.025f),fade*.5f);
            for(int i=0;i<10;i++)
            {
                float a=i*2.39996f;Vector3 ray=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                Vector3 p=impact+ray*(.4f+burst*1.35f)+Vector3.up*(.15f+after*(.38f+i%3*.10f));
                SoftSkillGlow(p,.25f+after*.3f,new Color(.22f,.17f,.15f),fade*.23f);
                if(after<.8f)FinishLine(p,p-ray*.10f+Vector3.up*.08f,.018f,new Color(1,.81f,.36f),Mathf.Exp(-after*3));
            }
        }
        else if(choice==2)
        {
            GateCurtain(impact,1,color,Mathf.Exp(-after*5)*.48f);
            float height=Mathf.Min(1,finisherHeight*5.2f/3.7f);
            for(int i=0;i<10;i++)
            {
                float a=i*Mathf.PI/5;Vector3 p=impact+new Vector3(Mathf.Cos(a)*(1+burst*.9f),(.5f+after*.7f)*height,Mathf.Sin(a)*(1+burst*.9f));
                Color feather=Color.Lerp(color,Color.white,.65f);feather.a=fade*fade*.65f;
                Effect(FinisherFeather(),p,new Vector3(.20f,.38f,1)*fade,feather,Quaternion.Euler(15,age*40+i*36,25),transparentSkillMaterial);
            }
        }
        else
        {
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4+after*.5f;Vector3 p=impact+new Vector3(Mathf.Cos(a)*burst*1.65f,.18f+after*.35f,Mathf.Sin(a)*burst*1.65f);
                FinishRing(p,.07f*fade,Color.Lerp(color,Color.white,.45f),fade*fade,Quaternion.Euler(90,0,45));
            }
        }
    }
    void GateCurtain(Vector3 impact,float open,Color color,float alpha)
    {
        float height=Mathf.Min(1,finisherHeight*5.2f/3.7f);color.a=alpha;
        Effect(skillGlowQuad,impact+Vector3.up*(1.45f*height),new Vector3(1.45f*open,2.4f*height,1),color,Quaternion.identity,softSkillMaterial);
        Effect(skillGlowQuad,impact+Vector3.up*(1.45f*height),new Vector3(.35f*open,2.6f*height,1),new Color(1,.97f,.83f,alpha*.8f),Quaternion.identity,softSkillMaterial);
    }
}
