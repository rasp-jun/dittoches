using System.Linq;
using UnityEngine;

public sealed partial class MultiLauncher
{
    bool showTeamPlan;
    readonly TeamPlannerUI teamPlanner=new TeamPlannerUI();
    void OpenTeamPlan()
    {showTraitGuide=false;showTeamPlan=true;onlineSkillId="";onlineItemGuide=-1;confirmLeave=false;arenaPointer.Reset();GUI.FocusControl(null);}
    void DrawTeamPlan()
    {
        if(!showTeamPlan)return;
        Player me=state!=null&&state.room!=null?state.room.players[state.room.side]:null;
        showTeamPlan=teamPlanner.Draw(new Rect(0,0,1600,1000),me==null?new string[0]:me.board.Select(u=>u.id).ToArray(),
            me==null?new string[0]:me.bench.Select(u=>u.id).ToArray(),me==null?9:OnlineFormationLimit(me),me!=null,me==null?null:me.board.Select(BuildMember).ToArray());
    }
}
