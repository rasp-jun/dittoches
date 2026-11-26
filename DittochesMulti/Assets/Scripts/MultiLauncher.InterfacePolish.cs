using System.Collections.Generic;
using UnityEngine;

public sealed partial class MultiLauncher
{
    readonly ArenaInterface onlineInterface=new ArenaInterface();
    readonly CombatLabelLayout onlineLabelLayout=new CombatLabelLayout();
    readonly List<CombatLabelLayout.Entry> onlineCombatLabels=new List<CombatLabelLayout.Entry>();
    void DrawOnlineEconomy(Player me,Room room,bool editable,bool fresh)
    {
        BattleHudTheme.Surface(new Rect(25,285,260,266));
        BattleHudTheme.Label(new Rect(40,297,110,30),"레벨 "+me.level,23,null,true);
        BattleHudTheme.Label(new Rect(151,302,119,24),me.level>=9?"MAX":me.xp+" / "+NativeGame.XpToNextLevel(me.level)+" XP",13,BattleHudTheme.Muted,false,TextAnchor.MiddleRight);
        BattleHudTheme.Bar(new Rect(40,337,230,5),me.level>=9?1:Mathf.Clamp01((float)me.xp/NativeGame.XpToNextLevel(me.level)),BattleHudTheme.Teal);
        var oddsStyle=BattleHudTheme.Style(12);
        for(int tier=1;tier<=5;tier++)
        {
            float x=40+(tier-1)*46;ArenaInterface.Fill(new Rect(x,355,3,13),ArenaInterface.Rarity(tier));
            GUI.Label(new Rect(x+6,349,41,28),new GUIContent(NativeGame.ShopChance(me.level,tier)+"%",tier+"골드 유닛 등장 확률"+(me.level<9?"\n다음 레벨: "+NativeGame.ShopChance(me.level+1,tier)+"%":"\n최고 레벨")),oddsStyle);
        }
        bool canAct=editable&&!busy;
        BattleHudTheme.Label(new Rect(40,373,235,15),me.level>=9?"최고 레벨 달성":"다음 레벨까지  "+(Mathf.CeilToInt((NativeGame.XpToNextLevel(me.level)-me.xp)/4f)*4)+" G",11,BattleHudTheme.Muted);
        if(ArenaPresentationUI.CommandButton(new Rect(30,390,245,43),"경험치 구매","경험치 +4","F",canAct&&me.gold>=4&&me.level<9,false,"4 G",RecruitmentAdvice.Experience(me.level,me.xp,NativeGame.XpToNextLevel(me.level))+"\n"+RecruitmentAdvice.Spend(me.gold,4)))Send("/action",new Command{action="xp"});
        if(ArenaPresentationUI.CommandButton(new Rect(30,440,245,43),"새로고침","새로운 디지몬 모집","D",canAct&&me.gold>=2,false,"2 G",RecruitmentAdvice.Spend(me.gold,2)))Send("/action",new Command{action="reroll"});
        Unit unit=SelectedOnlineUnit(me);
        string sale=unit==null?"판매할 유닛 선택":(room.phase=="battle"?"대기석 판매 [E] · ":"선택 판매 [E] · ")+(Def(unit.id).cost*(int)Mathf.Pow(3,unit.star-1))+"G";
        BattleHudTheme.Label(new Rect(40,491,230,20),"상점으로 드래그 → 판매  [E]",12,BattleHudTheme.Muted);
        BattleHudTheme.FitLabel(new Rect(40,512,230,18),unit==null?"유닛을 집으면 판매 가격 표시":CanSellOnline(room)?sale:"전투 중인 전장 유닛 판매 불가",12,unit!=null?BattleHudTheme.Gold:BattleHudTheme.Muted,false);
        int vacancies=OnlineFormationLimit(me)-me.board.Length;
        BattleHudTheme.Label(new Rect(40,533,235,18),room.phase=="battle"?"장비 즉시 적용 · 대기석 관리 가능":vacancies>0?"빈 슬롯 "+vacancies+"칸 · 추가 배치 가능":"편성 완료 · 전투를 시작하세요",11,vacancies>0?BattleHudTheme.Gold:BattleHudTheme.Teal);
        string command=room.phase=="finished"?"경기 종료":me.eliminated?me.placement+"위 확정":room.phase=="battle"?"전투 진행 중":me.ready?"준비 취소":"전투 준비 완료";
        string commandHint=room.phase=="finished"?room.result:me.eliminated?"상대 편성 정찰 가능":room.phase=="battle"?"상점 · 장비 이용 가능":me.ready?"다른 테이머 준비 대기":"편성 "+me.board.Length+" / "+OnlineFormationLimit(me);
        if(ArenaPresentationUI.CommandButton(new Rect(30,555,245,78),command,commandHint,"ENTER",room.phase=="prepare"&&fresh&&!busy&&!me.eliminated,true,"",vacancies>0?"현재 "+me.board.Length+" / "+OnlineFormationLimit(me)+"명 배치 · 빈 슬롯을 확인하세요":"모든 전장 슬롯을 채웠습니다"))Send("/action",new Command{action="ready"});
        Event e=Event.current;
        if(!GUI.enabled||busy||e.type!=EventType.KeyDown)return;
        if(!OnlineScouting&&canAct&&(e.keyCode==KeyCode.W||e.keyCode==KeyCode.E)&&TacticalArena.MultiViewport.Contains(e.mousePosition))
        {
            string area,blocked;int slot;Fighter live;var hovered=OnlineEquipmentTarget(out area,out slot,out live,out blocked);
            if(blocked.Length>0||hovered==null){notice="조작할 내 유닛 위에 마우스를 올리세요 · W 배치/회수 · E 판매";e.Use();return;}
            if(room.phase=="battle"&&area=="board"){notice="전투 중인 전장 유닛은 이동·판매할 수 없습니다";e.Use();return;}
            if(hovered!=null){unit=hovered;selectedArea=area;selectedSlot=slot;}
        }
        string action=e.keyCode==KeyCode.F&&canAct&&me.gold>=4&&me.level<9?"xp":e.keyCode==KeyCode.D&&canAct&&me.gold>=2?"reroll":
            e.keyCode==KeyCode.Return&&room.phase=="prepare"&&fresh&&!me.eliminated?"ready":null;
        if(!OnlineScouting&&e.keyCode==KeyCode.W&&canAct)
        {
            if(room.phase!="prepare"){notice="전장 배치·회수는 준비 단계에서 가능합니다";e.Use();return;}
            if(unit==null){notice="유닛 위에 마우스를 올리고 W · 전장 배치 / 대기석 회수";e.Use();return;}
            ResetOnlineDrag();arenaPointer.Reset();ResetEquipmentSelection();onlineEquipmentDrag.Reset();
            bool onBoard=selectedArea=="board";int target=-1;int limit=onBoard?9:28;var occupied=onBoard?me.bench:me.board;
            for(int i=0;i<limit;i++)if(At(occupied,i)==null){target=i;break;}
            if(target>=0&&(onBoard||me.board.Length<OnlineFormationLimit(me)))
            {Send("/action",new Command{action="move",area=selectedArea,slot=selectedSlot,targetArea=onBoard?"bench":"board",targetSlot=target});ClearOnlineUnitSelection();}
            else notice=onBoard?"대기석이 가득 찼습니다 · 드래그로 다른 유닛과 교환하세요":"배치 한도 "+OnlineFormationLimit(me)+"명 · 드래그로 기존 유닛과 교환하거나 레벨을 올리세요";
            e.Use();return;
        }
        if(!OnlineScouting&&e.keyCode==KeyCode.E&&canAct&&unit!=null&&CanSellOnline(room))
        {Send("/action",new Command{action="sell",area=selectedArea,slot=selectedSlot});ClearOnlineUnitSelection();e.Use();return;}
        if(action!=null){Send("/action",new Command{action=action});e.Use();}
    }
    CombatSignals.Signal OnlineCombatSignal(Fighter f,float time)
    {return f.combatStatsVersion>=2?CombatSignals.Evaluate(time,f.lowShieldUsed,f.crisisAt,f.friendshipActive,f.hp>0,f.synergyKind,f.synergyAt,time):new CombatSignals.Signal();}
    void DrawRefinedCombat(Room room,float remaining)
    {
        if(room.frames==null||room.frames.Length==0)return;
        float progress=CombatProgress(room,remaining),time=CombatTime(room,remaining);
        int frame=Mathf.FloorToInt(progress),next=Mathf.Min(frame+1,room.frames.Length-1);
        onlineCombatLabels.Clear();
        foreach(var f in room.frames[frame].units)
        {
            if(f.hp<=0)continue;
            Fighter to=FindFighter(room.frames[next],f.key)??f;
            float x=Mathf.Lerp(f.x,to.x,progress-frame),y=Mathf.Lerp(f.y,to.y,progress-frame);
            if(BattleSide(room)==1){x=6-x;y=7-y;}
            bool focused=f.side==room.side&&selectedArea=="board"&&selectedSlot==f.slot;
            string caption=f.stun>0?"기절 "+f.stun.ToString("0.0")+"초":focused?(ActiveCastAge(room,f,time)>=0?DigimonSkillCatalog.Find(f.id).name:Def(f.id).name):"";
            var signal=OnlineCombatSignal(f,time);if(f.stun<=0&&signal.kind>0)caption=signal.Caption;
            float width=Mathf.Clamp(arena.CellRect(Mathf.Clamp(Mathf.RoundToInt(y),0,7),3).width*.78f,56,88);
            if(caption.Length>0)width=Mathf.Max(width,Mathf.Min(146,caption.Length*12+12));
            var row=onlineLabelLayout.Borrow(onlineCombatLabels.Count);
            row.key=f;row.anchor=arena.Project(TacticalArena.CellWorld(x,y)+Vector3.up*TacticalArena.DigimonHeadHeight(f.id,f.star));
            row.width=width;row.priority=focused||signal.kind>0;row.caption=caption.Length>0;row.text=caption;row.captionColor=signal.kind>0&&f.stun<=0?signal.Color:Color.clear;
            onlineCombatLabels.Add(row);
        }
        onlineLabelLayout.Arrange(onlineCombatLabels,new Rect(312,205,956,524));
        foreach(var row in onlineCombatLabels)
        {
            var f=(Fighter)row.key;var to=FindFighter(room.frames[next],f.key)??f;
            CombatLabelLayout.Draw(row,Mathf.Lerp(f.hp,to.hp,progress-frame),f.maxHp,Mathf.Lerp(f.mana,to.mana,progress-frame),f.maxMana,
                Mathf.Lerp(f.shield,to.shield,progress-frame),f.side!=room.side);
            if(f.side==room.side&&GUI.enabled&&Event.current.type==EventType.MouseDown&&Event.current.button==0&&
                (row.rect.Contains(Event.current.mousePosition)||new Rect(row.anchor.x-36,row.anchor.y,72,80).Contains(Event.current.mousePosition)))
            {if(!EquipOnlineSelection("board",f.slot,At(OnlineMe.board,f.slot))){selectedArea="board";selectedSlot=f.slot;onlineReport=false;}Event.current.Use();}
        }
    }
}
