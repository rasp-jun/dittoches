using UnityEngine;

public sealed partial class NativeGame
{
    PurchaseForecast NativeRecruitHover(Vector2 pointer,bool enabled)
    {
        if(artPack!=0||!enabled||hp<=0||scoutedRival>=0||draggingUnit||equipmentDrag.Dragging||selectedItem>=0||NativeFinisherActive)return null;
        for(int i=0;i<shop.Length;i++)
        {
            if(shop[i]==null||!new Rect(249+i*285,893,271,170).Contains(pointer))continue;
            var preview=PreviewPurchase(shop[i]);return preview.available&&preview.result.star>1?preview:null;
        }
        return null;
    }
    Vector3 NativeMergePosition(PurchaseForecast.Source source)
    {
        Unit unit=source.board?board[source.slot]:bench[source.slot];Vector3 point;
        return unit!=null&&formationPositions.TryGetValue(unit,out point)?point:MergeFocusUI.Position(source);
    }
}
