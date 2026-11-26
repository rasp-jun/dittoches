#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public sealed partial class MultiLauncher
{
    TamerPacket peerTamerPacket;
    IEnumerator PeerTamerMove(State peer,int sequence,int x,int y)
    {
        using(var request=new UnityWebRequest(server+"/tamer-sync","POST"))
        {
            var command=new Command{expectedRoom=state.room.id,tamerMove=true,tamerSequence=sequence,x=x,y=y};
            request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(command)));request.downloadHandler=new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type","application/json");request.SetRequestHeader("Authorization","Bearer "+peer.token);request.timeout=3;
            yield return request.SendWebRequest();
            OnlineRequire(request.result==UnityWebRequest.Result.Success,"peer movement stream accepts independent participant");
            peerTamerPacket=JsonUtility.FromJson<TamerPacket>(request.downloadHandler.text);
            OnlineRequire(peerTamerPacket!=null&&peerTamerPacket.room==state.room.id&&peerTamerPacket.players.Length==8,"public movement packet includes exactly this room's eight tamers");
        }
    }
    IEnumerator ValidateLiveTamerSync(State[] peers)
    {
        ValidateTamerTimeline();
        OnlineRequire(state.tamerSyncVersion==1,"server advertises lightweight movement stream");
        var room=state.room;int own=room.side;
        for(int i=6;i>=0;i--)yield return PeerTamerMove(peers[i],1,i%7,4);
        float until=Time.unscaledTime+3;
        while(onlineTamers[0].Sequence<1&&Time.unscaledTime<until)yield return null;
        for(int i=0;i<7;i++)OnlineRequire(onlineTamers[i].Sequence==1&&onlineTamers[i].Target==TacticalArena.CellWorld(i%7,4),"global participant keeps independent destination "+i);
        Vector3 rivalStart=onlineTamers[0].Position;
        SetScoutedPlayer(0);yield return new WaitForSeconds(.25f);yield return new WaitForEndOfFrame();OnlineCapture("moving-scout");
        OnlineRequire((onlineTamers[0].Position-rivalStart).sqrMagnitude>.001f,"scouted rival continues moving instead of jumping to a destination");
        var same=onlineTamers[0];SetScoutedPlayer(6);yield return null;SetScoutedPlayer(-1);
        OnlineRequire(ReferenceEquals(same,onlineTamers[0]),"scouting switches preserve each global participant timeline");
        Vector3 ownStart=onlineTamers[own].Position;
        busy=true;
        try
        {
            MoveOnlineTamer(28);MoveOnlineTamer(55);
            var click=arena.Project(TacticalArena.CellWorld(5,6));yield return OnlineClick(click.x,click.y,1);
            yield return new WaitForSeconds(.3f);
            OnlineRequire((onlineTamers[own].Position-ownStart).sqrMagnitude>.001f,"own tamer responds while the economy request lane is busy");
            until=Time.unscaledTime+3;
            while(pendingTamerSequence>0&&Time.unscaledTime<until)yield return null;
            OnlineRequire(pendingTamerSequence==0&&onlineTamers[own].Target==TacticalArena.CellWorld(5,6),"rapid destinations coalesce to the final actual right click without dropping it");
        }
        finally{busy=false;}
        yield return PeerTamerMove(peers[0],2,6,7);
        OnlineRequire(peerTamerPacket.players[own].x==5&&peerTamerPacket.players[own].y==6,"other participant observes own accepted movement");
        yield return new WaitForSeconds(.3f);yield return new WaitForEndOfFrame();OnlineCapture("moving-both-tamers");
        OnlineRequire(onlineTamers[0].Sequence==2,"remote direction change arrives without a full game state request");
    }
    void ValidateTamerTimeline()
    {
        for(float y=4;y<=7;y+=.25f)for(float x=0;x<=6;x+=.5f)
            OnlineRequire((OnlineTamerMotion.View(TacticalArena.CellWorld(x,y),true)-TacticalArena.CellWorld(6-x,7-y)).sqrMagnitude<.00001f,"both sides mirror the same physical tamer location including staggered rows");
        Vector3 origin=TacticalArena.CellWorld(3,7),target=TacticalArena.CellWorld(0,4);
        var track=new OnlineTamerMotion(origin,0);track.Receive(origin,target,1,1000,0,0,0);
        for(int i=1;i<=30;i++)track.Step(i/60f);
        OnlineRequire(Vector3.Distance(track.Position,origin)>1.5f&&Vector3.Distance(track.Position,origin)<2.1f,"authoritative movement uses elapsed time and a bounded world speed");
        Vector3 priorTarget=track.Target;
        track.Receive(origin,origin,0,999,.5f,0,0);
        OnlineRequire(track.Sequence==1&&track.Target==priorTarget,"late full-state response cannot rewind a newer movement packet");
        track.Predict(origin,.5f);track.Receive(origin,target,1,1001,.5f,0,2);
        OnlineRequire(track.Target==origin,"older acknowledgement cannot overwrite a newer local click");
        track.CancelPrediction();OnlineRequire(track.Target==target,"unconfirmed input expires back to authoritative destination");
        track.Step(4);for(int i=1;i<=100;i++)track.Step(4+i/60f);
        OnlineRequire(Vector3.Distance(track.Position,target)<.01f,"network pause recovers and stops without extrapolating beyond the destination");
    }
}
#endif
