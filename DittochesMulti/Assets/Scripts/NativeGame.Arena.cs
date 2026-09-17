using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    private TacticalArena arena;
    private int arenaArtPack=-1;
    private readonly ArenaPointer arenaPointer=new ArenaPointer();
    private float legendCelebrateUntil;
    private Color LegendColor(){return legend==0?new Color(.25f,.85f,1f):legend==1?new Color(1f,.55f,.75f):legend==2?new Color(1f,.82f,.4f):new Color(.68f,.65f,1f);}
    private Rect LootRect(LootOrb orb)
    {
        Vector2 p=arena.Project(LegacyWorld(orb.pos)+Vector3.up*(.25f+Mathf.Sin(Time.unscaledTime*3f+orb.pos.x*.013f)*.07f));
        return new Rect(p.x-20,p.y-20,40,40);
    }
    private void HandleArenaPointer()
    {
        Event e=Event.current;Vector2 point=e.mousePosition;
        int target=PickFormationTarget(point);
        if(!battling)for(int i=lootOrbs.Count-1;i>=0;i--)if(LootRect(lootOrbs[i]).Contains(point)){target=100+i;break;}
        bool down=e.type==EventType.MouseDown&&e.button==0,up=e.type==EventType.MouseUp&&e.button==0;
        bool guide=showRecipeGuide&&Time.unscaledTime<recipeGuideUntil&&new Rect(270,470,470,recipeFocus<=3?270:150).Contains(point);
        guide|=traitFocus!=null&&Time.unscaledTime<traitGuideUntil&&new Rect(270,145,475,155).Contains(point);
        arenaPointer.Update(target,down,up,draggingUnit,showCarousel||hp<=0||guide);
        if(arenaPointer.Released>=100)
        {
            int index=arenaPointer.Released-100;
            if(index<lootOrbs.Count){legendCelebrateUntil=Time.unscaledTime+1.4f;CollectOrb(lootOrbs[index]);}
            e.Use();
        }
        else if(down&&target>=100&&!showCarousel&&hp>0&&!guide)e.Use();
    }
    private void EnsureArena()
    {
        if(arena!=null&&arenaArtPack!=artPack){arena.Dispose();arena=null;}
        if(arena==null){arena=new TacticalArena(SoloArenaViewport,artPack==0);arenaArtPack=artPack;}
    }
    private void OnDisable() { if(arena!=null){arena.Dispose();arena=null;}dragSource=-1;draggingUnit=false;arenaPointer.Reset();formationPositions.Clear();healthTrails.Clear();promotions.Clear();battleTraces.Clear();skillCasts.Clear(); }
    private void OnApplicationFocus(bool focused){if(!focused){dragSource=-1;draggingUnit=false;arenaPointer.Reset();}}
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
        if(fighter.attackFlash>0&&fighter.skillCast==null&&!(artPack==0&&DigimonModelLibrary.HasModel(fighter.unit.def.id)))
        {
            Vector3 direction=(TacticalArena.CellWorld(fighter.attackTarget.x,fighter.attackTarget.y)-p).normalized;
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
            int hover=blocked||scouting||battling?-1:PickFormationTarget(Event.current.mousePosition);
            if(hover>=56)hover=-1;
            int sourceCell=draggingUnit?(dragFromBoard?dragSource+28:-1):selectedBoard>=0?selectedBoard+28:-1;
            int sourceBench=draggingUnit?(!dragFromBoard?dragSource:-1):selectedBench;
            arena.BeginFrame(!scouting?sourceCell:-1,hover,sourceBench,!scouting&&!blocked&&(selectedBench>=0||selectedBoard>=0||draggingUnit));
            if(!battling&&!scouting&&!blocked&&HeldUnit()!=null)
            {
                int rangeCell=draggingUnit?arena.HitCell(Event.current.mousePosition):sourceCell;
                if(rangeCell>=28)arena.HighlightAttackRange(rangeCell,Mathf.Lerp(1.05f,2.9f,(Meta(HeldUnit().def.id).range-1)/3f));
            }
            if(!battling&&!scouting&&!blocked&&HeldUnit()!=null&&!draggingUnit)
            {
                int cell=arena.HitCell(Event.current.mousePosition),seat=arena.HitBench(Event.current.mousePosition);
                arena.HighlightDestination(cell,seat,seat>=0||ValidBoardDestination(cell));
            }
            if(battling)
            {
                foreach(Fighter f in fighters)if(!f.dead||Time.unscaledTime-f.diedAt<.45f)
                {
                    float death=f.dead?Mathf.Clamp01((Time.unscaledTime-f.diedAt)/.45f):0;
                    bool skeletal=artPack==0&&DigimonModelLibrary.HasModel(f.unit.def.id);
                    arena.SetActor(f,FighterWorld(f)-Vector3.up*(skeletal?0:death*.15f),Tex(f.unit.def.id=="apocalymon"&&(f.attackFlash>0||f.skillFlash>0)?"Apocalymon_Attack":UnitSprite(f.unit.def)),f.enemy?new Color(1,.35f,.28f):new Color(.25f,1,.62f),(1+(artPack==0?0:f.skillFlash*.16f))*(skeletal?1:1-death*.88f),f.hitFlash*2);
                    if(skeletal)
                    {
                        Vector3 facing=f.renderVelocity.sqrMagnitude>.01f?new Vector3(f.renderVelocity.x,0,-f.renderVelocity.y):
                            f.target!=null?TacticalArena.CellWorld(f.target.renderPos.x,f.target.renderPos.y)-FighterWorld(f):new Vector3(0,0,f.enemy?-1:1);
                        arena.FaceActor(f,facing);
                    }
                    bool gearTarget=!f.dead&&!f.enemy&&selectedItem>=0;
                    arena.DecorateActor(f,!f.dead&&inspectedUnit==f.unit||gearTarget,0,f.hitFlash/.18f,f.healFlash/.28f,f.shieldFlash/.35f,gearTarget&&!CanEquipSelected(f.unit));
                    if(artPack==0)arena.PoseDigimon(f,f.unit.def.id,f.renderVelocity.magnitude,f.attackTarget.x-f.renderPos.x,Time.unscaledTime,
                        f.skillCast!=null?f.skillCast.age:-1,f.attackFlash,death,f.unit.star);
                    else arena.PoseCombatActor(f,f.renderVelocity.magnitude,f.renderVelocity.x,Time.unscaledTime,death);
                }
            }
            else for(int i=0;i<visible.Length;i++)if(visible[i]!=null)
                RenderFormationPiece(visible[i],TacticalArena.CellWorld(i%7,i/7+4),scouting?new Color(1,.5f,.28f):new Color(.25f,1,.62f));
            for(int i=0;i<bench.Length;i++)if(bench[i]!=null)
                RenderFormationPiece(bench[i],TacticalArena.BenchWorld(i),new Color(.95f,.74f,.28f),.85f);
            foreach(Unit stale in formationPositions.Keys.Where(u=>!visible.Contains(u)&&!bench.Contains(u)).ToArray())formationPositions.Remove(stale);
            foreach(Unit stale in promotions.Keys.Where(u=>promotions[u]<=Time.unscaledTime||(!board.Contains(u)&&!bench.Contains(u))).ToArray())promotions.Remove(stale);
            float celebration=Mathf.Clamp01((legendCelebrateUntil-Time.unscaledTime)/.8f);
            if(win&&!battling&&Time.unscaledTime<resultNoticeUntil)celebration=Mathf.Max(celebration,.65f);
            arena.SetTactician(this,LegacyWorld(legendPos),LegacyWorld(legendTarget),Tex(LegendSprites[legend]),LegendColor(),
                legendVelocity.magnitude/550f,legendVelocity.x,Time.unscaledTime,celebration);
            if(battling&&artPack==0)DrawDigimonSkills();
            arena.Render();
        }
        GUI.DrawTexture(SoloArenaViewport,arena.Texture,ScaleMode.StretchToFill,false);
        string heading=battling?battleText:scouting?RivalNames[scoutedRival]+" / SCOUT":RoundType()+" / "+board.Count(u=>u!=null)+" / "+level;
        if(artPack!=0)GUI.Label(new Rect(295,96,1040,26),heading,center);
        GUI.Label(new Rect(SoloArenaViewport.x+20,SoloArenaViewport.y+12,300,22),scouting?heading:"FILE ISLAND / ARENA",small);
        GUI.Label(new Rect(SoloArenaViewport.xMax-260,SoloArenaViewport.y+12,245,22),battling?Mathf.CeilToInt(battleTimeRemaining)+"s":"7 x 4  /  HEX FORMATION",small);
        GUI.Label(new Rect(550,132,500,22),battling?"아군 "+fighters.Count(f=>!f.enemy&&!f.dead)+" / "+fighters.Count(f=>!f.enemy)+"   ·   상대 "+fighters.Count(f=>f.enemy&&!f.dead)+" / "+fighters.Count(f=>f.enemy):"",center);
        if(!battling)
        {
            int hit=PickFormationTarget(Event.current.mousePosition);
            for(int i=0;i<visible.Length;i++)
            {
                Vector3 point=TacticalArena.CellWorld(i%7,i/7+4);
                if(visible[i]!=null&&formationPositions.ContainsKey(visible[i]))point=formationPositions[visible[i]];
                if(visible[i]!=null&&!(draggingUnit&&visible[i]==HeldUnit()))
                {
                    bool focused=selectedBoard==i||inspectedUnit==visible[i]||hit==i+28;
                    DrawFormationLabel(visible[i],point,focused);
                }
                if(!blocked&&arenaPointer.Released==i+28&&Event.current.type==EventType.MouseUp&&Event.current.button==0)
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
            DrawAttackTraces();
            DrawCombatPopups();
        }
        for(int i=lootOrbs.Count-1;i>=0;i--)
        {
            LootOrb orb=lootOrbs[i];Vector2 p=arena.Project(LegacyWorld(orb.pos)+Vector3.up*(.25f+Mathf.Sin(Time.unscaledTime*3f+orb.pos.x*.013f)*.07f));
            Rect r=new Rect(p.x-20,p.y-20,40,40);
            Color c=orb.rarity==2?new Color(1,.75f,.15f):orb.rarity==1?new Color(.25f,.65f,1):new Color(.65f,1,.65f);
            Color old=GUI.color;GUI.color=c;GUI.Label(r,"◆",title);GUI.color=old;

        }
        DrawFormationStatus();
        UpdateLegendInput();
        Rect nameplate=arena.LabelRect(LegacyWorld(legendPos),12);
        DrawRect(new Rect(nameplate.x-6,nameplate.y-1,nameplate.width+12,23),new Color(.018f,.035f,.055f,.88f));
        DrawRect(new Rect(nameplate.x-6,nameplate.y+21,nameplate.width+12,2),LegendColor());
        GUI.Label(nameplate,Legends[legend],center);
        if(artPack!=0&&!string.IsNullOrEmpty(lastCombatSummary))GUI.Label(new Rect(305,808,1020,20),lastCombatSummary,small);
        if(!battling&&Time.unscaledTime<resultNoticeUntil)
        {
            GUI.Box(new Rect(560,198,510,74),GUIContent.none,card);
            GUI.Label(new Rect(580,201,470,36),win?"전투 승리":"전투 패배",title);
            GUI.Label(new Rect(580,239,470,28),lastReward,center);
        }
        if(battling)
        {
            MiniBar(new Rect(SoloArenaViewport.center.x-325,artPack==0?838:803,650,4),battleProgress,accent);
            if(Time.unscaledTime-battleStartedAt<1.1f)GUI.Label(new Rect(565,300,510,60),"전투 시작",title);
        }
    }
    private void DrawPerspectiveFighter(Fighter f)
    {
        Vector3 point=FighterWorld(f);Vector2 head=arena.Project(point+Vector3.up*(artPack==0?TacticalArena.DigimonHeadHeight(f.unit.def.id,f.unit.star):1.42f));
        float width=Mathf.Clamp(arena.CellRect(Mathf.Clamp(Mathf.RoundToInt(f.renderPos.y),0,7),3).width*.78f,52,88);
        float health=Mathf.Clamp01(f.hp/Mathf.Max(1,f.maxHp));
        float trail;if(!healthTrails.TryGetValue(f,out trail))trail=health;
        if(Event.current.type==EventType.Repaint)
        {
            trail=health>=trail?health:Mathf.MoveTowards(trail,health,Time.unscaledDeltaTime*.65f);
            healthTrails[f]=trail;
        }
        Rect hpBar=new Rect(head.x-width/2,head.y,width,7);
        DrawRect(new Rect(hpBar.x-2,hpBar.y-2,width+4,17),new Color(.008f,.015f,.025f,.95f));
        DrawRect(hpBar,new Color(.12f,.16f,.2f));
        DrawRect(new Rect(hpBar.x,hpBar.y,width*trail,7),new Color(1f,.8f,.45f));
        DrawRect(new Rect(hpBar.x,hpBar.y,width*health,7),f.enemy?new Color(.96f,.3f,.27f):new Color(.32f,.94f,.57f));
        int segments=Mathf.Clamp(Mathf.CeilToInt(f.maxHp/250f),1,12);
        for(int i=1;i<segments;i++)DrawRect(new Rect(hpBar.x+width*i/segments,hpBar.y,1,7),new Color(.015f,.025f,.03f,.7f));
        MiniBar(new Rect(head.x-width/2,head.y+9,width,3),f.mana/Mathf.Max(1,f.maxMana),new Color(.25f,.65f,1));
        if(f.shield>0)MiniBar(new Rect(head.x-width/2,head.y-5,width,2),f.shield/Mathf.Max(1,f.maxHp*.5f),new Color(.6f,.88f,1));
        if(f.stun>0)GUI.Label(new Rect(head.x-42,head.y-25,84,20),"기절",center);
        else if(f.skillFlash>0)GUI.Label(new Rect(head.x-95,head.y-25,190,20),SkillName(f.unit.def),center);
        Vector2 feet=arena.Project(point);
        if(!showCarousel&&GUI.Button(new Rect(feet.x-width/2,head.y,width,Mathf.Max(20,feet.y-head.y)),GUIContent.none,GUIStyle.none))
        {if(selectedItem>=0){if(f.enemy)NotifyPlacement("상대 유닛에는 장비를 장착할 수 없습니다");else Equip(f.unit);}else inspectedUnit=f.unit;}
    }
    private void DrawPerspectiveBench()
    {
        for(int i=0;i<9;i++)
        {
            Rect r=BenchRect(i);
            if(bench[i]!=null)
            {
                Vector3 ground; if(!formationPositions.TryGetValue(bench[i],out ground))ground=TacticalArena.BenchWorld(i);
                bool focused=selectedBench==i||PickFormationTarget(Event.current.mousePosition)==56+i;
                if(!(draggingUnit&&bench[i]==HeldUnit()))DrawFormationLabel(bench[i],ground,focused);
            }
            else GUI.Label(new Rect(r.x,r.yMax+3,r.width,20),(i+1).ToString(),small);
            if(!showCarousel&&hp>0&&arenaPointer.Released==56+i&&Event.current.type==EventType.MouseUp&&Event.current.button==0){ClickBench(i);Event.current.Use();}
        }
    }
}
