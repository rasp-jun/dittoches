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
    int onlineClickStep,onlineClickButton;Vector2 onlineClickPoint;
    KeyCode onlineTestKey;
    bool onlineDragValidation,onlineDragCancel;
    Vector2 onlineDragEnd;
    Event OnlineValidationEvent()
    {
        if(onlineClickStep==0||Event.current.type!=EventType.Repaint)return null;
        Event saved=new Event(Event.current);
        if(onlineTestKey!=KeyCode.None){Event.current=new Event{type=EventType.KeyDown,keyCode=onlineTestKey,mousePosition=onlineClickPoint};onlineTestKey=KeyCode.None;onlineClickStep=0;return saved;}
        if(onlineDragValidation)
        {
            if(onlineClickStep==3)return null;
            Event.current=new Event{type=onlineClickStep==1?EventType.MouseDown:onlineClickStep==2?EventType.MouseDrag:onlineDragCancel?EventType.KeyDown:EventType.MouseUp,
                keyCode=onlineDragCancel?KeyCode.Escape:KeyCode.None,button=0,mousePosition=onlineClickStep==1?onlineClickPoint:onlineDragEnd};
            onlineClickStep=onlineClickStep==1?2:onlineClickStep==2?3:0;return saved;
        }
        Event.current=new Event{type=onlineClickStep==1?EventType.MouseDown:EventType.MouseUp,button=onlineClickButton,mousePosition=onlineClickPoint};
        onlineClickStep=onlineClickStep==1?2:0;return saved;
    }
    IEnumerator OnlineClick(float x,float y,int mouseButton=0)
    {
        onlineClickButton=mouseButton;onlineClickPoint=new Vector2(x,y);onlineClickStep=1;float deadline=Time.unscaledTime+3;
        while(onlineClickStep>0&&Time.unscaledTime<deadline)yield return null;
        OnlineRequire(onlineClickStep==0,"online UI received both click phases");yield return null;
    }
    IEnumerator OnlineKey(KeyCode key,Vector2? point=null)
    {
        onlineTestKey=key;onlineClickPoint=point??Vector2.zero;onlineClickStep=1;float deadline=Time.unscaledTime+3;
        while(onlineClickStep>0&&Time.unscaledTime<deadline)yield return null;
        OnlineRequire(onlineClickStep==0,"online UI received keyboard input "+key);yield return null;
    }
    IEnumerator ValidateOnlineScouting()
    {
        int peer=1-state.room.side;var own=OnlineMe;var ownBag=own.inventory.ToArray();int bank=own.gold;
        yield return PeerRequest("/action",new Command{action="buy",slot=0});
        var theirs=smokePeer.room.players[smokePeer.room.side];var bought=theirs.bench.First();
        yield return PeerRequest("/action",new Command{action="move",area="bench",slot=bought.slot,targetArea="bench",targetSlot=8});
        yield return PeerRequest("/action",new Command{action="equip",area="bench",slot=8,itemSlot=0,inventoryRevision=theirs.inventoryRevision});
        yield return SmokeRequest("/state");
        OnlineRequire(state.room.players[peer].publicBench&&At(state.room.players[peer].bench,8).items.SequenceEqual(new[]{0}),"opponent bench and equipment arrive from real HTTP state");
        OnlineRequire(state.room.players[peer].shop.Length==0&&state.room.players[peer].inventory.Length==0,"scouting does not reveal private shop or item storage");
        SelectOnlineItem(0);yield return OnlineClick(310+peer*225+105,111);
        OnlineRequire(OnlineScouting&&scoutSide==peer&&onlineItem<0&&selectedSlot<0,"player navigation opens rival field and clears stale equip selection");
        yield return new WaitForEndOfFrame();OnlineCapture("scout-opponent-field");
        ValidateSharedOnlineInspectionStats();
        var unit=At(ScoutedPlayer.bench,8);Vector2 pointer=OnlinePieceRect(unit.id,unit.star,TacticalArena.BenchWorld(8)).center;
        yield return OnlineClick(pointer.x,pointer.y,1);
        OnlineRequire(ScoutedUnit!=null&&scoutSlot==8&&scoutArea=="bench"&&selectedSlot<0,"right click inspects actual opponent bench without own selection");
        yield return new WaitForEndOfFrame();OnlineCapture("scout-opponent-bench");
        yield return OnlineClick(1340,669,1);
        OnlineRequire(onlineItemGuide==0,"opponent equipped item opens recipe popup");yield return OnlineKey(KeyCode.Escape);
        foreach(KeyCode key in new[]{KeyCode.E,KeyCode.W})yield return OnlineKey(key);
        OnlineRequire(!busy&&OnlineMe.gold==bank&&OnlineMe.inventory.SequenceEqual(ownBag),"scout sell/deploy keys cannot mutate own or rival ownership");
        yield return PeerRequest("/action",new Command{action="move",area="bench",slot=8,targetArea="bench",targetSlot=7});
        yield return SmokeRequest("/state");yield return null;
        OnlineRequire(ScoutedUnit==null&&At(ScoutedPlayer.bench,7)!=null,"opponent reposition updates live and clears stale inspection");
        yield return OnlineKey(KeyCode.Space);
        OnlineRequire(!OnlineScouting&&!OnlineMe.ready&&!busy,"Space returns home without submitting ready");
        yield return OnlineKey(KeyCode.R);OnlineRequire(OnlineScouting,"R cycles to opponent");
        yield return OnlineKey(KeyCode.Q);OnlineRequire(!OnlineScouting,"Q cycles back to own field");
        ClearOnlineUnitSelection();var ownUnit=At(OnlineMe.board,3);
        yield return OnlineKey(KeyCode.W,OnlinePieceRect(ownUnit.id,ownUnit.star,TacticalArena.CellWorld(3,4)).center);yield return AwaitEquipmentAction();
        OnlineRequire(At(OnlineMe.board,3)==null&&OnlineMe.bench.Any(u=>u.id=="koromon"),"online W moves own board unit to bench through server");
        var moved=OnlineMe.bench.First(u=>u.id=="koromon");selectedArea="bench";selectedSlot=moved.slot;
        var emptyPoint=OnlinePieceRect(ownUnit.id,ownUnit.star,TacticalArena.CellWorld(3,4)).center;
        foreach(KeyCode key in new[]{KeyCode.E,KeyCode.W})yield return OnlineKey(key,emptyPoint);
        OnlineRequire(!busy&&At(OnlineMe.bench,moved.slot)!=null&&OnlineMe.gold==bank,"empty field W/E cannot act on previously selected bench unit");
        yield return OnlineKey(KeyCode.W);yield return AwaitEquipmentAction();
        OnlineRequire(At(OnlineMe.board,0)!=null,"online W deploys bench unit to vacant board slot");
        var move=new Command{action="move",area="board",slot=0,targetArea="board",targetSlot=3};
        string signature=UnitCommandSignature(At(OnlineMe.board,0));
        yield return SmokeRequest("/action",move);
        OnlineRequire(move.expectedUnit==signature&&move.expectedTarget=="-","HTTP command captures source gear/star and empty destination before sending");
        var stale=new Command{action="sell",area="board",slot=3,expectedUnit="koromon:3:27"};
        yield return Request("/action",stale);
        OnlineRequire(!busy&&!connectionError&&notice.Contains("변경"),"stale unit command is rejected as actionable feedback without connection failure");
        yield return new WaitForEndOfFrame();OnlineCapture("stale-unit-rejected");
        yield return SmokeRequest("/state");
        OnlineRequire(UnitCommandSignature(At(OnlineMe.board,3))==signature&&OnlineMe.gold==bank,"server rejection preserves unit equipment and gold after fresh poll");
        ClearOnlineUnitSelection();
    }
    IEnumerator OnlineSaleDrag(int slot,bool cancel)
    {
        onlineDragValidation=true;onlineDragCancel=cancel;onlineClickPoint=arena.Project(TacticalArena.BenchWorld(slot));
        onlineDragEnd=OnlineSellZone.center;onlineClickStep=1;float deadline=Time.unscaledTime+4;
        while(onlineClickStep!=3&&Time.unscaledTime<deadline)yield return null;
        OnlineRequire(onlineClickStep==3&&OnlineSaleVisible,"drag replaces entire recruit shop with sale target");
        yield return new WaitForEndOfFrame();OnlineCapture(cancel?"combat-sale-cancel":"combat-shop-sale");
        onlineClickStep=4;
        while(onlineClickStep>0&&Time.unscaledTime<deadline)yield return null;
        onlineDragValidation=false;OnlineRequire(onlineClickStep==0&&!OnlineSaleVisible,"sale release or cancel clears drag state");
        yield return AwaitEquipmentAction();
    }
    IEnumerator OnlineGearDrag(int slot,Vector2 target,bool cancel)
    {
        onlineInventoryView.Sync(OnlineMe.inventory,ref onlineItemPage);
        if(onlineInventoryView.PageOf(slot)<0)onlineInventoryView.SetFilter(0,ref onlineItemPage);
        onlineItemPage=onlineInventoryView.PageOf(slot);int visualSlot=Enumerable.Range(0,12).First(i=>onlineInventoryView.Index(onlineItemPage,i)==slot);
        onlineDragValidation=true;onlineDragCancel=cancel;
        onlineClickPoint=OnlineInventoryRect(visualSlot).center;onlineDragEnd=target;onlineClickStep=1;float deadline=Time.unscaledTime+4;
        while(onlineClickStep!=3&&Time.unscaledTime<deadline)yield return null;
        OnlineRequire(onlineClickStep==3&&onlineEquipmentDrag.Dragging&&!OnlineSaleVisible,"gear drag captures inventory without unit sale");
        onlineValidationPointer=target;
        yield return new WaitForEndOfFrame();OnlineCapture(cancel?"gear-drag-cancel":arena.HitBench(target)>=0?"gear-drag-bench":TacticalArena.MultiViewport.Contains(target)?"gear-drag-combat":"gear-drag-outside");
        onlineClickStep=4;
        while(onlineClickStep>0&&Time.unscaledTime<deadline)yield return null;
        onlineValidationPointer=null;onlineDragValidation=false;
        OnlineRequire(onlineClickStep==0&&!onlineEquipmentDrag.Dragging&&onlineItem<0,"gear release or cancel clears selected source");
        yield return AwaitEquipmentAction();
    }
    IEnumerator ValidateCombatManagementOnline()
    {
        yield return SmokeRequest("/leave");yield return PeerRequest("/leave",new Command());
        yield return SmokeRequest("/queue",new Command{mode="normal"});yield return PeerRequest("/queue",new Command{mode="normal"});yield return SmokeRequest("/state");
        OnlineRequire(state.combatActions==1,"server advertises combat management");
        yield return SmokeRequest("/action",new Command{action="ready"});yield return PeerRequest("/action",new Command{action="ready"});yield return SmokeRequest("/state");
        OnlineRequire(state.room.phase=="battle"&&OnlineManagementAllowed(state.room,OnlineMe),"combat management available even with ready flags");
        float max=state.room.frames[0].units.First(f=>f.side==state.room.side).maxHp;
        yield return ValidateGuideCrafting();
        SelectOnlineItem(Array.IndexOf(OnlineMe.inventory,5));ReconcileEquipmentSelection();
        OnlineRequire(onlineItem>=0,"selected equipment survives combat UI reconciliation");
        var row=onlineCombatLabels.First(r=>((Fighter)r.key).side==state.room.side&&((Fighter)r.key).slot==3);
        yield return OnlineGearDrag(onlineItem,row.rect.center,false);
        OnlineRequire(At(OnlineMe.board,3).items.SequenceEqual(new[]{5}),"combat UI equips item on persistent board");
        OnlineRequire(state.room.frames.Any(frame=>frame.units.Any(f=>f.side==state.room.side&&f.maxHp>max)),"live equipment updates authoritative replay health");
        ValidateReplayLookup(state.room);
        ValidateSharedOnlineInspectionStats();
        selectedArea="board";selectedSlot=3;OnlineRequire(!CanSellOnline(state.room),"combat board selection cannot be sold");ClearOnlineUnitSelection();
        int offer=Array.FindIndex(OnlineMe.shop,id=>!string.IsNullOrEmpty(id));
        yield return SmokeRequest("/action",new Command{action="buy",slot=offer});
        int seat=OnlineMe.bench[0].slot;int value=Def(OnlineMe.bench[0].id).cost;
        int gear=Array.IndexOf(OnlineMe.inventory,2);var bag=OnlineMe.inventory.ToArray();int revision=OnlineMe.inventoryRevision;
        yield return OnlineGearDrag(gear,arena.Project(TacticalArena.BenchWorld(seat)),true);
        OnlineRequire(OnlineMe.inventory.SequenceEqual(bag)&&OnlineMe.inventoryRevision==revision&&At(OnlineMe.bench,seat).items.Length==0,"gear cancel preserves authoritative inventory and unit");
        yield return OnlineGearDrag(gear,OnlineSellZone.center,false);
        OnlineRequire(OnlineMe.inventory.SequenceEqual(bag)&&OnlineMe.inventoryRevision==revision,"gear outside drop cannot purchase or consume");
        yield return OnlineGearDrag(gear,arena.Project(TacticalArena.BenchWorld(seat)),false);
        OnlineRequire(At(OnlineMe.bench,seat).items.SequenceEqual(new[]{2}),"combat bench gear drag equips exactly once");
        int bank=OnlineMe.gold;
        yield return OnlineSaleDrag(seat,true);
        OnlineRequire(At(OnlineMe.bench,seat)!=null&&OnlineMe.gold==bank,"ESC sale cancellation preserves unit gold and gear");
        yield return OnlineSaleDrag(seat,false);
        OnlineRequire(At(OnlineMe.bench,seat)==null&&OnlineMe.gold==bank+value&&OnlineMe.inventory.Contains(2),"shop drop sells combat bench unit once and returns gear");
        OnlineRequire(state.room.phase=="battle","shop interactions completed during actual combat");
        bank=OnlineMe.gold;yield return OnlineClick(140,460);yield return AwaitEquipmentAction();
        OnlineRequire(OnlineMe.gold==bank-2,"combat reroll button works");
        bank=OnlineMe.gold;yield return OnlineClick(140,410);yield return AwaitEquipmentAction();
        OnlineRequire(OnlineMe.gold==bank-4,"combat XP button works");
        yield return new WaitForEndOfFrame();OnlineCapture("combat-management");
    }

    Vector2? onlineValidationPointer;
    bool onlineRecruitInputPending;
    void ValidateOnlineRecruitInputInGUI()
    {
        if(!onlineRecruitInputPending||Event.current.type!=EventType.Repaint)return;
        onlineRecruitInputPending=false;var me=OnlineMe;string offer=me.shop[0];int savedGold=me.gold,count=me.bench.Length;
        Event saved=new Event(Event.current);bool enabled=GUI.enabled;
        try
        {
            me.gold=0;GUI.enabled=true;
            Event.current=new Event{type=EventType.MouseDown,button=1,mousePosition=new Vector2(310+160,814+87)};
            DrawOnlineRecruitCard(new Rect(310,814,181,108),offer,0,me,true);
            OnlineRequire(onlineSkillId==offer&&onlineSkillArea=="shop","online shop icon opens while purchase is unavailable");
            OnlineRequire(!busy&&me.gold==0&&me.shop[0]==offer&&me.bench.Length==count,"shop inspection never sends a purchase");
            onlineSkillId="";
            Event.current=new Event{type=EventType.MouseDown,button=1,mousePosition=new Vector2(330,834)};
            DrawOnlineRecruitCard(new Rect(310,814,181,108),offer,0,me,true);
            OnlineRequire(onlineSkillId==offer&&Event.current.type==EventType.Used&&!busy,"whole online card inspection consumes input without buying");
            foreach(KeyCode key in new[]{KeyCode.D,KeyCode.F,KeyCode.Space,KeyCode.Return,KeyCode.Q,KeyCode.R,KeyCode.W,KeyCode.E})
            {
                me.gold=50;GUI.enabled=false;Event.current=new Event{type=EventType.KeyDown,keyCode=key};
                DrawOnlineEconomy(me,state.room,true,true);
                OnlineRequire(!busy&&me.gold==50&&me.shop[0]==offer,"modal blocks online economy shortcut "+key);
            }
            me.gold=0;GUI.enabled=true;Event.current=new Event{type=EventType.KeyDown,keyCode=KeyCode.D};
            DrawOnlineEconomy(me,state.room,true,true);
            OnlineRequire(!busy&&me.gold==0&&me.shop[0]==offer,"insufficient gold blocks reroll shortcut");
            OpenTeamPlan();me.gold=50;
            foreach(KeyCode key in new[]{KeyCode.D,KeyCode.F,KeyCode.Space,KeyCode.Return,KeyCode.Q,KeyCode.R,KeyCode.W,KeyCode.E})
            {
                Event.current=new Event{type=EventType.KeyDown,keyCode=key};DrawMatch();
                OnlineRequire(!busy&&me.gold==50&&me.shop[0]==offer,"team planner blocks online action "+key);
            }
            Event.current=new Event{type=EventType.KeyDown,keyCode=KeyCode.Escape};DrawTeamPlan();
            OnlineRequire(!showTeamPlan&&Event.current.type==EventType.Used,"team planner closes online with Escape");
            OpenOnlineTrait("harmonizer");
            foreach(KeyCode key in new[]{KeyCode.D,KeyCode.F,KeyCode.Space,KeyCode.Return,KeyCode.Q,KeyCode.R,KeyCode.W,KeyCode.E})
            {
                Event.current=new Event{type=EventType.KeyDown,keyCode=key};DrawMatch();
                OnlineRequire(!busy&&me.gold==50&&me.shop[0]==offer,"trait guide blocks online action "+key);
            }
            GUI.enabled=true;Event.current=new Event{type=EventType.KeyDown,keyCode=KeyCode.Escape};DrawOnlineTraitGuide();
            OnlineRequire(!showTraitGuide&&Event.current.type==EventType.Used,"trait guide closes online with Escape");
            ValidateOnlineEquipmentTargetsInGUI();
            ValidateOnlineFormationDragInGUI();
        }
        finally{me.gold=savedGold;Event.current=saved;GUI.enabled=enabled;}
    }
    void OnlineRequire(bool condition,string message)
    {if(!condition){Application.Quit(2);throw new InvalidOperationException("ONLINE SMOKE FAILED: "+message);}onlineChecks++;}
    public void BeginOnlineSmoke(){StartCoroutine(OnlineSmoke());}
    void ValidateOnlineEquipmentTargetsInGUI()
    {
        var room=state.room;int oldSide=room.side;string oldPhase=room.phase;
        var boards=room.players.Select(p=>p.board).ToArray();var benches=room.players.Select(p=>p.bench).ToArray();
        try
        {
            room.phase="prepare";
            foreach(int side in new[]{0,1})
            {
                room.side=side;var own=new Unit{id="agumon",star=1,slot=17,items=new int[0]};var bench=new Unit{id="koromon",star=1,slot=0,items=new int[0]};
                var enemy=new Unit{id="greymon",star=1,slot=3,items=new int[0]};
                room.players[side].board=new[]{own};room.players[side].bench=new[]{bench};room.players[1-side].board=new[]{enemy};
                foreach(bool onBench in new[]{false,true})
                {
                    var u=onBench?bench:own;var r=OnlinePieceRect(u.id,u.star,onBench?TacticalArena.BenchWorld(u.slot):TacticalArena.CellWorld(u.slot%7,u.slot/7+4));
                    Event.current=new Event{mousePosition=new Vector2(r.center.x,r.y+r.height*.3f)};
                    string area,blocked;int slot;Fighter live;var selected=OnlineEquipmentTarget(out area,out slot,out live,out blocked);
                    OnlineRequire(selected==u&&slot==u.slot&&area==(onBench?"bench":"board")&&onlineEquipmentTargetRect.width>0,"model body selects exact equipment owner on side "+side);
                }
                var er=OnlinePieceRect(enemy.id,enemy.star,TacticalArena.CellWorld(3,3));Event.current=new Event{mousePosition=er.center};
                string targetArea,denied;int targetSlot;Fighter fighter;
                OnlineRequire(OnlineEquipmentTarget(out targetArea,out targetSlot,out fighter,out denied)==null&&denied.Length>0,"enemy preparation model rejects gear on side "+side);
            }
        }
        finally{room.side=oldSide;room.phase=oldPhase;for(int i=0;i<room.players.Length;i++){room.players[i].board=boards[i];room.players[i].bench=benches[i];}}
    }
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
    void ValidateSignalReplayRendering()
    {
        string folder=Path.Combine(Application.dataPath,"../CombatSignalCaptures"),file=Path.Combine(folder,"fixture.json");
        if(!File.Exists(file)){Debug.Log("SIGNAL REPLAY: no optional fixture; run create_combat_signal_fixture.py to render both sides.");return;}
        Room replay=JsonUtility.FromJson<Room>(File.ReadAllText(file));var previousArena=arena;
        string savedArea=selectedArea;int savedSlot=selectedSlot;
        try
        {
            using(var review=new TacticalArena(new Rect(0,0,960,600),true))
            {
                arena=review;
                foreach(int side in new[]{0,1})foreach(int kind in new[]{1,2})
                {
                    replay.side=side;
                    var frame=replay.frames.First(f=>f.units.Any(u=>OnlineCombatSignal(u,f.time).kind==kind));
                    float remaining=replay.battleDuration-frame.time,progress=CombatProgress(replay,remaining);
                    int index=Mathf.FloorToInt(progress),next=Mathf.Min(index+1,replay.frames.Length-1);
                    arena.BeginFrame(-1,-1,-1,false);
                    foreach(var f in replay.frames[index].units)RenderOnlineFighter(replay,f,index,next,progress,remaining);
                    int count=DrawOnlineCombatSignals(replay,remaining);arena.Render();
                    OnlineRequire(count>0&&arena.EffectCount>0&&arena.EffectCount<=count*7,"production replay signals render with bounded effects on side "+side+" kind "+kind);
                    RenderTexture old=RenderTexture.active;RenderTexture.active=arena.Texture;
                    var picture=new Texture2D(arena.Texture.width,arena.Texture.height,TextureFormat.RGB24,false);
                    picture.ReadPixels(new Rect(0,0,picture.width,picture.height),0,0);picture.Apply();
                    File.WriteAllBytes(Path.Combine(folder,"replay-side-"+side+"-kind-"+kind+".png"),picture.EncodeToPNG());Destroy(picture);RenderTexture.active=old;
                }
                var deathFrame=replay.frames.First(f=>f.units.Any(u=>u.hp<=0));var fallen=deathFrame.units.First(u=>u.hp<=0);
                replay.side=fallen.side;selectedArea="board";selectedSlot=fallen.slot;
                float deathRemaining=replay.battleDuration-deathFrame.time,deathProgress=CombatProgress(replay,deathRemaining);
                int deathIndex=Mathf.FloorToInt(deathProgress),deathNext=Mathf.Min(deathIndex+1,replay.frames.Length-1);
                arena.BeginFrame(OnlinePreparationCell(replay),-1,-1,false);
                foreach(var f in replay.frames[deathIndex].units)RenderOnlineFighter(replay,f,deathIndex,deathNext,deathProgress,deathRemaining);
                arena.Render();
                for(int tile=0;tile<56;tile++)
                {
                    Color color=arena.ValidationTileColor(tile);
                    OnlineRequire(color!=new Color(.12f,.75f,1f)&&color!=new Color(1,.76f,.24f),"fallen selected unit leaves no range or preparation tile "+tile);
                }
                RenderTexture savedTexture=RenderTexture.active;RenderTexture.active=arena.Texture;
                var deathPicture=new Texture2D(arena.Texture.width,arena.Texture.height,TextureFormat.RGB24,false);
                deathPicture.ReadPixels(new Rect(0,0,deathPicture.width,deathPicture.height),0,0);deathPicture.Apply();
                File.WriteAllBytes(Path.Combine(folder,"dead-selection.png"),deathPicture.EncodeToPNG());Destroy(deathPicture);RenderTexture.active=savedTexture;
                arena.BeginFrame(-1,-1,-1,false);
                OnlineRequire(DrawOnlineCombatSignals(replay,replay.battleDuration)==0,"new replay cannot reuse previously shown signals");arena.Render();
                OnlineRequire(arena.EffectCount==0,"pooled replay effects are cleared");
            }
        }
        finally{arena=previousArena;selectedArea=savedArea;selectedSlot=savedSlot;}
    }
    [Serializable] sealed class RecruitFixture { public RecruitCase[] rows; }
    [Serializable] sealed class RecruitCase { public string name,area;public Room before,after;public int star,slot,returnedItems; }
    float? onlineMergeArrivalReviewAge;
    IEnumerator ValidateRecruitmentFeedback()
    {
        string file=Path.Combine(Application.dataPath,"../RecruitmentCaptures/fixture.json");
        if(!File.Exists(file)){Debug.Log("RECRUITMENT: optional fixture absent; run create_recruitment_fixture.py for merge feedback cases.");yield break;}
        var fixture=JsonUtility.FromJson<RecruitFixture>(File.ReadAllText(file));var command=new Command{action="buy",slot=0};
        State savedState=state;var savedReceipt=recruitReceipt;float savedReceived=receivedAt;var savedArrivals=onlineMergeArrivals.ToArray();var soundSettings=new StarSoundValidation.Preferences();
        StarUpgradeAudio.Configure(35,false);StarUpgradeAudio.Cancel();
        OnlineRequire(OnlineRecruitHover(null,null,Vector2.zero,true)==null&&OnlineRecruitHover(new Room{phase="prepare"},new Player{board=new Unit[0],bench=new Unit[0]},new Vector2(370,860),true)==null,"partial or missing online shop data cannot create a hover target");
        try
        {
            foreach(var row in fixture.rows)
            {
                onlineMergeArrivals.Clear();onlineMergeArrivalReviewAge=null;
                var beforePlayer=MatchPlayer(row.before);string untouched=JsonUtility.ToJson(row.before);
                var preview=PreviewOnlinePurchase(beforePlayer,"koromon",row.before);
                OnlineRequire(JsonUtility.ToJson(row.before)==untouched,"purchase forecast cannot mutate server state "+row.name);
                OnlineRequire(preview.available==(row.star>0),"purchase forecast matches actual server rejection or acceptance "+row.name);
                if(preview.available)
                {
                    var actualPlayer=MatchPlayer(row.after);var actualUnit=At(row.area=="board"?actualPlayer.board:actualPlayer.bench,row.slot);
                    OnlineRequire(preview.result.star==row.star&&preview.result.board==(row.area=="board")&&preview.result.slot==row.slot&&preview.result.items.SequenceEqual(actualUnit.items),"purchase forecast survivor and exact equipment match server "+row.name);
                    OnlineRequire(preview.returned.SequenceEqual(actualPlayer.inventory.Skip(beforePlayer.inventory.Length)),"purchase forecast returned item identities and order match server "+row.name);
                    OnlineRequire(preview.after.Length==actualPlayer.board.Length+actualPlayer.bench.Length&&preview.after.All(p=>{var u=At(p.board?actualPlayer.board:actualPlayer.bench,p.slot);return u!=null&&u.id==p.id&&u.star==p.star&&u.items.SequenceEqual(p.items);}),"purchase forecast full formation matches server "+row.name);
                    if(row.star>1)
                    {
                        OnlineRequire(preview.sources.Count(s=>s.recipient)==1&&preview.sources.Where(s=>!s.recipient).All(s=>At(s.board?actualPlayer.board:actualPlayer.bench,s.slot)==null),"merge focus materials match actual server consumption "+row.name);
                        OnlineRequire(preview.sources.Sum(s=>s.star>0?(int)Mathf.Pow(3,s.star-1):0)+1==(int)Mathf.Pow(3,row.star-1),"merge focus accounts for every purchased base copy "+row.name);
                    }
                }
                if(row.name=="cascade"||row.name=="free_bench_pair"||row.name=="battle_cascade")
                {
                    state=new State{room=row.before,name="합성 대상 확인",combatActions=1};receivedAt=Time.unscaledTime;ClearOnlineUnitSelection();onlineReport=false;onlineSkillId="";
                    onlineValidationPointer=new Vector2(370,860);var focus=OnlineRecruitHover(row.before,beforePlayer,onlineValidationPointer.Value,true);
                    OnlineRequire(focus!=null&&focus.sources.Count==preview.sources.Count,"online shop hover uses the same exact purchase forecast "+row.name);
                    OnlineRequire(OnlineRecruitHover(row.before,beforePlayer,onlineValidationPointer.Value,false)==null&&OnlineRecruitHover(row.before,beforePlayer,new Vector2(1,1),true)==null,"online modal and outside pointer clear merge focus");
                    float received=receivedAt;receivedAt=Time.unscaledTime-10;OnlineRequire(OnlineRecruitHover(row.before,beforePlayer,onlineValidationPointer.Value,true)==null,"stale connection cannot retain a merge target");receivedAt=received;
                    foreach(var source in focus.sources)OnlineRequire(MergeFocusUI.Visible(source,row.before.phase=="battle")==(!source.board||row.before.phase!="battle"),"battle highlight excludes frozen board fighters");
                    yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();OnlineCapture("merge-focus-online-"+row.name);
                    OnlineRequire(JsonUtility.ToJson(row.before)==untouched,"merge hover leaves authoritative snapshot unchanged "+row.name);onlineValidationPointer=null;
                }
                if(row.name=="cascade"||row.name=="battle_cascade"||row.name=="duplicate_emblem"||row.name=="no_pair"||row.name=="ready_locked")
                {
                    state=new State{room=row.before,name="구매 미리보기"};receivedAt=Time.unscaledTime;ClearOnlineUnitSelection();onlineReport=false;
                    OpenOnlineShopSkill(Def("koromon"));yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();OnlineCapture("purchase-online-"+row.name);onlineSkillId="";
                    OnlineRequire(JsonUtility.ToJson(row.before)==untouched,"rendering purchase preview preserves server snapshot "+row.name);
                }
                state=new State{room=row.before,name="강화 현황 확인",combatActions=1};receivedAt=Time.unscaledTime;
                var collection=ReadOnlineCollection();var owned=beforePlayer.board.Concat(beforePlayer.bench).ToArray();
                foreach(var entry in collection.entries)
                    OnlineRequire(entry.singles==owned.Count(u=>u.id==entry.id&&u.star==1)&&entry.doubles==owned.Count(u=>u.id==entry.id&&u.star==2)&&entry.triples==owned.Count(u=>u.id==entry.id&&u.star==3)&&entry.offers==beforePlayer.shop.Count(id=>id==entry.id),"collection ranks and offers reflect server snapshot "+row.name+":"+entry.id);
                var collected=collection.entries.FirstOrDefault(r=>r.id=="koromon");
                OnlineRequire((collected!=null&&collected.CanUpgrade)==(row.star>1),"collection immediate merge agrees with actual server purchase "+row.name);
                if(row.name=="cascade"||row.name=="battle_cascade"||row.name=="ready_locked")
                {
                    string originalPlan=JsonUtility.ToJson(TeamPlan.Data);
                    if(row.name=="ready_locked"&&!TeamPlan.Contains("koromon"))TeamPlan.Toggle("koromon");
                    string plan=JsonUtility.ToJson(TeamPlan.Data);OpenTeamPlan();var panel=TeamPlannerUI.Bounds(new Rect(0,0,1600,1000));
                    var button=TeamPlannerUI.CollectionButton(panel);yield return OnlineClick(button.center.x,button.center.y);
                    OnlineRequire(showTeamPlan&&teamPlanner.CollectionOpen,"online collection opens through actual planner input");receivedAt=Time.unscaledTime;
                    if(row.name=="ready_locked")
                    {
                        var filter=TeamPlannerUI.CollectionFilterRect(panel,2);yield return OnlineClick(filter.center.x,filter.center.y);
                        var locked=ReadOnlineCollection().entries.First(r=>r.id=="koromon");
                        OnlineRequire(locked.offers>0&&!locked.CanBuy&&locked.Status.Contains("준비 해제 후 모집"),"ready collection explicitly explains why an offered planned recruit cannot be bought");
                    }
                    yield return new WaitForEndOfFrame();OnlineCapture("collection-online-"+row.name);
                    if(row.name=="cascade")
                    {
                        var card=TeamPlannerUI.CollectionCard(panel,0);yield return OnlineClick(card.center.x,card.center.y,1);
                        OnlineRequire(teamPlanner.InspectionId=="koromon","online collection inspects server chain-upgrade target");
                        yield return new WaitForEndOfFrame();OnlineCapture("collection-online-inspection");
                        foreach(var key in new[]{KeyCode.D,KeyCode.F,KeyCode.E,KeyCode.W,KeyCode.Return})yield return OnlineKey(key);
                        yield return OnlineKey(KeyCode.Escape);OnlineRequire(showTeamPlan&&teamPlanner.CollectionOpen&&teamPlanner.InspectionId=="","online first Escape returns to collection");
                        receivedAt=Time.unscaledTime-10;OnlineRequire(ReadOnlineCollection().entries.All(r=>!r.CanBuy),"stale connection never claims immediate collection purchases");
                        yield return new WaitForEndOfFrame();OnlineCapture("collection-online-stale");receivedAt=Time.unscaledTime;
                        string previousPhase=row.before.phase;row.before.phase="finished";
                        OnlineRequire(ReadOnlineCollection().entries.All(r=>!r.CanBuy)&&ReadOnlineCollection().entries.First(r=>r.id=="koromon").Status.Contains("경기 종료"),"finished match cannot advertise collection purchases");row.before.phase=previousPhase;
                    }
                    OnlineRequire(JsonUtility.ToJson(row.before)==untouched&&JsonUtility.ToJson(TeamPlan.Data)==plan,"collection inspection cannot change server units, equipment, gold or goals");
                    yield return OnlineKey(KeyCode.Escape);OnlineRequire(!showTeamPlan,"online collection returns to gameplay with Escape");
                    TeamPlan.Restore(originalPlan);
                }
                int requestedSounds=StarUpgradeAudio.RequestedCount;var receipt=RecruitResult(row.before,row.after,command);
                if(row.star==0){ReceiveRecruitment(row.before,row.after,"/action",command);OnlineRequire(receipt==null&&onlineMergeArrivals.Count==0&&StarUpgradeAudio.RequestedCount==requestedSounds,"rejected purchase cannot show success or arrival or sound: "+row.name);continue;}
                OnlineRequire(receipt!=null&&receipt.star==row.star&&receipt.area==row.area&&receipt.slot==row.slot,"receipt follows actual server merge target: "+row.name);
                OnlineRequire(receipt.text==RecruitmentAdvice.Result(Def("koromon").name,row.star,1,row.returnedItems),"actual merge receipt includes cost and overflow: "+row.name);
                ReceiveRecruitment(row.before,row.after,"/action",command);float deadline=recruitReceipt.until;
                OnlineRequire(StarUpgradeAudio.RequestedCount==requestedSounds+(row.star>1?1:0),"only authoritative star upgrades request one sound "+row.name);
                int arrivals=onlineMergeArrivals.Count;
                OnlineRequire(arrivals==(row.star>1&&!(row.before.phase=="battle"&&row.area=="board")?1:0),"only authoritative star merges create arrivals; battle board stays frozen "+row.name);
                float arrivalStarted=arrivals>0?onlineMergeArrivals[0].flight.started:0;
                if(arrivals>0)OnlineRequire(onlineMergeArrivals[0].flight.origins.Length==(row.star==3?4:2),"arrival uses exact consumed material origins "+row.name);
                ReceiveRecruitment(row.after,row.after,"/state",new Command());ReceiveRecruitment(row.after,row.after,"/login",new Command());
                ReceiveRecruitment(row.after,row.after,"/action",command);
                OnlineRequire(recruitReceipt.until==deadline&&onlineMergeArrivals.Count==arrivals&&(arrivals==0||onlineMergeArrivals[0].flight.started==arrivalStarted),"poll reconnect and duplicate action cannot replay purchase feedback or arrival");
                OnlineRequire(StarUpgradeAudio.RequestedCount==requestedSounds+(row.star>1?1:0),"poll login and duplicate action cannot replay upgrade audio "+row.name);
                var me=MatchPlayer(row.after);var target=At(row.area=="board"?me.board:me.bench,row.slot);
                OnlineRequire((OnlinePromotion(target,row.area)>0)==(row.star>1),"only promoted target pulses: "+row.name);
                if(row.star>1)
                {
                    recruitReceipt.until=Time.unscaledTime-1;OnlineRequire(OnlinePromotion(target,row.area)==0,"expired promotion stops");
                    recruitReceipt.until=deadline;
                }
                if(row.name=="cascade"||row.name=="split_pair"||row.name=="free_bench_pair"||row.name=="battle_cascade")
                {
                    state=new State{room=row.after,name="승급 확인",combatActions=1};receivedAt=Time.unscaledTime;ClearOnlineUnitSelection();onlineReport=false;
                    yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();OnlineCapture("recruitment-"+row.name);
                    onlineMergeArrivalReviewAge=.35f;
                    yield return new WaitForEndOfFrame();OnlineCapture("merge-arrival-online-"+row.name);
                    if(row.name=="battle_cascade")
                    {
                        DigimonBuildCatalog.Bonus bonus;Fighter live;var stats=OnlineStats(target,row.area,out bonus,out live);
                        OnlineRequire(live!=null&&live.star==2&&target.star==3&&OnlineInspectionStar(target,live)==2,"mid-battle upgrade inspects frozen combat star instead of next formation star");
                        var skill=DigimonSkillCatalog.Find(target.id);
                        OnlineRequire(Mathf.Abs(skill.Damage(OnlineInspectionStar(target,live),stats.attack,stats.abilityPower)-skill.Damage(2,live.attackDamage,live.abilityPower))<.001f,"mid-battle skill preview matches real battle coefficients");
                        int statsVersion=live.combatStatsVersion;live.combatStatsVersion=0;
                        var legacy=OnlineStats(target,row.area);var expectedLegacy=new DigimonSkillCatalog.Stats(skill,2,bonus);
                        OnlineRequire(Mathf.Abs(legacy.attack-expectedLegacy.attack)<.001f&&Mathf.Abs(legacy.health-expectedLegacy.health)<.01f,"legacy combat inspection also uses actual battle star after a three-star merge");live.combatStatsVersion=statsVersion;
                        selectedArea=row.area;selectedSlot=target.slot;onlineReport=false;
                        yield return new WaitForEndOfFrame();yield return OnlineClick(1450,812);
                        OnlineRequire(onlineSkillId==target.id&&onlineSkillArea==row.area,"left-click skill title opens detailed combat-star comparison");
                        yield return new WaitForEndOfFrame();OnlineCapture("upgrade-battle-current-vs-next");onlineSkillId="";
                    }
                }
                var unchanged=RecruitResult(row.after,row.after,command);OnlineRequire(unchanged==null,"duplicate response is not a second purchase");
                int originalGold=me.gold;me.gold=originalGold+1;OnlineRequire(RecruitResult(row.before,row.after,command)==null,"unexpected gold delta cannot fabricate receipt");me.gold=originalGold;
                if(arrivals>0)
                {
                    foreach(string action in new[]{"move","sell"})
                    {ReceiveMergeArrival(row.before,row.after,receipt);ReceiveRecruitment(row.after,row.after,"/action",new Command{action=action});OnlineRequire(onlineMergeArrivals.Count==0,"formation action clears stale arrival "+action);}
                    ReceiveMergeArrival(row.before,row.after,receipt);
                }
                var next=new Room{id=row.after.id,phase=row.after.phase=="battle"?"prepare":"battle",round=row.after.round};ReceiveRecruitment(row.after,next,"/state",new Command());
                OnlineRequire(recruitReceipt==null&&onlineMergeArrivals.Count==0,"combat transition clears purchase feedback and arrivals");
            }
            Debug.Log("PURCHASE FORECAST SERVER COMPLETE: "+fixture.rows.Length+" actual production purchases, exact targets/retained and returned items/full formations/rejections, read-only preview");
            Debug.Log("STAR SOUND ONLINE COMPLETE: real server purchases, ordinary/rejected purchase silence and duplicate/poll/reconnect suppression");
            Debug.Log("STAR COLLECTION ONLINE COMPLETE: real server rank/shop snapshots and purchase parity, prepare/battle/ready/stale states, actual input and read-only nested inspection");
            Debug.Log("MERGE ARRIVAL ONLINE COMPLETE: server result parity, exact origins, battle board frozen, duplicate/poll/reconnect suppression, move/sell/phase cleanup");
            Debug.Log("MERGE FOCUS ONLINE COMPLETE: server consumption parity, free bench destination, battle bench only, modal/pointer/connection guards and unchanged snapshots");
        }
        finally{soundSettings.Dispose();showTeamPlan=false;teamPlanner.ResetView();onlineMergeArrivalReviewAge=null;onlineMergeArrivals.Clear();onlineMergeArrivals.AddRange(savedArrivals);state=savedState;receivedAt=savedReceived;recruitReceipt=savedReceipt;onlineReport=true;onlineValidationPointer=null;}
    }
    void ValidateMatchSelectionRules()
    {
        Func<Room> room=()=>new Room{id="selection-test",phase="prepare",round=1,side=0,players=new[]{
            new Player{board=new[]{new Unit{id="koromon",star=1,slot=3}},bench=new Unit[0]},
            new Player{board=new Unit[0],bench=new Unit[0]}}};
        var previous=room();var next=room();
        selectedArea="board";selectedSlot=3;onlineReport=false;arenaPointer.Update(31,true,false,false,false);
        ReconcileMatchSelection(previous,next);
        OnlineRequire(selectedSlot==3&&!onlineReport&&arenaPointer.HasPress,"ordinary poll preserves selection and click in progress");
        next.players[0].ready=true;ReconcileMatchSelection(previous,next);
        OnlineRequire(selectedSlot==3&&!arenaPointer.HasPress,"ready transition cancels pending click while retaining inspection");
        next=room();next.players[0].board[0].items=new[]{5};ReconcileMatchSelection(previous,next);
        OnlineRequire(selectedSlot==3,"equipment update retains same unit inspection");
        next.players[0].board[0].id="tokomon";ReconcileMatchSelection(previous,next);
        OnlineRequire(selectedSlot==-1&&selectedArea=="","replacement in selected slot cancels old selection");
        foreach(string change in new[]{"battle","round","room","side","removed","star","finished","leave"})
        {
            next=room();selectedArea="board";selectedSlot=3;onlineReport=false;arenaPointer.Update(31,true,false,false,false);
            if(change=="battle"||change=="finished")next.phase=change;
            if(change=="round")next.round=2;if(change=="room")next.id="other";if(change=="side")next.side=1;
            if(change=="removed")next.players[0].board=new Unit[0];if(change=="star")next.players[0].board[0].star=2;
            if(change=="leave")next=null;
            ReconcileMatchSelection(previous,next);
            OnlineRequire(selectedSlot==-1&&selectedArea==""&&!arenaPointer.HasPress,"selection invalidated on "+change);
            arenaPointer.Update(31,false,true,false,false);
            OnlineRequire(arenaPointer.Released==-1,"old mouse release cannot move unit after "+change);
        }
        next=room();next.phase="battle";next.battleDuration=2;
        next.frames=new[]{new Frame{time=0,units=new[]{new Fighter{id="koromon",slot=3,side=0,hp=100,attackRange=2},new Fighter{side=1,hp=100}}},
            new Frame{time=2,units=new[]{new Fighter{id="koromon",slot=3,side=0,hp=0},new Fighter{side=1,hp=10}}}};
        selectedArea="board";selectedSlot=3;
        OnlineRequire(OnlinePreparationCell(next)==-1,"combat never selects preparation cell");
        OnlineRequire(OnlineRangeHint(next,next.players[0],2).Contains("2칸"),"living range hint uses replay range");
        OnlineRequire(OnlineRangeHint(next,next.players[0],0).Contains("전투 불능")&&!OnlineRangeHint(next,next.players[0],0).Contains("공격 범위"),"dead inspection has no attack range hint");
        OnlineRequire(OnlineCombatSummary(next,2).Contains("아군 1 / 1")&&OnlineCombatSummary(next,0).Contains("아군 0 / 1"),"survivor counts follow current frame instead of final result");
        ClearOnlineUnitSelection();onlineReport=true;
    }
    IEnumerator OnlineSmoke()
    {
        yield return null;
        ValidateMatchSelectionRules();
        ValidateOnlineTraitHudCache();
        string[] args=Environment.GetCommandLineArgs();int argument=Array.IndexOf(args,"--test-server");
        OnlineRequire(argument>=0&&argument+1<args.Length,"explicit isolated test server");
        server=args[argument+1];OnlineRequire(new Uri(server).IsLoopback,"test server must be local");artPack=0;ValidateBasicReplay();ValidateOnlineHitReactions();ValidateReplayLookup();ValidateContactReplay();ValidateOnlineCombatAudio();
        OnlineRequire(DecodeState("{\"token\":\"test\",\"room\":null}").room==null,"JSON null room remains lobby");
        string key=Guid.NewGuid().ToString("N")+Guid.NewGuid().ToString("N");
        yield return SmokeRequest("/login",new Command{name="장비 테스트",key=key});
        OnlineRequire(state.room==null,"login without a match remains in lobby");
        OnlineRequire(state.reliableCommands==1&&recoveryKey==key,"reliable protocol and recovery identity negotiated");
        yield return PeerRequest("/login",new Command{name="테스트 상대",key=Guid.NewGuid().ToString("N")+Guid.NewGuid().ToString("N")});
        lobbyLoadout=new TamerLoadout{tamer=1,field=1,finisher=2};
        lobbyTab=4;yield return new WaitForSeconds(.4f);yield return new WaitForEndOfFrame();OnlineCapture("14-loadout-lobby");lobbyTab=0;
        yield return SmokeRequest("/queue",new Command{mode="normal"});
        yield return PeerRequest("/queue",new Command{mode="normal",tamer=3,field=2,finisher=1});
        yield return SmokeRequest("/state");
        OnlineRequire(OnlineMe.inventory.SequenceEqual(new[]{0,1,2,3,14,15,16}),"initial supplies deserialize");
        string connectedRoom=state.room.id;
        token="expired-smoke-token";
        yield return Request("/state",new Command());
        OnlineRequire(recoveringLogin&&connectionError&&state.room.id==connectedRoom,"expired session keeps match view while login recovery is pending");
        yield return new WaitForEndOfFrame();OnlineCapture("network-session-recovery");
        nextPoll=0;PollConnection();yield return AwaitEquipmentAction();
        OnlineRequire(!recoveringLogin&&state.room.id==connectedRoom,"same identity restores the running match");
        OnlineRequire(state.room.players.All(p=>p.connectionKnown&&p.connected),"both real participants report connected presence");
        yield return Request("/action",new Command{action="buy",slot=-1});
        OnlineRequire(!connectionError&&!busy&&notice.Contains("슬롯"),"business rejection is not a network failure");
        yield return ServerPause(.2);
        OnlineRequire(OnlineMe.tamer==1&&OnlineMe.field==1&&OnlineMe.finisher==2,"equipped loadout reaches real server");
        OnlineRequire(state.room.players[1-state.room.side].tamer==3,"opponent tamer is visible");
        yield return SmokeRequest("/tamer-move",new Command{x=5,y=6});
        OnlineRequire(OnlineMe.tamerX==5&&OnlineMe.tamerY==6,"tamer movement reaches real server");
        yield return PeerRequest("/state",new Command());
        OnlineRequire(smokePeer.room.players[state.room.side].tamerX==5,"opponent sees tamer movement");

        OnlineRequire(state.room.players[1-state.room.side].inventory.Length==0,"opponent inventory hidden");
        TeamPlan.Restore("{}");TeamPlan.Toggle("agumon");TeamPlan.Toggle("greymon");TeamPlan.Toggle("wargreymon");TeamPlan.Toggle(OnlineMe.shop[0]);
        OpenTeamPlan();teamPlanner.Query="";teamPlanner.Cost=0;yield return new WaitForEndOfFrame();OnlineCapture("15-team-planner");
        string onlinePlanBefore=JsonUtility.ToJson(TeamPlan.Data);int onlinePlanGold=OnlineMe.gold;
        Rect onlinePlanCard=TeamPlannerUI.CardRect(TeamPlannerUI.Bounds(new Rect(0,0,1600,1000)),0);
        yield return OnlineClick(onlinePlanCard.center.x,onlinePlanCard.center.y,1);
        OnlineRequire(showTeamPlan&&teamPlanner.InspectionId=="koromon"&&JsonUtility.ToJson(TeamPlan.Data)==onlinePlanBefore&&OnlineMe.gold==onlinePlanGold,"online planner inspection does not change plan or gold");
        yield return new WaitForEndOfFrame();OnlineCapture("planner-online-inspection");
        yield return OnlineKey(KeyCode.Escape);OnlineRequire(showTeamPlan&&teamPlanner.InspectionId=="","online first Escape returns to planner");
        Rect onlineForecast=TeamPlannerUI.SynergyViewport(TeamPlannerUI.Bounds(new Rect(0,0,1600,1000)));
        string onlineTrait=TeamPlannerUI.ForecastTraits()[0].id;
        yield return OnlineClick(onlineForecast.x+100,onlineForecast.y+21);
        OnlineRequire(showTeamPlan&&teamPlanner.TraitFocus==onlineTrait&&JsonUtility.ToJson(TeamPlan.Data)==onlinePlanBefore&&OnlineMe.gold==onlinePlanGold,"online synergy forecast preserves plan and gold");
        yield return new WaitForEndOfFrame();OnlineCapture("planner-online-synergy");
        yield return OnlineKey(KeyCode.Escape);OnlineRequire(showTeamPlan&&teamPlanner.TraitFocus=="","online trait Escape returns to planner");
        yield return OnlineKey(KeyCode.Escape);OnlineRequire(!showTeamPlan,"online second Escape closes planner");
        yield return new WaitForEndOfFrame();OnlineCapture("01-inventory");
        OpenOnlineTrait("harmonizer");yield return new WaitForEndOfFrame();OnlineCapture("synergy-harmonizer");showTraitGuide=false;
        onlineRecruitInputPending=true;yield return new WaitForEndOfFrame();OnlineCapture("12-shop-skill");
        OnlineRequire(!onlineRecruitInputPending,"online shop inspection exercised in GUI");onlineSkillId="";
        int lockGold=OnlineMe.gold;var offersBeforeLock=OnlineMe.shop.ToArray();
        yield return OnlineClick(1190,796);yield return AwaitEquipmentAction();
        OnlineRequire(OnlineMe.shopLocked&&OnlineMe.gold==lockGold&&OnlineMe.shop.SequenceEqual(offersBeforeLock),"shop lock button preserves offers and gold");
        showTeamPlan=true;yield return OnlineClick(1190,796);
        OnlineRequire(OnlineMe.shopLocked&&!busy,"planner blocks shop lock behind modal");showTeamPlan=false;
        yield return new WaitForEndOfFrame();OnlineCapture("16-shop-locked");
        int beforeBuyGold=OnlineMe.gold,beforeBuyCopies=OnlineMe.bench.Length;
        int offerCost=Def(OnlineMe.shop[0]).cost;
        yield return SmokeRequest("/action",new Command{action="buy",slot=0});
        OnlineRequire(OnlineMe.gold==beforeBuyGold-offerCost&&OnlineMe.bench.Length==beforeBuyCopies+1,"retried purchase consumes gold and adds one unit exactly once");
        OnlineRequire(recruitReceipt!=null&&recruitReceipt.star==1&&recruitReceipt.text.Contains("모집 완료"),"real HTTP purchase produces recruitment receipt");
        yield return new WaitForEndOfFrame();OnlineCapture("recruitment-purchase");
        var lockedOffers=OnlineMe.shop.ToArray();
        OnlineRequire(lockedOffers[0]==""&&OnlineMe.shopLocked,"purchase keeps lock and leaves empty slot");
        int newcomerSlot=OnlineMe.bench[0].slot;selectedArea="bench";selectedSlot=newcomerSlot;
        onlineValidationPointer=arena.Project(TacticalArena.CellWorld(3,5));
        yield return new WaitForEndOfFrame();OnlineCapture("13-formation-preview");onlineValidationPointer=null;
        var formationBoard=new string[28];var formationBench=new string[9];
        foreach(var u in OnlineMe.board)formationBoard[u.slot]=u.id;
        foreach(var u in OnlineMe.bench)formationBench[u.slot]=u.id;
        var formationPlan=FormationForecast.Preview(formationBoard,formationBench,false,newcomerSlot,true,10,OnlineMe.level);
        yield return SmokeRequest("/action",new Command{action="move",area="bench",slot=newcomerSlot,targetArea="board",targetSlot=10});
        OnlineRequire(formationPlan.allowed&&OnlineMe.board.All(u=>formationPlan.board[u.slot]==u.id),"placement forecast matches server unit positions");
        foreach(var t in formationPlan.traits)OnlineRequire(t.after==DigimonBuildCatalog.Count(t.trait,OnlineMe.board.Select(u=>u.id)),"placement forecast matches server traits");
        yield return SmokeRequest("/action",new Command{action="move",area="board",slot=10,targetArea="bench",targetSlot=newcomerSlot});
        selectedArea="";selectedSlot=-1;
        yield return ValidateOnlineScouting();
        yield return ValidateConvenienceHelp(true);
        var recipeBag=OnlineMe.inventory.ToArray();int recipeGold=OnlineMe.gold;
        yield return OnlineClick(OnlineInventoryRect(0).center.x,OnlineInventoryRect(0).center.y,1);
        OnlineRequire(onlineItemGuide==0&&onlineItem<0,"real right click opens contextual recipe without selecting gear");
        yield return new WaitForEndOfFrame();OnlineCapture("02-recipes");
        Rect recipePopup=DigimonEquipmentUI.PopupRect(onlineEquipmentAnchor,new Rect(0,76,1600,868),0);
        OnlineRequire(recipePopup.yMax<=932,"recipe popup remains above the connection status footer");
        yield return OnlineClick(DigimonEquipmentUI.RecipeRowRect(recipePopup,0).center.x,DigimonEquipmentUI.RecipeRowRect(recipePopup,0).center.y);
        OnlineRequire(onlineItemGuide==DigimonBuildCatalog.Combine(0,0),"recipe row click opens finished equipment effects");
        recipePopup=DigimonEquipmentUI.PopupRect(onlineEquipmentAnchor,new Rect(0,76,1600,868),onlineItemGuide);
        yield return OnlineClick(DigimonEquipmentUI.MaterialRect(recipePopup,0).center.x,DigimonEquipmentUI.MaterialRect(recipePopup,0).center.y);
        OnlineRequire(onlineItemGuide==0,"reverse recipe material click returns to component combinations");
        yield return OnlineClick(1190,796);yield return AwaitEquipmentAction();
        OnlineRequire(onlineItemGuide<0&&OnlineMe.shopLocked&&OnlineMe.gold==recipeGold&&OnlineMe.inventory.SequenceEqual(recipeBag),"outside dismiss consumes both phases without shop action or crafting");
        SelectOnlineItem(0);SelectOnlineItem(1);yield return AwaitEquipmentAction();
        OnlineRequire(OnlineMe.inventory.SequenceEqual(new[]{2,3,14,15,16,5}),"UI combines two components");
        SelectOnlineItem(Array.IndexOf(OnlineMe.inventory,5));ClickSlot("board",3,At(OnlineMe.board,3));yield return AwaitEquipmentAction();
        OnlineRequire(At(OnlineMe.board,3).items.SequenceEqual(new[]{5}),"UI equips crafted item");
        SelectOnlineItem(Array.IndexOf(OnlineMe.inventory,3));ClickSlot("board",3,At(OnlineMe.board,3));yield return AwaitEquipmentAction();
        SelectOnlineItem(Array.IndexOf(OnlineMe.inventory,2));
        var projected=DigimonEquipmentPreview.Create(At(OnlineMe.board,3).id,At(OnlineMe.board,3).star,OnlineMe.board.Select(u=>u.id),At(OnlineMe.board,3).items,2);
        onlineValidationPointer=arena.Project(TacticalArena.CellWorld(3,4));
        yield return new WaitForEndOfFrame();OnlineCapture("08-craft-preview");onlineValidationPointer=null;
        ClickSlot("board",3,At(OnlineMe.board,3));yield return AwaitEquipmentAction();
        OnlineRequire(At(OnlineMe.board,3).items.SequenceEqual(new[]{5,12}),"auto combination preserves other equipped item");
        OnlineRequire(At(OnlineMe.board,3).items.SequenceEqual(projected.change.items)&&OnlineStats(At(OnlineMe.board,3),"board").abilityPower==projected.after.abilityPower,"preview agrees with authoritative server gear and AP");
        yield return new WaitForEndOfFrame();OnlineCapture("03-equipped");
        yield return OnlineClick(OnlineEquippedSlotRect(1).center.x,OnlineEquippedSlotRect(1).center.y,1);
        OnlineRequire(onlineItemGuide==12,"equipped item right click opens its reverse recipe");
        yield return new WaitForEndOfFrame();OnlineCapture("04-item-detail");onlineItemGuide=-1;
        var equippedStats=OnlineStats(At(OnlineMe.board,3),"board");
        OnlineRequire(equippedStats.abilityPower==145,"equipped AP appears in client stats");
        OpenOnlineSkill(At(OnlineMe.board,3),"board");yield return new WaitForEndOfFrame();OnlineCapture("07-skill-detail");onlineSkillId="";
        SelectOnlineItem(Array.IndexOf(OnlineMe.inventory,16));ClickSlot("board",3,At(OnlineMe.board,3));yield return AwaitEquipmentAction();
        OnlineRequire(At(OnlineMe.board,3).items.SequenceEqual(new[]{5,12,16}),"third equipment slot reaches authoritative server");
        yield return SmokeRequest("/login",new Command{name="장비 테스트",key=key});
        OnlineRequire(At(OnlineMe.board,3).items.SequenceEqual(new[]{5,12,16}),"reconnection preserves all three equipped items");
        selectedArea="board";selectedSlot=3;onlineReport=false;
        yield return new WaitForEndOfFrame();OnlineCapture("equipment-03-slots");
        yield return OnlineClick(OnlineEquippedSlotRect(2).center.x,OnlineEquippedSlotRect(2).center.y,1);
        OnlineRequire(onlineItemGuide==16&&onlineItem<0,"right click inspects third equipped item without selecting it");
        yield return new WaitForEndOfFrame();OnlineCapture("equipment-04-third-detail");
        yield return OnlineKey(KeyCode.Escape);
        SelectOnlineItem(Array.IndexOf(OnlineMe.inventory,14));ClickSlot("board",3,At(OnlineMe.board,3));yield return AwaitEquipmentAction();
        OnlineRequire(OnlineMe!=null&&At(OnlineMe.board,3)!=null,"removal preserves unit slot");
        OnlineRequire(OnlineMe.inventory.SequenceEqual(new[]{15,5,12,16})&&At(OnlineMe.board,3).items.Length==0,"removal returns all three items exactly once");
        foreach(int id in new[]{5,12})
        {SelectOnlineItem(Array.IndexOf(OnlineMe.inventory,id));ClickSlot("board",3,At(OnlineMe.board,3));yield return AwaitEquipmentAction();}
        yield return SmokeRequest("/login",new Command{name="장비 테스트",key=key});
        OnlineRequire(At(OnlineMe.board,3).items.SequenceEqual(new[]{5,12}),"reconnect retains equipped items");
        OnlineRequire(OnlineMe.shopLocked&&OnlineMe.shop.SequenceEqual(lockedOffers),"reconnect retains locked offers");
        OnlineRequire(OnlineMe.tamer==1&&OnlineMe.field==1&&OnlineMe.finisher==2&&OnlineMe.tamerX==5,"reconnect retains appearance and movement");
        selectedArea="board";selectedSlot=3;onlineReport=false;
        yield return SmokeRequest("/action",new Command{action="ready"});
        OnlineRequire(selectedSlot==3,"ready response preserves current inspection");
        yield return PeerRequest("/action",new Command{action="ready"});
        yield return SmokeRequest("/state");
        OnlineRequire(state.room.phase=="battle","both clients enter battle");
        ValidateReplayLookup(state.room);ValidateSharedOnlineInspectionStats();
        OnlineRequire(!HasRoundResult(state.room)&&state.room.roundWinner==-1,"first live battle exposes no completed result or future winner");
        OnlineRequire(selectedSlot==-1&&selectedArea==""&&onlineReport&&!arenaPointer.HasPress,"actual battle response clears preparation selection");
        yield return OnlineClick(1190,796);yield return AwaitEquipmentAction();
        OnlineRequire(!OnlineMe.shopLocked&&OnlineMe.shop.SequenceEqual(lockedOffers),"unlock is allowed during combat without reroll");
        yield return OnlineClick(1190,796);yield return AwaitEquipmentAction();
        OnlineRequire(OnlineMe.shopLocked,"lock can be restored during combat");
        Fighter own=state.room.frames[0].units.First(f=>f.side==state.room.side);
        Fighter enemy=state.room.frames[0].units.First(f=>f.side!=state.room.side);
        OnlineRequire(Mathf.Abs(own.maxHp-enemy.maxHp-120)<.01f,"equipment health in actual server frames");
        OnlineRequire(Mathf.Abs(own.mana-enemy.mana-20)<.01f,"equipment start mana in server frames");
        OnlineRequire(VisibleCombatFrame(state.room,state.room.battleDuration).units.All(f=>f.damageDone==0),"live report starts at current frame without future damage");
        OnlineRequire(state.room.frames.Last().units.Sum(f=>f.damageDone)>0,"completed frames contain damage counters");
        var replayState=DecodeState("{\"room\":{\"id\":\"state-test\",\"phase\":\"battle\",\"battleDuration\":6,\"frames\":[{\"time\":0,\"units\":[{\"id\":\"patamon\",\"lowShieldUsed\":false,\"combatAge\":0}]},{\"time\":6,\"units\":[{\"id\":\"patamon\",\"lowShieldUsed\":true,\"combatAge\":6}]}]}}");
        OnlineRequire(!VisibleCombatFrame(replayState.room,6).units[0].lowShieldUsed&&VisibleCombatFrame(replayState.room,0).units[0].lowShieldUsed,"crisis display follows visible replay frame");
        OnlineRequire(replayState.room.frames[1].units[0].combatAge==6,"public synergy clock deserializes");
        var signalUnit=new Fighter{hp=1,combatStatsVersion=2,lowShieldUsed=true,crisisAt=5,friendshipActive=true};
        OnlineRequire(OnlineCombatSignal(signalUnit,5.4f).kind==1&&OnlineCombatSignal(signalUnit,10).kind==0&&OnlineCombatSignal(signalUnit,15.2f).kind==2,"online signal uses authoritative timestamp and maximum-stack timing");
        signalUnit.combatStatsVersion=1;OnlineRequire(OnlineCombatSignal(signalUnit,5.4f).kind==0,"old server cannot fabricate effect timestamps");
        var miracle=JsonUtility.FromJson<Fighter>("{\"key\":7,\"id\":\"ophanimon\",\"hp\":400,\"combatStatsVersion\":2,\"synergyUsed\":4,\"synergyKind\":5,\"synergyAt\":5,\"revived\":true,\"revivedAt\":5,\"burnRemaining\":2,\"armorShred\":0.35,\"shredRemaining\":3,\"battleAge\":5}");
        OnlineRequire(miracle.revivedAt==5&&miracle.synergyUsed==4&&miracle.burnRemaining==2&&Mathf.Abs(miracle.armorShred-.35f)<.001f,"authoritative revive and debuff fields deserialize");
        OnlineRequire(OnlineCombatSignal(miracle,5.2f).kind==5&&OnlineCombatSignal(miracle,8).kind==0,"miracle signal uses replay time without restarting on reconnect");
        var revivalRoom=new Room{skillEvents=new[]{new SkillEvent{caster=7,id="ophanimon",started=4.5f}}};
        OnlineRequire(ActiveCastAge(revivalRoom,7,5.2f)>=0&&ActiveCastAge(revivalRoom,miracle,5.2f)<0,"released projectile persists but revived actor never resumes old cast");
        revivalRoom.skillEvents=new[]{revivalRoom.skillEvents[0],new SkillEvent{caster=7,id="ophanimon",started=5.1f}};
        OnlineRequire(Mathf.Abs(ActiveCastAge(revivalRoom,miracle,5.2f)-.1f)<.001f,"revived actor shows its new cast");
        var deathFrames=new[]{new Frame{units=new[]{new Fighter{key=7,hp=100}}},new Frame{units=new[]{new Fighter{key=7,hp=0}}},new Frame{units=new[]{miracle}},new Frame{units=new[]{new Fighter{key=7,hp=0}}}};
        OnlineRequire(FighterDeathFrame(deathFrames,3,7)==3,"second death after miracle starts a fresh down animation");
        onlineReport=true;
        yield return new WaitForEndOfFrame();OnlineCapture("05-combat");
        yield return ServerPause(1.5);yield return new WaitForEndOfFrame();OnlineCapture("09-live-report");
        onlineReport=false;selectedArea="board";selectedSlot=own.slot;
        OnlineRequire(OnlineLiveFighter(At(OnlineMe.board,own.slot),"board")!=null,"selected board unit resolves replay vitals");
        yield return new WaitForEndOfFrame();OnlineCapture("10-live-vitals");onlineReport=true;
        DateTime timeout=DateTime.UtcNow.AddSeconds(35);
        while(state.room.phase=="battle"&&DateTime.UtcNow<timeout)
        {yield return ServerPause(.7);yield return SmokeRequest("/state");}
        OnlineRequire(state.room.phase=="prepare"&&state.room.round==2,"battle settles to next round");
        OnlineRequire(selectedSlot==-1&&selectedArea==""&&onlineReport,"actual settlement clears combat inspection before preparation");
        OnlineRequire(OnlineMe.inventory.SequenceEqual(new[]{15,16,0}),"next round supply arrives once");
        OnlineRequire(OnlineMe.shopLocked&&OnlineMe.shop.SequenceEqual(lockedOffers),"locked offers and bought hole survive settlement");
        OnlineRequire(state.room.reportRound==1&&state.room.lastCombat.Length==2,"completed report received after frames expire");
        OnlineRequire(HasRoundResult(state.room)&&state.room.roundResult.round==1,"completed settlement deserializes from server");
        var settlement=state.room.roundResult;string frozenSettlement=JsonUtility.ToJson(settlement);
        OnlineRequire(settlement.hpAfter==OnlineMe.hp&&settlement.healthLost==settlement.hpBefore-settlement.hpAfter,"settlement health equals actual player health");
        OnlineRequire(settlement.income==settlement.baseIncome+settlement.interest+settlement.winBonus&&settlement.baseIncome==5,"settlement income breakdown reconciles");
        onlineReport=false;selectedArea="board";selectedSlot=3;
        yield return new WaitForEndOfFrame();OnlineCapture("round-settlement");
        OpenTeamPlan();yield return OnlineClick(OnlineRoundBannerRect.center.x,OnlineRoundBannerRect.center.y);
        OnlineRequire(!onlineReport&&selectedSlot==3,"planner blocks settlement banner click");showTeamPlan=false;
        yield return OnlineClick(OnlineRoundBannerRect.center.x,OnlineRoundBannerRect.center.y);
        OnlineRequire(onlineReport&&selectedSlot==-1&&onlineHistoricalFighter==null,"settlement banner opens completed report and clears placement selection");
        yield return new WaitForEndOfFrame();
        OnlineRequire(cosmeticReport==1&&matchFinisher!=null,"round settlement starts online finisher");
        OnlineRequire(matchFinisher.Finisher==(matchFinisher.Winner==0?OnlineMe.finisher:state.room.players[1-state.room.side].finisher),"winner selected effect is used");
        float settledEffectStarted=matchFinisher.Started;

        float damage=state.room.lastCombat.Sum(f=>f.damageDone);
        OnlineRequire(damage>0&&Mathf.Abs(damage-state.room.lastCombat.Sum(f=>f.damageTaken))<.1f,"reported damage matches received health loss");
        OnlineRequire(state.room.lastCombat.All(f=>Mathf.Abs(f.damageDone-f.basicDamageDone-f.skillDamageDone)<.1f),"online basic and skill totals reconcile");
        yield return SmokeRequest("/login",new Command{name="장비 테스트",key=key});
        OnlineRequire(state.room.reportRound==1&&Mathf.Abs(state.room.lastCombat.Sum(f=>f.damageDone)-damage)<.1f,"reconnect retains completed combat report");
        OnlineRequire(JsonUtility.ToJson(state.room.roundResult)==frozenSettlement,"reconnect retains frozen settlement");
        yield return new WaitForEndOfFrame();OnlineCapture("11-last-report");
        OnlineRequire(matchFinisher.Started==settledEffectStarted,"reconnect does not replay finisher");
        Vector2 firstClick=arena.Project(TacticalArena.CellWorld(3,4));
        yield return OnlineClick(firstClick.x,firstClick.y);
        OnlineRequire(selectedSlot==3&&selectedArea=="board"&&!busy&&At(OnlineMe.board,3)!=null,"first next-round click selects a unit without sending a move");
        ClearOnlineUnitSelection();onlineReport=true;yield return new WaitForEndOfFrame();
        var resultUnit=state.room.lastCombat.First(f=>f.side==state.room.side);
        OnlineRequire(resultUnit.combatStatsVersion==2&&resultUnit.abilityPower==145&&Mathf.Abs(resultUnit.attackDamage-equippedStats.attack)<.001f,"historical stats deserialize from actual frozen fight");
        yield return OnlineClick(1410,410);
        OnlineRequire(onlineHistoricalFighter!=null&&!ReferenceEquals(onlineHistoricalFighter,resultUnit)&&selectedSlot==-1&&selectedArea=="","historical row opens detached record without selecting current board");
        float recordedMax=onlineHistoricalFighter.maxHp,recordedAttack=onlineHistoricalFighter.attackDamage;
        yield return SmokeRequest("/action",new Command{action="sell",area="board",slot=3});
        OnlineRequire(At(OnlineMe.board,3)==null&&onlineHistoricalFighter.maxHp==recordedMax&&onlineHistoricalFighter.attackDamage==recordedAttack,"selling actual unit never rewrites historical detail");
        yield return new WaitForEndOfFrame();OnlineCapture("17-historical-unit");
        yield return OnlineClick(1435,892);
        OnlineRequire(onlineHistoricalFighter==null&&onlineReport,"back button returns to report list");
        selectedArea="";selectedSlot=-1;yield return new WaitForEndOfFrame();OnlineCapture("06-next-round");
        OnlineRequire(RoundResultDetails(state.room).Contains("기본 +5G")&&RoundResultTitle(state.room).Contains("지난 1R"),"settlement banner uses completed round and income components");
        var laggedPlayer=new Player{connectionKnown=true,connected=false,reconnectRemaining=45};
        OnlineRequire(OpponentConnectionText(laggedPlayer).Contains("재접속 대기"),"opponent reconnect grace is visible");
        connectionError=true;
        OnlineRequire(OpponentConnectionText(laggedPlayer).Contains("확인 중"),"local outage does not claim opponent disconnected");
        connectionError=false;
        ValidateSignalReplayRendering();
        yield return ValidateRecruitmentFeedback();
        yield return ValidateCombatManagementOnline();
        yield return ValidateEmblemsOnline();
        // Verify the visible cancel action also prevents the automatic poll from reusing credentials.
        state=null;token="";recoveringLogin=true;recoveryKey=key;recoveryName="장비 테스트";connectionError=true;lobbyTab=3;
        yield return new WaitForEndOfFrame();OnlineCapture("network-reconnect-cancel");
        yield return OnlineClick(LobbyServerActionRect.center.x,LobbyServerActionRect.center.y);
        OnlineRequire(!recoveringLogin&&!connectionError&&recoveryKey==""&&state==null,"cancel connection UI clears recovery identity and state");
        nextPoll=0;PollConnection();
        OnlineRequire(!busy&&token=="","cancelled connection does not automatically log in again");
        Debug.Log("ONLINE SMOKE COMPLETE: "+onlineChecks+" checks");Application.Quit();
    }
}
public sealed partial class TacticalArena
{
    public Color ValidationTileColor(int index)
    {
        var block=new MaterialPropertyBlock();tiles[index].GetPropertyBlock(block);return block.GetColor("_Color");
    }
}
#endif
