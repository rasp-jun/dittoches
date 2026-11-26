using System;
using System.Collections.Generic;
using System.Linq;

// A live, read-only view of formation ranks and current shop offers.
public sealed class StarCollection
{
    public struct Piece
    {
        public string id;public int star;public bool board;
        public Piece(string id,int star,bool board){this.id=id;this.star=star;this.board=board;}
    }
    public sealed class Entry
    {
        public string id,name,blocked;public int cost,singles,doubles,triples,board,bench,offers,gold;
        public bool planned,allowed,benchFull;
        public int Owned {get{return singles+doubles+triples;}}
        public int Copies {get{return singles+3*doubles+9*triples;}}
        public bool Complete {get{return triples>0;}}
        public bool OneAway {get{return !Complete&&singles>=2;}}
        public int NextStar {get{return doubles>=2?3:2;}}
        public int Goal {get{return doubles>0||Complete?9:3;}}
        public bool CanBuy {get{return offers>0&&!Complete&&allowed&&gold>=cost&&(!benchFull||OneAway);}}
        public bool CanUpgrade {get{return OneAway&&CanBuy;}}
        public string Progress {get{return Complete?"★★★ 완성 · 최고 성급":Math.Min(Goal,Copies)+" / "+Goal+"개 · "+(Goal==9?"3성":"2성")+"까지 "+Math.Max(0,Goal-Copies)+"개";}}
        public string Status
        {
            get
            {
                if(Complete)return "최고 성급 완성";
                if(offers==0)return OneAway?"한 개 더 모으면 "+new string('★',NextStar)+" 강화":"현재 상점에 없음";
                string label="상점 "+offers+"개 · ";
                if(!allowed)return label+blocked;
                if(gold<cost)return label+(cost-gold)+"G 부족";
                if(benchFull&&!OneAway)return label+"대기석 가득";
                return label+(OneAway?"이번 구매 "+new string('★',NextStar)+" 강화":"모집 가능 · "+cost+"G");
            }
        }
    }
    public Entry[] entries;
    public string context;
    public static StarCollection Create(IEnumerable<Piece> formation,IEnumerable<string> shop,IEnumerable<string> goals,int gold,bool allowed,string blocked,string context)
    {
        var pieces=(formation??Enumerable.Empty<Piece>()).ToArray();var offers=(shop??Enumerable.Empty<string>()).ToArray();
        var planned=new HashSet<string>(goals??Enumerable.Empty<string>());bool full=pieces.Count(p=>!p.board)>=9;
        var entries=new List<Entry>();
        foreach(var def in TeamPlan.Roster)
        {
            var own=pieces.Where(p=>p.id==def.id).ToArray();if(own.Length==0&&!planned.Contains(def.id))continue;
            entries.Add(new Entry{id=def.id,name=def.name,cost=def.cost,singles=own.Count(p=>p.star<=1),doubles=own.Count(p=>p.star==2),triples=own.Count(p=>p.star>=3),
                board=own.Count(p=>p.board),bench=own.Count(p=>!p.board),offers=offers.Count(id=>id==def.id),planned=planned.Contains(def.id),
                gold=gold,allowed=allowed,blocked=blocked??"현재 모집 불가",benchFull=full});
        }
        return new StarCollection{entries=entries.ToArray(),context=context??"현재 편성 기준"};
    }
    public Entry[] View(int filter,string query)
    {
        var matches=new HashSet<string>(TeamPlan.Search(query,0).Select(d=>d.id));
        return entries.Where(e=>matches.Contains(e.id)&&(filter==1?e.OneAway:filter==2?e.offers>0:filter==3?e.planned:e.Owned>0))
            .OrderByDescending(e=>e.CanUpgrade).ThenByDescending(e=>e.OneAway).ThenBy(e=>e.Complete).ThenByDescending(e=>e.Copies)
            .ThenBy(e=>e.cost).ThenBy(e=>e.id,StringComparer.Ordinal).ToArray();
    }
}
