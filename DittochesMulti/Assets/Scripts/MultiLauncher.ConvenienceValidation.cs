#if DITTOCHES_PORTABLE_PREVIEW
using System.Collections;
using System.Linq;
using UnityEngine;

public sealed partial class MultiLauncher
{
    IEnumerator ValidateConvenienceHelp(bool match)
    {
        var soundSettings=new StarSoundValidation.Preferences();
        try
        {
        int oldTab=lobbyTab;string before=match?JsonUtility.ToJson(OnlineMe):"";
        yield return OnlineKey(KeyCode.F1);OnlineRequire(showPlayHelp,"F1 help opens in "+(match?"match":"lobby"));
        Rect r=PlayHelpUI.Window(new Rect(0,0,1600,1000));
        for(int tab=0;tab<4;tab++)
        {Rect button=PlayHelpUI.TabRect(r,tab);yield return OnlineClick(button.center.x,button.center.y);OnlineRequire(playHelpTab==tab,"help tab "+tab+" is interactive");}
        foreach(var key in new[]{KeyCode.D,KeyCode.F,KeyCode.E,KeyCode.W,KeyCode.Return})yield return OnlineKey(key);
        OnlineRequire(showPlayHelp&&!busy&&lobbyTab==oldTab,"help blocks gameplay and navigation shortcuts");
        if(match)OnlineRequire(JsonUtility.ToJson(OnlineMe)==before,"help preserves exact player state and sends no action");
        yield return new WaitForEndOfFrame();OnlineCapture(match?"convenience-online-help":"convenience-lobby-help");
        StarUpgradeAudio.Configure(35,false);var volume=StarSoundUI.VolumeRect(r,3);yield return OnlineClick(volume.center.x,volume.center.y);
        OnlineRequire(StarUpgradeAudio.Volume==50&&PortablePreviewPrefs.GetInt(StarUpgradeAudio.VolumeKey)==50,"online sound volume is saved through F1 controls");
        var mute=StarSoundUI.MuteRect(r);yield return OnlineClick(mute.center.x,mute.center.y);int requests=StarUpgradeAudio.RequestedCount;
        var preview=StarSoundUI.PreviewRect(r,2);yield return OnlineClick(preview.center.x,preview.center.y);
        OnlineRequire(StarUpgradeAudio.Muted&&StarUpgradeAudio.RequestedCount==requests,"online muted sample does not play");
        yield return new WaitForEndOfFrame();OnlineCapture(match?"sound-online-muted":"sound-lobby-muted");
        yield return OnlineClick(mute.center.x,mute.center.y);yield return OnlineClick(preview.center.x,preview.center.y);yield return new WaitForSecondsRealtime(.12f);
        OnlineRequire(!StarUpgradeAudio.Muted&&StarUpgradeAudio.LastPlayedStar==2,"online selected two-star sample plays");
        var reset=StarSoundUI.ResetRect(r);yield return OnlineClick(reset.center.x,reset.center.y);
        OnlineRequire(StarUpgradeAudio.Volume==35&&!StarUpgradeAudio.Muted,"online default sound restores across contexts");
        CombatAudio.Configure(true);var combatToggle=StarSoundUI.CombatRect(r,0);yield return OnlineClick(combatToggle.center.x,combatToggle.center.y);int combatRequests=CombatAudio.RequestedCount;
        var combatSample=StarSoundUI.CombatRect(r,2);yield return OnlineClick(combatSample.center.x,combatSample.center.y);
        OnlineRequire(!CombatAudio.Enabled&&CombatAudio.RequestedCount==combatRequests,"disabled online/lobby combat preview stays silent");
        yield return OnlineClick(combatToggle.center.x,combatToggle.center.y);yield return OnlineClick(combatSample.center.x,combatSample.center.y);
        OnlineRequire(CombatAudio.Enabled&&CombatAudio.LastKind==CombatSound.Kind.Fire&&!CombatAudio.LastSkill,"online/lobby real input previews combat sound");
        Debug.Log("COMBAT AUDIO SETTINGS ONLINE COMPLETE: "+(match?"match":"lobby")+" independent toggle and actual sample input");
        if(match)OnlineRequire(JsonUtility.ToJson(OnlineMe)==before,"sound settings send no server action or inventory change");
        yield return new WaitForEndOfFrame();OnlineCapture(match?"sound-online-settings":"sound-lobby-settings");
        Debug.Log("STAR SOUND SETTINGS ONLINE COMPLETE: "+(match?"match":"lobby")+" F1 tab, volume persistence, mute/sample/defaults, unchanged player state");
        yield return OnlineKey(KeyCode.Escape);OnlineRequire(!showPlayHelp,"Escape closes help without leaving current screen");
        }
        finally{soundSettings.Dispose();}
    }
    IEnumerator ValidateGuideCrafting()
    {
        int bank=OnlineMe.gold,revision=OnlineMe.inventoryRevision;
        OpenOnlineEquipmentGuide(0);yield return null;
        Rect r=DigimonEquipmentUI.PopupRect(onlineEquipmentAnchor,new Rect(0,76,1600,868),0);
        Rect filter=DigimonEquipmentUI.FilterRect(r);yield return OnlineClick(filter.center.x,filter.center.y);
        OnlineRequire(DigimonEquipmentUI.CraftableOnly&&!DigimonEquipmentUI.VisibleRecipes(0,OnlineMe.inventory).Contains(4),"online ready filter rejects duplicate recipe with a single held copy");
        Rect row=DigimonEquipmentUI.RecipeRowRect(r,0);yield return OnlineClick(row.center.x,row.center.y);
        OnlineRequire(onlineItemGuide==5&&OnlineMe.inventoryRevision==revision,"online filtered result is inspected before any server mutation");
        yield return new WaitForEndOfFrame();OnlineCapture("convenience-online-craft");
        bool previousBusy=busy;busy=true;Rect action=DigimonEquipmentUI.CraftRect(r);yield return OnlineClick(action.center.x,action.center.y);busy=previousBusy;
        OnlineRequire(OnlineMe.inventoryRevision==revision,"craft button is disabled while a request is pending");
        yield return OnlineClick(action.center.x,action.center.y);yield return AwaitEquipmentAction();
        OnlineRequire(OnlineMe.inventory.SequenceEqual(new[]{2,3,14,15,16,5})&&OnlineMe.inventoryRevision==revision+1&&OnlineMe.gold==bank,"guide craft reaches server once with exact inventory slots and revision");
        yield return OnlineClick(action.center.x,action.center.y);yield return AwaitEquipmentAction();
        OnlineRequire(OnlineMe.inventoryRevision==revision+1,"repeat click after ingredient removal cannot craft again");
        yield return OnlineKey(KeyCode.Escape);
    }
}
#endif
