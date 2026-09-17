using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ArenaValidation
{
    [MenuItem("Dittoches Multi/Validate Perspective Arena")]
    public static void Validate()
    {
        CombatMotionValidation.Validate();
        SkillTimingValidation.Validate();
        ValidatePointer();
        ValidateView(TacticalArena.SoloViewport,"solo");
        ValidateView(TacticalArena.MultiViewport,"multi");
        Debug.Log("ARENA VALIDATION PASSED: 112 cell projections, bench bounds, outside rejection, actor lifecycle and rendering.");
    }
    static void Check(bool value,string message){if(!value)throw new Exception("Arena validation: "+message);}
    public static void ValidatePointer()
    {
        var pointer=new ArenaPointer();
        pointer.Update(28,false,true,false,false);Check(pointer.Released==-1,"release without press");
        pointer.Update(28,true,false,false,false);pointer.Update(28,false,true,false,false);Check(pointer.Released==28,"board click");
        pointer.Update(28,true,false,false,false);pointer.Update(29,false,true,false,false);Check(pointer.Released==-1,"release on another cell");
        pointer.Update(-1,true,false,false,false);pointer.Update(28,false,true,false,false);Check(pointer.Released==-1,"UI press cannot click board");
        pointer.Update(56,true,false,false,false);pointer.Update(56,false,true,false,false);Check(pointer.Released==56,"bench click");
        pointer.Update(100,true,false,false,false);pointer.Update(100,false,true,false,false);Check(pointer.Released==100,"loot owns click");
        pointer.Update(100,true,false,false,false);pointer.Update(28,false,true,false,false);Check(pointer.Released==-1,"loot cannot click through");
        pointer.Update(28,true,false,false,false);pointer.Update(28,false,false,true,false);pointer.Update(28,false,true,false,false);Check(pointer.Released==-1,"drag is not click");
        pointer.Update(28,true,false,false,false);pointer.Update(28,false,false,false,true);pointer.Update(28,false,true,false,false);Check(pointer.Released==-1,"overlay cancels press");
        pointer.Update(56,true,false,false,false);pointer.Reset();pointer.Update(56,false,true,false,false);Check(pointer.Released==-1,"screen change cancels press");
        pointer.Update(100,true,false,false,false);Check(pointer.HasPress,"automatic loot pickup waits for pointer release");
        pointer.Update(100,false,true,false,false);Check(!pointer.HasPress&&pointer.Released==100,"release unlocks automatic loot pickup");
        Debug.Log("ARENA POINTER: 12 regression checks passed.");
    }
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
            arena.SetTactician("legend",TacticalArena.CellWorld(0,7),TacticalArena.CellWorld(2,7),Resources.Load<Texture2D>("ArtVariants/LicensedFanArt/Koromon-v1"),Color.cyan,.6f,100,1,.8f);
            arena.Render();Check(arena.ActorCount==22,"tactician creation");
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
