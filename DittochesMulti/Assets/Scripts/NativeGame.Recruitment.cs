using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    int PlacementTarget()
    {
        if(!draggingUnit&&artPack==0)return PickFormationTarget(Event.current.mousePosition);
        int seat=arena.HitBench(Event.current.mousePosition);
        return seat>=0?56+seat:arena.HitCell(Event.current.mousePosition);
    }
    FormationForecast.Plan CurrentFormationForecast()
    {
        if(artPack!=0||battling||showCarousel||hp<=0||scoutedRival>=0||selectedItem>=0||!GUI.enabled||HeldUnit()==null||PointerOverGuide(Event.current.mousePosition))return null;
        int target=PlacementTarget();if(target<28)return null;
        bool fromBoard=draggingUnit?dragFromBoard:selectedBoard>=0;
        int source=draggingUnit?dragSource:fromBoard?selectedBoard:selectedBench;
        return FormationForecast.Preview(board.Select(u=>u==null?null:u.def.id).ToArray(),bench.Select(u=>u==null?null:u.def.id).ToArray(),
            fromBoard,source,target<56,target<56?target-28:target-56,level);
    }
    void OpenShopSkill(UnitDef definition)
    {
        OpenSkillDetails(new Unit(definition));skillDetailFromShop=true;
    }
}
