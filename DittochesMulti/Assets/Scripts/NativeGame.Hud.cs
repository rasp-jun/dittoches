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
        GUI.Label(new Rect(24,44,230,23),"파일 아일랜드 · "+Difficulties[difficulty],hudSmall);
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
        GUI.Label(new Rect(1240,18,215,27),"전장  "+board.Count(u=>u!=null)+" / "+level,hudName);
        GUI.Label(new Rect(1240,48,215,22),"같은 유닛 3개로 별 합성",hudSmall);
        GUI.Label(new Rect(1460,20,160,26),"대기석 "+bench.Count(u=>u!=null)+" / 9",hudSmall);
        for(int i=0;i<9;i++)DrawRect(new Rect(1460+i*17,56,12,4),bench[i]!=null?accent:new Color(.14f,.22f,.24f));
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
            GUI.Label(new Rect(30,121,188,28),"팀 시너지",hudName);
            var traits=TeamTraits();int pages=Mathf.Max(1,(traits.Count+7)/8);traitPage=Mathf.Clamp(traitPage,0,pages-1);
            for(int row=0;row<8;row++)
            {
                int i=traitPage*8+row;if(i>=traits.Count)break;
                var trait=traits[i];int tier=TraitTier(trait.category,trait.count);float y=164+row*37;
                Rect r=new Rect(28,y,189,32);DrawRect(r,tier>0?new Color(.16f,.16f,.105f):new Color(.04f,.065f,.072f));
                DrawRect(new Rect(r.x,r.y,3,r.height),TraitColor(tier));
                GUI.Label(new Rect(38,y+5,126,24),trait.key,hudSmall);
                GUI.Label(new Rect(169,y+5,49,24),trait.count+"/"+TraitTarget(trait.category,trait.count),hudSmall);
                if(GUI.Button(r,new GUIContent("",TraitEffectText(trait.category,trait.key,tier)),GUIStyle.none))
                {showRecipeGuide=false;traitFocus=trait;traitGuideUntil=Time.unscaledTime+60f;}
            }
            if(traits.Count==0)GUI.Label(new Rect(30,170,182,60),"유닛을 배치하면\n시너지가 표시됩니다",hudWrap);
            if(pages>1)
            {
                if(HudButton(new Rect(30,469,40,26),"‹",traitPage>0))traitPage--;
                GUI.Label(new Rect(75,469,90,26),(traitPage+1)+" / "+pages,center);
                if(HudButton(new Rect(174,469,40,26),"›",traitPage+1<pages))traitPage++;
            }
        }
        GUI.Label(new Rect(30,511,180,26),"장비  "+inventory.Count,hudName);
        int itemPages=Mathf.Max(1,(inventory.Count+11)/12);inventoryPage=Mathf.Clamp(inventoryPage,0,itemPages-1);
        for(int slot=0;slot<12;slot++)
        {
            int i=inventoryPage*12+slot;Rect r=new Rect(30+slot%4*47,550+slot/4*46,40,40);
            GUI.Box(r,GUIContent.none,i==selectedItem?selectedStyle:card);
            if(i>=inventory.Count)continue;int item=inventory[i];Event e=Event.current;
            if(GUI.enabled&&e.type==EventType.MouseDown&&e.button==1&&r.Contains(e.mousePosition))
            {traitFocus=null;recipeFocus=item;showRecipeGuide=true;recipeGuideUntil=Time.unscaledTime+60f;e.Use();}
            else if(HudButton(r,new GUIContent(ItemIcons[item],ItemNames[item]+"\n"+ItemDescriptions[item]),true,i==selectedItem))SelectInventoryItem(i);
        }
        if(itemPages>1)
        {
            if(HudButton(new Rect(30,693,40,26),"‹",inventoryPage>0))inventoryPage--;
            GUI.Label(new Rect(75,693,90,26),(inventoryPage+1)+" / "+itemPages,center);
            if(HudButton(new Rect(174,693,40,26),"›",inventoryPage+1<itemPages))inventoryPage++;
        }
        GUI.Label(new Rect(30,737,182,64),selectedItem>=0?EquipmentHint():"장비 클릭 → 아군에게 장착\n우클릭 → 조합 확인",hudWrap);
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
        bool canSell=!showCarousel&&(selectedBench>=0||(!battling&&selectedBoard>=0));
        if(HudButton(new Rect(x+13,767,192,42),"선택 유닛 판매",canSell))SellSelectedUnit();
    }
    void DrawArenaHudShop()
    {
        HudPanel(new Rect(0,850,1920,230));
        GUI.Label(new Rect(28,865,110,27),"레벨 "+level,hudName);
        GUI.Label(new Rect(136,865,90,26),level==9?"MAX":xp+" / "+NeedXp(),hudSmall);
        MiniBar(new Rect(28,900,192,5),level==9?1:(float)xp/NeedXp(),new Color(.33f,.64f,.88f));
        if(HudButton(new Rect(24,925,200,54),"경험치 +4   4G   [F]",gold>=4&&level<9))PurchaseExperience();
        if(HudButton(new Rect(24,991,200,54),"새로고침   2G   [D]",gold>=2))RerollShop();
        GUI.Label(new Rect(254,861,83,24),"등장 확률",hudSmall);
        for(int tier=1;tier<=5;tier++)
        {
            float x=342+(tier-1)*79;DrawRect(new Rect(x,869,4,12),CostColor(tier));
            GUI.Label(new Rect(x+11,861,68,25),new GUIContent(ShopOdds[level-1,tier-1]+"%",tier+"골드 유닛 등장 확률"),hudSmall);
        }
        GUI.Label(new Rect(793,855,176,35),gold+" G",hudNumber);
        int interest=Mathf.Min(5,gold/10);
        for(int i=0;i<5;i++)DrawRect(new Rect(1001+i*18,868,12,9),i<interest?accent:new Color(.17f,.22f,.23f));
        GUI.Label(new Rect(1104,859,295,28),new GUIContent("이자 +"+interest+" G", "보유 골드 10마다 이자 +1G, 최대 +5G"),hudSmall);
        if(HudButton(new Rect(1501,858,159,28),shopLocked?"잠금 유지 중":"상점 잠금",true,shopLocked)){shopLocked=!shopLocked;Save();}
        for(int i=0;i<5;i++)
        {
            Rect r=new Rect(249+i*285,893,271,170);var d=shop[i];
            bool hovered=GUI.enabled&&r.Contains(Event.current.mousePosition);
            if(Event.current.type==EventType.Repaint)shopHover[i]=Mathf.MoveTowards(shopHover[i],hovered?1:0,Time.unscaledDeltaTime*7);
            DrawRect(r,new Color(.025f,.055f,.075f));
            if(d==null){GUI.Label(r,"모집 완료",center);continue;}
            int singles=FindUnits(d.id,1).Count,doubles=FindUnits(d.id,2).Count;
            bool merges=singles>=2,room=bench.Any(u=>u==null)||merges,afford=gold>=d.cost;
            Color rarity=CostColor(d.cost);var skill=DigimonSkillCatalog.Find(d.id);
            DrawRect(new Rect(r.x+1,r.y+1,r.width-2,117),Color.Lerp(new Color(.025f,.055f,.075f),rarity,.16f+shopHover[i]*.1f));
            DrawRect(new Rect(r.x,r.y,r.width,3),rarity);
            Portrait(new Rect(r.x+4,r.y+7-shopHover[i]*3,126,117),UnitSprite(d));
            DrawRect(new Rect(r.x+216,r.y+9,46,29),new Color(.015f,.035f,.043f,.9f));
            GUI.Label(new Rect(r.x+218,r.y+11,43,25),d.cost+" G",center);
            GUI.Label(new Rect(r.x+141,r.y+43,119,23),DigimonBuildCatalog.ForUnit(d.id).First().name,hudSmall);
            GUI.Label(new Rect(r.x+141,r.y+68,119,23),DigimonBuildCatalog.ForUnit(d.id).Last().name,hudSmall);
            GUI.Label(new Rect(r.x+141,r.y+93,119,23),skill.ScalingRole.Replace(" 딜러","")+" · "+skill.attackRange+"칸",hudSmall);
            if(DigimonSkillUI.DrawIcon(new Rect(r.x+141,r.y+7,30,30),skill))OpenShopSkill(d);
            GUI.Label(new Rect(r.x+176,r.y+12,39,22),"스킬",hudSmall);
            DrawRect(new Rect(r.x,r.y+122,r.width,48),new Color(.022f,.039f,.052f));
            GUI.Label(new Rect(r.x+12,r.y+122,246,25),UnitName(d),hudName);
            string status=!afford?"골드 부족":!room?"대기석 가득":merges?"구매하면 자동 합성":singles+doubles>0?"보유  ★ "+singles+"  ★★ "+doubles:"클릭하여 모집";
            GUI.Label(new Rect(r.x+12,r.y+147,246,23),status,hudSmall);
            if(hovered||merges)
            {
                Color edge=merges?accent:rarity;
                DrawRect(new Rect(r.x,r.y,2,r.height),edge);DrawRect(new Rect(r.xMax-2,r.y,2,r.height),edge);DrawRect(new Rect(r.x,r.yMax-2,r.width,2),edge);
            }
            bool enabled=GUI.enabled;GUI.enabled=enabled&&afford&&room;
            string tip=new Rect(r.x+141,r.y+7,30,30).Contains(Event.current.mousePosition)?skill.name+" · 우클릭으로 스킬 정보":ShopUnitSummary(d,singles,doubles);
            if(GUI.Button(r,new GUIContent("",tip),GUIStyle.none)&&Buy(i))
            {NotifyPlacement(UnitName(d)+(merges?" · 자동 합성 완료":" 모집 완료"));Save();}
            GUI.enabled=enabled;
            if(!afford||!room)DrawRect(r,new Color(.012f,.018f,.026f,.22f));
        }
        bool carousel=RoundType()=="초밥집";
        GUI.Label(new Rect(1694,870,208,28),battling?"전투 진행 중":"배치 준비",center);
        if(HudButton(new Rect(1696,916,205,87),battling?"전투 중":carousel?"보상 선택\n[SPACE]":"전투 시작\n[SPACE]",!battling&&(carousel||board.Any(u=>u!=null)),true))StartRoundAction();
        GUI.Label(new Rect(1696,1014,205,46),"배치 "+board.Count(u=>u!=null)+" / "+level+"\n대기석 "+bench.Count(u=>u!=null)+" / 9",center);
    }
}
