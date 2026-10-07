#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public sealed partial class MultiLauncher
{
    IEnumerator FreshEmblemMatch()
    {
        yield return SmokeRequest("/leave");yield return PeerRequest("/leave",new Command());
        yield return SmokeRequest("/queue",new Command{mode="normal"});yield return PeerRequest("/queue",new Command{mode="normal"});yield return SmokeRequest("/state");
    }
    IEnumerator ValidateEmblemsOnline()
    {
        yield return FreshEmblemMatch();int level=OnlineMe.level;
        SelectOnlineItem(Array.IndexOf(OnlineMe.inventory,15));SelectOnlineItem(Array.IndexOf(OnlineMe.inventory,16));yield return AwaitEquipmentAction();
        OnlineRequire(OnlineMe.inventory.Contains(26)&&OnlineFormationLimit(OnlineMe)==level+1,"UI crafts digital gate and immediately expands owned capacity");
        SelectOnlineItem(Array.IndexOf(OnlineMe.inventory,26));ClickSlot("board",3,At(OnlineMe.board,3));yield return AwaitEquipmentAction();
        OnlineRequire(OnlineFormationLimit(OnlineMe)==level+1&&At(OnlineMe.board,3).items.Contains(26),"wearing capacity artifact never counts twice");
        SelectOnlineItem(Array.IndexOf(OnlineMe.inventory,14));ClickSlot("board",3,At(OnlineMe.board,3));yield return AwaitEquipmentAction();
        OnlineRequire(OnlineMe.inventory.Contains(26)&&OnlineFormationLimit(OnlineMe)==level+1,"extracting capacity preserves ownership bonus");
        onlineItemGuide=26;yield return new WaitForEndOfFrame();OnlineCapture("digital-gate-guide");onlineItemGuide=-1;
        yield return FreshEmblemMatch();
        SelectOnlineItem(Array.IndexOf(OnlineMe.inventory,15));SelectOnlineItem(Array.IndexOf(OnlineMe.inventory,1));yield return AwaitEquipmentAction();
        OnlineRequire(OnlineMe.inventory.Contains(18),"UI crafts friendship emblem from device and normal component");
        yield return SmokeRequest("/action",new Command{action="ready"});yield return PeerRequest("/action",new Command{action="ready"});yield return SmokeRequest("/state");
        float health=state.room.frames[0].units.First(f=>f.side==state.room.side).maxHp;
        var row=onlineCombatLabels.First(r=>((Fighter)r.key).side==state.room.side&&((Fighter)r.key).slot==3);
        yield return OnlineGearDrag(Array.IndexOf(OnlineMe.inventory,18),row.rect.center,false);
        OnlineRequire(At(OnlineMe.board,3).items.Contains(18)&&state.room.frames.Any(frame=>frame.units.Any(f=>f.side==state.room.side&&f.maxHp>health)),"actual combat emblem drag updates authoritative health");
        OnlineRequire(DigimonBuildCatalog.Count(DigimonBuildCatalog.Find("friendship"),OnlineMe.board.Select(BuildMember))==1,"online synergy membership includes equipped emblem");
        yield return new WaitForEndOfFrame();OnlineCapture("emblem-live-equipped");
    }
}
#endif
