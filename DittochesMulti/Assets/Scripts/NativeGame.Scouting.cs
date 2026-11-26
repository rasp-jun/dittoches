using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    readonly Dictionary<int,Unit[]> rivalFormations=new Dictionary<int,Unit[]>();
    int rivalFormationRound=-1,rivalFormationArt=-1;
    int NextRivalIndex { get { int stage=round<=3?1:2+(round-4)/7;return Mathf.Abs((round-4)*3+stage)%RivalNames.Length; } }
    Unit[] RivalFormation(int index)
    {
        if(rivalFormationRound!=round||rivalFormationArt!=artPack){rivalFormations.Clear();rivalFormationRound=round;rivalFormationArt=artPack;}
        Unit[] result;if(rivalFormations.TryGetValue(index,out result))return result;
        int stage=round<=3?1:2+(round-4)/7,count=Mathf.Clamp(stage+1,3,7),maxCost=Mathf.Clamp(stage,1,5);
        string[] roles={"전사","사수","마법사","지원","탱커","마법사","전사"};
        var rng=new System.Random(round*7919+index*397+artPack*53);
        var choices=ActiveRoster.Where(d=>d.cost<=maxCost).OrderByDescending(d=>d.role==roles[index]).ThenBy(_=>rng.Next()).ToArray();
        int[] columns={3,2,4,1,5,0,6},rows={1,1,1,2,2,2,2};result=new Unit[28];
        for(int i=0;i<count;i++)
        {
            var unit=new Unit(choices[i%choices.Length]);unit.star=stage>=5&&i<2?2:stage>=3&&i==0?2:1;
            if(stage>=4&&i<Mathf.Min(2,stage-3))unit.items.Add(rng.Next(0,4));
            result[(3-rows[i])*7+6-columns[i]]=unit;
        }
        rivalFormations[index]=result;return result;
    }
    void ReturnToOwnField()
    {
        scoutedRival=-1;ClearScoutInputs();
        if(arena!=null&&artPack==0)arena.SetField(tamerLoadout.field);
    }
    void ClearScoutInputs()
    {
        inspectedUnit=null;selectedBoard=selectedBench=selectedItem=-1;dragSource=-1;draggingUnit=false;
        equipmentDrag.Reset();arenaPointer.Reset();skillDetailUnit=null;showRecipeGuide=false;traitFocus=null;traitPage=0;
    }
    IEnumerable<DigimonBuildCatalog.Member> InspectedFormationMembers()
    {return scoutedRival>=0?scoutBoard.Where(u=>u!=null).Select(BuildMember):BoardBuildMembers();}
    void DrawSoloScoutingNavigation()
    {
        if(TrialsMode)
        {
            BattleHudTheme.Badge(new Rect(266,148,262,34),"아구몬박사의 시험",BattleHudTheme.Teal,15);
            BattleHudTheme.FitLabel(new Rect(553,150,battling&&CurrentTrial.boss?370:690,29),CurrentTrial.number.ToString("00")+" / 30  ·  "+CurrentTrial.name,16,BattleHudTheme.Text,true);
            if(battling&&CurrentTrial.boss)DrawTrialBossHealth(new Rect(951,149,295,33));
            BattleHudTheme.Label(new Rect(1268,150,354,29),"점수 "+TrialScore.ToString("N0")+"  ·  목숨 "+TrialLives+" / 3",15,BattleHudTheme.Gold,true,TextAnchor.MiddleRight);
            if(!battling&&!TrialFinished&&LastTrialRound!=null)DrawTrialLastResultStrip(new Rect(270,187,1354,28));
            return;
        }
        string viewing=scoutedRival>=0?RivalNames[scoutedRival]+" · AI 필드 정찰":"내 필드";
        string next=RoundType()=="크립"?"다음 상대: 악당 디지몬":RoundType()=="초밥집"?"보상 선택 라운드":"다음 상대: "+RivalNames[NextRivalIndex];
        BattleHudTheme.Badge(new Rect(266,148,192,34),scoutedRival>=0?RivalNames[scoutedRival]+" · 정찰 중":"나의 전장",scoutedRival>=0?BattleHudTheme.Gold:BattleHudTheme.Teal,15);
        BattleHudTheme.Label(new Rect(479,150,597,29),next,15,BattleHudTheme.Text,true);
        if(HudButton(new Rect(1120,149,82,31),"이전 [Q]"))ShowRivalBoard(scoutedRival<0?RivalNames.Length-1:scoutedRival-1);
        if(HudButton(new Rect(1208,149,82,31),"다음 [R]")){if(scoutedRival==RivalNames.Length-1)ReturnToOwnField();else ShowRivalBoard(scoutedRival+1);}
        if(HudButton(new Rect(1298,149,328,31),"내 필드로 돌아가기 [SPACE]",scoutedRival>=0))ReturnToOwnField();
    }
    void DrawSoloScoutedField()
    {
        if(rivalFormationRound!=round)ShowRivalBoard(scoutedRival);
        var e=Event.current;
        if(e.type==EventType.Repaint)
        {
            foreach(var stale in formationPositions.Keys.Where(u=>!scoutBoard.Contains(u)&&!board.Contains(u)&&!bench.Contains(u)).ToArray())formationPositions.Remove(stale);
            arena.SetField(scoutedRival%TamerLoadout.Fields.Length);arena.BeginFrame(-1,-1,-1,false);
            for(int i=0;i<scoutBoard.Length;i++)
            {
                var unit=scoutBoard[i];if(unit==null)continue;
                RenderFormationPiece(unit,TacticalArena.CellWorld(i%7,i/7+4),new Color(1,.62f,.3f));
                if(unit==inspectedUnit)arena.HighlightHexAttackRange(i%7,i/7+4,Meta(unit.def.id).range);
            }
            Vector3 tamer=TacticalArena.CellWorld(3,7);arena.SetTamer("scout-solo",scoutedRival%TamerLoadout.Tamers.Length,tamer,tamer,0,Time.unscaledTime,0);arena.Render();
        }
        GUI.DrawTexture(SoloArenaViewport,arena.Texture,ScaleMode.StretchToFill,false);
        GUI.Label(new Rect(270,117,800,25),"AI 상대의 실제 전투 편성 · "+scoutBoard.Count(u=>u!=null)+"명 · 대기석 없음",hudSmall);
        if(battling)GUI.Label(new Rect(270,193,900,26),"내 전투는 계속 진행 중 · "+Mathf.CeilToInt(battleTimeRemaining)+"초 · SPACE로 복귀",hudName);
        for(int i=0;i<scoutBoard.Length;i++)if(scoutBoard[i]!=null)
            DrawFormationLabel(scoutBoard[i],TacticalArena.CellWorld(i%7,i/7+4),scoutBoard[i]==inspectedUnit);
        if(GUI.enabled&&e.type==EventType.MouseDown&&(e.button==0||e.button==1)&&SoloArenaViewport.Contains(e.mousePosition)&&!new Rect(1115,145,515,40).Contains(e.mousePosition))
        {
            Unit picked=null;
            for(int i=scoutBoard.Length-1;i>=0;i--)if(scoutBoard[i]!=null&&FormationPieceRect(scoutBoard[i],TacticalArena.CellWorld(i%7,i/7+4),76).Contains(e.mousePosition)){picked=scoutBoard[i];break;}
            if(picked==null){int cell=arena.HitCell(e.mousePosition);if(cell>=28)picked=scoutBoard[cell-28];}
            inspectedUnit=picked;e.Use();
        }
        if(GUI.enabled&&(e.type==EventType.MouseUp||e.type==EventType.MouseDrag)&&SoloArenaViewport.Contains(e.mousePosition)&&!new Rect(1115,145,515,40).Contains(e.mousePosition))e.Use();
    }
    void HandleUnitInspection()
    {
        Event e=Event.current;if(!GUI.enabled||showCarousel||hp<=0||scoutedRival>=0||e.type!=EventType.MouseDown||e.button!=1||equipmentDrag.Slot>=0)return;
        Unit unit=null;
        if(battling)foreach(var f in fighters.Where(f=>!f.dead).OrderByDescending(f=>arena.Project(FighterWorld(f)).y))
            if(FormationPieceRect(f.unit,FighterWorld(f),76).Contains(e.mousePosition)){unit=f.unit;break;}
        int hit=PickFormationTarget(e.mousePosition);
        if(unit==null)unit=hit>=56?bench[hit-56]:!battling&&hit>=28?board[hit-28]:null;
        if(unit==null)return;
        inspectedUnit=unit;selectedItem=selectedBoard=selectedBench=-1;dragSource=-1;draggingUnit=false;arenaPointer.Reset();e.Use();
    }
    bool HandleScoutingHotkeys(Event e)
    {
        if(scoutedRival<0&&(e.keyCode==KeyCode.W||e.keyCode==KeyCode.E)&&SoloArenaViewport.Contains(e.mousePosition))
        {
            string blocked;var hovered=EquipmentHoverTarget(out blocked);
            if(blocked.Length>0||hovered==null){NotifyPlacement("조작할 내 유닛 위에 마우스를 올리세요 · W 배치/회수 · E 판매");e.Use();return true;}
            int seat=Array.IndexOf(bench,hovered),cell=Array.IndexOf(board,hovered);
            if(battling&&seat<0){NotifyPlacement("전투 중인 전장 유닛은 이동·판매할 수 없습니다");e.Use();return true;}
            if(hovered!=null&&seat>=0){selectedBench=seat;selectedBoard=-1;}
            else if(hovered!=null&&cell>=0&&!battling){selectedBoard=cell;selectedBench=-1;}
        }
        if(TrialsMode&&(e.keyCode==KeyCode.Q||e.keyCode==KeyCode.R)){e.Use();return true;}
        if(e.keyCode==KeyCode.Space){ReturnToOwnField();e.Use();return true;}
        if(e.keyCode==KeyCode.Q){if(scoutedRival==0)ReturnToOwnField();else ShowRivalBoard(scoutedRival<0?RivalNames.Length-1:scoutedRival-1);e.Use();return true;}
        if(e.keyCode==KeyCode.R){if(scoutedRival==RivalNames.Length-1)ReturnToOwnField();else ShowRivalBoard(scoutedRival+1);e.Use();return true;}
        if(e.keyCode==KeyCode.Escape&&scoutedRival>=0){ReturnToOwnField();e.Use();return true;}
        if(e.keyCode==KeyCode.W&&scoutedRival<0)
        {
            if(battling){NotifyPlacement("전장 배치·회수는 준비 단계에서 가능합니다");e.Use();return true;}
            dragSource=-1;draggingUnit=false;arenaPointer.Reset();equipmentDrag.Reset();selectedItem=-1;
            if(selectedBoard>=0)
            {int seat=Array.FindIndex(bench,u=>u==null);if(seat>=0){ClickBench(seat);NotifyPlacement("대기석으로 회수했습니다");}else NotifyPlacement("대기석이 가득 찼습니다 · 드래그로 다른 유닛과 교환하세요");}
            else if(selectedBench>=0)
            {
                if(board.Count(u=>u!=null)>=FormationLimit)NotifyPlacement("배치 한도 "+FormationLimit+"명 · 드래그로 기존 유닛과 교환하거나 레벨을 올리세요");
                else {int cell=Array.FindIndex(board,u=>u==null);if(cell>=0){ClickBoard(cell);NotifyPlacement("전장에 배치했습니다");}}
            }
            else NotifyPlacement("유닛 위에 마우스를 올리고 W · 전장 배치 / 대기석 회수");
            e.Use();return true;
        }
        return false;
    }
}
