using UnityEngine;

public sealed partial class TacticalArena
{
    readonly Material[] atmosphereMaterials=new Material[3];
    MeshRenderer atmosphere;
    Mesh atmosphereQuad;
    void SetAtmosphere(int id)
    {
        if(atmosphereMaterials[id]==null)
        {
            const int width=512,height=256;var pixels=new Color32[width*height];
            Color low=id==0?new Color(.025f,.085f,.11f):id==1?new Color(.035f,.075f,.17f):new Color(.035f,.018f,.082f);
            Color high=id==0?new Color(.25f,.39f,.35f):id==1?new Color(.19f,.31f,.46f):new Color(.24f,.12f,.36f);
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                float u=x/(float)width,v=y/(float)height;
                float clouds=Mathf.PerlinNoise(u*5+id*17,v*4)*Mathf.PerlinNoise(u*13,v*9+6);
                float horizon=Mathf.Exp(-(v-.54f)*(v-.54f)*8);
                Color c=Color.Lerp(low,high,horizon*(.25f+clouds*.7f));
                if(id==0)
                {
                    float sun=(new Vector2((u-.73f)*1.8f,v-.75f)).magnitude;
                    c=Color.Lerp(c,new Color(.91f,.82f,.48f),Mathf.Exp(-sun*sun*160)*.45f);
                    for(int layer=0;layer<4;layer++)
                    {
                        float ridge=.39f-layer*.085f+Mathf.PerlinNoise(u*(7+layer*4)+layer*11,2)*(.17f+layer*.035f);
                        if(v<ridge)c=Color.Lerp(c,low,.28f+layer*.12f);
                    }
                }
                else if(id==1)
                {
                    float wave=.71f+Mathf.Sin(u*8)*.075f+Mathf.Sin(u*19)*.019f;
                    float aurora=Mathf.Exp(-Mathf.Abs(v-wave)*28)*(Mathf.Sin(u*77)*.15f+.75f);
                    c=Color.Lerp(c,new Color(.24f,.71f,.65f),aurora*.34f);
                    float ridge=.32f+Mathf.Abs(Mathf.Sin(u*17))*.12f+Mathf.PerlinNoise(u*27,3)*.06f;
                    if(v<ridge)c=Color.Lerp(c,new Color(.09f,.16f,.26f),.64f);
                }
                else
                {
                    float cloudBand=Mathf.Exp(-Mathf.Pow(v-.75f+u*.15f-Mathf.Sin(u*5)*.06f,2)*48);
                    c=Color.Lerp(c,new Color(.41f,.25f,.57f),cloudBand*clouds*.56f);
                    float orbit=Mathf.Abs(new Vector2((u-.61f)*1.6f,(v-.63f)*1.4f).magnitude-.36f);
                    if(orbit<.003f)c=Color.Lerp(c,new Color(.6f,.36f,.7f),.25f);
                }
                uint star=(uint)(x+y*width+19);star^=star<<13;star^=star>>17;star^=star<<5;
                if(id>0&&v>.43f&&star%997<2)c=Color.Lerp(c,new Color(.76f,.84f,1),.55f);
                pixels[y*width+x]=c;
            }
            var texture=new Texture2D(width,height,TextureFormat.RGBA32,false){name="Field atmosphere "+id,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            texture.SetPixels32(pixels);texture.Apply(false,true);resources.Add(texture);
            var surface=Material(Color.white,true,texture);surface.renderQueue=1900;
            if(surface.HasProperty("_ZWrite"))surface.SetInt("_ZWrite",0);
            atmosphereMaterials[id]=surface;
        }
        if(atmosphere==null)
        {
            atmosphereQuad=Own(new Mesh{name="Camera sky plane",vertices=quad.vertices,uv=quad.uv,triangles=quad.triangles});
            atmosphereQuad.colors=new[]{Color.white,Color.white,Color.white,Color.white};
            atmosphere=Shape("Field atmosphere",atmosphereQuad,atmosphereMaterials[id],new Vector3(0,0,50),new Vector3(100,100,1),camera.transform);
        }
        atmosphere.sharedMaterial=atmosphereMaterials[id];
    }
    void FitAtmosphere()
    {
        if(atmosphere==null)return;
        float height=100*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f);
        atmosphere.transform.localScale=new Vector3(height*camera.aspect/1.3f,height/1.3f,1)*1.015f;
    }
    void BuildFieldTerraces(int id,Transform p,Material stoneSurface,Material metal,Material foliage,Material light)
    {
        for(int side=-1;side<=1;side+=2)
        {
            for(int i=0;i<5;i++)
            {
                float z=-3.75f+i*1.85f;
                Shape("Perimeter paving",hex,stoneSurface,new Vector3(side*5.61f,.13f,z),new Vector3(.34f,.12f,.73f),p);
                Shape("Inlaid edge marker",cube,metal,new Vector3(side*5.54f,.21f,z),new Vector3(.22f,.025f,.04f),p);
            }
            for(int i=0;i<3;i++)
            {
                float z=-2.9f+i*3.15f;
                var platform=Shape("Floating terrace",hex,stoneSurface,new Vector3(side*6.47f,-.54f,z),new Vector3(.89f,.40f,1.02f),p);
                platform.transform.localRotation=Quaternion.Euler(0,i*17,0);
                var suspended=Shape("Suspended island root",SkillTip(),stoneSurface,new Vector3(side*6.52f,-.56f,z),new Vector3(.65f,1.55f,.70f),p);
                suspended.transform.localRotation=Quaternion.Euler(0,i*19,180);
                if(id==0)
                {
                    Shape("Terrace moss",hex,foliage,new Vector3(side*6.48f,-.32f,z),new Vector3(.65f,.04f,.70f),p);
                    for(int j=0;j<3;j++)Shape("Waterfall thread",cube,light,new Vector3(side*(6.63f+j*.09f),-.99f,z-.27f),new Vector3(.034f,1.3f-j*.13f,.045f),p);
                }
                else if(id==1)
                {
                    var glacier=Shape("Outlying glacier",SkillTip(),foliage,new Vector3(side*6.6f,-.32f,z),new Vector3(.44f,1.15f,.39f),p);
                    glacier.transform.localRotation=Quaternion.Euler(0,i*37,side*14);
                    Shape("Glacier inset",hex,light,new Vector3(side*6.46f,-.31f,z),new Vector3(.55f,.016f,.57f),p);
                }
                else
                {
                    var core=Shape("Suspended data crystal",cube,light,new Vector3(side*6.62f,.03f,z),Vector3.one*.29f,p);
                    core.transform.localRotation=Quaternion.Euler(35,45,35);
                    Shape("Data terrace orbit",ringMesh,metal,new Vector3(side*6.61f,-.16f,z),Vector3.one*.49f,p);
                }
            }
        }
        // Decorative floor medallions live beyond the final unit row.
        for(int side=-1;side<=1;side+=2)
        {
            Vector3 center=new Vector3(side*3.8f,.16f,5.65f);
            Shape("Gate approach medallion",ringMesh,metal,center,Vector3.one*.31f,p);
            Shape("Gate approach gem",hex,light,center+Vector3.up*.02f,new Vector3(.095f,.018f,.095f),p);
        }
    }
}
