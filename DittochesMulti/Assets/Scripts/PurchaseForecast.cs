using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Read-only simulation of one shop purchase, including both merge stages.</summary>
public sealed class PurchaseForecast
{
    public sealed class Piece
    {
        public string id;
        public bool board;
        public int slot,star;
        public int[] items;
        public Piece(string id,bool board,int slot,int star,IEnumerable<int> items)
        {this.id=id;this.board=board;this.slot=slot;this.star=star;this.items=(items??Enumerable.Empty<int>()).ToArray();}
    }
    public bool available;
    public string reason="";
    public int singles,doubles,triples,cost,gold;
    public Piece result;
    public Piece[] after;
    public sealed class Source
    {
        public bool board,recipient;
        public int slot,star;
    }
    public readonly List<Source> sources=new List<Source>();
    public readonly List<int> returned=new List<int>();
    public DigimonSkillCatalog.Stats Stats
    {
        get
        {
            if(!available||result==null)return null;
            var members=result.board?after.Where(p=>p.board).Select(p=>new DigimonBuildCatalog.Member(p.id,p.items)):Enumerable.Empty<DigimonBuildCatalog.Member>();
            return new DigimonSkillCatalog.Stats(DigimonSkillCatalog.Find(result.id),result.star,DigimonBuildCatalog.Resolve(result.id,members,result.items));
        }
    }
    static IEnumerable<Piece> Ordered(IEnumerable<Piece> pieces)
    {return pieces.OrderBy(p=>p.board?0:1).ThenBy(p=>p.slot);}
    void Merge(List<Piece> pieces,Piece[] sources)
    {
        var keep=sources[0];var equipment=sources.SelectMany(p=>p.items).ToArray();
        foreach(var p in sources)pieces.Remove(p);
        keep.star++;var kept=new List<int>();var grants=new HashSet<string>();
        foreach(int item in equipment)
        {
            string grant=DigimonBuildCatalog.Data.items[item].grantsTrait;
            if(kept.Count>=DigimonBuildCatalog.EquipmentSlots||!string.IsNullOrEmpty(grant)&&grants.Contains(grant))returned.Add(item);
            else{kept.Add(item);if(!string.IsNullOrEmpty(grant))grants.Add(grant);}
        }
        keep.items=kept.ToArray();pieces.Add(keep);
        if(sources.Contains(result))result=keep;
    }
    public static PurchaseForecast Create(string id,IEnumerable<Piece> formation,int benchCapacity,int gold,int cost,bool offered=true)
    {
        var pieces=Ordered(formation??Enumerable.Empty<Piece>()).Select(p=>new Piece(p.id,p.board,p.slot,p.star,p.items)).ToList();
        var own=pieces.Where(p=>p.id==id).ToArray();
        var original=own.Select(p=>new Source{board=p.board,slot=p.slot,star=p.star}).ToArray();
        var forecast=new PurchaseForecast{gold=gold,cost=cost,singles=own.Count(p=>p.star==1),doubles=own.Count(p=>p.star==2),triples=own.Count(p=>p.star==3)};
        if(!offered){forecast.reason="상점이 변경되었습니다 · 현재 모집 목록을 확인하세요";return forecast;}
        if(gold<cost){forecast.reason="골드 "+(cost-gold)+"G 부족 · 보유 기물은 유지됩니다";return forecast;}
        int seat=Enumerable.Range(0,Math.Max(0,benchCapacity)).FirstOrDefault(i=>!pieces.Any(p=>!p.board&&p.slot==i));
        bool free=benchCapacity>0&&!pieces.Any(p=>!p.board&&p.slot==seat);
        if(!free&&forecast.singles<2){forecast.reason="대기석 가득 · 빈칸 또는 같은 1성 기물 2개 필요";return forecast;}
        forecast.available=true;
        if(free)
        {
            forecast.result=new Piece(id,false,seat,1,new int[0]);pieces.Add(forecast.result);
        }
        else
        {
            var pair=Ordered(pieces.Where(p=>p.id==id&&p.star==1)).Take(2).ToArray();
            forecast.result=pair[0];forecast.Merge(pieces,pair);
        }
        bool changed=true;
        while(changed)
        {
            changed=false;
            for(int star=1;star<3;star++)
            {
                var three=Ordered(pieces.Where(p=>p.id==id&&p.star==star)).Take(3).ToArray();
                if(three.Length<3)continue;
                forecast.Merge(pieces,three);changed=true;break;
            }
        }
        forecast.after=Ordered(pieces).ToArray();
        if(forecast.result.star>1)
        {
            foreach(var source in original)
            {
                source.recipient=source.board==forecast.result.board&&source.slot==forecast.result.slot;
                if(source.recipient||!forecast.after.Any(p=>p.board==source.board&&p.slot==source.slot))forecast.sources.Add(source);
            }
            // A newly bought unit can occupy an earlier empty bench seat and
            // become the survivor. Mark that destination even before it exists.
            if(!forecast.sources.Any(p=>p.recipient))forecast.sources.Add(new Source{board=forecast.result.board,slot=forecast.result.slot,star=0,recipient=true});
        }
        return forecast;
    }
}
