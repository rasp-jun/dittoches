using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class MultiLauncher
{
    sealed class OnlineMergeArrival
    {
        public string room,phase;public int side,round;public StarMergeArrival flight;
    }
    readonly List<OnlineMergeArrival> onlineMergeArrivals=new List<OnlineMergeArrival>();
    void ReceiveMergeArrival(Room before,Room after,RecruitReceipt receipt)
    {
        if(receipt.star<2||receipt.slot<0)return;
        var prior=MatchPlayer(before);var next=MatchPlayer(after);var preview=PreviewOnlinePurchase(prior,receipt.id,before);
        if(!preview.available||preview.result.star!=receipt.star||preview.result.slot!=receipt.slot||preview.result.board!=(receipt.area=="board"))return;
        // Attach a visual only when the authoritative result matches every predicted piece.
        if(preview.after.Length!=next.board.Length+next.bench.Length||!preview.after.All(p=>{var u=At(p.board?next.board:next.bench,p.slot);return u!=null&&u.id==p.id&&u.star==p.star&&u.items.SequenceEqual(p.items);}))return;
        int seat=Enumerable.Range(0,9).Where(i=>At(prior.bench,i)==null).DefaultIfEmpty(4).First();
        var flight=StarMergeArrival.Create(preview,MergeFocusUI.Position,TacticalArena.BenchWorld(seat)+Vector3.up*.65f,before.phase=="battle",Time.unscaledTime);
        if(flight==null)return;
        if(onlineMergeArrivals.Count>=4)onlineMergeArrivals.RemoveAt(0);
        onlineMergeArrivals.Add(new OnlineMergeArrival{room=after.id,side=after.side,round=after.round,phase=after.phase,flight=flight});
    }
    void DrawOnlineMergeArrivals(Room room,Player me)
    {
        if(onlineDragging){onlineMergeArrivals.Clear();return;}
        for(int i=onlineMergeArrivals.Count-1;i>=0;i--)
        {
            var record=onlineMergeArrivals[i];var flight=record.flight;
            float now=Time.unscaledTime;
#if DITTOCHES_PORTABLE_PREVIEW
            if(onlineMergeArrivalReviewAge.HasValue)now=flight.started+onlineMergeArrivalReviewAge.Value;
#endif
            var target=At(flight.board?me.board:me.bench,flight.slot);
            if(!flight.Active(now)||record.room!=room.id||record.side!=room.side||record.round!=room.round||record.phase!=room.phase||target==null||target.id!=flight.id||target.star!=flight.star)
            {onlineMergeArrivals.RemoveAt(i);continue;}
            if(artPack!=0||OnlineScouting||MatchFinisherActive)continue;
            Vector3 point=flight.board?TacticalArena.CellWorld(flight.slot%7,flight.slot/7+4):TacticalArena.BenchWorld(flight.slot);
            point+=Vector3.up*(TacticalArena.DigimonHeadHeight(flight.id,flight.star)*(flight.board?1:.85f)*.42f);
            arena.DrawMergeArrival(flight,point,now);
        }
    }
}
