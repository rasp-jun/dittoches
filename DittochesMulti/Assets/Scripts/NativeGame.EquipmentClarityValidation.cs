#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public static partial class DigimonTraitUI
{
    internal static Vector2 ReviewMemberScroll { get {return memberScroll;} set {memberScroll=value;} }
    internal static bool ReviewWheelConsumed { get; private set; }
    internal static float ReviewMemberOverflow { get; private set; }
    static float reviewWheelDelta;
    static int reviewWheelTraceUntil;
    static bool reviewWheelInside=true,reviewWheelEnabled=true,reviewPreviousEnabled;
    internal static void ReviewQueueWheel(float delta,bool inside=true,bool enabled=true)
    {reviewWheelDelta=delta;reviewWheelInside=inside;reviewWheelEnabled=enabled;ReviewWheelConsumed=false;reviewWheelTraceUntil=delta==0?0:Time.frameCount+4;}
    static void ReviewTraceWheel(string stage,Rect viewport,float contentHeight)
    {
        if(Time.frameCount>reviewWheelTraceUntil||reviewWheelTraceUntil==0)return;
        var e=Event.current;var m=GUI.matrix;
        Debug.Log("CLARITY WHEEL "+stage+" frame="+Time.frameCount+" event="+e.type+"/"+e.rawType+" mouse="+e.mousePosition+" delta="+e.delta+" contains="+viewport.Contains(e.mousePosition)+" enabled="+GUI.enabled+" matrix="+m.m00+","+m.m11+","+m.m03+","+m.m13+" viewport="+viewport+" content="+contentHeight+" scroll="+memberScroll+" hot="+GUIUtility.hotControl+" pending="+reviewWheelDelta+" consumed="+ReviewWheelConsumed);
    }
    static Event ReviewBeginWheel(Rect viewport,float contentHeight)
    {
        ReviewMemberOverflow=Mathf.Max(0,contentHeight-viewport.height);
        ReviewTraceWheel("begin-before",viewport,contentHeight);
        if(reviewWheelDelta==0||Event.current.type!=EventType.Repaint)return null;
        var previous=new Event(Event.current);
        reviewPreviousEnabled=GUI.enabled;GUI.enabled=reviewPreviousEnabled&&reviewWheelEnabled;
        Event.current=new Event{type=EventType.ScrollWheel,mousePosition=reviewWheelInside?viewport.center:viewport.min-Vector2.one*20,delta=new Vector2(0,reviewWheelDelta)};
        reviewWheelDelta=0;ReviewTraceWheel("begin-injected",viewport,contentHeight);return previous;
    }
    static void ReviewEndWheel(Event previous,Rect viewport,float contentHeight)
    {
        ReviewTraceWheel("end-before",viewport,contentHeight);
        if(previous==null)return;
        ReviewWheelConsumed=Event.current.type==EventType.Used;Event.current=previous;GUI.enabled=reviewPreviousEnabled;
        ReviewTraceWheel("end-restored",viewport,contentHeight);
    }
}

public sealed partial class NativeGame
{
    public void BeginEquipmentClaritySmoke(){StartCoroutine(EquipmentClaritySmoke());}
    IEnumerator EquipmentClaritySmoke()
    {
        Application.runInBackground=true;yield return null;
        artPack=0;lobby=false;round=12;level=8;gold=40;hp=100;showCombatReport=false;showTeamPlan=false;
        selectedBoard=-1;selectedBench=-1;selectedItem=-1;validationPointer=null;
        Array.Clear(board,0,board.Length);Array.Clear(bench,0,bench.Length);inventory.Clear();fighters.Clear();
        RebuildPool();RollShop();EnsureArena();ValidateEquipmentClarity();
        yield return CaptureEquipmentClarityReview();
        Debug.Log("CLARITY SMOKE COMPLETE checks="+validationChecks);Application.Quit(0);
    }
    void ValidateEquipmentClarity()
    {
        var oldFighters=fighters.ToArray();int oldArt=artPack;var random=UnityEngine.Random.state;
        try
        {
            artPack=0;
            foreach(string holder in new[]{"gabumon","agumon"})
            foreach(int incoming in new[]{2,11,12,18,14})
            foreach(float age in new[]{0f,2.99f,3f,5.99f,6f,12f,14.99f,15f,30f})
            {
                fighters.Clear();var unit=new Unit(RosterById[holder]);
                if(incoming==14)unit.items.AddRange(new[]{2,holder=="agumon"?18:12});
                var live=CreateFighter(unit,false,new Vector2(3,5));fighters.Add(live);
                fighters.Add(CreateFighter(new Unit(RosterById["tsunomon"]),false,new Vector2(2,5)));
                if(holder=="gabumon")fighters.Add(CreateFighter(new Unit(RosterById["garurumon"]),false,new Vector2(4,5)));
                if(holder=="agumon")fighters.Add(CreateFighter(new Unit(RosterById["gabumon"]),false,new Vector2(4,5)));
                InitializeBuildBonuses();live.combatAge=age;live.hp=live.maxHp*.4f;live.mana=7;live.lowShieldUsed=true;live.attacks=4;
                var items=unit.items.ToArray();var team=CombatBuildMembers(false).ToArray();
                var p=DigimonEquipmentPreview.Create(holder,1,team,unit.items,incoming);
                DigimonEquipmentPreview.SetCombatState(p,live.hp,live.maxHp,live.mana,age);
                float before=DigimonCombatMath.AttackSpeed(Meta(holder).speed,live.build.speed,live.build.rampSpeed,age);
                Require(Mathf.Abs(p.before.speed-before)<.0001f,"combat preview uses current friendship age: "+holder+"/"+incoming+"/"+age);
                Require(unit.items.SequenceEqual(items)&&team[0].items.SequenceEqual(items),"equipment projection never mutates live or source gear");
                if(!p.change.allowed)
                {Require(Mathf.Abs(p.before.speed-p.after.speed)<.0001f,"rejected equipment cannot predict a speed change");continue;}
                ApplyLiveEquipment(live,p.change.items);
                float after=DigimonCombatMath.AttackSpeed(Meta(holder).speed,live.build.speed,live.build.rampSpeed,age);
                Require(Mathf.Abs(p.after.speed-after)<.0001f,"projected speed matches live equipment application: "+holder+"/"+incoming+"/"+age);
                Require(Mathf.Abs(p.projectedHealth-live.hp)<.001f&&live.mana==7&&live.lowShieldUsed&&live.attacks==4&&live.combatAge==age,"preview/application preserve combat health ratio and existing counters");
                DigimonEquipmentPreview.SetCombatState(p,p.currentHealth,p.before.health,p.currentMana,age);
                Require(Mathf.Abs(p.after.speed-after)<.0001f,"repeated combat projection never accumulates extra attack speed");
            }
            var sample=DigimonEquipmentPreview.Create("gabumon",1,new[]{"gabumon","tsunomon","garurumon"},new int[0],2);
            DigimonEquipmentPreview.SetCombatState(sample,100,100,7,15);
            Require(Mathf.Abs(sample.before.speed-1.105f)<.0001f&&Mathf.Abs(sample.after.speed-1.207f)<.0001f,"three-beast five-stack preview includes base speed and equipment additively");
            DigimonEquipmentPreview.SetCombatState(sample,100,100,7,3);
            Require(Mathf.Abs(sample.before.speed-1.003f)<.0001f,"replayed earlier age recomputes from base without retained future stacks");
            foreach(var skill in DigimonSkillCatalog.All)
            foreach(int star in new[]{1,2,3})
            {
                string label=skill.ScalingStats(star);int i=DigimonCombatMath.StarIndex(star);
                Require(label.Contains("공격력")== (skill.adRatio[i]>0)&&label.Contains("주문력")== (skill.apRatio[i]>0),"skill strengthening label uses current star coefficients: "+skill.id+"/"+star);
                Require(!skill.ScalingRole.Contains("딜러"),"skill coefficient category never relabels tanks or supports as damage roles: "+skill.id);
            }
        }
        finally{fighters.Clear();fighters.AddRange(oldFighters);artPack=oldArt;UnityEngine.Random.state=random;}
    }
    DigimonBuildCatalog.Member[] EmblemReviewMembers()
    {
        return new[]{"mochimon","tentomon","kabuterimon","atlur","herakle","kuwagamon","palmon","patamon"}
            .Select(id=>new DigimonBuildCatalog.Member(id,DigimonBuildCatalog.Find("knowledge").members.Contains(id)?new int[0]:new[]{20})).ToArray();
    }
    IEnumerator CaptureEquipmentClarityReview()
    {
        var oldBoard=(Unit[])board.Clone();var oldBench=(Unit[])bench.Clone();var oldBag=inventory.ToArray();var oldFighters=fighters.ToArray();
        int oldLevel=level,oldScout=scoutedRival,oldSelectedItem=selectedItem;bool oldBattle=battling,oldCarousel=showCarousel,oldReport=showCombatReport;
        var oldPointer=validationPointer;var oldInspection=inspectedUnit;var oldSkill=skillDetailUnit;var oldTrait=traitFocus;var random=UnityEngine.Random.state;
        try
        {
            battling=false;showCarousel=false;scoutedRival=-1;showCombatReport=false;selectedItem=-1;skillDetailUnit=null;inspectedUnit=null;traitFocus=null;showRecipeGuide=false;level=8;
            Array.Clear(board,0,board.Length);Array.Clear(bench,0,bench.Length);inventory.Clear();EnsureArena();
            var members=EmblemReviewMembers();for(int i=0;i<members.Length;i++){board[i]=new Unit(RosterById[members[i].id]);board[i].items.AddRange(members[i].items);}
            OpenTraitGuide("knowledge");yield return null;yield return new WaitForEndOfFrame();CaptureRuntime("clarity-synergy-emblems-top");
            DigimonTraitUI.ReviewQueueWheel(1);yield return null;yield return new WaitForEndOfFrame();
            Require(DigimonTraitUI.ReviewWheelConsumed&&DigimonTraitUI.ReviewMemberScroll.y==20,"one downward wheel step moves trait contributors exactly twenty GUI pixels");
            yield return null;yield return new WaitForEndOfFrame();
            Require(DigimonTraitUI.ReviewMemberScroll.y==20,"trait wheel is not applied again by the following ScrollView frame");
            DigimonTraitUI.ReviewQueueWheel(100);yield return null;yield return new WaitForEndOfFrame();yield return null;yield return new WaitForEndOfFrame();
            CaptureRuntime("clarity-synergy-emblems-bottom");
            Require(DigimonTraitUI.ReviewWheelConsumed&&DigimonTraitUI.ReviewMemberScroll.y==89&&DigimonTraitUI.ReviewMemberOverflow==89,"downward wheel clamps at the final row and exposes all eight emblem contributors");
            DigimonTraitUI.ReviewQueueWheel(-1);yield return null;yield return new WaitForEndOfFrame();
            Require(DigimonTraitUI.ReviewWheelConsumed&&DigimonTraitUI.ReviewMemberScroll.y==69,"one upward wheel step moves twenty GUI pixels toward the first row");
            DigimonTraitUI.ReviewQueueWheel(-100);yield return null;yield return new WaitForEndOfFrame();yield return null;yield return new WaitForEndOfFrame();
            Require(DigimonTraitUI.ReviewWheelConsumed&&DigimonTraitUI.ReviewMemberScroll.y==0,"reverse wheel clamps the trait roster at its first row");
            DigimonTraitUI.ReviewQueueWheel(1,false);yield return null;yield return new WaitForEndOfFrame();
            Require(!DigimonTraitUI.ReviewWheelConsumed&&DigimonTraitUI.ReviewMemberScroll.y==0,"wheel outside the contributor viewport is not consumed by the trait roster");
            DigimonTraitUI.ReviewQueueWheel(1,true,false);yield return null;yield return new WaitForEndOfFrame();
            Require(!DigimonTraitUI.ReviewWheelConsumed&&DigimonTraitUI.ReviewMemberScroll.y==0,"disabled contributor viewport does not handle wheel input");
            board[6]=null;board[7]=null;DigimonTraitUI.ReviewQueueWheel(1);yield return null;yield return new WaitForEndOfFrame();
            Require(!DigimonTraitUI.ReviewWheelConsumed&&DigimonTraitUI.ReviewMemberScroll.y==0&&DigimonTraitUI.ReviewMemberOverflow==0,"six contributors fit without scrolling or consuming wheel input");
            traitFocus=null;Array.Clear(board,0,board.Length);board[3]=new Unit(RosterById["gabumon"]);board[4]=new Unit(RosterById["tsunomon"]);board[5]=new Unit(RosterById["garurumon"]);
            fighters.Clear();foreach(int slot in new[]{3,4,5}){var f=CreateFighter(board[slot],false,new Vector2(slot,5));f.sourceSlot=slot;fighters.Add(f);}InitializeBuildBonuses();
            foreach(var f in fighters){f.combatAge=15;f.hp=f.maxHp*.6f;f.mana=7;}battling=true;inventory.Add(2);selectedItem=0;
            var holder=fighters[0];validationPointer=arena.Project(FighterWorld(holder)+Vector3.up*(TacticalArena.DigimonHeadHeight(holder.unit.def.id,1)*.45f));
            yield return null;yield return new WaitForEndOfFrame();CaptureRuntime("clarity-friendship-combat-preview");
            Require(battling&&holder.combatAge==15&&selectedItem==0,"combat preview capture keeps the controlled fifteen-second frame and selected gear");
            selectedItem=-1;battling=false;validationPointer=null;
            foreach(string id in new[]{"agumon","weregarurumon","wargreymon"})
            {
                OpenSkillDetails(new Unit(RosterById[id]));yield return null;yield return new WaitForEndOfFrame();CaptureRuntime("clarity-skill-"+id);skillDetailUnit=null;
            }
            foreach(int item in new[]{5,11,12,17,25})
            {OpenEquipmentGuide(item);yield return null;yield return new WaitForEndOfFrame();CaptureRuntime("clarity-item-"+item);showRecipeGuide=false;}
        }
        finally
        {
            Array.Copy(oldBoard,board,board.Length);Array.Copy(oldBench,bench,bench.Length);inventory.Clear();inventory.AddRange(oldBag);fighters.Clear();fighters.AddRange(oldFighters);
            level=oldLevel;scoutedRival=oldScout;battling=oldBattle;showCarousel=oldCarousel;showCombatReport=oldReport;selectedItem=oldSelectedItem;validationPointer=oldPointer;inspectedUnit=oldInspection;skillDetailUnit=oldSkill;traitFocus=oldTrait;showRecipeGuide=false;
            DigimonTraitUI.ReviewQueueWheel(0);DigimonTraitUI.ReviewMemberScroll=Vector2.zero;UnityEngine.Random.state=random;
        }
    }
}
#endif
