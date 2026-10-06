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
            restored=TamerLoadout.Load();Require(restored.tamer==3&&restored.field==0&&restored.finisher==2,"damaged preference values clamp");
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
        using(var arena=new TacticalArena(new Rect(0,0,800,600),true))
        {
            for(int field=0;field<3;field++)
            {
                arena.SetField(field);Require(arena.FieldId==field,"field selection "+field);
                for(int cell=0;cell<56;cell++)Require(arena.HitCell(arena.Project(TacticalArena.CellWorld(cell%7,cell/7)))==cell,"field preserves hit cell "+cell);
            }
            arena.SetField(0);Require(arena.FieldId==0,"return to original field");
            for(int effect=0;effect<3;effect++)
            {
                arena.BeginFrame(-1,-1,-1,false);arena.DrawFinisher(effect,Vector3.zero,Vector3.forward*2,1);arena.Render();
                Require(arena.EffectCount>0&&arena.EffectCount<40,"finisher uses bounded effect pool");
                arena.BeginFrame(-1,-1,-1,false);arena.DrawFinisher(effect,Vector3.zero,Vector3.forward*2,3);arena.Render();
                Require(arena.EffectCount==0,"finisher expires cleanly");
            }
        }
        for(int tab=0;tab<3;tab++)for(int choice=0;choice<(tab==0?4:3);choice++)
        {
            var value=new TamerLoadout{tamer=tab==0?choice:1,field=tab==1?choice:0,finisher=tab==2?choice:0};
            if(tab==0)Require(TamerLoadout.Portrait(choice)!=null,"tamer portrait "+choice);
            studio.Preview(value,tab);
            yield return new WaitForSecondsRealtime(tab==2?.95f:1.2f);
            yield return new WaitForEndOfFrame();
            string folder=Path.Combine(Application.dataPath,"../LoadoutCaptures");Directory.CreateDirectory(folder);
            var texture=ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(folder,"tab-"+tab+"-choice-"+choice+".png"),texture.EncodeToPNG());Destroy(texture);
            Require(studio.Draft.tamer==value.tamer&&studio.Draft.field==value.field&&studio.Draft.finisher==value.finisher,"preview selection preserved");
        }
        Debug.Log("LOADOUT SMOKE COMPLETE: "+checks+" checks, 10 captures.");Application.Quit();
    }
    void OnDestroy(){studio.Dispose();}
}
#endif
