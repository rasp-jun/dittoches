using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class NativeGame
{
    sealed class NativeMergeArrival
    {
        public Unit target;public StarMergeArrival flight;public int round;public bool battle;
    }
    readonly List<NativeMergeArrival> nativeMergeArrivals=new List<NativeMergeArrival>();
    StarMergeArrival CaptureNativeMergeArrival(UnitDef definition)
    {
        var preview=PreviewPurchase(definition);int seat=Array.FindIndex(bench,u=>u==null);
        Vector3 incoming=TacticalArena.BenchWorld(seat>=0?seat:4)+Vector3.up*.65f;
        return StarMergeArrival.Create(preview,NativeMergePosition,incoming,battling,Time.unscaledTime);
    }
    void CommitNativeMergeArrival(StarMergeArrival flight)
    {
        if(flight==null)return;Unit target=flight.board?board[flight.slot]:bench[flight.slot];
        if(target==null||target.def.id!=flight.id||target.star!=flight.star)return;
        if(nativeMergeArrivals.Count>=4)nativeMergeArrivals.RemoveAt(0);
        nativeMergeArrivals.Add(new NativeMergeArrival{target=target,flight=flight,round=round,battle=battling});
    }
    void DrawNativeMergeArrivals()
    {
        for(int i=nativeMergeArrivals.Count-1;i>=0;i--)
        {
            var record=nativeMergeArrivals[i];var flight=record.flight;
            float now=Time.unscaledTime;
#if DITTOCHES_PORTABLE_PREVIEW
            if(nativeMergeArrivalReviewAge.HasValue)now=flight.started+nativeMergeArrivalReviewAge.Value;
#endif
            int boardSlot=Array.IndexOf(board,record.target),benchSlot=Array.IndexOf(bench,record.target);
            if(!flight.Active(now)||record.round!=round||record.battle!=battling||record.target.star!=flight.star||boardSlot<0&&benchSlot<0)
            {nativeMergeArrivals.RemoveAt(i);continue;}
            if(artPack!=0||scoutedRival>=0||NativeFinisherActive)continue;
            Vector3 point;
            if(!formationPositions.TryGetValue(record.target,out point))point=boardSlot>=0?TacticalArena.CellWorld(boardSlot%7,boardSlot/7+4):TacticalArena.BenchWorld(benchSlot);
            point+=Vector3.up*(TacticalArena.DigimonHeadHeight(flight.id,flight.star)*(boardSlot>=0?1:.85f)*.42f);
            arena.DrawMergeArrival(flight,point,now);
        }
    }
}
