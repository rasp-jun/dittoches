using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    bool showTeamPlan;
    readonly TeamPlannerUI teamPlanner=new TeamPlannerUI();
    void OpenTeamPlan()
    {
        showTeamPlan=true;showRecipeGuide=false;traitFocus=null;skillDetailUnit=null;
        draggingUnit=false;dragSource=-1;arenaPointer.Reset();GUI.FocusControl(null);
    }
    void DrawTeamPlan()
    {
        if(!showTeamPlan)return;
        Matrix4x4 previous=GUI.matrix;
        GUI.matrix=previous*Matrix4x4.Scale(new Vector3(1.2f,1.2f,1));
        showTeamPlan=teamPlanner.Draw(new Rect(0,0,1600,900),board.Where(u=>u!=null).Select(u=>u.def.id).ToArray(),
            bench.Where(u=>u!=null).Select(u=>u.def.id).ToArray(),FormationLimit,!lobby,BoardBuildMembers().ToArray());
        GUI.matrix=previous;
    }
}
