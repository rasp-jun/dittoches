using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    private readonly Dictionary<Unit,Vector3> formationPositions=new Dictionary<Unit,Vector3>();
    private readonly Dictionary<Fighter,float> healthTrails=new Dictionary<Fighter,float>();
    private readonly List<Fighter> lastBattleReport=new List<Fighter>();
    private bool showCombatReport=true;
    private int reportMetric;
    private string lastReportRound="";
    private string placementNotice="";
    private float placementNoticeUntil;

    private int traitPage,inventoryPage;
    private string ShopUnitSummary(UnitDef d,int singles,int doubles)
    {
        UnitMeta meta=Meta(d.id);
        int triples=FindUnits(d.id,3).Count;
        string owned="보유  ★ "+singles+"  /  ★★ "+doubles+"  /  ★★★ "+triples;
        string[] categories={"속성","계열","역할"},keys={meta.attr,meta.family,d.role};
        string summary=UnitName(d)+" · "+d.cost+"G\n"+owned;
        bool distinct=!board.Any(u=>u!=null&&u.def.id==d.id);
        for(int i=0;i<keys.Length;i++)
        {
            int count=TraitCount(categories[i],keys[i]);
            bool activates=distinct&&TraitTier(categories[i],count+1)>TraitTier(categories[i],count);
            summary+="\n"+keys[i]+"  "+count+"/"+TraitTarget(categories[i],count)+(activates?" · 추가 배치 시 다음 단계":"");
        }
        return summary;
    }
    private bool PointerOverGuide(Vector2 point)
    {
        return (showRecipeGuide&&Time.unscaledTime<recipeGuideUntil&&new Rect(270,470,470,recipeFocus<=3?270:150).Contains(point))
            ||(traitFocus!=null&&Time.unscaledTime<traitGuideUntil&&new Rect(270,145,475,155).Contains(point));
    }
    private Unit HeldUnit()
    {
        if(draggingUnit&&dragSource>=0)return dragFromBoard?board[dragSource]:bench[dragSource];
        return selectedBench>=0?bench[selectedBench]:selectedBoard>=0?board[selectedBoard]:null;
    }
    private bool ValidBoardDestination(int cell)
    {
        if(cell<28||cell>=56)return false;
        bool fromBoard=draggingUnit?dragFromBoard:selectedBoard>=0;
        return fromBoard||board[cell-28]!=null||board.Count(u=>u!=null)<level;
    }
    private void NotifyPlacement(string text)
    {
        placementNotice=text;placementNoticeUntil=Time.unscaledTime+2.2f;
    }
    private void RenderFormationPiece(Unit unit,Vector3 target,Color color,float scale=1f)
    {
        Vector3 displayed;
        if(!formationPositions.TryGetValue(unit,out displayed))displayed=target;
        bool held=draggingUnit&&unit==HeldUnit();
        if(held)
        {
            Vector2 mouse=Event.current.mousePosition;
            int seat=arena.HitBench(mouse),cell=arena.HitCell(mouse);
            bool valid=seat>=0||ValidBoardDestination(cell);
            if(seat>=0)target=TacticalArena.BenchWorld(seat);
            else if(cell>=0)target=TacticalArena.CellWorld(cell%7,cell/7);
            else {Vector3 ground;if(arena.GroundPoint(mouse,out ground)&&TacticalArena.SoloViewport.Contains(mouse))target=ground;}
            target+=Vector3.up*.35f;
            color=valid?new Color(.3f,1f,.75f):new Color(1f,.3f,.24f);
            arena.HighlightDestination(cell,seat,valid);
            scale=1.08f;
        }
        displayed=Vector3.Lerp(displayed,target,1-Mathf.Exp(-Time.unscaledDeltaTime*(held?24f:15f)));
        formationPositions[unit]=displayed;
        arena.SetActor(unit,displayed,Tex(UnitSprite(unit.def)),color,scale);
    }
    private void DrawFormationStatus()
    {
        if(showCarousel||hp<=0)return;
        string text;
        if(Time.unscaledTime<placementNoticeUntil)text=placementNotice;
        else if(battling)text="전투 중 · 유닛을 눌러 정보 확인 / 아이템 장착";
        else if(scoutedRival>=0)text="관전 중 · 오른쪽 나의 테이머를 눌러 복귀";
        else if(HeldUnit()!=null)
        {
            int cell=arena.HitCell(Event.current.mousePosition),seat=arena.HitBench(Event.current.mousePosition);
            text=seat>=0?(bench[seat]==null?"대기석으로 이동":"대기석 유닛과 교환")
                :cell>=28?(ValidBoardDestination(cell)?(board[cell-28]==null?"이 위치에 배치":"두 유닛의 위치 교환"):"배치 인원 초과 · 유닛과 교환하거나 레벨을 올리세요")
                :"내 전장의 칸 또는 대기석을 선택하세요";
            text+="  ·  ESC / 우클릭 취소";
        }
        else text="유닛을 끌어서 배치 · 클릭 후 다른 칸을 눌러도 이동합니다";
        DrawRect(new Rect(330,158,972,32),new Color(.01f,.025f,.045f,.92f));
        GUI.Label(new Rect(340,161,952,26),text,center);
    }
    private void DrawRoundStatus()
    {
        int steps=round<=3?3:7,step=round<=3?round:1+(round-4)%7;
        string phase=showCarousel?"보상 선택":battling?"전투":"준비";
        GUI.Box(new Rect(785,8,350,76),GUIContent.none,selectedStyle);
        GUI.Label(new Rect(795,12,330,24),RoundLabel()+"  ·  "+RoundType()+"  ·  "+phase,center);
        float width=302f/steps;
        for(int i=0;i<steps;i++)
        {
            Color color=i<step?accent:new Color(.12f,.21f,.27f);
            if(i==step-1)color=battling?new Color(1f,.48f,.27f):new Color(.35f,.94f,.8f);
            DrawRect(new Rect(809+i*width,45,width-5,5),color);
        }
        GUI.Label(new Rect(795,55,330,23),battling?Mathf.CeilToInt(battleTimeRemaining)+"초 남음":"배치 "+board.Count(u=>u!=null)+" / "+level,center);
    }
    private float ReportValue(Fighter f){return reportMetric==1?f.healingDone:reportMetric==2?f.shieldingDone:f.damageDone;}
    private bool DrawCombatReportPanel()
    {
        if(inspectedUnit!=null||!showCombatReport)return false;
        var source=battling?fighters:lastBattleReport;
        Fighter[] rows=source.Where(f=>!f.enemy).OrderByDescending(ReportValue).ToArray();
        if(rows.Length==0)return false;
        DrawRect(new Rect(1368,108,532,620),new Color(panel.r,panel.g,panel.b,.96f));
        GUI.Label(new Rect(1390,124,470,30),battling?"실시간 전투 기록 · "+lastReportRound:"지난 전투 기록 · "+lastReportRound,header);
        string[] tabs={"피해량","회복량","보호막"};
        for(int i=0;i<3;i++)
        {
            Rect tab=new Rect(1390+i*160,165,150,30);
            if(Btn(tab,tabs[i]))reportMetric=i;
            if(reportMetric==i)DrawRect(new Rect(tab.x,tab.yMax+2,tab.width,3),accent);
        }
        float max=Mathf.Max(1,rows.Max(ReportValue));
        for(int i=0;i<rows.Length&&i<9;i++)
        {
            Fighter f=rows[i];float y=215+i*47;
            Portrait(new Rect(1390,y,38,38),UnitSprite(f.unit.def));
            GUI.Label(new Rect(1438,y,290,23),UnitName(f.unit.def)+(f.dead?" · 전투 불능":""),label);
            GUI.Label(new Rect(1740,y,120,23),Mathf.RoundToInt(ReportValue(f)).ToString(),center);
            MiniBar(new Rect(1438,y+27,415,5),ReportValue(f)/max,reportMetric==1?new Color(.3f,.9f,.55f):reportMetric==2?new Color(.3f,.7f,1f):RoleColor(f.unit.def.role));
            if(GUI.Button(new Rect(1385,y,485,40),GUIContent.none,GUIStyle.none))inspectedUnit=f.unit;
        }
        GUI.Label(new Rect(1390,645,470,24),"총합 "+Mathf.RoundToInt(rows.Sum(ReportValue))+"  ·  유닛을 눌러 정보 확인",small);
        if(Btn(new Rect(1390,680,220,32),"테이머 목록 보기"))showCombatReport=false;
        DrawSellControl();
        return true;
    }
    private void DrawSellControl()
    {
        bool enabled=!showCarousel&&(selectedBench>=0||(!battling&&selectedBoard>=0));
        if(Btn(new Rect(1645,674,225,42),"선택 유닛 판매",enabled))SellSelectedUnit();
    }
    private void DrawReportToggle()
    {
        if(lastBattleReport.Count==0&&!battling)return;
        if(Btn(new Rect(1390,744,470,38),showCombatReport&&inspectedUnit==null?"테이머 목록으로 전환":"전투 기록 보기"))
        {if(inspectedUnit!=null){inspectedUnit=null;showCombatReport=true;}else showCombatReport=!showCombatReport;}
    }
}
