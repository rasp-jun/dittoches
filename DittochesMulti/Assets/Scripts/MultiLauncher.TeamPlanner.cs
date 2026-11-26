using System.Linq;
using UnityEngine;

public sealed partial class MultiLauncher
{
    bool showTeamPlan;
    readonly TeamPlannerUI teamPlanner=new TeamPlannerUI();
    void OpenTeamPlan()
    {teamPlanner.ResetView();showTraitGuide=false;showTeamPlan=true;onlineSkillId="";onlineItemGuide=-1;confirmLeave=false;arenaPointer.Reset();GUI.FocusControl(null);}
    void DrawTeamPlan()
    {
        if(!showTeamPlan)return;
        Player me=state!=null&&state.room!=null?state.room.players[state.room.side]:null;
        showTeamPlan=teamPlanner.Draw(new Rect(0,0,1600,1000),me==null?new string[0]:me.board.Select(u=>u.id).ToArray(),
            me==null?new string[0]:me.bench.Select(u=>u.id).ToArray(),me==null?9:OnlineFormationLimit(me),me!=null,me==null?null:me.board.Select(BuildMember).ToArray(),ReadOnlineCollection());
    }
    StarCollection ReadOnlineCollection()
    {
        var room=state==null?null:state.room;var me=room==null?null:MatchPlayer(room);if(me==null)return null;
        var pieces=me.board.Select(u=>new StarCollection.Piece(u.id,u.star,true)).Concat(me.bench.Select(u=>new StarCollection.Piece(u.id,u.star,false)));
        bool fresh=OnlineConnectionFresh();string reason=!fresh?"연결 확인 중":me.eliminated?"탈락":room.phase=="finished"?"경기 종료":room.phase=="prepare"&&me.ready?"준비 해제 후 모집":busy?"처리 중":"모집 대기";
        return StarCollection.Create(pieces,me.shop,TeamPlan.Units,me.gold,fresh&&!busy&&OnlineManagementAllowed(room,me),reason,
            !fresh?"최근 수신한 편성 · 연결을 확인하고 있습니다":room.phase=="battle"?"다음 전투 편성 기준 · 전장 성급은 다음 전투부터 적용":"현재 서버 편성 기준");
    }
}
