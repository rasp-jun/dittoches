#if DITTOCHES_PORTABLE_PREVIEW
using PlayerPrefs = PortablePreviewPrefs;
#endif
using System;
using UnityEngine;

public sealed partial class NativeGame
{
    public enum TrialRoundOutcome { None, Victory, Defeat, Timeout, Interrupted }

    [Serializable] public sealed class TrialRoundSummary
    {
        public int stage;
        public TrialRoundOutcome outcome;
        public float elapsed;
        public bool elapsedKnown;
        // XP is the awarded amount, not the remaining XP bar after a level-up.
        public int goldGained,xpAwarded,item=-1,scoreGained,livesLost,levelBefore,levelAfter;
    }

    [Serializable] public sealed class TrialRecord
    {
        public int version=1,bestScore,furthestStage,bestWins;
        public bool cleared;
        public bool HasRecord {get{return furthestStage>0;}}
    }

    TrialRoundSummary lastTrialRound;
    int[] trialWaveResults=new int[30];
    readonly TrialRecord[] trialRecordCache=new TrialRecord[3];
    public TrialRoundSummary LastTrialRound {get{return lastTrialRound;}}
    public TrialRecord TrialPersonalBest {get{return GetTrialRecord(trialDifficulty);}}
    static string TrialRecordKey(int selectedDifficulty){return "multiTrialsRecordsV1_"+Mathf.Clamp(selectedDifficulty,0,2);}

    public int TrialWaveResult(int stage)
    {return stage>=1&&stage<=30&&trialWaveResults!=null&&trialWaveResults.Length==30?trialWaveResults[stage-1]:0;}

    public static string TrialOutcomeLabel(TrialRoundOutcome outcome)
    {
        switch(outcome)
        {
            case TrialRoundOutcome.Victory:return "시험 승리";
            case TrialRoundOutcome.Defeat:return "시험 패배";
            case TrialRoundOutcome.Timeout:return "시간 초과";
            case TrialRoundOutcome.Interrupted:return "전투 중단";
            default:return "기록 없음";
        }
    }

    static bool ValidTrialRecord(TrialRecord record)
    {
        return record!=null&&record.version==1&&record.bestScore>=0&&record.furthestStage>=0&&record.furthestStage<=30
            &&record.bestWins>=0&&record.bestWins<=record.furthestStage&&(!record.cleared||record.furthestStage==30)
            &&(record.furthestStage>0||(record.bestScore==0&&record.bestWins==0&&!record.cleared));
    }

    public TrialRecord GetTrialRecord(int selectedDifficulty)
    {
        int index=Mathf.Clamp(selectedDifficulty,0,2);
        if(trialRecordCache[index]!=null)return trialRecordCache[index];
        TrialRecord record=null;
        try
        {
            string key=TrialRecordKey(index);
            if(PlayerPrefs.HasKey(key))record=JsonUtility.FromJson<TrialRecord>(PlayerPrefs.GetString(key));
        }
        catch(Exception){/* A damaged optional record must not prevent loading a valid run. */}
        trialRecordCache[index]=ValidTrialRecord(record)?record:new TrialRecord();
        return trialRecordCache[index];
    }

    void ClearTrialRecordCache(){Array.Clear(trialRecordCache,0,trialRecordCache.Length);}

    void UpdateTrialPersonalBest()
    {
        // Called only after settlement, or to migrate an already finished legacy run.
        var best=GetTrialRecord(trialDifficulty);
        int score=Mathf.Max(best.bestScore,trialScore),stage=Mathf.Max(best.furthestStage,TrialStage),wins=Mathf.Max(best.bestWins,trialCleared);
        bool complete=best.cleared||TrialComplete;
        if(score==best.bestScore&&stage==best.furthestStage&&wins==best.bestWins&&complete==best.cleared)return;
        best=new TrialRecord{bestScore=score,furthestStage=stage,bestWins=wins,cleared=complete};
        trialRecordCache[trialDifficulty]=best;
        // SaveTrial flushes the record together with the just-settled run.
        PlayerPrefs.SetString(TrialRecordKey(trialDifficulty),JsonUtility.ToJson(best));
    }

    void ResetTrialRoundRecords(){lastTrialRound=null;trialWaveResults=new int[30];}

    static bool ValidTrialRoundSummary(TrialRoundSummary summary,TrialSave save)
    {
        if(summary==null||summary.stage<1||summary.stage>30||summary.stage!=save.game.round-1
            ||summary.outcome<TrialRoundOutcome.Victory||summary.outcome>TrialRoundOutcome.Interrupted
            ||float.IsNaN(summary.elapsed)||float.IsInfinity(summary.elapsed)||summary.elapsed<0
            ||summary.levelBefore<1||summary.levelBefore>9||summary.levelAfter<summary.levelBefore||summary.levelAfter>9
            ||summary.scoreGained<0||summary.scoreGained>save.score)return false;
        var wave=TrialsCatalog.Get(summary.stage);bool victory=summary.outcome==TrialRoundOutcome.Victory;
        if(summary.elapsed>wave.timeLimit||(!summary.elapsedKnown&&summary.outcome!=TrialRoundOutcome.Interrupted)
            ||summary.xpAwarded!=(victory?wave.xp:1)||summary.item!=(victory?wave.item:-1)
            ||summary.livesLost!=(victory?0:1)||summary.livesLost>3-save.lives)return false;
        if(victory)return save.cleared>0&&summary.goldGained>=wave.gold&&summary.goldGained<=wave.gold+5;
        return !save.complete&&summary.goldGained==3&&summary.scoreGained==0
            &&(summary.outcome!=TrialRoundOutcome.Timeout||summary.elapsed==wave.timeLimit);
    }

    void RestoreTrialRoundRecords(TrialSave save)
    {
        ResetTrialRoundRecords();
        if(save.waveResults!=null&&save.waveResults.Length==30)
        {
            int wins=0,losses=0;
            for(int i=0;i<Mathf.Min(30,save.game.round-1);i++)
            {
                int result=save.waveResults[i];
                if(result!=1&&result!=-1)continue;
                trialWaveResults[i]=result;if(result==1)wins++;else losses++;
            }
            // Inconsistent optional history is discarded; the underlying run is retained.
            if(wins>save.cleared||losses>3-save.lives||(save.complete&&trialWaveResults[29]==-1))trialWaveResults=new int[30];
        }
        if(ValidTrialRoundSummary(save.lastRound,save))
        {
            lastTrialRound=save.lastRound;
            trialWaveResults[lastTrialRound.stage-1]=lastTrialRound.outcome==TrialRoundOutcome.Victory?1:-1;
        }
        int knownWins=0,knownLosses=0;
        foreach(int result in trialWaveResults){if(result==1)knownWins++;else if(result==-1)knownLosses++;}
        if(knownWins>save.cleared||knownLosses>3-save.lives)
        {
            trialWaveResults=new int[30];
            if(lastTrialRound!=null)trialWaveResults[lastTrialRound.stage-1]=lastTrialRound.outcome==TrialRoundOutcome.Victory?1:-1;
        }
        // Missing fields in V1 saves mean unknown outcomes, never inferred victories.
    }

    void CaptureTrialRoundRecord(TrialsCatalog.Wave wave,bool victory,float elapsed,bool elapsedKnown,TrialRoundOutcome reason,
        int goldBefore,int scoreBefore,int livesBefore,int levelBefore)
    {
        bool finite=!float.IsNaN(elapsed)&&!float.IsInfinity(elapsed);
        var outcome=victory?TrialRoundOutcome.Victory:reason==TrialRoundOutcome.Interrupted?TrialRoundOutcome.Interrupted:
            elapsed>=wave.timeLimit?TrialRoundOutcome.Timeout:TrialRoundOutcome.Defeat;
        lastTrialRound=new TrialRoundSummary{stage=wave.number,outcome=outcome,elapsed=finite?Mathf.Clamp(elapsed,0,wave.timeLimit):0,
            elapsedKnown=elapsedKnown&&finite,goldGained=gold-goldBefore,xpAwarded=victory?wave.xp:1,item=victory?wave.item:-1,
            scoreGained=trialScore-scoreBefore,livesLost=livesBefore-trialLives,levelBefore=levelBefore,levelAfter=level};
        if(trialWaveResults==null||trialWaveResults.Length!=30)trialWaveResults=new int[30];
        trialWaveResults[wave.number-1]=victory?1:-1;
    }
}
