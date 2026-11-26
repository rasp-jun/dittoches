#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Linq;

public sealed partial class NativeGame
{
    void ValidateTraitHudCache()
    {
        DigimonTraitHudSnapshotValidation.Run(Require);
        var own=new[]{new Unit(RosterById["agumon"]),new Unit(RosterById["gabumon"]),null};
        own[1].items.Add(17);var scout=new[]{new Unit(RosterById["palmon"]),new Unit(RosterById["patamon"])};
        var first=ReadTraitHud(own,false);int revision=first.RebuildCount;
        ReadTraitHud(scout,true);Require(ReferenceEquals(ReadTraitHud(own,false),first)&&first.RebuildCount==revision,"native own/scout caches stay independent across inspection changes");
        own[0].star=3;Require(ReadTraitHud(own,false).RebuildCount==revision,"star-only changes do not rebuild species/emblem HUD counts");
        own[1].items.Clear();Require(ReadTraitHud(own,false).RebuildCount==revision+1,"native live List equipment removal refreshes the HUD immediately");
        DigimonTraitHudSnapshotValidation.Compare(first,own.Select(BuildMember).ToArray(),DigimonTraitHudSnapshot.RowOrder.NativeOwn,Require,"native raw Unit adapter");
        var saved=(Unit[])board.Clone();
        try
        {
            Array.Clear(board,0,board.Length);board[0]=own[0];board[1]=own[1];
            var rows=BuildTraits();var expected=DigimonBuildCatalog.Data.traits.Select(t=>new TraitEntry(t.category,t.name,DigimonBuildCatalog.Count(t,BoardBuildMembers())))
                .Where(t=>t.count>0).OrderByDescending(t=>DigimonBuildCatalog.Find(t.key).Level(t.count)).ThenByDescending(t=>t.count).ThenBy(t=>t.name).ToArray();
            Require(rows.Select(t=>t.name+":"+t.count).SequenceEqual(expected.Select(t=>t.name+":"+t.count)),"native compatibility list retains the original name tie ordering");
            Require(ReferenceEquals(rows,BuildTraits()),"native compatibility rows are reused while the board stays unchanged");
        }
        finally{Array.Copy(saved,board,board.Length);ReadTraitHud(board,false);}
    }
}
#endif
