using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    Unit promotionNoticeUnit;
    float promotionNoticeUntil;
    void DrawPromotionNotice()
    {
        if(artPack!=0||scoutedRival>=0||promotionNoticeUnit==null||Time.unscaledTime>=promotionNoticeUntil)return;
        if(!board.Contains(promotionNoticeUnit)&&!bench.Contains(promotionNoticeUnit))return;
        StarUpgradeUI.DrawPromotionNotice(new Rect(656,204,580,70),UnitName(promotionNoticeUnit.def),promotionNoticeUnit.star,battling,promotionNoticeUntil-Time.unscaledTime);
    }
}
