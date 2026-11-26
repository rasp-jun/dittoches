using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public sealed partial class MultiLauncher
{
    [Serializable] public class TamerSample {public int side,sequence;public float x,y,worldX,worldZ;}
    [Serializable] public class TamerPacket {public string room,error;public int side;public double sampledAt;public TamerSample[] players;}
    OnlineTamerMotion[] onlineTamers=new OnlineTamerMotion[0];
    string tamerRoom="";
    int pendingTamerSequence,tamerIntentSequence;
    Vector2 pendingTamerCell;
    float nextTamerPoll,tamerIntentAt,nextTamerMoveAllowed;
    bool tamerSyncBusy;

    void EnsureTamerTimelines(Room room)
    {
        if(tamerRoom==room.id&&onlineTamers.Length==room.players.Length)return;
        tamerRoom=room.id;pendingTamerSequence=tamerIntentSequence=0;nextTamerPoll=0;
        onlineTamers=new OnlineTamerMotion[room.players.Length];
        for(int i=0;i<onlineTamers.Length;i++)onlineTamers[i]=new OnlineTamerMotion(TacticalArena.CellWorld(room.players[i].tamerX,room.players[i].tamerY),Time.unscaledTime);
    }
    void ReceiveTamers(TamerPacket packet,float transit=0)
    {
        var room=state==null?null:state.room;
        if(room==null||packet==null||packet.room!=room.id||packet.side!=room.side||packet.players==null||packet.players.Length!=room.players.Length)return;
        EnsureTamerTimelines(room);
        foreach(var p in packet.players)
        {
            if(p.side<0||p.side>=onlineTamers.Length||float.IsNaN(p.worldX)||float.IsNaN(p.worldZ)||float.IsInfinity(p.worldX)||float.IsInfinity(p.worldZ))continue;
            var track=onlineTamers[p.side];
            track.Receive(new Vector3(p.worldX,.19f,p.worldZ),TacticalArena.CellWorld(p.x,p.y),p.sequence,packet.sampledAt,Time.unscaledTime,transit,p.side==room.side?pendingTamerSequence:0);
            if(p.side==room.side&&p.sequence>=pendingTamerSequence){pendingTamerSequence=0;tamerIntentSequence=Math.Max(tamerIntentSequence,p.sequence);}
        }
    }
    void ReceiveLegacyTamers()
    {
        var room=state==null?null:state.room;if(room==null)return;
        EnsureTamerTimelines(room);
        for(int i=0;i<onlineTamers.Length;i++)
        {
            Vector3 target=TacticalArena.CellWorld(room.players[i].tamerX,room.players[i].tamerY);
            if(target!=onlineTamers[i].Target)onlineTamers[i].Predict(target,Time.unscaledTime);
        }
    }
    void TickTamerSync()
    {
        var room=state==null?null:state.room;
        if(solo||room==null){tamerRoom="";pendingTamerSequence=0;return;}
        EnsureTamerTimelines(room);
        if(pendingTamerSequence>0&&(Time.unscaledTime-tamerIntentAt>3||room.phase=="finished"||OnlineMe.eliminated))
        {pendingTamerSequence=0;onlineTamers[room.side].CancelPrediction();}
        foreach(var track in onlineTamers)track.Step(Time.unscaledTime);
        if(state.tamerSyncVersion<1||token.Length==0||recoveringLogin||connectionError||tamerSyncBusy||Time.unscaledTime<Mathf.Max(nextTamerPoll,nextTamerMoveAllowed))return;
        StartCoroutine(SyncTamers());
    }
    IEnumerator SyncTamers()
    {
        string room=tamerRoom,credential=token;
        var command=new Command{expectedRoom=room,tamerMove=pendingTamerSequence>0,tamerSequence=pendingTamerSequence,x=pendingTamerCell.x,y=pendingTamerCell.y};
        tamerSyncBusy=true;nextTamerMoveAllowed=Time.unscaledTime+.12f;
        bool success=false;
        try
        {
            using(var request=new UnityWebRequest(server+"/tamer-sync","POST"))
            {
                request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(command)));
                request.downloadHandler=new DownloadHandlerBuffer();request.SetRequestHeader("Content-Type","application/json");
                request.SetRequestHeader("Authorization","Bearer "+credential);request.timeout=2;
                float started=Time.unscaledTime;
                yield return request.SendWebRequest();
                if(room!=tamerRoom||credential!=token)yield break;
                TamerPacket packet=null;try{packet=JsonUtility.FromJson<TamerPacket>(request.downloadHandler.text);}catch(Exception){}
                success=request.result==UnityWebRequest.Result.Success&&packet!=null&&packet.room==room&&packet.players!=null;
                if(success)ReceiveTamers(packet,(Time.unscaledTime-started)*.5f);
                else if(request.responseCode>=400&&request.responseCode<500)nextPoll=0;
            }
        }
        finally{tamerSyncBusy=false;nextTamerPoll=Time.unscaledTime+(success?.12f:.4f);}
    }
    Vector3 TamerPosition(Room room,int side,bool rival)
    {EnsureTamerTimelines(room);return OnlineTamerMotion.View(onlineTamers[side].Position,rival);}
    void DrawLiveTamer(Room room,int side,bool rival,object key,int appearance=-1)
    {
        EnsureTamerTimelines(room);var track=onlineTamers[side];
        arena.SetTamer(key,appearance<0?room.players[side].tamer:appearance,OnlineTamerMotion.View(track.Position,rival),
            OnlineTamerMotion.View(track.Target,rival),Mathf.Clamp01(track.Velocity.magnitude),Time.unscaledTime);
    }
    void MoveOnlineTamer(int cell)
    {
        var room=state==null?null:state.room;
        if(cell<28||cell>=56||room==null||OnlineScouting||OnlineMe.eliminated||room.phase=="finished"||!OnlineConnectionFresh())return;
        if(state.tamerSyncVersion<1){if(!busy)Send("/tamer-move",new Command{x=cell%7,y=cell/7});return;}
        EnsureTamerTimelines(room);
        pendingTamerSequence=++tamerIntentSequence;pendingTamerCell=new Vector2(cell%7,cell/7);tamerIntentAt=Time.unscaledTime;
        onlineTamers[room.side].Predict(TacticalArena.CellWorld(pendingTamerCell.x,pendingTamerCell.y),Time.unscaledTime);
        // Coalesce rapid clicks; at most one movement request can be in flight.
        nextTamerPoll=Mathf.Min(nextTamerPoll,Time.unscaledTime+.02f);
    }
}
