using System.Linq;
using UnityEngine;

public sealed partial class MultiLauncher
{
    bool showTraitGuide;
    void OpenOnlineTrait(string id)
    {onlineTrait=id;showTraitGuide=true;showTeamPlan=false;onlineSkillId="";onlineItemGuide=-1;arenaPointer.Reset();}
    void DrawOnlineTraitGuide()
    {
        if(!showTraitGuide)return;
        var me=ScoutedPlayer;if(me==null){showTraitGuide=false;return;}
        showTraitGuide=DigimonTraitUI.DrawGuide(new Rect(0,0,1600,1000),ref onlineTrait,me.board.Select(u=>u.id).ToArray(),me.bench.Select(u=>u.id).ToArray(),me.board.Select(BuildMember).ToArray(),me.bench.Select(BuildMember).ToArray(),OnlineScouting?me.name+" · 상대 필드":"내 전장",!OnlineScouting||me.publicBench);
    }
}
