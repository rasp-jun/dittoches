using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    Rect SoloArenaViewport { get { return artPack==0?TacticalArena.WideViewport:TacticalArena.SoloViewport; } }
    GUIStyle hudSmall,hudName,hudWrap,hudButton,hudNumber;
    readonly ArenaInterface arenaInterface=new ArenaInterface();
    void HudStyles()
    {
        if(hudSmall!=null)return;
        hudSmall=new GUIStyle(label){fontSize=13};hudSmall.normal.textColor=new Color(.67f,.76f,.76f);
        hudName=new GUIStyle(header){fontSize=17};
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
    {DrawRect(rect,new Color(.025f,.045f,.056f,.97f));DrawRect(new Rect(rect.x,rect.y,rect.width,1),new Color(.32f,.38f,.33f));}
    void DrawArenaHudTop()
    {
        HudStyles();HudPanel(new Rect(0,0,1920,84));
        GUI.Label(new Rect(24,15,230,25),"DITTOCHES",hudName);
        GUI.Label(new Rect(24,44,230,23),TamerLoadout.Fields[tamerLoadout.field]+" · "+Difficulties[difficulty],hudSmall);
        GUI.Label(new Rect(291,19,210,28),"체력  "+hp,hudName);
        MiniBar(new Rect(291,56,160,4),hp/100f,new Color(.40f,.85f,.59f));
        GUI.Label(new Rect(493,19,225,28),gold+" G",hudName);
        GUI.Label(new Rect(493,49,225,22),"이자 +"+Mathf.Min(5,gold/10)+" G",hudSmall);
        string phase=showCarousel?"보상 선택":battling?"전투":"준비";
        HudPanel(new Rect(766,7,388,70));
        GUI.Label(new Rect(783,12,99,36),RoundLabel(),hudNumber);
        GUI.Label(new Rect(888,12,127,24),RoundType()+" · "+phase,center);
        GUI.Label(new Rect(1020,14,120,32),battling?Mathf.CeilToInt(battleTimeRemaining)+"초":"배치 중",hudName);
        int count=round<=3?3:7,step=round<=3?round:1+(round-4)%7;
        for(int i=0;i<count;i++)
        {
            Rect segment=new Rect(788+i*347f/count,57,347f/count-6,5);
            DrawRect(segment,i<step-1?accent:new Color(.14f,.22f,.24f));
            if(i==step-1)DrawRect(new Rect(segment.x,segment.y,segment.width*(battling?1-battleProgress:1),5),new Color(.39f,.87f,.73f));
        }
        GUI.Label(new Rect(1240,18,215,27),"전장  "+board.Count(u=>u!=null)+" / "+FormationLimit,hudName);
        int vacancies=FormationLimit-board.Count(u=>u!=null);
        GUI.Label(new Rect(1240,48,215,22),vacancies>0?"빈 전장 슬롯 "+vacancies+"칸 · 배치 가능":"최대 인원 배치 완료",hudSmall);
        GUI.Label(new Rect(1460,20,160,26),"대기석 "+bench.Count(u=>u!=null)+" / 9",hudSmall);
        for(int i=0;i<9;i++)DrawRect(new Rect(1460+i*17,56,12,4),bench[i]!=null?accent:new Color(.14f,.22f,.24f));
        if(HudButton(new Rect(1615,22,78,38),"팀 계획"))OpenTeamPlan();
        if(HudButton(new Rect(1702,22,88,38),"로비",!battling))lobby=true;
        if(HudButton(new Rect(1800,22,94,38),"종료"))Application.Quit();
    }
    void DrawArenaHudLeft()
    {
        HudPanel(new Rect(16,106,215,720));
        var formationPreview=CurrentFormationForecast();
        if(formationPreview!=null)FormationForecastUI.Draw(new Rect(16,106,215,398),formationPreview);
        else
        {
            GUI.Label(new Rect(30,121,126,28),"팀 시너지",hudName);
            if(HudButton(new Rect(161,119,53,28),"도감"))OpenTraitGuide("courage");
            var traits=TeamTraits();int pages=Mathf.Max(1,(traits.Count+7)/8);traitPage=Mathf.Clamp(traitPage,0,pages-1);
            for(int row=0;row<8;row++)
            {
                int i=traitPage*8+row;if(i>=traits.Count)break;
                var trait=traits[i];var definition=DigimonBuildCatalog.Find(trait.key);int tier=definition.Level(trait.count);float y=164+row*37;
                Rect r=new Rect(28,y,189,32);DrawRect(r,tier>0?new Color(.16f,.16f,.105f):new Color(.04f,.065f,.072f));
                DrawRect(new Rect(r.x,r.y,3,r.height),TraitColor(tier));
                GUI.DrawTexture(new Rect(35,y+4,24,24),DigimonTraitUI.Icon(definition.id));
                GUI.Label(new Rect(66,y+5,103,24),trait.key,hudSmall);
                GUI.Label(new Rect(169,y+5,49,24),trait.count+"/"+definition.Target(trait.count),hudSmall);
                if(GUI.Button(r,new GUIContent("",TraitEffectText(trait.category,trait.key,tier)),GUIStyle.none))
                {OpenTraitGuide(trait.key);}
            }
            if(traits.Count==0)GUI.Label(new Rect(30,170,182,60),"유닛을 배치하면\n시너지가 표시됩니다",hudWrap);
            if(pages>1)
            {
                if(HudButton(new Rect(30,469,40,26),"‹",traitPage>0))traitPage--;
                GUI.Label(new Rect(75,469,90,26),(traitPage+1)+" / "+pages,center);
                if(HudButton(new Rect(174,469,40,26),"›",traitPage+1<pages))traitPage++;
            }
        }
        GUI.Label(new Rect(30,511,118,26),"무장  "+inventory.Count,hudName);
        if(HudButton(new Rect(153,509,61,29),"도감"))OpenEquipmentGuide(selectedItem>=0&&selectedItem<inventory.Count?inventory[selectedItem]:0);
        int itemPages=Mathf.Max(1,(inventory.Count+11)/12);inventoryPage=Mathf.Clamp(inventoryPage,0,itemPages-1);
        for(int slot=0;slot<12;slot++)
        {
            int i=inventoryPage*12+slot;Rect r=InventorySlotRect(slot);
            GUI.Box(r,GUIContent.none,i==selectedItem?selectedStyle:card);
            if(i>=inventory.Count)continue;int item=inventory[i];Event e=Event.current;
            if(GUI.enabled&&e.type==EventType.MouseDown&&e.button==1&&r.Contains(e.mousePosition))
            {OpenEquipmentGuide(item);e.Use();}
            else if(DigimonEquipmentArt.Button(r,item,i==selectedItem,true,selectedItem>=0&&selectedItem<inventory.Count&&selectedItem!=i?inventory[selectedItem]:-1))SelectInventoryItem(i);
        }
        if(itemPages>1)
        {
            if(HudButton(new Rect(30,693,40,26),"‹",inventoryPage>0))inventoryPage--;
            GUI.Label(new Rect(75,693,90,26),(inventoryPage+1)+" / "+itemPages,center);
            if(HudButton(new Rect(174,693,40,26),"›",inventoryPage+1<itemPages))inventoryPage++;
        }
        GUI.Label(new Rect(30,737,182,64),selectedItem>=0?EquipmentHint():"무장 드래그 / 클릭 → 장착\n재료 2개 클릭 → 융합\n우클릭 / 도감 → 전체 조합",hudWrap);
    }
    void DrawArenaHudRight()
    {
        if(DrawEquipmentPreview())return;
        const float x=1686;HudPanel(new Rect(x,106,218,720));
        if(inspectedUnit!=null)DrawTacticalUnitDetails(inspectedUnit,x);
        else if(showCombatReport&&(battling||lastBattleReport.Count>0))DrawTacticalReport(x);
        else
        {
            GUI.Label(new Rect(x+14,122,190,29),"테이머",hudName);
            for(int i=0;i<8;i++)
            {
                float y=169+i*61;string name=i==0?"나의 테이머":RivalNames[i-1];
                bool viewing=!battling&&(i==0?scoutedRival<0:scoutedRival==i-1),opponent=battling&&name==currentOpponent;
                Rect r=new Rect(x+10,y,198,53);GUI.Box(r,GUIContent.none,viewing||opponent?selectedStyle:card);
                GUI.Label(new Rect(x+22,y+7,174,23),name,hudName);
                GUI.Label(new Rect(x+22,y+31,174,20),i==0?hp+" HP":opponent?"현재 상대":"AI 테이머 · 관전",hudSmall);
                if(!battling&&GUI.Button(r,GUIContent.none,GUIStyle.none)){if(i==0)scoutedRival=-1;else ShowRivalBoard(i-1);}
            }
            if(lastBattleReport.Count>0||battling)if(HudButton(new Rect(x+13,695,192,34),"전투 기록"))showCombatReport=true;
        }
        GUI.Label(new Rect(x+13,767,192,42),battling?"대기석 → 상점에 드래그\n[E] 판매":"유닛 → 상점에 드래그\n[E] 판매",hudSmall);
    }
    void DrawArenaHudShop()
    {
        HudPanel(new Rect(0,850,1920,230));
        GUI.Label(new Rect(28,865,110,27),"레벨 "+level,hudName);
        GUI.Label(new Rect(136,865,90,26),level==9?"MAX":xp+" / "+NeedXp(),hudSmall);
        MiniBar(new Rect(28,900,192,5),level==9?1:(float)xp/NeedXp(),new Color(.33f,.64f,.88f));
        if(HudButton(new Rect(24,925,200,54),new GUIContent("경험치 +4   4G   [F]",RecruitmentAdvice.Experience(level,xp,NeedXp())+"\n"+RecruitmentAdvice.Spend(gold,4)),gold>=4&&level<9))PurchaseExperience();
        if(HudButton(new Rect(24,991,200,54),new GUIContent("새로고침   2G   [D]",RecruitmentAdvice.Spend(gold,2)+"\n현재 상점 유닛을 교체합니다"),gold>=2))RerollShop();
        GUI.Label(new Rect(254,861,83,24),"등장 확률",hudSmall);
        for(int tier=1;tier<=5;tier++)
        {
            float x=342+(tier-1)*79;DrawRect(new Rect(x,869,4,12),CostColor(tier));
            GUI.Label(new Rect(x+11,861,68,25),new GUIContent(ShopOdds[level-1,tier-1]+"%",tier+"골드 유닛 등장 확률"+(level<9?"\n다음 레벨: "+ShopOdds[level,tier-1]+"%":"\n최고 레벨")),hudSmall);
        }
        GUI.Label(new Rect(793,855,176,35),gold+" G",hudNumber);
        int interest=Mathf.Min(5,gold/10);
        for(int i=0;i<5;i++)DrawRect(new Rect(1001+i*18,868,12,9),i<interest?accent:new Color(.17f,.22f,.23f));
        int previewCost=-1;Vector2 mouse=Event.current.mousePosition;
        for(int i=0;i<5;i++)if(shop[i]!=null&&new Rect(249+i*285,893,271,170).Contains(mouse))previewCost=shop[i].cost;
        if(level<9&&new Rect(24,925,200,54).Contains(mouse))previewCost=4;
        if(new Rect(24,991,200,54).Contains(mouse))previewCost=2;
        GUI.Label(new Rect(1104,859,385,28),new GUIContent(draggingUnit&&HeldUnit()!=null?"판매 후 "+(gold+UnitSaleValue(HeldUnit()))+"G · 이자 +"+RecruitmentAdvice.Interest(gold+UnitSaleValue(HeldUnit()))+"G":previewCost>=0?RecruitmentAdvice.Spend(gold,previewCost):RecruitmentAdvice.Bank(gold), "보유 골드 10마다 이자 +1G, 최대 +5G · 현재 보유 골드 기준"),hudSmall);
        if(HudButton(new Rect(1501,858,159,28),new GUIContent(shopLocked?"잠금 유지 중":"상점 잠금",RecruitmentAdvice.ShopLockHint),true,shopLocked)){shopLocked=!shopLocked;Save();}
        for(int i=0;i<5;i++)
        {
            if(draggingUnit&&HeldUnit()!=null)continue;
            Rect r=new Rect(249+i*285,893,271,170);var d=shop[i];
            bool hovered=GUI.enabled&&r.Contains(Event.current.mousePosition);
            if(Event.current.type==EventType.Repaint)shopHover[i]=Mathf.MoveTowards(shopHover[i],hovered?1:0,Time.unscaledDeltaTime*7);
            DrawRect(r,new Color(.025f,.055f,.075f));
            if(d==null){GUI.Label(r,"모집 완료",center);continue;}
            int singles=FindUnits(d.id,1).Count,doubles=FindUnits(d.id,2).Count;
            bool merges=singles>=2,room=bench.Any(u=>u==null)||merges,afford=gold>=d.cost;
            Color rarity=CostColor(d.cost);var skill=DigimonSkillCatalog.Find(d.id);
            CharacterCardArt.Stage(new Rect(r.x+1,r.y+1,r.width-2,120),d.cost);
            DrawRect(new Rect(r.x,r.y,r.width,3),rarity);
            if(FaithfulPortraits.Get(d.id,true)!=null)FaithfulPortraits.Draw(new Rect(r.x+2,r.y+3,135,118),d.id,true);
            else Portrait(new Rect(r.x+4,r.y+7,126,114),UnitSprite(d));
            DrawRect(new Rect(r.x+216,r.y+9,46,29),new Color(.015f,.035f,.043f,.9f));
            GUI.Label(new Rect(r.x+218,r.y+11,43,25),d.cost+" G",center);
            GUI.Label(new Rect(r.x+141,r.y+43,119,23),DigimonBuildCatalog.ForUnit(d.id).First().name,hudSmall);
            GUI.Label(new Rect(r.x+141,r.y+68,119,23),DigimonBuildCatalog.ForUnit(d.id).Last().name,hudSmall);
            GUI.Label(new Rect(r.x+141,r.y+93,119,23),skill.ScalingRole.Replace(" 딜러","")+" · "+skill.attackRange+"칸",hudSmall);
            if(DigimonSkillUI.DrawIcon(new Rect(r.x+141,r.y+7,30,30),skill))OpenShopSkill(d);
            GUI.Label(new Rect(r.x+176,r.y+12,39,22),"스킬",hudSmall);
            DrawRect(new Rect(r.x,r.y+122,r.width,48),new Color(.022f,.039f,.052f));
            GUI.Label(new Rect(r.x+12,r.y+122,246,25),UnitName(d),hudName);
            string status=!afford?"골드 부족":!room?"대기석 가득":RecruitmentAdvice.Copies(singles,doubles);
            GUI.Label(new Rect(r.x+12,r.y+147,246,23),status,hudSmall);
            CharacterCardArt.Border(r,d.cost,merges,shopHover[i]);
            if(hovered||merges)
            {
                Color edge=merges?accent:rarity;
                DrawRect(new Rect(r.x,r.y,2,r.height),edge);DrawRect(new Rect(r.xMax-2,r.y,2,r.height),edge);DrawRect(new Rect(r.x,r.yMax-2,r.width,2),edge);
            }
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
        GUI.Label(new Rect(1694,870,208,28),battling?"장비·상점 이용 가능":"배치 준비",center);
        if(HudButton(new Rect(1696,916,205,87),battling?"전투 중":carousel?"보상 선택\n[SPACE]":"전투 시작\n[SPACE]",!battling&&(carousel||board.Any(u=>u!=null)),true))StartRoundAction();
        GUI.Label(new Rect(1696,1014,205,46),"배치 "+board.Count(u=>u!=null)+" / "+FormationLimit+"\n대기석 "+bench.Count(u=>u!=null)+" / 9",center);
    }
}
