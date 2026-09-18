#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    int validationChecks;
    int validationRepaints;
    bool validationIconGallery;
    bool validatedIconInput;
    Vector2? validationPointer;
    bool validationShopInputPending;
    void ValidateShopInputInGUI()
    {
        if(!validationShopInputPending||Event.current.type!=EventType.Repaint)return;
        validationShopInputPending=false;Event saved=new Event(Event.current);bool enabled=GUI.enabled;int savedGold=gold;
        var offer=shop[0];int count=board.Concat(bench).Count(u=>u!=null);
        try
        {
            GUI.enabled=true;gold=0;
            Event.current=new Event{type=EventType.MouseDown,button=1,mousePosition=new Vector2(249+156,893+22)};
            DrawArenaHudShop();
            Require(skillDetailUnit!=null&&skillDetailUnit.def==offer&&skillDetailFromShop,"shop icon opens basic skill inspection without gold");
            Require(gold==0&&shop[0]==offer&&board.Concat(bench).Count(u=>u!=null)==count,"shop inspection cannot buy or consume gold");
        }
        finally{Event.current=saved;GUI.enabled=enabled;gold=savedGold;}
    }
    void ValidateIconInputInGUI()
    {
        if(validatedIconInput||Event.current.type!=EventType.Repaint)return;
        validatedIconInput=true;Event saved=new Event(Event.current);bool enabled=GUI.enabled;
        try
        {
            GUI.enabled=true;Rect r=new Rect(10,10,60,60);
            Event.current=new Event{type=EventType.MouseDown,button=1,mousePosition=r.center};
            Require(DigimonSkillUI.DrawIcon(r,DigimonSkillCatalog.Find("agumon")),"right click requests skill details in GUI");
            Event.current=new Event{type=EventType.MouseDown,button=0,mousePosition=r.center};
            Require(!DigimonSkillUI.DrawIcon(r,DigimonSkillCatalog.Find("agumon")),"left click does not request skill details");
        }
        finally{Event.current=saved;GUI.enabled=enabled;}
    }
    void DrawValidationIcons()
    {
        DrawRect(new Rect(150,90,1620,900),new Color(.02f,.04f,.065f));int i=0;
        foreach(var s in DigimonSkillCatalog.All)
        {
            float x=180+i%6*260,y=118+i/6*145;
            GUI.DrawTexture(new Rect(x,y,72,72),DigimonSkillUI.Icon(s));
            GUI.Label(new Rect(x+84,y,163,46),s.name,new GUIStyle(label){wordWrap=true});
            GUI.Label(new Rect(x+84,y+49,165,50),s.ScalingRole+"\n기본 공격 "+s.attackRange+"칸",label);i++;
        }
    }
    void Require(bool condition,string message)
    {if(!condition){Application.Quit(2);throw new InvalidOperationException("ARENA SMOKE FAILED: "+message);}validationChecks++;}
    public void BeginArenaSmoke(){StartCoroutine(ArenaSmoke());}
    void ValidateCombatLabels()
    {
        var layout=new CombatLabelLayout();var bounds=new Rect(312,205,956,524);
        foreach(Vector2 anchor in new[]{bounds.center,bounds.min,bounds.max})
        {
            var rows=Enumerable.Range(0,18).Select(i=>new CombatLabelLayout.Entry{key=i,anchor=anchor,width=i%4==0?120:72,caption=i%4==0,priority=i==0}).ToList();
            layout.Arrange(rows,bounds);var placed=rows.Select(r=>r.rect).ToArray();
            Require(rows.All(r=>r.rect.xMin>=bounds.xMin&&r.rect.yMin>=bounds.yMin&&r.rect.xMax<=bounds.xMax&&r.rect.yMax<=bounds.yMax),"crowded combat labels stay inside field");
            Require(rows.All(a=>rows.All(b=>a==b||!a.rect.Overlaps(b.rect))),"crowded combat labels do not overlap");
            layout.Arrange(rows,bounds);
            Require(rows.Select((r,i)=>r.rect==placed[i]).All(same=>same),"combat label placement is deterministic");
        }
    }
    void CaptureRuntime(string name)
    {
        string folder=Path.Combine(Application.dataPath,"../ArenaCaptures");Directory.CreateDirectory(folder);
        var texture=ScreenCapture.CaptureScreenshotAsTexture();
        try{File.WriteAllBytes(Path.Combine(folder,name+".png"),texture.EncodeToPNG());}
        finally{Destroy(texture);}
        Debug.Log("ARENA SCREEN "+name+" repaints="+validationRepaints+" actors="+arena.ActorCount);
        Require(validationRepaints>0,"screen received GUI repaint");
        RenderTexture old=RenderTexture.active;var field=new Texture2D(arena.Texture.width,arena.Texture.height,TextureFormat.RGB24,false);
        try{RenderTexture.active=arena.Texture;field.ReadPixels(new Rect(0,0,field.width,field.height),0,0);field.Apply();File.WriteAllBytes(Path.Combine(folder,name+"-field.png"),field.EncodeToPNG());}
        finally{RenderTexture.active=old;Destroy(field);}
    }
    IEnumerator ArenaSmoke()
    {
        Application.runInBackground=true;
        yield return null;
        ValidateBuildRules();
        ValidateScalingRules();
        ValidateTacticalRules();
        ValidateReportRules();
        ValidateFormationRules();
        ValidateCombatLabels();
        artPack=0;lobby=false;round=12;level=6;gold=40;hp=100;showCombatReport=false;
        Array.Clear(board,0,board.Length);Array.Clear(bench,0,bench.Length);inventory.Clear();
        string[] team={"agumon","greymon","garurumon","gabumon","palmon","lilimon"};
        int[] slots={2,3,4,16,17,18};
        for(int i=0;i<team.Length;i++)board[slots[i]]=new Unit(RosterById[team[i]]);
        bench[0]=new Unit(RosterById["koromon"]);bench[3]=new Unit(RosterById["agumon"]);
        inventory.AddRange(new[]{0,1,2,3});RebuildPool();RollShop();EnsureArena();
        board[3].items.Add(8);board[17].items.Add(10);board[2].items.Add(4);
        Require(!DigimonModelLibrary.PreviewEnabled,"unfinished model must be opt-in");
        for(int row=0;row<8;row++)for(int col=0;col<7;col++)
        {
            Vector2 point=arena.Project(TacticalArena.CellWorld(col,row));
            Require(SoloArenaViewport.Contains(point),"visible hex "+row+":"+col);
            Require(arena.HitCell(point)==row*7+col,"hex picking "+row+":"+col);
        }
        for(int i=0;i<9;i++)Require(arena.HitBench(arena.Project(TacticalArena.BenchWorld(i)))==i,"bench picking "+i);
        Require(!SoloArenaViewport.Contains(SellDropZone.center),"sell target cannot also place on board");
        yield return new WaitForSeconds(.4f);yield return new WaitForEndOfFrame();CaptureRuntime("01-prepare");
        traitFocus=TeamTraits().First(t=>t.key=="용기");traitGuideUntil=Time.unscaledTime+60;
        yield return new WaitForEndOfFrame();CaptureRuntime("08-synergies");traitFocus=null;
        recipeFocus=1;showRecipeGuide=true;recipeGuideUntil=Time.unscaledTime+60;
        yield return new WaitForEndOfFrame();CaptureRuntime("09-recipes");showRecipeGuide=false;
        selectedBoard=3;validationPointer=arena.Project(TacticalArena.CellWorld(3,4));
        yield return new WaitForEndOfFrame();CaptureRuntime("13-melee-range");
        selectedBoard=18;validationPointer=arena.Project(TacticalArena.CellWorld(4,6));
        yield return new WaitForEndOfFrame();CaptureRuntime("14-ranged-range");selectedBoard=-1;
        selectedItem=3;validationPointer=arena.Project(TacticalArena.CellWorld(3,4));
        yield return new WaitForEndOfFrame();CaptureRuntime("15-equipment-preview");
        selectedItem=-1;validationPointer=null;
        int item=Array.FindIndex(shop,u=>u!=null);int cost=shop[item].cost,before=gold;
        Require(Buy(item),"shop purchase");Require(gold==before-cost&&shop[item]==null,"purchase charged once");
        selectedBench=0;selectedBoard=-1;
        yield return new WaitForEndOfFrame();CaptureRuntime("02-placement");selectedBench=-1;
        inspectedUnit=board[3];yield return new WaitForEndOfFrame();CaptureRuntime("03-detail");
        OpenSkillDetails(board[3]);yield return new WaitForEndOfFrame();CaptureRuntime("10-skill-magic");skillDetailUnit=null;
        OpenSkillDetails(new Unit(RosterById["wargreymon"]){star=2});yield return new WaitForEndOfFrame();CaptureRuntime("11-skill-hybrid");skillDetailUnit=null;
        validationIconGallery=true;yield return new WaitForEndOfFrame();CaptureRuntime("12-skill-icons");validationIconGallery=false;inspectedUnit=null;
        selectedBench=0;selectedBoard=-1;validationPointer=arena.Project(TacticalArena.CellWorld(3,6));
        yield return new WaitForEndOfFrame();CaptureRuntime("19-synergy-swap");
        validationPointer=arena.Project(TacticalArena.CellWorld(6,7));
        yield return new WaitForEndOfFrame();CaptureRuntime("21-placement-blocked");selectedBench=-1;validationPointer=null;
        var previousOffer=shop[0];shop[0]=RosterById["wargreymon"];validationShopInputPending=true;
        yield return new WaitForEndOfFrame();CaptureRuntime("20-shop-skill");Require(!validationShopInputPending,"shop right click exercised in GUI");skillDetailUnit=null;shop[0]=previousOffer;
        Unit held=bench[0],swapped=board[17];Vector3 destination=TacticalArena.CellWorld(3,6);
        Vector2 left=arena.Project(destination+Vector3.left*.18f),right=arena.Project(destination+Vector3.right*.18f);
        Require(arena.HitCell(left)==45&&arena.HitCell(right)==45,"drag fixture stays inside one hex");
        dragFromBoard=false;dragSource=0;draggingUnit=true;validationPointer=left;
        yield return new WaitForSeconds(.3f);yield return new WaitForEndOfFrame();Vector3 first=formationPositions[held];
        validationPointer=right;yield return new WaitForSeconds(.3f);yield return new WaitForEndOfFrame();
        Require(formationPositions[held].x-first.x>.2f,"held unit follows pointer continuously within one hex");
        Require(bench[0]==held&&board[17]==swapped,"drag preview leaves formation unchanged");
        CaptureRuntime("22-drag-follow");DropDraggedUnit(right);draggingUnit=false;dragSource=-1;validationPointer=null;
        yield return new WaitForSeconds(.4f);yield return new WaitForEndOfFrame();
        Require(board[17]==held&&bench[0]==swapped,"drop swaps board and bench units");
        Require(Vector3.Distance(formationPositions[held],destination)<.05f,"dropped unit settles onto target hex");
        CaptureRuntime("23-drop-settle");board[17]=swapped;bench[0]=held;inspectedUnit=null;
        StartCoroutine(Battle());
        foreach(Fighter fighter in fighters)fighter.mana=fighter.maxMana;
        inspectedUnit=board[18];
        yield return new WaitForSeconds(.8f);yield return new WaitForEndOfFrame();CaptureRuntime("04-combat");
        yield return new WaitForSeconds(2f);yield return new WaitForEndOfFrame();CaptureRuntime("05-skills");
        inspectedUnit=null;tacticalReportMetric=0;yield return new WaitForEndOfFrame();CaptureRuntime("16-live-damage");
        tacticalReportMetric=1;yield return new WaitForEndOfFrame();CaptureRuntime("17-damage-received");tacticalReportMetric=0;
        float deadline=Time.unscaledTime+34;
        while(battling&&Time.unscaledTime<deadline)yield return null;
        Require(!battling,"battle reaches results");Require(lastBattleReport.Count==6,"combat report preserves team");
        Require(lastBattleReport.Sum(f=>f.casts)>0,"canonical skills actually cast in runtime");
        Require(lastBattleReport.All(f=>Mathf.Abs(f.damageDone-f.basicDamageDone-f.skillDamageDone)<.1f),"completed native report damage totals reconcile");
        Require(lastBattleReport.All(f=>!board.Contains(f.unit)),"completed report unit records are detached from preparation board");
        Require(skillCasts.Count==0||skillCasts.All(c=>c.hits>=c.skill.shots),"no unprocessed released hits");
        yield return new WaitForEndOfFrame();CaptureRuntime("06-result");
        inspectedUnit=lastBattleReport.OrderByDescending(f=>f.damageDone).First().unit;
        yield return new WaitForEndOfFrame();CaptureRuntime("18-last-combat-unit");inspectedUnit=null;
        artPack=1;EnsureArena();Require(SoloArenaViewport==TacticalArena.SoloViewport,"original layout retained");
        yield return new WaitForEndOfFrame();CaptureRuntime("07-original");
        Debug.Log("ARENA SMOKE COMPLETE: "+validationChecks+" checks");Application.Quit();
    }
}
#endif
