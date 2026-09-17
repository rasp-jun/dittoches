using System;
using System.Linq;

/// <summary>Read-only simulation of a board/bench swap. Duplicate species count once.</summary>
public static class FormationForecast
{
    public sealed class TraitChange
    {
        public DigimonBuildCatalog.Trait trait;public int before,after;
        public int BeforeTier { get {return trait.Level(before);} }
        public int AfterTier { get {return trait.Level(after);} }
    }
    public sealed class Plan
    {
        public bool allowed;public string message;public string[] board,bench;public TraitChange[] traits;
        public bool Changed { get {return traits.Any(t=>t.before!=t.after);} }
    }
    public static Plan Preview(string[] board,string[] bench,bool fromBoard,int source,bool toBoard,int destination,int level)
    {
        var plan=new Plan{board=(string[])board.Clone(),bench=(string[])bench.Clone()};
        var from=fromBoard?plan.board:plan.bench;var to=toBoard?plan.board:plan.bench;
        if(source<0||source>=from.Length||destination<0||destination>=to.Length||string.IsNullOrEmpty(from[source]))
            plan.message="이동할 유닛과 아군 칸을 선택하세요.";
        else if(!fromBoard&&toBoard&&string.IsNullOrEmpty(to[destination])&&board.Count(id=>!string.IsNullOrEmpty(id))>=level)
            plan.message="배치 인원이 가득 찼습니다.\n기존 유닛과 교환하거나 레벨을 올리세요.";
        else
        {
            string displaced=to[destination];to[destination]=from[source];from[source]=displaced;
            plan.allowed=true;plan.message="현재 → 이동 후 · 서로 다른 종류만 집계";
        }
        plan.traits=DigimonBuildCatalog.Data.traits.Select(t=>new TraitChange{trait=t,
            before=DigimonBuildCatalog.Count(t,board),after=DigimonBuildCatalog.Count(t,plan.board)})
            .Where(t=>t.before>0||t.after>0).OrderByDescending(t=>Math.Abs(t.AfterTier-t.BeforeTier))
            .ThenByDescending(t=>t.before!=t.after).ThenByDescending(t=>t.AfterTier).ThenBy(t=>t.trait.name).ToArray();
        if(plan.allowed&&!plan.Changed)plan.message="전장 구성 유지 · 시너지 변화 없음";
        return plan;
    }
}
