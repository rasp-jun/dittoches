#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

public sealed partial class TeamPlanValidation : MonoBehaviour
{
    readonly TeamPlannerUI ui=new TeamPlannerUI();
    string previous;
    int checks,clickStep,clickButton,captures;
    KeyCode keyPending;
    Vector2 clickPoint;
    bool open=true,wheelPending;
    readonly string[] board={"koromon","agumon","greymon","greymon"},bench={"gabumon","wargreymon"};
    void Require(bool condition,string message)
    {if(!condition){Debug.LogError("TEAM PLAN FAILED: "+message);Application.Quit(2);throw new InvalidOperationException(message);}checks++;}
    void OnGUI()
    {
        GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1600f,Screen.height/1000f,1));
        Event saved=null;
        if(clickStep>0&&Event.current.type==EventType.Repaint)
        {saved=new Event(Event.current);Event.current=new Event{type=clickStep==1?EventType.MouseDown:EventType.MouseUp,button=clickButton,mousePosition=clickPoint};clickStep=clickStep==1?2:0;}
        if(keyPending!=KeyCode.None&&Event.current.type==EventType.Repaint){saved=new Event(Event.current);Event.current=new Event{type=EventType.KeyDown,keyCode=keyPending};keyPending=KeyCode.None;}
        if(wheelPending&&Event.current.type==EventType.Repaint){saved=new Event(Event.current);Event.current=new Event{type=EventType.ScrollWheel,mousePosition=clickPoint,delta=new Vector2(0,100)};wheelPending=false;}
        if(open)open=ui.Draw(new Rect(0,0,1600,1000),board,bench,6,true,collection:collectionData);
        if(saved!=null)Event.current=saved;
    }
    IEnumerator Click(float x,float y,int button=0)
    {clickPoint=new Vector2(x,y);clickButton=button;clickStep=1;while(clickStep>0)yield return null;yield return null;}
    IEnumerator Click(Rect r,int button=0){yield return Click(r.center.x,r.center.y,button);}
    IEnumerator Press(KeyCode key){keyPending=key;while(keyPending!=KeyCode.None)yield return null;yield return null;}
    void Capture(string name)
    {
        string folder=Path.Combine(Application.dataPath,"../TeamPlanCaptures");Directory.CreateDirectory(folder);
        var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());Destroy(image);
        captures++;Debug.Log("TEAM PLAN SCREEN "+name);
    }
    IEnumerator ValidateTraitInspection(Rect panel)
    {
        Rect canvas=new Rect(0,0,1600,1000),guide=DigimonTraitUI.GuideBounds(canvas),viewport=TeamPlannerUI.SynergyViewport(panel);
        TeamPlan.Restore("{}");foreach(var id in DigimonBuildCatalog.Find("light").members)TeamPlan.Toggle(id);
        string plan=JsonUtility.ToJson(TeamPlan.Data),query=ui.Query;int price=ui.Cost;
        float library=ui.LibraryScrollY;
        int index=Array.FindIndex(TeamPlannerUI.ForecastTraits(),t=>t.id=="light");
        yield return Click(viewport.x+100,viewport.y+index*48-ui.SynergyScrollY+21);
        Require(ui.TraitFocus=="light"&&ui.InspectionId==""&&JsonUtility.ToJson(TeamPlan.Data)==plan,"forecast opens nine-sanctuary details without changing plan (trait="+ui.TraitFocus+", index="+index+", scroll="+ui.SynergyScrollY+")");
        yield return new WaitForEndOfFrame();Capture("09-planned-nine-sanctuary");
        foreach(var trait in DigimonBuildCatalog.Data.traits)
        {
            int i=Array.IndexOf(DigimonBuildCatalog.Data.traits,trait);float step=Mathf.Min(60,(guide.height-118)/DigimonBuildCatalog.Data.traits.Length);
            yield return Click(new Rect(guide.x+18,guide.y+102+i*step,254,step-7));
            Require(ui.TraitFocus==trait.id&&JsonUtility.ToJson(TeamPlan.Data)==plan,"all trait pages navigate without changing planned formation "+trait.id);
            if(trait.id=="machine"){yield return new WaitForEndOfFrame();Capture("10-planned-inactive-machine");}
        }
        int casterIndex=Array.FindIndex(DigimonBuildCatalog.Data.traits,t=>t.id=="caster");float railStep=Mathf.Min(60,(guide.height-118)/DigimonBuildCatalog.Data.traits.Length);
        yield return Click(new Rect(guide.x+18,guide.y+102+casterIndex*railStep,254,railStep-7));
        Require(DigimonTraitUI.ReviewMemberScroll.y==0,"switching trait starts contributors at first row");
        clickPoint=new Vector2(guide.x+760,guide.y+580);wheelPending=true;while(wheelPending)yield return null;yield return null;
        float expected=Mathf.Max(0,Mathf.CeilToInt(DigimonBuildCatalog.Find("caster").members.Length/3f)*90-181);
        Require(expected>0&&DigimonTraitUI.ReviewMemberScroll.y==expected&&ui.LibraryScrollY==library,"child wheel reaches final contributors without scrolling parent library");
        yield return new WaitForEndOfFrame();Capture("11-planned-caster-last-row");
        yield return Click(guide.center.x,guide.y+340);
        foreach(var key in new[]{KeyCode.D,KeyCode.F,KeyCode.Return,KeyCode.E})yield return Press(key);
        Require(JsonUtility.ToJson(TeamPlan.Data)==plan&&ui.Query==query&&ui.Cost==price,"trait child blocks parent clicks and keys and preserves filters");
        yield return Press(KeyCode.Escape);Require(open&&ui.TraitFocus==""&&ui.Query==query&&ui.Cost==price,"Escape closes only trait guide and preserves filters");
        yield return Click(viewport.x+100,viewport.y+index*48-ui.SynergyScrollY+21);yield return Click(TeamPlannerUI.ActionRect(panel,0).x+5,TeamPlannerUI.ActionRect(panel,0).center.y);
        Require(open&&ui.TraitFocus==""&&JsonUtility.ToJson(TeamPlan.Data)==plan,"outside click closes trait guide without clearing plan");
        yield return Click(viewport.x+100,viewport.y+index*48-ui.SynergyScrollY+21);yield return Click(DigimonTraitUI.GuideCloseRect(canvas));
        Require(open&&ui.TraitFocus==""&&JsonUtility.ToJson(TeamPlan.Data)==plan,"trait guide close button returns to unchanged planner");
        TeamPlan.Restore("{}");
        foreach(var u in TeamPlan.Roster.OrderByDescending(u=>DigimonBuildCatalog.ForUnit(u.id).Count()))
        {
            if(TeamPlan.Units.Count==9)break;
            if(DigimonBuildCatalog.ForUnit(u.id).Any(t=>!TeamPlannerUI.ForecastTraits().Contains(t)))TeamPlan.Toggle(u.id);
        }
        Require(TeamPlannerUI.ForecastTraits().Length*48>viewport.height,"mixed plan overflows forecast sidebar");
        clickPoint=viewport.center;wheelPending=true;while(wheelPending)yield return null;yield return null;
        var traits=TeamPlannerUI.ForecastTraits();float scroll=ui.SynergyScrollY;plan=JsonUtility.ToJson(TeamPlan.Data);
        yield return Click(viewport.x+100,viewport.y+(traits.Length-1)*48-scroll+21);
        Require(ui.TraitFocus==traits[traits.Length-1].id,"scrolled last forecast opens correct trait");
        yield return new WaitForEndOfFrame();Capture("12-planned-scrolled-forecast");
        yield return Press(KeyCode.Escape);Require(open&&ui.SynergyScrollY==scroll&&JsonUtility.ToJson(TeamPlan.Data)==plan,"return from trait preserves forecast scroll and plan");
        yield return Press(KeyCode.Escape);Require(!open,"second Escape closes planner after trait inspection");open=true;
        Debug.Log("PLANNER SYNERGY COMPLETE: thirteen trait pages, nine-sanctuary forecast, inactive candidates, child scrolling, last forecast row, preserved plans and nested controls");
    }
    IEnumerator Start()
    {
        Application.runInBackground=true;Application.targetFrameRate=60;
        previous=PortablePreviewPrefs.GetString(TeamPlan.Key,"");yield return null;
        var roster=TeamPlan.Roster;Require(roster.Length==47,"all 47 recruitable units in planner");
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
        for(int cost=1;cost<=5;cost++)Require(TeamPlan.Search("",cost).Length==new[]{0,7,7,14,10,9}[cost]&&TeamPlan.Search("",cost).All(u=>u.cost==cost),"cost filter "+cost);
        foreach(var u in roster)
        {
            Require(TeamPlan.Search(u.name,0).Any(d=>d.id==u.id),"name search "+u.id);
            foreach(var t in DigimonBuildCatalog.ForUnit(u.id))Require(TeamPlan.Search(t.name,0).Any(d=>d.id==u.id),"trait search "+u.id);
        }
        Require(TeamPlan.Search("없음XYZ",0).Length==0&&TeamPlan.Search("  ",0).Length==47,"empty and missing search");
        Require(TeamPlan.Progress("greymon",board,bench)=="전장 배치"&&TeamPlan.Progress("gabumon",board,bench)=="대기석 보유"&&TeamPlan.Progress("rosemon",board,bench)=="모집 필요","progress separates deployed, benched, missing");
        foreach(var t in DigimonBuildCatalog.Data.traits)Require(DigimonBuildCatalog.Count(t,board)==DigimonBuildCatalog.Count(t,board.Distinct()),"duplicate units never double count traits");
        Rect panel=TeamPlannerUI.Bounds(new Rect(0,0,1600,1000));
        TeamPlan.Restore("{}");
        yield return Click(TeamPlannerUI.CardRect(panel,46));Require(TeamPlan.Units.Count==0,"clipped last row cannot be clicked before scrolling");
        yield return Click(TeamPlannerUI.CardRect(panel,46),1);Require(ui.InspectionId==""&&TeamPlan.Units.Count==0,"clipped card cannot be inspected before scrolling");
        yield return Click(TeamPlannerUI.CardRect(panel,0));Require(TeamPlan.Contains("koromon"),"card click adds goal");
        yield return Click(TeamPlannerUI.CardRect(panel,0));Require(!TeamPlan.Contains("koromon"),"card click removes goal");
        yield return Click(TeamPlannerUI.CardRect(panel,0));yield return Click(TeamPlannerUI.GoalRect(panel,0));Require(TeamPlan.Units.Count==0,"target portrait removes selected goal");
        yield return Click(TeamPlannerUI.CardRect(panel,0));yield return Click(TeamPlannerUI.PlanRect(panel,1));Require(TeamPlan.Data.active==1&&TeamPlan.Units.Count==0,"preset button selects independent plan");
        yield return Click(TeamPlannerUI.CardRect(panel,0));yield return Click(TeamPlannerUI.ActionRect(panel,0));Require(TeamPlan.Units.Count==0,"clear button clears only active plan");
        yield return Click(TeamPlannerUI.ActionRect(panel,1));Require(TeamPlan.Contains("koromon")&&TeamPlan.Data.plans[0].units.Contains("koromon"),"undo restores cleared plan without changing other preset");
        foreach(var u in roster.Skip(1).Take(8))TeamPlan.Toggle(u.id);
        yield return Click(TeamPlannerUI.CardRect(panel,9));Require(TeamPlan.Units.Count==9&&!TeamPlan.Contains(roster[9].id),"full plan cannot add tenth goal via UI");
        yield return new WaitForEndOfFrame();Capture("01-plans-and-traits");
        yield return Click(TeamPlannerUI.ActionRect(panel,2));Require(TeamPlan.Units.SequenceEqual(board.Distinct()),"capture current board ignores duplicates and bench");
        Require(board.Length==4&&bench.Length==2,"capture never mutates actual formation");
        yield return Click(TeamPlannerUI.ActionRect(panel,1));Require(TeamPlan.Units.Count==9,"undo restores replaced plan");
        ui.Query="그레이몬";ui.Cost=0;yield return new WaitForEndOfFrame();Capture("02-name-search");
        ui.Query="";ui.Cost=5;yield return new WaitForEndOfFrame();Capture("03-ultimate-filter");
        ui.Query="";ui.Cost=0;TeamPlan.Restore("{}");yield return null;
        clickPoint=TeamPlannerUI.ActionRect(panel,0).center;wheelPending=true;while(wheelPending)yield return null;yield return null;
        Require(Mathf.Approximately(ui.LibraryScrollY,0),"wheel outside library cannot scroll its cards");
        clickPoint=new Vector2(panel.x+680,panel.y+620);wheelPending=true;while(wheelPending)yield return null;yield return null;
        Require(ui.LibraryScrollY>0,"mouse wheel scrolls the expanded roster library");
        Rect lastCard=TeamPlannerUI.CardRect(panel,46);lastCard.y-=ui.LibraryScrollY;
        Require(TeamPlannerUI.LibraryViewport(panel).Contains(lastCard.center),"last row click point lies inside visible library");
        yield return Click(lastCard);Require(TeamPlan.Contains(TeamPlan.Search("",0)[46].id),"last row is reachable and clickable after scrolling");
        yield return new WaitForEndOfFrame();Capture("05-library-last-row");
        yield return Click(TeamPlannerUI.ActionRect(panel,1));Require(TeamPlan.Units.Count==0,"footer undo remains clickable after library scrolling");
        yield return Click(lastCard);Require(TeamPlan.Contains(TeamPlan.Search("",0)[46].id),"last row selection remains correct after footer undo");
        string storedPlan=JsonUtility.ToJson(TeamPlan.Data);float storedScroll=ui.LibraryScrollY;
        yield return Click(lastCard,1);
        Require(ui.InspectionId==TeamPlan.Search("",0)[46].id&&JsonUtility.ToJson(TeamPlan.Data)==storedPlan,"right-click final scrolled card opens inspection without changing plan");
        yield return new WaitForEndOfFrame();Capture("06-right-click-last-row");
        clickPoint=TeamPlannerUI.LibraryViewport(panel).center;wheelPending=true;while(wheelPending)yield return null;yield return null;
        Require(ui.LibraryScrollY==storedScroll,"inspection blocks scrolling the library behind it");
        yield return Click(TeamPlannerUI.InspectionRect(new Rect(0,0,1600,1000)).center.x,440);
        Require(ui.InspectionId.Length>0&&JsonUtility.ToJson(TeamPlan.Data)==storedPlan,"inspection clicks never select the card underneath");
        foreach(KeyCode key in new[]{KeyCode.D,KeyCode.F,KeyCode.Return,KeyCode.E})yield return Press(key);
        Require(ui.InspectionId.Length>0&&ui.Query==""&&JsonUtility.ToJson(TeamPlan.Data)==storedPlan,"inspection consumes gameplay keys and preserves search and plan");
        yield return Press(KeyCode.Escape);
        Require(open&&ui.InspectionId==""&&ui.LibraryScrollY==storedScroll,"first Escape returns to the same scrolled planner");
        yield return Click(TeamPlannerUI.GoalRect(panel,0),1);
        Require(ui.InspectionId==TeamPlan.Units[0]&&JsonUtility.ToJson(TeamPlan.Data)==storedPlan,"goal portrait right-click inspects without removing goal");
        yield return new WaitForEndOfFrame();Capture("07-goal-inspection");
        yield return Click(TeamPlannerUI.ActionRect(panel,0));
        Require(open&&ui.InspectionId==""&&JsonUtility.ToJson(TeamPlan.Data)==storedPlan,"outside click closes inspection without clearing the underlying plan");
        ui.Query="피에몬";ui.Cost=5;yield return Click(TeamPlannerUI.CardRect(panel,0),1);
        Require(ui.InspectionId=="piedmon"&&JsonUtility.ToJson(TeamPlan.Data)==storedPlan,"filtered enemy inspection preserves unrelated goals");
        yield return new WaitForEndOfFrame();Capture("08-filtered-enemy-inspection");
        Rect inspect=TeamPlannerUI.InspectionRect(new Rect(0,0,1600,1000));yield return Click(new Rect(inspect.xMax-50,inspect.y+17,32,32));
        Require(open&&ui.InspectionId==""&&ui.Query=="피에몬"&&ui.Cost==5,"close button preserves active filters (open="+open+", inspect="+ui.InspectionId+", query="+ui.Query+", cost="+ui.Cost+")");
        yield return Click(TeamPlannerUI.CardRect(panel,0),1);yield return Press(KeyCode.Escape);yield return Press(KeyCode.Escape);
        Require(!open&&ui.InspectionId=="","second Escape closes the planner");open=true;
        yield return ValidateTraitInspection(panel);
        yield return ValidateCollection(panel);
        ui.Query="성역";ui.Cost=0;yield return new WaitForEndOfFrame();Capture("04-light-line");
        Require(TeamPlan.Search("성역",0).Length==9,"all nine sanctuary units searchable");
        foreach(var id in new[]{"nyaromon","plotmon","tailmon","angewomon","ophanimon","kuwagamon","shellmon","devimon","etemon","monochromon","ogremon","meramon","leomon","andromon","skullgreymon","piedmon","machinedramon"})
        {
            ui.Query=TeamPlan.Roster.First(u=>u.id==id).name;TeamPlan.Restore("{}");yield return Click(TeamPlannerUI.CardRect(panel,0));
            Require(TeamPlan.Contains(id),"new model card selectable "+id);
        }
        ui.Query="없음XYZ";yield return new WaitForEndOfFrame();Capture("04-empty-search");
        yield return Click(TeamPlannerUI.CloseRect(panel));Require(!open,"close button exits modal");
        Debug.Log("PLANNER INSPECTION COMPLETE: clipped-card right-click, goal inspection, nested input blocking, filters, close and Escape order");
        Debug.Log("TEAM PLAN SMOKE COMPLETE: "+checks+" checks, "+captures+" captures.");Application.Quit();
    }
    void OnDestroy(){if(previous!=null){PortablePreviewPrefs.SetString(TeamPlan.Key,previous);PortablePreviewPrefs.Save();TeamPlan.Reload();}}
}
#endif
