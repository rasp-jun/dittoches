using System;
using UnityEngine;

public sealed partial class MultiLauncher
{
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
        if(onlineInterface.Button(new Rect(500,94,780,28),new GUIContent(RoundResultTitle(room),RoundResultDetails(room)),true,false,14))
        {ClearOnlineUnitSelection();ResetEquipmentSelection();onlineHistoricalFighter=null;onlineReport=true;}
    }
}
