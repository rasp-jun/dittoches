#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Linq;

public sealed partial class NativeGame
{
    void ValidateTraitOverview()
    {
        foreach(var trait in DigimonBuildCatalog.Data.traits)
        {
            var deployed=new[]{new DigimonBuildCatalog.Member(trait.members[0]),new DigimonBuildCatalog.Member(trait.members[0]),new DigimonBuildCatalog.Member(trait.members[1])};
            var held=new[]{new DigimonBuildCatalog.Member(trait.members[0]),new DigimonBuildCatalog.Member(trait.members[2]),new DigimonBuildCatalog.Member(trait.members[2])};
            var overview=DigimonTraitOverview.Create(trait,deployed,held);
            Require(overview.count==2&&overview.duplicates==1,"trait overview counts distinct species and explains duplicate: "+trait.id);
            Require(overview.benchCandidates==1&&overview.entries.Count(u=>u.deployed>0)==2,"bench candidate excludes deployed and duplicate species: "+trait.id);
            Require(overview.entries.First().deployed>0&&overview.entries.First(u=>u.id==trait.members[2]).held==2,"roster prioritizes actual contributors and retains bench copies: "+trait.id);
            var unknown=DigimonTraitOverview.Create(trait,deployed,held,false);
            Require(unknown.count==overview.count&&unknown.benchCandidates==0&&unknown.entries.All(u=>u.held==0),"private or unsupported bench never becomes a candidate: "+trait.id);
            var maximum=DigimonTraitOverview.Create(trait,trait.members.Select(id=>new DigimonBuildCatalog.Member(id)),held);
            Require(maximum.Maximum&&maximum.missing==0,"maximum trait tier has no extra requirement: "+trait.id);
            var item=DigimonBuildCatalog.Data.items.FirstOrDefault(i=>i.grantsTrait==trait.id);
            if(item!=null)
            {
                string outsider=TeamPlan.Roster.First(u=>!trait.members.Contains(u.id)).id;
                var emblem=new DigimonBuildCatalog.Member(outsider,new[]{item.id});
                var mixed=deployed.Concat(new[]{new DigimonBuildCatalog.Member(outsider),emblem,emblem}).ToArray();
                var withEmblem=DigimonTraitOverview.Create(trait,mixed,held.Concat(new[]{emblem}));
                Require(withEmblem.count==3&&withEmblem.count==DigimonBuildCatalog.Count(trait,mixed),"emblem overview matches authoritative combat counting: "+trait.id);
                var contributor=withEmblem.entries.Single(u=>u.id==outsider);
                Require(!contributor.innate&&contributor.deployed==2&&withEmblem.benchCandidates==1,"only equipped duplicates contribute and a held emblem copy adds no species: "+trait.id);
                var benchEmblem=DigimonTraitOverview.Create(trait,deployed,new[]{emblem,emblem});
                Require(benchEmblem.benchCandidates==1&&benchEmblem.entries.Single(u=>u.id==outsider).held==2,"bench emblem exposes one new species candidate: "+trait.id);
            }
            for(int take=0;take<=TeamPlan.Roster.Length;take++)
            {
                var formation=TeamPlan.Roster.Take(take).Select(u=>new DigimonBuildCatalog.Member(u.id)).ToArray();
                var actual=DigimonTraitOverview.Create(trait,formation,held);
                Require(actual.count==DigimonBuildCatalog.Count(trait,formation)&&actual.tier==trait.Level(actual.count),"overview agrees with combat for roster prefix "+trait.id+"/"+take);
            }
        }
        var source=new[]{new DigimonBuildCatalog.Member("agumon",new[]{0}),null};
        var snapshot=source.Select(u=>u==null?null:new DigimonBuildCatalog.Member(u.id,u.items)).ToArray();
        Require(DigimonTraitOverview.SameMembers(source,snapshot),"overview cache accepts structurally equal snapshots");
        source[0].items[0]=1;
        Require(!DigimonTraitOverview.SameMembers(source,snapshot)&&snapshot[0].items[0]==0,"gear mutation invalidates overview without mutating cached snapshot");
        Require(!DigimonTraitOverview.SameMembers(source,new[]{source[0]}),"formation resize invalidates overview cache");
    }
}
#endif
