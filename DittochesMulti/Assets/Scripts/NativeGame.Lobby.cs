using UnityEngine;

public sealed partial class NativeGame
{
    bool confirmSoloReset;
    static readonly Rect SoloLobbyEnterRect=new Rect(1458,978,420,66),SoloLobbyResetRect=new Rect(966,989,222,46);
    static readonly Rect SoloResetCancelRect=new Rect(652,643,260,54),SoloResetConfirmRect=new Rect(941,643,324,54);
    static Rect SoloDifficultyRect(int index){return new Rect(44,277+index*119,252,99);}
    void DrawModernSoloLobby()
    {
        if(TrialsMode){DrawTrialsLobby();return;}
        ArenaInterface.Fill(new Rect(0,0,1920,1080),BattleHudTheme.Background);
        SeasonLobbyArt.Background(new Rect(0,0,1920,1080),lobbyBackground,.88f);
        bool enabled=GUI.enabled;GUI.enabled=enabled&&!confirmSoloReset;
        ArenaInterface.Fill(new Rect(0,0,320,960),BattleHudTheme.Panel);
        BattleHudTheme.Label(new Rect(36,26,266,38),"DITTOCHES",28,BattleHudTheme.Text,true);
        BattleHudTheme.Label(new Rect(38,71,240,22),"FILE ISLAND / SOLO",12,BattleHudTheme.Teal,true);
        if(arenaInterface.Button(new Rect(38,124,252,44),new GUIContent("←  대전 로비로"),true,false,16)){var launcher=FindAnyObjectByType<MultiLauncher>();if(launcher!=null)launcher.ReturnToMulti();return;}
        BattleHudTheme.Label(new Rect(44,216,252,26),"플레이 난이도",21,null,true);
        BattleHudTheme.Label(new Rect(45,248,252,20),"AI 테이머 7명과 함께하는 리그",12,BattleHudTheme.Muted);
        string[] notes={"기본 규칙부터 천천히","표준 전투로 실력을 쌓으세요","강한 상대에게 도전하세요"};
        string[] powers={"AI 전투력 88%","AI 전투력 100%","AI 전투력 112%"};
        for(int i=0;i<3;i++)
        {
            Rect card=SoloDifficultyRect(i);bool selected=difficulty==i;
            BattleHudTheme.Surface(card,selected,selected?BattleHudTheme.Teal:(Color?)null);
            BattleHudTheme.Label(new Rect(card.x+16,card.y+11,card.width-32,27),Difficulties[i]+(selected?"  ✓":""),21,selected?BattleHudTheme.Teal:BattleHudTheme.Text,true);
            BattleHudTheme.Label(new Rect(card.x+16,card.y+46,card.width-32,21),notes[i],13,BattleHudTheme.Muted);
            BattleHudTheme.Label(new Rect(card.x+16,card.y+70,card.width-32,19),powers[i],11,BattleHudTheme.Muted);
            if(GUI.Button(card,GUIContent.none,GUIStyle.none))difficulty=i;
        }
        BattleHudTheme.Section(new Rect(44,675,246,140),"HOW TO PLAY","나만의 조합으로","모집하고, 배치하고, 전투하세요.");
        BattleHudTheme.Label(new Rect(44,785,242,100),"01   디지몬 모집\n\n02   시너지와 장비 구성\n\n03   마지막 테이머까지 생존",14,BattleHudTheme.Muted);
        BattleHudTheme.Label(new Rect(352,27,1100,25),"PLAY / SOLO LEAGUE",12,BattleHudTheme.Teal,true);
        BattleHudTheme.Label(new Rect(350,63,1050,48),"여정을 준비하세요",36,BattleHudTheme.Text,true);
        BattleHudTheme.Badge(new Rect(1529,53,348,42),hp<=0?"지난 리그 종료":round>1?"진행 중  ·  "+RoundLabel()+"  /  "+hp+" HP":"새 여정  ·  100 HP",hp<=0?BattleHudTheme.Gold:BattleHudTheme.Teal,16);
        if(tamerLoadout==null)ApplyEquippedLoadout();if(tamerStudio==null)tamerStudio=new LoadoutStudio();
        if(tamerStudio.Draw(new Rect(348,136,1530,802),ref tamerLoadout)){legend=tamerLoadout.tamer;Save();}
        ArenaInterface.Fill(new Rect(0,960,1920,120),BattleHudTheme.Panel);LobbyVisuals.Rule(new Rect(0,960,1920,1),BattleHudTheme.Line);
        BattleHudTheme.Label(new Rect(43,978,780,32),Difficulties[difficulty]+" · 솔로 리그",23,null,true);
        BattleHudTheme.Label(new Rect(44,1020,780,25),"준비한 편성으로 직접 전투를 시작합니다  ·  진행 상황 자동 저장",14,BattleHudTheme.Muted);
        if(SeasonLobbyArt.Play(SoloLobbyEnterRect,hp<=0?"새로운 리그  →":round>1?"이어서 플레이  →":"솔로 리그 시작  →",true,false)){if(hp<=0)confirmSoloReset=true;else {lobby=false;Save();}}
        if(arenaInterface.Button(new Rect(1210,989,220,46),new GUIContent("팀 계획  ↗"),true,false,16))OpenTeamPlan();
        if(arenaInterface.Button(SoloLobbyResetRect,new GUIContent("새 게임 시작…"),true,false,15)){confirmSoloReset=true;GUI.FocusControl(null);}
        GUI.enabled=enabled;
        if(confirmSoloReset)DrawSoloResetConfirmation();
    }
    void DrawSoloResetConfirmation()
    {
        ArenaInterface.Fill(new Rect(0,0,1920,1080),new Color(.003f,.014f,.018f,.86f));
        BattleHudTheme.Surface(new Rect(587,342,746,407),true,BattleHudTheme.Gold);
        LobbyVisuals.Rule(new Rect(633,370,651,2),SeasonLobbyArt.Mint);
        BattleHudTheme.Label(new Rect(636,397,649,30),"NEW JOURNEY",14,SeasonLobbyArt.Mint,true);
        BattleHudTheme.Label(new Rect(632,446,660,46),"새로운 리그를 시작할까요?",32,SeasonLobbyArt.Cream,true);
        BattleHudTheme.Label(new Rect(637,512,646,73),"현재 리그의 편성·골드·라운드는 초기화됩니다.\n장착한 외형과 팀 계획은 그대로 유지됩니다.",18,BattleHudTheme.Muted);
        BattleHudTheme.Label(new Rect(637,595,646,27),RoundLabel()+"  ·  "+hp+" HP  ·  "+gold+" G",16,LobbyVisuals.Gold,true);
        if(SeasonLobbyArt.Tab(SoloResetCancelRect,"계속 준비하기  [ESC]",false))confirmSoloReset=false;
        if(SeasonLobbyArt.Play(SoloResetConfirmRect,"초기화하고 시작",true,false)){confirmSoloReset=false;ResetGame();lobby=false;}
        Event e=Event.current;
        if(GUI.enabled&&e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape){confirmSoloReset=false;e.Use();}
        if(e.type==EventType.MouseDown||e.type==EventType.MouseUp||e.type==EventType.ScrollWheel)e.Use();
    }
}
