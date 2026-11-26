using UnityEngine;

public sealed partial class TacticalArena
{
    public const float FinisherDuration=3.2f;
    public const float FinisherLaunchTime=.28f;
    public static float FinisherImpactTime(int choice){return choice==0?1.10f:choice==1?1.40f:1.50f;}
    Vector3 finisherCameraOffset,finisherSource,finisherDestination;
    float finisherCameraPush,previousFinisherAge=-1,finisherHeight=1;
    int previousFinisherChoice=-1,previousFinisherGeneration=-10;
    Mesh finisherRing,meteorCore,meteorCrust;
    bool finisherFollowing;
    readonly Vector3[] finisherTrail=new Vector3[64];
    readonly float[] finisherTrailAges=new float[64];
    int finisherTrailCount,finisherTrailCursor;
    public void DrawFinisher(int choice,Vector3 source,Vector3 destination,float age,bool followTarget=false)
    {
        if(float.IsNaN(age)||float.IsInfinity(age)||age<0||age>=FinisherDuration)return;
        choice=Mathf.Clamp(choice,0,2);
        if(choice!=previousFinisherChoice||age<previousFinisherAge||generation>previousFinisherGeneration+1||followTarget!=finisherFollowing)
        {
            finisherSource=source;finisherDestination=destination;finisherTrailCount=finisherTrailCursor=0;
            finisherHeight=Mathf.Min(FitFinisherHeight(destination+Vector3.up*.07f,5.2f),FitFinisherHeight(source+Vector3.up*.07f,3.0f));
        }
        if(followTarget)
        {
            // Keep the launch origin after release. Track only until the first impact frame.
            if(previousFinisherAge<FinisherLaunchTime)finisherSource=source;
            if(previousFinisherAge<FinisherImpactTime(choice))finisherDestination=destination;
            finisherHeight=Mathf.Min(finisherHeight,FitFinisherHeight(finisherDestination+Vector3.up*.07f,5.2f));
        }
        finisherFollowing=followTarget;
        previousFinisherAge=age;previousFinisherChoice=choice;previousFinisherGeneration=generation;
        source=finisherSource;destination=finisherDestination;
        EnsureSkillFinish();Color color=TamerLoadout.FinishColor(choice);
        Vector3 impact=destination+Vector3.up*.07f;
        float hit=FinisherImpactTime(choice);
        if(followTarget&&age<hit)
        {
            finisherTrail[finisherTrailCursor]=FlightHead(source,impact,choice,age,hit);
            finisherTrailAges[finisherTrailCursor]=age;finisherTrailCursor=(finisherTrailCursor+1)%64;finisherTrailCount=Mathf.Min(64,finisherTrailCount+1);
        }
        float after=age-hit,tail=Mathf.Clamp01(1-Mathf.Max(0,after)/(FinisherDuration-hit));
        if(after>=0)
        {
            float punch=Mathf.Exp(-after*10)*(choice==1?1.12f:.68f);
            finisherCameraPush=.52f*punch;
            finisherCameraOffset=new Vector3(Mathf.Sin(after*71)*.045f,Mathf.Sin(after*53)*.025f,0)*punch;
            GroundRadiance(impact,1.1f+after*1.1f,color,.33f*tail*tail);
            SoftSkillGlow(impact+Vector3.up*.6f,1.6f,color,.80f*Mathf.Exp(-after*6));
        }
        if(choice==0)DataPulseFinisher(source,impact,age,hit,color,tail);
        else if(choice==1)GaiaFinisher(source,impact,age,hit,color,tail);
        else HolyGateFinisher(source,impact,age,hit,color,tail);
        DrawFinisherAccent(choice,source,impact,age,hit,color,tail);
        FinisherImpactCraft(choice,impact,after,color);
    }
    static Vector3 FlightPoint(Vector3 source,Vector3 impact,int choice,float progress,float height)
    {
        float t=Mathf.Clamp01(progress),eased=t*t*(3-2*t);
        Vector3 point=Vector3.Lerp(source+Vector3.up*.65f,impact+Vector3.up*.55f,eased);
        return point+Vector3.up*(Mathf.Sin(t*Mathf.PI)*(choice==1?1.25f:choice==2?.8f:.4f)*height);
    }
    Vector3 FlightHead(Vector3 source,Vector3 impact,int choice,float age,float hit)
    {return FlightPoint(source,impact,choice,(age-FinisherLaunchTime)/(hit-FinisherLaunchTime),finisherHeight);}
    Vector3 FlightTrailPoint(Vector3 source,Vector3 impact,int choice,float progress,float height)
    {
        if(!finisherFollowing||finisherTrailCount==0)return FlightPoint(source,impact,choice,progress,height);
        float age=Mathf.Lerp(FinisherLaunchTime,FinisherImpactTime(choice),progress);
        int newer=(finisherTrailCursor+63)%64;
        for(int i=1;i<finisherTrailCount;i++)
        {
            int older=(finisherTrailCursor+63-i)%64;
            if(finisherTrailAges[older]<=age)return Vector3.Lerp(finisherTrail[older],finisherTrail[newer],Mathf.InverseLerp(finisherTrailAges[older],finisherTrailAges[newer],age));
            newer=older;
        }
        return finisherTrail[newer];
    }
    float FitFinisherHeight(Vector3 impact,float envelope)
    {
        // Fit the complete vertical silhouette, including its crown and glow, in
        // the current field camera. Keep the result fixed throughout the shot.
        float low=0,high=1;
        for(int i=0;i<10;i++)
        {
            float scale=(low+high)*.5f;
            Vector3 top=camera.WorldToViewportPoint(root.transform.TransformPoint(impact+Vector3.up*(envelope*scale)));
            if(top.z>0&&top.y<.91f)low=scale;else high=scale;
        }
        return low;
    }
    void GroundRadiance(Vector3 point,float radius,Color color,float alpha)
    {
        if(!EnsureSkillFinish()||alpha<=.001f)return;color.a*=alpha;
        Effect(skillGlowQuad,point,Vector3.one*(radius/.65f),color,Quaternion.Euler(90,0,0),softSkillMaterial);
    }
    void FinishLine(Vector3 from,Vector3 to,float width,Color color,float alpha=1)
    {
        Vector3 delta=to-from;if(delta.sqrMagnitude<.00001f||alpha<.001f)return;color.a*=alpha;
        Effect(cube,(from+to)*.5f,new Vector3(width,width,delta.magnitude),color,Quaternion.LookRotation(delta),transparentSkillMaterial);
    }
    void FinishRing(Vector3 point,float radius,Color color,float fade,Quaternion rotation)
    {
        if(finisherRing==null)
        {
            const int count=80;var vertices=new Vector3[count*2];var triangles=new int[count*6];
            for(int i=0;i<count;i++)
            {
                float a=i*Mathf.PI*2/count;Vector3 p=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                vertices[i*2]=p;vertices[i*2+1]=p*.96f;int n=i*2,next=(i+1)%count*2,t=i*6;
                triangles[t]=n;triangles[t+1]=next;triangles[t+2]=n+1;triangles[t+3]=n+1;triangles[t+4]=next;triangles[t+5]=next+1;
            }
            finisherRing=Own(new Mesh{name="Fine finisher wavefront",vertices=vertices,triangles=triangles});
        }
        color.a*=fade;Effect(finisherRing,point,Vector3.one*radius,color,rotation,transparentSkillMaterial);
    }

    Mesh MeteorMesh(bool crust)
    {
        var cached=crust?meteorCrust:meteorCore;if(cached!=null)return cached;
        float golden=(1+Mathf.Sqrt(5))*.5f;
        var corners=new[]{new Vector3(-1,golden,0),new Vector3(1,golden,0),new Vector3(-1,-golden,0),new Vector3(1,-golden,0),
            new Vector3(0,-1,golden),new Vector3(0,1,golden),new Vector3(0,-1,-golden),new Vector3(0,1,-golden),
            new Vector3(golden,0,-1),new Vector3(golden,0,1),new Vector3(-golden,0,-1),new Vector3(-golden,0,1)};
        int[] faces={0,11,5,0,5,1,0,1,7,0,7,10,0,10,11,1,5,9,5,11,4,11,10,2,10,7,6,7,1,8,
            3,9,4,3,4,2,3,2,6,3,6,8,3,8,9,4,9,5,2,4,11,6,2,10,8,6,7,9,8,1};
        for(int i=0;i<corners.Length;i++)corners[i]=corners[i].normalized;
        var vertices=new Vector3[faces.Length*4];var triangles=new int[faces.Length*4];int cursor=0;
        for(int i=0;i<faces.Length;i+=3)
        {
            Vector3 a=corners[faces[i]],b=corners[faces[i+1]],c=corners[faces[i+2]];
            Vector3 ab=(a+b).normalized,bc=(b+c).normalized,ca=(c+a).normalized;
            for(int part=0;part<4;part++)
            {
                Vector3 p=MeteorPoint(part==0?a:part==1?ab:part==2?ca:ab);
                Vector3 q=MeteorPoint(part==0?ab:part==1?b:part==2?bc:bc);
                Vector3 r=MeteorPoint(part==0?ca:part==1?bc:part==2?c:ca);
                Vector3 center=(p+q+r)/3;float inset=.07f+.025f*Mathf.Sin(i*1.73f+part*2.1f);
                vertices[cursor]=crust?Vector3.Lerp(p,center,inset)*1.022f:p;
                vertices[cursor+1]=crust?Vector3.Lerp(q,center,inset)*1.022f:q;
                vertices[cursor+2]=crust?Vector3.Lerp(r,center,inset)*1.022f:r;
                for(int j=0;j<3;j++)triangles[cursor+j]=cursor+j;cursor+=3;
            }
        }
        cached=Own(new Mesh{name=crust?"Fractured meteor crust":"Molten meteor core",vertices=vertices,triangles=triangles});
        if(crust)meteorCrust=cached;else meteorCore=cached;return cached;
    }
    static Vector3 MeteorPoint(Vector3 point)
    {
        point.Normalize();
        float relief=.075f*Mathf.Sin(point.x*9+point.y*4)*Mathf.Cos(point.z*7-point.y*3)+.025f*Mathf.Sin(point.z*13+point.x*7);
        return point*(1+relief);
    }


}
