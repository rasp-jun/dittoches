using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public sealed partial class MultiLauncher
{
    bool recoveringLogin;
    string recoveryKey="",recoveryName="";
    int connectionFailures,requestAttempt;
    float lastRoundTripMs;

    void PollConnection()
    {
        if(solo||busy||Time.unscaledTime<nextPoll)return;
        if(recoveringLogin)StartCoroutine(Request("/login",new Command{name=recoveryName,key=recoveryKey}));
        else if(token.Length>0)StartCoroutine(Request("/state",new Command()));
    }

    void ResetConnectionRecovery()
    {
        recoveringLogin=false;recoveryKey="";recoveryName="";connectionFailures=0;connectionError=false;
    }

    string ConnectionStatusText()
    {
        if(recoveringLogin)return "세션 복구 중 · 기존 계정으로 경기 상태를 확인합니다.";
        if(connectionError)return "연결 복구 중 · "+notice;
        if(busy)return (requestAttempt>0?"요청 결과 재확인 중":"서버 통신 중")+" · "+notice;
        return token.Length>0?Mathf.RoundToInt(lastRoundTripMs)+"ms · "+notice:notice;
    }

    string OpponentConnectionText(Player enemy)
    {
        if(connectionError||recoveringLogin)return "상대 연결 확인 중";
        if(enemy.connectionKnown&&!enemy.connected)
            return "재접속 대기 · "+Mathf.CeilToInt(Mathf.Max(0,enemy.reconnectRemaining-(Time.unscaledTime-receivedAt)))+"초";
        return "LV "+enemy.level+" · "+(enemy.ready?"준비 완료":"준비 중");
    }

    IEnumerator Request(string path,Command command)
    {
        if(busy)yield break;
        if(path=="/queue"){command.tamer=lobbyLoadout.tamer;command.field=lobbyLoadout.field;command.finisher=lobbyLoadout.finisher;}
        bool mutation=path!="/state"&&path!="/login";
        bool reliable=state!=null&&state.reliableCommands>=1;
        if(mutation&&reliable&&string.IsNullOrEmpty(command.requestId))
        {
            command.requestId=Guid.NewGuid().ToString("N");
            Room room=state.room;command.expectedRoom=room==null?"":room.id;
            command.expectedRound=room==null?0:room.round;command.expectedPhase=room==null?"":room.phase;
        }
        // Freeze both body and credentials for all attempts of this logical command.
        byte[] body=Encoding.UTF8.GetBytes(JsonUtility.ToJson(command));
        string requestToken=token,endpoint=server+path;
        bool accepted=false,rejected=false;
        busy=true;requestAttempt=0;
        try
        {
            int attempts=!mutation||reliable?3:1;
            for(int attempt=0;attempt<attempts;attempt++)
            {
                requestAttempt=attempt;
                if(attempt>0)yield return new WaitForSecondsRealtime(.35f*attempt);
                using(var request=new UnityWebRequest(endpoint,"POST"))
                {
                    request.uploadHandler=new UploadHandlerRaw(body);request.downloadHandler=new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type","application/json");
                    if(requestToken.Length>0)request.SetRequestHeader("Authorization","Bearer "+requestToken);
                    request.timeout=5;float started=Time.realtimeSinceStartup;
                    yield return request.SendWebRequest();
                    State response=null;
                    try{response=DecodeState(request.downloadHandler.text);}catch(Exception){}
                    bool businessError=request.responseCode>=400&&request.responseCode<500&&response!=null&&!string.IsNullOrEmpty(response.error);
                    if(businessError)
                    {
                        rejected=true;notice=response.error;
                        if(response.error.Contains("인증이 만료"))
                        {
                            token="";recoveringLogin=recoveryKey.Length==64;connectionError=recoveringLogin;
                            if(!recoveringLogin)state=null;
                        }
                        else connectionError=false;
                        break;
                    }
                    bool valid=request.result==UnityWebRequest.Result.Success&&response!=null&&string.IsNullOrEmpty(response.error)&&!string.IsNullOrEmpty(response.token);
                    if(valid&&mutation&&reliable)valid=response.acknowledgedRequestId==command.requestId;
                    if(!valid)continue;
                    bool recovered=connectionError||recoveringLogin||attempt>0;
                    ReceiveRecruitment(state==null?null:state.room,response.room,path,command);
                    ReconcileMatchSelection(state==null?null:state.room,response.room);
                    state=response;token=response.token;receivedAt=Time.unscaledTime;
                    lastRoundTripMs=(Time.realtimeSinceStartup-started)*1000;
                    if(path=="/login"){recoveryKey=command.key;recoveryName=command.name;}
                    recoveringLogin=false;connectionError=false;connectionFailures=0;
                    ReconcileEquipmentSelection();
                    if(recovered)notice="연결 복구 완료 · 최신 경기 상태를 반영했습니다.";
                    else if(path!="/state")notice=path=="/login"?"서버 접속 완료":"서버에 연결되었습니다.";
                    if(state.room==null||state.room.phase=="finished"){selectedSlot=-1;selectedArea="";}
                    if(path=="/leave")confirmLeave=false;
                    accepted=true;break;
                }
            }
            if(!accepted&&!rejected)
            {
                connectionError=true;connectionFailures++;
                if(path=="/login"&&command.key!=null&&command.key.Length==64)
                {recoveryKey=command.key;recoveryName=command.name;recoveringLogin=true;}
                notice=mutation?"요청 결과를 확인하지 못했습니다. 상태를 다시 받아 확인합니다.":"서버 응답을 기다리고 있습니다. 자동으로 다시 연결합니다.";
            }
        }
        finally
        {
            busy=false;requestAttempt=0;
            nextPoll=Time.unscaledTime+(rejected?.15f:accepted?1:Mathf.Min(5,Mathf.Max(1,connectionFailures)));
        }
    }

    void Send(string path,Command command=null)
    {
        if(!busy&&!connectionError&&!recoveringLogin)StartCoroutine(Request(path,command??new Command()));
    }
}
