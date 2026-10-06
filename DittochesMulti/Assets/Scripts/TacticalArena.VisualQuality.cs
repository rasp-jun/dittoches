using System.Collections.Generic;
using UnityEngine;

public sealed partial class TacticalArena
{
    bool presentationQuality;
    Mesh contactMesh;
    Material contactMaterial;
    Texture2D contactTexture;
    static float ContactOpacity(float x,float y)
    {
        float r=x*x+y*y;
        return (.38f*Mathf.Exp(-14*r)+.25f*Mathf.Exp(-3.5f*r))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1,r)));
    }
    void CreateContactResources()
    {
        if(!presentationQuality)return;
        Shader shader=Shader.Find("Sprites/Default");if(shader==null||!shader.isSupported)return;
        const int size=96;contactTexture=new Texture2D(size,size,TextureFormat.RGBA32,false);
        contactTexture.name="Soft ground contact";contactTexture.wrapMode=TextureWrapMode.Clamp;contactTexture.filterMode=FilterMode.Bilinear;
        var pixels=new Color32[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)pixels[y*size+x]=new Color(1,1,1,ContactOpacity((x+.5f)*2/size-1,(y+.5f)*2/size-1));
        contactTexture.SetPixels32(pixels);contactTexture.Apply(false,true);resources.Add(contactTexture);
        contactMaterial=new Material(shader){name="Soft contact shadow",hideFlags=HideFlags.HideAndDontSave,color=new Color(.015f,.025f,.035f,.8f),mainTexture=contactTexture,renderQueue=2990};
        resources.Add(contactMaterial);
        contactMesh=Own(new Mesh{name="Soft contact plane",vertices=new[]{new Vector3(-1.3f,0,-1.3f),new Vector3(-1.3f,0,1.3f),new Vector3(1.3f,0,1.3f),new Vector3(1.3f,0,-1.3f)},
            uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)},triangles=new[]{0,1,2,0,2,3}});
    }
    MeshRenderer ContactShadow(Transform parent)
    {
        return Shape("Contact shadow",contactMesh!=null?contactMesh:disc,contactMaterial!=null?contactMaterial:shadow,
            new Vector3(0,.012f,0),new Vector3(.43f,.01f,.30f),parent);
    }
    Mesh BeveledHex()
    {
        var vertices=new List<Vector3>();var triangles=new List<int>();
        for(int i=0;i<6;i++)
        {
            float a=(30+i*60)*Mathf.Deg2Rad,b=(30+(i+1)*60)*Mathf.Deg2Rad;
            Vector3 p=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),q=new Vector3(Mathf.Cos(b),0,Mathf.Sin(b));
            Vector3 topP=p*.94f+Vector3.up*.5f,topQ=q*.94f+Vector3.up*.5f;
            Vector3 edgeP=p+Vector3.up*.32f,edgeQ=q+Vector3.up*.32f;
            int n=vertices.Count;vertices.Add(Vector3.up*.5f);vertices.Add(topQ);vertices.Add(topP);
            triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);
            AddBevelFace(vertices,triangles,topP,topQ,edgeQ,edgeP);
            AddBevelFace(vertices,triangles,edgeP,edgeQ,q-Vector3.up*.5f,p-Vector3.up*.5f);
        }
        return Own(new Mesh{name="Chamfered hex stone",vertices=vertices.ToArray(),triangles=triangles.ToArray()});
    }
    static void AddBevelFace(List<Vector3> vertices,List<int> triangles,Vector3 a,Vector3 b,Vector3 c,Vector3 d)
    {
        int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);
        triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);triangles.Add(n);triangles.Add(n+2);triangles.Add(n+3);
    }
#if DITTOCHES_PORTABLE_PREVIEW
    public bool ValidateContactResources()
    {
        if(!presentationQuality)return contactMaterial==null&&contactMesh==null;
        if(contactMaterial==null||contactTexture==null||contactMesh==null||contactTexture.width!=96||contactMesh.vertexCount!=4)return false;
        foreach(var actor in actors.Values)
            if(actor.contactShadow.sharedMaterial!=contactMaterial||actor.contactShadow.GetComponent<MeshFilter>().sharedMesh!=contactMesh)return false;
        return true;
    }
    public int OwnedResourceCount {get{return resources.Count;}}
#endif
}
