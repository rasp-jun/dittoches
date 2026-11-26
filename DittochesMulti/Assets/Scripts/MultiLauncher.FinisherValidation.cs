#if DITTOCHES_PORTABLE_PREVIEW
using UnityEngine;

public sealed partial class MultiLauncher
{
    void ValidateFinisherRouting()
    {
        string oldRoom=cosmeticRoom;int oldReport=cosmeticReport;var oldScene=matchFinisher;
        var tracks=onlineTamers;string movementRoom=tamerRoom;int pending=pendingTamerSequence,intent=tamerIntentSequence;
        try
        {
            var room=new Room{id="finisher-routing-fixture",side=7,phase="prepare",opponentSide=1,reportOpponentSide=3,players=new Player[8]};
            for(int i=0;i<8;i++)room.players[i]=new Player{name="테이머 "+i,tamer=i%4,finisher=i%3,tamerX=1+i%5,tamerY=6};
            var me=room.players[7];ObserveMatchFinisher(room,me,room.players[1]);
            room.reportRound=1;room.roundWinner=7;ObserveMatchFinisher(room,me,room.players[1]);
            var scene=matchFinisher;
            OnlineRequire(scene!=null&&scene.OpponentSide==3&&scene.RivalName=="테이머 3"&&scene.RivalTamer==3,"finisher binds to fought global opponent, not next pairing");
            OnlineRequire(scene.Source==TacticalArena.CellWorld(me.tamerX,me.tamerY)&&scene.Target==TacticalArena.CellWorld(6-room.players[3].tamerX,7-room.players[3].tamerY),"winning shot flies from own tamer to mirrored fought tamer");
            Vector3 frozen=scene.Target;room.players[3].tamerX=6;room.players[3].tamer=2;room.players[3].name="changed";
            ObserveMatchFinisher(room,me,room.players[1]);
            OnlineRequire(ReferenceEquals(scene,matchFinisher)&&scene.Target==frozen&&scene.RivalTamer==3&&scene.RivalName=="테이머 3","movement, loadout and repeated state updates cannot retarget the shot");
            room.reportRound=2;room.roundWinner=3;ObserveMatchFinisher(room,me,room.players[1]);
            OnlineRequire(matchFinisher.Winner==1&&matchFinisher.Target==matchFinisher.OwnPosition&&matchFinisher.Finisher==room.players[3].finisher,"defeat uses the actual opponent's effect directed at own tamer");
            room.reportRound=3;room.roundWinner=1;ObserveMatchFinisher(room,me,room.players[1]);
            OnlineRequire(matchFinisher==null,"unrelated duel winner never starts a finisher");
            room.reportRound=4;room.roundWinner=-1;ObserveMatchFinisher(room,me,room.players[1]);
            OnlineRequire(matchFinisher==null,"draw never executes either tamer");
            room.id="reconnected-fixture";room.roundWinner=7;ObserveMatchFinisher(room,me,room.players[1]);
            OnlineRequire(matchFinisher==null,"entering an existing report never replays a historical execution");
        }
        finally
        {
            cosmeticRoom=oldRoom;cosmeticReport=oldReport;matchFinisher=oldScene;
            onlineTamers=tracks;tamerRoom=movementRoom;pendingTamerSequence=pending;tamerIntentSequence=intent;
        }
    }
}
#endif
