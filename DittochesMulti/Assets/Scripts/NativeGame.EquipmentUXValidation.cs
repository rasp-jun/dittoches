#if DITTOCHES_PORTABLE_PREVIEW
using System.Collections;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    IEnumerator ValidateEquipmentUXInput()
    {
        var bag=inventory.ToArray();var unit=board.First(u=>u!=null);var gear=unit.items.ToArray();
        int page=inventoryPage,filter=inventoryView.Filter,bank=gold;var priorInspection=inspectedUnit;
        string stored=PortablePreviewPrefs.GetString(SaveKey,"");int width=Screen.width,height=Screen.height;
        var random=UnityEngine.Random.state;var target=legendTarget;
        try
        {
            inventory.Clear();inventory.AddRange(new[]{0,1,2,3,14,15,16,0});inventoryView.SetFilter(0,ref inventoryPage);inventoryPage=0;
            unit.items.Clear();unit.items.AddRange(new[]{8,12,13});inspectedUnit=unit;selectedItem=selectedBoard=selectedBench=-1;
            yield return new WaitForEndOfFrame();CaptureRuntime("equipment-01-three-slots");
            yield return SoloLobbyClick(InventorySlotRect(0),1);
            Require(showRecipeGuide&&recipeFocus==0&&selectedItem<0,"actual inventory right click opens read-only equipment guide");
            Rect popup=RecipeGuideRect;
            yield return SoloLobbyClick(DigimonEquipmentUI.RecipeRowRect(popup,0));
            Require(recipeFocus==DigimonBuildCatalog.Combine(0,0)&&RecipeGuideRect==popup,"recipe result opens effects without moving the guide");
            Require(DigimonEquipmentUI.CanCraft(recipeFocus,inventory),"two held copies mark matching recipe craftable");
            yield return new WaitForEndOfFrame();CaptureRuntime("equipment-02-recipe-effects");
            yield return SoloLobbyClick(DigimonEquipmentUI.MaterialRect(popup,0));
            Require(recipeFocus==0,"recipe material returns to its combinations");
            int[] components=DigimonEquipmentUI.ComponentIds();
            for(int i=0;i<components.Length;i++)
            {
                yield return SoloLobbyClick(DigimonEquipmentUI.ComponentRect(popup,i));
                Require(recipeFocus==components[i],"material selector chooses actual component "+components[i]);
                int[] recipes=DigimonEquipmentUI.RecipeResults(components[i]);
                for(int row=0;row<recipes.Length;row++)
                {
                    yield return SoloLobbyClick(DigimonEquipmentUI.RecipeRowRect(popup,row));
                    Require(recipeFocus==recipes[row]&&RecipeGuideRect==popup,"clickable result keeps stable material list and opens correct item "+recipes[row]);
                }
            }
            Require(legendTarget==target&&!TryMoveLegend(new Vector2(1500,560)),"guide blocks pointer and touch movement behind its content");
            foreach(var key in new[]{KeyCode.D,KeyCode.F,KeyCode.E,KeyCode.W,KeyCode.Return})yield return ModalValidationKey(key);
            Require(gold==bank&&!battling&&inventory.SequenceEqual(new[]{0,1,2,3,14,15,16,0})&&unit.items.SequenceEqual(new[]{8,12,13}),"browsing and hotkeys cannot equip craft sell or spend");
            Screen.SetResolution(1280,720,false);yield return new WaitForSecondsRealtime(.4f);
            yield return new WaitForEndOfFrame();CaptureRuntime("equipment-03-guide-720");
            Screen.SetResolution(width,height,false);yield return new WaitForSecondsRealtime(.4f);
            yield return ModalValidationKey(KeyCode.Escape);Require(!showRecipeGuide,"Escape closes equipment guide");
            yield return SoloLobbyClick(new Rect(1830,733,60,49),1);
            Require(showRecipeGuide&&recipeFocus==13,"third equipped slot opens its exact item options");
            yield return new WaitForEndOfFrame();CaptureRuntime("equipment-04-equipped-options");
            yield return SoloLobbyClick(new Rect(24,925,200,54));
            Require(!showRecipeGuide&&legendTarget==target&&gold==bank,"outside dismissal does not leak movement");
            selectedItem=0;validationPointer=InventorySlotRect(1).center;
            yield return new WaitForEndOfFrame();validationPointer=null;
            yield return SoloLobbyClick(new Rect(30,757,184,57));
            Require(showRecipeGuide&&recipeFocus==DigimonBuildCatalog.Combine(0,1),"inventory result preview card opens its effects instead of crafting");
            Require(inventory.SequenceEqual(new[]{0,1,2,3,14,15,16,0})&&PortablePreviewPrefs.GetString(SaveKey,"")==stored,"all recipe browsing preserves exact inventory and saved progress");
            Require(UnityEngine.Random.state.Equals(random),"equipment browsing consumes no gameplay RNG");
        }
        finally
        {
            showRecipeGuide=false;selectedItem=-1;validationPointer=null;equipmentDrag.Reset();arenaPointer.Reset();
            unit.items.Clear();unit.items.AddRange(gear);inventory.Clear();inventory.AddRange(bag);
            inventoryView.SetFilter(filter,ref inventoryPage);inventoryPage=page;inspectedUnit=priorInspection;
            Screen.SetResolution(width,height,false);
        }
    }
}
#endif
