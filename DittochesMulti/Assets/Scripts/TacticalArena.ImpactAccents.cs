using UnityEngine;

public sealed partial class TacticalArena
{
    Mesh impactFlare;
    Mesh ImpactFlareMesh()
    {
        if(impactFlare!=null)return impactFlare;
        // One smooth star-shaped surface: bright center, transparent tapered rays.
        const int segments=32;var vertices=new Vector3[segments+1];var colors=new Color[segments+1];var triangles=new int[segments*3];
        colors[0]=Color.white;
        for(int i=0;i<segments;i++)
        {
            float a=i*Mathf.PI*2/segments,r=i%4==0?1:i%2==0?.5f:.17f;
            vertices[i+1]=new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,0);colors[i+1]=new Color(1,1,1,0);
            triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=(i+1)%segments+1;
        }
        impactFlare=Own(new Mesh{name="Soft tapered impact flare",vertices=vertices,triangles=triangles});impactFlare.colors=colors;return impactFlare;
    }
    void CombatFlare(Vector3 center,Vector3 direction,float size,Color color,float roll=0,float aspect=1)
    {
        if(size<=.001f||color.a<=.002f||!EnsureSkillFinish())return;
        float facing=Mathf.Atan2(Vector3.Dot(direction,camera.transform.up),Vector3.Dot(direction,camera.transform.right))*Mathf.Rad2Deg;
        Effect(ImpactFlareMesh(),center,new Vector3(size,size*aspect,1),color,camera.transform.rotation*Quaternion.Euler(0,0,roll+facing),transparentSkillMaterial);
    }
    void CombatImpactAccent(CombatEffectStyle.Element element,Vector3 center,Vector3 direction,float t,float size,int seed,Color color,bool major)
    {
        float flash=Mathf.Clamp01(1-t/.58f);flash*=flash;
        if(flash<=.002f)return;
        Color core=Color.Lerp(color,Color.white,element==CombatEffectStyle.Element.Shadow?.3f:.78f);core.a=flash*(major?.95f:.82f);
        bool liquid=element==CombatEffectStyle.Element.Water||element==CombatEffectStyle.Element.Nature;
        if(liquid)SoftSkillGlow(center,size*(1.1f+t),core,.8f);
        else CombatFlare(center,direction,size*(1.7f+t*2.4f),core,seed*47,element==CombatEffectStyle.Element.Force?.58f:1);
    }
    void CombatLaunch(CombatEffectStyle.Element element,Vector3 start,Vector3 direction,float age,float size,Color color,bool major)
    {
        float duration=major?.095f:.07f;
        if(age<0||age>=duration||size<=0||!EnsureSkillFinish())return;
        float t=age/duration,fade=(1-t)*(1-t);direction=direction.sqrMagnitude>.0001f?direction.normalized:Vector3.forward;
        // Exactly the cached joint position, never a target-bound contact-depth offset.
        SoftSkillGlow(start,size*(1.3f+t*.4f),color,fade*.5f);
        bool energetic=element==CombatEffectStyle.Element.Force||element==CombatEffectStyle.Element.Flame||element==CombatEffectStyle.Element.Storm||element==CombatEffectStyle.Element.Light;
        if(energetic)
        {
            Color core=Color.Lerp(color,Color.white,.65f);core.a=fade*.72f;
            CombatFlare(start,direction,size*(1.25f+t*.5f),core,0,.6f);
            color.a=fade*.65f;CombatTrail(start+direction*size*(1+t*.7f),direction,size*1.5f,size*.30f,color);
        }
    }
    void CombatSpark(Vector3 point,Vector3 direction,float length,float width,Color color)
    {
        // Fading ribbon tips read as moving sparks without opaque cylinder end caps.
        CombatTrail(point,direction,Mathf.Max(.005f,length),width,color);
    }
}
