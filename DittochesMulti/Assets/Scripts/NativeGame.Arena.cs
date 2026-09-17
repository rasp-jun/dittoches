using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    private TacticalArena arena;
    private void EnsureArena() { if(arena==null)arena=new TacticalArena(TacticalArena.SoloViewport); }
    private void OnDisable() { if(arena!=null){arena.Dispose();arena=null;}dragSource=-1;draggingUnit=false; }
    private void OnDestroy() { if(arena!=null){arena.Dispose();arena=null;} }
    private Vector3 LegacyWorld(Vector2 p)
    {
        return new Vector3(Mathf.Lerp(-5,5,Mathf.InverseLerp(345,1290,p.x)),.23f,Mathf.Lerp(4.6f,-4.6f,Mathf.InverseLerp(175,665,p.y)));
    }
    private Vector2 LegacyPoint(Vector3 p)
    {
        return new Vector2(Mathf.Lerp(345,1290,Mathf.InverseLerp(-5,5,p.x)),Mathf.Lerp(175,665,Mathf.InverseLerp(4.6f,-4.6f,p.z)));
    }
    private Vector3 FighterWorld(Fighter fighter)
    {
        Vector3 p=TacticalArena.CellWorld(fighter.renderPos.x,fighter.renderPos.y);
        Fighter target=SelectTarget(fighter);
        if(target!=null&&fighter.attackFlash>0)
        {
            Vector3 direction=(TacticalArena.CellWorld(target.renderPos.x,target.renderPos.y)-p).normalized;
            p+=direction*Mathf.Sin((1-Mathf.Clamp01(fighter.attackFlash/.35f))*Mathf.PI)*.22f;
        }
        return p;
    }
    private void DrawPerspectiveBoard()
    {
        EnsureArena();
        bool scouting=!battling&&scoutedRival>=0, blocked=showCarousel||hp<=0;
        Unit[] visible=scouting?scoutBoard:board;
        if(Event.current.type==EventType.Repaint)
        {
            legendPos=Vector2.SmoothDamp(legendPos,legendTarget,ref legendVelocity,battling?.30f:.18f,battling?430f:720f,Time.deltaTime);
            int hover=blocked||scouting||battling?-1:arena.HitCell(Event.current.mousePosition);
            arena.BeginFrame(!scouting&&selectedBoard>=0?selectedBoard+28:-1,hover,selectedBench,!scouting&&!blocked&&(selectedBench>=0||selectedBoard>=0||draggingUnit));
            if(battling)
            {
                foreach(Fighter f in fighters)if(!f.dead)
                    arena.SetActor(f,FighterWorld(f),Tex(f.unit.def.id=="apocalymon"&&(f.attackFlash>0||f.skillFlash>0)?"Apocalymon_Attack":UnitSprite(f.unit.def)),f.enemy?new Color(1,.35f,.28f):new Color(.25f,1,.62f),1+f.skillFlash*.16f,f.hitFlash*2);
            }
            else for(int i=0;i<visible.Length;i++)if(visible[i]!=null)
                arena.SetActor(visible[i],TacticalArena.CellWorld(i%7,i/7+4),Tex(UnitSprite(visible[i].def)),scouting?new Color(1,.5f,.28f):new Color(.25f,1,.62f));
            for(int i=0;i<bench.Length;i++)if(bench[i]!=null)
                arena.SetActor(bench[i],TacticalArena.BenchWorld(i),Tex(UnitSprite(bench[i].def)),new Color(.95f,.74f,.28f),.85f);
            arena.SetActor(this,LegacyWorld(legendPos),Tex(LegendSprites[legend]),new Color(1,.8f,.32f),.83f);
            arena.Render();
        }
        GUI.DrawTexture(TacticalArena.SoloViewport,arena.Texture,ScaleMode.StretchToFill,false);
        string heading=battling?battleText:scouting?RivalNames[scoutedRival]+" / SCOUT":RoundType()+" / "+board.Count(u=>u!=null)+" / "+level;
        GUI.Label(new Rect(295,96,1040,26),heading,center);
        GUI.Label(new Rect(306,132,240,22),"FILE ISLAND / ARENA",small);
        GUI.Label(new Rect(1080,132,245,22),battling?Mathf.CeilToInt(battleTimeRemaining)+"s":"7 x 4  /  HEX FORMATION",small);
        if(!battling)
        {
            int hit=arena.HitCell(Event.current.mousePosition);
            for(int i=0;i<visible.Length;i++)
            {
                Vector3 point=TacticalArena.CellWorld(i%7,i/7+4);
                if(visible[i]!=null)GUI.Label(arena.LabelRect(point,3),new string('★',visible[i].star)+" "+UnitName(visible[i].def),center);
                if(!blocked&&hit==i+28&&Event.current.type==EventType.MouseUp&&Event.current.button==0)
                {
                    if(scouting){if(visible[i]!=null)inspectedUnit=visible[i];}
                    else ClickBoard(i);
                    Event.current.Use();
                }
            }
        }
        else
        {
            foreach(Fighter f in fighters)if(!f.dead)DrawFighter(f);
            DrawCombatPopups();
        }
        for(int i=lootOrbs.Count-1;i>=0;i--)
        {
            LootOrb orb=lootOrbs[i];Vector2 p=arena.Project(LegacyWorld(orb.pos)+Vector3.up*.25f);
            Rect r=new Rect(p.x-16,p.y-16,32,32);
            Color c=orb.rarity==2?new Color(1,.75f,.15f):orb.rarity==1?new Color(.25f,.65f,1):new Color(.65f,1,.65f);
            Color old=GUI.color;GUI.color=c;GUI.Label(r,"◆",title);GUI.color=old;
            if(!battling&&!blocked&&GUI.Button(r,GUIContent.none,GUIStyle.none))CollectOrb(orb);
        }
        UpdateLegendInput();
        GUI.Label(arena.LabelRect(LegacyWorld(legendPos),8),Legends[legend],center);
        if(!string.IsNullOrEmpty(lastCombatSummary))GUI.Label(new Rect(305,808,1020,20),lastCombatSummary,small);
        if(!battling&&Time.unscaledTime<resultNoticeUntil)
        {
            GUI.Box(new Rect(560,300,510,94),GUIContent.none,card);
            GUI.Label(new Rect(580,309,470,36),win?"전투 승리":"전투 패배",title);
            GUI.Label(new Rect(580,350,470,28),lastReward,center);
        }
        if(battling)
        {
            MiniBar(new Rect(490,803,650,4),battleProgress,accent);
            if(Time.unscaledTime-battleStartedAt<1.1f)GUI.Label(new Rect(565,300,510,60),"전투 시작",title);
        }
    }
    private void DrawPerspectiveFighter(Fighter f)
    {
        Vector3 point=FighterWorld(f);Vector2 head=arena.Project(point+Vector3.up*1.42f);
        float width=Mathf.Clamp(arena.CellRect(Mathf.Clamp(Mathf.RoundToInt(f.renderPos.y),0,7),3).width*.70f,42,80);
        MiniBar(new Rect(head.x-width/2,head.y,width,5),f.hp/f.maxHp,f.enemy?new Color(.94f,.28f,.22f):new Color(.3f,.93f,.48f));
        MiniBar(new Rect(head.x-width/2,head.y+6,width,3),f.mana/f.maxMana,new Color(.25f,.65f,1));
        if(f.shield>0)MiniBar(new Rect(head.x-width/2,head.y-3,width,2),f.shield/(f.maxHp*.5f),new Color(.35f,.8f,1));
        if(f.skillFlash>0)GUI.Label(new Rect(head.x-75,head.y-23,150,20),SkillName(f.unit.def),center);
        if(f.attackFlash>0&&Meta(f.unit.def.id).range>1)
        {
            Fighter target=SelectTarget(f);
            if(target!=null){Vector2 a=arena.Project(point+Vector3.up*.7f),b=arena.Project(FighterWorld(target)+Vector3.up*.7f);Vector2 p=Vector2.Lerp(a,b,1-Mathf.Clamp01(f.attackFlash/.35f));DrawRect(new Rect(p.x-4,p.y-4,8,8),RoleColor(f.unit.def.role));}
        }
        Vector2 feet=arena.Project(point);
        if(!showCarousel&&!f.enemy&&GUI.Button(new Rect(feet.x-width/2,head.y,width,Mathf.Max(20,feet.y-head.y)),GUIContent.none,GUIStyle.none))
        {if(selectedItem>=0)Equip(f.unit);else inspectedUnit=f.unit;}
    }
    private void DrawPerspectiveBench()
    {
        for(int i=0;i<9;i++)
        {
            Rect r=BenchRect(i);
            if(bench[i]!=null)GUI.Label(new Rect(r.x-10,r.yMax+3,r.width+20,20),new string('★',bench[i].star),center);
            else GUI.Label(new Rect(r.x,r.yMax+3,r.width,20),(i+1).ToString(),small);
            if(!showCarousel&&hp>0&&arena.HitBench(Event.current.mousePosition)==i&&Event.current.type==EventType.MouseUp&&Event.current.button==0){ClickBench(i);Event.current.Use();}
        }
    }
}
