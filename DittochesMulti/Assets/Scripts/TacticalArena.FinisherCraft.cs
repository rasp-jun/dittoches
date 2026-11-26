using UnityEngine;

public sealed partial class TacticalArena
{
    readonly Mesh[] flightRibbons=new Mesh[2];
    readonly Vector3[] ribbonVertices=new Vector3[66];
    readonly Color[] ribbonColors=new Color[66];
    Mesh impactCorona;
    void FlowingFlightRibbon(int choice,Vector3 source,Vector3 target,float age,float hit,Color color)
    {
        float head=Mathf.Clamp01((age-FinisherLaunchTime)/(hit-FinisherLaunchTime));if(head<=.001f)return;
        float tail=Mathf.Max(0,head-.34f);
        for(int layer=0;layer<2;layer++)
        {
            if(flightRibbons[layer]==null)
            {
                int[] triangles=new int[32*6];
                for(int i=0;i<32;i++){int n=i*2,t=i*6;triangles[t]=n;triangles[t+1]=n+1;triangles[t+2]=n+2;triangles[t+3]=n+1;triangles[t+4]=n+3;triangles[t+5]=n+2;}
                flightRibbons[layer]=Own(new Mesh{name="Continuous homing ribbon "+layer});flightRibbons[layer].MarkDynamic();
                flightRibbons[layer].vertices=ribbonVertices;flightRibbons[layer].triangles=triangles;
            }
            for(int i=0;i<=32;i++)
            {
                float u=i/32f,t=Mathf.Lerp(tail,head,u);Vector3 p=FlightTrailPoint(source,target,choice,t,finisherHeight);
                Vector3 near=FlightTrailPoint(source,target,choice,Mathf.Max(0,t-.005f),finisherHeight);
                Vector3 direction=p-near;if(direction.sqrMagnitude<.000001f)direction=target-source;
                if(direction.sqrMagnitude<.000001f)direction=Vector3.up;
                Vector3 side=Vector3.Cross(camera.transform.forward,direction).normalized;
                float width=(choice==1?.20f:.14f)*(layer==0?1:.24f)*Mathf.Sin(u*Mathf.PI*.5f);
                ribbonVertices[i*2]=p-side*width;ribbonVertices[i*2+1]=p+side*width;
                Color tint=layer==0?color:Color.Lerp(color,Color.white,.88f);tint.a=Mathf.Pow(u,1.5f)*(layer==0?.48f:.88f);
                ribbonColors[i*2]=ribbonColors[i*2+1]=tint;
            }
            Mesh mesh=flightRibbons[layer];mesh.vertices=ribbonVertices;mesh.colors=ribbonColors;mesh.RecalculateBounds();
            Effect(mesh,Vector3.zero,Vector3.one,Color.white,Quaternion.identity,transparentSkillMaterial);
        }
    }
    Mesh ImpactCorona()
    {
        if(impactCorona!=null)return impactCorona;
        const int sectors=64;var vertices=new Vector3[(sectors+1)*3];var colors=new Color[vertices.Length];var indices=new int[sectors*12];
        for(int i=0;i<=sectors;i++)
        {
            float a=i*Mathf.PI*2/sectors;Vector3 ray=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
            vertices[i*3]=ray*.70f;vertices[i*3+1]=ray*.93f+Vector3.up*(.20f+.05f*Mathf.Sin(a*11));vertices[i*3+2]=ray*1.06f;
            colors[i*3]=new Color(1,1,1,0);colors[i*3+1]=Color.white;colors[i*3+2]=new Color(1,1,1,0);
            if(i==sectors)continue;
            for(int layer=0;layer<2;layer++){int n=i*3+layer,t=i*12+layer*6;indices[t]=n;indices[t+1]=n+3;indices[t+2]=n+1;indices[t+3]=n+1;indices[t+4]=n+3;indices[t+5]=n+4;}
        }
        impactCorona=Own(new Mesh{name="Sculpted luminous impact crown",vertices=vertices,triangles=indices});impactCorona.colors=colors;return impactCorona;
    }
    void FinisherImpactCraft(int choice,Vector3 impact,float after,Color color)
    {
        if(after<0)return;float pulse=Mathf.Exp(-after*4.5f),expand=1-Mathf.Exp(-after*6);
        if(pulse<.006f)return;
        Color bright=Color.Lerp(color,Color.white,.35f);bright.a=pulse*.8f;
        float radius=.35f+expand*(choice==1?2.8f:2.3f);
        Effect(ImpactCorona(),impact+Vector3.up*.035f,new Vector3(radius,(choice==1?2.4f:1.6f)*finisherHeight,radius),bright,Quaternion.Euler(0,after*16,0),transparentSkillMaterial);
        for(int i=0;i<12;i++)
        {
            float a=i*2.39996f;Vector3 ray=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
            float lift=Mathf.Max(0,(1.9f+i%3*.35f)*after-1.8f*after*after)*finisherHeight;
            Vector3 tip=impact+ray*(.18f+expand*(1.0f+i%4*.21f))+Vector3.up*(.20f+lift);
            SoftSkillGlow(tip,.075f+(i%3)*.017f,Color.Lerp(color,Color.white,.65f),pulse*.82f);
        }
        // Short opposing lens petals give impact a bright core without filling the arena with opaque shapes.
        if(after<.30f)
        {
            float flash=Mathf.Pow(1-after/.30f,2);
            Effect(skillGlowQuad,impact+Vector3.up*.56f,new Vector3(3.3f,.085f,1),new Color(1,.98f,.88f,flash*.70f),camera.transform.rotation,softSkillMaterial);
            Effect(skillGlowQuad,impact+Vector3.up*.56f,new Vector3(.085f,1.6f*finisherHeight,1),new Color(1,.98f,.88f,flash*.55f),camera.transform.rotation,softSkillMaterial);
        }
    }
}
