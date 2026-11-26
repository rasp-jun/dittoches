using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    Rect SoloArenaViewport { get { return artPack==0?TacticalArena.WideViewport:TacticalArena.SoloViewport; } }
    GUIStyle hudSmall,hudName,hudWrap,hudButton,hudNumber;
    readonly ArenaInterface arenaInterface=new ArenaInterface();
    readonly ArenaInterface rewardInterface=new ArenaInterface();
    void HudStyles()
    {
        if(hudSmall!=null)return;
        hudSmall=new GUIStyle(label){fontSize=13};hudSmall.normal.textColor=BattleHudTheme.Muted;
        hudName=new GUIStyle(header){fontSize=18};hudName.normal.textColor=BattleHudTheme.Text;
        hudWrap=new GUIStyle(hudSmall){wordWrap=true};
        hudButton=new GUIStyle(GUIStyle.none){fontSize=14,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
        hudNumber=new GUIStyle(hudName){fontSize=23,alignment=TextAnchor.MiddleCenter};
    }
    bool HudButton(Rect rect,string text,bool enabled=true,bool selected=false)
    {return HudButton(rect,new GUIContent(text),enabled,selected);}
    bool HudButton(Rect rect,GUIContent content,bool enabled=true,bool selected=false)
    {
        return arenaInterface.Button(rect,content,enabled,selected);
    }
    void HudPanel(Rect rect)
    {BattleHudTheme.Surface(rect);}
    void DrawArenaHudTop()
    {
        HudStyles();DrawRect(new Rect(0,0,1920,84),BattleHudTheme.Background);
        DrawRect(new Rect(0,83,1920,1),BattleHudTheme.Line);
        ArenaInterface.Fill(new Rect(24,19,4,43),BattleHudTheme.Teal);
        BattleHudTheme.Label(new Rect(40,14,196,31),"DITTOCHES",23,null,true);
        BattleHudTheme.Label(new Rect(41,47,190,17),TrialsMode?"TRIALS  /  "+TrialsCatalog.DifficultyNames[TrialDifficulty]:"SOLO LEAGUE  /  "+Difficulties[difficulty],11,BattleHudTheme.Muted);
        Color life=(TrialsMode?TrialLives<=1:hp<=30)?BattleHudTheme.Danger:BattleHudTheme.Teal;
        BattleHudTheme.Label(new Rect(277,11,134,18),TrialsMode?"시험 기회":"테이머 체력",11,BattleHudTheme.Muted);
        BattleHudTheme.Label(new Rect(277,30,112,33),TrialsMode?TrialLives.ToString():hp.ToString(),29,life,true);
        BattleHudTheme.Label(new Rect(355,37,45,22),TrialsMode?"/ 3":"HP",13,life,true);
        BattleHudTheme.Bar(new Rect(278,70,159,4),TrialsMode?TrialLives/3f:hp/100f,life);
        ArenaInterface.Fill(new Rect(467,21,1,40),BattleHudTheme.Line);
        BattleHudTheme.Label(new Rect(492,12,200,18),"현재 전장",11,BattleHudTheme.Muted);
        BattleHudTheme.Label(new Rect(491,34,99,29),board.Count(u=>u!=null)+" / "+FormationLimit,24,null,true);
        BattleHudTheme.FitLabel(new Rect(586,36,128,25),TamerLoadout.Fields[tamerLoadout.field],12,BattleHudTheme.Muted,false);
        bool finished=hp<=0&&!battling;
        string phase=finished?"경기 종료":showCarousel?"보상 선택":battling?"전투":"준비";
        ArenaPresentationUI.RoundPlate(new Rect(742,6,434,66),TrialsMode?TrialStage.ToString("00")+"/30":RoundLabel(),finished?phase:RoundType()+" · "+phase,finished?"FINAL":battling?Mathf.CeilToInt(battleTimeRemaining)+"초":"READY",battling,remaining:TrialsMode&&battling?battleTimeRemaining/CurrentTrial.timeLimit:-1,trial:TrialsMode);
        int count=TrialsMode?30:round<=3?3:7,step=TrialsMode?TrialStage:round<=3?round:1+(round-4)%7;
        for(int i=0;i<count;i++)
        {
            Rect segment=new Rect(746+i*430f/count,76,430f/count-5,3);
            DrawRect(segment,i<step-1?BattleHudTheme.Teal:BattleHudTheme.Line);
            if(i==step-1)DrawRect(new Rect(segment.x,segment.y,segment.width*(battling?1-battleProgress:1),3),BattleHudTheme.Gold);
        }
        int vacancies=FormationLimit-board.Count(u=>u!=null);
        BattleHudTheme.Label(new Rect(1217,14,200,19),"배치 상태",11,BattleHudTheme.Muted);
        BattleHudTheme.Label(new Rect(1217,36,204,30),vacancies>0?"빈 슬롯  "+vacancies+"칸":"편성 완료",20,vacancies>0?BattleHudTheme.Gold:BattleHudTheme.Teal,true);
        BattleHudTheme.Label(new Rect(1442,17,149,25),"대기석  "+bench.Count(u=>u!=null)+" / 9",13,BattleHudTheme.Muted);
        ArenaPresentationUI.SeatMeter(new Rect(1442,55,146,5),bench.Count(u=>u!=null),9,BattleHudTheme.Gold);
        if(HudButton(new Rect(1615,22,78,38),"팀 계획"))OpenTeamPlan();
        if(HudButton(new Rect(1702,22,88,38),"로비",!battling))lobby=true;
        if(HudButton(new Rect(1800,22,94,38),"종료"))Application.Quit();
    }
    void DrawArenaHudLeft()
    {
        HudPanel(new Rect(16,106,215,398));
        HudPanel(new Rect(16,513,215,313));
        var formationPreview=CurrentFormationForecast();
        if(formationPreview!=null)FormationForecastUI.Draw(new Rect(16,106,215,398),formationPreview);
        else
        {
            ArenaPresentationUI.SectionHeading(new Rect(29,120,126,26),scoutedRival>=0?"상대 시너지":"팀 시너지","");
            if(HudButton(new Rect(161,119,53,28),"도감"))OpenTraitGuide("courage");
            bool scouting=scoutedRival>=0;
            var snapshot=ReadTraitHud(scouting?scoutBoard:board,scouting);
            var panel=scouting?scoutTraitPanel:ownTraitPanel;
            string clicked=panel.Draw(new Rect(28,156,189,344),snapshot,new NativeTraitFormation(scouting?null:bench),!scouting);
            if(clicked!=null)OpenTraitGuide(clicked);
        }
        BattleHudTheme.Label(new Rect(30,516,77,23),"보관함",17,null,true);
        if(HudButton(new Rect(110,516,65,24),"합성 도감"))OpenEquipmentGuide(0);
        BattleHudTheme.Badge(new Rect(183,518,31,20),inventory.Count.ToString(),BattleHudTheme.Gold,12);
        inventoryView.Sync(inventory,ref inventoryPage);
        inventoryView.DrawTabs(InventoryTabs,hudSmall,equipmentDrag.Slot>=0,ref inventoryPage);
        int itemPages=inventoryView.Pages,hoveredItem=-1;
        for(int slot=0;slot<12;slot++)
        {
            int i=inventoryView.Index(inventoryPage,slot);Rect r=InventorySlotRect(slot);
            ArenaInterface.Fill(r,i>=0&&i==selectedItem?Color.Lerp(BattleHudTheme.Raised,BattleHudTheme.Teal,.18f):BattleHudTheme.Raised);
            if(i<0){ArenaInterface.Fill(new Rect(r.center.x-3,r.center.y,6,1),BattleHudTheme.Line);ArenaInterface.Fill(new Rect(r.center.x,r.center.y-3,1,6),BattleHudTheme.Line);}
            if(i<0)continue;int item=inventory[i];Event e=Event.current;
            if(r.Contains(e.mousePosition))hoveredItem=i;
            if(GUI.enabled&&e.type==EventType.MouseDown&&e.button==1&&r.Contains(e.mousePosition))
            {OpenEquipmentGuide(item);e.Use();}
            else if(DigimonEquipmentArt.Button(r,item,i==selectedItem,true,selectedItem>=0&&selectedItem<inventory.Count&&selectedItem!=i?inventory[selectedItem]:-1))SelectInventoryItem(i);
        }
        if(itemPages>1)
        {
            if(HudButton(new Rect(30,719,40,26),"‹",inventoryPage>0&&equipmentDrag.Slot<0))inventoryPage--;
            GUI.Label(new Rect(75,719,90,26),new GUIContent((inventoryPage+1)+" / "+itemPages,"보관함 위에서 휠로 페이지 이동 · 선택한 장비 유지"),center);
            if(HudButton(new Rect(174,719,40,26),"›",inventoryPage+1<itemPages&&equipmentDrag.Slot<0))inventoryPage++;
        }
        DrawRect(new Rect(29,748,188,1),BattleHudTheme.Line);
        int inspect=inventoryView.DrawHint(new Rect(30,757,184,57),hudWrap,inventory,selectedItem,hoveredItem,
            "드래그 → 장착 · 최대 3개\n우클릭 → 옵션 / 합성 도감\n재료 2개 선택 → 합성");
        if(inspect>=0)OpenEquipmentGuide(inspect);
    }
    void DrawArenaHudRight()
    {
        if(DrawEquipmentPreview())return;
        const float x=1686;HudPanel(new Rect(x,106,218,inspectedUnit!=null?720:638));
        if(inspectedUnit!=null)DrawTacticalUnitDetails(inspectedUnit,x);
        else if(showCombatReport&&(battling||lastBattleReport.Count>0))DrawTacticalReport(x);
        else if(TrialsMode)DrawTrialsHud(x);
        else
        {
            ArenaPresentationUI.SectionHeading(new Rect(x+14,120,190,29),"테이머", "선택하여 상대 편성 정찰");
            for(int i=0;i<8;i++)
            {
                float y=169+i*61;string name=i==0?"나의 테이머":RivalNames[i-1];
                bool viewing=i==0?scoutedRival<0:scoutedRival==i-1,opponent=battling&&name==currentOpponent;
                bool next=!battling&&i>0&&i-1==NextRivalIndex&&RoundType()=="PvP";
                Color tint=viewing?BattleHudTheme.Teal:opponent?BattleHudTheme.Danger:next?BattleHudTheme.Gold:BattleHudTheme.Line;
                Rect r=new Rect(x+10,y,198,53);
                ArenaInterface.Fill(r,viewing?Color.Lerp(BattleHudTheme.Panel,BattleHudTheme.Teal,.13f):next?Color.Lerp(BattleHudTheme.Panel,BattleHudTheme.Gold,.08f):BattleHudTheme.Panel);
                ArenaInterface.Fill(new Rect(r.x+1,r.yMax-1,r.width-2,1),Color.Lerp(BattleHudTheme.Panel,BattleHudTheme.Line,.58f));
                if(viewing)ArenaInterface.Fill(new Rect(r.x,r.y,3,r.height),tint);
                ArenaInterface.Fill(new Rect(x+15,y+7,39,39),Color.Lerp(BattleHudTheme.Raised,tint,.08f));
                var portrait=TamerLoadout.Portrait(i==0?tamerLoadout.tamer:(i-1)%TamerLoadout.Tamers.Length);
                if(portrait!=null)GUI.DrawTexture(new Rect(x+13,y+7,42,39),portrait,ScaleMode.ScaleToFit);
                if(next||opponent)ArenaInterface.Fill(new Rect(r.x,r.y,3,r.height),tint);
                BattleHudTheme.Label(new Rect(x+64,y+4,129,25),name,16,null,true);
                BattleHudTheme.Label(new Rect(x+64,y+29,129,18),i==0?hp+" HP · 나의 필드":viewing?"정찰 중":opponent?"현재 대전":next?"다음 대전":"AI 테이머",12,viewing||opponent||next?tint:BattleHudTheme.Muted);
                if(GUI.Button(r,GUIContent.none,GUIStyle.none)){if(i==0)ReturnToOwnField();else ShowRivalBoard(i-1);}
            }
            if(lastBattleReport.Count>0||battling)if(HudButton(new Rect(x+13,695,192,34),"전투 기록"))showCombatReport=true;
        }
        if(inspectedUnit==null)
        {
            BattleHudTheme.Surface(new Rect(x,755,218,71));
            GUI.Label(new Rect(x+14,766,191,45),scoutedRival>=0?"Q / R   이전 · 다음 테이머\nSPACE   내 필드 복귀":battling?"상점으로 드래그 → 판매\nE   대기석 유닛 판매":"W   유닛 배치 / 회수\nE   판매 · 우클릭   유닛 정보",hudSmall);
        }
    }
    void DrawArenaHudShop()
    {
        DrawRect(new Rect(0,845,1920,235),BattleHudTheme.Background);
        HudPanel(new Rect(16,850,215,220));
        HudPanel(new Rect(239,850,1431,220));
        HudPanel(new Rect(1686,850,218,220));
        BattleHudTheme.Label(new Rect(28,863,90,31),"레벨 "+level,22,null,true);
        BattleHudTheme.Label(new Rect(119,868,99,22),level==9?"MAX":xp+" / "+NeedXp()+" XP",12,BattleHudTheme.Muted,false,TextAnchor.MiddleRight);
        BattleHudTheme.Bar(new Rect(28,901,191,5),level==9?1:(float)xp/NeedXp(),BattleHudTheme.Teal);
        if(ArenaPresentationUI.CommandButton(new Rect(24,925,200,54),"경험치 구매","경험치 +4","F",gold>=4&&level<9,false,"4 G",RecruitmentAdvice.Experience(level,xp,NeedXp())+"\n"+RecruitmentAdvice.Spend(gold,4)))PurchaseExperience();
        if(ArenaPresentationUI.CommandButton(new Rect(24,991,200,54),"새로고침","새로운 디지몬 모집","D",gold>=2,false,"2 G",RecruitmentAdvice.Spend(gold,2)+"\n현재 상점 유닛을 교체합니다"))RerollShop();
        BattleHudTheme.Label(new Rect(254,861,83,24),"모집 확률",12,BattleHudTheme.Muted);
        for(int tier=1;tier<=5;tier++)
        {
            float x=342+(tier-1)*76;DrawRect(new Rect(x,867,5,14),CostColor(tier));
            GUI.Label(new Rect(x+11,861,64,25),new GUIContent(ShopOdds[level-1,tier-1]+"%",tier+"골드 유닛 등장 확률"+(level<9?"\n다음 레벨: "+ShopOdds[level,tier-1]+"%":"\n최고 레벨")),hudSmall);
        }
        DrawRect(new Rect(755,860,1,25),BattleHudTheme.Line);
        BattleHudTheme.Label(new Rect(784,854,128,37),gold+" G",27,BattleHudTheme.Gold,true);
        int interest=Mathf.Min(5,gold/10);
        ArenaPresentationUI.SeatMeter(new Rect(927,866,82,5),interest,5,BattleHudTheme.Gold);
        BattleHudTheme.Label(new Rect(927,874,90,13),"이자 +"+interest+" G",10,BattleHudTheme.Muted);
        int previewCost=-1;Vector2 mouse=Event.current.mousePosition;
        for(int i=0;i<5;i++)if(shop[i]!=null&&new Rect(249+i*285,893,271,170).Contains(mouse))previewCost=shop[i].cost;
        if(level<9&&new Rect(24,925,200,54).Contains(mouse))previewCost=4;
        if(new Rect(24,991,200,54).Contains(mouse))previewCost=2;
        GUI.Label(new Rect(1040,859,450,28),new GUIContent(draggingUnit&&HeldUnit()!=null?"판매 후 "+(gold+UnitSaleValue(HeldUnit()))+"G · 이자 +"+RecruitmentAdvice.Interest(gold+UnitSaleValue(HeldUnit()))+"G":previewCost>=0?RecruitmentAdvice.Spend(gold,previewCost):RecruitmentAdvice.Bank(gold), "보유 골드 10마다 이자 +1G, 최대 +5G · 현재 보유 골드 기준"),hudSmall);
        if(HudButton(new Rect(1501,858,159,28),new GUIContent(shopLocked?"잠금 유지 중":"상점 잠금",RecruitmentAdvice.ShopLockHint),true,shopLocked)){shopLocked=!shopLocked;Save();}
        for(int i=0;i<5;i++)
        {
            if(draggingUnit&&HeldUnit()!=null)continue;
            Rect r=new Rect(249+i*285,893,271,170);var d=shop[i];
            bool hovered=GUI.enabled&&r.Contains(Event.current.mousePosition);
            if(Event.current.type==EventType.Repaint)shopHover[i]=Mathf.MoveTowards(shopHover[i],hovered?1:0,Time.unscaledDeltaTime*7);
            if(d==null)
            {
                BattleHudTheme.Surface(r);BattleHudTheme.Label(new Rect(r.x+15,r.y+47,r.width-30,32),"모집 완료",19,BattleHudTheme.Muted,true,TextAnchor.MiddleCenter);
                BattleHudTheme.Label(new Rect(r.x+15,r.y+88,r.width-30,26),"다음 상점에서 다시 만나요",12,BattleHudTheme.Muted,false,TextAnchor.MiddleCenter);continue;
            }
            int singles=FindUnits(d.id,1).Count,doubles=FindUnits(d.id,2).Count;
            bool merges=singles>=2,room=bench.Any(u=>u==null)||merges,afford=gold>=d.cost;
            var skill=DigimonSkillCatalog.Find(d.id);
            string status=!afford?"골드 부족":!room?"대기석 가득":merges?"구매 시 "+(doubles>=2?"★★★":"★★")+" 승급":RecruitmentAdvice.Copies(singles,doubles);
            BattleHudTheme.ShopArtwork(r,d.id,UnitName(d),d.cost,status,singles,doubles,merges,shopHover[i],false,ReadTraitHud(board,false).DeploymentBreakpoint(d.id));
            if(DigimonSkillUI.DrawIcon(new Rect(r.x+158,r.y+9,27,27),skill))OpenShopSkill(d);
            if(GUI.enabled&&Event.current.type==EventType.MouseDown&&Event.current.button==1&&r.Contains(Event.current.mousePosition))
            {OpenShopSkill(d);Event.current.Use();}
            TeamPlannerUI.ShopMarker(new Rect(r.x+5,r.y+7,46,22),d.id,center);
            string tip=(TeamPlan.Contains(d.id)?"[팀 계획 목표]\n":"")+ShopUnitSummary(d,singles,doubles)+"\n"+RecruitmentAdvice.Spend(gold,d.cost)+"\n우클릭: 유닛 / 스킬 정보";
            GUI.Label(r,new GUIContent("",tip),GUIStyle.none);
            bool enabled=GUI.enabled;GUI.enabled=enabled&&afford&&room;
            if(GUI.Button(r,new GUIContent("",tip),GUIStyle.none)&&Buy(i))
            {Save();}
            GUI.enabled=enabled;
            if(!afford||!room)DrawRect(r,new Color(.012f,.018f,.026f,.22f));
        }
        bool carousel=RoundType()=="초밥집";
        BattleHudTheme.Label(new Rect(1698,864,192,20),"NEXT ACTION",11,BattleHudTheme.Muted,true);
        BattleHudTheme.Label(new Rect(1698,885,192,22),battling?"상점 · 장비 사용 가능":carousel?"원하는 보상을 선택하세요":"편성을 마치고 출발하세요",12,BattleHudTheme.Muted);
        if(ArenaPresentationUI.CommandButton(new Rect(1696,916,205,87),battling?"전투 진행 중":carousel?"보상 선택":"전투 시작",battling?"전투 종료를 기다리는 중":"준비 완료","ENTER",!battling&&hp>0&&(carousel||board.Any(u=>u!=null)),true))StartRoundAction();
        int deployed=board.Count(u=>u!=null);
        BattleHudTheme.Label(new Rect(1699,1016,190,24),"편성  "+deployed+" / "+FormationLimit,15,deployed<FormationLimit?BattleHudTheme.Gold:BattleHudTheme.Teal,true);
        ArenaPresentationUI.SeatMeter(new Rect(1699,1050,190,5),deployed,FormationLimit,deployed<FormationLimit?BattleHudTheme.Gold:BattleHudTheme.Teal);
    }
}
