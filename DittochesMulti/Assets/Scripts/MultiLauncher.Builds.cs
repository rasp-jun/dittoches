using System.Linq;
using UnityEngine;

public sealed partial class MultiLauncher
{
    string onlineTrait="";
    void DrawOnlineTraits(Player me)
    {
        Unit unit=SelectedOnlineUnit(me);if(unit!=null){DrawOnlineSkillStats(unit);return;}
        var ids=me.board.Select(u=>u.id).ToArray();
        var traits=DigimonBuildCatalog.Data.traits.Where(t=>DigimonBuildCatalog.Count(t,ids)>0)
            .OrderByDescending(t=>t.Level(DigimonBuildCatalog.Count(t,ids))).ThenByDescending(t=>DigimonBuildCatalog.Count(t,ids)).ToArray();
        Card(new Rect(1305,490,270,430),surface,new Color(.2f,.38f,.43f));
        GUI.Label(new Rect(1320,500,240,25),"팀 시너지 · 전장만 계산",small);
        for(int i=0;i<traits.Length;i++)
        {
            var trait=traits[i];int count=DigimonBuildCatalog.Count(trait,ids),tier=trait.Level(count);
            if(Btn(new Rect(1320,533+i*25,240,23),(tier>0?"● ":"○ ")+trait.name+"    "+count+" / "+trait.Target(count)))onlineTrait=trait.name;
        }
        var selected=DigimonBuildCatalog.Find(onlineTrait)??traits.FirstOrDefault();
        if(selected!=null)
        {
            int tier=selected.Level(DigimonBuildCatalog.Count(selected,ids));
            var wrap=new GUIStyle(small){wordWrap=true,fontSize=13};
            GUI.Label(new Rect(1320,818,240,90),selected.name+" · "+(tier>0?"활성":"다음 단계")+"\n"+selected.tiers[Mathf.Max(0,tier-1)].text,wrap);
        }
    }
}
