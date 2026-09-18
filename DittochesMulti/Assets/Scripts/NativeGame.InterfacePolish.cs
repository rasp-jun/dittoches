using System.Collections.Generic;
using UnityEngine;

public sealed partial class NativeGame
{
    readonly CombatLabelLayout combatLabelLayout=new CombatLabelLayout();
    readonly List<CombatLabelLayout.Entry> combatLabels=new List<CombatLabelLayout.Entry>();
    readonly Dictionary<Fighter,CombatLabelLayout.Entry> fighterLabels=new Dictionary<Fighter,CombatLabelLayout.Entry>();
    void PrepareCombatLabels()
    {
        combatLabels.Clear();fighterLabels.Clear();
        foreach(var f in fighters)
        {
            if(f.dead)continue;
            bool focused=inspectedUnit==f.unit;
            string caption=f.stun>0?"기절 "+f.stun.ToString("0.0")+"초":focused?(f.skillCast!=null?SkillName(f.unit.def):UnitName(f.unit.def)):"";
            float width=Mathf.Clamp(arena.CellRect(Mathf.Clamp(Mathf.RoundToInt(f.renderPos.y),0,7),3).width*.78f,56,88);
            if(caption.Length>0)width=Mathf.Max(width,Mathf.Min(146,caption.Length*12+12));
            var row=new CombatLabelLayout.Entry{key=f,anchor=arena.Project(FighterWorld(f)+Vector3.up*TacticalArena.DigimonHeadHeight(f.unit.def.id,f.unit.star)),
                width=width,priority=focused,caption=caption.Length>0,text=caption};
            combatLabels.Add(row);fighterLabels.Add(f,row);
        }
        combatLabelLayout.Arrange(combatLabels,new Rect(SoloArenaViewport.x+12,SoloArenaViewport.y+85,SoloArenaViewport.width-24,SoloArenaViewport.height-130));
    }
    void DrawRefinedFighter(Fighter f)
    {
        CombatLabelLayout.Entry row;if(!fighterLabels.TryGetValue(f,out row))return;
        float health=Mathf.Clamp01(f.hp/Mathf.Max(1,f.maxHp)),trail;
        if(!healthTrails.TryGetValue(f,out trail))trail=health;
        if(Event.current.type==EventType.Repaint){trail=health>=trail?health:Mathf.MoveTowards(trail,health,Time.unscaledDeltaTime*.65f);healthTrails[f]=trail;}
        CombatLabelLayout.Draw(row,f.hp,f.maxHp,f.mana,f.maxMana,f.shield,f.enemy,trail);
        Vector2 feet=arena.Project(FighterWorld(f));
        Rect portrait=new Rect(feet.x-36,row.anchor.y,72,Mathf.Max(20,feet.y-row.anchor.y));
        bool inspect=GUI.Button(row.rect,GUIContent.none,GUIStyle.none)|GUI.Button(portrait,GUIContent.none,GUIStyle.none);
        if(!showCarousel&&inspect)
        {if(selectedItem>=0){if(f.enemy)NotifyPlacement("상대 유닛에는 장비를 장착할 수 없습니다");else Equip(f.unit);}else inspectedUnit=f.unit;}
    }
}
