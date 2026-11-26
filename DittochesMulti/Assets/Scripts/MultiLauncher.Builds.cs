using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class MultiLauncher
{
    static DigimonBuildCatalog.Member BuildMember(Unit u){return u==null?null:new DigimonBuildCatalog.Member(u.id,u.items);}
    int OnlineFormationLimit(Player me){return Mathf.Min(28,me.level+(me.inventory??new int[0]).Concat(me.board.Concat(me.bench).SelectMany(u=>u.items??new int[0])).Sum(i=>DigimonBuildCatalog.Data.items[i].teamSize));}
    string onlineTrait="";
    readonly DigimonTraitHudSnapshot onlineOwnTraitHud=new DigimonTraitHudSnapshot(DigimonTraitHudSnapshot.RowOrder.OnlineOwn);
    readonly DigimonTraitHudSnapshot onlineScoutTraitHud=new DigimonTraitHudSnapshot(DigimonTraitHudSnapshot.RowOrder.Scout);
    readonly DigimonTraitPanel onlineOwnTraitPanel=new DigimonTraitPanel(),onlineScoutTraitPanel=new DigimonTraitPanel();
    struct OnlineTraitFormation:DigimonTraitHudSnapshot.IFormation
    {
        readonly Unit[] units;
        public OnlineTraitFormation(Unit[] units){this.units=units;}
        public int Count { get {return units==null?0:units.Length;} }
        public string IdAt(int index){return units[index]==null?null:units[index].id;}
        public IList<int> ItemsAt(int index){return units[index]==null?null:units[index].items;}
    }
    DigimonTraitHudSnapshot ReadOnlineTraitHud(Unit[] formation,bool scouting)
    {
        var snapshot=scouting?onlineScoutTraitHud:onlineOwnTraitHud;snapshot.Refresh(new OnlineTraitFormation(formation));return snapshot;
    }
    void DrawOnlineTraits(Player me)
    {
        Unit unit=SelectedOnlineUnit(me);if(unit!=null){DrawOnlineSkillStats(unit);return;}
        var snapshot=ReadOnlineTraitHud(me.board,false);
        BattleHudTheme.Surface(new Rect(1305,490,270,430));
        ArenaPresentationUI.SectionHeading(new Rect(1320,498,165,27),"팀 시너지","");
        if(Btn(new Rect(1497,498,64,28),"도감"))OpenOnlineTrait(onlineTrait);
        string clicked=onlineOwnTraitPanel.Draw(new Rect(1320,532,240,374),snapshot,new OnlineTraitFormation(me.bench));
        if(clicked!=null)OpenOnlineTrait(clicked);
    }
}
