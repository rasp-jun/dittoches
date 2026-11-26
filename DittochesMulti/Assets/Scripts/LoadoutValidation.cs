#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Collections;
using System.IO;
using UnityEngine;

public sealed class LoadoutValidation : MonoBehaviour
{
    LoadoutStudio studio=new LoadoutStudio();
    TamerLoadout equipped=new TamerLoadout();
    int checks;
    int clickStep;
    Vector2 clickPoint;
    bool editable=true,applied;
    void Require(bool condition,string message)
    {if(!condition){Debug.LogError("LOADOUT FAILED: "+message);Application.Quit(2);throw new InvalidOperationException(message);}checks++;}
    void OnGUI()
    {
        // This automated review injects its own clicks. Desktop input must not select a different capture model.
        if(Event.current.type!=EventType.Layout&&Event.current.type!=EventType.Repaint)return;
        GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1600f,Screen.height/1000f,1));
        Event saved=null;
        if(clickStep>0&&Event.current.type==EventType.Repaint)
        {saved=new Event(Event.current);Event.current=new Event{type=clickStep==1?EventType.MouseDown:EventType.MouseUp,button=0,mousePosition=clickPoint};clickStep=clickStep==1?2:0;}
        if(studio.Draw(new Rect(55,110,1490,790),ref equipped,editable))applied=true;
        if(saved!=null)Event.current=saved;
    }
    IEnumerator Click(float x,float y)
    {clickPoint=new Vector2(x,y);clickStep=1;applied=false;while(clickStep>0)yield return null;yield return null;}
    IEnumerator Start()
    {
        Application.runInBackground=true;Application.targetFrameRate=60;
        yield return null;
        TamerMotionValidation.Validate(Require);
        LocomotionHitchValidation.Validate(Require);
        if(PortablePreview.HasArgument("--tamer-motion-capture"))yield return TamerMotionValidation.CaptureReview();
        string previous=PortablePreviewPrefs.GetString(TamerLoadout.Key,"");
        try
        {
            PortablePreviewPrefs.SetString(TamerLoadout.Key,"not json");
            Require(TamerLoadout.Load(2).tamer==2,"legacy tamer migration");
            var saved=new TamerLoadout{tamer=3,field=2,finisher=1};saved.Save();
            var restored=TamerLoadout.Load();
            Require(restored.tamer==3&&restored.field==2&&restored.finisher==1,"saved loadout round trip");
            var copy=restored.Copy();copy.tamer=0;Require(restored.tamer==3,"draft never changes equipped loadout");
            PortablePreviewPrefs.SetString(TamerLoadout.Key,"{\"tamer\":99,\"field\":-1,\"finisher\":5}");
            restored=TamerLoadout.Load();Require(restored.tamer==TamerLoadout.Tamers.Length-1&&restored.field==0&&restored.finisher==2,"damaged preference values clamp");
            for(int id=0;id<TamerLoadout.Tamers.Length;id++)
            {
                new TamerLoadout{tamer=id}.Save();Require(TamerLoadout.Load().tamer==id,"every new tamer survives persistence: "+id);
                if(id>0)Require(FaithfulModelData.Available(TamerLoadout.Models[id]),"new tamer has its actual reviewed 3D model: "+id);
            }
        }
        finally{PortablePreviewPrefs.SetString(TamerLoadout.Key,previous);PortablePreviewPrefs.Save();}
        studio.Preview(new TamerLoadout{tamer=2,field=1,finisher=2},0);
        yield return Click(1410,855);
        Require(applied&&equipped.tamer==2&&equipped.field==1&&equipped.finisher==2,"apply button commits draft");
        Require(TamerLoadout.Load().Summary==equipped.Summary,"apply button persists all selections");
        studio.Preview(new TamerLoadout{tamer=1},0);editable=false;
        yield return Click(1410,855);
        Require(!applied&&equipped.tamer==2,"queue lock prevents application");editable=true;
        yield return Click(1200,855);
        Require(studio.Draft.Summary==equipped.Summary,"reset button restores equipped selections");
        PortablePreviewPrefs.SetString(TamerLoadout.Key,previous);PortablePreviewPrefs.Save();
        using(var lobby=new TacticalArena(new Rect(0,0,1160,412),true,false))lobby.ValidateLobbyStage(Require);
        using(var arena=new TacticalArena(new Rect(0,0,800,600),true))
        {
            arena.ValidateFinisherFraming(Require);
            arena.ValidateFieldThemeArchitecture(Require);
            for(int field=0;field<3;field++)
            {
                arena.SetField(field);Require(arena.FieldId==field,"field selection "+field);
                for(int cell=0;cell<56;cell++)Require(arena.HitCell(arena.Project(TacticalArena.CellWorld(cell%7,cell/7)))==cell,"field preserves hit cell "+cell);
            }
            arena.SetField(0);Require(arena.FieldId==0,"return to original field");
            for(int effect=0;effect<3;effect++)
            {
                foreach(float age in new[]{.15f,.55f,.92f,1.15f,1.70f,2.2f,2.7f})
                {
                    arena.BeginFrame(-1,-1,-1,false);Vector2 before=arena.Project(TacticalArena.CellWorld(3,4));
                    arena.SetTamer("shot-source",1,new Vector3(0,.23f,-2),Vector3.forward,0,age);arena.SetTamer("shot-target",2,new Vector3(0,.23f,1.8f),Vector3.back,0,age);
                    arena.DrawFinisher(effect,new Vector3(0,.23f,-2),new Vector3(0,.23f,1.8f),age);arena.Render();
                    Require(arena.EffectCount>0&&arena.EffectCount<128,"finisher has bounded effects at age "+age);
                    Require((arena.Project(TacticalArena.CellWorld(3,4))-before).sqrMagnitude<.0001f,"camera impact never accumulates or changes picking");
                    if(age==.55f||age==1.15f||age==1.70f||age==2.2f)CaptureArena(arena,"finisher-"+effect+"-"+age.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture));
                }
                arena.BeginFrame(-1,-1,-1,false);arena.DrawFinisher(effect,Vector3.zero,Vector3.forward*2,TacticalArena.FinisherDuration+.1f);arena.Render();
                Require(arena.EffectCount==0,"finisher expires cleanly");
            }
            arena.BeginFrame(-1,-1,-1,false);arena.DrawFinisher(2,new Vector3(0,.23f,-3.8f),new Vector3(0,.23f,3.8f),.7f);arena.Render();
            CaptureArena(arena,"holy-gate-far-target");
            Require(arena.EffectCount>0&&arena.EffectCount<128,"far target gate uses compact formation framing");
            foreach(int effect in new[]{1,2})foreach(float z in new[]{-3.8f,3.8f})foreach(float age in new[]{.55f,1.70f})
            {
                arena.BeginFrame(-1,-1,-1,false);arena.DrawFinisher(effect,new Vector3(0,.23f,-z),new Vector3(0,.23f,z),age);arena.Render();
                CaptureArena(arena,"framed-"+effect+"-"+(z<0?"near":"far")+"-"+age.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture));
            }
            int resources=arena.OwnedResourceCount;
            for(int pass=0;pass<6;pass++)
            {
                arena.SetField(pass%3);arena.BeginFrame(-1,-1,-1,false);arena.Render();
                Require(arena.OwnedResourceCount==resources,"theme switching reuses textures materials and geometry");
                for(int seat=0;seat<9;seat++)Require(arena.HitBench(arena.Project(TacticalArena.BenchWorld(seat)))==seat,"detailed field preserves bench picking");
                if(pass<3)CaptureArena(arena,"field-"+pass);
            }
        }
        using(var wide=new TacticalArena(TacticalArena.WideViewport,true))
        {wide.ValidateFinisherFraming(Require);CaptureThemeFormation(wide,"solo");}
        using(var online=new TacticalArena(TacticalArena.MultiViewport,true))
        {online.ValidateFinisherFraming(Require);CaptureThemeFormation(online,"online");}
        for(int tab=0;tab<3;tab++)for(int choice=0;choice<(tab==0?TamerLoadout.Tamers.Length:3);choice++)
        {
            var value=new TamerLoadout{tamer=tab==0?choice:1,field=tab==1?choice:0,finisher=tab==2?choice:0};
            if(tab==0)Require(TamerLoadout.Portrait(choice)!=null,"tamer portrait "+choice);
            studio.Preview(value,tab);
            yield return new WaitForSecondsRealtime(tab==2?1.30f:1.2f);
            yield return new WaitForEndOfFrame();
            string folder=Path.Combine(Application.dataPath,"../LoadoutCaptures");Directory.CreateDirectory(folder);
            var texture=ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(folder,"tab-"+tab+"-choice-"+choice+".png"),texture.EncodeToPNG());Destroy(texture);
            Require(studio.Draft.tamer==value.tamer&&studio.Draft.field==value.field&&studio.Draft.finisher==value.finisher,"preview selection preserved: expected "+value.Summary+"; actual "+studio.Draft.Summary);
        }
        Debug.Log("LOADOUT SMOKE COMPLETE: "+checks+" checks, "+(TamerLoadout.Tamers.Length+6)+" captures.");Application.Quit();
    }
    void CaptureThemeFormation(TacticalArena arena,string view)
    {
        string[] ids={"agumon","wargreymon","garurumon","holyangemon","kuwagamon","lilimon"};
        int[] cols={0,3,6,0,3,6},rows={1,1,1,6,6,6};
        foreach(string id in ids)Require(FaithfulModelData.Available(id),"theme review uses an available faithful model: "+id);
        for(int field=0;field<3;field++)
        {
            arena.SetField(field);arena.BeginFrame(45,-1,-1,false);
            for(int i=0;i<ids.Length;i++)
            {
                arena.SetActor(i,TacticalArena.CellWorld(cols[i],rows[i]),null,i<3?Color.red:Color.cyan);
                arena.FaceActor(i,i<3?Vector3.back:Vector3.forward);
                arena.PoseDigimon(i,ids[i],0,0,field+1,star:i==1?3:1);
            }
            arena.HighlightHexAttackRange(3,6,2);arena.Render();
            Require(arena.ActorCount==ids.Length,"theme review retains every deployed model: "+view+" / "+field);
            int models=0;foreach(string motion in arena.FaithfulMotions())models++;
            Require(models==ids.Length,"theme review resolves every faithful model: "+view+" / "+field);
            CaptureArena(arena,"theme-units-"+view+"-"+field);
        }
    }
    static void CaptureArena(TacticalArena arena,string name)
    {
        string folder=Path.Combine(Application.dataPath,"../LoadoutCaptures");Directory.CreateDirectory(folder);
        var previous=RenderTexture.active;Texture2D image=null;
        try
        {
            RenderTexture.active=arena.Texture;image=new Texture2D(arena.Texture.width,arena.Texture.height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,image.width,image.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());
        }
        finally{RenderTexture.active=previous;if(image!=null)Destroy(image);}
    }
    void OnDestroy(){studio.Dispose();}
}
#endif
