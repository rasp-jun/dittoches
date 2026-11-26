using System;
using System.Linq;
using UnityEngine;

public sealed partial class TeamPlannerUI
{
    readonly ArenaInterface ui=new ArenaInterface();
    GUIStyle small,input,caption;
    string query="",undo="",notice="변경 사항은 자동 저장됩니다",inspectionId="",traitFocus="";
    int cost;
    Vector2 synergyScroll,libraryScroll;
    public float LibraryScrollY {get{return libraryScroll.y;}}
    public string InspectionId {get{return inspectionId;}}
    public string TraitFocus {get{return traitFocus;}}
    public float SynergyScrollY {get{return synergyScroll.y;}}
    public void DismissInspection(){inspectionId="";traitFocus="";}
    public static Rect InspectionRect(Rect canvas){return new Rect(canvas.center.x-325,canvas.center.y-385,650,770);}
    void Inspect(string id){DismissInspection();inspectionId=id;GUI.FocusControl(null);}
    public string Query {get{return query;}set{query=value??"";libraryScroll=Vector2.zero;}}
    public int Cost {get{return cost;}set{cost=Mathf.Clamp(value,0,5);libraryScroll=Vector2.zero;}}
    public static Rect Bounds(Rect canvas){return new Rect(canvas.center.x-730,canvas.center.y-430,1460,860);}
    public static Rect PlanRect(Rect r,int i){return new Rect(r.x+350+i*146,r.y+30,134,42);}
    public static Rect GoalRect(Rect r,int i){return new Rect(r.x+24+i*158,r.y+121,148,102);}
    public static Rect CardRect(Rect r,int i){return new Rect(r.x+24+i%5*212,r.y+315+i/5*70,202,62);}
    public static Rect LibraryViewport(Rect r){return new Rect(r.x+24,r.y+315,1072,420);}
    public static Rect SynergyViewport(Rect r){return new Rect(r.x+1112,r.y+370,312,349);}
    public static DigimonBuildCatalog.Trait[] ForecastTraits(){return DigimonBuildCatalog.Data.traits.Where(t=>TeamPlan.Units.Any(id=>t.members.Contains(id))).OrderByDescending(t=>t.Level(DigimonBuildCatalog.Count(t,TeamPlan.Units))).ThenByDescending(t=>DigimonBuildCatalog.Count(t,TeamPlan.Units)).ToArray();}
    public static Rect ActionRect(Rect r,int i){return new Rect(r.x+24+i*164,r.yMax-64,154,38);}
    public static Rect CloseRect(Rect r){return new Rect(r.xMax-128,r.y+30,104,42);}
    void Remember(){undo=JsonUtility.ToJson(TeamPlan.Data);}
    void Toggle(string id)
    {
        if(!TeamPlan.Contains(id)&&TeamPlan.Units.Count>=9){notice="목표가 가득 찼습니다 · 한 종을 해제한 뒤 선택하세요";return;}
        Remember();TeamPlan.Toggle(id);notice="플랜 "+(TeamPlan.Data.active+1)+" 저장 완료";
    }
    static void HandleWheel(Rect viewport,float contentHeight,ref Vector2 scroll)
    {
        float overflow=Mathf.Max(0,contentHeight-viewport.height);
        scroll.x=0;scroll.y=Mathf.Clamp(scroll.y,0,overflow);
        Event e=Event.current;
        if(!GUI.enabled||e.type!=EventType.ScrollWheel||overflow<=0||!viewport.Contains(e.mousePosition)||Mathf.Approximately(e.delta.y,0))return;
        scroll.y=Mathf.Clamp(scroll.y+e.delta.y*20f,0,overflow);e.Use();
    }
    static Rect VisiblePart(Rect card,Rect viewport)
    {
        float left=Mathf.Max(card.x,viewport.x),top=Mathf.Max(card.y,viewport.y);
        return new Rect(left,top,Mathf.Max(0,Mathf.Min(card.xMax,viewport.xMax)-left),Mathf.Max(0,Mathf.Min(card.yMax,viewport.yMax)-top));
    }
    public bool Draw(Rect canvas,string[] board,string[] bench,int level,bool inMatch,DigimonBuildCatalog.Member[] members=null,StarCollection collection=null)
    {
        if(small==null)
        {
            small=new GUIStyle(GUI.skin.label){fontSize=12,wordWrap=true};small.normal.textColor=BattleHudTheme.Muted;
            caption=new GUIStyle(small){fontSize=11};
            input=new GUIStyle(GUI.skin.textField){fontSize=16,padding=new RectOffset(13,13,8,8)};input.normal.textColor=BattleHudTheme.Text;
        }
        if(collectionOpen&&inMatch&&collection!=null)return DrawCollection(canvas,collection);
        collectionOpen=false;
        bool close=false;Event e=Event.current;
        bool inspecting=inspectionId.Length>0||traitFocus.Length>0,previousEnabled=GUI.enabled;
        Vector2 detailPointer=e.mousePosition;
        Rect detail=traitFocus.Length>0?DigimonTraitUI.GuideBounds(canvas):InspectionRect(canvas);
        Rect detailClose=traitFocus.Length>0?DigimonTraitUI.GuideCloseRect(canvas):DigimonSkillUI.DetailCloseRect(detail);
        if(inspecting&&(e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape||e.type==EventType.MouseDown&&(!detail.Contains(e.mousePosition)||e.button==0&&detailClose.Contains(e.mousePosition))))
        {DismissInspection();GUI.FocusControl(null);e.Use();return true;}
        if(e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape){GUI.FocusControl(null);e.Use();return false;}
        if(inspecting)
        {
            // Preserve input for the child guide (including its own scroll view),
            // while preventing every parent control from receiving that event.
            if(e.isKey||e.isMouse||e.type==EventType.ScrollWheel)Event.current=new Event(e){type=EventType.Ignore};
            GUI.enabled=false;
        }
        ArenaInterface.Fill(canvas,new Color(.008f,.015f,.025f,.91f));
        Rect r=Bounds(canvas);BattleHudTheme.Surface(r,false,BattleHudTheme.Teal);
        BattleHudTheme.Label(new Rect(r.x+25,r.y+21,280,16),"TEAM PLANNER",11,BattleHudTheme.Teal,true);
        BattleHudTheme.Label(new Rect(r.x+24,r.y+43,300,39),"승리를 먼저 설계하세요",25,BattleHudTheme.Text,true);
        for(int i=0;i<3;i++)if(ui.Button(PlanRect(r,i),new GUIContent("플랜 0"+(i+1)+"   ·   "+TeamPlan.Data.plans[i].units.Count),true,TeamPlan.Data.active==i))
        {Remember();TeamPlan.Data.active=i;TeamPlan.Save();synergyScroll=Vector2.zero;notice="상점 목표를 플랜 "+(i+1)+"로 변경했습니다";GUI.FocusControl(null);}
        BattleHudTheme.Badge(new Rect(r.xMax-363,r.y+36,208,30),inMatch?"진행 중 · 전투는 계속됩니다":"세 개의 계획 · 언제든 전환",inMatch?BattleHudTheme.Gold:BattleHudTheme.Muted);
        if(ui.Button(CollectionButton(r),new GUIContent("강화 현황  ↗","보유 성급 · 한 개면 합성 · 상점 등장 현황"),inMatch&&collection!=null,false,15))
        {collectionOpen=true;GUI.FocusControl(null);e.Use();}
        if(ui.Button(CloseRect(r),new GUIContent("닫기  ESC")))close=true;
        ArenaInterface.Fill(new Rect(r.x+24,r.y+94,r.width-48,1),BattleHudTheme.Line);
        var ids=TeamPlan.Units.ToArray();
        BattleHudTheme.Label(new Rect(r.x+24,r.y+98,350,22),"목표 편성  /  "+ids.Length.ToString("00")+" OF 09",11,BattleHudTheme.Muted,true);
        BattleHudTheme.Label(new Rect(r.xMax-500,r.y+98,475,22),inMatch?TeamPlan.Summary(board.Concat(bench))+"  ·  현재 배치 한도 "+level+"명":"디지몬을 선택해 나만의 조합을 완성하세요",12,BattleHudTheme.Muted,false,TextAnchor.MiddleRight);
        for(int i=0;i<9;i++)
        {
            Rect slot=GoalRect(r,i);bool filled=i<ids.Length;
            BattleHudTheme.Round(slot,filled?BattleHudTheme.Raised:Color.Lerp(BattleHudTheme.Panel,BattleHudTheme.Background,.5f));
            if(!filled)
            {
                BattleHudTheme.Label(new Rect(slot.x,slot.y+17,slot.width,37),"+",28,BattleHudTheme.Line,false,TextAnchor.MiddleCenter);
                BattleHudTheme.Label(new Rect(slot.x,slot.y+59,slot.width,25),"목표 "+(i+1).ToString("00"),11,BattleHudTheme.Muted,false,TextAnchor.MiddleCenter);continue;
            }
            string id=ids[i];var def=TeamPlan.Roster.First(u=>u.id==id);Color rarity=ArenaInterface.Rarity(def.cost);
            FaithfulPortraits.Draw(new Rect(slot.x+40,slot.y+3,68,58),id,true);
            BattleHudTheme.Badge(new Rect(slot.x+7,slot.y+7,28,20),def.cost.ToString(),rarity,11);
            BattleHudTheme.Label(new Rect(slot.x+9,slot.y+60,slot.width-18,21),def.name,12,BattleHudTheme.Text,true,TextAnchor.MiddleCenter);
            BattleHudTheme.Label(new Rect(slot.x+9,slot.y+82,slot.width-18,16),inMatch?TeamPlan.Progress(id,board,bench):"선택 해제 ×",10,inMatch&&board.Contains(id)?BattleHudTheme.Teal:BattleHudTheme.Muted,false,TextAnchor.MiddleCenter);
            if(GUI.enabled&&DigimonSkillUI.WantsDetails(slot,e)){Inspect(id);e.Use();}
            if(GUI.Button(slot,new GUIContent("",def.name+" · 클릭: 목표 해제 · 우클릭: 성급/스킬 비교"),GUIStyle.none))Toggle(id);
        }
        BattleHudTheme.Label(new Rect(r.x+24,r.y+241,1050,18),"디지몬 라이브러리",13,BattleHudTheme.Text,true);
        string previousQuery=query;query=SeasonLobbyArt.Input(new Rect(r.x+24,r.y+268,279,36),"team-plan-search",query,30,input,"이름 또는 시너지로 검색");
        if(query!=previousQuery)libraryScroll=Vector2.zero;
        for(int i=0;i<=5;i++)if(ui.Button(new Rect(r.x+321+i*87,r.y+268,77,36),new GUIContent(i==0?"전체":i+" G"),true,cost==i)){cost=i;libraryScroll=Vector2.zero;GUI.FocusControl(null);}
        if(ui.Button(new Rect(r.x+858,r.y+268,202,36),new GUIContent("필터 초기화"))){query="";cost=0;libraryScroll=Vector2.zero;GUI.FocusControl(null);}
        var matches=TeamPlan.Search(query,cost);
        BattleHudTheme.Label(new Rect(r.x+650,r.y+239,410,22),matches.Length+"종 · 클릭: 목표 선택 · 우클릭: 성급/스킬 정보",11,BattleHudTheme.Muted,false,TextAnchor.MiddleRight);
        // Keep root-canvas controls outside the scroll-view event transform.
        ArenaInterface.Fill(new Rect(r.x+24,r.yMax-93,r.width-48,1),BattleHudTheme.Line);
        if(ui.Button(ActionRect(r,0),new GUIContent("플랜 비우기"),ids.Length>0)){Remember();TeamPlan.Units.Clear();TeamPlan.Save();notice="플랜을 비웠습니다 · 이전 변경 취소로 복구할 수 있습니다";}
        if(ui.Button(ActionRect(r,1),new GUIContent("이전 변경 취소"),undo.Length>0)){TeamPlan.Restore(undo);undo="";notice="이전 계획을 복구했습니다";}
        if(ui.Button(ActionRect(r,2),new GUIContent("현재 전장 담기","배치한 디지몬을 저장합니다. 같은 디지몬과 대기석은 중복 포함하지 않습니다."),inMatch&&board.Length>0))
        {Remember();TeamPlan.Capture(board);notice="현재 전장을 팀 계획에 저장했습니다";}
        BattleHudTheme.Label(new Rect(r.x+541,r.yMax-70,r.width-565,24),notice,13,BattleHudTheme.Teal,true);
        BattleHudTheme.Label(new Rect(r.x+541,r.yMax-42,r.width-565,19),"자동 저장 · 상점에서 목표를 표시합니다 · 실제 편성과 보유 유닛은 유지됩니다",12,BattleHudTheme.Muted);
        Rect libraryViewport=LibraryViewport(r);
        float libraryHeight=Mathf.Max(420,Mathf.CeilToInt(matches.Length/5f)*70-8);
        HandleWheel(libraryViewport,libraryHeight,ref libraryScroll);
        // Resolve card input in the same canvas space as tabs and the footer.
        // BeginScrollView clips the artwork only; synthetic and native pointer
        // events therefore share one hit-test path, with hidden card parts excluded.
        var libraryHovers=new float[matches.Length];
        for(int i=0;i<matches.Length;i++)
        {
            Rect card=CardRect(r,i);card.y-=libraryScroll.y;
            Rect hit=VisiblePart(card,libraryViewport);
            libraryHovers[i]=ui.HoverAmount(card,GUI.enabled&&libraryViewport.Contains(e.mousePosition));
            if(hit.width>0&&hit.height>0)
            {
                if(GUI.enabled&&DigimonSkillUI.WantsDetails(hit,e)){Inspect(matches[i].id);e.Use();}
                if(GUI.Button(hit,new GUIContent("","클릭: 목표 선택 · 우클릭: 성급/스킬 비교"),GUIStyle.none))Toggle(matches[i].id);
            }
        }
        var traits=ForecastTraits();Rect viewport=SynergyViewport(r);
        bool scrolling=traits.Length*48>viewport.height;
        HandleWheel(viewport,traits.Length*48,ref synergyScroll);
        float rowWidth=viewport.width-(scrolling?18:0);
        // Resolve both scroll panes' controls before either clip can transform
        // the event pointer. This also keeps a scrolled row's hit area clipped.
        for(int i=0;i<traits.Length;i++)
        {
            Rect hit=VisiblePart(new Rect(viewport.x,viewport.y+i*48-synergyScroll.y,rowWidth,43),viewport);
            if(hit.width>0&&hit.height>0&&GUI.Button(hit,new GUIContent("",traits[i].name+" · 클릭: 단계별 효과와 참여 디지몬"),GUIStyle.none))
            {DismissInspection();traitFocus=traits[i].id;GUI.FocusControl(null);}
        }
        libraryScroll=GUI.BeginScrollView(libraryViewport,libraryScroll,new Rect(0,0,1050,libraryHeight),false,libraryHeight>420);
        for(int i=0;i<matches.Length;i++)
        {
            var def=matches[i];Rect card=new Rect(i%5*212,i/5*70,202,62);bool chosen=TeamPlan.Contains(def.id);float hover=libraryHovers[i];
            BattleHudTheme.Round(card,Color.Lerp(chosen?new Color(.067f,.22f,.23f):BattleHudTheme.Raised,BattleHudTheme.Line,hover*.45f));
            BattleHudTheme.Round(new Rect(card.x+2,card.y+10,3,card.height-20),chosen?BattleHudTheme.Teal:ArenaInterface.Rarity(def.cost));
            FaithfulPortraits.Draw(new Rect(card.x+8,card.y+4,49,54),def.id,true);
            BattleHudTheme.FitLabel(new Rect(card.x+64,card.y+5,card.width-91,21),def.name,12,BattleHudTheme.Text);
            BattleHudTheme.Label(new Rect(card.xMax-27,card.y+6,20,19),chosen?"✓":def.cost.ToString(),12,chosen?BattleHudTheme.Teal:BattleHudTheme.Gold,true,TextAnchor.MiddleCenter);
            GUI.Label(new Rect(card.x+64,card.y+28,card.width-70,19),string.Join(" · ",DigimonBuildCatalog.ForUnit(def.id).Select(t=>t.name).ToArray()),caption);
            BattleHudTheme.Label(new Rect(card.x+64,card.y+45,card.width-71,14),chosen?"선택됨":inMatch?TeamPlan.Progress(def.id,board,bench):def.role,10,chosen?BattleHudTheme.Teal:BattleHudTheme.Muted);
        }
        if(matches.Length==0)
        {
            BattleHudTheme.Round(new Rect(0,0,1050,402),BattleHudTheme.Background);
            BattleHudTheme.Label(new Rect(30,150,990,36),"일치하는 디지몬이 없습니다",23,BattleHudTheme.Text,true,TextAnchor.MiddleCenter);
            BattleHudTheme.Label(new Rect(30,194,990,27),"이름이나 시너지를 바꾸거나 필터를 초기화하세요",14,BattleHudTheme.Muted,false,TextAnchor.MiddleCenter);
        }
        GUI.EndScrollView(false);
        Rect side=new Rect(r.x+1100,r.y+244,336,492);BattleHudTheme.Surface(side,true);
        BattleHudTheme.Label(new Rect(side.x+19,side.y+15,298,19),"SYNERGY FORECAST",10,BattleHudTheme.Teal,true);
        BattleHudTheme.Label(new Rect(side.x+19,side.y+40,298,29),"완성될 시너지",21,BattleHudTheme.Text,true);
        GUI.Label(new Rect(side.x+19,side.y+79,298,35),inMatch?"현재 전장 → 목표 전체 배치\n시너지 클릭: 단계별 효과 / 참여 디지몬":"목표 전체 배치 시 효과 · 기본 소속 기준\n시너지 클릭: 단계별 효과 / 참여 디지몬",small);
        synergyScroll=GUI.BeginScrollView(viewport,synergyScroll,new Rect(0,0,rowWidth,Mathf.Max(viewport.height,traits.Length*48)),false,scrolling);
        for(int i=0;i<traits.Length;i++)
        {
            var t=traits[i];int planned=DigimonBuildCatalog.Count(t,TeamPlan.Units),present=members==null?DigimonBuildCatalog.Count(t,board):DigimonBuildCatalog.Count(t,members),tier=t.Level(planned);float y=i*48;
            BattleHudTheme.Round(new Rect(0,y,rowWidth,43),tier>0?new Color(.061f,.179f,.207f):BattleHudTheme.Panel);
            GUI.DrawTexture(new Rect(9,y+9,24,24),DigimonTraitUI.Icon(t.id));
            BattleHudTheme.Label(new Rect(43,y+3,148,22),t.name,12,tier>0?BattleHudTheme.Text:BattleHudTheme.Muted,true);
            BattleHudTheme.Label(new Rect(43,y+24,148,14),(tier>0?tier+"단계 예상":"미활성")+" · 상세 ›",10,tier>0?BattleHudTheme.Teal:BattleHudTheme.Muted);
            BattleHudTheme.Label(new Rect(rowWidth-107,y+3,98,35),(inMatch?present+" → ":"")+planned+" / "+t.Target(planned),12,tier>0?BattleHudTheme.Teal:BattleHudTheme.Muted,true,TextAnchor.MiddleRight);
            string tip=t.name+" · 목표 "+planned+"종\n"+(tier>0?t.tiers[tier-1].text:"아직 활성화되지 않습니다")+"\n"+DigimonTraitUI.Next(t,planned)+" · "+t.description;
            GUI.Label(new Rect(0,y,rowWidth,43),new GUIContent("",tip),GUIStyle.none);
        }
        if(traits.Length==0)GUI.Label(new Rect(14,40,rowWidth-28,100),"목표 디지몬을 선택하면\n시너지 진행도와 완성 단계를\n이곳에서 확인할 수 있습니다.",small);
        GUI.EndScrollView(false);
        GUI.enabled=previousEnabled;
        if(inspecting){Event.current=e;e.mousePosition=detailPointer;}
        if(traitFocus.Length>0)
        {
            if(!DigimonTraitUI.DrawGuide(canvas,ref traitFocus,ids,new string[0],context:"팀 계획 · 목표 전체 배치 예상",plannedFormation:true))DismissInspection();
        }
        else if(inspectionId.Length>0)
        {
            var definition=TeamPlan.Roster.First(u=>u.id==inspectionId);var skill=DigimonSkillCatalog.Find(inspectionId);
            ArenaInterface.Fill(canvas,new Color(.004f,.01f,.02f,.72f));
            var stats=new DigimonSkillCatalog.Stats(skill,1,new DigimonBuildCatalog.Bonus());
            if(!DigimonSkillUI.DrawDetails(InspectionRect(canvas),skill,1,stats,definition.name+" · "+definition.cost+"G · 1성 기본 능력치 (장비·시너지 없음)",
                "팀 계획 조회 · 목표 선택과 실제 편성은 유지됩니다\nESC / 바깥 클릭: 계획으로 돌아가기 · 같은 종 3개로 다음 성급"))DismissInspection();
        }
        else ui.Tooltip(canvas);
        if(e.isMouse||e.isKey)e.Use();return !close;
    }
    public static void ShopMarker(Rect rect,string id,GUIStyle style)
    {
        if(!TeamPlan.Contains(id))return;
        BattleHudTheme.Round(rect,new Color(.055f,.24f,.25f,.97f));GUI.Label(rect,"목표",style);
    }
}
