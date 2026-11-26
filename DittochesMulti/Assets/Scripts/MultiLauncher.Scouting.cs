using System;
using System.Linq;
using UnityEngine;

public sealed partial class MultiLauncher
{
    int scoutSide=-1,scoutSlot=-1;
    string scoutRoom="",scoutArea="",scoutId="";
    bool OnlineScouting { get { return state!=null&&state.room!=null&&scoutSide>=0&&scoutSide<state.room.players.Length&&scoutSide!=state.room.side&&state.room.phase!="finished"; } }
    Player ScoutedPlayer { get { return OnlineScouting?state.room.players[scoutSide]:OnlineMe; } }
    Unit ScoutedUnit { get { var p=ScoutedPlayer;var unit=p==null||scoutSlot<0?null:At(scoutArea=="bench"?p.bench:p.board,scoutSlot);return unit!=null&&unit.id==scoutId?unit:null; } }
    void SetScoutedPlayer(int side)
    {
        var room=state==null?null:state.room;if(room==null)return;
        scoutSide=side>=0&&side<room.players.Length&&side!=room.side&&room.phase!="finished"?side:-1;
        scoutRoom=room.id;scoutSlot=-1;scoutArea=scoutId="";
        ClearOnlineUnitSelection();ResetEquipmentSelection();onlineEquipmentDrag.Reset();
        onlineSkillId="";onlineItemGuide=-1;showTraitGuide=false;onlineHistoricalFighter=null;
    }
    void ReconcileScouting()
    {
        var room=state.room;
        if(scoutRoom!=room.id||room.phase=="finished"||scoutSide>=room.players.Length)SetScoutedPlayer(-1);
        if(ScoutedUnit==null){scoutSlot=-1;scoutArea=scoutId="";}
    }
    void DrawScoutingNavigation()
    {
        var room=state.room;Event e=Event.current;
        if(GUI.enabled&&e.type==EventType.KeyDown)
        {
            if(e.keyCode==KeyCode.Space||e.keyCode==KeyCode.Escape&&OnlineScouting){SetScoutedPlayer(-1);e.Use();}
            else if(e.keyCode==KeyCode.Q||e.keyCode==KeyCode.R)
            {
                int current=OnlineScouting?scoutSide:room.side;
                SetScoutedPlayer((current+(e.keyCode==KeyCode.Q?-1:1)+room.players.Length)%room.players.Length);e.Use();
            }
        }
        float width=Mathf.Min(225,(room.players.Length>2?945f:670f)/Mathf.Max(1,room.players.Length));
        for(int i=0;i<room.players.Length;i++)
        {
            bool own=i==room.side,viewing=OnlineScouting?scoutSide==i:own;
            var p=room.players[i];Rect r=new Rect(310+i*width,96,width-6,32);
            string name=own?"내 필드":p.name.Length>5?p.name.Substring(0,5):p.name;
            if(onlineInterface.Button(r,new GUIContent((viewing?"● ":"")+name+" · "+(p.eliminated?p.placement+"위":p.hp.ToString()),own?"내 필드로 복귀 [SPACE]":p.name+" · 상대 필드 정찰 · 전장 / 대기석 / 장비 / 시너지"),room.phase!="finished",viewing,room.players.Length>2?11:13))SetScoutedPlayer(i);
            if(!p.eliminated)BattleHudTheme.Bar(new Rect(r.x+1,r.yMax-2,r.width-2,2),p.hp/100f,own?BattleHudTheme.Teal:BattleHudTheme.Gold);
        }
        if(room.players.Length<=2&&onlineInterface.Button(new Rect(1000,96,255,32),new GUIContent(OnlineScouting?"내 필드로 돌아가기 [SPACE]":"Q / R 상대 정찰 · SPACE 복귀"),true,OnlineScouting,13))SetScoutedPlayer(OnlineScouting?-1:OpponentSide(room));
    }
    void InspectScoutedUnit(Unit unit,string area)
    {scoutSlot=unit.slot;scoutArea=area;scoutId=unit.id;}
    void InspectOnlineFieldUnit(Room room,Player me)
    {
        var e=Event.current;if(!GUI.enabled||e.type!=EventType.MouseDown||e.button!=1||onlineEquipmentDrag.Slot>=0||!TacticalArena.MultiViewport.Contains(e.mousePosition))return;
        Unit unit=null;int side=room.side;string area="board";
        foreach(var u in me.bench.OrderByDescending(u=>u.slot))if(OnlinePieceRect(u.id,u.star,TacticalArena.BenchWorld(u.slot)).Contains(e.mousePosition)){unit=u;area="bench";break;}
        if(unit==null&&room.phase=="prepare")foreach(int player in new[]{room.side,OpponentSide(room)})
        {
            bool own=player==room.side;
            foreach(var u in room.players[player].board.OrderByDescending(u=>own?u.slot/7:3-u.slot/7))
            {
                Vector3 ground=TacticalArena.CellWorld(own?u.slot%7:6-u.slot%7,own?u.slot/7+4:3-u.slot/7);
                if(OnlinePieceRect(u.id,u.star,ground).Contains(e.mousePosition)){unit=u;side=player;break;}
            }
            if(unit!=null)break;
        }
        if(unit==null&&room.phase=="battle")
        {
            var frame=VisibleCombatFrame(room,Mathf.Max(0,room.remaining-(Time.unscaledTime-receivedAt)));
            if(frame!=null)foreach(var f in frame.units.Where(f=>f.hp>0).OrderByDescending(f=>OnlineCombatBody(f).yMax))
                if(OnlineCombatBody(f).Contains(e.mousePosition)){side=f.side;unit=At(room.players[side].board,f.slot);break;}
        }
        if(unit==null)return;
        ClearOnlineUnitSelection();ResetEquipmentSelection();
        if(side!=room.side){SetScoutedPlayer(side);InspectScoutedUnit(unit,area);}
        else{selectedArea=area;selectedSlot=unit.slot;onlineReport=false;}
        e.Use();
    }
    void DrawScoutedField(Room room,float remaining)
    {
        var p=ScoutedPlayer;Event e=Event.current;
        Unit selected=ScoutedUnit;
        if(e.type==EventType.Repaint)
        {
            arena.SetField(p.field);arena.BeginFrame(selected!=null&&scoutArea=="board"?selected.slot+28:-1,-1,selected!=null&&scoutArea=="bench"?selected.slot:-1,false);
            foreach(var unit in p.board)
            {
                Vector3 ground=TacticalArena.CellWorld(unit.slot%7,unit.slot/7+4);string key="scout:board:"+unit.slot;
                arena.SetActor(key,ground,LobbyPortrait(unit.id),new Color(1,.62f,.3f));arena.FaceActor(key,Vector3.forward);
                arena.PoseDigimon(key,unit.id,0,0,Time.unscaledTime,star:unit.star);
            }
            foreach(var unit in p.bench??new Unit[0])
            {
                string key="scout:bench:"+unit.slot;arena.SetActor(key,TacticalArena.BenchWorld(unit.slot),LobbyPortrait(unit.id),gold,.85f);
                arena.PoseDigimon(key,unit.id,0,0,Time.unscaledTime,star:unit.star);
            }
            if(selected!=null&&scoutArea=="board")arena.HighlightHexAttackRange(selected.slot%7,selected.slot/7+4,DigimonSkillCatalog.Find(selected.id).attackRange);
            DrawLiveTamer(room,scoutSide,false,"scout:tamer:"+scoutSide);
            arena.Render();
        }
        GUI.DrawTexture(TacticalArena.MultiViewport,arena.Texture,ScaleMode.StretchToFill,false);
        DrawOnlineArenaCaption(new Rect(330,143,920,28),p.name+"의 필드 · 레벨 "+p.level+" · 배치 "+p.board.Length+"명 · "+(room.phase=="battle"?"다음 배치 정찰 / 전투 진행 중":"준비 배치 정찰"));
        GUI.Label(new Rect(350,176,room.players.Length>2?635:880,24),room.players.Length>2?"유닛 클릭: 능력치 · 장비 · 시너지  /  Q · R 정찰 전환":"유닛 클릭: 능력치·장비·시너지   |   Q / R 필드 전환   |   SPACE 내 필드",small);
        // Inspect visible bodies before ground tiles, with nearer models taking precedence.
        if(GUI.enabled&&e.type==EventType.MouseDown&&(e.button==0||e.button==1)&&TacticalArena.MultiViewport.Contains(e.mousePosition)&&!(room.players.Length>2&&LeagueReturnRect.Contains(e.mousePosition)))
        {
            Unit hit=null;string area="board";
            foreach(var unit in (p.bench??new Unit[0]).OrderByDescending(u=>u.slot))
                if(OnlinePieceRect(unit.id,unit.star,TacticalArena.BenchWorld(unit.slot)).Contains(e.mousePosition)){hit=unit;area="bench";break;}
            if(hit==null)foreach(var unit in p.board.OrderByDescending(u=>u.slot))
                if(OnlinePieceRect(unit.id,unit.star,TacticalArena.CellWorld(unit.slot%7,unit.slot/7+4)).Contains(e.mousePosition)){hit=unit;break;}
            if(hit==null){int seat=arena.HitBench(e.mousePosition),cell=arena.HitCell(e.mousePosition);area=seat>=0?"bench":"board";hit=seat>=0?At(p.bench,seat):cell>=28?At(p.board,cell-28):null;}
            if(hit!=null)InspectScoutedUnit(hit,area);else{scoutSlot=-1;scoutId=scoutArea="";}e.Use();
        }
        foreach(var unit in p.board)
        {
            var ground=TacticalArena.CellWorld(unit.slot%7,unit.slot/7+4);
            GUI.Label(arena.LabelRect(ground,3),new string('★',unit.star),centered);DrawUnitItemBadges(unit,ground);
        }
        foreach(var unit in p.bench??new Unit[0])
        {var ground=TacticalArena.BenchWorld(unit.slot);GUI.Label(arena.LabelRect(ground,3),new string('★',unit.star),centered);DrawUnitItemBadges(unit,ground);}
        if(!p.publicBench)GUI.Label(new Rect(350,753,850,26),"상대 대기석 정보 미수신",centered);
        // Every pointer gesture in this field is inspection-only; no own-board action is routed here.
        if(GUI.enabled&&(e.type==EventType.MouseUp||e.type==EventType.MouseDrag)&&TacticalArena.MultiViewport.Contains(e.mousePosition)&&!(room.players.Length>2&&LeagueReturnRect.Contains(e.mousePosition)))e.Use();
    }
    void DrawScoutedDetails()
    {
        var p=ScoutedPlayer;var unit=ScoutedUnit;Card(new Rect(1305,290,270,638),surface,new Color(.65f,.4f,.18f));
        if(unit==null)
        {
            DrawScoutingComparison(p);return;
        }
        GUI.Label(new Rect(1320,302,198,30),Def(unit.id).name+" "+new string('★',unit.star),text);
        if(Btn(new Rect(1522,302,36,29),"×")){scoutSlot=-1;return;}
        Portrait(new Rect(1320,342,95,110),unit.id);
        GUI.Label(new Rect(1425,345,132,104),(scoutArea=="board"?"상대 전장":"상대 대기석")+"\n"+DigimonSkillCatalog.Find(unit.id).ScalingRole,small);
        var skill=DigimonSkillCatalog.Find(unit.id);var stats=OnlineStats(unit,"scout-"+scoutArea);
        BattleHudTheme.StatTile(new Rect(1320,464,116,63),"체력",stats.health.ToString("0"),BattleHudTheme.Teal);
        BattleHudTheme.StatTile(new Rect(1444,464,116,63),"공격력 / 주문력",stats.attack.ToString("0")+" / "+stats.abilityPower.ToString("0"));
        BattleHudTheme.StatTile(new Rect(1320,534,116,63),"방어 / 마법 저항",stats.armor.ToString("0")+" / "+stats.magicResist.ToString("0"));
        BattleHudTheme.StatTile(new Rect(1444,534,116,63),"공속 / 사거리",stats.speed.ToString("0.00")+" / "+stats.range);
        GUI.Label(new Rect(1320,618,240,25),"장착 장비 · 우클릭으로 조합 확인",small);
        int[] items=unit.items??new int[0];
        for(int i=0;i<items.Length;i++)
        {
            Rect r=new Rect(1320,653+i*42,240,36);int item=items[i];DigimonEquipmentArt.Draw(new Rect(r.x,r.y,34,34),item);
            GUI.Label(new Rect(r.x+45,r.y+4,190,28),DigimonBuildCatalog.Data.items[item].name,small);
            if(DigimonEquipmentUI.RightClick(r)||GUI.Button(r,GUIContent.none,GUIStyle.none))OpenOnlineEquipmentGuide(item);
        }
        if(items.Length==0)GUI.Label(new Rect(1320,651,240,32),"장착 장비 없음",small);
        if(DigimonSkillUI.DrawIcon(new Rect(1320,787,50,50),skill))OpenOnlineSkill(unit,"scout-"+scoutArea);
        GUI.Label(new Rect(1380,790,175,47),skill.name+"\n우클릭 → 스킬 정보",small);
        BattleHudTheme.Label(new Rect(1320,840,240,22),"마나 "+stats.startMana.ToString("0")+" / "+stats.maxMana.ToString("0"),13,BattleHudTheme.Teal);
        GUI.Label(new Rect(1320,864,240,52),scoutArea=="board"?"장비 + 상대 전장 시너지 적용\n전투 중에는 다음 배치 기준":"대기석 · 장비만 적용",small);
    }
}
