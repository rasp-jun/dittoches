#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    string PurchaseState()
    {return string.Join("|",board.Concat(bench).Select(u=>JsonUtility.ToJson(SaveUnit(u))).ToArray())+" / "+gold+" / "+string.Join(",",inventory.Select(i=>i.ToString()).ToArray());}
    void ValidatePurchaseForecasts()
    {
        var savedBoard=board.ToArray();var savedBench=bench.ToArray();var savedShop=shop.ToArray();var savedItems=inventory.ToArray();var savedPool=pool.ToArray();
        var savedArrivals=nativeMergeArrivals.ToArray();var savedFighters=fighters.ToArray();var savedPromotions=promotions.ToArray();var savedNotice=promotionNoticeUnit;float savedUntil=promotionNoticeUntil;
        string savedPlacement=placementNotice;float savedPlacementUntil=placementNoticeUntil;int savedGold=gold;bool savedBattle=battling;int start=validationChecks,cases=0;
        try
        {
            battling=false;fighters.Clear();
            foreach(var definition in Roster)
            for(int layout=0;layout<3;layout++)for(int singles=0;singles<=2;singles++)for(int doubles=0;doubles<=2;doubles++)
            {
                Array.Clear(board,0,board.Length);Array.Clear(bench,0,bench.Length);Array.Clear(shop,0,shop.Length);inventory.Clear();promotions.Clear();nativeMergeArrivals.Clear();gold=50;
                if(layout==2)for(int i=0;i<bench.Length;i++)bench[i]=new Unit(RosterById[definition.id=="koromon"?"agumon":"koromon"]){star=3};
                for(int i=0;i<doubles;i++)
                {var u=new Unit(definition){star=2};u.items.AddRange(i==0?new[]{17,8,12}:new[]{17,13,6});if(layout==1)bench[2+i]=u;else board[i]=u;}
                for(int i=0;i<singles;i++)
                {var u=new Unit(definition);u.items.AddRange(new[]{17,2,3});if(layout==1)bench[7+i]=u;else if(layout==2)bench[i]=u;else board[10+i]=u;}
                shop[0]=definition;shop[1]=definition;
                string before=PurchaseState();var preview=PreviewPurchase(definition);
                Require(before==PurchaseState(),"purchase forecast never mutates formation, gold or inventory");
                bool bought=Buy(0);Require(preview.available==bought,"forecast availability matches native purchase");
                Require(nativeMergeArrivals.Count==(artPack==0&&bought&&preview.result.star>1?1:0),"only successful native star merges start one arrival");
                if(!bought){Require(before==PurchaseState(),"rejected native purchase stays unchanged");continue;}
                var actual=preview.result.board?board[preview.result.slot]:bench[preview.result.slot];
                Require(actual!=null&&actual.def.id==definition.id&&actual.star==preview.result.star&&actual.items.SequenceEqual(preview.result.items),"forecast survivor and retained equipment match native merge "+definition.id+":"+layout+":"+singles+":"+doubles);
                Require(inventory.SequenceEqual(preview.returned)&&gold==50-definition.cost,"forecast returned item identities/order and gold match native purchase");
                Require(preview.after.Length==board.Concat(bench).Count(u=>u!=null)&&preview.after.All(p=>{var u=p.board?board[p.slot]:bench[p.slot];return u!=null&&u.def.id==p.id&&u.star==p.star&&u.items.SequenceEqual(p.items);}),"forecast full formation matches actual purchase");
                if(preview.result.star>1)
                {
                    if(artPack==0)Require(nativeMergeArrivals[0].target==actual&&nativeMergeArrivals[0].flight.origins.Length==(actual.star==3?4:2),"arrival uses exact consumed sources and actual surviving unit");
                    Require(preview.sources.Count(s=>s.recipient)==1&&preview.sources.Where(s=>s.recipient).All(s=>s.board==preview.result.board&&s.slot==preview.result.slot),"merge focus has exactly one actual final destination");
                    Require(preview.sources.Sum(s=>s.star>0?(int)Mathf.Pow(3,s.star-1):0)+1==(int)Mathf.Pow(3,actual.star-1),"merge focus marks exactly the copies used in the final upgrade");
                    Require(preview.sources.Where(s=>!s.recipient).All(s=>(s.board?board[s.slot]:bench[s.slot])==null),"highlighted materials actually disappear after purchase");
                }
                else Require(preview.sources.Count==0,"plain purchase does not fabricate merge highlights");
                var expected=InspectStats(actual);var predicted=preview.Stats;
                Require(Mathf.Abs(expected.health-predicted.health)<.01f&&Mathf.Abs(expected.attack-predicted.attack)<.001f&&Mathf.Abs(expected.abilityPower-predicted.abilityPower)<.001f,"forecast prepared stats include actual retained gear and resulting board traits");cases++;
            }
            Array.Clear(board,0,board.Length);Array.Clear(bench,0,bench.Length);shop[0]=RosterById["agumon"];gold=0;
            Require(!PreviewPurchase(shop[0]).available&&!Buy(0),"no-gold forecast matches rejected purchase");
            shop[0]=null;gold=20;Require(!PreviewPurchase(RosterById["agumon"]).available,"stale shop details cannot promise a removed offer");
            Debug.Log("PURCHASE FORECAST NATIVE COMPLETE: "+cases+" successful actual purchases, "+(validationChecks-start)+" checks; all 47 species, board/bench/full bench, cascades, duplicate emblems, items, stats and rejection");
        }
        finally
        {
            savedBoard.CopyTo(board,0);savedBench.CopyTo(bench,0);savedShop.CopyTo(shop,0);inventory.Clear();inventory.AddRange(savedItems);pool.Clear();foreach(var pair in savedPool)pool[pair.Key]=pair.Value;
            nativeMergeArrivals.Clear();nativeMergeArrivals.AddRange(savedArrivals);fighters.Clear();fighters.AddRange(savedFighters);promotions.Clear();foreach(var pair in savedPromotions)promotions[pair.Key]=pair.Value;
            promotionNoticeUnit=savedNotice;promotionNoticeUntil=savedUntil;placementNotice=savedPlacement;placementNoticeUntil=savedPlacementUntil;gold=savedGold;battling=savedBattle;
        }
    }
}
#endif
