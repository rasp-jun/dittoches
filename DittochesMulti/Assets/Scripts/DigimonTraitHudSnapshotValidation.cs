#if DITTOCHES_PORTABLE_PREVIEW || DITTOCHES_HUD_CACHE_REVIEW
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class DigimonTraitHudSnapshotValidation
{
    sealed class ProbeUnit
    {
        public string id;public List<int> items;
        public ProbeUnit(string id,params int[] items){this.id=id;this.items=items==null?null:items.ToList();}
    }
    struct Formation:DigimonTraitHudSnapshot.IFormation
    {
        readonly ProbeUnit[] units;
        public Formation(ProbeUnit[] units){this.units=units;}
        public int Count { get {return units==null?0:units.Length;} }
        public string IdAt(int i){return units[i]==null?null:units[i].id;}
        public IList<int> ItemsAt(int i){return units[i]==null?null:units[i].items;}
    }
    static DigimonBuildCatalog.Member[] Members(ProbeUnit[] units)
    {return (units??new ProbeUnit[0]).Select(u=>u==null?null:new DigimonBuildCatalog.Member(u.id,u.items)).ToArray();}
    public static void Compare(DigimonTraitHudSnapshot actual,DigimonBuildCatalog.Member[] members,DigimonTraitHudSnapshot.RowOrder order,Action<bool,string> require,string context)
    {
        var catalog=DigimonBuildCatalog.Data;
        require(actual.AllRows.Length==catalog.traits.Length,"all trait counts including zero remain available: "+context);
        for(int i=0;i<catalog.traits.Length;i++)
        {
            var trait=catalog.traits[i];var row=actual.AllRows[i];int count=DigimonBuildCatalog.Count(trait,members),tier=trait.Level(count),target=trait.Target(count);
            require(ReferenceEquals(row.trait,trait)&&row.count==count&&row.tier==tier&&row.target==target,"cached count/tier/target match canonical catalog: "+context+"/"+trait.id);
            require(ReferenceEquals(actual.Find(trait.id),row)&&ReferenceEquals(actual.Find(trait.name),row),"lookup includes inactive and active traits: "+context+"/"+trait.id);
            require(row.description==trait.name+" · "+(tier>0?"활성":"다음 단계")+"\n"+trait.tiers[Math.Max(0,tier-1)].text,"selected trait description matches the original text: "+context+"/"+trait.id);
        }
        var expected=catalog.traits.Where(t=>DigimonBuildCatalog.Count(t,members)>0).OrderByDescending(t=>t.Level(DigimonBuildCatalog.Count(t,members)));
        if(order==DigimonTraitHudSnapshot.RowOrder.NativeOwn)expected=expected.ThenByDescending(t=>DigimonBuildCatalog.Count(t,members)).ThenBy(t=>t.category+" · "+t.name);
        else if(order==DigimonTraitHudSnapshot.RowOrder.OnlineOwn)expected=expected.ThenByDescending(t=>DigimonBuildCatalog.Count(t,members));
        require(actual.Rows.Select(r=>r.trait.id).SequenceEqual(expected.Select(t=>t.id)),"stable original row order retained: "+context);
    }
    public static void Run(Action<bool,string> require)
    {
        var catalog=DigimonBuildCatalog.Data;var ids=catalog.traits.SelectMany(t=>t.members).Distinct().ToArray();
        foreach(DigimonTraitHudSnapshot.RowOrder order in Enum.GetValues(typeof(DigimonTraitHudSnapshot.RowOrder)))
        {
            var snapshot=new DigimonTraitHudSnapshot(order);
            for(int take=0;take<=ids.Length;take++)
            {
                var units=ids.Take(take).Select(id=>new ProbeUnit(id)).ToArray();
                int calls=snapshot.CountEvaluations;require(snapshot.Refresh(new Formation(units)),"changed roster rebuilds: "+order+"/"+take);
                require(snapshot.CountEvaluations==calls+catalog.traits.Length,"canonical Count runs exactly once per trait on rebuild: "+order+"/"+take);
                Compare(snapshot,Members(units),order,require,order+"/prefix"+take);
                int revision=snapshot.RebuildCount;var rows=snapshot.Rows;
                for(int repeat=0;repeat<20;repeat++)require(!snapshot.Refresh(new Formation(units)),"unchanged raw source reuses the snapshot");
                require(snapshot.RebuildCount==revision&&snapshot.CountEvaluations==calls+catalog.traits.Length&&ReferenceEquals(rows,snapshot.Rows),"warm reads preserve row identity without Count calls");
                var fresh=units.Select(u=>new ProbeUnit(u.id,u.items.ToArray())).ToArray();
                require(!snapshot.Refresh(new Formation(fresh)),"fresh online-style object graph with equal values hits the cache");
            }
            foreach(var item in catalog.items.Where(i=>!string.IsNullOrEmpty(i.grantsTrait)))
            {
                var trait=DigimonBuildCatalog.Find(item.grantsTrait);string outsider=ids.First(id=>!trait.members.Contains(id));
                var units=new[]{new ProbeUnit(outsider),new ProbeUnit(outsider,item.id),new ProbeUnit(trait.members[0]),null};
                snapshot.Refresh(new Formation(units));Compare(snapshot,Members(units),order,require,order+"/emblem"+item.id);
                require(snapshot.Find(trait.id).count==2,"only one equipped duplicate contributes one species: "+trait.id);
                units[1].items.Clear();require(snapshot.Refresh(new Formation(units)),"in-place extractor removal invalidates the snapshot");
                require(snapshot.Find(trait.id).count==1,"extracting the only duplicate emblem removes its participation");
                units[0].items.Add(item.id);require(snapshot.Refresh(new Formation(units)),"equipping another duplicate invalidates in place");
                require(snapshot.Find(trait.id).count==2,"another equipped copy restores exactly one species");
                units[0].items[0]=-1;require(snapshot.Refresh(new Formation(units))&&snapshot.Find(trait.id).count==1,"invalid gear cannot grant a cached trait");
                units[0].items[0]=catalog.items.Length;require(snapshot.Refresh(new Formation(units))&&snapshot.Find(trait.id).count==1,"out-of-range gear cannot grant a cached trait");
            }
            var changing=new[]{new ProbeUnit(ids[0],0,1),new ProbeUnit(ids[1]),null};snapshot.Refresh(new Formation(changing));
            changing[0].items.Reverse();require(snapshot.Refresh(new Formation(changing)),"ordered in-place equipment changes rebuild even if trait totals are equal");
            var held=changing[0];changing[0]=changing[2];changing[2]=held;require(snapshot.Refresh(new Formation(changing)),"board slot swap is observed");
            changing[1]=null;require(snapshot.Refresh(new Formation(changing)),"deployed sale is observed");
            changing[0]=new ProbeUnit(ids[2]);require(snapshot.Refresh(new Formation(changing)),"bench deployment is observed");
            changing[0].id=ids[3];require(snapshot.Refresh(new Formation(changing)),"in-place species replacement is observed");
            Compare(snapshot,Members(changing),order,require,order+"/mutations");
            var nullItems=new[]{new ProbeUnit(ids[0],(int[])null)};snapshot.Refresh(new Formation(nullItems));nullItems[0].items=new List<int>();
            require(!snapshot.Refresh(new Formation(nullItems)),"null and empty equipment are semantically equivalent");
            var empty=new DigimonTraitHudSnapshot(order);empty.Refresh(new Formation(null));
            require(empty.Rows.Length==0&&empty.Find(catalog.traits[0].id).tier==0&&empty.Find(catalog.traits[0].id).description.Contains("다음 단계"),"explicitly selected absent trait retains its next-tier description");
        }
        var versioned=new DigimonTraitHudSnapshot(DigimonTraitHudSnapshot.RowOrder.OnlineOwn);var source=new Formation(new[]{new ProbeUnit(ids[0])});versioned.Refresh(source);
        int version=catalog.version;var originalTraits=catalog.traits;var originalItems=catalog.items;
        int originalThreshold=catalog.traits[0].tiers[0].count;string originalText=catalog.traits[0].tiers[0].text;
        var field=typeof(DigimonBuildCatalog).GetField("data",BindingFlags.Static|BindingFlags.NonPublic);
        try
        {
            catalog.traits[0].tiers[0].count=1;catalog.traits[0].tiers[0].text=originalText+" [review]";
            catalog.version++;require(versioned.Refresh(source),"catalog version invalidates derived rows");
            require(versioned.AllRows[0].tier==1&&versioned.AllRows[0].description.EndsWith(" [review]"),"versioned threshold/text edits update derived tier and description");
            catalog.traits=(DigimonBuildCatalog.Trait[])catalog.traits.Clone();require(versioned.Refresh(source),"replaced trait catalog invalidates derived rows");
            catalog.items=(DigimonBuildCatalog.Item[])catalog.items.Clone();require(versioned.Refresh(source),"replaced item catalog invalidates derived rows");
            var replacement=new DigimonBuildCatalog.Document{version=catalog.version,traits=catalog.traits,items=catalog.items};field.SetValue(null,replacement);
            require(versioned.Refresh(source),"equal-version catalog document replacement invalidates derived rows");
        }
        finally{field.SetValue(null,catalog);catalog.version=version;catalog.traits=originalTraits;catalog.items=originalItems;catalog.traits[0].tiers[0].count=originalThreshold;catalog.traits[0].tiers[0].text=originalText;}
        require(versioned.Refresh(source),"restored catalog refreshes after isolated mutation checks");
        // Shop advice must agree with the same distinct-species/emblem rules used by combat.
        var forecast=new DigimonTraitHudSnapshot(DigimonTraitHudSnapshot.RowOrder.NativeOwn);
        var heldSnapshot=new DigimonTraitHudSnapshot(DigimonTraitHudSnapshot.RowOrder.Scout);
        foreach(var trait in catalog.traits)
        {
            var deployed=trait.members.Take(trait.tiers[0].count-1).Select(id=>new ProbeUnit(id)).ToArray();
            forecast.Refresh(new Formation(deployed));
            string candidate=trait.members[trait.tiers[0].count-1];
            var hint=forecast.DeploymentBreakpoint(candidate);
            var after=Members(deployed).Concat(new[]{new DigimonBuildCatalog.Member(candidate)}).ToArray();
            require(hint!=null&&hint.trait.Level(DigimonBuildCatalog.Count(hint.trait,after))>hint.tier,"shop breakpoint really advances combat tier: "+trait.id);
            require(forecast.DeploymentBreakpoint(deployed[0].id)==null,"duplicate recruitment cannot promise a new synergy species: "+trait.id);
            heldSnapshot.Refresh(new Formation(new[]{new ProbeUnit(candidate),new ProbeUnit(candidate)}));
            var info=forecast.Describe(trait.id,heldSnapshot);
            require(info.benchCandidates==1&&info.count==DigimonBuildCatalog.Count(trait,Members(deployed)),"inline contributor panel includes one candidate for duplicate bench copies: "+trait.id);
            require(forecast.Describe(trait.id,heldSnapshot,false).benchCandidates==0,"unknown scout bench never becomes a suggested candidate: "+trait.id);
            forecast.Refresh(new Formation(deployed.Concat(new[]{new ProbeUnit(candidate)}).ToArray()));
            require(forecast.ContainsSpecies(candidate)&&forecast.DeploymentBreakpoint(candidate)==null,"deploying suggested species invalidates its shop hint: "+trait.id);
        }
        forecast.Refresh(new Formation(ids.Select(id=>new ProbeUnit(id)).ToArray()));
        require(ids.All(id=>forecast.DeploymentBreakpoint(id)==null),"full roster cannot promise another tier for owned species");
    }
}
#endif
