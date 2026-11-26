using UnityEngine;

public sealed partial class TacticalArena
{
    Mesh finisherFeather;
    Mesh FinisherFeather()
    {
        if(finisherFeather!=null)return finisherFeather;
        const int steps=12;var vertices=new Vector3[(steps+1)*2];var colors=new Color[vertices.Length];var triangles=new int[steps*6];
        for(int i=0;i<=steps;i++)
        {
            float t=i/(float)steps,width=Mathf.Sin(t*Mathf.PI)*.14f;
            vertices[i*2]=new Vector3(t*t*.2f-width,t,0);vertices[i*2+1]=new Vector3(t*t*.2f+width,t,0);
            colors[i*2]=colors[i*2+1]=Color.Lerp(new Color(.6f,.7f,1,.25f),Color.white,Mathf.Sin(t*Mathf.PI));
            if(i==steps)continue;int n=i*2,j=i*6;
            triangles[j]=n;triangles[j+1]=n+1;triangles[j+2]=n+2;triangles[j+3]=n+1;triangles[j+4]=n+3;triangles[j+5]=n+2;
        }
        finisherFeather=Own(new Mesh{name="Curved luminous feather",vertices=vertices,triangles=triangles});finisherFeather.colors=colors;return finisherFeather;
    }
    void TravelingRibbons(int choice,Vector3 source,Vector3 impact,float age,float hit,Color color)
    {
        FlowingFlightRibbon(choice,source,impact,age,hit,color);
    }
    void DataPulseFinisher(Vector3 source,Vector3 impact,float age,float hit,Color c,float fade)
    {
        if(age<hit)
        {
            float charge=Mathf.Clamp01(age/FinisherLaunchTime),flight=Mathf.Clamp01((age-FinisherLaunchTime)/(hit-FinisherLaunchTime));
            Vector3 p=FlightHead(source,impact,0,age,hit);
            FinishRing(source+Vector3.up*.05f,.30f+.48f*charge,c,(1-flight)*.5f,Quaternion.identity);
            Orb(p,.20f*charge,Color.Lerp(c,Color.white,.7f));SoftSkillGlow(p,.68f*charge,c,.8f);
            for(int ring=0;ring<2;ring++)FinishRing(p,(.34f+ring*.12f)*charge,c,.8f,Quaternion.Euler(62+ring*67,age*150*(ring==0?1:-1),15));
            for(int i=0;i<6;i++)
            {
                float a=i*Mathf.PI/3+age*6;var ray=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
                var shard=p+ray*(.33f+.05f*Mathf.Sin(age*15));
                Effect(cube,shard,new Vector3(.055f,.17f,.045f)*charge,Color.Lerp(c,Color.white,i%2*.7f),Quaternion.Euler(0,age*90,a*Mathf.Rad2Deg),transparentSkillMaterial);
            }
            TravelingRibbons(0,source,impact,age,hit,c);return;
        }
        float t=age-hit,expand=1-Mathf.Exp(-t*6),burst=Mathf.Exp(-t*5);
        Vector3 center=impact+Vector3.up*.55f;
        if(t<.35f)Orb(center,.62f*(1-t/.35f),new Color(.8f,1,1));
        SoftSkillGlow(center,1.7f,c,burst*.8f);
        for(int layer=0;layer<3;layer++)FinishRing(center,(.2f+expand*(.8f+layer*.20f)),Color.Lerp(c,Color.white,layer*.2f),fade*fade*.7f,Quaternion.Euler(25+layer*61,t*80,layer*37));
        for(int i=0;i<16;i++)
        {
            float a=i*Mathf.PI/8;Vector3 ray=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
            Vector3 p=impact+ray*(.3f+expand*(1.3f+i%3*.26f))+Vector3.up*(.15f+(i%4)*.12f+t*.45f);
            Effect(cube,p,new Vector3(.09f,.11f,.04f)*fade,Color.Lerp(c,Color.white,i%3*.3f),Quaternion.Euler(i*33,t*100,i*21),transparentSkillMaterial);
            if(i%2==0)FinishLine(p,p-ray*.38f*fade,.025f,c,fade*fade*.7f);
        }
        FinishRing(impact+Vector3.up*.05f,.35f+expand*2.25f,c,fade*.75f,Quaternion.identity);
    }
    void GaiaFinisher(Vector3 source,Vector3 impact,float age,float hit,Color c,float fade)
    {
        if(age<hit)
        {
            float charge=Mathf.Clamp01(age/FinisherLaunchTime),flight=Mathf.Clamp01((age-FinisherLaunchTime)/(hit-FinisherLaunchTime));
            Vector3 p=FlightHead(source,impact,1,age,hit);Quaternion spin=Quaternion.Euler(age*115,age*85,20);
            Vector3 size=Vector3.one*(.55f*Mathf.SmoothStep(0,1,charge));
            Effect(MeteorMesh(false),p,size,new Color(1,.72f,.16f),spin,roundedSkillMaterial);
            Effect(MeteorMesh(true),p,size,new Color(.19f,.08f,.045f),spin,roundedSkillMaterial);
            SoftSkillGlow(p,.91f*charge,c,.68f);
            for(int i=0;i<5;i++)
            {
                float a=i*Mathf.PI*2/5+age*8;Vector3 embers=p+new Vector3(Mathf.Cos(a)*.53f,Mathf.Sin(a)*.37f,.16f*Mathf.Sin(a*2))*charge;
                SoftSkillGlow(embers,.19f,new Color(1,.75f,.23f),.8f);
            }
            FinishRing(source+Vector3.up*.05f,.30f+charge*.48f,c,.5f*(1-flight),Quaternion.identity);
            TravelingRibbons(1,source,impact,age,hit,new Color(1,.33f,.06f));return;
        }
        float t=age-hit,expand=1-Mathf.Exp(-t*5),blast=Mathf.Exp(-t*5);
        Vector3 center=impact+Vector3.up*.34f;
        SoftSkillGlow(center,2.1f,new Color(1,.55f,.14f),blast*.9f);
        if(t<.32f)Orb(center,.8f*(1-t/.32f),new Color(1,.87f,.45f));
        GroundRadiance(impact+Vector3.up*.012f,1.75f,new Color(.06f,.025f,.012f),fade*.8f);
        for(int i=0;i<10;i++)
        {
            float a=i*Mathf.PI/5;Vector3 ray=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
            float h=Mathf.Max(0,(2.1f+i%3*.33f)*t-2.7f*t*t);
            Vector3 point=impact+ray*(.30f+expand*(1.3f+i%3*.28f))+Vector3.up*(h+.08f);
            Color crust=Color.Lerp(new Color(.16f,.075f,.045f),c,blast*.65f);crust.a=fade;
            Effect(MeteorMesh(true),point,Vector3.one*(.14f+i%3*.055f)*fade,crust,Quaternion.Euler(i*37,t*150,i*59),transparentSkillMaterial);
            FinishLine(impact+ray*.35f+Vector3.up*.02f,impact+ray*(.5f+expand*1.25f)+Vector3.up*.02f,.04f,c,fade*fade*.8f);
            if(i%2==0)
            {
                Vector3 flame=impact+ray*(.4f+expand*.7f)+Vector3.up*.03f;
                Effect(FinisherFeather(),flame,new Vector3(.5f,(1.2f+i%3*.22f)*finisherHeight,.5f)*Mathf.Exp(-t*2.2f),new Color(1,.62f,.14f,fade*.75f),Quaternion.Euler(0,i*36,15*Mathf.Sin(i)),transparentSkillMaterial);
            }
        }
        FinishRing(impact+Vector3.up*.045f,.25f+expand*2.9f,c,fade*.85f,Quaternion.identity);
        FinishRing(impact+Vector3.up*.15f,.2f+expand*1.7f,new Color(1,.88f,.47f),fade*fade*.7f,Quaternion.identity);
    }
    void HolyGateFinisher(Vector3 source,Vector3 impact,float age,float hit,Color c,float fade)
    {
        if(age<hit)
        {
            float charge=Mathf.Clamp01(age/FinisherLaunchTime);Vector3 p=FlightHead(source,impact,2,age,hit);
            SoftSkillGlow(p,.55f*charge,c,.75f);FinishRing(p,.28f*charge,c,.9f,Quaternion.Euler(90,age*150,0));
            FinishLine(p-Vector3.up*.33f*charge,p+Vector3.up*.33f*charge,.065f,new Color(1,.98f,.8f));
            for(int side=-1;side<=1;side+=2)for(int feather=0;feather<4;feather++)
            {
                var point=p+new Vector3(side*(.09f+feather*.11f),-.02f-feather*.045f,0)*charge;
                Effect(FinisherFeather(),point,new Vector3(side*.6f,.37f+feather*.02f,1)*charge,Color.Lerp(c,Color.white,.5f),Quaternion.Euler(0,0,-side*(42+feather*6)),transparentSkillMaterial);
            }
            TravelingRibbons(2,source,impact,age,hit,c);return;
        }
        float t=age-hit,opening=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.24f)),dissolve=Mathf.Clamp01(1-t/1.4f);
        float height=Mathf.Min(1,finisherHeight*5.2f/3.7f);Vector3 center=impact+Vector3.up*(1.5f*height);
        FinishRing(center,1.04f*opening*Mathf.Sqrt(height),c,dissolve*.85f,Quaternion.Euler(90,0,0));
        FinishRing(center,.83f*opening*Mathf.Sqrt(height),new Color(1,.96f,.73f),dissolve*.55f,Quaternion.Euler(90,0,0));
        for(int side=-1;side<=1;side+=2)
        {
            Vector3 foot=impact+Vector3.right*(side*.65f*opening);
            FinishLine(foot,foot+Vector3.up*(2.65f*height),.075f,c,dissolve);
            FinishLine(foot+Vector3.up*(2.65f*height),center+Vector3.up*(1.72f*height),.06f,c,dissolve);
            for(int feather=0;feather<6;feather++)
            {
                Vector3 start=center+new Vector3(side*(.40f+feather*.13f)*opening,(.30f-feather*.16f)*height,0);
                Color tint=Color.Lerp(c,Color.white,.22f+feather*.09f);tint.a=dissolve*.9f;
                Effect(FinisherFeather(),start,new Vector3(side*.85f,(.90f+feather*.12f)*height,1)*opening,tint,Quaternion.Euler(0,0,-side*(32+feather*9)),transparentSkillMaterial);
            }
        }
        FinishLine(impact,impact+Vector3.up*(4.55f*finisherHeight),.1f+.24f*Mathf.Exp(-t*6),new Color(1,.97f,.83f),fade*fade*.8f);
        SoftSkillGlow(center,1.2f,c,dissolve*.6f);GroundRadiance(impact,1.3f,c,dissolve*.4f);
        for(int i=0;i<3;i++)FinishRing(impact+Vector3.up*((.13f+i*.28f+t*.38f)*height),.25f+(1-Mathf.Exp(-t*4))*(1.5f-i*.2f),c,fade*fade*.7f,Quaternion.identity);
    }
}
