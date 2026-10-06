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
    bool validationEquipmentGallery;
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
            skillDetailUnit=null;
            Event.current=new Event{type=EventType.MouseDown,button=1,mousePosition=new Vector2(290,1025)};
            DrawArenaHudShop();
            Require(skillDetailUnit!=null&&skillDetailUnit.def==offer&&Event.current.type==EventType.Used,"whole card right click opens details and consumes input");
            Require(gold==0&&shop[0]==offer,"whole card inspection never purchases");
            var inspectedSkill=skillDetailUnit;skillDetailUnit=null;OpenEquipmentGuide(8);gold=50;var bag=inventory.ToArray();
            foreach(KeyCode key in new[]{KeyCode.D,KeyCode.F,KeyCode.Space})
            {
                Event.current=new Event{type=EventType.KeyDown,keyCode=key};DrawGame();
                Require(gold==50&&!battling&&inventory.SequenceEqual(bag),"equipment guide blocks underlying gameplay shortcut "+key);
            }
            Event.current=new Event{type=EventType.KeyDown,keyCode=KeyCode.Escape};DrawBuildRecipeGuide();
            Require(!showRecipeGuide&&Event.current.type==EventType.Used,"equipment guide closes with Escape");
            skillDetailUnit=null;OpenTeamPlan();
            foreach(KeyCode key in new[]{KeyCode.D,KeyCode.F,KeyCode.Space})
            {
                Event.current=new Event{type=EventType.KeyDown,keyCode=key};DrawGame();
                Require(gold==50&&!battling&&inventory.SequenceEqual(bag),"team planner blocks gameplay shortcut "+key);
            }
            Event.current=new Event{type=EventType.KeyDown,keyCode=KeyCode.Escape};DrawTeamPlan();
            Require(!showTeamPlan&&Event.current.type==EventType.Used,"team planner Escape closes without passing input through");
            OpenTraitGuide("friendship");
            foreach(KeyCode key in new[]{KeyCode.D,KeyCode.F,KeyCode.Space})
            {
                Event.current=new Event{type=EventType.KeyDown,keyCode=key};DrawGame();
                Require(gold==50&&!battling&&inventory.SequenceEqual(bag),"trait guide blocks gameplay shortcut "+key);
            }
            Event.current=new Event{type=EventType.KeyDown,keyCode=KeyCode.Escape};DrawBuildTraitGuide();
            Require(traitFocus==null&&Event.current.type==EventType.Used,"trait guide Escape consumes input");
            skillDetailUnit=inspectedSkill;
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
        if(validationEquipmentGallery)
        {
            DrawRect(new Rect(100,90,1720,890),new Color(.014f,.026f,.045f));
            GUI.Label(new Rect(140,111,1200,42),"디지털 무장 · 전체 15종",header);
            foreach(var item in DigimonBuildCatalog.Data.items)
            {
                float x=140+item.id%5*331,y=175+item.id/5*253;
                DigimonEquipmentArt.Draw(new Rect(x,y,114,114),item.id);
                GUI.Label(new Rect(x+128,y+6,192,56),item.name,header);
                GUI.Label(new Rect(x+128,y+65,184,52),item.role,label);
                GUI.Label(new Rect(x,y+129,302,68),DigimonEquipmentUI.Stats(item.id),new GUIStyle(small){wordWrap=true});
            }
            return;
        }
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
    void ValidateRecruitmentResults()
    {
        artPack=0;var def=RosterById["koromon"];
        for(int rank=1;rank<=3;rank++)
        {
            Array.Clear(board,0,board.Length);Array.Clear(shop,0,shop.Length);inventory.Clear();gold=10;
            for(int i=0;i<bench.Length;i++)bench[i]=new Unit(RosterById["agumon"]);
            if(rank==1)bench[0]=null;
            if(rank==2)
            {
                board[0]=new Unit(def);board[0].items.AddRange(new[]{8,12});
                bench[1]=new Unit(def);bench[1].items.AddRange(new[]{0,1});
            }
            if(rank==3)
            {
                board[0]=new Unit(def){star=2};board[0].items.AddRange(new[]{8,12});
                board[1]=new Unit(def){star=2};board[1].items.AddRange(new[]{13,6});
                board[2]=new Unit(def);board[2].items.AddRange(new[]{0,1});
                bench[0]=new Unit(def);bench[0].items.AddRange(new[]{2,3});
            }
            shop[0]=def;
            Require(Buy(0)&&gold==9&&shop[0]==null,"native purchase succeeds once at rank "+rank);
            Require(FindUnits(def.id,rank).Count==1,"native result has expected final rank "+rank);
            Require(inventory.Count==(rank==3?6:rank==2?2:0),"native purchase returns exact overflow "+rank);
            Require(placementNotice==RecruitmentAdvice.Result(UnitName(def),rank,1,inventory.Count),"native receipt keeps final stars/cost/overflow "+rank);
            string prior=placementNotice;Require(!Buy(0)&&gold==9&&placementNotice==prior,"repeat empty purchase adds no new receipt");
        }
        Array.Clear(board,0,board.Length);for(int i=0;i<bench.Length;i++)bench[i]=new Unit(RosterById["agumon"]);
        shop[0]=def;gold=10;placementNotice="unchanged";
        Require(!Buy(0)&&gold==10&&shop[0]==def&&placementNotice=="unchanged","full bench without pair cannot report success");
        bench[0]=null;gold=0;Require(!Buy(0)&&placementNotice=="unchanged","unaffordable purchase cannot report success");
        Array.Clear(board,0,board.Length);Array.Clear(bench,0,bench.Length);Array.Clear(shop,0,shop.Length);inventory.Clear();promotions.Clear();placementNoticeUntil=0;
    }
    void ValidateArenaVisualQuality()
    {
        string folder=Path.Combine(Application.dataPath,"../ArenaQualityCaptures");Directory.CreateDirectory(folder);
        string[] ids={"koromon","agumon","greymon","metalgreymon","wargreymon","gabumon","garurumon","metalgarurumon"};
        foreach(bool quality in new[]{false,true})
        using(var review=new TacticalArena(new Rect(0,0,1440,900),true,quality))
        {
            for(int pass=0;pass<3;pass++)
            {
                review.BeginFrame(-1,-1,-1,false);
                for(int i=0;i<ids.Length;i++)
                {
                    Vector3 at=TacticalArena.CellWorld(1+(i%4)*1.5f,2+(i/4)*3);
                    review.SetActor(i,at,null,i<4?Color.red:Color.green);
                    review.FaceActor(i,new Vector3(i%2==0?-.25f:.25f,0,-1));
                    review.PoseDigimon(i,ids[i],0,0,0,star:1);
                }
                review.Render();
            }
            int owned=review.OwnedResourceCount;review.BeginFrame(-1,-1,-1,false);
            Require(review.ValidateContactResources(),"shadow mesh and texture shared by all actors: "+quality);
            Require(review.ActorCount==8,"quality mode preserves character count");
            for(int i=0;i<ids.Length;i++)
            {
                review.SetActor(i,TacticalArena.CellWorld(1+(i%4)*1.5f,2+(i/4)*3),null,i<4?Color.red:Color.green);
                review.FaceActor(i,new Vector3(i%2==0?-.25f:.25f,0,-1));review.PoseDigimon(i,ids[i],0,0,0,star:1);
            }
            review.Render();Require(review.OwnedResourceCount==owned,"quality redraw allocates no new arena resources");
            Debug.Log("ARENA QUALITY "+quality+" MSAA="+review.Texture.antiAliasing+" resources="+owned);
            RenderTexture previous=RenderTexture.active;RenderTexture.active=review.Texture;
            var picture=new Texture2D(review.Texture.width,review.Texture.height,TextureFormat.RGB24,false);
            picture.ReadPixels(new Rect(0,0,picture.width,picture.height),0,0);picture.Apply();
            File.WriteAllBytes(Path.Combine(folder,quality?"enhanced.png":"baseline.png"),picture.EncodeToPNG());Destroy(picture);RenderTexture.active=previous;
        }
    }
    IEnumerator ArenaSmoke()
    {
        Application.runInBackground=true;
        yield return null;
        ValidateBuildRules();
        ValidateSynergyRules();
        ValidateScalingRules();
        ValidateTacticalRules();
        ValidateReportRules();
        ValidateFormationRules();
        ValidateCombatLabels();
        ValidateHudPerformance();
        ValidateRecruitmentResults();
        ValidateArenaVisualQuality();
        ValidateImpactMotion();
        ValidateSkillFinish();
        Require(RecruitmentAdvice.Interest(9)==0&&RecruitmentAdvice.Interest(10)==1&&RecruitmentAdvice.Interest(50)==5&&RecruitmentAdvice.Interest(99)==5,"interest thresholds and cap");
        Require(RecruitmentAdvice.Spend(20,1).Contains("19G")&&RecruitmentAdvice.Spend(20,1).Contains("−1G"),"purchase forecast warns on interest threshold");
        Require(RecruitmentAdvice.Spend(19,1).Contains("+1G")&&!RecruitmentAdvice.Spend(19,1).Contains("−"),"purchase forecast preserves interest inside bracket");
        Require(RecruitmentAdvice.Spend(0,5).Contains("5G 부족"),"unaffordable forecast never predicts negative bank");
        Require(RecruitmentAdvice.Copies(2,2).Contains("★★★")&&RecruitmentAdvice.Copies(2,1).Contains("★★ 자동"),"two and three star merge forecasts");
        Require(RecruitmentAdvice.Experience(6,19,20).Contains("4G")&&RecruitmentAdvice.Experience(6,0,20).Contains("20G"),"XP purchase rounds up to four gold");
        TeamPlan.Restore("{}");TeamPlan.Toggle("agumon");TeamPlan.Toggle("greymon");TeamPlan.Toggle("wargreymon");
        artPack=0;lobby=false;round=12;level=6;gold=40;hp=100;showCombatReport=false;
        Array.Clear(board,0,board.Length);Array.Clear(bench,0,bench.Length);inventory.Clear();
        string[] team={"agumon","greymon","garurumon","gabumon","palmon","lilimon"};
        int[] slots={2,3,4,16,17,18};
        for(int i=0;i<team.Length;i++)board[slots[i]]=new Unit(RosterById[team[i]]);
        bench[0]=new Unit(RosterById["koromon"]);bench[3]=new Unit(RosterById["agumon"]);
        inventory.AddRange(new[]{0,1,2,3});RebuildPool();RollShop();EnsureArena();
        board[3].items.Add(8);board[17].items.Add(10);board[2].items.Add(4);board[16].items.AddRange(new[]{6,8});
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
        int forecastGold=gold;var forecastOffer=shop[0];gold=20;shop[0]=RosterById["agumon"];
        validationPointer=new Vector2(360,970);
        yield return new WaitForSeconds(.6f);yield return new WaitForEndOfFrame();CaptureRuntime("24-purchase-forecast");
        validationPointer=new Vector2(120,950);
        yield return new WaitForSeconds(.6f);yield return new WaitForEndOfFrame();CaptureRuntime("25-level-forecast");
        validationPointer=null;gold=forecastGold;shop[0]=forecastOffer;
        traitFocus=TeamTraits().First(t=>t.key=="용기");traitGuideUntil=Time.unscaledTime+60;
        yield return new WaitForEndOfFrame();CaptureRuntime("08-synergies");traitFocus=null;
        foreach(var trait in DigimonBuildCatalog.Data.traits)
        {
            OpenTraitGuide(trait.id);yield return new WaitForEndOfFrame();CaptureRuntime("synergy-"+trait.id);
        }
        traitFocus=null;
        recipeFocus=1;showRecipeGuide=true;recipeGuideUntil=Time.unscaledTime+60;
        yield return new WaitForEndOfFrame();CaptureRuntime("09-recipes");showRecipeGuide=false;
        OpenEquipmentGuide(8);yield return new WaitForEndOfFrame();CaptureRuntime("26-shield-guide");
        recipeFocus=14;yield return new WaitForEndOfFrame();CaptureRuntime("27-extractor-guide");showRecipeGuide=false;
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
        validationIconGallery=validationEquipmentGallery=true;yield return new WaitForEndOfFrame();CaptureRuntime("28-all-equipment");validationIconGallery=validationEquipmentGallery=false;
        showTeamPlan=true;yield return new WaitForEndOfFrame();CaptureRuntime("31-team-planner");showTeamPlan=false;
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
        inspectedUnit=board[16];yield return new WaitForSeconds(.5f);yield return new WaitForEndOfFrame();CaptureRuntime("32-combat-state");
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
        Require(Time.unscaledTime-finisherStarted<2.2f,"round settlement starts finisher");
        yield return new WaitForSeconds(.8f);yield return new WaitForEndOfFrame();CaptureRuntime("29-round-finisher");
        Require(arena.EffectCount>0,"finisher renders in actual solo arena");
        lobby=true;tamerLoadout=new TamerLoadout{tamer=1,field=2,finisher=2};legend=1;
        yield return new WaitForSeconds(.5f);yield return new WaitForEndOfFrame();CaptureRuntime("30-solo-loadout-lobby");
        lobby=false;
        artPack=1;EnsureArena();Require(SoloArenaViewport==TacticalArena.SoloViewport,"original layout retained");
        yield return new WaitForEndOfFrame();CaptureRuntime("07-original");
        Debug.Log("ARENA SMOKE COMPLETE: "+validationChecks+" checks");Application.Quit();
    }
}
#endif
