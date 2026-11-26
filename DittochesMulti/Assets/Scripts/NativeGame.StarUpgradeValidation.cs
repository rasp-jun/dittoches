#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    float? nativeMergeArrivalReviewAge;
    IEnumerator ValidateNativePlannerInspection()
    {
        OpenTeamPlan();teamPlanner.Query="";teamPlanner.Cost=0;yield return new WaitForEndOfFrame();CaptureRuntime("31-team-planner");
        string previousPlan=JsonUtility.ToJson(TeamPlan.Data);int previousGold=gold;
        Rect card=TeamPlannerUI.CardRect(TeamPlannerUI.Bounds(new Rect(0,0,1600,900)),0);
        card=new Rect(card.x*1.2f,card.y*1.2f,card.width*1.2f,card.height*1.2f);
        yield return SoloLobbyClick(card,1);
        Require(showTeamPlan&&teamPlanner.InspectionId=="koromon"&&JsonUtility.ToJson(TeamPlan.Data)==previousPlan&&gold==previousGold,
            "native scaled planner right-click is read-only (open="+showTeamPlan+", id="+teamPlanner.InspectionId+", gold="+gold+"/"+previousGold+", planSame="+(JsonUtility.ToJson(TeamPlan.Data)==previousPlan)+")");
        yield return new WaitForEndOfFrame();CaptureRuntime("planner-native-inspection");
        yield return ModalValidationKey(KeyCode.Escape);Require(showTeamPlan&&teamPlanner.InspectionId=="","native first Escape returns to planner");
        if(TeamPlan.Units.Count==0)TeamPlan.Toggle("agumon");
        string synergyPlan=JsonUtility.ToJson(TeamPlan.Data);var firstTrait=TeamPlannerUI.ForecastTraits()[0];
        Rect forecast=TeamPlannerUI.SynergyViewport(TeamPlannerUI.Bounds(new Rect(0,0,1600,900)));
        yield return SoloLobbyClick(new Rect((forecast.x+10)*1.2f,(forecast.y+5)*1.2f,150*1.2f,30*1.2f));
        Require(showTeamPlan&&teamPlanner.TraitFocus==firstTrait.id&&JsonUtility.ToJson(TeamPlan.Data)==synergyPlan&&gold==previousGold,"native scaled planner forecast is read-only");
        yield return new WaitForEndOfFrame();CaptureRuntime("planner-native-synergy");
        yield return ModalValidationKey(KeyCode.Escape);Require(showTeamPlan&&teamPlanner.TraitFocus=="","native trait Escape returns to planner");
        TeamPlan.Restore(previousPlan);
        yield return ModalValidationKey(KeyCode.Escape);Require(!showTeamPlan,"native second Escape closes planner");
    }
    void ValidateStarUpgrades()
    {
        StarSoundValidation.Rules(Require);
        ValidatePurchaseForecasts();
        int checks=validationChecks;
        foreach(var skill in DigimonSkillCatalog.All)
        foreach(var bonus in new[]{new DigimonBuildCatalog.Bonus(),new DigimonBuildCatalog.Bonus{health=200,hp=.35f,attack=.28f,abilityPower=45}})
        for(int from=1;from<=3;from++)for(int to=1;to<=3;to++)
        {
            var current=new DigimonSkillCatalog.Stats(skill,from,bonus);
            var actual=new DigimonSkillCatalog.Stats(skill,to,bonus);
            Require(Mathf.Abs(StarUpgradeUI.AttackAt(current,from,to)-actual.attack)<.001f,"upgrade attack matches combat calculation "+skill.id+":"+from+":"+to);
            Require(Mathf.Abs(StarUpgradeUI.HealthAt(current,from,to)-actual.health)<.01f,"upgrade health matches combat calculation "+skill.id+":"+from+":"+to);
        }
        Require(StarUpgradeUI.Progress(2,0).Contains("2 / 3")&&StarUpgradeUI.Progress(2,0).Contains("1개 남음"),"two copies need one for two-star");
        Require(StarUpgradeUI.Progress(1,1).Contains("4 / 9")&&StarUpgradeUI.Progress(1,1).Contains("5개 남음"),"star-two converts to three base copies");
        Require(StarUpgradeUI.Collection(2,2).Contains("★★★ 연속 승급"),"eight copies predicts chain upgrade");
        Require(StarUpgradeUI.Collection(0,0,1).Contains("최고 성급"),"max star has no further upgrade promise");
        using(var review=new TacticalArena(new Rect(0,0,960,540)))review.ValidatePromotionEffects(Require);
        Debug.Log("STAR UPGRADE RULES COMPLETE: "+(validationChecks-checks)+" checks / combat-stat parity, copies, bounded pooled effects");
    }
    IEnumerator CaptureStarUpgradeReview()
    {
        var savedBoard=board.ToArray();var savedBench=bench.ToArray();var savedShop=shop.ToArray();var savedInventory=inventory.ToArray();
        int savedGold=gold;bool savedBattle=battling;var savedInspect=inspectedUnit;var soundPreferences=new StarSoundValidation.Preferences();
        var savedArrivals=nativeMergeArrivals.ToArray();var savedPromotions=promotions.ToArray();var savedNotice=promotionNoticeUnit;float savedUntil=promotionNoticeUntil;
        string savedPlacement=placementNotice;float savedPlacementUntil=placementNoticeUntil;
        try
        {
            StarUpgradeAudio.Configure(35,false);StarUpgradeAudio.Cancel();
            battling=false;inspectedUnit=null;promotionNoticeUnit=null;promotions.Clear();nativeMergeArrivals.Clear();
            Array.Clear(board,0,board.Length);Array.Clear(bench,0,bench.Length);Array.Clear(shop,0,shop.Length);inventory.Clear();gold=30;
            var agumon=RosterById["agumon"];var greymon=RosterById["greymon"];
            board[10]=new Unit(agumon){star=2};board[11]=new Unit(agumon){star=2};board[12]=new Unit(agumon);bench[0]=new Unit(agumon);
            board[17]=new Unit(greymon);bench[1]=new Unit(greymon);
            board[10].items.AddRange(new[]{8,12,13});board[11].items.Add(6);
            shop[0]=agumon;shop[1]=greymon;shop[2]=RosterById["piedmon"];shop[3]=RosterById["machinedramon"];shop[4]=RosterById["meramon"];
            yield return new WaitForSeconds(.25f);yield return new WaitForEndOfFrame();CaptureRuntime("upgrade-shop-progress");
            string collectionState=PurchaseState(),collectionPlan=JsonUtility.ToJson(TeamPlan.Data);
            var collection=ReadNativeCollection();var agumonRow=collection.entries.First(r=>r.id=="agumon");
            Require(agumonRow.Copies==8&&agumonRow.singles==2&&agumonRow.doubles==2&&agumonRow.board==3&&agumonRow.bench==1&&agumonRow.CanUpgrade&&agumonRow.NextStar==3,"native collection uses real formation ranks, locations and current offers");
            OpenTeamPlan();Rect collectionPanel=TeamPlannerUI.Bounds(new Rect(0,0,1600,900));Rect collectionButton=TeamPlannerUI.CollectionButton(collectionPanel);
            yield return SoloLobbyClick(new Rect(collectionButton.x*1.2f,collectionButton.y*1.2f,collectionButton.width*1.2f,collectionButton.height*1.2f));
            Require(showTeamPlan&&teamPlanner.CollectionOpen,"scaled native collection navigation works");
            yield return new WaitForEndOfFrame();CaptureRuntime("collection-native-overview");
            Rect collectionCard=TeamPlannerUI.CollectionCard(collectionPanel,0);
            yield return SoloLobbyClick(new Rect(collectionCard.x*1.2f,collectionCard.y*1.2f,collectionCard.width*1.2f,collectionCard.height*1.2f),1);
            Require(teamPlanner.InspectionId=="agumon","native collection inspects highest-priority chain merge");
            yield return new WaitForEndOfFrame();CaptureRuntime("collection-native-inspection");
            foreach(var key in new[]{KeyCode.D,KeyCode.F,KeyCode.E,KeyCode.W,KeyCode.Return})yield return ModalValidationKey(key);
            Require(PurchaseState()==collectionState&&JsonUtility.ToJson(TeamPlan.Data)==collectionPlan,"native collection and nested inspection preserve formation items gold and goals");
            yield return ModalValidationKey(KeyCode.Escape);Require(showTeamPlan&&teamPlanner.CollectionOpen&&teamPlanner.InspectionId=="","native collection child Escape returns to collection");
            gold=0;Require(ReadNativeCollection().entries.All(r=>!r.CanUpgrade),"native live gold changes disable affordable merge status");gold=30;
            yield return ModalValidationKey(KeyCode.Escape);Require(!showTeamPlan,"native collection Escape returns to gameplay");
            Debug.Log("STAR COLLECTION NATIVE COMPLETE: actual formation and shop ranks, scaled input, nested inspection, blocked gameplay keys, live gold and unchanged unit/plan state");
            string beforeHover=PurchaseState();validationPointer=new Vector2(380,960);
            var merge=NativeRecruitHover(validationPointer.Value,true);
            Require(merge!=null&&merge.sources.Count==4&&merge.result.star==3,"native hover identifies all four owned contributors to a three-star merge");
            Require(NativeRecruitHover(validationPointer.Value,false)==null&&NativeRecruitHover(new Vector2(1,1),true)==null,"native merge hover is disabled behind modals and outside the shop");
            gold=0;Require(NativeRecruitHover(validationPointer.Value,true)==null,"native unaffordable purchase cannot highlight a merge");gold=30;
            selectedItem=0;Require(NativeRecruitHover(validationPointer.Value,true)==null,"native equipment selection hides merge focus");selectedItem=-1;
            yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();CaptureRuntime("merge-focus-native-cascade");
            Require(PurchaseState()==beforeHover,"native hover and rendering cannot change units gold or equipment");
            validationPointer=new Vector2(660,960);yield return new WaitForEndOfFrame();CaptureRuntime("merge-focus-native-pair");
            validationPointer=null;
            Debug.Log("MERGE FOCUS NATIVE COMPLETE: actual shop hover, exact cascade sources, two-star target, modal/position/gold/equipment guards and state preservation");
            OpenShopSkill(agumon);yield return new WaitForEndOfFrame();CaptureRuntime("upgrade-before-purchase");CaptureRuntime("purchase-native-cascade");skillDetailUnit=null;
            OpenShopSkill(greymon);yield return new WaitForEndOfFrame();CaptureRuntime("purchase-native-pair");skillDetailUnit=null;
            gold=0;OpenShopSkill(agumon);yield return new WaitForEndOfFrame();CaptureRuntime("purchase-native-no-gold");skillDetailUnit=null;gold=30;
            StarUpgradeAudio.Cancel();int soundPlayed=StarUpgradeAudio.PlayedCount;
            Require(Buy(1),"review two-star purchase succeeds");
            Require(StarUpgradeAudio.PendingStar==2,"native actual two-star purchase queues rank-two sound");
            Require(nativeMergeArrivals.Count==1&&nativeMergeArrivals[0].flight.origins.Length==2,"actual purchase immediately records both two-star contributors");
            var pairFlight=nativeMergeArrivals[0].flight;
            // Freeze only the capture clock, as for the cascade below: PNG/GC stalls must not expire a reviewed effect.
            nativeMergeArrivalReviewAge=.25f;
            yield return new WaitForSeconds(.25f);yield return new WaitForEndOfFrame();CaptureRuntime("upgrade-two-star");CaptureRuntime("merge-arrival-native-pair");
            Require(nativeMergeArrivals.Count==1&&nativeMergeArrivals[0].flight.origins.Length==2,"two-star arrival gathers consumed copy and purchased copy");
            nativeMergeArrivalReviewAge=pairFlight.Duration+.01f;DrawNativeMergeArrivals();
            Require(nativeMergeArrivals.Count==0,"two-star arrival expires at its actual duration");nativeMergeArrivalReviewAge=null;
            Require(board[17].star==2,"review kept field unit at two-star");
            Require(StarUpgradeAudio.PlayedCount==soundPlayed+1&&StarUpgradeAudio.LastPlayedStar==2,"native actual purchase emits exactly one two-star sound");
            soundPlayed=StarUpgradeAudio.PlayedCount;Require(Buy(0),"review chain three-star purchase succeeds");
            Require(StarUpgradeAudio.PendingStar==3,"native chain purchase keeps only final three-star sound pending");
            var arrival=nativeMergeArrivals.Last().flight;
            Require(board[10].star==3&&board[10].items.Count==3&&inventory.Count==1,"review three-star keeps three items and returns overflow");
            nativeMergeArrivalReviewAge=.2f;yield return new WaitForEndOfFrame();CaptureRuntime("merge-arrival-native-cascade-start");CaptureRuntime("upgrade-three-star");
            Require(arrival.origins.Length==4,"three-star cascade gathers all consumed material locations");
            nativeMergeArrivalReviewAge=.6f;yield return new WaitForEndOfFrame();CaptureRuntime("merge-arrival-native-cascade-flight");
            nativeMergeArrivalReviewAge=.98f;yield return new WaitForEndOfFrame();CaptureRuntime("merge-arrival-native-cascade-arrive");
            nativeMergeArrivalReviewAge=null;arrival.started=Time.unscaledTime;nativeMergeArrivals.Clear();
            for(int n=0;n<6;n++)CommitNativeMergeArrival(arrival);
            Require(nativeMergeArrivals.Count==4,"burst purchases keep at most four active native arrivals");
            var merged=board[10];board[10]=null;bench[8]=merged;DrawNativeMergeArrivals();
            Require(nativeMergeArrivals.Count==4,"native arrival follows the same unit after moving");
            bench[8]=null;DrawNativeMergeArrivals();Require(nativeMergeArrivals.Count==0,"sold or removed unit clears its arrivals");board[10]=merged;
            CommitNativeMergeArrival(arrival);battling=true;DrawNativeMergeArrivals();Require(nativeMergeArrivals.Count==0,"combat transition clears preparation arrivals");battling=false;
            Require(StarUpgradeAudio.PlayedCount==soundPlayed+1&&StarUpgradeAudio.LastPlayedStar==3,"native chain merge plays one three-star flourish instead of both ranks");
            Debug.Log("STAR SOUND NATIVE COMPLETE: actual two-star purchase and chain-three-star transaction emit exactly one final-rank cue");
            Debug.Log("MERGE ARRIVAL NATIVE COMPLETE: actual two/three-star purchases, exact material origins, bounded records, moved/removed targets and phase cleanup");
            OpenSkillDetails(board[10]);yield return new WaitForEndOfFrame();CaptureRuntime("upgrade-max-star-details");skillDetailUnit=null;
            Debug.Log("STAR UPGRADE CAPTURES COMPLETE: real two-star and three-star purchases, overflow, shop forecast and max-star stats");
        }
        finally
        {
            soundPreferences.Dispose();savedBoard.CopyTo(board,0);savedBench.CopyTo(bench,0);savedShop.CopyTo(shop,0);inventory.Clear();inventory.AddRange(savedInventory);gold=savedGold;battling=savedBattle;inspectedUnit=savedInspect;
            nativeMergeArrivalReviewAge=null;nativeMergeArrivals.Clear();nativeMergeArrivals.AddRange(savedArrivals);skillDetailUnit=null;promotions.Clear();foreach(var p in savedPromotions)promotions[p.Key]=p.Value;promotionNoticeUnit=savedNotice;promotionNoticeUntil=savedUntil;
            placementNotice=savedPlacement;placementNoticeUntil=savedPlacementUntil;
            validationPointer=null;
        }
    }
}
public sealed partial class TacticalArena
{
    public void ValidatePromotionEffects(Action<bool,string> require)
    {
        for(int star=1;star<=3;star++)foreach(float remaining in new[]{1f,.8f,.5f,.1f,0f,-.1f})
        {
            BeginFrame(-1,-1,-1,false);SetActor("upgrade",CellWorld(3,5),null,Color.green);
            DrawPromotion("upgrade",star,remaining);Render();
            require(EffectCount<=41,"promotion stays within 41 pooled effects");
            require((EffectCount>0)==(star>1&&remaining>0),"only valid active upgrades produce effects");
            require(SkillEffectsHealthy(),"promotion effect geometry and pool state stay healthy");
        }
        int capacity=effectPool.Count;
        for(int i=0;i<10;i++)
        {BeginFrame(-1,-1,-1,false);SetActor("upgrade",CellWorld(3,5),null,Color.green);DrawPromotion("upgrade",3,.6f);Render();require(effectPool.Count==capacity,"promotion reuses existing geometry pool");}
        BeginFrame(-1,-1,-1,false);Render();require(EffectCount==0&&SkillEffectsHealthy(),"promotion clears when actor disappears");
        foreach(int star in new[]{1,2,3,4})foreach(bool recipient in new[]{false,true})
        {
            BeginFrame(-1,-1,-1,false);DrawMergeFocus(CellWorld(3,5),star,recipient,1);Render();
            require(EffectCount==(star>=2&&star<=3?(recipient?6:5):0)&&SkillEffectsHealthy(),"merge highlight uses at most six healthy pooled shapes and rejects invalid ranks");
            require(effectPool.Count==capacity,"merge hover reuses promotion geometry capacity");
        }
        BeginFrame(-1,-1,-1,false);Render();require(EffectCount==0,"merge focus disappears on the next frame after hover ends");
        foreach(bool onBoard in new[]{false,true})
        {
            var forecast=PurchaseForecast.Create("agumon",new[]{new PurchaseForecast.Piece("agumon",onBoard,0,1,null),new PurchaseForecast.Piece("agumon",onBoard,1,1,null)},9,10,1);
            var flight=StarMergeArrival.Create(forecast,MergeFocusUI.Position,BenchWorld(4),true,10);
            require((flight==null)==onBoard,"battle arrival is allowed only on bench, never on frozen combat board");
        }
        var plain=PurchaseForecast.Create("agumon",new PurchaseForecast.Piece[0],9,10,1);
        require(StarMergeArrival.Create(plain,MergeFocusUI.Position,Vector3.zero,false,10)==null,"plain purchase has no merge arrival");
        plain.available=false;require(StarMergeArrival.Create(plain,MergeFocusUI.Position,Vector3.zero,false,10)==null,"rejected purchase has no merge arrival");
        var from=BenchWorld(0);var to=CellWorld(3,5);
        require(Vector3.Distance(StarMergeArrival.Curve(from,to,-1),from)<.0001f&&Vector3.Distance(StarMergeArrival.Curve(from,to,2),to)<.0001f,"arrival curve clamps exactly to its endpoints");
        foreach(int star in new[]{2,3})
        {
            var flight=new StarMergeArrival{id="agumon",star=star,started=10,origins=Enumerable.Range(0,8).Select(BenchWorld).ToArray()};
            foreach(float progress in new[]{-.1f,0f,.1f,.4f,.65f,.9f,1f,1.1f})
            {
                BeginFrame(-1,-1,-1,false);DrawMergeArrival(flight,to,10+flight.Duration*progress);Render();
                require(EffectCount<=66&&SkillEffectsHealthy(),"arrival remains within 66 healthy pooled shapes even with eight sources");
                if(progress<=0||progress>=1)require(EffectCount==0,"future, initial and expired arrivals cannot leave stray effects "+star+":"+progress+":"+EffectCount);
            }
            capacity=effectPool.Count;
            for(int n=0;n<5;n++){BeginFrame(-1,-1,-1,false);DrawMergeArrival(flight,to,10+flight.Duration*.4f);Render();require(effectPool.Count==capacity,"arrival reuses its warmed geometry pool");}
        }
        BeginFrame(-1,-1,-1,false);Render();require(EffectCount==0&&SkillEffectsHealthy(),"arrival geometry disappears on the next frame after completion");
    }
}
#endif
