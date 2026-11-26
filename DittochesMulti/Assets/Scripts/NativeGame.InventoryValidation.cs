#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    void InventoryClick(Vector2 p){EquipmentGesture(EventType.MouseDown,p);EquipmentGesture(EventType.MouseUp,p);}
    void ValidateEquipmentPopupInGUI()
    {
        var bag=inventory.ToArray();int page=inventoryPage,filter=inventoryView.Filter;var anchor=equipmentGuideAnchor;
        try
        {
            inventory.Clear();inventory.AddRange(new[]{8,0,15,16});inventoryView.SetFilter(1,ref inventoryPage);
            selectedItem=0;int bank=gold;
            EquipmentGesture(EventType.MouseDown,InventorySlotRect(0).center,button:1);
            Require(showRecipeGuide&&recipeFocus==0&&selectedItem==-1&&Event.current.type==EventType.Used,"filtered inventory right click opens clicked recipe and clears equip selection");
            Require(inventory.SequenceEqual(new[]{8,0,15,16})&&gold==bank,"recipe inspection does not mutate inventory or spend gold");
            EquipmentGesture(EventType.MouseDown,new Vector2(1450,980));
            Require(!showRecipeGuide&&Event.current.type==EventType.Used&&gold==bank,"outside click dismisses popup without buying a unit underneath");
            EquipmentGesture(EventType.MouseUp,new Vector2(1450,980));
            Require(gold==bank,"dismiss release cannot trigger underlying button");
            EquipmentGesture(EventType.MouseDown,InventorySlotRect(1).center,button:1);
            Require(showRecipeGuide&&recipeFocus==15,"special device opens its six recipes through filtered slot");
            EquipmentGesture(EventType.MouseDown,RecipeGuideRect.center,button:1);
            Require(!showRecipeGuide&&Event.current.type==EventType.Used,"right click closes popup and consumes input");
            foreach(var size in new[]{new Vector2(1920,1080),new Vector2(1600,1000)})
            foreach(int id in new[]{0,8,14,15,16,26})
            foreach(var point in new[]{Vector2.zero,size,new Vector2(0,size.y),new Vector2(size.x,0)})
            {
                Rect popup=DigimonEquipmentUI.PopupRect(point,new Rect(0,76,size.x,size.y-76),id);
                Require(popup.xMin>=12&&popup.yMin>=88&&popup.xMax<=size.x-12&&popup.yMax<=size.y-12,"context popup stays within canvas at every corner");
            }
            int duplicate=DigimonBuildCatalog.Combine(15,15);
            Require(!DigimonEquipmentUI.CanCraft(duplicate,new[]{15})&&DigimonEquipmentUI.CanCraft(duplicate,new[]{15,15}),"duplicate recipe requires two actual copies");
            Require(DigimonEquipmentUI.Materials(duplicate,new[]{15}).Contains(DigimonBuildCatalog.Data.items[15].name),"duplicate missing material is named");
        }
        finally
        {
            showRecipeGuide=false;equipmentGuideAnchor=anchor;selectedItem=-1;equipmentDrag.Reset();
            inventory.Clear();inventory.AddRange(bag);inventoryView.SetFilter(filter,ref inventoryPage);inventoryPage=page;
        }
    }
    void ValidateInventoryBrowserInGUI()
    {
        var bag=inventory.ToArray();var oldBench=bench[0];int page=inventoryPage,filter=inventoryView.Filter;
        try
        {
            inventory.Clear();inventory.AddRange(new[]{8,14,15,9,16,0});selectedItem=-1;inventoryPage=0;
            inventoryView.SetFilter(0,ref inventoryPage);bench[0]=new Unit(RosterById["agumon"]);
            // IMGUI buttons require control IDs across real frames (covered online).
            // This in-Repaint fixture exercises the shared mapping and item gestures.
            inventoryView.SetFilter(1,ref inventoryPage);inventoryView.Sync(inventory,ref inventoryPage);
            Require(inventoryView.Filter==1&&inventoryView.Count==3,"material tab includes both special devices and hides finished items");
            InventoryClick(InventorySlotRect(0).center);
            Require(selectedItem==2&&inventory[selectedItem]==15,"filtered click preserves authoritative inventory index");
            Require(EquipmentInventoryView.CombinePreview(inventory,selectedItem,4)==26,"device pair preview shows digital gate before crafting");
            Require(EquipmentInventoryView.CombinePreview(inventory,selectedItem,selectedItem)==-1,"one material cannot preview combining with itself");
            InventoryClick(InventorySlotRect(1).center);
            Require(inventory.SequenceEqual(new[]{8,14,9,0,26})&&selectedItem==-1,"filtered material clicks consume exactly the two displayed components");
            inventoryView.SetFilter(2,ref inventoryPage);inventoryView.Sync(inventory,ref inventoryPage);
            Require(inventoryView.Filter==2&&inventoryView.Count==4,"equipment tab includes extractor and capacity artifact");
            Vector2 start=InventorySlotRect(3).center,target=arena.Project(TacticalArena.BenchWorld(0));
            EquipmentGesture(EventType.MouseDown,start);EquipmentGesture(EventType.MouseDrag,target);EquipmentGesture(EventType.MouseUp,target);
            Require(bench[0].items.SequenceEqual(new[]{26})&&inventory.SequenceEqual(new[]{8,14,9,0}),"filtered drag equips displayed artifact rather than raw visual slot");

            inventoryView.SetFilter(0,ref inventoryPage);inventory.Clear();inventory.AddRange(Enumerable.Repeat(0,13));inventory.Add(16);
            inventoryView.Sync(inventory,ref inventoryPage);InventoryClick(InventorySlotRect(0).center);
            Event.current=new Event{type=EventType.ScrollWheel,mousePosition=InventorySlotRect(0).center,delta=new Vector2(0,1)};DrawGame();
            Require(inventoryPage==1&&selectedItem==0&&Event.current.type==EventType.Used,"wheel pages locally and preserves selected source across pages");
            InventoryClick(InventorySlotRect(1).center);
            Require(inventory.Count==13&&inventory.Last()==21,"cross page crafting uses selected original slot");
            inventory.Clear();inventory.Add(8);selectedItem=-1;inventoryPage=1;
            InventoryClick(InventorySlotRect(0).center);
            Require(inventoryPage==0&&selectedItem==0,"inventory shrink clamps page before first hit test");

            inventory.Clear();inventory.AddRange(Enumerable.Repeat(8,13));selectedItem=-1;
            EquipmentGesture(EventType.MouseDown,InventorySlotRect(0).center);EquipmentGesture(EventType.MouseDrag,target);
            Event.current=new Event{type=EventType.ScrollWheel,mousePosition=InventorySlotRect(0).center,delta=new Vector2(0,1)};DrawGame();
            Require(inventoryPage==0&&equipmentDrag.Dragging,"scroll during held gesture cannot change source page");
            inventoryPage=1;EquipmentGesture(EventType.MouseUp,target);
            Require(bench[0].items.SequenceEqual(new[]{26})&&inventory.Count==13&&selectedItem==-1&&Event.current.type==EventType.Used,"page change cancels stale drag even when every item ID is equal");
            var outside=new Event{type=EventType.ScrollWheel,mousePosition=new Vector2(1000,400),delta=new Vector2(0,-1)};
            inventoryView.Scroll(outside,new Rect(28,543,190,202),true,false,ref inventoryPage);
            Require(inventoryPage==1&&outside.type==EventType.ScrollWheel,"inventory wheel does not capture the board");
            inventoryView.SetFilter(1,ref inventoryPage);inventoryView.Sync(inventory,ref inventoryPage);
            Require(inventoryView.Count==0&&inventoryView.Pages==1&&inventoryView.Index(0,0)==-1,"empty category has no selectable ghost slot");
        }
        finally
        {
            equipmentDrag.Reset();selectedItem=-1;inventory.Clear();inventory.AddRange(bag);bench[0]=oldBench;
            inventoryView.SetFilter(filter,ref inventoryPage);inventoryPage=page;inventoryView.Sync(inventory,ref inventoryPage);
            arenaPointer.Reset();
        }
    }
}
#endif
