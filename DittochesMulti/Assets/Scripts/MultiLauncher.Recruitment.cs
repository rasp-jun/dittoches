using System.Linq;
using UnityEngine;

public sealed partial class MultiLauncher
{
    FormationForecast.Plan OnlineFormationForecast(Player me,bool editable)
    {
        if(artPack!=0||!editable||!GUI.enabled||busy||onlineItem>=0||selectedSlot<0||arena==null)return null;
        int seat=arena.HitBench(Event.current.mousePosition),cell=arena.HitCell(Event.current.mousePosition);
        if(seat<0&&cell<28)return null;
        var board=new string[28];var bench=new string[9];
        foreach(var unit in me.board)board[unit.slot]=unit.id;
        foreach(var unit in me.bench)bench[unit.slot]=unit.id;
        return FormationForecast.Preview(board,bench,selectedArea=="board",selectedSlot,seat<0,seat>=0?seat:cell-28,me.level);
    }
    void OpenOnlineShopSkill(UnitDef definition)
    {onlineSkillId=definition.id;onlineSkillArea="shop";onlineSkillSlot=-1;onlineItemGuide=-1;arenaPointer.Reset();}
    void DrawOnlineRecruitCard(Rect r,string id,int slot,Player me,bool editable)
    {
        var def=Def(id);Card(r,surface2,def==null?new Color(.15f,.25f,.3f):gold);
        if(def==null)return;
        var skill=DigimonSkillCatalog.Find(id);bool afford=me.gold>=def.cost,space=me.bench.Length<9;
        bool merge=me.board.Concat(me.bench).Count(u=>u.id==id&&u.star==1)>=2;
        var caption=new GUIStyle(small){fontSize=12};
        GUI.Label(new Rect(r.x+8,r.y+5,132,22),def.name,new GUIStyle(small){fontSize=14});
        GUI.Label(new Rect(r.xMax-37,r.y+5,30,22),def.cost+"G",caption);
        Portrait(new Rect(r.x+6,r.y+29,52,55),id);
        GUI.Label(new Rect(r.x+63,r.y+30,111,22),skill.ScalingRole.Replace(" 딜러","")+" · "+skill.attackRange+"칸",caption);
        GUI.Label(new Rect(r.x+63,r.y+53,111,35),string.Join(" · ",DigimonBuildCatalog.ForUnit(id).Select(t=>t.name).ToArray()),new GUIStyle(caption){fontSize=11});
        string hint=!editable?"준비 단계에 모집":!afford?"골드 부족":!space?"대기석 가득":merge?"구매 시 자동 합성":"클릭하여 모집";
        GUI.Label(new Rect(r.x+8,r.y+87,133,20),hint,new GUIStyle(caption){fontSize=11});
        // Inspection is available even when a purchase is disabled.
        if(DigimonSkillUI.DrawIcon(new Rect(r.xMax-35,r.y+73,28,28),skill))OpenOnlineShopSkill(def);
        bool enabled=GUI.enabled;GUI.enabled=enabled&&editable&&afford&&space;
        if(GUI.Button(r,GUIContent.none,GUIStyle.none))Send("/action",new Command{action="buy",slot=slot});
        GUI.enabled=enabled;
    }
}
