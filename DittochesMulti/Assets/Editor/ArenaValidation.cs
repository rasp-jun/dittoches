using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ArenaValidation
{
    [MenuItem("Dittoches Multi/Validate Perspective Arena")]
    public static void Validate()
    {
        ValidateView(TacticalArena.SoloViewport,"solo");
        ValidateView(TacticalArena.MultiViewport,"multi");
        Debug.Log("ARENA VALIDATION PASSED: 112 cell projections, bench bounds, outside rejection, actor lifecycle and rendering.");
    }
    static void Check(bool value,string message){if(!value)throw new Exception("Arena validation: "+message);}
    static void ValidateView(Rect view,string name)
    {
        using(var arena=new TacticalArena(view))
        {
            for(int row=0;row<8;row++)for(int col=0;col<7;col++)
            {
                Vector3 world=TacticalArena.CellWorld(col,row);Vector2 screen=arena.Project(world);
                Check(view.Contains(screen),"cell outside viewport "+row+":"+col);
                Check(arena.HitCell(screen)==row*7+col,"cell ray mismatch "+row+":"+col);
                Vector3 restored;Check(arena.GroundPoint(screen,out restored)&&Vector3.Distance(world,restored)<.005f,"ground inverse");
            }
            Check(arena.HitCell(new Vector2(-10,-10))==-1,"outside input");
            for(int i=0;i<9;i++)
            {
                Rect rect=arena.BenchRect(i);Check(view.Contains(rect.center),"bench outside viewport");
                Check(arena.HitCell(rect.center)==-1,"bench overlaps playable field");
                Check(arena.HitBench(arena.Project(TacticalArena.BenchWorld(i)))==i,"bench ray mismatch");
            }
            string[] sprites={"Agumon","Gabumon","Patamon","Greymon","Angemon","Garurumon","Koromon"};
            arena.BeginFrame(38,39,2,true);
            for(int i=0;i<7;i++)
            {
                Texture2D texture=Resources.Load<Texture2D>("Sprites/"+sprites[i]);
                arena.SetActor(i,TacticalArena.CellWorld(i,4+i%3),texture,Color.green);
                arena.SetActor(i+10,TacticalArena.CellWorld(i,1+i%2),texture,Color.red);
                arena.SetActor(i+20,TacticalArena.BenchWorld(i),texture,Color.yellow,.85f);
            }
            arena.Render();Check(arena.ActorCount==21,"actor creation");
            RenderTexture previous=RenderTexture.active;
            var capture=new Texture2D(arena.Texture.width,arena.Texture.height,TextureFormat.RGB24,false);
            try
            {
                RenderTexture.active=arena.Texture;capture.ReadPixels(new Rect(0,0,capture.width,capture.height),0,0);capture.Apply();
                Directory.CreateDirectory("Builds/Validation");
                File.WriteAllBytes("Builds/Validation/arena-"+name+".png",capture.EncodeToPNG());
            }
            finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(capture);}
            arena.BeginFrame(-1,-1,-1,false);arena.Render();Check(arena.ActorCount==0,"stale actor cleanup");
        }
    }
}
