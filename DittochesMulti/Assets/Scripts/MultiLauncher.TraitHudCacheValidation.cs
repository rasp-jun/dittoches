#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Linq;

public sealed partial class MultiLauncher
{
    void ValidateOnlineTraitHudCache()
    {
        var own=new[]{new Unit{id="agumon",star=1,slot=0,items=new[]{0}},new Unit{id="gabumon",star=1,slot=1,items=new[]{17}}};
        var snapshot=ReadOnlineTraitHud(own,false);int revision=snapshot.RebuildCount;
        var fresh=own.Select(u=>new Unit{id=u.id,star=3,slot=u.slot,items=(int[])u.items.Clone()}).ToArray();
        OnlineRequire(ReferenceEquals(snapshot,ReadOnlineTraitHud(fresh,false))&&snapshot.RebuildCount==revision,"fresh equal online board values reuse the HUD snapshot despite new Unit arrays and stars");
        var scout=new[]{new Unit{id="patamon",star=1,slot=5,items=new[]{18}}};
        ReadOnlineTraitHud(scout,true);OnlineRequire(ReferenceEquals(snapshot,ReadOnlineTraitHud(fresh,false))&&snapshot.RebuildCount==revision,"online own/scout caches remain separate when changing visible owner");
        fresh[1].items[0]=18;OnlineRequire(ReadOnlineTraitHud(fresh,false).RebuildCount==revision+1,"same-reference online equipment mutation rebuilds the HUD");
        DigimonTraitHudSnapshotValidation.Compare(snapshot,fresh.Select(BuildMember).ToArray(),DigimonTraitHudSnapshot.RowOrder.OnlineOwn,OnlineRequire,"online raw Unit adapter");
        scout[0].id="palmon";var changed=ReadOnlineTraitHud(scout,true);
        DigimonTraitHudSnapshotValidation.Compare(changed,scout.Select(BuildMember).ToArray(),DigimonTraitHudSnapshot.RowOrder.Scout,OnlineRequire,"online scout raw Unit adapter");
        fresh[1].items=null;OnlineRequire(ReadOnlineTraitHud(fresh,false).RebuildCount==revision+2,"online equipment extraction refreshes derived trait counts");
    }
}
#endif
