#if DITTOCHES_PORTABLE_PREVIEW
using PlayerPrefs = PortablePreviewPrefs;
#endif
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    bool confirmTrialReset;
    GUIStyle trialWrap;
    static readonly Rect TrialEnterRect=new Rect(1458,978,420,66);
    static Rect TrialLobbyEnemyRect(int index,int count)
    {float width=432f/Mathf.Max(4,count);return new Rect(1420+index*width,345,width-4,127);}
    void TrialParagraph(Rect r,string text,int size=15,Color? color=null)
    {
        if(trialWrap==null)trialWrap=new GUIStyle(GUI.skin.label){wordWrap=true,padding=new RectOffset(0,0,0,0)};
        trialWrap.fontSize=size;trialWrap.normal.textColor=color??BattleHudTheme.Muted;GUI.Label(r,text,trialWrap);
    }
    string TrialRewardText(TrialsCatalog.Wave wave)
    {return wave.gold+" G  ·  "+wave.xp+" XP"+(wave.item>=0?"  ·  "+ItemNames[wave.item]:"");}
    void DrawTrialsLobby()
    {
        ArenaInterface.Fill(new Rect(0,0,1920,1080),BattleHudTheme.Background);
        SeasonLobbyArt.Background(new Rect(320,0,1600,960),lobbyBackground,.72f);
        bool enabled=GUI.enabled;GUI.enabled=enabled&&!confirmTrialReset;
        ArenaInterface.Fill(new Rect(0,0,320,960),BattleHudTheme.Panel);
        BattleHudTheme.Label(new Rect(36,26,266,38),"DITTOCHES",28,BattleHudTheme.Text,true);
        BattleHudTheme.Label(new Rect(38,71,240,22),"AGUMON'S TRIALS / PVE",11,BattleHudTheme.Teal,true);
        if(arenaInterface.Button(new Rect(38,124,252,44),new GUIContent("←  플레이 모드로"),true,false,16)){FindAnyObjectByType<MultiLauncher>().ReturnToMulti();return;}
        BattleHudTheme.Label(new Rect(44,216,252,26),"시험 난이도",21,null,true);
        BattleHudTheme.Label(new Rect(45,248,252,20),"각 난이도의 진행을 따로 저장합니다",12,BattleHudTheme.Muted);
        string[] notes={"첫 조합을 완성하는 여유로운 도전","배치와 장비가 중요한 정규 시험","강화된 크립과 보스의 최종 시험"};
        for(int i=0;i<3;i++)
        {
            Rect card=SoloDifficultyRect(i);bool selected=TrialDifficulty==i;
            BattleHudTheme.Surface(card,selected,selected?BattleHudTheme.Teal:(Color?)null);
            BattleHudTheme.Label(new Rect(card.x+16,card.y+11,card.width-32,27),TrialsCatalog.DifficultyNames[i]+(selected?"  ✓":""),21,selected?BattleHudTheme.Teal:BattleHudTheme.Text,true);
            TrialParagraph(new Rect(card.x+16,card.y+46,card.width-32,43),notes[i],12);
            if(GUI.Button(card,GUIContent.none,GUIStyle.none)&&!selected){EnterTrials(i);PlayerPrefs.SetInt("multi.trials.difficulty",i);PlayerPrefs.Save();}
        }
        BattleHudTheme.Label(new Rect(44,680,246,27),"준비 시간은 무제한",19,SeasonLobbyArt.Cream,true);
        TrialParagraph(new Rect(44,721,242,82),"시험 번호를 눌러 적의 배치와\n개별 능력치를 정찰하세요.\n준비 중 진행은 자동 저장됩니다.",14);
        DrawTrialLobbyRecord(new Rect(44,823,242,109));
        BattleHudTheme.Label(new Rect(352,27,1100,25),"PLAY / PVE CHALLENGE",12,BattleHudTheme.Teal,true);
        BattleHudTheme.Label(new Rect(350,66,1050,48),"아구몬박사의 시험",38,BattleHudTheme.Text,true);
        bool ended=TrialComplete||TrialFailed;
        BattleHudTheme.Badge(new Rect(1484,54,392,42),TrialComplete?"시험 합격 · 30개 전투 완료":TrialFailed?"시험 종료 · 다시 도전하세요":"진행 "+TrialStage.ToString("00")+" / 30   ·   남은 목숨 "+TrialLives,BattleHudTheme.Teal,16);
        BattleHudTheme.Surface(new Rect(350,140,1024,300));
        BattleHudTheme.Label(new Rect(384,169,700,25),"DR. AGUMON'S FIELD LAB",12,BattleHudTheme.Teal,true);
        BattleHudTheme.Label(new Rect(382,217,682,88),"완벽한 조합은\n시험 속에서 태어난다!",33,BattleHudTheme.Text,true);
        TrialParagraph(new Rect(385,330,620,76),"야생 크립 무리부터 보스 군단까지.\n전투마다 달라지는 적을 읽고 30번째 시험을 통과하세요.",16);
        FaithfulPortraits.Draw(new Rect(1065,167,289,269),"agumon");
        string[] ruleTitles={"30개의 전투","세 번의 기회","승리할수록 성장"};
        string[] ruleText={"12종의 적 · 서로 다른 보스 6종\n편성에 맞춰 배치와 장비를 바꾸세요","패배하면 목숨 -1, 다음 시험으로\n시간 초과도 패배로 판정","전투 후 골드와 경험치 지급\n승리 보상으로 장비를 모으세요"};
        for(int i=0;i<3;i++){Rect c=new Rect(350+i*345,461,334,126);BattleHudTheme.Surface(c);BattleHudTheme.Label(new Rect(c.x+21,c.y+16,c.width-42,28),ruleTitles[i],21,BattleHudTheme.Text,true);TrialParagraph(new Rect(c.x+22,c.y+59,c.width-44,52),ruleText[i],14);}
        Rect info=new Rect(1396,140,480,447);BattleHudTheme.Surface(info,true,BattleHudTheme.Gold);
        var wave=CurrentTrial;
        BattleHudTheme.Label(new Rect(info.x+24,info.y+21,432,23),ended?"LAST ENCOUNTER":"NEXT ENCOUNTER",11,BattleHudTheme.Gold,true);
        BattleHudTheme.Label(new Rect(info.x+22,info.y+59,436,35),wave.number.ToString("00")+"  /  "+wave.name,24,BattleHudTheme.Text,true);
        TrialParagraph(new Rect(info.x+24,info.y+108,427,70),wave.description,15);
        BattleHudTheme.Label(new Rect(info.x+24,info.y+177,427,22),"적 "+wave.enemies.Length+"명   ·   제한 "+wave.timeLimit.ToString("0")+"초   ·   "+(wave.boss?"보스 관문":"일반 시험"),12,BattleHudTheme.Gold,true);
        var groups=wave.enemies.GroupBy(e=>e.id).ToArray();
        for(int i=0;i<groups.Length;i++)
        {
            Rect cell=TrialLobbyEnemyRect(i,groups.Length);string id=groups[i].Key;
            FaithfulPortraits.Draw(new Rect(cell.x,cell.y,cell.width,83),id,true);
            BattleHudTheme.Label(new Rect(cell.x,cell.y+83,cell.width,20),TrialEnemyName(id),groups.Length>4?11:12,BattleHudTheme.Text,true,TextAnchor.MiddleCenter);
            BattleHudTheme.Label(new Rect(cell.x,cell.y+105,cell.width,20),"× "+groups[i].Count()+"  ↗",12,BattleHudTheme.Teal,true,TextAnchor.MiddleCenter);
            if(GUI.Button(cell,new GUIContent("",TrialEnemyName(id)+" · 배치와 기술 정찰"),GUIStyle.none))
            {OpenTrialDossier(wave.number);trialScoutedEnemyIndex=System.Array.FindIndex(wave.enemies,e=>e.id==id);}
        }
        BattleHudTheme.Label(new Rect(info.x+24,info.y+342,431,21),"승리 보상",12,BattleHudTheme.Gold,true);
        BattleHudTheme.FitLabel(new Rect(info.x+24,info.y+366,431,27),TrialRewardText(wave),16,BattleHudTheme.Text,false);
        if(arenaInterface.Button(new Rect(info.x+24,info.y+402,431,32),new GUIContent("배치 · 능력치 정찰  ↗"),true,false,14))OpenTrialDossier(wave.number);
        BattleHudTheme.Label(new Rect(351,610,870,24),"TRIAL ROADMAP  /  번호와 보스를 눌러 미리 정찰하세요",13,BattleHudTheme.Teal,true);
        BattleHudTheme.Label(new Rect(1290,610,586,24),"금색 현재   ·   청록 승리   ·   붉은색 패배",12,BattleHudTheme.Muted,false,TextAnchor.MiddleRight);
        for(int chapter=0;chapter<6;chapter++)
        {
            var boss=TrialsCatalog.Waves[chapter*5+4];Rect c=new Rect(350+chapter*257,649,242,225);bool active=wave.chapter==chapter+1;
            BattleHudTheme.Surface(c,active,active?BattleHudTheme.Teal:(Color?)null);
            BattleHudTheme.Label(new Rect(c.x+18,c.y+14,209,23),"관문 0"+(chapter+1),14,active?BattleHudTheme.Teal:BattleHudTheme.Muted,true);
            FaithfulPortraits.Draw(new Rect(c.x+143,c.y+31,85,94),boss.enemies[0].id,true);
            TrialParagraph(new Rect(c.x+19,c.y+64,123,61),boss.name.Replace("의 ","\n"),17,BattleHudTheme.Text);
            if(GUI.Button(new Rect(c.x,c.y,c.width,133),new GUIContent("","보스 "+boss.number+" · "+boss.name+" 정찰"),GUIStyle.none))OpenTrialDossier(boss.number);
            for(int n=0;n<5;n++){int number=chapter*5+n+1;DrawTrialRoadmapNode(new Rect(c.x+18+n*42,c.y+144,35,34),number,false);}
            BattleHudTheme.Label(new Rect(c.x+19,c.y+192,207,18),"BOSS / "+boss.number.ToString("00")+"  ·  정찰 ↗",10,active?BattleHudTheme.Teal:BattleHudTheme.Muted,true);
        }
        BattleHudTheme.Label(new Rect(352,899,1519,23),"30번째 보스를 쓰러뜨리면 합격 · 목숨이 모두 소진되거나 마지막 시험에서 패배하면 종료 · 전투 중 종료는 1패로 기록",13,BattleHudTheme.Muted);
        ArenaInterface.Fill(new Rect(0,960,1920,120),BattleHudTheme.Panel);LobbyVisuals.Rule(new Rect(0,960,1920,1),BattleHudTheme.Line);
        BattleHudTheme.Label(new Rect(43,978,780,32),TrialsCatalog.DifficultyNames[TrialDifficulty]+" · 아구몬박사의 시험",23,null,true);
        BattleHudTheme.Label(new Rect(44,1020,780,25),"점수 "+TrialScore.ToString("N0")+"  ·  승리 "+TrialCleared+"회  ·  남은 목숨 "+TrialLives,14,BattleHudTheme.Muted);
        if(SeasonLobbyArt.Play(TrialEnterRect,ended?"새로운 시험  →":TrialStage>1?"이어서 도전  →":"시험 시작  →",true,false)){if(ended)confirmTrialReset=true;else {lobby=false;Save();}}
        if(arenaInterface.Button(new Rect(1210,989,220,46),new GUIContent("팀 계획  ↗"),true,false,16))OpenTeamPlan();
        if(arenaInterface.Button(SoloLobbyResetRect,new GUIContent("처음부터 도전…"),true,false,15))confirmTrialReset=true;
        GUI.enabled=enabled;if(confirmTrialReset)DrawTrialResetConfirmation();
    }
    void DrawTrialResetConfirmation()
    {
        ArenaInterface.Fill(new Rect(0,0,1920,1080),new Color(.003f,.014f,.018f,.86f));BattleHudTheme.Surface(new Rect(587,342,746,407),true,BattleHudTheme.Gold);
        BattleHudTheme.Label(new Rect(636,389,649,30),"NEW TRIAL / "+TrialsCatalog.DifficultyNames[TrialDifficulty],14,BattleHudTheme.Teal,true);
        BattleHudTheme.Label(new Rect(632,443,660,46),"시험을 처음부터 시작할까요?",30,BattleHudTheme.Text,true);
        TrialParagraph(new Rect(637,517,646,78),"이 난이도의 편성·골드·단계·점수를 초기화합니다.\n다른 난이도의 기록과 솔로 리그 진행은 유지됩니다.",18);
        if(SeasonLobbyArt.Tab(SoloResetCancelRect,"계속 준비하기  [ESC]",false))confirmTrialReset=false;
        if(SeasonLobbyArt.Play(SoloResetConfirmRect,"초기화하고 시작",true,false)){confirmTrialReset=false;RestartTrials();lobby=false;}
        Event e=Event.current;if(e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape){confirmTrialReset=false;e.Use();}if(e.isMouse)e.Use();
    }
    void DrawTrialsHud(float x)
    {
        var wave=CurrentTrial;
        BattleHudTheme.Label(new Rect(x+14,120,190,23),wave.boss?"보스 시험":"이번 시험",18,BattleHudTheme.Gold,true);
        TrialParagraph(new Rect(x+14,160,190,62),wave.name,21,BattleHudTheme.Text);
        TrialParagraph(new Rect(x+14,226,189,85),wave.description,13);
        var groups=wave.enemies.GroupBy(e=>e.id).ToArray();
        for(int i=0;i<Mathf.Min(4,groups.Length);i++)
        {
            float y=321+i*56;var enemy=groups[i].First();FaithfulPortraits.Draw(new Rect(x+9,y,57,51),enemy.id,true);
            var unit=TeamPlan.Roster.FirstOrDefault(u=>u.id==enemy.id);int index=System.Array.IndexOf(DigimonModelLibrary.Roster,enemy.id);
            string name=unit!=null?unit.name:index>=0?DigimonModelLibrary.RosterNames[index]:enemy.id;
            BattleHudTheme.FitLabel(new Rect(x+72,y+4,130,24),name,14,BattleHudTheme.Text);
            BattleHudTheme.Label(new Rect(x+72,y+28,132,20),new string('★',enemy.star)+"  × "+groups[i].Count(),12,BattleHudTheme.Muted);
        }
        BattleHudTheme.Label(new Rect(x+14,553,190,21),"승리 보상",11,BattleHudTheme.Gold,true);
        TrialParagraph(new Rect(x+14,582,190,48),TrialRewardText(wave),13,BattleHudTheme.Text);
        if(HudButton(new Rect(x+13,646,192,34),"배치 · 능력치 정찰 ↗"))OpenTrialDossier(wave.number);
        if(lastBattleReport.Count>0||battling){if(HudButton(new Rect(x+13,695,192,34),"전투 기록"))showCombatReport=true;}
        else BattleHudTheme.Label(new Rect(x+14,692,190,30),"점수  "+TrialScore.ToString("N0"),18,BattleHudTheme.Teal,true);
    }
    void DrawTrialsResult()
    {
        ArenaInterface.Fill(new Rect(0,0,1920,1080),new Color(.005f,.012f,.025f,.93f));
        BattleHudTheme.Surface(new Rect(490,175,940,730),false,TrialComplete?BattleHudTheme.Teal:BattleHudTheme.Gold);
        BattleHudTheme.Label(new Rect(553,216,809,24),"DR. AGUMON'S TRIALS / "+TrialsCatalog.DifficultyNames[TrialDifficulty],13,BattleHudTheme.Teal,true);
        BattleHudTheme.Label(new Rect(549,271,810,59),TrialComplete?"시험 합격!":"이번 시험은 여기까지",43,BattleHudTheme.Text,true);
        FaithfulPortraits.Draw(new Rect(1120,255,237,235),"agumon");
        BattleHudTheme.Label(new Rect(555,359,530,23),"최종 점수",16,BattleHudTheme.Muted);
        BattleHudTheme.Label(new Rect(550,398,559,70),TrialScore.ToString("N0"),52,BattleHudTheme.Gold,true);
        string[] labels={"통과한 전투","도달 시험","남은 목숨"};string[] values={TrialCleared+" / 30",Mathf.Min(30,TrialStage).ToString("00"),TrialLives+" / 3"};
        for(int i=0;i<3;i++){Rect c=new Rect(554+i*279,506,255,108);BattleHudTheme.Round(c,BattleHudTheme.Raised);BattleHudTheme.Label(new Rect(c.x+20,c.y+15,214,23),labels[i],13,BattleHudTheme.Muted);BattleHudTheme.Label(new Rect(c.x+18,c.y+48,217,40),values[i],29,BattleHudTheme.Text,true);}
        DrawTrialResultRecord(new Rect(555,634,810,64));
        if(arenaInterface.Button(new Rect(554,720,391,61),new GUIContent("같은 난이도로 재도전  →"),true,true,19)){RestartTrials();lobby=false;}
        if(arenaInterface.Button(new Rect(974,720,391,61),new GUIContent("시험 준비 화면으로"),true,false,19))lobby=true;
        if(arenaInterface.Button(new Rect(554,806,811,48),new GUIContent("플레이 모드 선택으로"),true,false,16))FindAnyObjectByType<MultiLauncher>().ReturnToMulti();
    }
}
