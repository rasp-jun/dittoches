using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>HUD-only derived trait rows. Never caches stats, live combat state or input permissions.
/// Catalog documents/arrays are identity checked; edits within an existing catalog must bump its version.</summary>
public sealed class DigimonTraitHudSnapshot
{
    public interface IFormation
    {
        int Count { get; }
        string IdAt(int index);
        IList<int> ItemsAt(int index);
    }
    public enum RowOrder { NativeOwn, OnlineOwn, Scout }
    public sealed class Row
    {
        public readonly DigimonBuildCatalog.Trait trait;
        public readonly int count,tier,target;
        public readonly string progress,description,sortName,onlineCaption,scoutCaption;
        public Row(DigimonBuildCatalog.Trait trait,int count)
        {
            this.trait=trait;this.count=count;tier=trait.Level(count);target=trait.Target(count);
            progress=count+"/"+target;sortName=trait.category+" · "+trait.name;
            onlineCaption="     "+trait.name+"    "+count+" / "+target;scoutCaption=trait.name+"  "+count+" / "+target;
            description=trait.name+" · "+(tier>0?"활성":"다음 단계")+"\n"+trait.tiers[Math.Max(0,tier-1)].text;
        }
    }
    readonly RowOrder order;
    DigimonBuildCatalog.Document catalog;
    DigimonBuildCatalog.Trait[] catalogTraits;
    DigimonBuildCatalog.Item[] catalogItems;
    DigimonBuildCatalog.Member[] members;
    int catalogVersion;
    Row[] all=new Row[0],visible=new Row[0];
    public Row[] Rows { get {return visible;} }
    public Row[] AllRows { get {return all;} }
    public int RebuildCount { get; private set; }
    public int CountEvaluations { get; private set; }
    public DigimonTraitHudSnapshot(RowOrder order){this.order=order;}

    public bool Refresh<T>(T formation) where T:struct,IFormation
    {
        var current=DigimonBuildCatalog.Data;
        bool same=ReferenceEquals(catalog,current)&&catalogVersion==current.version&&ReferenceEquals(catalogTraits,current.traits)&&ReferenceEquals(catalogItems,current.items)&&members!=null&&members.Length==formation.Count;
        if(same)for(int i=0;i<members.Length;i++)
        {
            string id=formation.IdAt(i);var saved=members[i];
            if(saved==null){if(id!=null){same=false;break;}continue;}
            if(saved.id!=id){same=false;break;}
            var gear=formation.ItemsAt(i);int count=gear==null?0:gear.Count;
            if(saved.items.Length!=count){same=false;break;}
            for(int j=0;j<count;j++)if(saved.items[j]!=gear[j]){same=false;break;}
            if(!same)break;
        }
        if(same)return false;
        var copy=new DigimonBuildCatalog.Member[formation.Count];
        for(int i=0;i<copy.Length;i++)
        {
            string id=formation.IdAt(i);
            if(id!=null)copy[i]=new DigimonBuildCatalog.Member(id,formation.ItemsAt(i));
        }
        var next=new Row[current.traits.Length];
        for(int i=0;i<next.Length;i++)
        {next[i]=new Row(current.traits[i],DigimonBuildCatalog.Count(current.traits[i],copy));CountEvaluations++;}
        var sorted=next.Where(r=>r.count>0).OrderByDescending(r=>r.tier);
        if(order==RowOrder.NativeOwn)sorted=sorted.ThenByDescending(r=>r.count).ThenBy(r=>r.sortName);
        else if(order==RowOrder.OnlineOwn)sorted=sorted.ThenByDescending(r=>r.count);
        visible=sorted.ToArray();all=next;members=copy;
        catalog=current;catalogVersion=current.version;catalogTraits=current.traits;catalogItems=current.items;RebuildCount++;
        return true;
    }
    public Row Find(string idOrName)
    {
        for(int i=0;i<all.Length;i++)if(all[i].trait.id==idOrName||all[i].trait.name==idOrName)return all[i];
        return null;
    }
    public bool ContainsSpecies(string id)
    {
        if(members!=null)foreach(var member in members)if(member!=null&&member.id==id)return true;
        return false;
    }
    public DigimonTraitOverview Describe(string id,DigimonTraitHudSnapshot bench,bool benchKnown=true)
    {
        var row=Find(id);
        return row==null?null:DigimonTraitOverview.Create(row.trait,members,bench==null?null:bench.members,benchKnown);
    }
    // A recruitment hint, not a promise that buying changes the deployed formation.
    public Row DeploymentBreakpoint(string id)
    {
        if(ContainsSpecies(id))return null;
        Row best=null;
        foreach(var row in all)
            if(row.tier<row.trait.tiers.Length&&row.target-row.count==1&&row.trait.members.Contains(id)&&
                (best==null||row.tier>best.tier))best=row;
        return best;
    }
}
