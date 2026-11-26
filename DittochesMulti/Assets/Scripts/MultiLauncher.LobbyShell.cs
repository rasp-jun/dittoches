#if DITTOCHES_PORTABLE_PREVIEW
using PlayerPrefs = PortablePreviewPrefs;
#endif
using System;
using System.Linq;
using UnityEngine;

public sealed partial class MultiLauncher
{
    static readonly string[] LobbyModes={"솔로 연습","일반 리그","랭크 리그","아구몬박사의 시험"};
    static readonly string[] LobbyModeTags={"SOLO / AI 7","NORMAL / 8","RANKED / 8","TRIALS / PVE"};
    bool LobbyOffline {get{return lobbyMode==0||lobbyMode==3;}}
    bool LobbyWorkspace {get{return lobbyTab==2||lobbyTab==4;}}
    Rect LobbyPrimaryRect {get{return LobbyWorkspace?new Rect(1352,34,224,60):new Rect(1250,738,304,70);}}
    Rect LobbyPlannerRect {get{return LobbyWorkspace?new Rect(1162,34,172,60):new Rect(1250,832,304,55);}}
    Rect LobbyModeOpenRect {get{return LobbyWorkspace?new Rect(978,34,168,60):new Rect(1250,356,304,89);}}
    static readonly Rect LobbyServerActionRect=new Rect(264,622,450,64),LobbyStudioRect=new Rect(222,120,1354,804);
    static readonly Rect LobbyModeCloseRect=new Rect(1490,107,48,48),LobbyConnectionRect=new Rect(20,684,156,74);
    static readonly Rect LobbyHomeActionRect=new Rect(1250,738,304,70);
    static Rect LobbyNavRect(int index){return new Rect(20,184+index*92,156,78);}
    static Rect LobbyModeRect(int index){return new Rect(270+index%2*634,219+index/2*292,610,268);}
    static Rect LobbyLoadoutRect(int index){return new Rect(242+index*318,795,306,109);}
    int lobbyMode;
    string lobbyFocusedControl;
    bool lobbyModeOpen;
    float lobbyQueueSince=-1;
    TacticalArena lobbyShowcase;
    bool LobbyQueued {get{return state!=null&&!string.IsNullOrEmpty(state.queue);}}
    bool LobbyTamerSupported {get{return state==null||lobbyLoadout.tamer<Mathf.Max(4,state.tamerCount);}}
    void SetLobbyTab(int tab){lobbyTab=tab;lobbyModeOpen=false;GUI.FocusControl(null);if(tab!=0&&tab!=1)DisposeLobbyShowcase();}
    void SetLobbyMode(int mode)
    {if(LobbyQueued||busy)return;lobbyMode=Mathf.Clamp(mode,0,3);PlayerPrefs.SetInt("multi.lobby.mode",lobbyMode);PlayerPrefs.Save();}
    void DisposeLobbyShowcase(){if(lobbyShowcase!=null){lobbyShowcase.Dispose();lobbyShowcase=null;}}
    void DrawModernLobby()
    {
        if(LobbyQueued){if(lobbyQueueSince<0)lobbyQueueSince=Time.unscaledTime;lobbyMode=state.queue.StartsWith("ranked")?2:1;}
        else lobbyQueueSince=-1;
        ArenaInterface.Fill(new Rect(0,0,1600,944),LobbyVisuals.Ink);
        SeasonLobbyArt.Background(new Rect(196,0,1404,944),lobbyBackground,.86f);
        bool enabled=GUI.enabled;GUI.enabled=enabled&&!lobbyModeOpen;
        if(lobbyTab==0)DrawModernHome();
        else if(lobbyTab==1)DrawModernPlay();
        else if(lobbyTab==2)DrawModernCodex();
        else if(lobbyTab==4){if(loadoutStudio==null)loadoutStudio=new LoadoutStudio();loadoutStudio.Draw(LobbyStudioRect,ref lobbyLoadout,!busy&&!LobbyQueued);}
        else DrawModernConnection();
        DrawModernNavigation();DrawLobbyPlayBar();GUI.enabled=enabled;
        if(lobbyModeOpen)DrawLobbyModePicker();
        if(Event.current.type==EventType.Repaint)lobbyFocusedControl=GUI.GetNameOfFocusedControl();
    }
    void DrawModernNavigation()
    {
        ArenaInterface.Fill(new Rect(0,0,196,944),new Color(.018f,.030f,.051f));
        ArenaInterface.Fill(new Rect(195,0,1,944),new Color(.19f,.32f,.39f,.7f));
        SeasonLobbyArt.Panel(new Rect(22,28,48,48),new Color(.06f,.25f,.28f));
        BattleHudTheme.Label(new Rect(22,28,48,48),"D",31,SeasonLobbyArt.Mint,true,TextAnchor.MiddleCenter);
        BattleHudTheme.Label(new Rect(21,90,166,28),"DITTOCHES",20,SeasonLobbyArt.Cream,true);
        BattleHudTheme.Label(new Rect(23,127,164,20),"TACTICAL LEAGUE",9,BattleHudTheme.Muted,true);
        string[] names={"홈","대전 준비","컬렉션","꾸미기"};string[] captions={"FILE ISLAND","PLAY","COLLECTION","CUSTOMIZE"};int[] tabs={0,1,2,4};
        for(int i=0;i<4;i++)
        {
            Rect r=LobbyNavRect(i);bool active=lobbyTab==tabs[i],hover=GUI.enabled&&r.Contains(Event.current.mousePosition);
            if(active||hover)SeasonLobbyArt.Panel(r,active?new Color(.055f,.18f,.22f):new Color(.05f,.09f,.13f));
            if(active)ArenaInterface.Fill(new Rect(r.x,r.y+12,3,r.height-24),SeasonLobbyArt.Mint);
            BattleHudTheme.Label(new Rect(r.x+16,r.y+12,r.width-28,29),names[i],20,active?SeasonLobbyArt.Cream:BattleHudTheme.Text,true);
            BattleHudTheme.Label(new Rect(r.x+17,r.y+48,r.width-29,16),captions[i],9,active?SeasonLobbyArt.Mint:BattleHudTheme.Muted,true);
            if(GUI.Button(r,GUIContent.none,GUIStyle.none))SetLobbyTab(tabs[i]);
        }
        LobbyVisuals.Rule(new Rect(24,654,148,1),LobbyVisuals.Edge);
        bool connected=token.Length>0&&state!=null;
        if(lobbyTab==3)SeasonLobbyArt.Panel(LobbyConnectionRect,new Color(.055f,.18f,.22f));
        BattleHudTheme.Label(new Rect(36,695,130,25),"서버 연결  ↗",16,lobbyTab==3?SeasonLobbyArt.Mint:BattleHudTheme.Text,true);
        BattleHudTheme.Label(new Rect(37,730,128,19),connected?"● ONLINE":"○ OFFLINE",10,connected?SeasonLobbyArt.Mint:BattleHudTheme.Muted,true);
        if(GUI.Button(LobbyConnectionRect,GUIContent.none,GUIStyle.none))SetLobbyTab(3);
        BattleHudTheme.Label(new Rect(23,843,154,20),"나의 테이머",10,BattleHudTheme.Muted);
        BattleHudTheme.FitLabel(new Rect(23,871,154,31),state==null?nickname:state.name,14,SeasonLobbyArt.Cream,true);
        string title=lobbyTab==0?"파일 아일랜드":lobbyTab==1?"대전 준비":lobbyTab==2?"파트너 컬렉션":lobbyTab==4?"나만의 플레이 스타일":"서버 연결";
        string caption=lobbyTab==0?"당신의 다음 전략이 시작되는 곳":lobbyTab==1?"모드를 고르고 전장에 입장하세요":lobbyTab==2?"파트너를 탐색하고 다음 조합을 설계하세요":lobbyTab==4?"테이머 · 아레나 · 처형 효과":"친구들과 같은 전장에서 만나세요";
        BattleHudTheme.Label(new Rect(228,28,LobbyWorkspace?714:980,42),title,30,SeasonLobbyArt.Cream,true);
        if(LobbyQueued&&LobbyWorkspace)
        {
            int wait=Mathf.Max(0,(int)(Time.unscaledTime-lobbyQueueSince));
            caption="대전 탐색 중  ·  "+state.waiting+" / 8명  ·  "+(wait/60).ToString("00")+":"+(wait%60).ToString("00")+"  ·  전원 참가 시 자동 시작";
        }
        BattleHudTheme.Label(new Rect(231,78,LobbyWorkspace?714:980,22),caption,13,LobbyQueued&&LobbyWorkspace?SeasonLobbyArt.Mint:BattleHudTheme.Muted);
        if(!LobbyWorkspace){BattleHudTheme.Label(new Rect(1230,37,346,22),"FILE ISLAND / 01",12,SeasonLobbyArt.Mint,true,TextAnchor.MiddleRight);BattleHudTheme.Label(new Rect(1230,76,346,20),"8인 오토 배틀 전략",11,BattleHudTheme.Muted,false,TextAnchor.MiddleRight);}
    }
    void DrawLobbyPlayBar()
    {
        bool online=token.Length>0&&state!=null,outdated=online&&(state.leagueVersion<1||!LobbyTamerSupported);
        int seconds=LobbyQueued?Mathf.Max(0,(int)(Time.unscaledTime-lobbyQueueSince)):0;
        if(!LobbyWorkspace)
        {
            LobbyVisuals.FeatureCard(new Rect(1228,120,348,804),0,false);
            BattleHudTheme.Label(new Rect(1250,145,302,22),"READY TO PLAY",11,SeasonLobbyArt.Mint,true);
            var portrait=TamerLoadout.Portrait(lobbyLoadout.tamer);if(portrait!=null)GUI.DrawTexture(new Rect(1247,191,90,89),portrait,ScaleMode.ScaleToFit,true);
            BattleHudTheme.FitLabel(new Rect(1348,203,201,29),state==null?nickname:state.name,16,SeasonLobbyArt.Cream,true);
            BattleHudTheme.Label(new Rect(1350,245,198,25),online?state.rating+" RP":"오프라인",12,online?SeasonLobbyArt.Mint:BattleHudTheme.Muted);
            LobbyVisuals.Rule(new Rect(1250,305,304,1),LobbyVisuals.Edge);
            BattleHudTheme.Label(new Rect(1250,324,304,22),"플레이 모드",11,BattleHudTheme.Muted,true);
            SeasonLobbyArt.Panel(LobbyModeOpenRect,new Color(.047f,.095f,.14f));
            BattleHudTheme.Label(new Rect(1268,371,269,28),LobbyModes[lobbyMode]+"  ▾",lobbyMode==3?18:21,SeasonLobbyArt.Cream,true);
            BattleHudTheme.Label(new Rect(1269,412,266,18),LobbyQueued?"대기 중 · 모드 고정":LobbyModeTags[lobbyMode],11,LobbyQueued?SeasonLobbyArt.Mint:BattleHudTheme.Muted);
            BattleHudTheme.Label(new Rect(1250,481,304,29),LobbyQueued?"테이머를 찾고 있어요":lobbyMode==3?"30개의 시험에 도전하세요":lobbyMode==0?"자유롭게 연습하세요":online?"입장 준비 완료":"서버 연결이 필요해요",19,SeasonLobbyArt.Cream,true);
            string info=LobbyQueued?state.waiting+" / 8명 참여 · "+(seconds/60).ToString("00")+":"+(seconds%60).ToString("00"):lobbyMode==3?"세 가지 난이도 · 세 번의 기회\n크립과 보스를 넘어 시험을 완주하세요.":lobbyMode==0?"AI 테이머 7명과 함께\n나만의 속도로 조합을 완성하세요.":online?"같은 모드에 8명이 모이면\n리그가 자동으로 시작됩니다.":"서버에 연결한 뒤\n8인 일반 · 랭크 리그에 참여하세요.";
            BattleHudTheme.Label(new Rect(1252,530,297,62),info,14,BattleHudTheme.Muted);
            if(LobbyQueued)
            {
                for(int i=0;i<8;i++)ArenaInterface.Fill(new Rect(1252+i*38,620,30,5),i<state.waiting?SeasonLobbyArt.Mint:new Color(.13f,.22f,.29f));
                BattleHudTheme.Label(new Rect(1250,652,304,50),"탐색 중에도 컬렉션과\n팀 계획을 살펴볼 수 있습니다.",12,BattleHudTheme.Muted);
            }
            else
            {
                BattleHudTheme.Label(new Rect(1252,618,299,25),"현재 팀 계획  ·  "+TeamPlan.Units.Count+" / 9",12,SeasonLobbyArt.Cream,true);
                var plannedUnits=TeamPlan.Units.ToArray();
                if(outdated&&!LobbyOffline)BattleHudTheme.Label(new Rect(1252,656,298,48),"현재 외형과 8인 리그를 지원하는\n서버로 업데이트해 주세요.",12,BattleHudTheme.Muted);
                else if(plannedUnits.Length>0)for(int i=0;i<Mathf.Min(5,plannedUnits.Length);i++)FaithfulPortraits.Draw(new Rect(1250+i*61,657,54,54),plannedUnits[i],true);
                else BattleHudTheme.Label(new Rect(1252,656,298,48),"컬렉션에서 파트너를 선택해\n나만의 조합을 준비하세요.",12,BattleHudTheme.Muted);
            }
        }
        else
        {
            SeasonLobbyArt.Panel(LobbyModeOpenRect,new Color(.04f,.075f,.11f));
            BattleHudTheme.Label(LobbyModeOpenRect,LobbyModes[lobbyMode]+"  ▾",15,SeasonLobbyArt.Cream,true,TextAnchor.MiddleCenter);
        }
        if(GUI.Button(LobbyModeOpenRect,GUIContent.none,GUIStyle.none)&&!LobbyQueued&&!busy)lobbyModeOpen=true;
        if(SeasonLobbyArt.Tab(LobbyPlannerRect,"팀 계획  ↗",false,true,16))OpenTeamPlan();
        string action=LobbyQueued?"탐색 취소":recoveringLogin&&!LobbyOffline?"연결 복구 중":connectionError&&!LobbyOffline?"연결 확인":lobbyTab==0?"플레이  →":LobbyOffline?"플레이  →":!online?lobbyTab==3?"연결 정보 입력  ←":"온라인 연결":outdated?"업데이트 필요":"대전 찾기  →";
        bool available=!busy&&(!recoveringLogin||LobbyOffline)&&(!connectionError||!LobbyQueued)&&(LobbyQueued||!outdated||LobbyOffline);
        if(SeasonLobbyArt.Play(LobbyPrimaryRect,action,available,LobbyQueued))
        {
            if(LobbyQueued)Send("/cancel");else if(connectionError&&!LobbyOffline)SetLobbyTab(3);else if(lobbyTab==0)SetLobbyTab(1);
            else if(LobbyOffline){DisposeLobbyShowcase();EnterSolo(lobbyMode==3);}else if(!online){if(lobbyTab==3)GUI.FocusControl("lobby-server");else SetLobbyTab(3);}else Send("/queue",new Command{mode=lobbyMode==1?"normal8":"ranked8"});
        }
    }
    void DrawSeasonScene(bool preparing)
    {
        Rect scene=new Rect(222,120,986,614);
        SeasonLobbyArt.Background(scene,lobbyBackground,.12f);
        SeasonLobbyArt.Shade(new Rect(222,120,686,614),new Color(.008f,.024f,.042f,.96f));
        SeasonLobbyArt.Shade(new Rect(222,544,986,190),new Color(.012f,.03f,.05f,.92f),true,true);
        ArenaInterface.Fill(new Rect(scene.x,scene.y,scene.width,1),new Color(.38f,.66f,.68f,.4f));
        Rect view=new Rect(516,266,656,468);
        if(lobbyShowcase==null)lobbyShowcase=new TacticalArena(view,true,false);
        if(Event.current.type==EventType.Repaint)
        {
            lobbyShowcase.BeginFrame(-1,-1,-1,false);lobbyShowcase.DrawSeasonLobbyPortrait();
            lobbyShowcase.SetSeasonLobbyTamer(lobbyLoadout.tamer,Time.unscaledTime);lobbyShowcase.Render();
        }
        Vector2 ground=lobbyShowcase.Project(new Vector3(0,.05f,0));
        BattleHudTheme.GroundShadow(new Rect(ground.x-85,ground.y-12,170,32),.42f);
        GUI.DrawTexture(view,lobbyShowcase.Texture,ScaleMode.StretchToFill,true);
        if(!preparing)
        {
            BattleHudTheme.FitLabel(new Rect(673,616,445,32),TamerLoadout.Tamers[lobbyLoadout.tamer],23,SeasonLobbyArt.Cream,true);
            BattleHudTheme.Label(new Rect(675,658,437,22),"EQUIPPED TAMER  /  나의 테이머",10,SeasonLobbyArt.Mint,true);
        }
        SeasonLobbyArt.Panel(new Rect(222,750,986,174),new Color(.024f,.047f,.073f,.96f));
        BattleHudTheme.Label(new Rect(243,760,785,24),"나의 장착 구성",13,SeasonLobbyArt.Cream,true);
        BattleHudTheme.Label(new Rect(951,760,236,24),"선택해서 꾸미기  ↗",11,BattleHudTheme.Muted,false,TextAnchor.MiddleRight);
        for(int i=0;i<3;i++)DrawLobbyLoadoutTile(LobbyLoadoutRect(i),i);
    }
    void DrawModernHome()
    {
        DrawSeasonScene(false);
        BattleHudTheme.Label(new Rect(257,154,542,23),"EXPLORE THE FILE ISLAND",12,SeasonLobbyArt.Mint,true);
        BattleHudTheme.Label(new Rect(250,208,510,156),"전략이\n진화하는 곳",57,SeasonLobbyArt.Cream,true);
        BattleHudTheme.Label(new Rect(259,402,351,69),"파트너를 모으고, 시너지를 연결하세요.\n당신만의 조합이 다음 승리를 만듭니다.",16,new Color(.80f,.88f,.93f));
        LobbyVisuals.Rule(new Rect(258,509,300,1),new Color(.48f,.71f,.73f,.65f));
        BattleHudTheme.Label(new Rect(258,539,130,43),catalog.units.Length.ToString("00"),34,SeasonLobbyArt.Cream,true);
        BattleHudTheme.Label(new Rect(413,539,160,43),"08",34,SeasonLobbyArt.Cream,true);
        BattleHudTheme.Label(new Rect(261,589,134,22),"PARTNERS",10,SeasonLobbyArt.Mint,true);
        BattleHudTheme.Label(new Rect(416,589,144,22),"TAMERS / LEAGUE",10,SeasonLobbyArt.Mint,true);
        Rect explore=new Rect(255,658,331,42);BattleHudTheme.Label(explore,"파트너 컬렉션 탐색  ↗",15,SeasonLobbyArt.Cream,true);
        if(GUI.Button(explore,GUIContent.none,GUIStyle.none))SetLobbyTab(2);
    }
    void DrawModernPlay()
    {
        DrawSeasonScene(true);
        BattleHudTheme.Label(new Rect(257,154,528,23),LobbyQueued?"MATCHMAKING IN PROGRESS":"YOUR NEXT BATTLE",12,SeasonLobbyArt.Mint,true);
        BattleHudTheme.Label(new Rect(250,211,590,82),LobbyQueued?"함께할 테이머":LobbyModes[lobbyMode],lobbyMode==3?37:49,SeasonLobbyArt.Cream,true);
        BattleHudTheme.Label(new Rect(258,319,354,74),LobbyQueued?"같은 전장을 향하는\n테이머들을 찾고 있습니다.":lobbyMode==3?"아구몬박사가 준비한 30개의 시험.\n크립 무리와 여섯 보스에 도전하세요.":lobbyMode==0?"실험하고, 배우고, 다시 도전하세요.\n일곱 AI 테이머가 함께합니다.":"새로운 조합으로 전장에 도전하세요.\n여덟 명이 모이면 리그가 시작됩니다.",16,new Color(.80f,.88f,.93f));
        var units=TeamPlan.Units.ToArray();
        BattleHudTheme.Label(new Rect(258,422,316,23),"나의 팀 계획  /  "+(TeamPlan.Data.active+1),12,SeasonLobbyArt.Mint,true);
        if(units.Length>0)for(int i=0;i<Mathf.Min(5,units.Length);i++)FaithfulPortraits.Draw(new Rect(258+i*61,464,53,58),units[i],true);
        else BattleHudTheme.Label(new Rect(259,462,315,55),"좋아하는 파트너를 팀 계획에 담아\n다음 전투를 준비하세요.",13,BattleHudTheme.Muted);
        if(lobbyMode==3)
        {
            SeasonLobbyArt.Panel(new Rect(242,606,946,110),new Color(.018f,.044f,.064f,.93f));
            BattleHudTheme.Label(new Rect(263,610,680,26),"여섯 관문 · 30개의 크립 전투",17,SeasonLobbyArt.Cream,true);
            BattleHudTheme.Label(new Rect(867,614,298,22),"SOLO PVE / 3 LIVES",11,SeasonLobbyArt.Mint,true,TextAnchor.MiddleRight);
            for(int i=0;i<6;i++){Rect c=new Rect(263+i*151,650,137,49);SeasonLobbyArt.Panel(c,new Color(.048f,.13f,.16f));BattleHudTheme.Label(c,"관문 0"+(i+1)+"  /  "+((i+1)*5),14,SeasonLobbyArt.Cream,true,TextAnchor.MiddleCenter);}
            return;
        }
        int joined=LobbyQueued?Mathf.Clamp(state.waiting,0,8):1;
        SeasonLobbyArt.Panel(new Rect(242,606,946,110),new Color(.018f,.044f,.064f,.93f));
        BattleHudTheme.Label(new Rect(263,610,680,26),LobbyQueued?joined+" / 8 테이머 참여":"대전 준비",17,SeasonLobbyArt.Cream,true);
        BattleHudTheme.Label(new Rect(867,614,298,22),LobbyQueued?"전원 참가 시 자동 시작":lobbyMode==0?"SOLO / AI 7":"ONLINE / 8",11,SeasonLobbyArt.Mint,true,TextAnchor.MiddleRight);
        for(int i=0;i<8;i++)
        {
            Rect slot=new Rect(262+i*113,646,95,56);SeasonLobbyArt.Panel(slot,i<joined?new Color(.075f,.22f,.25f):new Color(.033f,.071f,.099f));
            if(i==0){var portrait=TamerLoadout.Portrait(lobbyLoadout.tamer);if(portrait!=null)GUI.DrawTexture(new Rect(slot.x+2,slot.y+3,49,50),portrait,ScaleMode.ScaleToFit,true);}
            BattleHudTheme.Label(new Rect(slot.x+(i==0?53:7),slot.y+4,i==0?37:81,25),(i+1).ToString("00"),17,i<joined?SeasonLobbyArt.Mint:BattleHudTheme.Muted,true,i==0?TextAnchor.MiddleLeft:TextAnchor.MiddleCenter);
            BattleHudTheme.Label(new Rect(slot.x+(i==0?52:4),slot.y+31,i==0?40:87,19),i==0?"나":LobbyQueued&&i<joined?"참여":lobbyMode==0?"AI":"대기",10,BattleHudTheme.Muted,false,i==0?TextAnchor.MiddleLeft:TextAnchor.MiddleCenter);
        }
    }
    void OpenLobbyLoadout(int tab){SetLobbyTab(4);if(loadoutStudio==null)loadoutStudio=new LoadoutStudio();loadoutStudio.Preview(lobbyLoadout,tab);}
    void DrawLobbyLoadoutTile(Rect r,int tab)
    {
        bool hover=GUI.enabled&&r.Contains(Event.current.mousePosition);
        SeasonLobbyArt.Panel(r,hover?new Color(.068f,.16f,.19f):new Color(.036f,.073f,.102f));
        if(hover)ArenaInterface.Fill(new Rect(r.x,r.y,2,r.height),SeasonLobbyArt.Mint);
        if(tab==0){var t=TamerLoadout.Portrait(lobbyLoadout.tamer);if(t!=null)GUI.DrawTexture(new Rect(r.x+8,r.y+14,75,78),t,ScaleMode.ScaleToFit,true);}
        else ArenaPresentationUI.CosmeticGlyph(new Rect(r.x+17,r.y+31,54,54),tab,tab==1?lobbyLoadout.field:lobbyLoadout.finisher,tab==1?TamerLoadout.FieldColor(lobbyLoadout.field):TamerLoadout.FinishColor(lobbyLoadout.finisher));
        BattleHudTheme.Label(new Rect(r.x+97,r.y+18,r.width-112,22),tab==0?"테이머":tab==1?"아레나":"처형 효과",11,BattleHudTheme.Muted);
        BattleHudTheme.FitLabel(new Rect(r.x+96,r.y+51,r.width-109,29),tab==0?TamerLoadout.Tamers[lobbyLoadout.tamer]:tab==1?TamerLoadout.Fields[lobbyLoadout.field]:TamerLoadout.Finishers[lobbyLoadout.finisher],16,SeasonLobbyArt.Cream,true);
        if(GUI.Button(r,GUIContent.none,GUIStyle.none))OpenLobbyLoadout(tab);
    }
    void DrawLobbyModePicker()
    {
        ArenaInterface.Fill(new Rect(0,0,1600,944),new Color(.006f,.015f,.027f,.97f));
        LobbyVisuals.Eyebrow(new Rect(270,112,1100,23),"CHOOSE YOUR BATTLE");
        BattleHudTheme.Label(new Rect(264,154,1160,49),"어떤 전장으로 향할까요?",33,SeasonLobbyArt.Cream,true);
        string[] ids={"agumon","metalgarurumon","wargreymon","agumon"};
        string[] copy={"실험하고, 배우고, 성장하세요.","새로운 조합을 시험하세요.","마지막 테이머로 실력을 증명하세요.","크립과 보스를 넘어 시험을 완주하세요."};
        string[] rules={"서버 없이 플레이 · AI 테이머 7명","8인 생존 · RP 변동 없음","8인 생존 · 최종 순위로 RP 정산","30개 전투 · 3단계 난이도 · 목숨 3개"};
        for(int i=0;i<4;i++)
        {
            Rect r=LobbyModeRect(i);bool selected=i==lobbyMode;
            LobbyVisuals.FeatureCard(r,i,selected);CharacterCardArt.Stage(new Rect(r.x+1,r.y+1,203,r.height-2),i+2);
            FaithfulPortraits.Draw(new Rect(r.x+3,r.y+32,203,224),ids[i]);
            BattleHudTheme.Badge(new Rect(r.x+18,r.y+17,selected?86:56,26),selected?"선택 중":"0"+(i+1),selected?SeasonLobbyArt.Mint:BattleHudTheme.Muted,12);
            BattleHudTheme.Label(new Rect(r.x+226,r.y+28,365,34),LobbyModes[i],26,SeasonLobbyArt.Cream,true);
            BattleHudTheme.Label(new Rect(r.x+228,r.y+76,358,20),LobbyModeTags[i],11,SeasonLobbyArt.Mint,true);
            BattleHudTheme.Label(new Rect(r.x+228,r.y+117,358,48),copy[i],15,BattleHudTheme.Muted);
            BattleHudTheme.Label(new Rect(r.x+228,r.y+169,358,27),rules[i],12,BattleHudTheme.Muted);
            SeasonLobbyArt.Panel(new Rect(r.x+226,r.y+217,362,33),selected?new Color(.075f,.27f,.29f):new Color(.06f,.11f,.16f));
            BattleHudTheme.Label(new Rect(r.x+228,r.y+220,358,26),selected?"현재 모드  ✓":"이 모드 선택  →",14,selected?SeasonLobbyArt.Mint:SeasonLobbyArt.Cream,true,TextAnchor.MiddleCenter);
            if(GUI.Button(r,GUIContent.none,GUIStyle.none)){SetLobbyMode(i);lobbyModeOpen=false;}
        }
        if(onlineInterface.Button(LobbyModeCloseRect,new GUIContent("×"),true,false,21)||(GUI.enabled&&Event.current.type==EventType.KeyDown&&Event.current.keyCode==KeyCode.Escape)){lobbyModeOpen=false;Event.current.Use();}
        BattleHudTheme.Label(new Rect(270,834,1208,27),"모드를 선택한 뒤 플레이 버튼으로 시작하세요.  ·  ESC 닫기",13,BattleHudTheme.Muted,false,TextAnchor.MiddleCenter);
    }
    void DrawModernConnection()
    {
        LobbyVisuals.FeatureCard(new Rect(222,120,986,804),0,false);
        SeasonLobbyArt.Background(new Rect(748,121,459,802),lobbyBackground,.46f);
        SeasonLobbyArt.Shade(new Rect(748,121,459,802),new Color(.009f,.028f,.047f,.78f));
        BattleHudTheme.Label(new Rect(264,155,450,23),"CONNECT / ONLINE LEAGUE",11,SeasonLobbyArt.Mint,true);
        BattleHudTheme.Label(new Rect(258,212,470,91),"같은 전장,\n새로운 라이벌",34,SeasonLobbyArt.Cream,true);
        BattleHudTheme.Label(new Rect(265,330,449,49),"서버 주소와 테이머 이름을 입력하세요.\n연결 후 일반 · 랭크 리그에 참여할 수 있습니다.",14,BattleHudTheme.Muted);
        BattleHudTheme.Label(new Rect(264,414,450,24),"서버 주소",13,SeasonLobbyArt.Cream,true);
        BattleHudTheme.Label(new Rect(264,516,450,24),"테이머 이름",13,SeasonLobbyArt.Cream,true);
        bool enabled=GUI.enabled;GUI.enabled=enabled&&!busy&&token.Length==0&&!recoveringLogin;
        server=SeasonLobbyArt.Input(new Rect(264,450,450,49),"lobby-server",server,200,input);nickname=SeasonLobbyArt.Input(new Rect(264,552,450,49),"lobby-nickname",nickname,20,input);GUI.enabled=enabled;
        if(recoveringLogin){if(onlineInterface.Button(LobbyServerActionRect,new GUIContent("연결 시도 중단"),true,false,17)){token="";state=null;ResetConnectionRecovery();notice="연결 시도를 중단했습니다.";}}
        else if(token.Length==0){if(SeasonLobbyArt.Play(LobbyServerActionRect,busy?"연결 중…":"서버 연결  →",!busy,false))Connect();}
        else if(onlineInterface.Button(LobbyServerActionRect,new GUIContent("접속 해제"),!busy&&!LobbyQueued,false,17)){token="";state=null;ResetConnectionRecovery();notice="접속을 해제했습니다.";}
        BattleHudTheme.FitLabel(new Rect(264,713,450,69),recoveringLogin?"기존 계정으로 연결을 복구하고 있습니다.":token.Length>0&&state!=null?"연결됨 · "+state.name+"  /  "+state.rating+" RP":notice,14,token.Length>0&&state!=null?SeasonLobbyArt.Mint:BattleHudTheme.Muted,false);
        LobbyVisuals.Rule(new Rect(264,822,450,1),LobbyVisuals.Edge);
        BattleHudTheme.Label(new Rect(265,844,449,38),"대기열 참여와 취소는 오른쪽 패널에서 진행합니다.",12,BattleHudTheme.Muted);
        BattleHudTheme.Label(new Rect(782,160,391,23),"처음 연결하나요?",18,SeasonLobbyArt.Cream,true);
        string[] headings={"서버를 열어주세요","같은 주소로 모여주세요","모드를 맞춰주세요"};
        string[] descriptions={"이 PC에서 연습\n05_온라인서버실행.bat\n\n다른 PC와 함께\n06_8인서버실행.bat","이 PC의 서버\nhttp://127.0.0.1:7777\n\n다른 PC에서는 서버 PC의\nIP 주소를 입력하세요.","일반 또는 랭크를 선택하고\n대전 찾기를 누르세요.\n\n같은 모드에 8명이 모이면\n리그가 자동으로 시작됩니다."};
        for(int i=0;i<3;i++)
        {
            float y=221+i*219;BattleHudTheme.Badge(new Rect(782,y,35,30),"0"+(i+1),SeasonLobbyArt.Mint,13);
            BattleHudTheme.Label(new Rect(832,y-1,348,31),headings[i],17,SeasonLobbyArt.Cream,true);
            BattleHudTheme.Label(new Rect(832,y+47,345,127),descriptions[i],13,new Color(.77f,.86f,.91f));
            if(i<2)LobbyVisuals.Rule(new Rect(783,y+196,389,1),new Color(.24f,.41f,.46f,.8f));
        }
    }
}
