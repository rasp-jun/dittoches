#if DITTOCHES_PORTABLE_PREVIEW
using System.Collections;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    IEnumerator ValidateConvenienceInput()
    {
        var bag=inventory.ToArray();int bank=gold;string saved=PortablePreviewPrefs.GetString(SaveKey,"");var target=legendTarget;
        int width=Screen.width,height=Screen.height;var soundSettings=new StarSoundValidation.Preferences();
        try
        {
            yield return StarSoundValidation.Playback(Require);
            inventory.Clear();inventory.AddRange(new[]{0,1,15});selectedItem=-1;
            OpenEquipmentGuide(0);yield return ModalValidationKey(KeyCode.F1);
            Require(showPlayHelp&&showRecipeGuide,"F1 help preserves the underlying equipment guide");
            var help=PlayHelpUI.Window(new Rect(0,0,1920,1080));
            yield return SoloLobbyClick(PlayHelpUI.TabRect(help,1));Require(playHelpTab==1,"help equipment tab receives real clicks");
            foreach(var key in new[]{KeyCode.D,KeyCode.F,KeyCode.E,KeyCode.W,KeyCode.Return})yield return ModalValidationKey(key);
            Require(gold==bank&&!battling&&inventory.SequenceEqual(new[]{0,1,15})&&legendTarget==target&&!TryMoveLegend(new Vector2(1000,600)),"help blocks gameplay shortcuts and touch without changing progress");
            yield return new WaitForEndOfFrame();CaptureRuntime("convenience-01-help");
            Screen.SetResolution(1280,720,false);yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();CaptureRuntime("convenience-02-help-720");
            Screen.SetResolution(width,height,false);yield return new WaitForSecondsRealtime(.4f);
            yield return SoloLobbyClick(PlayHelpUI.TabRect(help,3));Require(playHelpTab==3,"native F1 sound tab is reachable");
            StarUpgradeAudio.Configure(35,false);yield return SoloLobbyClick(StarSoundUI.VolumeRect(help,3));
            Require(StarUpgradeAudio.Volume==50&&PortablePreviewPrefs.GetInt(StarUpgradeAudio.VolumeKey)==50,"native sound volume updates and saves through actual UI");
            yield return SoloLobbyClick(StarSoundUI.MuteRect(help));int requests=StarUpgradeAudio.RequestedCount;
            yield return SoloLobbyClick(StarSoundUI.PreviewRect(help,2));
            Require(StarUpgradeAudio.Muted&&StarUpgradeAudio.RequestedCount==requests,"muted preview button cannot queue a sound");
            yield return new WaitForEndOfFrame();CaptureRuntime("sound-native-muted");
            yield return SoloLobbyClick(StarSoundUI.MuteRect(help));yield return SoloLobbyClick(StarSoundUI.PreviewRect(help,3));yield return new WaitForSecondsRealtime(.12f);
            Require(!StarUpgradeAudio.Muted&&StarUpgradeAudio.LastPlayedStar==3,"native preview emits selected rank without changing the match");
            yield return SoloLobbyClick(StarSoundUI.ResetRect(help));Require(StarUpgradeAudio.Volume==35&&!StarUpgradeAudio.Muted,"native sound default restores audible 35 percent");
            CombatAudio.Configure(true);yield return SoloLobbyClick(StarSoundUI.CombatRect(help,0));int combatRequests=CombatAudio.RequestedCount;
            yield return SoloLobbyClick(StarSoundUI.CombatRect(help,1));Require(!CombatAudio.Enabled&&CombatAudio.RequestedCount==combatRequests,"native disabled combat sample stays silent");
            yield return SoloLobbyClick(StarSoundUI.CombatRect(help,0));yield return SoloLobbyClick(StarSoundUI.CombatRect(help,3));
            Require(CombatAudio.Enabled&&CombatAudio.LastKind==CombatSound.Kind.Holy&&CombatAudio.LastSkill,"native combat toggle and skill sample use real clicks");
            Debug.Log("COMBAT AUDIO SETTINGS NATIVE COMPLETE: independent toggle and disabled/enabled samples through real F1 input");
            Require(gold==bank&&!battling&&inventory.SequenceEqual(new[]{0,1,15}),"sound settings cannot spend gold or change inventory and phase");
            yield return new WaitForEndOfFrame();CaptureRuntime("sound-native-settings");
            Screen.SetResolution(1280,720,false);yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();CaptureRuntime("sound-native-settings-720");
            Screen.SetResolution(width,height,false);yield return new WaitForSecondsRealtime(.4f);
            Debug.Log("STAR SOUND SETTINGS NATIVE COMPLETE: F1 tab, saved volume, mute-disabled preview, rank preview, defaults and 720p layout");
            yield return ModalValidationKey(KeyCode.Escape);Require(!showPlayHelp&&showRecipeGuide,"Escape closes only help and returns to previous guide");
            Rect popup=RecipeGuideRect;
            yield return SoloLobbyClick(DigimonEquipmentUI.FilterRect(popup));
            Require(DigimonEquipmentUI.CraftableOnly&&DigimonEquipmentUI.VisibleRecipes(0,inventory).SequenceEqual(new[]{5,17}),"ready filter uses actual inventory and omits unavailable duplicate ingredients");
            yield return SoloLobbyClick(DigimonEquipmentUI.RecipeRowRect(popup,0));
            Require(recipeFocus==5&&inventory.SequenceEqual(new[]{0,1,15}),"filtered result click only inspects the matching recipe");
            yield return new WaitForEndOfFrame();CaptureRuntime("convenience-03-ready-recipes");
            yield return SoloLobbyClick(DigimonEquipmentUI.CraftRect(popup));
            Require(inventory.SequenceEqual(new[]{15,5})&&gold==bank&&showRecipeGuide,"explicit craft consumes two exact materials and keeps guide open");
            yield return SoloLobbyClick(DigimonEquipmentUI.CraftRect(popup));
            Require(inventory.SequenceEqual(new[]{15,5})&&DigimonEquipmentUI.VisibleRecipes(0,inventory).Length==0,"empty recipe list and repeated disabled craft cannot consume other items");
            yield return new WaitForEndOfFrame();CaptureRuntime("convenience-04-crafted");
            foreach(var item in DigimonBuildCatalog.Data.items.Where(i=>i.recipe!=null&&i.recipe.Length==2))
            {
                var held=new[]{14,item.recipe[0],14,item.recipe[1]};var slots=DigimonEquipmentUI.CraftSlots(item.id,held);
                Require(slots.SequenceEqual(new[]{1,3})&&held[slots[0]]==item.recipe[0]&&held[slots[1]]==item.recipe[1],"craft locates two distinct authoritative inventory slots for "+item.id);
                Require(DigimonEquipmentUI.CraftSlots(item.id,new[]{item.recipe[0]}).Length==0,"missing material prevents craft for "+item.id);
            }
            yield return ModalValidationKey(KeyCode.Escape);
            lobby=true;yield return ModalValidationKey(KeyCode.F1);Require(showPlayHelp&&lobby,"F1 help opens from solo preparation too");
            yield return ModalValidationKey(KeyCode.F1);Require(!showPlayHelp&&lobby,"F1 closes help without starting or leaving preparation");
        }
        finally
        {
            soundSettings.Dispose();lobby=false;showPlayHelp=false;showRecipeGuide=false;selectedItem=-1;inventory.Clear();inventory.AddRange(bag);
            PortablePreviewPrefs.SetString(SaveKey,saved);Screen.SetResolution(width,height,false);
        }
    }
}
#endif
