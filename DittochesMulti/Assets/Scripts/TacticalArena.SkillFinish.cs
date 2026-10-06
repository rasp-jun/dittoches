using UnityEngine;

public sealed partial class TacticalArena
{
    Material softSkillMaterial,transparentSkillMaterial,roundedSkillMaterial;
    Texture2D softSkillTexture;
    Mesh skillTip,skillGlowQuad;
    bool skillFinishAttempted;
    bool EnsureSkillFinish()
    {
        if(!presentationQuality)return false;
        if(skillFinishAttempted)return softSkillMaterial!=null;
        skillFinishAttempted=true;
        Shader shader=Shader.Find("Sprites/Default");if(shader==null||!shader.isSupported)return false;
        const int size=64;var pixels=new Color32[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float u=(x+.5f)*2/size-1,v=(y+.5f)*2/size-1,r=u*u+v*v;
            float alpha=Mathf.Exp(-4.5f*r)*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.55f,1,r)));
            pixels[y*size+x]=new Color(1,1,1,alpha);
        }
        softSkillTexture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Skill radiance",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
        softSkillTexture.SetPixels32(pixels);softSkillTexture.Apply(false,true);resources.Add(softSkillTexture);
        softSkillMaterial=new Material(shader){name="Soft skill radiance",color=Color.white,mainTexture=softSkillTexture,renderQueue=3000,hideFlags=HideFlags.HideAndDontSave};resources.Add(softSkillMaterial);
        transparentSkillMaterial=new Material(shader){name="Fading skill geometry",color=Color.white,mainTexture=Texture2D.whiteTexture,renderQueue=3000,hideFlags=HideFlags.HideAndDontSave};resources.Add(transparentSkillMaterial);
        // Sprite shaders multiply mesh vertex tint. Give glows a white mesh, independent of the arena's baked shading.
        skillGlowQuad=Own(new Mesh{name="Skill glow billboard",vertices=quad.vertices,uv=quad.uv,triangles=quad.triangles});
        skillGlowQuad.colors=new[]{Color.white,Color.white,Color.white,Color.white};
        roundedSkillMaterial=Material(Color.white);if(roundedSkillMaterial.HasProperty("_Unlit"))roundedSkillMaterial.SetFloat("_Unlit",.45f);
        return true;
    }
    void SoftSkillGlow(Vector3 p,float radius,Color color,float alpha=.35f)
    {
        if(radius<=.001f||alpha<=.001f||!EnsureSkillFinish())return;
        color.a*=alpha;
        Effect(skillGlowQuad,p-camera.transform.forward*.025f,Vector3.one*(radius/.65f),color,camera.transform.rotation,softSkillMaterial);
    }
    Mesh SkillTip()
    {
        if(skillTip!=null)return skillTip;
        var vertices=new Vector3[24];var triangles=new int[24];
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.PI/4,b=(i+1)*Mathf.PI/4;int n=i*3;
            vertices[n]=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));vertices[n+1]=Vector3.up;vertices[n+2]=new Vector3(Mathf.Cos(b),0,Mathf.Sin(b));
            triangles[n]=n;triangles[n+1]=n+1;triangles[n+2]=n+2;
        }
        skillTip=Own(new Mesh{name="Skill tapered tip",vertices=vertices,triangles=triangles});return skillTip;
    }
    bool DrawFinishedFlight(DigimonSkillCatalog.Entry skill,Vector3 start,Vector3 end,Vector3 p,Vector3 side,float t,float age,Color color)
    {
        if(!EnsureSkillFinish())return false;
        Vector3 forward=(end-start).normalized;
        if(skill.visual=="fire"||skill.visual=="bluefire")
        {
            Orb(p,.15f,color);
            for(int i=1;i<=4;i++)
            {
                float f=i/4f;Vector3 tail=p-forward*(.13f+i*.105f)+Vector3.up*(.035f+Mathf.Sin(age*21+i)*.055f)*f;
                Color ember=Color.Lerp(color,skill.visual=="fire"?new Color(1,.17f,.035f):new Color(.15f,.32f,1),f);
                SoftSkillGlow(tail,.18f*(1-f*.55f),ember,.55f*(1-f*.45f));
            }
            return true;
        }
        if(skill.visual=="missiles")
        {
            forward=(end-start+Vector3.up*(Mathf.Cos(t*Mathf.PI)*.7f*Mathf.PI)).normalized;
            Quaternion along=Quaternion.FromToRotation(Vector3.up,forward),fin=Quaternion.LookRotation(forward);
            Effect(hex,p,new Vector3(.078f,.30f,.078f),new Color(.57f,.67f,.76f),along,stone);
            Effect(SkillTip(),p+forward*.15f,new Vector3(.08f,.14f,.08f),new Color(.92f,.94f,.96f),along,stone);
            Effect(cube,p-forward*.1f,new Vector3(.27f,.018f,.11f),new Color(.29f,.37f,.45f),fin,stone);
            Effect(cube,p-forward*.1f,new Vector3(.018f,.27f,.11f),new Color(.29f,.37f,.45f),fin,stone);
            SoftSkillGlow(p-forward*.22f,.19f,color,.85f);
            for(int i=1;i<=4;i++)SoftSkillGlow(p-forward*(.17f+i*.13f),.10f+i*.017f,Color.Lerp(color,new Color(.52f,.57f,.61f),i/4f),.42f*(1-i/5f));
            return true;
        }
        if(skill.visual=="ice")
        {
            for(int i=0;i<7;i++)
            {
                float progress=Mathf.Clamp01(t*1.12f-i*.04f);
                Vector3 at=Vector3.Lerp(start,end,progress)+side*Mathf.Sin(i*3.7f)*progress*.36f;
                Vector3 direction=(forward+side*Mathf.Sin(i*2)*.15f).normalized;
                Effect(SkillTip(),at-direction*.12f,new Vector3(.085f,.34f,.07f),Color.Lerp(color,Color.white,(i%3)*.2f),Quaternion.FromToRotation(Vector3.up,direction),stone);
                SoftSkillGlow(at,.12f,color,.28f);
            }
            return true;
        }
        return false;
    }
    void DrawFinishedImpact(DigimonSkillCatalog.Entry skill,Vector3 center,float age,int shot,Color color)
    {
        if(age<0||age>=.3f||!EnsureSkillFinish())return;
        float t=age/.3f,fade=(1-t)*(1-t);
        SoftSkillGlow(center+Vector3.up*.5f,.23f+t*.55f,color,.65f*fade);
        // Small deterministic bursts leave the silhouette and health bars readable.
        int count=skill.visual=="bubble"?3:6;
        for(int i=0;i<count;i++)
        {
            float angle=i*Mathf.PI*2/count+shot*.73f;
            Vector3 direction=new Vector3(Mathf.Cos(angle),.5f+(i%2)*.45f,Mathf.Sin(angle)).normalized;
            Vector3 p=center+Vector3.up*.35f+direction*(.12f+t*.65f)-Vector3.up*t*t*.18f;
            Color spark=Color.Lerp(color,Color.white,.48f);spark.a=fade*.8f;
            Vector3 length=direction*(.055f+fade*.085f);
            Effect(cube,p, new Vector3(.018f,.018f,length.magnitude),spark,Quaternion.LookRotation(direction),transparentSkillMaterial);
        }
    }
#if DITTOCHES_PORTABLE_PREVIEW
    public bool SkillSphereFacesOutward()
    {
        Mesh mesh=EffectSphere();var vertices=mesh.vertices;var normals=mesh.normals;
        for(int i=0;i<vertices.Length;i++)
            if(Mathf.Abs(vertices[i].y)<.99f&&Vector3.Dot(vertices[i].normalized,normals[i])<.9f)return false;
        return true;
    }
    public int SkillPoolSize {get{return effectPool.Count;}}
    public bool SkillFinishResourcesValid {get{return softSkillMaterial!=null&&transparentSkillMaterial!=null&&softSkillTexture!=null;}}
    public bool SkillEffectsHealthy()
    {
        for(int i=0;i<effectPool.Count;i++)
        {
            var renderer=effectPool[i];
            if(i>=effectCursor){if(renderer.enabled)return false;continue;}
            Vector3 p=renderer.transform.localPosition,scale=renderer.transform.localScale;
            if(!renderer.enabled||float.IsNaN(p.sqrMagnitude)||float.IsInfinity(p.sqrMagnitude)||scale.magnitude>15||renderer.sharedMaterial==null)return false;
        }
        return effectCursor<EffectBudget;
    }
#endif
}
