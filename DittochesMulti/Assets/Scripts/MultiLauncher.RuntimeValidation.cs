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
    int onlineClickStep;Vector2 onlineClickPoint;
    bool onlineDragValidation,onlineDragCancel;
    Vector2 onlineDragEnd;
    Event OnlineValidationEvent()
    {
        if(onlineClickStep==0||Event.current.type!=EventType.Repaint)return null;
        Event saved=new Event(Event.current);
        if(onlineDragValidation)
        {
            if(onlineClickStep==3)return null;
            Event.current=new Event{type=onlineClickStep==1?EventType.MouseDown:onlineClickStep==2?EventType.MouseDrag:onlineDragCancel?EventType.KeyDown:EventType.MouseUp,
                keyCode=onlineDragCancel?KeyCode.Escape:KeyCode.None,button=0,mousePosition=onlineClickStep==1?onlineClickPoint:onlineDragEnd};
            onlineClickStep=onlineClickStep==1?2:onlineClickStep==2?3:0;return saved;
        }
        Event.current=new Event{type=onlineClickStep==1?EventType.MouseDown:EventType.MouseUp,button=0,mousePosition=onlineClickPoint};
        onlineClickStep=onlineClickStep==1?2:0;return saved;
    }
    IEnumerator OnlineClick(float x,float y)
    {
        onlineClickPoint=new Vector2(x,y);onlineClickStep=1;float deadline=Time.unscaledTime+3;
        while(onlineClickStep>0&&Time.unscaledTime<deadline)yield return null;
        OnlineRequire(onlineClickStep==0,"online UI received both click phases");yield return null;
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
        onlineItemPage=slot/12;onlineDragValidation=true;onlineDragCancel=cancel;
        onlineClickPoint=OnlineInventoryRect(slot%12).center;onlineDragEnd=target;onlineClickStep=1;float deadline=Time.unscaledTime+4;
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
        SelectOnlineItem(0);SelectOnlineItem(1);yield return AwaitEquipmentAction();
        SelectOnlineItem(Array.IndexOf(OnlineMe.inventory,5));ReconcileEquipmentSelection();
        OnlineRequire(onlineItem>=0,"selected equipment survives combat UI reconciliation");
        var row=onlineCombatLabels.First(r=>((Fighter)r.key).side==state.room.side&&((Fighter)r.key).slot==3);
        yield return OnlineGearDrag(onlineItem,row.rect.center,false);
        OnlineRequire(At(OnlineMe.board,3).items.SequenceEqual(new[]{5}),"combat UI equips item on persistent board");
        OnlineRequire(state.room.frames.Any(frame=>frame.units.Any(f=>f.side==state.room.side&&f.maxHp>max)),"live equipment updates authoritative replay health");
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
            foreach(KeyCode key in new[]{KeyCode.D,KeyCode.F,KeyCode.Space})
            {
                me.gold=50;GUI.enabled=false;Event.current=new Event{type=EventType.KeyDown,keyCode=key};
                DrawOnlineEconomy(me,state.room,true,true);
                OnlineRequire(!busy&&me.gold==50&&me.shop[0]==offer,"modal blocks online economy shortcut "+key);
            }
            me.gold=0;GUI.enabled=true;Event.current=new Event{type=EventType.KeyDown,keyCode=KeyCode.D};
            DrawOnlineEconomy(me,state.room,true,true);
            OnlineRequire(!busy&&me.gold==0&&me.shop[0]==offer,"insufficient gold blocks reroll shortcut");
            OpenTeamPlan();me.gold=50;
            foreach(KeyCode key in new[]{KeyCode.D,KeyCode.F,KeyCode.Space})
            {
                Event.current=new Event{type=EventType.KeyDown,keyCode=key};DrawMatch();
                OnlineRequire(!busy&&me.gold==50&&me.shop[0]==offer,"team planner blocks online action "+key);
            }
            Event.current=new Event{type=EventType.KeyDown,keyCode=KeyCode.Escape};DrawTeamPlan();
            OnlineRequire(!showTeamPlan&&Event.current.type==EventType.Used,"team planner closes online with Escape");
            OpenOnlineTrait("harmonizer");
            foreach(KeyCode key in new[]{KeyCode.D,KeyCode.F,KeyCode.Space})
            {
                Event.current=new Event{type=EventType.KeyDown,keyCode=key};DrawMatch();
                OnlineRequire(!busy&&me.gold==50&&me.shop[0]==offer,"trait guide blocks online action "+key);
            }
            GUI.enabled=true;Event.current=new Event{type=EventType.KeyDown,keyCode=KeyCode.Escape};DrawOnlineTraitGuide();
            OnlineRequire(!showTraitGuide&&Event.current.type==EventType.Used,"trait guide closes online with Escape");
            ValidateOnlineEquipmentTargetsInGUI();
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
    IEnumerator ValidateRecruitmentFeedback()
    {
        string file=Path.Combine(Application.dataPath,"../RecruitmentCaptures/fixture.json");
        if(!File.Exists(file)){Debug.Log("RECRUITMENT: optional fixture absent; run create_recruitment_fixture.py for merge feedback cases.");yield break;}
        var fixture=JsonUtility.FromJson<RecruitFixture>(File.ReadAllText(file));var command=new Command{action="buy",slot=0};
        State savedState=state;var savedReceipt=recruitReceipt;float savedReceived=receivedAt;
        try
        {
            foreach(var row in fixture.rows)
            {
                var receipt=RecruitResult(row.before,row.after,command);
                if(row.star==0){OnlineRequire(receipt==null,"rejected purchase cannot show success: "+row.name);continue;}
                OnlineRequire(receipt!=null&&receipt.star==row.star&&receipt.area==row.area&&receipt.slot==row.slot,"receipt follows actual server merge target: "+row.name);
                OnlineRequire(receipt.text==RecruitmentAdvice.Result(Def("koromon").name,row.star,1,row.returnedItems),"actual merge receipt includes cost and overflow: "+row.name);
                ReceiveRecruitment(row.before,row.after,"/action",command);float deadline=recruitReceipt.until;
                ReceiveRecruitment(row.after,row.after,"/state",new Command());ReceiveRecruitment(row.after,row.after,"/login",new Command());
                OnlineRequire(recruitReceipt.until==deadline,"poll and reconnect cannot replay purchase feedback");
                var me=MatchPlayer(row.after);var target=At(row.area=="board"?me.board:me.bench,row.slot);
                OnlineRequire((OnlinePromotion(target,row.area)>0)==(row.star>1),"only promoted target pulses: "+row.name);
                if(row.star>1)
                {
                    recruitReceipt.until=Time.unscaledTime-1;OnlineRequire(OnlinePromotion(target,row.area)==0,"expired promotion stops");
                    recruitReceipt.until=deadline;
                }
                if(row.name=="cascade"||row.name=="split_pair")
                {
                    state=new State{room=row.after,name="승급 확인"};receivedAt=Time.unscaledTime;ClearOnlineUnitSelection();onlineReport=false;
                    yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();OnlineCapture("recruitment-"+row.name);
                }
                var unchanged=RecruitResult(row.after,row.after,command);OnlineRequire(unchanged==null,"duplicate response is not a second purchase");
                int originalGold=me.gold;me.gold=originalGold+1;OnlineRequire(RecruitResult(row.before,row.after,command)==null,"unexpected gold delta cannot fabricate receipt");me.gold=originalGold;
                var next=new Room{id=row.after.id,phase="battle",round=row.after.round};ReceiveRecruitment(row.after,next,"/state",new Command());
                OnlineRequire(recruitReceipt==null,"combat transition clears purchase feedback");
            }
        }
        finally{state=savedState;receivedAt=savedReceived;recruitReceipt=savedReceipt;onlineReport=true;}
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
        string[] args=Environment.GetCommandLineArgs();int argument=Array.IndexOf(args,"--test-server");
        OnlineRequire(argument>=0&&argument+1<args.Length,"explicit isolated test server");
        server=args[argument+1];OnlineRequire(new Uri(server).IsLoopback,"test server must be local");artPack=0;
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
        showTeamPlan=true;yield return new WaitForEndOfFrame();OnlineCapture("15-team-planner");showTeamPlan=false;
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
        onlineItemGuide=0;yield return new WaitForEndOfFrame();OnlineCapture("02-recipes");onlineItemGuide=-1;
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
        OnlineRequire(At(OnlineMe.board,3).items.SequenceEqual(new[]{5,12}),"auto combination on full unit");
        OnlineRequire(At(OnlineMe.board,3).items.SequenceEqual(projected.change.items)&&OnlineStats(At(OnlineMe.board,3),"board").abilityPower==projected.after.abilityPower,"preview agrees with authoritative server gear and AP");
        yield return new WaitForEndOfFrame();OnlineCapture("03-equipped");
        onlineItemGuide=12;yield return new WaitForEndOfFrame();OnlineCapture("04-item-detail");onlineItemGuide=-1;
        var equippedStats=OnlineStats(At(OnlineMe.board,3),"board");
        OnlineRequire(equippedStats.abilityPower==145,"equipped AP appears in client stats");
        OpenOnlineSkill(At(OnlineMe.board,3),"board");yield return new WaitForEndOfFrame();OnlineCapture("07-skill-detail");onlineSkillId="";
        SelectOnlineItem(Array.IndexOf(OnlineMe.inventory,14));ClickSlot("board",3,At(OnlineMe.board,3));yield return AwaitEquipmentAction();
        OnlineRequire(OnlineMe!=null&&At(OnlineMe.board,3)!=null,"removal preserves unit slot");
        OnlineRequire(OnlineMe.inventory.SequenceEqual(new[]{15,16,5,12})&&At(OnlineMe.board,3).items.Length==0,"removal returns both items");
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
        OpenTeamPlan();yield return OnlineClick(900,108);
        OnlineRequire(!onlineReport&&selectedSlot==3,"planner blocks settlement banner click");showTeamPlan=false;
        yield return OnlineClick(900,108);
        OnlineRequire(onlineReport&&selectedSlot==-1&&onlineHistoricalFighter==null,"settlement banner opens completed report and clears placement selection");
        yield return new WaitForEndOfFrame();
        OnlineRequire(cosmeticReport==1&&cosmeticWinner>=0,"round settlement starts online finisher");
        OnlineRequire(cosmeticFinisher==(cosmeticWinner==0?OnlineMe.finisher:state.room.players[1-state.room.side].finisher),"winner selected effect is used");
        float settledEffectStarted=cosmeticEffectStarted;

        float damage=state.room.lastCombat.Sum(f=>f.damageDone);
        OnlineRequire(damage>0&&Mathf.Abs(damage-state.room.lastCombat.Sum(f=>f.damageTaken))<.1f,"reported damage matches received health loss");
        OnlineRequire(state.room.lastCombat.All(f=>Mathf.Abs(f.damageDone-f.basicDamageDone-f.skillDamageDone)<.1f),"online basic and skill totals reconcile");
        yield return SmokeRequest("/login",new Command{name="장비 테스트",key=key});
        OnlineRequire(state.room.reportRound==1&&Mathf.Abs(state.room.lastCombat.Sum(f=>f.damageDone)-damage)<.1f,"reconnect retains completed combat report");
        OnlineRequire(JsonUtility.ToJson(state.room.roundResult)==frozenSettlement,"reconnect retains frozen settlement");
        yield return new WaitForEndOfFrame();OnlineCapture("11-last-report");
        OnlineRequire(cosmeticEffectStarted==settledEffectStarted,"reconnect does not replay finisher");
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
        yield return OnlineClick(1340,317);
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
