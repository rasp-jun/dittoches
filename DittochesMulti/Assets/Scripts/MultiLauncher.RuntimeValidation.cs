#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public sealed partial class MultiLauncher
{
    int onlineChecks;State smokePeer;
    void OnlineRequire(bool condition,string message)
    {if(!condition){Application.Quit(2);throw new InvalidOperationException("ONLINE SMOKE FAILED: "+message);}onlineChecks++;}
    public void BeginOnlineSmoke(){StartCoroutine(OnlineSmoke());}
    IEnumerator ServerPause(double seconds)
    {
        // Server throttling uses wall time, independent of player simulation time.
        DateTime until=DateTime.UtcNow.AddSeconds(seconds);
        while(DateTime.UtcNow<until)yield return null;
    }
    IEnumerator SmokeRequest(string path,Command command=null)
    {
        yield return Request(path,command??new Command());
        OnlineRequire(!connectionError,"client request "+path+" "+notice);
        yield return ServerPause(.2);
    }
    IEnumerator PeerRequest(string path,Command command)
    {
        using(var request=new UnityWebRequest(server+path,"POST"))
        {
            request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(command)));
            request.downloadHandler=new DownloadHandlerBuffer();request.SetRequestHeader("Content-Type","application/json");request.timeout=5;
            if(smokePeer!=null)request.SetRequestHeader("Authorization","Bearer "+smokePeer.token);
            yield return request.SendWebRequest();
            OnlineRequire(request.result==UnityWebRequest.Result.Success,"second HTTP client "+path);
            smokePeer=DecodeState(request.downloadHandler.text);
            OnlineRequire(smokePeer!=null&&string.IsNullOrEmpty(smokePeer.error),"second client snapshot");
        }
        yield return ServerPause(.2);
    }
    IEnumerator AwaitEquipmentAction()
    {
        float deadline=Time.unscaledTime+8;
        while(busy&&Time.unscaledTime<deadline)yield return null;
        OnlineRequire(!busy&&!connectionError,"equipment request succeeded: "+notice);
        yield return ServerPause(.2);
    }
    void OnlineCapture(string name)
    {
        string folder=Path.Combine(Application.dataPath,"../OnlineCaptures");Directory.CreateDirectory(folder);
        var texture=ScreenCapture.CaptureScreenshotAsTexture();
        try{File.WriteAllBytes(Path.Combine(folder,name+".png"),texture.EncodeToPNG());}
        finally{Destroy(texture);}
        Debug.Log("ONLINE SCREEN "+name);
    }
    IEnumerator OnlineSmoke()
    {
        yield return null;
        string[] args=Environment.GetCommandLineArgs();int argument=Array.IndexOf(args,"--test-server");
        OnlineRequire(argument>=0&&argument+1<args.Length,"explicit isolated test server");
        server=args[argument+1];OnlineRequire(new Uri(server).IsLoopback,"test server must be local");artPack=0;
        OnlineRequire(DecodeState("{\"token\":\"test\",\"room\":null}").room==null,"JSON null room remains lobby");
        string key=Guid.NewGuid().ToString("N")+Guid.NewGuid().ToString("N");
        yield return SmokeRequest("/login",new Command{name="장비 테스트",key=key});
        OnlineRequire(state.room==null,"login without a match remains in lobby");
        yield return PeerRequest("/login",new Command{name="테스트 상대",key=Guid.NewGuid().ToString("N")+Guid.NewGuid().ToString("N")});
        yield return SmokeRequest("/queue",new Command{mode="normal"});
        yield return PeerRequest("/queue",new Command{mode="normal"});
        yield return SmokeRequest("/state");
        OnlineRequire(OnlineMe.inventory.SequenceEqual(new[]{0,1,2,3,14}),"initial supplies deserialize");
        OnlineRequire(state.room.players[1-state.room.side].inventory.Length==0,"opponent inventory hidden");
        yield return new WaitForEndOfFrame();OnlineCapture("01-inventory");
        onlineItemGuide=0;yield return new WaitForEndOfFrame();OnlineCapture("02-recipes");onlineItemGuide=-1;
        SelectOnlineItem(0);SelectOnlineItem(1);yield return AwaitEquipmentAction();
        OnlineRequire(OnlineMe.inventory.SequenceEqual(new[]{2,3,14,5}),"UI combines two components");
        SelectOnlineItem(Array.IndexOf(OnlineMe.inventory,5));ClickSlot("board",3,At(OnlineMe.board,3));yield return AwaitEquipmentAction();
        OnlineRequire(At(OnlineMe.board,3).items.SequenceEqual(new[]{5}),"UI equips crafted item");
        SelectOnlineItem(Array.IndexOf(OnlineMe.inventory,3));ClickSlot("board",3,At(OnlineMe.board,3));yield return AwaitEquipmentAction();
        SelectOnlineItem(Array.IndexOf(OnlineMe.inventory,2));ClickSlot("board",3,At(OnlineMe.board,3));yield return AwaitEquipmentAction();
        OnlineRequire(At(OnlineMe.board,3).items.SequenceEqual(new[]{5,12}),"auto combination on full unit");
        yield return new WaitForEndOfFrame();OnlineCapture("03-equipped");
        onlineItemGuide=12;yield return new WaitForEndOfFrame();OnlineCapture("04-item-detail");onlineItemGuide=-1;
        var equippedStats=OnlineStats(At(OnlineMe.board,3),"board");
        OnlineRequire(equippedStats.abilityPower==145,"equipped AP appears in client stats");
        OpenOnlineSkill(At(OnlineMe.board,3),"board");yield return new WaitForEndOfFrame();OnlineCapture("07-skill-detail");onlineSkillId="";
        SelectOnlineItem(Array.IndexOf(OnlineMe.inventory,14));ClickSlot("board",3,At(OnlineMe.board,3));yield return AwaitEquipmentAction();
        OnlineRequire(OnlineMe!=null&&At(OnlineMe.board,3)!=null,"removal preserves unit slot");
        OnlineRequire(OnlineMe.inventory.SequenceEqual(new[]{5,12})&&At(OnlineMe.board,3).items.Length==0,"removal returns both items");
        foreach(int id in new[]{5,12})
        {SelectOnlineItem(Array.IndexOf(OnlineMe.inventory,id));ClickSlot("board",3,At(OnlineMe.board,3));yield return AwaitEquipmentAction();}
        yield return SmokeRequest("/login",new Command{name="장비 테스트",key=key});
        OnlineRequire(At(OnlineMe.board,3).items.SequenceEqual(new[]{5,12}),"reconnect retains equipped items");
        yield return SmokeRequest("/action",new Command{action="ready"});
        yield return PeerRequest("/action",new Command{action="ready"});
        yield return SmokeRequest("/state");
        OnlineRequire(state.room.phase=="battle","both clients enter battle");
        Fighter own=state.room.frames[0].units.First(f=>f.side==state.room.side);
        Fighter enemy=state.room.frames[0].units.First(f=>f.side!=state.room.side);
        OnlineRequire(Mathf.Abs(own.maxHp-enemy.maxHp-120)<.01f,"equipment health in actual server frames");
        OnlineRequire(Mathf.Abs(own.mana-enemy.mana-20)<.01f,"equipment start mana in server frames");
        yield return new WaitForEndOfFrame();OnlineCapture("05-combat");
        DateTime timeout=DateTime.UtcNow.AddSeconds(35);
        while(state.room.phase=="battle"&&DateTime.UtcNow<timeout)
        {yield return ServerPause(.7);yield return SmokeRequest("/state");}
        OnlineRequire(state.room.phase=="prepare"&&state.room.round==2,"battle settles to next round");
        OnlineRequire(OnlineMe.inventory.SequenceEqual(new[]{0}),"next round supply arrives once");
        selectedArea="board";selectedSlot=3;yield return new WaitForEndOfFrame();OnlineCapture("06-next-round");
        Debug.Log("ONLINE SMOKE COMPLETE: "+onlineChecks+" checks");Application.Quit();
    }
}
#endif
