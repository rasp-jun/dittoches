using System.Linq;
using UnityEngine;

public sealed partial class MultiLauncher
{
    void DrawOnlineCommandHeader(Room room,float remaining)
    {
        ArenaInterface.Fill(new Rect(0,0,1600,84),BattleHudTheme.Background);
        ArenaInterface.Fill(new Rect(0,83,1600,1),BattleHudTheme.Line);
        ArenaInterface.Fill(new Rect(29,20,4,43),BattleHudTheme.Teal);
        BattleHudTheme.Label(new Rect(45,12,242,32),"DITTOCHES",24,null,true);
        BattleHudTheme.Label(new Rect(46,48,263,19),room.players.Length+"인 리그  /  "+(room.mode.StartsWith("ranked")?"RANKED":"NORMAL"),12,BattleHudTheme.Muted);
        var me=room.players[room.side];
        BattleHudTheme.Label(new Rect(333,15,188,19),"나의 편성",11,BattleHudTheme.Muted);
        BattleHudTheme.Label(new Rect(333,36,191,30),me.board.Length+" / "+OnlineFormationLimit(me)+"  배치",20,me.board.Length<OnlineFormationLimit(me)?BattleHudTheme.Gold:BattleHudTheme.Teal,true);
        ArenaPresentationUI.RoundPlate(new Rect(550,9,410,65),"R "+room.round,room.phase=="prepare"?"준비 단계":room.phase=="battle"?"전투 중":"경기 종료",room.phase=="finished"?"FINAL":Mathf.CeilToInt(remaining)+"초",room.phase=="battle",room.phase=="battle"?remaining/Mathf.Max(1,room.battleDuration):-1);
    }
    void DrawOnlineRoundOverview(Room room)
    {
        var me=room.players[room.side];var opponent=room.players[OpponentSide(room)];
        BattleHudTheme.Surface(new Rect(1305,290,270,180));
        if(me.eliminated||room.phase=="finished")
        {
            ArenaPresentationUI.SectionHeading(new Rect(1320,305,240,28),"경기 결과","");
            BattleHudTheme.Label(new Rect(1320,343,240,36),me.placement>0?me.placement+"위 확정":"경기 종료",27,BattleHudTheme.Gold,true);
            BattleHudTheme.Label(new Rect(1320,391,240,22),"다른 테이머의 편성을 정찰하세요",13,BattleHudTheme.Muted);
            BattleHudTheme.Label(new Rect(1320,426,240,24),"상단 로비 버튼으로 돌아가기",12,BattleHudTheme.Muted);return;
        }
        bool combat=room.phase=="battle";
        BattleHudTheme.Label(new Rect(1320,302,240,22),combat?"현재 대전  /  BATTLE":"다음 대전  /  MATCHUP",11,BattleHudTheme.Muted,true);
        var portrait=TamerLoadout.Portrait(opponent.tamer);
        ArenaInterface.Fill(new Rect(1320,331,44,44),BattleHudTheme.Raised);
        if(portrait!=null)GUI.DrawTexture(new Rect(1321,332,42,42),portrait,ScaleMode.ScaleToFit);
        BattleHudTheme.FitLabel(new Rect(1375,329,117,29),opponent.name,19);
        BattleHudTheme.Label(new Rect(1375,358,147,18),"선택하여 상대 정찰",11,BattleHudTheme.Muted);
        BattleHudTheme.Label(new Rect(1491,335,69,24),opponent.hp+" HP",13,BattleHudTheme.Gold,true,TextAnchor.MiddleRight);
        if(GUI.Button(new Rect(1320,333,240,36),new GUIContent("","상대 필드 정찰"),GUIStyle.none))SetScoutedPlayer(OpponentSide(room));
        int alive=room.players.Count(p=>!p.eliminated),ready=room.players.Count(p=>!p.eliminated&&p.ready);
        BattleHudTheme.Label(new Rect(1320,388,240,24),combat?"상점과 장비를 이용할 수 있어요":"준비 완료  "+ready+" / "+alive,14,combat?BattleHudTheme.Muted:BattleHudTheme.Teal,true);
        if(!combat)ArenaPresentationUI.SeatMeter(new Rect(1320,421,240,4),ready,alive,BattleHudTheme.Teal);
        else ArenaInterface.Fill(new Rect(1320,421,240,1),BattleHudTheme.Line);
        BattleHudTheme.Label(new Rect(1320,435,240,20),room.ghostOpponent?"복제 상대 · 원본 체력 감소 없음":combat?"장비 변경은 다음 전투부터 적용":"모두 준비하거나 시간이 끝나면 시작",11,BattleHudTheme.Muted);
    }
    void DrawTamerCommandCard(Player me)
    {
        BattleHudTheme.Surface(new Rect(25,95,260,175));
        ArenaInterface.Fill(new Rect(40,108,53,53),Color.Lerp(BattleHudTheme.Raised,BattleHudTheme.Teal,.1f));
        var portrait=TamerLoadout.Portrait(me.tamer);if(portrait!=null)GUI.DrawTexture(new Rect(40,109,53,51),portrait,ScaleMode.ScaleToFit);
        BattleHudTheme.Label(new Rect(106,107,158,17),"나의 테이머",11,BattleHudTheme.Muted);
        BattleHudTheme.FitLabel(new Rect(106,128,159,27),me.name,21);
        BattleHudTheme.Label(new Rect(41,178,103,28),me.hp+" HP",25,me.hp<=30?BattleHudTheme.Danger:BattleHudTheme.Teal,true);
        BattleHudTheme.Label(new Rect(158,177,107,30),me.gold+" G",27,BattleHudTheme.Gold,true,TextAnchor.MiddleRight);
        BattleHudTheme.Bar(new Rect(42,212,223,4),me.hp/100f,me.hp<=30?BattleHudTheme.Danger:BattleHudTheme.Teal);
        BattleHudTheme.Label(new Rect(42,229,118,22),me.rating+" RP",13,BattleHudTheme.Muted,true);
        BattleHudTheme.Label(new Rect(171,225,94,19),"대기석 "+me.bench.Length+" / 9",11,BattleHudTheme.Muted,false,TextAnchor.MiddleRight);
        ArenaPresentationUI.SeatMeter(new Rect(174,251,90,4),me.bench.Length,9,BattleHudTheme.Gold);
    }
    void DrawScoutingComparison(Player viewed)
    {
        var me=OnlineMe;
        ArenaPresentationUI.SectionHeading(new Rect(1320,305,170,23),"정찰 브리핑","",BattleHudTheme.Gold);
        BattleHudTheme.Label(new Rect(1480,305,80,23),viewed.hp+" HP",14,BattleHudTheme.Teal,true,TextAnchor.MiddleRight);
        BattleHudTheme.FitLabel(new Rect(1320,337,240,25),viewed.name+" · 레벨 "+viewed.level,17);
        BattleHudTheme.Label(new Rect(1320,369,120,19),"전장 비교",11,BattleHudTheme.Muted);
        BattleHudTheme.Label(new Rect(1440,369,54,19),"나",11,BattleHudTheme.Teal,true,TextAnchor.MiddleRight);
        BattleHudTheme.Label(new Rect(1500,369,60,19),"상대",11,BattleHudTheme.Gold,true,TextAnchor.MiddleRight);
        CompareFormationLine(395,"배치 유닛",me.board.Length,viewed.board.Length);
        CompareFormationLine(422,"2성 이상",me.board.Count(u=>u.star>=2),viewed.board.Count(u=>u.star>=2));
        CompareFormationLine(449,"장착 장비",me.board.Sum(u=>(u.items??new int[0]).Length),viewed.board.Sum(u=>(u.items??new int[0]).Length));
        BattleHudTheme.Label(new Rect(1320,481,240,22),viewed.publicBench?"대기석 "+viewed.bench.Length+" / 9 · 전투력 합산 제외":"대기석 정보 미수신",11,BattleHudTheme.Muted);
        BattleHudTheme.Label(new Rect(1320,511,240,24),"상대 시너지",16,null,true);
        string clicked=onlineScoutTraitPanel.Draw(new Rect(1320,543,240,363),ReadOnlineTraitHud(viewed.board,true),new OnlineTraitFormation(viewed.bench),viewed.publicBench);
        if(clicked!=null)OpenOnlineTrait(clicked);
    }
    static void CompareFormationLine(float y,string title,int own,int other)
    {
        ArenaInterface.Fill(new Rect(1320,y,240,24),Color.Lerp(BattleHudTheme.Panel,BattleHudTheme.Raised,.7f));
        BattleHudTheme.Label(new Rect(1329,y,113,24),title,12,BattleHudTheme.Muted);
        BattleHudTheme.Label(new Rect(1440,y,54,24),own.ToString(),14,BattleHudTheme.Teal,true,TextAnchor.MiddleRight);
        BattleHudTheme.Label(new Rect(1500,y,50,24),other.ToString(),14,BattleHudTheme.Gold,true,TextAnchor.MiddleRight);
    }
}
