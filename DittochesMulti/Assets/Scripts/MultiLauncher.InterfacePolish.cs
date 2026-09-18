using System.Collections.Generic;
using UnityEngine;

public sealed partial class MultiLauncher
{
    readonly ArenaInterface onlineInterface=new ArenaInterface();
    readonly CombatLabelLayout onlineLabelLayout=new CombatLabelLayout();
    readonly List<CombatLabelLayout.Entry> onlineCombatLabels=new List<CombatLabelLayout.Entry>();
    void DrawOnlineEconomy(Player me,Room room,bool editable,bool fresh)
    {
        Card(new Rect(25,285,260,256),ArenaInterface.Ink,new Color(.22f,.35f,.36f));
        var heading=new GUIStyle(small){fontSize=17,fontStyle=FontStyle.Bold};heading.normal.textColor=new Color(.9f,.95f,.94f);
        GUI.Label(new Rect(40,299,110,27),"레벨 "+me.level,heading);
        GUI.Label(new Rect(151,303,119,24),me.level>=9?"MAX":me.xp+" / "+NativeGame.XpToNextLevel(me.level)+" XP",small);
        ArenaInterface.Fill(new Rect(40,337,230,5),new Color(.10f,.17f,.21f));
        ArenaInterface.Fill(new Rect(40,337,230*(me.level>=9?1:Mathf.Clamp01((float)me.xp/NativeGame.XpToNextLevel(me.level))),5),new Color(.33f,.64f,.88f));
        var oddsStyle=new GUIStyle(small){fontSize=12};
        for(int tier=1;tier<=5;tier++)
        {
            float x=40+(tier-1)*46;ArenaInterface.Fill(new Rect(x,355,3,13),ArenaInterface.Rarity(tier));
            GUI.Label(new Rect(x+6,349,41,28),new GUIContent(NativeGame.ShopChance(me.level,tier)+"%",tier+"골드 유닛 등장 확률"),oddsStyle);
        }
        bool canAct=editable&&!busy;
        if(onlineInterface.Button(new Rect(30,390,245,43),new GUIContent("경험치 +4   4G   [F]"),canAct&&me.gold>=4&&me.level<9))Send("/action",new Command{action="xp"});
        if(onlineInterface.Button(new Rect(30,440,245,43),new GUIContent("새로고침   2G   [D]"),canAct&&me.gold>=2))Send("/action",new Command{action="reroll"});
        Unit unit=SelectedOnlineUnit(me);
        string sale=unit==null?"판매할 유닛 선택":"선택 유닛 판매  ·  "+(Def(unit.id).cost*(int)Mathf.Pow(3,unit.star-1))+"G";
        if(onlineInterface.Button(new Rect(30,490,245,41),new GUIContent(sale),canAct&&unit!=null))
        {Send("/action",new Command{action="sell",area=selectedArea,slot=selectedSlot});selectedSlot=-1;selectedArea="";}
        if(onlineInterface.Button(new Rect(30,555,245,78),new GUIContent(me.ready?"준비 취소  [SPACE]":"전투 준비 완료  [SPACE]"),room.phase=="prepare"&&fresh&&!busy,true,16))Send("/action",new Command{action="ready"});
        Event e=Event.current;
        if(!GUI.enabled||busy||e.type!=EventType.KeyDown)return;
        string action=e.keyCode==KeyCode.F&&canAct&&me.gold>=4&&me.level<9?"xp":e.keyCode==KeyCode.D&&canAct&&me.gold>=2?"reroll":
            e.keyCode==KeyCode.Space&&room.phase=="prepare"&&fresh?"ready":null;
        if(action!=null){Send("/action",new Command{action=action});e.Use();}
    }
    void DrawRefinedCombat(Room room,float remaining)
    {
        if(room.frames==null||room.frames.Length==0)return;
        float progress=CombatProgress(room,remaining),time=CombatTime(room,remaining);
        int frame=Mathf.FloorToInt(progress),next=Mathf.Min(frame+1,room.frames.Length-1);
        onlineCombatLabels.Clear();
        foreach(var f in room.frames[frame].units)
        {
            if(f.hp<=0)continue;
            Fighter to=System.Array.Find(room.frames[next].units,u=>u.key==f.key)??f;
            float x=Mathf.Lerp(f.x,to.x,progress-frame),y=Mathf.Lerp(f.y,to.y,progress-frame);
            if(room.side==1){x=6-x;y=7-y;}
            bool focused=f.side==room.side&&selectedArea=="board"&&selectedSlot==f.slot;
            string caption=f.stun>0?"기절 "+f.stun.ToString("0.0")+"초":focused?(ActiveCastAge(room,f.key,time)>=0?DigimonSkillCatalog.Find(f.id).name:Def(f.id).name):"";
            float width=Mathf.Clamp(arena.CellRect(Mathf.Clamp(Mathf.RoundToInt(y),0,7),3).width*.78f,56,88);
            if(caption.Length>0)width=Mathf.Max(width,Mathf.Min(146,caption.Length*12+12));
            onlineCombatLabels.Add(new CombatLabelLayout.Entry{key=f,anchor=arena.Project(TacticalArena.CellWorld(x,y)+Vector3.up*TacticalArena.DigimonHeadHeight(f.id,f.star)),
                width=width,priority=focused,caption=caption.Length>0,text=caption});
        }
        onlineLabelLayout.Arrange(onlineCombatLabels,new Rect(312,205,956,524));
        foreach(var row in onlineCombatLabels)
        {
            var f=(Fighter)row.key;var to=System.Array.Find(room.frames[next].units,u=>u.key==f.key)??f;
            CombatLabelLayout.Draw(row,Mathf.Lerp(f.hp,to.hp,progress-frame),f.maxHp,Mathf.Lerp(f.mana,to.mana,progress-frame),f.maxMana,
                Mathf.Lerp(f.shield,to.shield,progress-frame),f.side!=room.side);
            if(f.side==room.side&&GUI.enabled&&Event.current.type==EventType.MouseDown&&Event.current.button==0&&
                (row.rect.Contains(Event.current.mousePosition)||new Rect(row.anchor.x-36,row.anchor.y,72,80).Contains(Event.current.mousePosition)))
            {selectedArea="board";selectedSlot=f.slot;onlineReport=false;Event.current.Use();}
        }
    }
}
