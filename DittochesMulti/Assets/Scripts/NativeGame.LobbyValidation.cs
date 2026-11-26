#if DITTOCHES_PORTABLE_PREVIEW
using System.Collections;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    int soloLobbyClickStep;
    bool soloLobbyEventInjected;
    Vector2 soloLobbyClickPoint;
    KeyCode soloLobbyTestKey;
    Event SoloLobbyValidationEvent()
    {
        soloLobbyEventInjected=false;
        if(soloLobbyClickStep==0||Event.current.type!=EventType.Repaint)return null;
        soloLobbyEventInjected=true;
        Event saved=new Event(Event.current);
        if(soloLobbyTestKey!=KeyCode.None)
        {
            Event.current=new Event{type=EventType.KeyDown,keyCode=soloLobbyTestKey};
            soloLobbyTestKey=KeyCode.None;soloLobbyClickStep=0;return saved;
        }
        Event.current=new Event{type=soloLobbyClickStep==1?EventType.MouseDown:EventType.MouseUp,button=soloLobbyClickButton,mousePosition=soloLobbyClickPoint};
        soloLobbyClickStep=soloLobbyClickStep==1?2:0;return saved;
    }
    int soloLobbyClickButton;
    IEnumerator SoloLobbyClick(Rect rect,int button=0)
    {
        soloLobbyClickButton=button;soloLobbyClickPoint=rect.center;soloLobbyClickStep=1;float deadline=Time.unscaledTime+3;
        while(soloLobbyClickStep>0&&Time.unscaledTime<deadline)yield return null;
        Require(soloLobbyClickStep==0,"solo lobby receives both click phases");yield return null;
    }
    IEnumerator ValidateSoloLobbyPolish()
    {
        int savedRound=round,savedGold=gold,savedHp=hp,savedDifficulty=difficulty;
        var savedBoard=board.ToArray();string savedLoadout=tamerLoadout.Summary,savedPlan=JsonUtility.ToJson(TeamPlan.Data);
        string stored=PortablePreviewPrefs.GetString(SaveKey,"");
        for(int choice=0;choice<3;choice++)
        {
            yield return SoloLobbyClick(SoloDifficultyRect(choice));
            Require(difficulty==choice&&lobby,"rebuilt difficulty card selects its own option");
            Require(round==savedRound&&gold==savedGold&&board.SequenceEqual(savedBoard),"difficulty preview keeps league progress");
        }
        yield return SoloLobbyClick(SoloDifficultyRect(savedDifficulty));
        Require(PortablePreviewPrefs.GetString(SaveKey,"")==stored,"difficulty preview does not overwrite saved league before entering");
        yield return SoloLobbyClick(SoloLobbyResetRect);
        Require(confirmSoloReset&&lobby,"new-game action opens confirmation and stays in preparation");
        Require(round==savedRound&&gold==savedGold&&hp==savedHp&&board.SequenceEqual(savedBoard),"opening reset cannot mutate league progress");
        yield return new WaitForEndOfFrame();CaptureRuntime("31-solo-reset-confirmation");
        yield return SoloLobbyClick(SoloDifficultyRect((difficulty+1)%3));
        yield return SoloLobbyClick(SoloLobbyEnterRect);
        Require(confirmSoloReset&&lobby&&difficulty==savedDifficulty,"confirmation blocks difficulty and continue controls underneath");
        soloLobbyTestKey=KeyCode.Escape;soloLobbyClickStep=1;float deadline=Time.unscaledTime+3;
        while(soloLobbyClickStep>0&&Time.unscaledTime<deadline)yield return null;
        Require(!confirmSoloReset&&lobby,"Escape cancels reset without leaving preparation");
        Require(PortablePreviewPrefs.GetString(SaveKey,"")==stored,"cancelled reset does not write over saved league");
        yield return SoloLobbyClick(SoloLobbyResetRect);yield return SoloLobbyClick(SoloResetCancelRect);
        Require(!confirmSoloReset&&round==savedRound&&board.SequenceEqual(savedBoard),"cancel button preserves exact formation and progress");
        yield return SoloLobbyClick(SoloLobbyEnterRect);
        Require(!lobby&&round==savedRound&&gold==savedGold,"continue enters the existing league without resetting progress");
        lobby=true;yield return SoloLobbyClick(SoloLobbyResetRect);yield return SoloLobbyClick(SoloResetConfirmRect);
        Require(!lobby&&!confirmSoloReset&&round==1&&hp==100&&gold==0&&level==1,"explicit confirmation starts a fresh league");
        Require(tamerLoadout.Summary==savedLoadout&&JsonUtility.ToJson(TeamPlan.Data)==savedPlan,"reset retains equipped cosmetics and team plan");
        Require(Load()&&round==1&&hp==100&&gold==0,"confirmed reset persists the new league");
        hp=0;lobby=true;yield return SoloLobbyClick(SoloLobbyEnterRect);
        Require(confirmSoloReset&&lobby&&hp==0,"finished league primary action opens new-game confirmation");
        yield return SoloLobbyClick(SoloResetCancelRect);hp=100;lobby=false;
        // The following legacy-art capture must receive its original fixture, not the fresh Digimon emblems.
        PortablePreviewPrefs.SetString(SaveKey,stored);
        Require(Load()&&round==savedRound&&gold==savedGold&&hp==savedHp,"reset review restores its isolated league fixture before legacy-art checks");
    }
    IEnumerator ModalValidationKey(KeyCode key)
    {
        soloLobbyTestKey=key;soloLobbyClickStep=1;float deadline=Time.unscaledTime+3;
        while(soloLobbyClickStep>0&&Time.unscaledTime<deadline)yield return null;
        Require(soloLobbyClickStep==0,"modal receives gameplay shortcut "+key);yield return null;
    }
    IEnumerator ValidateRewardAndGameOverScreens()
    {
        Require(PortablePreview.HasArgument("--arena-smoke"),"reward review uses the isolated validation preference prefix");
        string stored=PortablePreviewPrefs.GetString(SaveKey,"");
        Require(stored.Length>0,"reward review has a restorable isolated league fixture");
        int storedDifficulty=PortablePreviewPrefs.GetInt("multiSoloDifficulty"),storedLegend=PortablePreviewPrefs.GetInt("multiSoloLegend");
        int savedRound=round,savedGold=gold,savedHp=hp,savedDifficulty=difficulty;
        string savedLoadout=tamerLoadout.Summary,savedPlan=JsonUtility.ToJson(TeamPlan.Data);
        var savedReport=lastBattleReport.ToArray();var savedCarousel=(UnitDef[])carouselUnits.Clone();var savedItems=(int[])carouselItems.Clone();
        var random=UnityEngine.Random.state;var oldPointer=validationPointer;bool oldReport=showCombatReport,restored=false;
        try
        {
            lobby=false;battling=false;showCarousel=false;showTeamPlan=false;showRecipeGuide=false;traitFocus=null;skillDetailUnit=null;inspectedUnit=null;
            nativeFinisher=null;finisherStarted=-10;selectedBoard=selectedBench=selectedItem=-1;draggingUnit=false;dragSource=-1;scoutedRival=-1;validationPointer=null;
            showCombatReport=false;round=7;level=6;xp=2;gold=40;hp=100;shopLocked=true;
            System.Array.Clear(board,0,board.Length);System.Array.Clear(bench,0,bench.Length);inventory.Clear();board[3]=new Unit(RosterById["koromon"]);
            RebuildPool();Save();string beforeOpening=PortablePreviewPrefs.GetString(SaveKey,"");
            Require(RoundType()=="초밥집","reward fixture uses a real reward round");
            yield return SoloLobbyClick(new Rect(1696,916,205,87));
            Require(showCarousel&&round==7&&!battling,"main battle action opens reward selection without advancing the round");
            Require(PortablePreviewPrefs.GetString(SaveKey,"")==beforeOpening,"opening reward selection does not save or grant a reward");
            while(Time.unscaledTime<screenTransitionUntil)yield return null;
            yield return new WaitForEndOfFrame();CaptureRuntime("35-reward-selection");
            validationPointer=new Vector2(585,646);yield return new WaitForSeconds(.6f);yield return new WaitForEndOfFrame();CaptureRuntime("36-reward-equipment-tooltip");validationPointer=null;
            var offer=carouselUnits[0];int gift=carouselItems[0],rewardGold=gold;var preservedShop=shop.ToArray();
            Require(offer!=null,"reward fixture contains a recruitable first offer");
            yield return SoloLobbyClick(new Rect(24,925,200,54));
            yield return SoloLobbyClick(new Rect(249,893,271,170));
            foreach(KeyCode key in new[]{KeyCode.D,KeyCode.F,KeyCode.Return,KeyCode.Escape})yield return ModalValidationKey(key);
            Require(showCarousel&&gold==rewardGold&&xp==2&&inventory.Count==0&&round==7&&shop.SequenceEqual(preservedShop),"reward modal blocks background economy, purchases and gameplay shortcuts");
            Require(PortablePreviewPrefs.GetString(SaveKey,"")==beforeOpening,"blocked reward-modal inputs leave the stored league unchanged");
            yield return SoloLobbyClick(new Rect(430,700,310,34));
            Require(!showCarousel&&round==8&&gold==rewardGold&&inventory.SequenceEqual(new[]{gift}),"reward selection grants exactly one item and advances once");
            Require(bench.Count(u=>u!=null&&u.def.id==offer.id)==1&&shop.SequenceEqual(preservedShop),"reward recruits the chosen unit and preserves a locked shop");
            var granted=JsonUtility.FromJson<SoloSave>(PortablePreviewPrefs.GetString(SaveKey,""));
            Require(granted.round==8&&granted.inventory.SequenceEqual(new[]{gift}),"selected reward is persisted exactly once");
            yield return null;Require(round==8&&inventory.Count==1,"a later frame cannot replay the reward grant");

            var filler=Roster.Where(d=>d.id!="agumon").Take(9).ToArray();
            for(int i=0;i<9;i++)bench[i]=new Unit(filler[i]);
            carouselUnits[0]=RosterById["agumon"];carouselItems[0]=15;round=7;gold=24;showCarousel=true;RebuildPool();
            var fullBench=bench.ToArray();int itemsBefore=inventory.Count;
            yield return new WaitForEndOfFrame();CaptureRuntime("37-reward-full-bench");
            yield return SoloLobbyClick(new Rect(430,700,310,34));
            Require(!showCarousel&&round==8&&gold==26&&bench.SequenceEqual(fullBench),"full bench reward converts the chosen unit into its exact gold cost");
            Require(inventory.Count==itemsBefore+1&&inventory.Last()==15,"full bench conversion still grants the selected equipment");

            bench[0]=new Unit(RosterById["agumon"]);bench[1]=new Unit(RosterById["agumon"]);
            round=7;gold=24;showCarousel=true;RebuildPool();itemsBefore=inventory.Count;
            yield return SoloLobbyClick(new Rect(430,700,310,34));
            Require(!showCarousel&&round==8&&gold==24&&FindUnits("agumon",2).Count==1&&bench.Count(u=>u!=null)==8,"full bench reward uses an existing pair for automatic promotion instead of gold conversion");
            Require(inventory.Count==itemsBefore+1,"automatic promotion grants equipment only once");

            System.Array.Clear(bench,0,bench.Length);carouselUnits[2]=null;carouselItems[2]=2;round=7;gold=24;showCarousel=true;itemsBefore=inventory.Count;
            yield return SoloLobbyClick(new Rect(1180,700,310,34));
            Require(!showCarousel&&round==8&&gold==26&&!bench.Any(u=>u!=null)&&inventory.Count==itemsBefore+1&&inventory.Last()==2,"gold-supply fallback grants gold and equipment through the third card");

            hp=0;round=19;level=6;gold=50;Save();string finished=PortablePreviewPrefs.GetString(SaveKey,"");
            yield return new WaitForEndOfFrame();CaptureRuntime("38-solo-game-over");
            yield return SoloLobbyClick(new Rect(24,925,200,54));yield return SoloLobbyClick(new Rect(1702,22,88,38));
            foreach(KeyCode key in new[]{KeyCode.D,KeyCode.F,KeyCode.Return})yield return ModalValidationKey(key);
            Require(!lobby&&hp==0&&gold==50&&round==19&&!battling,"game-over modal blocks the hidden shop, top navigation and battle shortcuts");
            Require(PortablePreviewPrefs.GetString(SaveKey,"")==finished,"game-over preview and blocked inputs do not overwrite the finished league");
            yield return SoloLobbyClick(new Rect(980,625,290,70));
            Require(lobby&&hp==0&&round==19&&PortablePreviewPrefs.GetString(SaveKey,"")==finished,"game-over lobby action preserves the finished league");
            lobby=false;yield return SoloLobbyClick(new Rect(650,625,290,70));
            Require(!lobby&&hp==100&&round==1&&gold==0&&level==1,"game-over retry starts and saves a fresh league using the original action");
            Require(difficulty==savedDifficulty&&tamerLoadout.Summary==savedLoadout&&JsonUtility.ToJson(TeamPlan.Data)==savedPlan,"game-over retry preserves difficulty, cosmetics and team plan");
            var restarted=JsonUtility.FromJson<SoloSave>(PortablePreviewPrefs.GetString(SaveKey,""));
            Require(restarted.round==1&&restarted.hp==100&&restarted.gold==0,"game-over retry persists the fresh league");
        }
        finally
        {
            soloLobbyClickStep=0;soloLobbyTestKey=KeyCode.None;showCarousel=false;confirmSoloReset=false;lobby=false;battling=false;
            PortablePreviewPrefs.SetString(SaveKey,stored);PortablePreviewPrefs.SetInt("multiSoloDifficulty",storedDifficulty);PortablePreviewPrefs.SetInt("multiSoloLegend",storedLegend);PortablePreviewPrefs.Save();
            restored=Load();lastBattleReport.Clear();lastBattleReport.AddRange(savedReport);showCombatReport=oldReport;
            System.Array.Copy(savedCarousel,carouselUnits,carouselUnits.Length);System.Array.Copy(savedItems,carouselItems,carouselItems.Length);
            validationPointer=oldPointer;UnityEngine.Random.state=random;
        }
        Require(restored&&round==savedRound&&gold==savedGold&&hp==savedHp&&difficulty==savedDifficulty,"reward and game-over review restores the original isolated fixture for legacy checks");
        Require(PortablePreviewPrefs.GetString(SaveKey,"")==stored&&tamerLoadout.Summary==savedLoadout&&JsonUtility.ToJson(TeamPlan.Data)==savedPlan,"reward and game-over review preserves saved league bytes, cosmetics and team plan");
    }
}
#endif
