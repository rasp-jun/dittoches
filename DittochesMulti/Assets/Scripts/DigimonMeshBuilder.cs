using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Code-authored geometry, skin weights and rest skeleton. No generated/paid assets.</summary>
public sealed class DigimonMeshBuilder
{
    public sealed class Skeleton
    {
        public readonly List<string> names=new List<string>();
        public readonly List<int> parents=new List<int>();
        public readonly List<Vector3> positions=new List<Vector3>();
        public int Add(string name,int parent,Vector3 position)
        {names.Add(name);parents.Add(parent);positions.Add(position);return names.Count-1;}
    }
    public readonly Skeleton skeleton=new Skeleton();
    readonly List<Vector3> vertices=new List<Vector3>(),normals=new List<Vector3>();
    readonly List<Color> colors=new List<Color>();
    readonly List<BoneWeight> weights=new List<BoneWeight>();
    readonly List<int> triangles=new List<int>();
    static readonly Vector3 Light=new Vector3(-.45f,.85f,.65f).normalized;
    public int VertexCount { get { return vertices.Count; } }
    int Vertex(Vector3 p,Vector3 normal,Color color,int bone,int other=-1,float blend=0)
    {
        vertices.Add(p);normals.Add(normal);
#if DITTOCHES_PORTABLE_PREVIEW
        // The old player cannot import a new shader. Preserve volume using authored vertex lighting.
        float light=.54f+.46f*Mathf.Max(0,Vector3.Dot(normal,Light));
        color=new Color(color.r*light,color.g*light,color.b*light,color.a);
#endif
        colors.Add(color);
        weights.Add(new BoneWeight{boneIndex0=bone,weight0=1-blend,boneIndex1=other<0?bone:other,weight1=blend});
        return vertices.Count-1;
    }
    public void Ellipsoid(Vector3 center,Vector3 radius,Color color,int bone,Quaternion rotation,int other=-1,float blend=0,int rings=10,int sides=16)
    {
        int start=vertices.Count;
        for(int y=0;y<=rings;y++)for(int x=0;x<=sides;x++)
        {
            float a=y*Mathf.PI/rings,b=x*Mathf.PI*2/sides;
            Vector3 n=new Vector3(Mathf.Sin(a)*Mathf.Cos(b),Mathf.Cos(a),Mathf.Sin(a)*Mathf.Sin(b));
            Vector3 normal=rotation*new Vector3(n.x/radius.x,n.y/radius.y,n.z/radius.z).normalized;
            Vertex(center+rotation*Vector3.Scale(n,radius),normal,color,bone,other,blend);
            if(y==rings||x==sides)continue;
            int i=start+y*(sides+1)+x;
            triangles.Add(i);triangles.Add(i+1);triangles.Add(i+sides+1);
            triangles.Add(i+1);triangles.Add(i+sides+2);triangles.Add(i+sides+1);
        }
    }
    public void Ball(Vector3 center,Vector3 radius,Color color,int bone,int other=-1,float blend=0)
    {Ellipsoid(center,radius,color,bone,Quaternion.identity,other,blend);}
    public void Capsule(Vector3 a,Vector3 b,float width,float depth,Color color,int bone,int other=-1,float blend=0)
    {Ellipsoid((a+b)*.5f,new Vector3(width,(b-a).magnitude*.5f+width*.25f,depth),color,bone,Quaternion.FromToRotation(Vector3.up,b-a),other,blend);}
    /// <summary>Continuous tapered surface along a path, with smoothly interpolated two-bone weights.</summary>
    public void Tube(Vector3[] points,float[] radii,Color color,int[] bones,int sides=12)
    {
        int start=vertices.Count;
        for(int j=0;j<points.Length;j++)
        {
            Vector3 tangent=(points[Mathf.Min(j+1,points.Length-1)]-points[Mathf.Max(0,j-1)]).normalized;
            Vector3 u=Vector3.Cross(tangent,Mathf.Abs(tangent.y)>.9f?Vector3.forward:Vector3.up).normalized;
            Vector3 v=Vector3.Cross(tangent,u);
            for(int k=0;k<=sides;k++)
            {
                float angle=k*Mathf.PI*2/sides;Vector3 n=u*Mathf.Cos(angle)+v*Mathf.Sin(angle);
                int bone=bones[Mathf.Min(j,bones.Length-1)],next=bones[Mathf.Min(j+1,bones.Length-1)];
                Vertex(points[j]+n*radii[j],n,color,bone,next,j==0?0:.35f);
                if(j==points.Length-1||k==sides)continue;
                int i=start+j*(sides+1)+k;
                triangles.Add(i);triangles.Add(i+sides+1);triangles.Add(i+1);
                triangles.Add(i+1);triangles.Add(i+sides+1);triangles.Add(i+sides+2);
            }
        }
    }
    public void Horn(Vector3 a,Vector3 b,Vector3 tip,float radius,Color color,int bone)
    {Tube(new[]{a,Vector3.Lerp(a,b,.5f),b,tip},new[]{radius,radius*.85f,radius*.45f,.002f},color,new[]{bone},10);}
    public Mesh Build(string name)
    {
        var mesh=new Mesh{name=name,hideFlags=HideFlags.HideAndDontSave};
        if(vertices.Count>65535)mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);
        mesh.boneWeights=weights.ToArray();
        var binds=new Matrix4x4[skeleton.names.Count];
        for(int i=0;i<binds.Length;i++)binds[i]=Matrix4x4.Translate(-skeleton.positions[i]);
        mesh.bindposes=binds;mesh.RecalculateBounds();return mesh;
    }
}
