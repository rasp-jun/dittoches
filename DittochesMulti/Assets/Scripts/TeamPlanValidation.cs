#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

public sealed class TeamPlanValidation : MonoBehaviour
{
    readonly TeamPlannerUI ui=new TeamPlannerUI();
    string previous;
    int checks,clickStep;
    Vector2 clickPoint;
    bool open=true;
    readonly string[] board={"koromon","agumon","greymon","greymon"},bench={"gabumon","wargreymon"};
    void Require(bool condition,string message)
    {if(!condition){Debug.LogError("TEAM PLAN FAILED: "+message);Application.Quit(2);throw new InvalidOperationException(message);}checks++;}
    void OnGUI()
    {
        GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1600f,Screen.height/1000f,1));
        Event saved=null;
        if(clickStep>0&&Event.current.type==EventType.Repaint)
        {saved=new Event(Event.current);Event.current=new Event{type=clickStep==1?EventType.MouseDown:EventType.MouseUp,button=0,mousePosition=clickPoint};clickStep=clickStep==1?2:0;}
        if(open)open=ui.Draw(new Rect(0,0,1600,1000),board,bench,6,true);
        if(saved!=null)Event.current=saved;
    }
    IEnumerator Click(float x,float y)
    {clickPoint=new Vector2(x,y);clickStep=1;while(clickStep>0)yield return null;yield return null;}
    void Capture(string name)
    {
        string folder=Path.Combine(Application.dataPath,"../TeamPlanCaptures");Directory.CreateDirectory(folder);
        var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());Destroy(image);
    }
    IEnumerator Start()
    {
        Application.runInBackground=true;Application.targetFrameRate=60;
        previous=PortablePreviewPrefs.GetString(TeamPlan.Key,"");yield return null;
        var roster=TeamPlan.Roster;Require(roster.Length==30,"only 30 recruitable units in planner");
        var malformed=TeamPlan.Decode("invalid");Require(malformed.plans.Length==3&&malformed.plans.All(p=>p.units.Count==0),"invalid JSON defaults");
        malformed=TeamPlan.Decode("{\"active\":99,\"plans\":[null,{\"units\":[\"agumon\",\"agumon\",\"enemy_unknown\",null]}]}");
        Require(malformed.active==2&&malformed.plans[1].units.SequenceEqual(new[]{"agumon"})&&malformed.plans[2].units.Count==0,"normalize damaged plans and duplicates");
        TeamPlan.Restore("{}");
        for(int i=0;i<9;i++)Require(TeamPlan.Toggle(roster[i].id),"add goal "+i);
        Require(!TeamPlan.Toggle(roster[9].id)&&TeamPlan.Units.Count==9,"nine goal cap");
        Require(!TeamPlan.Toggle("enemy_unknown")&&TeamPlan.Units.Count==9,"invalid unit rejected");
        Require(TeamPlan.Toggle(roster[0].id)&&TeamPlan.Units.Count==8,"remove existing goal");
        TeamPlan.Data.active=2;TeamPlan.Toggle("wargreymon");TeamPlan.Reload();
        Require(TeamPlan.Data.active==2&&TeamPlan.Units.SequenceEqual(new[]{"wargreymon"})&&TeamPlan.Data.plans[0].units.Count==8,"three presets persist independently");
        for(int cost=1;cost<=5;cost++)Require(TeamPlan.Search("",cost).Length==6&&TeamPlan.Search("",cost).All(u=>u.cost==cost),"cost filter "+cost);
        foreach(var u in roster)
        {
            Require(TeamPlan.Search(u.name,0).Any(d=>d.id==u.id),"name search "+u.id);
            foreach(var t in DigimonBuildCatalog.ForUnit(u.id))Require(TeamPlan.Search(t.name,0).Any(d=>d.id==u.id),"trait search "+u.id);
        }
        Require(TeamPlan.Search("없음XYZ",0).Length==0&&TeamPlan.Search("  ",0).Length==30,"empty and missing search");
        Require(TeamPlan.Progress("greymon",board,bench)=="전장 배치"&&TeamPlan.Progress("gabumon",board,bench)=="대기석 보유"&&TeamPlan.Progress("rosemon",board,bench)=="모집 필요","progress separates deployed, benched, missing");
        foreach(var t in DigimonBuildCatalog.Data.traits)Require(DigimonBuildCatalog.Count(t,board)==DigimonBuildCatalog.Count(t,board.Distinct()),"duplicate units never double count traits");
        TeamPlan.Restore("{}");yield return Click(170,355);Require(TeamPlan.Contains("koromon"),"card click adds goal");
        yield return Click(170,355);Require(!TeamPlan.Contains("koromon"),"card click removes goal");
        yield return Click(170,355);yield return Click(450,106);Require(TeamPlan.Data.active==1&&TeamPlan.Units.Count==0,"preset button selects independent plan");
        yield return Click(170,355);yield return Click(160,887);Require(TeamPlan.Units.Count==0,"clear button clears only active plan");
        yield return Click(325,887);Require(TeamPlan.Contains("koromon")&&TeamPlan.Data.plans[0].units.Contains("koromon"),"undo restores cleared plan without changing other preset");
        foreach(var u in roster.Skip(1).Take(8))TeamPlan.Toggle(u.id);
        yield return Click(1045,448);Require(TeamPlan.Units.Count==9&&!TeamPlan.Contains(roster[9].id),"full plan cannot add tenth goal via UI");
        yield return new WaitForEndOfFrame();Capture("01-plans-and-traits");
        yield return Click(510,887);Require(TeamPlan.Units.SequenceEqual(board.Distinct()),"capture current board ignores duplicates and bench");
        Require(board.Length==4&&bench.Length==2,"capture never mutates actual formation");
        yield return Click(325,887);Require(TeamPlan.Units.Count==9,"undo restores replaced plan");
        ui.Query="그레이몬";ui.Cost=0;yield return new WaitForEndOfFrame();Capture("02-name-search");
        ui.Query="";ui.Cost=5;yield return new WaitForEndOfFrame();Capture("03-ultimate-filter");
        ui.Query="없음XYZ";yield return new WaitForEndOfFrame();Capture("04-empty-search");
        yield return Click(1460,106);Require(!open,"close button exits modal");
        Debug.Log("TEAM PLAN SMOKE COMPLETE: "+checks+" checks, 4 captures.");Application.Quit();
    }
    void OnDestroy(){if(previous!=null){PortablePreviewPrefs.SetString(TeamPlan.Key,previous);PortablePreviewPrefs.Save();TeamPlan.Reload();}}
}
#endif
