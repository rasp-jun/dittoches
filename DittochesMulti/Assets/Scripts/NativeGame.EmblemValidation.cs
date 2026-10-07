#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    void ValidateEmblems()
    {
        artPack=0;battling=false;showCarousel=false;scoutedRival=-1;hp=100;level=3;gold=100;
        Array.Clear(board,0,board.Length);Array.Clear(bench,0,bench.Length);
        foreach(int a in new[]{0,1,2,3,15,16})foreach(int b in new[]{0,1,2,3,15,16})
        {
            int made=DigimonBuildCatalog.Combine(a,b);Require(made>=4&&made==DigimonBuildCatalog.Combine(b,a),"all six-material combinations symmetric");
            inventory.Clear();inventory.AddRange(new[]{a,b});selectedItem=-1;SelectInventoryItem(0);SelectInventoryItem(1);
            Require(inventory.SequenceEqual(new[]{made}),"special material combination consumes exact slots");
        }
        foreach(var item in DigimonBuildCatalog.Data.items.Where(i=>i.kind=="emblem"))
        {
            var trait=DigimonBuildCatalog.Find(item.grantsTrait);string foreign=Roster.First(u=>!trait.members.Contains(u.id)).id;
            Array.Clear(board,0,board.Length);board[0]=new Unit(RosterById[trait.members[0]]);board[1]=new Unit(RosterById[foreign]);
            inventory.Clear();inventory.Add(item.id);selectedItem=0;Equip(board[1]);
            Require(board[1].items.Contains(item.id)&&DigimonBuildCatalog.Count(trait,BoardBuildMembers())==2,"emblem adds foreign species to synergy "+item.grantsTrait);
            board[2]=new Unit(RosterById[foreign]);board[2].items.Add(item.id);
            Require(DigimonBuildCatalog.Count(trait,BoardBuildMembers())==2,"same species emblem never double counts");
            Require(!DigimonBuildCatalog.PreviewEquipment(board[1].items,item.id,foreign).allowed&&!DigimonBuildCatalog.PreviewEquipment(new int[0],item.id,trait.members[0]).allowed,"natural and duplicate traits reject without consumption");
        }
        Array.Clear(board,0,board.Length);Array.Clear(bench,0,bench.Length);inventory.Clear();inventory.AddRange(new[]{25,26,27});
        Require(FormationLimit==6,"three owned artifacts stack independent of level");
        bench[0]=new Unit(RosterById["gabumon"]);selectedItem=0;Equip(bench[0]);Require(FormationLimit==6,"equipping owned capacity never counts twice");
        selectedBench=0;selectedBoard=-1;SellSelectedUnit();Require(FormationLimit==6&&inventory.Count==3,"selling carrier returns capacity without losing limit");
        board[3]=new Unit(RosterById["gabumon"]);board[3].items.Add(17);Save();Require(Load()&&board[3].items.Contains(17)&&FormationLimit==6,"save roundtrip keeps emblem and capacity artifacts");
        var keep=board[3];inventory.Clear();AssignMergedEquipment(keep,new[]{17,17,8});Require(keep.items.SequenceEqual(new[]{17,8})&&inventory.SequenceEqual(new[]{17}),"merge returns duplicate emblem without losing useful second item");
        Array.Clear(board,0,board.Length);board[3]=new Unit(RosterById["agumon"]);board[4]=new Unit(RosterById["gabumon"]);
        inventory.Clear();inventory.Add(17);SetupBattle();battling=true;
        var ally=fighters.First(f=>!f.enemy&&f.sourceSlot==3);var holder=fighters.First(f=>!f.enemy&&f.sourceSlot==4);
        float oldAttack=AttackDamage(ally);holder.hp=holder.maxHp*.4f;holder.mana=7;holder.lowShieldUsed=true;
        var forecast=DigimonEquipmentPreview.Create(holder.unit.def.id,holder.unit.star,CombatBuildMembers(false),holder.unit.items,17);
        DigimonEquipmentPreview.SetCombatState(forecast,holder.hp,holder.maxHp,holder.mana);
        selectedItem=0;Equip(holder.unit);
        Require(AttackDamage(ally)>oldAttack&&holder.build.attack>0,"live emblem updates whole frozen team");
        Require(Mathf.Abs(holder.hp-forecast.projectedHealth)<.01f&&holder.mana==7&&holder.lowShieldUsed,"live emblem preview matches application without resetting battle");
        inventory.Add(14);selectedItem=0;Equip(holder.unit);Require(Mathf.Abs(AttackDamage(ally)-oldAttack)<.01f&&holder.mana==7,"live extraction removes team membership without resetting mana");
        fighters.Clear();skillCasts.Clear();battling=false;selectedItem=selectedBoard=selectedBench=-1;
    }
}
#endif
