#if DITTOCHES_PORTABLE_PREVIEW
using System.Collections;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    void ValidateTrialPolishRecords()
    {
        for(int i=0;i<3;i++)PortablePreviewPrefs.DeleteKey(TrialRecordKey(i));ClearTrialRecordCache();
        trialsMode=true;trialDifficulty=0;ResetTrialCore();
        Require(!TrialPersonalBest.HasRecord&&LastTrialRound==null,"new run has no fabricated record or recap");
        int oldGold=gold,oldScore=trialScore;trialBattlePending=true;ResolveTrialOutcome(true,12.5f);
        var summary=LastTrialRound;var best=TrialPersonalBest;
        Require(summary.stage==1&&summary.outcome==TrialRoundOutcome.Victory&&summary.elapsedKnown&&summary.elapsed==12.5f,"recap records actual resolved wave and elapsed time");
        Require(summary.goldGained==gold-oldGold&&summary.scoreGained==trialScore-oldScore&&summary.xpAwarded==TrialsCatalog.Get(1).xp&&summary.item==-1&&summary.livesLost==0,"recap records actual reward deltas");
        Require(TrialWaveResult(1)==1&&TrialWaveResult(2)==0&&best.bestScore==trialScore&&best.furthestStage==2&&best.bestWins==1,"best record tracks earned score and reached next wave");
        string resolved=PortablePreviewPrefs.GetString(TrialSaveKey(0)),record=PortablePreviewPrefs.GetString(TrialRecordKey(0));
        ResolveTrialOutcome(true,1);
        Require(PortablePreviewPrefs.GetString(TrialSaveKey(0))==resolved&&PortablePreviewPrefs.GetString(TrialRecordKey(0))==record,"duplicate settlement cannot change summary or record");
        lastTrialRound=null;trialWaveResults=new int[30];
        Require(LoadTrial()&&LastTrialRound!=null&&LastTrialRound.goldGained==summary.goldGained&&TrialWaveResult(1)==1,"recap and known outcome survive reload");
        RestartTrials();Require(LastTrialRound==null&&TrialWaveResult(1)==0&&TrialPersonalBest.bestScore==best.bestScore,"restart clears this run and retains personal best");
        Require(!GetTrialRecord(1).HasRecord&&!GetTrialRecord(2).HasRecord,"best records are independent by difficulty");
        trialBattlePending=true;ResolveTrialOutcome(false,CurrentTrial.timeLimit);
        Require(LastTrialRound.outcome==TrialRoundOutcome.Timeout&&LastTrialRound.livesLost==1&&LastTrialRound.scoreGained==0&&LastTrialRound.item==-1&&TrialWaveResult(1)==-1,"timeout recap records loss without victory rewards");
        trialBattlePending=true;SaveTrial();
        Require(LoadTrial()&&LastTrialRound.outcome==TrialRoundOutcome.Interrupted&&!LastTrialRound.elapsedKnown&&TrialWaveResult(2)==-1,"interrupted save records honest unknown elapsed time");
        int resumedGold=gold;string recovered=JsonUtility.ToJson(LastTrialRound);
        Require(LoadTrial()&&gold==resumedGold&&JsonUtility.ToJson(LastTrialRound)==recovered,"repeated interrupted reload preserves single loss recap");
        var legacy=JsonUtility.FromJson<TrialSave>(PortablePreviewPrefs.GetString(TrialSaveKey(0)));legacy.lastRound=null;legacy.waveResults=null;
        PortablePreviewPrefs.SetString(TrialSaveKey(0),JsonUtility.ToJson(legacy));
        Require(LoadTrial()&&LastTrialRound==null&&TrialWaveResult(1)==0&&TrialWaveResult(2)==0,"legacy run loads without inventing historical victories");
        PortablePreviewPrefs.SetString(TrialRecordKey(0),"not-json");ClearTrialRecordCache();
        Require(!TrialPersonalBest.HasRecord&&LoadTrial(),"invalid optional record cannot block valid run");
        legacy.lastRound=new TrialRoundSummary{stage=2,outcome=TrialRoundOutcome.Victory,goldGained=999999};
        legacy.waveResults=Enumerable.Repeat(1,30).ToArray();PortablePreviewPrefs.SetString(TrialSaveKey(0),JsonUtility.ToJson(legacy));
        Require(LoadTrial()&&LastTrialRound==null&&TrialWaveResult(1)==0,"corrupt optional reward/history discarded without losing run");
        trialDifficulty=2;ResetTrialCore();round=31;trialFinished=trialComplete=true;trialLives=2;trialScore=777;trialCleared=28;SaveTrial();
        Require(LoadTrial()&&TrialPersonalBest.cleared&&TrialPersonalBest.bestScore==777&&TrialPersonalBest.furthestStage==30&&TrialPersonalBest.bestWins==28,"finished legacy run migrates trusted aggregate record");
        RestartTrials();Require(TrialPersonalBest.cleared&&TrialPersonalBest.bestScore==777,"restarting a completed run retains its achievement");
    }
    IEnumerator ValidateTrialDossierInput()
    {
        int savedRound=round,savedGold=gold,mode=trialDifficulty;string save=PortablePreviewPrefs.GetString(TrialSaveKey(mode));
        var random=UnityEngine.Random.state;
        round=29;yield return null;yield return new WaitForEndOfFrame();CaptureRuntime("polish-03-six-enemies");
        for(int i=0;i<6;i++)
        {
            yield return SoloLobbyClick(TrialLobbyEnemyRect(i,6));
            Require(TrialDossierOpen&&TrialScoutedStage==29&&TrialScoutedEnemyIndex==i,"all six lobby enemy portraits open their own scouting profile");
            yield return ModalValidationKey(KeyCode.Escape);
        }
        round=savedRound;yield return null;
        Rect wave28=new Rect(350+5*257+18+2*42,649+144,35,34);
        yield return SoloLobbyClick(wave28);
        Require(TrialDossierOpen&&TrialScoutedStage==28,"actual roadmap opens future encounter without starting it");
        yield return SoloLobbyClick(TrialDossierCellRect(1,3));
        Require(TrialScoutedEnemyIndex==0,"dossier selects physical-defense shellmon by its actual board position");
        var first=InspectTrialEnemy(28,0,mode);yield return SoloLobbyClick(TrialDossierCellRect(5,3));
        var second=InspectTrialEnemy(28,1,mode);
        Require(TrialScoutedEnemyIndex==1&&first.armor>second.armor&&first.magicResist<second.magicResist,"same-species enemies retain distinct resistance profiles");
        yield return new WaitForEndOfFrame();CaptureRuntime("polish-01-dossier");
        yield return ModalValidationKey(KeyCode.Return);yield return ModalValidationKey(KeyCode.D);
        Require(TrialDossierOpen&&lobby&&!battling&&gold==savedGold&&round==savedRound,"dossier blocks gameplay hotkeys");
        yield return SoloLobbyClick(TrialDossierNextRect);Require(TrialScoutedStage==29,"dossier next button browses encounters");
        yield return SoloLobbyClick(TrialDossierPreviousRect);Require(TrialScoutedStage==28,"dossier previous button restores preview stage");
        int width=Screen.width,height=Screen.height;
        Screen.SetResolution(1280,720,false);yield return new WaitForSecondsRealtime(.45f);yield return new WaitForEndOfFrame();CaptureRuntime("polish-02-dossier-720");
        Screen.SetResolution(width,height,false);yield return new WaitForSecondsRealtime(.45f);
        yield return ModalValidationKey(KeyCode.Escape);Require(!TrialDossierOpen&&lobby,"escape closes scout without exiting preparation");
        yield return SoloLobbyClick(wave28);yield return SoloLobbyClick(TrialEnterRect);
        Require(!TrialDossierOpen&&lobby&&!battling,"outside click closes dossier without activating underlying start");
        Require(round==savedRound&&gold==savedGold&&trialDifficulty==mode&&PortablePreviewPrefs.GetString(TrialSaveKey(mode))==save,"all scouting leaves exact run state and stored save unchanged");
        Require(UnityEngine.Random.state.Equals(random),"actual dossier browsing consumes no gameplay RNG");
    }
    IEnumerator ValidateTrialDossierFieldInput()
    {
        int storedGold=gold,storedRound=round;var storedBoard=board.ToArray();Vector2 target=legendTarget;
        showCombatReport=false;
        yield return SoloLobbyClick(new Rect(1699,646,192,34));
        Require(TrialDossierOpen&&!lobby,"field HUD opens the encounter dossier");
        yield return SoloLobbyClick(TrialDossierCellRect(3,3));
        Require(legendTarget==target,"clicking the dossier board cannot move the tamer underneath");
        Require(!TryMoveLegend(new Vector2(930,565)),"direct touch movement is blocked by the dossier");
        yield return ModalValidationKey(KeyCode.Return);yield return ModalValidationKey(KeyCode.D);yield return ModalValidationKey(KeyCode.F);
        Require(!battling&&gold==storedGold&&round==storedRound&&board.SequenceEqual(storedBoard),"field dossier blocks battle, shop, and experience shortcuts");
        yield return SoloLobbyClick(TrialDossierCloseRect);
        Require(!TrialDossierOpen&&!lobby&&legendTarget==target,"dossier close returns to same field without leaking mouse input");
    }
}
#endif
