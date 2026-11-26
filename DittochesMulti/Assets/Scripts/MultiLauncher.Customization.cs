using UnityEngine;

public sealed partial class MultiLauncher
{
    TamerLoadout lobbyLoadout;
    LoadoutStudio loadoutStudio;
    string cosmeticRoom="";
    int cosmeticReport;
    TamerFinisherScene matchFinisher;
    bool MatchFinisherActive {get{return matchFinisher!=null&&matchFinisher.Active(Time.unscaledTime);}}
    void DrawLoadoutLobby()
    {
        if(loadoutStudio==null)loadoutStudio=new LoadoutStudio();
        loadoutStudio.Draw(new Rect(55,110,1490,790),ref lobbyLoadout,!busy&&(state==null||string.IsNullOrEmpty(state.queue)));
    }
    void ObserveMatchFinisher(Room room,Player me,Player enemy)
    {
        if(cosmeticRoom!=room.id)
        {
            cosmeticRoom=room.id;cosmeticReport=room.reportRound;matchFinisher=null;
            EnsureTamerTimelines(room);
        }
        if(room.reportRound>cosmeticReport)
        {
            cosmeticReport=room.reportRound;
            matchFinisher=null;
            int opponent=room.players.Length>2?room.reportOpponentSide:1-room.side;
            if(room.phase!="battle"&&opponent>=0&&opponent<room.players.Length&&opponent!=room.side&&
                (room.roundWinner==room.side||room.roundWinner==opponent))
            {
                var fought=room.players[opponent];int winner=room.roundWinner==room.side?0:1;
                // Use the report's global participant, never the next round's pairing.

                matchFinisher=new TamerFinisherScene(Time.unscaledTime,winner,winner==0?me.finisher:fought.finisher,
                    opponent,me.tamer,fought.tamer,TamerPosition(room,room.side,false),TamerPosition(room,opponent,true),me.name,fought.name);
            }
        }
        if(room.phase=="battle")matchFinisher=null;
    }
    void DrawMatchTamers(Room room,Player me,Player enemy)
    {
        arena.SetField(me.field);
        int opponent=MatchFinisherActive?matchFinisher.OpponentSide:OpponentSide(room);
        DrawLiveTamer(room,room.side,false,"tamer:own:"+room.side,MatchFinisherActive?matchFinisher.OwnTamer:-1);
        DrawLiveTamer(room,opponent,true,"tamer:rival:"+opponent,MatchFinisherActive?matchFinisher.RivalTamer:-1);
        if(MatchFinisherActive)matchFinisher.DrawMovingEffect(arena,Time.unscaledTime,TamerPosition(room,room.side,false),TamerPosition(room,opponent,true));
    }
}
