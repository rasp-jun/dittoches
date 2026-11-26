using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    bool showTeamPlan;
    readonly TeamPlannerUI teamPlanner=new TeamPlannerUI();
    void OpenTeamPlan()
    {
        teamPlanner.ResetView();
        showTeamPlan=true;showRecipeGuide=false;traitFocus=null;skillDetailUnit=null;
        draggingUnit=false;dragSource=-1;arenaPointer.Reset();GUI.FocusControl(null);
    }
    void DrawTeamPlan()
    {
        if(!showTeamPlan)return;
        Matrix4x4 previous=GUI.matrix;
        GUI.matrix=previous*Matrix4x4.Scale(new Vector3(1.2f,1.2f,1));
#if DITTOCHES_PORTABLE_PREVIEW
        // Unity reprojects its native pointer when GUI.matrix changes. Injected test
        // events must be expressed again in the nested canvas after that change.
        if(soloLobbyEventInjected&&Event.current.isMouse)Event.current.mousePosition=soloLobbyClickPoint/1.2f;
#endif
        showTeamPlan=teamPlanner.Draw(new Rect(0,0,1600,900),board.Where(u=>u!=null).Select(u=>u.def.id).ToArray(),
            bench.Where(u=>u!=null).Select(u=>u.def.id).ToArray(),FormationLimit,!lobby,BoardBuildMembers().ToArray(),ReadNativeCollection());
        GUI.matrix=previous;
    }
    StarCollection ReadNativeCollection()
    {
        var pieces=board.Where(u=>u!=null).Select(u=>new StarCollection.Piece(u.def.id,u.star,true)).Concat(bench.Where(u=>u!=null).Select(u=>new StarCollection.Piece(u.def.id,u.star,false)));
        return StarCollection.Create(pieces,shop.Where(d=>d!=null).Select(d=>d.id),TeamPlan.Units,gold,!lobby&&!showCarousel&&hp>0,
            hp<=0?"경기 종료":showCarousel?"선택 단계":"로비",battling?"다음 전투 편성 기준 · 전장 성급은 다음 전투부터 적용":"현재 전장·대기석 기준");
    }
}
