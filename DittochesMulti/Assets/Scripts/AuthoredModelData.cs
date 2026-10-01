using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Blender-authored mesh and baked skeletal clips. See Tools/author_agumon.py.</summary>
public sealed class AuthoredModelData
{
    public sealed class Clip
    {
        public string name;
        public float duration;
        public bool loop;
        public int frames,boneCount;
        public Vector3[] positions;
        public Quaternion[] rotations;
        public void Sample(float time,Vector3[] p,Quaternion[] q)
        {
            float t=loop?Mathf.Repeat(time,duration):Mathf.Clamp(time,0,duration);
            float frame=t/duration*(frames-1);
            int a=Mathf.Min((int)frame,frames-1),b=Mathf.Min(a+1,frames-1);
            float u=frame-a;
            for(int i=0;i<boneCount;i++)
            {
                p[i]=Vector3.LerpUnclamped(positions[a*boneCount+i],positions[b*boneCount+i],u);
                q[i]=Quaternion.SlerpUnclamped(rotations[a*boneCount+i],rotations[b*boneCount+i],u);
            }
        }
    }
    public readonly Dictionary<string,Clip> clips=new Dictionary<string,Clip>();
    static Vector3 Vector(BinaryReader reader){return new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());}
    static string Name(BinaryReader reader)
    {
        int length=reader.ReadInt32();if(length<1||length>128)throw new InvalidDataException("Invalid model name length.");
        return System.Text.Encoding.UTF8.GetString(reader.ReadBytes(length));
    }
    public static DigimonModelLibrary.Model LoadAgumon(){return Load("agumon");}
    public static DigimonModelLibrary.Model Load(string id)
    {
        if(string.IsNullOrEmpty(id)||!System.Text.RegularExpressions.Regex.IsMatch(id,"^[a-z]+$"))
            throw new ArgumentException("Invalid authored model identifier.");
        byte[] bytes;
#if DITTOCHES_PORTABLE_PREVIEW
        string file=Path.Combine(Application.streamingAssetsPath,"Models",id=="agumon"?"Agumon.bytes":id+".bytes");
        if(!File.Exists(file))return null;
        bytes=File.ReadAllBytes(file);
#else
        var asset=Resources.Load<TextAsset>(id=="agumon"?"Models/Agumon/Agumon":"Models/Roster/"+id);
        if(asset==null)return null;bytes=asset.bytes;
#endif
        using(var stream=new MemoryStream(bytes,false))using(var reader=new BinaryReader(stream))
        {
            if(new string(reader.ReadChars(4))!="DTM3"||reader.ReadInt32()!=1)throw new InvalidDataException("Unsupported authored model.");
            int count=reader.ReadInt32(),vertices=reader.ReadInt32(),faces=reader.ReadInt32(),clipCount=reader.ReadInt32();
            if(count<1||count>256||vertices<3||vertices>500000||faces<1||faces>1000000||clipCount<1||clipCount>128)
                throw new InvalidDataException("Invalid authored model dimensions.");
            var skeleton=new DigimonMeshBuilder.Skeleton();
            for(int i=0;i<count;i++)
            {
                string name=Name(reader);int parent=reader.ReadInt32();
                if(parent>=i||parent< -1)throw new InvalidDataException("Invalid bone hierarchy.");
                skeleton.Add(name,parent,Vector(reader));
            }
            var p=new Vector3[vertices];var n=new Vector3[vertices];var c=new Color[vertices];var w=new BoneWeight[vertices];
            for(int i=0;i<vertices;i++)
            {
                p[i]=Vector(reader);n[i]=Vector(reader);
                c[i]=new Color(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
#if DITTOCHES_PORTABLE_PREVIEW
                // The preserved player cannot import a shader. Match the Blender palette
                // with rest-pose lighting; the regular Unity build uses CharacterToon.
                float dot=Mathf.Max(0,Vector3.Dot(n[i],new Vector3(-.38f,.81f,.45f).normalized));
                float shade=dot<.42f?Mathf.Lerp(.40f,.80f,dot/.42f):Mathf.Lerp(.80f,1,(dot-.42f)/.48f);
                c[i]=new Color(c[i].r*shade,c[i].g*shade,c[i].b*shade,1).gamma;
#endif
                w[i]=new BoneWeight {boneIndex0=reader.ReadInt32(),boneIndex1=reader.ReadInt32(),boneIndex2=reader.ReadInt32(),boneIndex3=reader.ReadInt32(),
                    weight0=reader.ReadSingle(),weight1=reader.ReadSingle(),weight2=reader.ReadSingle(),weight3=reader.ReadSingle()};
                if(w[i].boneIndex0>=count||w[i].boneIndex1>=count||w[i].boneIndex2>=count||w[i].boneIndex3>=count)
                    throw new InvalidDataException("Invalid bone index.");
            }
            var triangles=new int[faces*3];
            for(int i=0;i<triangles.Length;i++)
            {triangles[i]=reader.ReadInt32();if(triangles[i]<0||triangles[i]>=vertices)throw new InvalidDataException("Invalid vertex index.");}
            var data=new AuthoredModelData();
            for(int i=0;i<clipCount;i++)
            {
                var clip=new Clip{name=Name(reader),duration=reader.ReadSingle(),loop=reader.ReadInt32()!=0,frames=reader.ReadInt32(),boneCount=count};
                if(clip.duration<=0||clip.frames<2||clip.frames>10000)throw new InvalidDataException("Invalid motion clip.");
                clip.positions=new Vector3[clip.frames*count];clip.rotations=new Quaternion[clip.positions.Length];
                for(int frame=0;frame<clip.frames;frame++)for(int bone=0;bone<count;bone++)
                {
                    int index=frame*count+bone;clip.positions[index]=Vector(reader);
                    clip.rotations[index]=new Quaternion(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
                }
                data.clips.Add(clip.name,clip);
            }
            if(stream.Position!=stream.Length)throw new InvalidDataException("Unexpected model trailing bytes.");
            var mesh=new Mesh{name=id+" / Blender authored",hideFlags=HideFlags.HideAndDontSave};
            if(vertices>65535)mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices=p;mesh.normals=n;mesh.colors=c;mesh.triangles=triangles;mesh.boneWeights=w;
            var binds=new Matrix4x4[count];for(int i=0;i<count;i++)binds[i]=Matrix4x4.Translate(-skeleton.positions[i]);
            mesh.bindposes=binds;mesh.RecalculateBounds();
            Debug.Log("AUTHORED MODEL LOADED: "+id+"; "+vertices+" vertices; "+faces+" triangles; "+count+" bones; "+clipCount+" clips.");
            return new DigimonModelLibrary.Model{id=id,mesh=mesh,skeleton=skeleton,height=mesh.bounds.size.y,hipHeight=.73f,
                legLength=.29f,footSpread=.31f,authored=data,
                walkCycleDistance=(id=="agumon"||id=="garurumon"||id=="metalgarurumon"?.30f:.24f)/.62f};
        }
    }
}
