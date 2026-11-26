using UnityEngine;

public sealed partial class MultiLauncher
{
    PurchaseForecast OnlineRecruitHover(Room room,Player me,Vector2 pointer,bool enabled)
    {
        if(room==null||me==null||me.shop==null||me.board==null||me.bench==null)return null;
        if(artPack!=0||!enabled||busy||OnlineScouting||onlineDragging||onlineEquipmentDrag.Dragging||onlineItem>=0||MatchFinisherActive||!OnlineConnectionFresh()||!OnlineManagementAllowed(room,me))return null;
        for(int i=0;i<me.shop.Length;i++)
        {
            if(Def(me.shop[i])==null||!new Rect(310+i*191,814,181,108).Contains(pointer))continue;
            var preview=PreviewOnlinePurchase(me,me.shop[i],room);return preview.available&&preview.result.star>1?preview:null;
        }
        return null;
    }
}
