using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Read-only formation information. Uses the same species/emblem rules as combat.</summary>
public sealed class DigimonTraitOverview
{
    public sealed class Entry
    {
        public string id;
        public int deployed,held;
        public bool innate;
    }
    public DigimonBuildCatalog.Trait trait;
    public Entry[] entries;
    public int count,tier,missing,duplicates,benchCandidates;
    public bool benchKnown;
    public bool Maximum { get { return tier==trait.tiers.Length; } }
    public string Progress
    {
        get
        {
            if(Maximum)return "최대 단계 달성 · 추가 배치로 단계는 오르지 않습니다";
            string next=(tier+1)+"단계까지 서로 다른 "+missing+"종 필요";
            if(!benchKnown)return next+" · 상대 대기석 정보 미수신";
            return next+" · 대기석 후보 "+benchCandidates+"종";
        }
    }
    public string Scope
    {
        get
        {
            var bonus=trait.tiers[Math.Max(0,tier-1)].bonus;
            if(bonus!=null&&(bonus.teamShield>0||bonus.teamResist>0))return "효과 대상: 모든 아군 · 인원수만큼 중복 적용되지 않음";
            if(bonus!=null&&bonus.castHeal>0)return "발동 주체: "+trait.name+" 디지몬 · 회복 대상: 모든 아군 중 1명";
            if(bonus!=null&&bonus.castAllyShield>0)return "발동 주체: "+trait.name+" 디지몬 · 보호막 대상: 체력 비율이 가장 낮은 아군 1명";
            return "효과 대상: "+trait.name+" 디지몬 · 인장으로 참여한 디지몬 포함";
        }
    }
    public static DigimonTraitOverview Create(DigimonBuildCatalog.Trait trait,IEnumerable<DigimonBuildCatalog.Member> board,IEnumerable<DigimonBuildCatalog.Member> bench,bool benchKnown=true)
    {
        var deployed=Matching(trait,board);var held=benchKnown?Matching(trait,bench):new DigimonBuildCatalog.Member[0];
        var fieldIds=new HashSet<string>(deployed.Select(u=>u.id));
        var entries=trait.members.Concat(fieldIds).Concat(held.Select(u=>u.id)).Distinct().Select(id=>new Entry
        {
            id=id,innate=trait.members.Contains(id),deployed=deployed.Count(u=>u.id==id),held=held.Count(u=>u.id==id)
        }).OrderByDescending(u=>u.deployed>0).ThenByDescending(u=>u.held>0).ToArray();
        int count=fieldIds.Count,tier=trait.Level(count);
        return new DigimonTraitOverview
        {
            trait=trait,entries=entries,count=count,tier=tier,missing=Math.Max(0,trait.Target(count)-count),
            duplicates=deployed.Length-count,benchCandidates=held.Select(u=>u.id).Distinct().Count(id=>!fieldIds.Contains(id)),benchKnown=benchKnown
        };
    }
    static DigimonBuildCatalog.Member[] Matching(DigimonBuildCatalog.Trait trait,IEnumerable<DigimonBuildCatalog.Member> members)
    {return (members??Enumerable.Empty<DigimonBuildCatalog.Member>()).Where(u=>u!=null&&!string.IsNullOrEmpty(u.id)&&DigimonBuildCatalog.HasTrait(trait,u.id,u.items)).ToArray();}
    public static bool SameMembers(DigimonBuildCatalog.Member[] a,DigimonBuildCatalog.Member[] b)
    {
        if(a==null||b==null||a.Length!=b.Length)return a==b;
        for(int i=0;i<a.Length;i++)
        {
            if(a[i]==null||b[i]==null){if(a[i]!=b[i])return false;continue;}
            if(a[i].id!=b[i].id||!(a[i].items??new int[0]).SequenceEqual(b[i].items??new int[0]))return false;
        }
        return true;
    }
}
