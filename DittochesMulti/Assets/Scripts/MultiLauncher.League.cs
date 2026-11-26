using UnityEngine;

public sealed partial class MultiLauncher
{
    static readonly Rect LeagueReturnRect=new Rect(1000,177,254,28);
    void DrawLeagueHome()
    {
        DrawLobbyBackground(.38f);DrawTopNavigation();
        BattleHudTheme.Surface(new Rect(62,124,1476,736),false,BattleHudTheme.Teal);
        BattleHudTheme.Label(new Rect(106,160,690,28),"FILE ISLAND  /  EIGHT TAMERS. ONE SURVIVOR.",14,BattleHudTheme.Teal,true);
        BattleHudTheme.Label(new Rect(102,220,760,86),"당신의 팀으로,",57,null,true);
        BattleHudTheme.Label(new Rect(102,301,760,86),"마지막까지.",57,BattleHudTheme.Gold,true);
        GUI.Label(new Rect(108,413,630,76),"디지몬을 모으고, 시너지를 완성하세요.\n8명의 테이머와 함께하는 파일 아일랜드 리그.",text);
        if(GoldBtn(new Rect(108,527,304,70),"플레이   →"))lobbyTab=1;
        if(Btn(new Rect(428,527,218,70),"팀 계획"))OpenTeamPlan();
        BattleHudTheme.Label(new Rect(109,613,565,26),"솔로 연습 · 8인 일반 · 8인 랭크",15,BattleHudTheme.Muted);
        CharacterCardArt.Stage(new Rect(827,174,656,479),5);
        FaithfulPortraits.Draw(new Rect(1060,230,363,347),"metalgarurumon");
        FaithfulPortraits.Draw(new Rect(833,167,411,447),"wargreymon");
        BattleHudTheme.Badge(new Rect(882,609,550,37),"수집  →  배치  →  승급",BattleHudTheme.Gold,17);
        string[] headings={"01  편성을 설계하세요","02  상대를 정찰하세요","03  최후의 테이머가 되세요"};
        string[] captions={"계열과 역할을 조합해 팀을 완성","8명의 체력과 배치를 확인","같은 디지몬 3마리로 별 승급"};
        for(int i=0;i<3;i++)
        {
            float x=108+i*461;BattleHudTheme.Surface(new Rect(x,702,433,110),true);
            BattleHudTheme.Label(new Rect(x+22,719,390,31),headings[i],18,null,true);
            BattleHudTheme.Label(new Rect(x+22,761,390,26),captions[i],14,BattleHudTheme.Muted);
        }
        BattleHudTheme.Label(new Rect(64,890,1010,27),"장착 외형  /  "+lobbyLoadout.Summary,14,BattleHudTheme.Muted);
        if(Btn(new Rect(1281,877,256,42),"테이머 · 필드 꾸미기"))lobbyTab=4;
    }
    void DrawLeagueModes()
    {
        BattleHudTheme.Label(new Rect(70,108,1000,53),"플레이 방식 선택",32,null,true);
        BattleHudTheme.Label(new Rect(72,173,1130,30),"혼자 연습하거나, 여덟 명의 테이머와 생존 리그를 시작하세요.",17,BattleHudTheme.Muted);
        string[] names={"솔로 플레이","일반 리그","랭크 리그"},ids={"agumon","metalgarurumon","wargreymon"};
        string[] descriptions={"AI 7명과 나만의 속도로 플레이\n서버 연결 없이 시작","실제 테이머 8명 · 순위 경쟁\nRP 변동 없이 편성을 실험","실제 테이머 8명 · 순위 경쟁\n최종 순위에 따라 RP 정산"};
        bool queued=state!=null&&!string.IsNullOrEmpty(state.queue);
        for(int i=0;i<3;i++)
        {
            float x=70+i*495;Color tint=i==2?BattleHudTheme.Gold:i==1?BattleHudTheme.Teal:BattleHudTheme.Muted;
            BattleHudTheme.Surface(new Rect(x,230,460,555),false,tint);
            CharacterCardArt.Stage(new Rect(x+1,234,458,211),i==0?2:5);
            FaithfulPortraits.Draw(new Rect(x+90,236,280,203),ids[i],true);
            BattleHudTheme.Badge(new Rect(x+23,250,130,29),i==0?"SOLO":i==1?"NORMAL / 8":"RANKED / 8",tint);
            BattleHudTheme.Label(new Rect(x+30,464,400,49),names[i],30,null,true);
            GUI.Label(new Rect(x+30,534,400,92),descriptions[i],text);
            string action=i==0?"솔로 리그 입장":token.Length==0?"서버 연결하기":state!=null&&state.leagueVersion<1?"8인 서버 업데이트 필요":"8인 대전 찾기";
            if(onlineInterface.Button(new Rect(x+30,680,400,65),new GUIContent(action),!busy&&!queued&&(i==0||token.Length==0||state!=null&&state.leagueVersion>=1),i==2,19))
            {if(i==0)EnterSolo();else if(token.Length==0)lobbyTab=3;else Send("/queue",new Command{mode=i==1?"normal8":"ranked8"});}
        }
        if(queued)
        {
            BattleHudTheme.Surface(new Rect(70,812,1460,84),true,BattleHudTheme.Teal);
            BattleHudTheme.Label(new Rect(96,825,1120,43),"테이머 모집  "+state.waiting+" / "+Mathf.Max(2,state.queueTarget)+"  ·  모두 모이면 경기가 시작됩니다",21,null,true);
            if(Btn(new Rect(1264,828,243,49),"매칭 취소"))Send("/cancel");
        }
        else
        {
            BattleHudTheme.Label(new Rect(72,829,1080,35),state==null?"온라인 리그는 서버 설정에서 접속 후 이용할 수 있습니다.":"연결됨 · "+state.name+"  /  "+state.rating+" RP",16,BattleHudTheme.Muted);
            if(Btn(new Rect(1260,821,270,47),"팀 계획"))OpenTeamPlan();
        }
    }
    static int OpponentSide(Room room)
    {return room.players.Length>2?Mathf.Clamp(room.opponentSide,0,room.players.Length-1):1-room.side;}
    static int BattleSide(Room room)
    {return room.players!=null&&room.players.Length>2?room.battleSide:room.side;}
    void DrawLeagueRoster(Room room)
    {
        BattleHudTheme.Surface(new Rect(1305,95,270,182));
        BattleHudTheme.Label(new Rect(1319,101,155,23),"테이머",16,null,true);
        BattleHudTheme.Label(new Rect(1474,103,85,21),room.aliveCount+" / "+room.players.Length+" 생존",12,BattleHudTheme.Teal,true,TextAnchor.MiddleRight);
        for(int i=0;i<room.players.Length;i++)
        {
            var p=room.players[i];bool own=i==room.side,viewing=OnlineScouting?scoutSide==i:own;
            Rect r=new Rect(1316+(i%2)*126,128+(i/2)*35,121,31);
            Color tint=p.eliminated?BattleHudTheme.Muted:viewing?BattleHudTheme.Teal:i==OpponentSide(room)?BattleHudTheme.Gold:BattleHudTheme.Text;
            ArenaInterface.Fill(r,viewing?Color.Lerp(BattleHudTheme.Panel,BattleHudTheme.Teal,.14f):i==OpponentSide(room)?Color.Lerp(BattleHudTheme.Panel,BattleHudTheme.Gold,.08f):BattleHudTheme.Panel);
            if(viewing||i==OpponentSide(room))ArenaInterface.Fill(new Rect(r.x,r.y,2,r.height),tint);
            var portrait=TamerLoadout.Portrait(p.tamer);
            if(portrait!=null)
            {
                Color old=GUI.color;if(p.eliminated)GUI.color=new Color(1,1,1,.42f);
                GUI.DrawTexture(new Rect(r.x+4,r.y+3,23,25),portrait,ScaleMode.ScaleToFit);GUI.color=old;
            }
            BattleHudTheme.FitLabel(new Rect(r.x+31,r.y,52,17),p.name,11,tint);
            BattleHudTheme.Label(new Rect(r.x+84,r.y,33,17),p.eliminated?p.placement+"위":p.hp.ToString(),11,tint,true,TextAnchor.MiddleRight);
            BattleHudTheme.Label(new Rect(r.x+31,r.y+17,85,13),p.eliminated?"탈락 · 정찰":own?"나의 테이머":i==OpponentSide(room)?"대전 상대"+(room.ghostOpponent?" · 복제":""):p.ready?"준비 완료":"준비 중",9,BattleHudTheme.Muted);
            if(!p.eliminated)BattleHudTheme.Bar(new Rect(r.x+31,r.yMax-1,r.width-35,1),p.hp/100f,viewing?BattleHudTheme.Teal:i==OpponentSide(room)?BattleHudTheme.Gold:BattleHudTheme.Line);
            if(GUI.Button(r,new GUIContent("",p.name+" · "+p.hp+" HP · 클릭하여 필드 정찰"),GUIStyle.none))SetScoutedPlayer(i);
        }
    }
    void DrawLeagueFieldStatus(Room room)
    {
        if(room.players.Length<=2)return;
        var me=room.players[room.side];
        BattleHudTheme.Badge(new Rect(977,23,150,43),room.phase=="finished"?"순위 확정":me.eliminated?me.placement+"위 · 정찰":room.aliveCount+"명 생존",me.eliminated?BattleHudTheme.Muted:BattleHudTheme.Teal,15);
        if(OnlineScouting)
        {
            if(onlineInterface.Button(LeagueReturnRect,new GUIContent("내 필드로 복귀  [SPACE]"),true,true,13))SetScoutedPlayer(-1);
        }
        else if(room.ghostOpponent)
            BattleHudTheme.Badge(new Rect(488,176,610,25),"복제 편성과 전투 · 상대 원본의 체력은 감소하지 않습니다",BattleHudTheme.Gold,12);
    }
    void DrawLeagueResult(Room room)
    {
        Rect r=new Rect(461,262,674,487);BattleHudTheme.Surface(r,false,BattleHudTheme.Gold);
        ArenaInterface.Fill(new Rect(r.x+1,r.y+1,r.width-2,106),Color.Lerp(BattleHudTheme.Panel,BattleHudTheme.Gold,.09f));
        BattleHudTheme.Label(new Rect(r.x+25,r.y+13,82,74),room.placement.ToString("00"),53,BattleHudTheme.Gold,true);
        BattleHudTheme.Label(new Rect(r.x+125,r.y+15,520,19),"FINAL STANDINGS  /  FILE ISLAND",11,BattleHudTheme.Gold,true);
        BattleHudTheme.Label(new Rect(r.x+122,r.y+38,522,38),room.placement==1?"최후의 테이머":"리그 종료 · "+room.placement+"위",29,null,true);
        BattleHudTheme.Label(new Rect(r.x+125,r.y+80,519,21),room.mode.StartsWith("ranked")?"랭크 변동 "+room.ratingDelta.ToString("+0;-0;0")+" RP  /  현재 "+state.rating+" RP":"일반 리그 · RP 변동 없음",13,BattleHudTheme.Muted);
        for(int place=1;place<=room.players.Length;place++)
        {
            var p=System.Array.Find(room.players,v=>v.placement==place);if(p==null)continue;
            bool own=p==room.players[room.side];Rect row=new Rect(r.x+28,r.y+117+(place-1)*41,618,36);
            ArenaInterface.Fill(row,own?Color.Lerp(BattleHudTheme.Panel,BattleHudTheme.Teal,.14f):place%2==0?Color.Lerp(BattleHudTheme.Panel,BattleHudTheme.Raised,.45f):BattleHudTheme.Panel);
            if(own)ArenaInterface.Fill(new Rect(row.x,row.y,3,row.height),BattleHudTheme.Teal);
            BattleHudTheme.Label(new Rect(row.x+11,row.y,62,36),place+"위",16,place==1?BattleHudTheme.Gold:BattleHudTheme.Muted,true);
            var portrait=TamerLoadout.Portrait(p.tamer);if(portrait!=null)GUI.DrawTexture(new Rect(row.x+78,row.y+3,30,30),portrait,ScaleMode.ScaleToFit);
            BattleHudTheme.Label(new Rect(row.x+121,row.y,336,36),p.name+(own?" · 나":""),17,null,own);
            BattleHudTheme.Label(new Rect(row.x+477,row.y,125,36),p.rating+" RP",14,BattleHudTheme.Muted,false,TextAnchor.MiddleRight);
        }
    }
}
