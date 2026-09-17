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
    private Rect SellDropZone { get { return artPack==0?new Rect(1699,767,192,42):new Rect(1380,672,500,48); } }
    private int UnitSaleValue(Unit unit){return unit==null?0:unit.def.cost*(int)Mathf.Pow(3,unit.star-1);}
    private void RerollShop()
    {
        if(showCarousel||hp<=0||gold<2)return;
        gold-=2;RollShop();Save();
    }
    private void PurchaseExperience()
    {
        if(showCarousel||hp<=0||gold<4||level>=9)return;
        int previous=level;gold-=4;AddXp(4);Save();
        if(level>previous)NotifyPlacement("레벨 "+level+" · 배치 한도가 늘어났습니다");
    }
    private void DrawDragSaleTarget()
    {
        if(!draggingUnit||HeldUnit()==null||showCarousel)return;
        bool hovered=SellDropZone.Contains(Event.current.mousePosition);
        DrawRect(SellDropZone,hovered?new Color(.42f,.16f,.12f):new Color(.12f,.075f,.05f));
        DrawRect(new Rect(SellDropZone.x,SellDropZone.y,SellDropZone.width,3),hovered?new Color(1f,.5f,.3f):accent);
        GUI.Label(SellDropZone,"여기에 놓아 판매  ·  "+UnitSaleValue(HeldUnit())+" G",center);
    }
    private readonly Dictionary<Unit,float> promotions=new Dictionary<Unit,float>();
    private readonly List<BattleTrace> battleTraces=new List<BattleTrace>();
    private sealed class BattleTrace
    {
        public Vector3 from,to;
        public Color color;
        public float started;
    }
    private void NotifyPromotion(Unit unit)
    {
        promotions[unit]=Time.unscaledTime+1.25f;
        NotifyPlacement(UnitName(unit.def)+" · "+new string('★',unit.star)+" 합성 완료");
    }
    private void RecordAttackTrace(Fighter source,Fighter target)
    {
        if(Meta(source.unit.def.id).range<=1)return;
        if(battleTraces.Count>=80)battleTraces.RemoveAt(0);
        battleTraces.Add(new BattleTrace{from=TacticalArena.CellWorld(source.renderPos.x,source.renderPos.y)+Vector3.up*.7f,
            to=TacticalArena.CellWorld(target.renderPos.x,target.renderPos.y)+Vector3.up*.7f,
            color=RoleColor(source.unit.def.role),started=Time.unscaledTime});
    }
    private void DrawAttackTraces()
    {
        if(Event.current.type!=EventType.Repaint)return;
        for(int i=battleTraces.Count-1;i>=0;i--)
        {
            BattleTrace trace=battleTraces[i];float progress=(Time.unscaledTime-trace.started)/.24f;
            if(progress>=1){battleTraces.RemoveAt(i);continue;}
            Vector2 from=arena.Project(trace.from),to=arena.Project(trace.to);
            for(int tail=3;tail>=0;tail--)
            {
                Vector2 point=Vector2.Lerp(from,to,Mathf.Clamp01(progress-tail*.065f));
                Color color=trace.color;color.a=1-tail*.22f;
                float size=tail==0?7:4;
                DrawRect(new Rect(point.x-size*.5f,point.y-size*.5f,size,size),color);
            }
        }
    }

    private int traitPage,inventoryPage;
    private string ShopUnitSummary(UnitDef d,int singles,int doubles)
    {
        UnitMeta meta=Meta(d.id);
        int triples=FindUnits(d.id,3).Count;
        string owned="보유  ★ "+singles+"  /  ★★ "+doubles+"  /  ★★★ "+triples;
        string[] categories={"속성","계열","역할"},keys={meta.attr,meta.family,d.role};
        if(artPack==0){var tags=DigimonBuildCatalog.ForUnit(d.id).ToArray();categories=tags.Select(t=>t.category).ToArray();keys=tags.Select(t=>t.name).ToArray();}
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
        return (showRecipeGuide&&Time.unscaledTime<recipeGuideUntil&&RecipeGuideRect.Contains(point))
            ||(traitFocus!=null&&Time.unscaledTime<traitGuideUntil&&TraitGuideRect.Contains(point));
    }
    private Rect FormationPieceRect(Unit unit,Vector3 ground,float width)
    {
        Vector3 displayed;if(formationPositions.TryGetValue(unit,out displayed))ground=displayed;
        Vector2 head=arena.Project(ground+Vector3.up*(artPack==0?TacticalArena.DigimonHeadHeight(unit.def.id,unit.star):1.3f)),feet=arena.Project(ground);
        return new Rect(feet.x-width*.5f,head.y,width,Mathf.Max(24,feet.y-head.y+8));
    }
    private void DrawFormationLabel(Unit unit,Vector3 ground,bool focused)
    {
        Vector2 point=arena.Project(ground);
        GUI.Label(new Rect(point.x-45,point.y+2,90,20),new string('★',unit.star),center);
        if(!focused)return;
        Rect nameplate=new Rect(Mathf.Clamp(point.x-90,286,1166),point.y+22,180,unit.items.Count>0?44:25);
        DrawRect(nameplate,new Color(.008f,.022f,.038f,.93f));
        DrawRect(new Rect(nameplate.x,nameplate.y,nameplate.width,2),CostColor(unit.def.cost));
        GUI.Label(new Rect(nameplate.x+4,nameplate.y+2,172,23),UnitName(unit.def),center);
        if(unit.items.Count>0)GUI.Label(new Rect(nameplate.x+4,nameplate.y+24,172,20),string.Join("  ",unit.items.Select(i=>ItemIcons[i]).ToArray()),center);
    }
    private int PickFormationTarget(Vector2 point)
    {
        if(!SoloArenaViewport.Contains(point))return -1;
        if(!draggingUnit)
        {
            // Nearer portraits take precedence where silhouettes overlap.
            for(int i=bench.Length-1;i>=0;i--)
                if(bench[i]!=null&&FormationPieceRect(bench[i],TacticalArena.BenchWorld(i),62).Contains(point))return 56+i;
            if(!battling)
            {
                Unit[] visible=scoutedRival>=0?scoutBoard:board;
                for(int i=visible.Length-1;i>=0;i--)
                {
                    if(visible[i]==null)continue;
                    float width=Mathf.Clamp(arena.CellRect(i/7+4,i%7).width*.72f,44,78);
                    if(FormationPieceRect(visible[i],TacticalArena.CellWorld(i%7,i/7+4),width).Contains(point))return i+28;
                }
            }
        }
        int seat=arena.HitBench(point);
        return seat>=0?56+seat:arena.HitCell(point);
    }
    private Unit HeldUnit()
    {
        if(draggingUnit&&dragSource>=0)return dragFromBoard?board[dragSource]:bench[dragSource];
        return selectedBench>=0?bench[selectedBench]:selectedBoard>=0?board[selectedBoard]:null;
    }
    private bool CanEquipSelected(Unit unit)
    {
        if(unit==null||selectedItem<0||selectedItem>=inventory.Count)return false;
        int item=inventory[selectedItem];
        if(item==14)return unit.items.Count>0;
        return (item<=3&&unit.items.Any(i=>i<=3))||unit.items.Count<2;
    }
    private string EquipmentHint()
    {
        if(selectedItem<0||selectedItem>=inventory.Count)return "";
        int hit=PickFormationTarget(Event.current.mousePosition);
        Unit target=hit>=56?bench[hit-56]:!battling&&scoutedRival<0&&hit>=28?board[hit-28]:null;
        if(battling&&target==null)
        {
            foreach(Fighter fighter in fighters.Where(f=>!f.dead).OrderByDescending(f=>f.renderPos.y))
            {
                Vector2 head=arena.Project(FighterWorld(fighter)+Vector3.up*(artPack==0?TacticalArena.DigimonHeadHeight(fighter.unit.def.id,fighter.unit.star):1.4f)),feet=arena.Project(FighterWorld(fighter));
                if(!new Rect(feet.x-40,head.y,80,Mathf.Max(24,feet.y-head.y)).Contains(Event.current.mousePosition))continue;
                if(fighter.enemy)return "상대 유닛에는 장비를 장착할 수 없습니다";
                target=fighter.unit;break;
            }
        }
        int item=inventory[selectedItem];
        if(target==null)return ItemNames[item]+" · 장착할 아군 유닛을 선택하세요";
        if(artPack==0&&battling&&board.Contains(target))return "전장 장비 변경은 준비 단계에 가능합니다";
        if(item==14)return target.items.Count>0?UnitName(target.def)+" · 장비 "+target.items.Count+"개 회수":"회수할 장비가 없는 유닛입니다";
        int partner=target.items.FindIndex(i=>i<=3);
        if(item<=3&&partner>=0)return UnitName(target.def)+" · "+ItemNames[ItemRecipes[target.items[partner],item]]+" 자동 합성";
        return target.items.Count>=2?"장비 슬롯이 가득 찼습니다 · 재료 합성 또는 자석 제거기를 이용하세요":UnitName(target.def)+" · "+ItemNames[item]+" 장착";
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
            else {Vector3 ground;if(arena.GroundPoint(mouse,out ground)&&SoloArenaViewport.Contains(mouse))target=ground;}
            target+=Vector3.up*.35f;
            color=valid?new Color(.3f,1f,.75f):new Color(1f,.3f,.24f);
            arena.HighlightDestination(cell,seat,valid);
            scale=1.08f;
        }
        displayed=Vector3.Lerp(displayed,target,1-Mathf.Exp(-Time.unscaledDeltaTime*(held?24f:15f)));
        formationPositions[unit]=displayed;
        float until;float promotion=promotions.TryGetValue(unit,out until)?Mathf.Clamp01((until-Time.unscaledTime)/1.25f):0;
        scale*=1+Mathf.Sin((1-promotion)*Mathf.PI)*.12f*promotion;
        arena.SetActor(unit,displayed,Tex(UnitSprite(unit.def)),color,scale);
        if(artPack==0)arena.PoseDigimon(unit,unit.def.id,Vector3.Distance(displayed,target)*3f,0,Time.unscaledTime,-1,0,0,unit.star);
        bool selected=held||(!battling&&(selectedBoard>=0&&board[selectedBoard]==unit||selectedBench>=0&&bench[selectedBench]==unit));
        bool gearTarget=selectedItem>=0&&scoutedRival<0&&(board.Contains(unit)||bench.Contains(unit));
        arena.DecorateActor(unit,selected||gearTarget,promotion,invalid:gearTarget&&!CanEquipSelected(unit));
    }
    private void DrawFormationStatus()
    {
        if(showCarousel||hp<=0)return;
        string text;
        if(Time.unscaledTime<placementNoticeUntil)text=placementNotice;
        else if(draggingUnit&&SellDropZone.Contains(Event.current.mousePosition))text=UnitName(HeldUnit().def)+" · 놓으면 "+UnitSaleValue(HeldUnit())+"G에 판매합니다";
        else if(selectedItem>=0)text=EquipmentHint();
        else if(battling)text=artPack==0?"전투 중 · 유닛을 눌러 정보 확인 / 전장 장비는 준비 단계에 변경":"전투 중 · 유닛을 눌러 정보 확인 / 아이템 장착";
        else if(scoutedRival>=0)text="관전 중 · 오른쪽 나의 테이머를 눌러 복귀";
        else if(HeldUnit()!=null)
        {
            int cell=arena.HitCell(Event.current.mousePosition),seat=arena.HitBench(Event.current.mousePosition);
            text=seat>=0?(bench[seat]==null?"대기석으로 이동":"대기석 유닛과 교환")
                :cell>=28?(ValidBoardDestination(cell)?(board[cell-28]==null?"이 위치에 배치":"두 유닛의 위치 교환"):"배치 인원 초과 · 유닛과 교환하거나 레벨을 올리세요")
                :"내 전장의 칸 또는 대기석을 선택하세요";
            text+="  ·  ESC / 우클릭 취소";
        }
        else text=artPack==0?"드래그하여 배치   ·   우클릭 / ESC로 선택 취소":"유닛을 끌어서 배치 · 클릭 후 다른 칸을 눌러도 이동합니다";
        bool focused=HeldUnit()!=null||selectedItem>=0||scoutedRival>=0||Time.unscaledTime<placementNoticeUntil;
        float width=artPack==0&&!focused?650:972;
        float y=artPack==0?803:158,x=SoloArenaViewport.center.x-width*.5f;
        DrawRect(new Rect(x,y,width,32),new Color(.01f,.025f,.045f,artPack==0&&!focused?.45f:.92f));
        GUI.Label(new Rect(x+10,y+3,width-20,26),text,center);
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
