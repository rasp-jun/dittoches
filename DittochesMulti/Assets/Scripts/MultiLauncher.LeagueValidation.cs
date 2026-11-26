#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public sealed partial class MultiLauncher
{
    public void BeginLeagueSmoke(){StartCoroutine(LeagueSmoke());}
    IEnumerator LeagueSmoke()
    {
        onlineValidationPointer=Vector2.zero;yield return null;
        string[] args=Environment.GetCommandLineArgs();int argument=Array.IndexOf(args,"--test-server");
        OnlineRequire(argument>=0&&argument+1<args.Length,"explicit league test server");
        server=args[argument+1];OnlineRequire(new Uri(server).IsLoopback,"league test server is local");
        string key=Guid.NewGuid().ToString("N")+Guid.NewGuid().ToString("N");
        yield return SmokeRequest("/login",new Command{name="8인 UI 테스트",key=key});
        lobbyTab=0;yield return new WaitForEndOfFrame();OnlineCapture("league-home");
        lobbyTab=1;yield return new WaitForEndOfFrame();OnlineCapture("league-modes");
        var peers=new State[7];
        for(int i=0;i<7;i++)
        {
            smokePeer=null;
            yield return PeerRequest("/login",new Command{name="테이머 "+(i+1),key=Guid.NewGuid().ToString("N")+Guid.NewGuid().ToString("N")});
            yield return PeerRequest("/queue",new Command{mode="ranked8",tamer=i+4,field=i%3});
            peers[i]=smokePeer;
            OnlineRequire(smokePeer.room==null&&smokePeer.waiting==i+1,"eight-player queue waits for full roster");
        }
        lobbyLoadout.tamer=11;
        yield return SmokeRequest("/queue",new Command{mode="ranked8"});
        OnlineRequire(state.room.players.Length==8&&state.room.side==7,"eighth client has stable global side seven");
        for(int i=0;i<8;i++)OnlineRequire(state.room.players[i].tamer==i+4,"eight-player roster preserves each expanded tamer identity "+i);
        OnlineRequire(state.room.aliveCount==8&&OpponentSide(state.room)!=7,"league opponent and alive count deserialize");
        yield return ValidateLiveTamerSync(peers);
        for(int i=0;i<8;i++)OnlineRequire(i==7||state.room.players[i].shop.Length==0&&state.room.players[i].inventory.Length==0,"other economy hidden "+i);
        yield return new WaitForSeconds(.6f);yield return new WaitForEndOfFrame();OnlineCapture("league-prepare");
        onlineValidationPointer=new Vector2(1400,603);yield return null;yield return new WaitForEndOfFrame();
        OnlineRequire(onlineOwnTraitPanel.Focus=="fighter"&&!showTraitGuide,"trait hover reveals exact contribution without opening a modal");
        OnlineCapture("league-trait-hover");onlineValidationPointer=Vector2.zero;
        yield return OnlineClick(1400,571);
        OnlineRequire(showTraitGuide&&onlineTrait=="courage","inline trait click opens matching full guide");
        yield return new WaitForEndOfFrame();OnlineCapture("league-synergy");
        yield return OnlineKey(KeyCode.Escape);OnlineRequire(!showTraitGuide,"trait guide returns to HUD with Escape");
        for(int i=0;i<7;i++)
        {
            yield return OnlineClick(1316+(i%2)*126+60,128+(i/2)*35+15);
            OnlineRequire(OnlineScouting&&scoutSide==i,"roster click selects exact player "+i);
            yield return new WaitForEndOfFrame();
            OnlineRequire(arena.ActorCount>=state.room.players[i].board.Length,"scouted formation rendered "+i);
        }
        yield return new WaitForEndOfFrame();OnlineCapture("league-scout");
        yield return OnlineClick(LeagueReturnRect.center.x,LeagueReturnRect.center.y);
        OnlineRequire(!OnlineScouting,"return button consumes field gesture and restores own board");
        // Break the otherwise symmetric one-unit draw using normal player actions,
        // so the real server settlement exercises the routed finisher as well.
        yield return SmokeRequest("/action",new Command{action="buy",slot=0});
        OnlineRequire(OnlineMe.bench.Length==1,"league finisher review recruits a real second unit");
        yield return SmokeRequest("/action",new Command{action="move",area="bench",slot=OnlineMe.bench[0].slot,targetArea="board",targetSlot=10});
        OnlineRequire(OnlineMe.board.Length==2,"additional unit is deployed before the league fight");
        for(int i=0;i<7;i++){smokePeer=peers[i];yield return PeerRequest("/action",new Command{action="ready"});peers[i]=smokePeer;}
        yield return SmokeRequest("/action",new Command{action="ready"});
        OnlineRequire(state.room.phase=="battle"&&BattleSide(state.room)==1,"side seven uses duel orientation one");
        int opponent=OpponentSide(state.room);
        OnlineRequire(state.room.frames.SelectMany(f=>f.units).All(f=>f.side==7||f.side==opponent),"replay uses exact global participants");
        yield return SmokeRequest("/action",new Command{action="equip",area="board",slot=3,itemSlot=0,inventoryRevision=OnlineMe.inventoryRevision});
        OnlineRequire(OnlineMe.board.Any(u=>u.slot==3&&u.items.Length==1),"equipment action applies in eight-player battle");
        yield return new WaitForSeconds(1);yield return new WaitForEndOfFrame();OnlineCapture("league-battle");
        float until=Time.realtimeSinceStartup+65;
        while(state.room.phase=="battle"&&Time.realtimeSinceStartup<until)
        {
            for(int i=0;i<7;i++){smokePeer=peers[i];yield return PeerRequest("/state",new Command());peers[i]=smokePeer;}
            yield return SmokeRequest("/state");
        }
        OnlineRequire(state.room.phase=="prepare"&&state.room.round==2&&HasRoundResult(state.room),"four concurrent battles settle together");
        yield return new WaitForEndOfFrame();OnlineCapture("league-round-two");
        OnlineRequire(matchFinisher!=null&&matchFinisher.OpponentSide==opponent&&matchFinisher.RivalName==state.room.players[opponent].name,"real eight-player settlement targets the completed duel's opponent");
        OnlineRequire(matchFinisher.OpponentSide==state.room.reportOpponentSide,"server report and rendered tamer identity agree after pairing rotates");
        ValidateFinisherRouting();
        Vector3 targetBefore=TamerPosition(state.room,opponent,true);
        yield return PeerTamerMove(peers[opponent],10,6,4);
        MoveOnlineTamer(28);
        yield return new WaitForSeconds(.55f);
        OnlineRequire(matchFinisher.OpponentSide==opponent&&(TamerPosition(state.room,opponent,true)-targetBefore).sqrMagnitude>.001f,"completed duel identity stays bound while its tamer moves during the finisher");yield return new WaitForEndOfFrame();OnlineCapture("league-finisher-flight");
        yield return new WaitForSeconds(.85f);yield return new WaitForEndOfFrame();OnlineCapture("league-finisher-impact");
        for(int i=0;i<6;i++){smokePeer=peers[i];yield return PeerRequest("/leave",new Command());}
        yield return SmokeRequest("/state");
        OnlineRequire(state.room.aliveCount==2&&state.room.phase=="prepare","six forfeits leave two live contenders");
        yield return new WaitForEndOfFrame();OnlineCapture("league-final-two");
        smokePeer=peers[6];yield return PeerRequest("/leave",new Command());
        yield return SmokeRequest("/state");
        OnlineRequire(state.room.phase=="finished"&&state.room.placement==1,"last survivor is first place");
        OnlineRequire(state.room.players.Select(p=>p.placement).Distinct().Count()==8,"all eight places are unique");
        while(MatchFinisherActive)yield return null;
        yield return new WaitForEndOfFrame();OnlineCapture("league-result");
        yield return CaptureLeagueHudFixture();
        Debug.Log("LEAGUE SMOKE COMPLETE: "+onlineChecks+" checks");Application.Quit();
    }
    IEnumerator CaptureLeagueHudFixture()
    {
        // Offline, render-only late-game layout fixture. Never sent to the server.
        var actual=state;float received=receivedAt;
        state=DecodeState(JsonUtility.ToJson(actual));var room=state.room;var me=OnlineMe;
        room.phase="prepare";room.round=17;room.remaining=40;room.aliveCount=8;room.opponentSide=0;room.placement=0;room.frames=null;
        room.result="";room.message="";room.roundResult=null;room.lastCombat=null;room.reportRound=0;receivedAt=Time.unscaledTime;
        string[] ids={"wargreymon","metalgarurumon","agumon","gabumon","patamon","palmon","tentomon","piyomon"};
        for(int i=0;i<8;i++){var p=room.players[i];p.eliminated=false;p.placement=0;p.hp=24+i*9;p.ready=i%2==0;p.level=8;}
        me.gold=53;me.xp=20;me.level=8;me.ready=false;
        me.board=ids.Select((id,i)=>new Unit{id=id,slot=i<4?2+i:i+15,star=i<2?2:1,items=i<2?new[]{5,8}:new int[0]}).ToArray();
        me.bench=new[]{new Unit{id="agumon",star=1,slot=0,items=new int[0]},new Unit{id="agumon",star=1,slot=1,items=new int[0]},new Unit{id="togemon",star=1,slot=4,items=new int[0]}};
        me.shop=new[]{"agumon","greymon","angemon","garurumon","togemon"};
        OnlineRequire(me.board.Concat(me.bench).All(u=>Def(u.id)!=null)&&me.shop.All(id=>Def(id)!=null),"layout fixture only uses available roster units");
        var rival=room.players[0];rival.board=me.board.Take(6).Select(u=>new Unit{id=u.id,slot=u.slot,star=u.star,items=(int[])u.items.Clone()}).ToArray();
        rival.bench=new[]{new Unit{id="tentomon",star=1,slot=0,items=new int[0]}};rival.publicBench=true;
        SetScoutedPlayer(-1);onlineReport=false;recruitReceipt=null;
        yield return null;yield return new WaitForEndOfFrame();OnlineCapture("fixture-late-game-hud");
        int page=onlineOwnTraitPanel.Page;
        yield return OnlineClick(1543,770);
        OnlineRequire(onlineOwnTraitPanel.Page==page+1,"crowded synergy dashboard advances to next page");
        yield return new WaitForEndOfFrame();OnlineCapture("fixture-trait-page-two");
        SetScoutedPlayer(0);yield return null;yield return new WaitForEndOfFrame();OnlineCapture("fixture-scout-comparison");
        InspectScoutedUnit(rival.board[0],"board");yield return null;yield return new WaitForEndOfFrame();OnlineCapture("fixture-scout-unit");
        state=actual;receivedAt=received;SetScoutedPlayer(-1);
    }
}
#endif
