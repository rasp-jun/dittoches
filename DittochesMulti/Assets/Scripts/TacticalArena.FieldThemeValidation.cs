#if DITTOCHES_PORTABLE_PREVIEW
using System;
using UnityEngine;

public sealed partial class TacticalArena
{
    public void ValidateFieldThemeArchitecture(Action<bool,string> require)
    {
        if(!presentationQuality)return;int savedField=FieldId;var vertexCounts=new int[3];
        try
        {
            for(int id=0;id<3;id++)
            {
                SetField(id);BeginFrame(-1,-1,-1,false);int batches=0;
                foreach(var renderer in fieldDecor[id].GetComponentsInChildren<MeshRenderer>())
                {
                    if(renderer.name!="Field architecture batch")continue;batches++;
                    var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;vertexCounts[id]+=mesh.vertexCount;
                    require(mesh.uv.Length==mesh.vertexCount&&mesh.colors.Length==mesh.vertexCount,"crafted field materials have continuous surface coordinates and baked lighting");
                    require(Array.TrueForAll(mesh.colors,c=>c.a==1),"opaque architecture has no accidental translucent vertices");
                    if(renderer.sharedMaterial.name!="Field luminous")require(renderer.sharedMaterial.mainTexture!=Texture2D.whiteTexture,"stone, metal and foliage carry their crafted surface grain");
                    foreach(var point in mesh.vertices)
                    {
                        require(!float.IsNaN(point.x)&&!float.IsNaN(point.y)&&!float.IsNaN(point.z)&&!float.IsInfinity(point.x)&&!float.IsInfinity(point.y)&&!float.IsInfinity(point.z),"field architecture vertices remain finite: "+id);
                        require(point.y<=2.65f,"field landmark remains within the rebuilt scenery height limit: "+id);
                        require(Mathf.Abs(point.x)>4.9f||point.z>=5.30f,"detailed static architecture stays outside playable and reserve footprints: "+id);
                    }
                }
                require(batches>0&&batches<=4,"theme static geometry uses at most four shared-material batches: "+id);
                require(fieldMotes[id].Count==12&&fieldSeals[id].Count==(id==2?2:0),"theme distinction adds no animated objects: "+id);
                Debug.Log("FIELD THEME "+id+": static batches="+batches+" vertices="+vertexCounts[id]+" animated="+(fieldMotes[id].Count+fieldSeals[id].Count));
            }
            require(vertexCounts[0]!=vertexCounts[1]&&vertexCounts[1]!=vertexCounts[2]&&vertexCounts[0]!=vertexCounts[2],"the three field themes use different static geometry");
            int owned=OwnedResourceCount;
            require(islandVegetation.Count>0&&islandAltar.Count>0&&islandLanterns.Count>0,"theme switching tracks original island decoration without destroying its renderers");
            foreach(int id in new[]{0,1,2,0,2,1,0})
            {
                SetField(id);BeginFrame(-1,-1,-1,false);
                require(OwnedResourceCount==owned,"warmed field changes reuse all owned meshes, textures and materials");
                foreach(var renderer in islandVegetation)require(renderer!=null&&!renderer.enabled,"island vegetation stays hidden behind the rebuilt scenery after theme changes");
                foreach(var renderer in islandAltar)require(renderer!=null&&!renderer.enabled,"island altar stays hidden behind the rebuilt scenery after theme changes");
                foreach(var renderer in islandLanterns)require(renderer!=null&&!renderer.enabled,"island lantern stays hidden behind the rebuilt scenery after theme changes");
                for(int other=0;other<3;other++)require(fieldDecor[other].activeSelf==(other==id),"only the chosen detailed field root stays active");
                for(int row=0;row<8;row++)for(int col=0;col<7;col++)require(HitCell(Project(CellWorld(col,row)))==row*7+col,"theme architecture preserves all fifty-six cell picks");
                for(int seat=0;seat<9;seat++)require(HitBench(Project(BenchWorld(seat)))==seat,"theme architecture preserves reserve picking");
            }
        }
        finally{SetField(savedField);BeginFrame(-1,-1,-1,false);}
    }
}
#endif
