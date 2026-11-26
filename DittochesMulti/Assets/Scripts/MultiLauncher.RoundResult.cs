using System;
using UnityEngine;

public sealed partial class MultiLauncher
{
    Rect OnlineRoundBannerRect { get { return new Rect(500,artPack==0&&MatchFinisherActive&&!OnlineScouting?137:210,750,28); } }
    bool OnlineRoundBannerVisible(Room room)
    {return room.phase!="battle"&&(HasRoundResult(room)||room.phase=="prepare"&&recruitReceipt!=null&&recruitReceipt.until>Time.unscaledTime);}
    [Serializable] public sealed class RoundResult
    {
        public int version,round,winner,hpBefore,hpAfter,healthLost,opponentHealthLost,baseIncome,interest,winBonus,income;
    }
    static bool HasRoundResult(Room room)
    {return room!=null&&room.roundResult!=null&&room.roundResult.version==1&&room.reportRound>0&&room.roundResult.round==room.reportRound;}
    static string RoundResultTitle(Room room)
    {
        var r=room.roundResult;string outcome=r.winner<0?"무승부":r.winner==room.side?"승리":"패배";
        return "지난 "+r.round+"R "+outcome+"  ·  내 체력 "+r.hpBefore+" → "+r.hpAfter+"  ·  수입 +"+r.income+"G  ·  기록 보기";
    }
    static string RoundResultDetails(Room room)
    {
        var r=room.roundResult;
        return "완료된 "+r.round+"라운드 정산\n내 체력 −"+r.healthLost+" · 상대 체력 −"+r.opponentHealthLost+
            "\n기본 +"+r.baseIncome+"G · 이자 +"+r.interest+"G · 승리 보너스 +"+r.winBonus+"G"+
            "\n합계 +"+r.income+"G · 정산 당시 확정값\n클릭하면 지난 전투 기록을 엽니다.";
    }
    void DrawOnlineRoundResult(Room room)
    {
        if(room.phase=="battle"||!HasRoundResult(room))return;
        if(room.phase=="prepare"&&recruitReceipt!=null&&recruitReceipt.until>Time.unscaledTime)return;
        bool won=room.roundResult.winner==room.side;
        Rect bar=OnlineRoundBannerRect;Color tint=won?BattleHudTheme.Teal:room.roundResult.winner<0?BattleHudTheme.Muted:BattleHudTheme.Danger;
        BattleHudTheme.Round(bar,new Color(.031f,.067f,.097f,.97f));
        BattleHudTheme.Badge(new Rect(bar.x+4,bar.y+3,67,bar.height-6),won?"승리":room.roundResult.winner<0?"무승부":"패배",tint,12);
        BattleHudTheme.Label(new Rect(bar.x+85,bar.y,122,bar.height),"ROUND "+room.roundResult.round,11,BattleHudTheme.Muted,true);
        BattleHudTheme.Label(new Rect(bar.x+212,bar.y,240,bar.height),"체력  "+room.roundResult.hpBefore+" → "+room.roundResult.hpAfter,13,BattleHudTheme.Text);
        BattleHudTheme.Label(new Rect(bar.xMax-273,bar.y,151,bar.height),"수입  +"+room.roundResult.income+" G",13,BattleHudTheme.Gold,true);
        BattleHudTheme.Label(new Rect(bar.xMax-119,bar.y,105,bar.height),"전투 기록  ↗",12,BattleHudTheme.Teal,true,TextAnchor.MiddleRight);
        if(GUI.Button(bar,new GUIContent("",RoundResultDetails(room)),GUIStyle.none))
        {if(OnlineScouting)SetScoutedPlayer(-1);ClearOnlineUnitSelection();ResetEquipmentSelection();onlineHistoricalFighter=null;onlineReport=true;}
    }
}
