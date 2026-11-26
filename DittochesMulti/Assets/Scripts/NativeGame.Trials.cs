#if DITTOCHES_PORTABLE_PREVIEW
using PlayerPrefs = PortablePreviewPrefs;
#endif
using System;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    [Serializable] sealed class TrialSave
    {
        public int version=1,difficulty,lives=3,score,cleared;public bool finished,complete,inBattle;
        public SoloSave game;
        public TrialRoundSummary lastRound;
        public int[] waveResults;
    }
    bool trialsMode,trialFinished,trialComplete,trialBattlePending;
    int trialDifficulty,trialLives=3,trialScore,trialCleared;
    public bool TrialsMode {get{return trialsMode;}}
    public TrialsCatalog.Wave CurrentTrial {get{return TrialsCatalog.Get(TrialStage);}}
    public int TrialLives {get{return trialLives;}}
    public int TrialScore {get{return trialScore;}}
    public int TrialStage {get{return Mathf.Clamp(round-(trialFinished?1:0),1,30);}}
    public int TrialDifficulty {get{return trialDifficulty;}}
    public int TrialCleared {get{return trialCleared;}}
    public bool TrialComplete {get{return trialFinished&&trialComplete;}}
    public bool TrialFailed {get{return trialFinished&&!trialComplete;}}
    public bool TrialFinished {get{return trialFinished;}}
    static string TrialSaveKey(int selectedDifficulty){return "multiTrialsStateV1_"+Mathf.Clamp(selectedDifficulty,0,2);}
    static bool ValidTrialSave(TrialSave save)
    {
        return save!=null&&save.version==1&&save.difficulty>=0&&save.difficulty<3&&save.lives>=0&&save.lives<=3&&save.game!=null
            &&save.game.board!=null&&save.game.board.Length==28&&save.game.bench!=null&&save.game.bench.Length==9
            &&save.game.shop!=null&&save.game.shop.Length==5&&save.game.round>=1&&save.game.round<=31
            &&save.cleared>=0&&save.cleared<=save.game.round-1&&save.score>=0
            &&save.finished==(save.lives==0||save.game.round==31)&&(!save.finished||save.game.round>1)
            &&(!save.complete||(save.finished&&save.lives>0&&save.game.round==31))&&(!save.finished||!save.inBattle);
    }
    public bool HasTrialSave(int selectedDifficulty)
    {
        string key=TrialSaveKey(selectedDifficulty);if(!PlayerPrefs.HasKey(key))return false;
        try{var data=JsonUtility.FromJson<TrialSave>(PlayerPrefs.GetString(key));return ValidTrialSave(data)&&data.difficulty==Mathf.Clamp(selectedDifficulty,0,2);}
        catch{return false;}
    }
    public void EnterTrials(int selectedDifficulty,bool resume=true)
    {
        if(battling)return;
        Save();trialsMode=true;trialDifficulty=Mathf.Clamp(selectedDifficulty,0,2);artPack=0;
        ClearTrialPresentation();
        if(!resume||!LoadTrial())ResetTrialCore();
        lobby=true;ApplyEquippedLoadout();
    }
    public void EnterSoloMode()
    {
        if(battling||!trialsMode)return;
        SaveTrial();trialsMode=false;ClearTrialPresentation();
        if(!Load())ResetGame();lobby=true;ApplyEquippedLoadout();
    }
    public void RestartTrials()
    {
        if(battling)return;trialsMode=true;artPack=0;ResetTrialCore();lobby=false;
    }
    void ClearTrialPresentation()
    {
        CloseTrialDossier();
        nativeFinisher=null;finisherStarted=-10;ReturnToOwnField();rivalFormations.Clear();rivalFormationRound=-1;
        promotions.Clear();battleTraces.Clear();traitPage=inventoryPage=0;formationPositions.Clear();healthTrails.Clear();
        lastBattleReport.Clear();fighters.Clear();skillCasts.Clear();combatPopups.Clear();showCombatReport=true;
        placementNoticeUntil=0;showCarousel=false;showTeamPlan=false;shopLocked=false;confirmSoloReset=false;confirmTrialReset=false;
        resultNoticeUntil=0;battleProgress=0;lastReward="";battleText="전투 준비";lastCombatSummary="";currentOpponent="";
    }
    void TrialOnDisable()
    {
        // SetActive(false) stops the Battle coroutine. Settle a pending trial once before it can be stranded.
        if(trialsMode&&trialBattlePending){FinishTrialBattle(false,Mathf.Clamp(CurrentTrial.timeLimit-battleTimeRemaining,0,CurrentTrial.timeLimit),TrialRoundOutcome.Interrupted);}
    }
    void ResetTrialCore()
    {
        ClearTrialPresentation();Array.Clear(board,0,board.Length);Array.Clear(bench,0,bench.Length);Array.Clear(shop,0,shop.Length);
        inventory.Clear();inventory.AddRange(new[]{0,1});lootOrbs.Clear();
        trialLives=3;trialScore=trialCleared=0;trialFinished=trialComplete=trialBattlePending=false;
        ResetTrialRoundRecords();
        gold=12;hp=100;level=3;xp=0;round=1;selectedBench=selectedBoard=selectedItem=-1;
        board[2]=new Unit(RosterById["koromon"]);board[4]=new Unit(RosterById["nyaromon"]);board[17]=new Unit(RosterById["tsunomon"]);
        inspectedUnit=null;legendPos=legendTarget=new Vector2(785,690);legendVelocity=Vector2.zero;
        RebuildPool();RollShop();lastReward="12G · 시작 재료 2개 · 목숨 3개";battleText="아구몬박사의 시험 · 편성을 준비하세요";SaveTrial();
    }
    SoloSave CaptureTrialFormation()
    {
        return new SoloSave{version=2,difficulty=difficulty,legend=legend,gold=gold,hp=hp,level=level,xp=xp,round=round,shopLocked=shopLocked,
            board=board.Select(SaveUnit).ToArray(),bench=bench.Select(SaveUnit).ToArray(),shop=shop.Select(u=>u==null?"":u.id).ToArray(),inventory=inventory.ToArray(),lootOrbs=new LootOrbSave[0]};
    }
    void SaveTrial()
    {
        if(!trialsMode)return;
        var save=new TrialSave{difficulty=trialDifficulty,lives=trialLives,score=trialScore,cleared=trialCleared,
            finished=trialFinished,complete=trialComplete,inBattle=trialBattlePending,game=CaptureTrialFormation(),lastRound=lastTrialRound,waveResults=trialWaveResults};
        PlayerPrefs.SetString(TrialSaveKey(trialDifficulty),JsonUtility.ToJson(save));PlayerPrefs.Save();
    }
    bool LoadTrial()
    {
        string key=TrialSaveKey(trialDifficulty);if(!PlayerPrefs.HasKey(key))return false;
        try
        {
            var data=JsonUtility.FromJson<TrialSave>(PlayerPrefs.GetString(key));
            if(!ValidTrialSave(data)||data.difficulty!=trialDifficulty)return false;
            var saved=data.game;trialLives=data.lives;trialScore=data.score;trialCleared=data.cleared;
            trialFinished=data.finished;trialComplete=data.complete;trialBattlePending=data.inBattle;
            gold=Mathf.Max(0,saved.gold);level=Mathf.Clamp(saved.level,1,9);xp=Mathf.Max(0,saved.xp);round=saved.round;shopLocked=saved.shopLocked;
            hp=trialFinished?0:Mathf.CeilToInt(trialLives*100f/3);legend=Mathf.Clamp(saved.legend,0,TamerLoadout.Tamers.Length-1);
            for(int i=0;i<board.Length;i++)board[i]=LoadUnit(saved.board[i]);
            for(int i=0;i<bench.Length;i++)bench[i]=LoadUnit(saved.bench[i]);
            for(int i=0;i<shop.Length;i++){UnitDef def;shop[i]=saved.shop[i]!=null&&RosterById.TryGetValue(saved.shop[i],out def)?def:null;}
            inventory.Clear();if(saved.inventory!=null)inventory.AddRange(saved.inventory.Where(i=>i>=0&&i<DigimonBuildCatalog.Data.items.Length));
            lootOrbs.Clear();RebuildPool();
            RestoreTrialRoundRecords(data);
            if(trialBattlePending&&!trialFinished){ResolveTrialOutcome(false,0,TrialRoundOutcome.Interrupted,false);}
            else if(trialFinished)battleText=trialComplete?"시험 통과":"시험 종료";
            else battleText=CurrentTrial.name+" · 준비";
            // Finished legacy saves contain reliable totals, but no individual battle history.
            if(trialFinished){UpdateTrialPersonalBest();PlayerPrefs.Save();}
            return true;
        }
        catch(Exception error){Debug.LogWarning("Trial save could not be loaded: "+error.Message);return false;}
    }
    void BeginTrialBattle()
    {
        if(!trialsMode||trialFinished)return;trialBattlePending=true;SaveTrial();
    }
    void SetupTrialEnemies()
    {
        currentOpponent=CurrentTrial.name;
        foreach(var entry in CurrentTrial.enemies)
        {
            UnitDef def;if(!RosterById.TryGetValue(entry.id,out def))
            {
                string name=TrialEnemyName(entry.id);
                def=new UnitDef(entry.id,name,1,char.ToUpperInvariant(entry.id[0])+entry.id.Substring(1),"적");
            }
            fighters.Add(CreateFighter(new Unit(def){star=entry.star},true,new Vector2(entry.col,entry.row)));
        }
    }
    void ApplyTrialEnemyModifiers()
    {
        var enemies=fighters.Where(f=>f.enemy).ToArray();
        for(int i=0;i<enemies.Length;i++)
        {
            var f=enemies[i];var profile=CurrentTrial.enemies[i];
            f.maxHp*=profile.healthScale*TrialsCatalog.HealthScale[trialDifficulty];f.hp=f.maxHp;
            f.attackScale=profile.attackScale*TrialsCatalog.AttackScale[trialDifficulty];
            f.build.armor+=profile.armor;f.build.magicResist+=profile.magicResist;f.build.speed+=profile.speed;
            Shield(f,f,f.maxHp*profile.shield);
        }
    }
    bool TrialCastCanHit(SkillCast cast)
    {
        if(!cast.released||cast.cancelled||cast.hits>=cast.skill.shots)return false;
        // Match released-hit targeting, including the next update's homing aim. A dead
        // caster's projectile can still hit; a projectile with no living victim cannot.
        var skill=cast.skill;var aim=cast.aim;
        if(cast.hits==0&&cast.target!=null&&!cast.target.dead&&(skill.shape=="single"||skill.shape=="splash"))aim=cast.target.pos;
        return fighters.Any(f=>!f.dead&&f.enemy!=cast.caster.enemy&&(skill.shape=="single"?f==cast.target:
            SkillGeometry.Contains(skill.shape,cast.origin.x,cast.origin.y,aim.x,aim.y,f.pos.x,f.pos.y,skill.radius,skill.reach)));
    }
    bool TrialBattleShouldContinue(float elapsed)
    {
        if(elapsed>=CurrentTrial.timeLimit)return false;
        return (fighters.Any(f=>!f.enemy&&!f.dead)&&fighters.Any(f=>f.enemy&&!f.dead))||skillCasts.Any(TrialCastCanHit);
    }
    bool TrialBattleWon(float elapsed)
    {return elapsed<CurrentTrial.timeLimit&&fighters.Any(f=>!f.enemy&&!f.dead)&&!fighters.Any(f=>f.enemy&&!f.dead)&&!skillCasts.Any(TrialCastCanHit);}
    void ResolveTrialOutcome(bool victory,float elapsed,TrialRoundOutcome reason=TrialRoundOutcome.None,bool elapsedKnown=true)
    {
        if(!trialsMode||!trialBattlePending||trialFinished)return;
        var wave=CurrentTrial;trialBattlePending=false;int income;
        int goldBefore=gold,scoreBefore=trialScore,livesBefore=trialLives,levelBefore=level;
        if(victory)
        {
            income=wave.gold+Mathf.Min(5,gold/10);gold+=income;AddXp(wave.xp);trialCleared++;
            if(wave.item>=0)inventory.Add(wave.item);
            int earned=Mathf.RoundToInt((100+wave.number*20+(wave.boss?200:0)+Mathf.Max(0,wave.timeLimit-elapsed)*3)*TrialsCatalog.ScoreScale[trialDifficulty]);
            trialScore+=earned;lastReward=income+"G · 경험치 +"+wave.xp+(wave.item>=0?" · "+ItemNames[wave.item]:"")+" · 점수 +"+earned;
        }
        else
        {
            trialLives=Mathf.Max(0,trialLives-1);income=3;gold+=income;AddXp(1);
            lastReward="목숨 -1 · 남은 목숨 "+trialLives+" · 3G · 경험치 +1";
        }
        round=Mathf.Min(31,round+1);trialFinished=trialLives==0||round>30;trialComplete=trialFinished&&trialLives>0&&wave.number==30&&victory;
        if(trialComplete){int bonus=Mathf.RoundToInt(trialLives*250*TrialsCatalog.ScoreScale[trialDifficulty]);trialScore+=bonus;lastReward+=" · 생존 보너스 +"+bonus;}
        hp=trialFinished?0:Mathf.CeilToInt(trialLives*100f/3);if(!trialFinished&&!shopLocked)RollShop();
        CaptureTrialRoundRecord(wave,victory,elapsed,elapsedKnown,reason,goldBefore,scoreBefore,livesBefore,levelBefore);
        UpdateTrialPersonalBest();
        battleText=TrialOutcomeLabel(lastTrialRound.outcome)+" · "+lastReward;SaveTrial();
    }
    void FinishTrialBattle(bool victory,float elapsed,TrialRoundOutcome reason=TrialRoundOutcome.None)
    {
        if(!trialsMode||!trialBattlePending||trialFinished)return;
        win=victory;ResolveTrialOutcome(victory,elapsed,reason);resultNoticeUntil=Time.unscaledTime+5;
        lastBattleReport.Clear();lastBattleReport.AddRange(fighters.Where(f=>!f.enemy).Select(f=>f.Snapshot()));
        var ace=lastBattleReport.OrderByDescending(f=>f.damageDone).FirstOrDefault();
        lastCombatSummary=ace==null?"":"시험 통계 · 피해 1위 "+UnitName(ace.unit.def)+" "+ace.damageDone.ToString("0")+" · 기술 "+lastBattleReport.Sum(f=>f.casts)+"회";
        foreach(var survivor in fighters.Where(f=>!f.enemy&&!f.dead&&f.sourceSlot>=0))
        {var owner=board[survivor.sourceSlot];if(owner!=null)formationPositions[owner]=TacticalArena.CellWorld(survivor.renderPos.x,survivor.renderPos.y);}
        fighters.Clear();skillCasts.Clear();battling=false;SaveTrial();
    }
}
