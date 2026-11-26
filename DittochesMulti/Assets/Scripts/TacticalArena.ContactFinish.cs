using UnityEngine;

public sealed partial class TacticalArena
{
    Vector3 ContactPoint(Vector3 center,float depth)
    {
        Vector3 toward=root.transform.InverseTransformPoint(camera.transform.position)-center;
        return center+toward.normalized*Mathf.Min(depth,Mathf.Max(0,toward.magnitude-.1f));
    }
    public float ContactSurface(object key,float height,float fallback)
    {
        Actor actor;
        if(key==null||!actors.TryGetValue(key,out actor)||actor.generation!=generation||actor.faithfulGeneration!=generation||actor.faithful==null)return fallback;
        var model=actor.faithful;var bounds=model.Data.restBounds;
        Vector3 worldPoint=root.transform.TransformPoint(actor.root.transform.localPosition+Vector3.up*height);
        Vector3 origin=model.transform.InverseTransformPoint(worldPoint);
        Vector3 direction=model.transform.InverseTransformVector((camera.transform.position-worldPoint).normalized);
        // Exit the cached rest bounds toward the camera at the actual torso point. No skin baking or allocations per hit.
        float near=0,far=3;Vector3 min=bounds.min,max=bounds.max;
        for(int axis=0;axis<3;axis++)
        {
            if(Mathf.Abs(direction[axis])<.00001f){if(origin[axis]<min[axis]||origin[axis]>max[axis])return fallback;continue;}
            float a=(min[axis]-origin[axis])/direction[axis],b=(max[axis]-origin[axis])/direction[axis];
            near=Mathf.Max(near,Mathf.Min(a,b));far=Mathf.Min(far,Mathf.Max(a,b));if(near>far)return fallback;
        }
        return Mathf.Max(fallback,Mathf.Clamp(far+.04f,.14f,3));
    }
    bool DrawContactFinish(BasicAttackPresentation.Kind kind,string id,Vector3 start,Vector3 end,Vector3 center,Vector3 direction,float u,float size,Color color,int serial)
    {
        if(!EnsureSkillFinish())return false;
        Vector3 side=camera.transform.right,up=camera.transform.up;Color light=Color.Lerp(color,Color.white,.55f);light.a=color.a;
        switch(kind)
        {
            case BasicAttackPresentation.Kind.Bite:
                for(int jaw=-1;jaw<=1;jaw+=2)
                {
                    Vector3 jawCenter=center+up*jaw*size*(1-u)*.30f;
                    CombatSweep(jawCenter,direction,size*(1.2f-u*.3f),jaw<0?180:0,light,.55f);
                    for(int tooth=-1;tooth<=1;tooth++)
                    {
                        Vector3 p=jawCenter+side*tooth*size*.55f+up*jaw*size*.48f;
                        Effect(SkillTip(),p,new Vector3(size*.11f,size*.34f,size*.08f),light,Quaternion.FromToRotation(Vector3.up,-up*jaw),transparentSkillMaterial);
                    }
                }
                return true;
            case BasicAttackPresentation.Kind.Punch:
                CombatTrail(center,direction,size*(1.7f-u*.8f),size*.45f,light);
                for(int i=0;i<3;i++)
                {
                    Vector3 offset=up*(i-1)*size*.38f;
                    SkillBeam(center-direction*size*1.1f+offset,center+offset,.022f*(1-u),light);
                }
                CombatSweep(center,direction,size*(.9f+u*1.1f),serial%2==0?30:210,color,.55f);
                return true;
            case BasicAttackPresentation.Kind.Horn:
                Effect(SkillTip(),center-direction*size*.9f,new Vector3(size*.33f,size*1.45f,size*.27f),light,Quaternion.FromToRotation(Vector3.up,direction),transparentSkillMaterial);
                CombatTrail(center-direction*size*.25f,direction,size*1.5f,size*.3f,color);
                CombatSweep(center,direction,size*(.65f+u),u*160,light,.6f);return true;
            case BasicAttackPresentation.Kind.Bash:
                CombatSweep(center,direction,size*(.8f+u*1.5f),u*80,light,.8f);
                CombatSweep(center,direction,size*(.65f+u*1.2f),180-u*65,color,.65f);return true;
            case BasicAttackPresentation.Kind.Whip:
                Vector3 previous=start;Vector3 across=Side(direction);
                for(int i=1;i<=16;i++)
                {
                    float f=i/16f;Vector3 next=Vector3.Lerp(start,end,f)+across*Mathf.Sin(f*Mathf.PI)*Mathf.Sin(u*Mathf.PI*2)*(id=="devimon"?.13f:.32f);
                    SkillBeam(previous,next,.03f*(1-f*.7f),color);previous=next;
                }
                CombatSweep(center,direction,size*1.1f,-60+u*170,light,.55f);return true;
            default:return false;
        }
    }
}
