using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Reviewed GLB data packed by export_faithful_runtime.py. No procedural fallback.</summary>
public sealed class FaithfulModelData
{
    [Serializable] public sealed class Technique {public string id,emitter;public float attackRelease,skillRelease;}
    [Serializable] sealed class Techniques {public Technique[] models;}
    static Dictionary<string,Technique> techniques;
    public Technique technique;
    public struct Node { public string name; public int parent; public Vector3 p,s; public Quaternion q; }
    public sealed class Part { public Mesh mesh; public int node,material; public int[] joints; }
    public sealed class Track
    {
        public int node,kind; public bool step; public float[] times; public Vector4[] values;
        public Vector4 Sample(float t)
        {
            int a=0,b=times.Length-1;
            while(a+1<b){int m=(a+b)/2;if(times[m]<=t)a=m;else b=m;}
            if(t>=times[b])a=b;
            float u=step||a==b?0:Mathf.Clamp01((t-times[a])/(times[b]-times[a]));
            if(kind!=1)return Vector4.LerpUnclamped(values[a],values[b],u);
            Vector4 x=values[a],y=values[b];Quaternion q=Quaternion.SlerpUnclamped(new Quaternion(x.x,x.y,x.z,x.w),new Quaternion(y.x,y.y,y.z,y.w),u);
            return new Vector4(q.x,q.y,q.z,q.w);
        }
    }
    public sealed class Clip { public string name; public float duration; public Track[] tracks; public bool Loop {get{return name=="Idle"||name=="Walk"||name=="Run";}} }
    public string id,sourceHash; public Node[] nodes; public Part[] parts; public Material[] materials;
    public bool hasBounds;public Bounds restBounds;
    public readonly Dictionary<string,Clip> clips=new Dictionary<string,Clip>();
    readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
    static readonly Dictionary<string,FaithfulModelData> cache=new Dictionary<string,FaithfulModelData>();
    static readonly Dictionary<string,bool> available=new Dictionary<string,bool>();
    int references;
    public static bool Enabled=true;
    public static bool Available(string id) {bool found;if(!Enabled||string.IsNullOrEmpty(id))return false;if(!available.TryGetValue(id,out found)){found=File.Exists(Path.Combine(Application.streamingAssetsPath,"FaithfulModels",id+".bytes"));available[id]=found;}return found;}
    static int Count(BinaryReader r,int max){int n=r.ReadInt32();if(n<0||n>max)throw new InvalidDataException("FDM1 count: "+n);return n;}
    static string Text(BinaryReader r){return System.Text.Encoding.UTF8.GetString(r.ReadBytes(Count(r,4096)));}
    static Vector3 V3(BinaryReader r){return new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());}
    static Vector4 V4(BinaryReader r){return new Vector4(r.ReadSingle(),r.ReadSingle(),r.ReadSingle(),r.ReadSingle());}
    static Quaternion Q(BinaryReader r){Vector4 v=V4(r);return new Quaternion(v.x,v.y,v.z,v.w);}
    public static FaithfulModelData Acquire(string id)
    {
        FaithfulModelData data;
        if(!cache.TryGetValue(id,out data))
        {
            data=new FaithfulModelData();
            try{data.Load(id);cache.Add(id,data);}catch{data.Dispose();throw;}
        }
        data.references++;return data;
    }
    public void Release(){if(--references<=0){cache.Remove(id);Dispose();}}
    void Dispose(){foreach(var value in owned)if(value!=null)UnityEngine.Object.Destroy(value);owned.Clear();}
    void Load(string modelId)
    {
        using(var stream=File.OpenRead(Path.Combine(Application.streamingAssetsPath,"FaithfulModels",modelId+".bytes")))
        using(var r=new BinaryReader(stream))
        {
            if(new string(r.ReadChars(4))!="FDM1")throw new InvalidDataException("Unsupported faithful model");
            id=Text(r);sourceHash=Text(r);if(id!=modelId)throw new InvalidDataException("Wrong model identity");
            if(techniques==null)
            {
                techniques=new Dictionary<string,Technique>();
                var catalog=JsonUtility.FromJson<Techniques>(File.ReadAllText(Path.Combine(Application.streamingAssetsPath,"FaithfulModels","techniques.json")));
                foreach(var entry in catalog.models)techniques.Add(entry.id,entry);
            }
            technique=techniques[id];
            nodes=new Node[Count(r,2048)];
            for(int i=0;i<nodes.Length;i++)
            {
                var node=new Node{name=Text(r),parent=r.ReadInt32()};
                if(r.ReadInt32()!=0){var m=new Matrix4x4();for(int k=0;k<16;k++)m[k]=r.ReadSingle();node.p=m.GetColumn(3);node.q=m.rotation;node.s=m.lossyScale;}
                else{node.p=V3(r);node.q=Q(r);node.s=V3(r);}
                nodes[i]=node;
            }
            var images=new Texture2D[Count(r,512)];
            for(int i=0;i<images.Length;i++)
            {
                byte[] image=r.ReadBytes(Count(r,64000000));var texture=new Texture2D(2,2,TextureFormat.RGBA32,true);
                owned.Add(texture);if(!texture.LoadImage(image,true))throw new InvalidDataException("Texture decode failed");
                texture.name=id+" texture "+i;texture.anisoLevel=4;texture.filterMode=FilterMode.Trilinear;images[i]=texture;
            }
            materials=new Material[Count(r,512)];
            for(int i=0;i<materials.Length;i++)
            {
                string name=Text(r);Vector4 color=V4(r);float metal=r.ReadSingle(),rough=r.ReadSingle(),cutoff=r.ReadSingle();
                int image=r.ReadInt32(),alpha=r.ReadInt32(),twoSided=r.ReadInt32();
                var shader=Resources.Load<Shader>("FaithfulCharacter");
                if(shader==null)shader=Resources.Load<Shader>("ArenaSurface");
#if DITTOCHES_PORTABLE_PREVIEW
                if(shader==null)shader=PortablePreview.ArenaShader(image>=0);
#endif
                if(shader==null)shader=Shader.Find("Standard");
                if(shader==null)throw new InvalidOperationException("Faithful character shader unavailable");
                var material=new Material(shader){name=id+" / "+name};owned.Add(material);
#if DITTOCHES_PORTABLE_PREVIEW
                PortablePreview.ConfigureMaterial(material,image>=0);
#endif
                material.color=new Color(color.x,color.y,color.z,color.w);material.mainTexture=image>=0?images[image]:Texture2D.whiteTexture;
                if(material.HasProperty("_VertexColor"))material.SetFloat("_VertexColor",1);
                if(material.HasProperty("_Metallic"))material.SetFloat("_Metallic",metal*.65f);
                if(material.HasProperty("_Glossiness"))material.SetFloat("_Glossiness",1-Mathf.Max(.25f,rough));
                if(material.HasProperty("_Cull"))material.SetFloat("_Cull",twoSided!=0?0:2);
                if(material.HasProperty("_Cutoff"))material.SetFloat("_Cutoff",alpha==0?0:cutoff);
                materials[i]=material;
            }
            parts=new Part[Count(r,1024)];
            for(int k=0;k<parts.Length;k++)
            {
                var part=new Part{node=r.ReadInt32(),material=r.ReadInt32()};
                int count=Count(r,1000000),indexCount=Count(r,3000000),jointCount=Count(r,2048);
                part.joints=new int[jointCount];for(int j=0;j<jointCount;j++)part.joints[j]=r.ReadInt32();
                var binds=new Matrix4x4[jointCount];
                for(int j=0;j<jointCount;j++)for(int c=0;c<16;c++)binds[j][c]=r.ReadSingle();
                var p=new Vector3[count];var n=new Vector3[count];var uv=new Vector2[count];var colors=new Color[count];var weights=new BoneWeight[count];
                for(int j=0;j<count;j++)
                {
                    p[j]=V3(r);n[j]=V3(r);uv[j]=new Vector2(r.ReadSingle(),r.ReadSingle());Vector4 c=V4(r);colors[j]=new Color(c.x,c.y,c.z,c.w);
                    weights[j]=new BoneWeight{boneIndex0=r.ReadInt32(),boneIndex1=r.ReadInt32(),boneIndex2=r.ReadInt32(),boneIndex3=r.ReadInt32(),weight0=r.ReadSingle(),weight1=r.ReadSingle(),weight2=r.ReadSingle(),weight3=r.ReadSingle()};
                }
                var indices=new int[indexCount];for(int j=0;j<indexCount;j++)indices[j]=r.ReadInt32();
                var mesh=new Mesh{name=id+" part "+k};owned.Add(mesh);
                if(count>65535)mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.vertices=p;mesh.normals=n;mesh.uv=uv;mesh.colors=colors;mesh.triangles=indices;
                if(jointCount>0){mesh.boneWeights=weights;mesh.bindposes=binds;}
                mesh.RecalculateBounds();part.mesh=mesh;parts[k]=part;
            }
            int clipCount=Count(r,128);
            for(int i=0;i<clipCount;i++)
            {
                var clip=new Clip{name=Text(r),duration=r.ReadSingle(),tracks=new Track[Count(r,10000)]};
                foreach(int j in Indices(clip.tracks.Length))
                {
                    var t=new Track{node=r.ReadInt32(),kind=r.ReadInt32(),step=r.ReadInt32()!=0};int count=Count(r,100000);
                    t.times=new float[count];t.values=new Vector4[count];
                    for(int key=0;key<count;key++){t.times[key]=r.ReadSingle();t.values[key]=t.kind==1?V4(r):(Vector4)V3(r);}
                    clip.tracks[j]=t;
                }
                clips.Add(clip.name,clip);
            }
            if(stream.Position!=stream.Length)throw new InvalidDataException("Unexpected FDM1 trailing bytes");
        }
    }
    static IEnumerable<int> Indices(int count){for(int i=0;i<count;i++)yield return i;}
}
