using System.Linq;
using UnityEngine;

public sealed partial class TeamPlannerUI
{
    bool collectionOpen;
    int collectionFilter;
    string collectionQuery="";
    Vector2 collectionScroll;
    public bool CollectionOpen {get{return collectionOpen;}}
    public int CollectionFilter {get{return collectionFilter;}}
    public float CollectionScrollY {get{return collectionScroll.y;}}
    public string CollectionQuery {get{return collectionQuery;}set{collectionQuery=value??"";collectionScroll=Vector2.zero;}}
    public void ResetView(){DismissInspection();collectionOpen=false;}
    public static Rect CollectionButton(Rect panel){return new Rect(panel.x+798,panel.y+30,178,42);}
    public static Rect CollectionFilterRect(Rect panel,int i){return new Rect(panel.x+24+i*176,panel.y+211,164,36);}
    public static Rect CollectionViewport(Rect panel){return new Rect(panel.x+24,panel.y+263,panel.width-48,470);}
    public static Rect CollectionCard(Rect panel,int i){return new Rect(panel.x+24+i%3*464,panel.y+263+i/3*166,452,154);}
    bool DrawCollection(Rect canvas,StarCollection data)
    {
        Event e=Event.current;bool child=inspectionId.Length>0,enabled=GUI.enabled;Vector2 pointer=e.mousePosition;
        Rect panel=Bounds(canvas),detail=InspectionRect(canvas);
        if(child&&(e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape||e.type==EventType.MouseDown&&(!detail.Contains(pointer)||e.button==0&&DigimonSkillUI.DetailCloseRect(detail).Contains(pointer))))
        {DismissInspection();GUI.FocusControl(null);e.Use();return true;}
        if(!child&&e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape){e.Use();return false;}
        if(child){if(e.isMouse||e.isKey||e.type==EventType.ScrollWheel)Event.current=new Event(e){type=EventType.Ignore};GUI.enabled=false;}
        ArenaInterface.Fill(canvas,new Color(.008f,.015f,.025f,.94f));BattleHudTheme.Surface(panel,false,BattleHudTheme.Teal);
        BattleHudTheme.Label(new Rect(panel.x+25,panel.y+21,580,18),"STAR COLLECTION  /  보유 편성",11,BattleHudTheme.Teal,true);
        BattleHudTheme.Label(new Rect(panel.x+24,panel.y+43,680,40),"성급 강화 현황",27,BattleHudTheme.Text,true);
        if(ui.Button(CollectionButton(panel),new GUIContent("팀 계획으로 돌아가기")))
        {collectionOpen=false;GUI.FocusControl(null);e.Use();return true;}
        bool close=ui.Button(CloseRect(panel),new GUIContent("닫기  ESC"));
        int owned=data.entries.Count(r=>r.Owned>0),pairs=data.entries.Count(r=>r.OneAway),ready=data.entries.Count(r=>r.CanUpgrade),complete=data.entries.Count(r=>r.Complete);
        string[] titles={"한 개 더 있으면 강화","현재 상점에서 강화 가능","3성 완성"};int[] values={pairs,ready,complete};
        for(int i=0;i<3;i++)
        {
            Rect metric=new Rect(panel.x+24+i*470,panel.y+104,458,87);Color tint=i==2?BattleHudTheme.Gold:BattleHudTheme.Teal;
            BattleHudTheme.Surface(metric,true);
            BattleHudTheme.Label(new Rect(metric.x+18,metric.y+13,345,22),titles[i],15,BattleHudTheme.Muted);
            BattleHudTheme.Label(new Rect(metric.x+18,metric.y+39,345,31),values[i]+"종",25,tint,true);
        }
        string[] filters={"전체 보유  "+owned,"한 개면 강화  "+pairs,"상점 등장","계획 대상"};
        for(int i=0;i<filters.Length;i++)if(ui.Button(CollectionFilterRect(panel,i),new GUIContent(filters[i]),true,collectionFilter==i,14))
        {collectionFilter=i;collectionScroll=Vector2.zero;GUI.FocusControl(null);}
        string prior=collectionQuery;
        collectionQuery=SeasonLobbyArt.Input(new Rect(panel.x+744,panel.y+211,437,36),"star-collection-search",collectionQuery,30,input,"이름 또는 시너지로 검색");
        if(prior!=collectionQuery)collectionScroll=Vector2.zero;
        if(ui.Button(new Rect(panel.x+1197,panel.y+211,239,36),new GUIContent("필터 초기화")))
        {collectionQuery="";collectionFilter=0;collectionScroll=Vector2.zero;GUI.FocusControl(null);}
        var rows=data.View(collectionFilter,collectionQuery);Rect viewport=CollectionViewport(panel);
        float height=Mathf.Max(viewport.height,Mathf.CeilToInt(rows.Length/3f)*166-12);HandleWheel(viewport,height,ref collectionScroll);
        // Resolve all input before the scroll view transforms the event pointer.
        for(int i=0;i<rows.Length;i++)
        {
            Rect card=CollectionCard(panel,i);card.y-=collectionScroll.y;Rect hit=VisiblePart(card,viewport);
            if(hit.width>0&&hit.height>0&&GUI.enabled&&DigimonSkillUI.WantsDetails(hit,e)){Inspect(rows[i].id);e.Use();}
        }
        BattleHudTheme.Label(new Rect(panel.x+24,panel.yMax-103,panel.width-48,21),rows.Length+"종 표시 · 우클릭: 성급별 기본 능력치 / 스킬 비교",12,BattleHudTheme.Muted);
        BattleHudTheme.Label(new Rect(panel.x+24,panel.yMax-70,panel.width-48,24),"같은 종·같은 성급 3개 → 다음 성급 · 진행도는 1성 환산 · 전장 우선 합성",15,BattleHudTheme.Text,true);
        BattleHudTheme.Label(new Rect(panel.x+24,panel.yMax-39,panel.width-48,21),data.context+" · 조회 중에도 전투와 제한 시간은 계속됩니다",13,BattleHudTheme.Muted);
        collectionScroll=GUI.BeginScrollView(viewport,collectionScroll,new Rect(0,0,1390,height),false,height>viewport.height);
        for(int i=0;i<rows.Length;i++)
        {
            var row=rows[i];Rect card=new Rect(i%3*464,i/3*166,452,154);
            Color tint=row.Complete||row.OneAway&&row.NextStar==3?BattleHudTheme.Gold:BattleHudTheme.Teal;
            BattleHudTheme.Round(card,row.CanUpgrade?new Color(.06f,.20f,.21f):BattleHudTheme.Raised);
            BattleHudTheme.Round(new Rect(card.x+2,card.y+12,3,card.height-24),row.OneAway||row.Complete?tint:ArenaInterface.Rarity(row.cost));
            FaithfulPortraits.Draw(new Rect(card.x+12,card.y+12,64,76),row.id,true);
            BattleHudTheme.FitLabel(new Rect(card.x+88,card.y+11,card.width-170,24),row.name,18,BattleHudTheme.Text,true);
            BattleHudTheme.Badge(new Rect(card.xMax-68,card.y+12,53,23),row.cost+" G",ArenaInterface.Rarity(row.cost),12);
            BattleHudTheme.Label(new Rect(card.x+88,card.y+39,card.width-104,20),"★ "+row.singles+"   ·   ★★ "+row.doubles+"   ·   ★★★ "+row.triples,13,BattleHudTheme.Muted);
            BattleHudTheme.Label(new Rect(card.x+88,card.y+66,card.width-104,20),row.Progress,14,row.Complete||row.OneAway?tint:BattleHudTheme.Text,true);
            BattleHudTheme.Bar(new Rect(card.x+88,card.y+93,card.width-104,4),Mathf.Clamp01((float)row.Copies/row.Goal),tint);
            BattleHudTheme.Label(new Rect(card.x+15,card.y+106,card.width-30,21),row.Status,13,row.CanUpgrade?tint:row.offers>0&&!row.CanBuy&&!row.Complete?BattleHudTheme.Gold:BattleHudTheme.Muted,true);
            BattleHudTheme.Label(new Rect(card.x+15,card.y+130,card.width-30,18),"전장 "+row.board+" · 대기석 "+row.bench+(row.planned?"   /   팀 계획 목표":""),12,BattleHudTheme.Muted);
        }
        if(rows.Length==0)
        {
            BattleHudTheme.Label(new Rect(24,142,1350,40),owned==0&&collectionFilter==0?"보유한 디지몬이 아직 없습니다":"조건에 맞는 디지몬이 없습니다",25,BattleHudTheme.Text,true,TextAnchor.MiddleCenter);
            BattleHudTheme.Label(new Rect(24,195,1350,28),"계획 대상으로 모집할 기물을 보거나 필터를 초기화하세요",15,BattleHudTheme.Muted,false,TextAnchor.MiddleCenter);
        }
        GUI.EndScrollView(false);GUI.enabled=enabled;
        if(child){Event.current=e;e.mousePosition=pointer;}
        if(inspectionId.Length>0)
        {
            var def=TeamPlan.Roster.First(u=>u.id==inspectionId);var skill=DigimonSkillCatalog.Find(inspectionId);
            ArenaInterface.Fill(canvas,new Color(.004f,.01f,.02f,.72f));
            if(!DigimonSkillUI.DrawDetails(detail,skill,1,new DigimonSkillCatalog.Stats(skill,1,new DigimonBuildCatalog.Bonus()),
                def.name+" · "+def.cost+"G · 1성 기본 능력치 (장비·시너지 없음)","강화 현황 조회 · 보유 기물과 팀 계획은 유지됩니다\nESC / 바깥 클릭: 강화 현황으로 돌아가기"))DismissInspection();
        }
        if(e.isMouse||e.isKey)e.Use();return !close;
    }
}
