#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    [Serializable] sealed class TrialCombatProof
    {
        public int difficulty,wave,allies,enemies;public string team;public bool won,economyBounded;
        public int equipmentCount,equipmentAvailableBeforeFinal,unitPurchaseGold,levelPurchaseGold,guaranteedGoldBeforeFinal,uncommittedGoldBeforeInterest;
        public float seconds,ownRemaining,enemyRemaining,damage,shielding;
    }
    [Serializable] sealed class TrialProofDocument
    {public string purpose="Actual NativeGame.UpdateCombat at 30 Hz. Opening uses starter team; beginner/standard final use 9 two-star units and 12 equipment from exact pre-final rewards; chaos final uses a three-star upper-bound team. Budget feasibility is not a guarantee of random shop acquisition or a progression win-rate study.";public TrialCombatProof[] combats;}
    [Serializable] sealed class TrialEnemySignature {public TrialsCatalog.Enemy[] enemies;}
    public void BeginTrialsSmoke(){StartCoroutine(TrialsSmoke());}
    int[] TrialFinalEquipmentBudget()
    {
        var rewards=new List<int>{0,1};rewards.AddRange(TrialsCatalog.Waves.Take(29).Where(w=>w.item>=0).Select(w=>w.item));
        foreach(var pair in new[]{new[]{0,0},new[]{0,0},new[]{1,1},new[]{1,1},new[]{2,2},new[]{2,3},new[]{3,3}})
        {
            Require(rewards.Remove(pair[0])&&rewards.Remove(pair[1]),"pre-final wave rewards supply both actual crafting components");
            int gear=DigimonBuildCatalog.Combine(pair[0],pair[1]);Require(gear>=4,"fixture craft is a valid recipe");rewards.Add(gear);
        }
        Require(rewards.All(i=>!DigimonBuildCatalog.IsComponent(i)),"fixture accounts for every rewarded component");return rewards.ToArray();
    }
    void TrialValidationFormation(bool final,bool economy=false)
    {
        Array.Clear(board,0,board.Length);Array.Clear(bench,0,bench.Length);inventory.Clear();
        if(!final)
        {
            level=3;board[2]=new Unit(RosterById["koromon"]);board[4]=new Unit(RosterById["nyaromon"]);board[17]=new Unit(RosterById["tsunomon"]);
        }
        else
        {
            level=9;string[] ids={"wargreymon","weregarurumon","herakle","ophanimon","seraphimon","angewomon","metalgarurumon","rosemon","holyangemon"};
            int[] slots={2,3,4,16,17,18,21,24,27};
            for(int i=0;i<ids.Length;i++)
            {
                var unit=new Unit(RosterById[ids[i]]){star=economy?2:3};
                if(!economy)unit.items.AddRange(i<3?new[]{8,4}:new[]{12,13});
                else if(i==0||i==1)unit.items.AddRange(new[]{8,4});
                else if(i==2)unit.items.AddRange(new[]{8,10});
                else if(i==3||i==4)unit.items.AddRange(new[]{12,13});
                else if(i==5)unit.items.Add(6);else if(i==6)unit.items.Add(11);
                board[slots[i]]=unit;
            }
        }
        RebuildPool();
    }
    TrialCombatProof SimulateTrialValidation(int mode,bool final)
    {
        trialDifficulty=mode;round=final?30:1;trialFinished=trialComplete=trialBattlePending=false;trialLives=3;hp=100;artPack=0;trialsMode=true;
        bool economy=final&&mode<2;
        TrialValidationFormation(final,economy);UnityEngine.Random.InitState(711+mode);SetupBattle();
        var proof=new TrialCombatProof{difficulty=mode,wave=round,allies=board.Count(u=>u!=null),enemies=CurrentTrial.enemies.Length,
            team=string.Join(",",board.Where(u=>u!=null).Select(u=>u.def.id+":"+u.star+":"+string.Join("/",u.items.Select(i=>i.ToString()).ToArray())).ToArray())};
        if(final)
        {
            proof.economyBounded=economy;proof.equipmentCount=board.Where(u=>u!=null).Sum(u=>u.items.Count);
            var available=TrialFinalEquipmentBudget();proof.equipmentAvailableBeforeFinal=available.Length;proof.unitPurchaseGold=board.Where(u=>u!=null).Sum(u=>u.def.cost*(int)Mathf.Pow(3,u.star-1));
            proof.levelPurchaseGold=200-TrialsCatalog.Waves.Take(29).Sum(w=>w.xp);
            proof.guaranteedGoldBeforeFinal=12+TrialsCatalog.Waves.Take(29).Sum(w=>w.gold);
            proof.uncommittedGoldBeforeInterest=proof.guaranteedGoldBeforeFinal-proof.unitPurchaseGold-proof.levelPurchaseGold;
            if(economy)
            {
                Require(proof.equipmentCount<=12&&proof.uncommittedGoldBeforeInterest>=50,"standard final fixture fits guaranteed currency with reroll allowance");
                Require(board.Where(u=>u!=null).SelectMany(u=>u.items).OrderBy(i=>i).SequenceEqual(available.OrderBy(i=>i)),"final equipment is exactly craftable from earned materials and first five boss drops");
            }
        }
        float age=0;
        while(TrialBattleShouldContinue(age)){age+=1f/30;UpdateCombat(1f/30);}
        proof.seconds=age;proof.won=TrialBattleWon(age);proof.ownRemaining=fighters.Where(f=>!f.enemy).Sum(f=>f.hp);proof.enemyRemaining=fighters.Where(f=>f.enemy).Sum(f=>f.hp);
        proof.damage=fighters.Where(f=>!f.enemy).Sum(f=>f.damageDone);proof.shielding=fighters.Where(f=>!f.enemy).Sum(f=>f.shieldingDone);
        Require(proof.won,(final?(economy?"economy-bounded two-star final team":"three-star chaos ceiling team"):"unmodified starter team")+" wins actual simulation at difficulty "+mode+" (time="+age+", own="+proof.ownRemaining+", enemy="+proof.enemyRemaining+")");
        Require(proof.damage>0&&fighters.All(f=>f.hp>=0&&f.hp<=f.maxHp+.1f&&f.shield<=f.maxHp*.5f+.1f),"trial simulation has actual bounded damage/shields");
        Require(Mathf.Abs(fighters.Sum(f=>f.damageDone)-fighters.Sum(f=>f.damageTaken))<1,"trial combat damage accounting");
        return proof;
    }
    void ValidateTrialCombatEnding()
    {
        trialsMode=true;trialDifficulty=1;round=1;trialFinished=trialComplete=false;
        fighters.Clear();skillCasts.Clear();
        var shooter=CreateFighter(new Unit(RosterById["koromon"]),false,new Vector2(2,4));
        var finisher=CreateFighter(new Unit(RosterById["agumon"]),false,new Vector2(3,4));
        var enemy=CreateFighter(new Unit(RosterById["koromon"]),true,new Vector2(3,3));
        fighters.Add(shooter);fighters.Add(finisher);fighters.Add(enemy);InitializeBuildBonuses();
        enemy.hp=1;enemy.shield=0;finisher.mana=0;finisher.cooldown=0;
        Require(StartDigimonSkill(shooter,enemy),"deadline fixture launches a real allied multi-shot skill");
        UpdateDigimonSkills(shooter.skillCast.skill.windup+.001f);
        float elapsed=CurrentTrial.timeLimit-.01f;elapsed+=.005f;UpdateCombat(.005f);
        Require(enemy.dead&&skillCasts.Any(c=>c.released&&c.hits<c.skill.shots),"last enemy dies before deadline with allied shots still in flight");
        Require(!TrialBattleShouldContinue(elapsed)&&TrialBattleWon(elapsed),"harmless remaining allied shots cannot turn a timely clear into timeout");

        fighters.Clear();skillCasts.Clear();
        var survivor=CreateFighter(new Unit(RosterById["agumon"]),false,new Vector2(3,4));
        enemy=CreateFighter(new Unit(RosterById["koromon"]),true,new Vector2(3,3));
        fighters.Add(survivor);fighters.Add(enemy);InitializeBuildBonuses();
        survivor.hp=enemy.hp=1;survivor.shield=enemy.shield=0;survivor.mana=0;survivor.cooldown=0;
        Require(StartDigimonSkill(enemy,survivor),"retaliation fixture launches a real enemy multi-shot skill");
        var incoming=enemy.skillCast;UpdateDigimonSkills(incoming.skill.windup+.001f);UpdateCombat(.005f);
        Require(enemy.dead&&!survivor.dead&&TrialBattleShouldContinue(10)&&!TrialBattleWon(10),"released enemy damage still blocks premature victory after its caster dies");
        Require(!TrialBattleShouldContinue(CurrentTrial.timeLimit)&&!TrialBattleWon(CurrentTrial.timeLimit),"deadline still ends unresolved harmful projectiles as a loss");
        UpdateCombat(incoming.skill.Duration);
        Require(survivor.dead&&enemy.dead&&!TrialBattleShouldContinue(11)&&!TrialBattleWon(11),"released enemy damage can cause mutual defeat and never a trial victory");

        var remaining=CreateFighter(new Unit(RosterById["agumon"]),false,new Vector2(2,4));fighters.Add(remaining);
        incoming.hits=0;incoming.released=true;skillCasts.Add(incoming);
        Require(!TrialBattleShouldContinue(12)&&TrialBattleWon(12),"locked single-target shots at a dead ally cannot delay another surviving ally's victory");
        fighters.Clear();skillCasts.Clear();combatPopups.Clear();
    }
    void ValidateTrialsRules()
    {
        Require(TrialsCatalog.Waves.SelectMany(w=>w.enemies).Select(e=>e.id).Distinct().Count()==12,"twelve enemy species across the trials");
        Require(TrialsCatalog.Waves.Where(w=>w.boss).Select(w=>w.enemies[0].id).Distinct().Count()==6,"six distinct boss species");
        Require(TrialsCatalog.Waves.Select(w=>string.Join(",",w.enemies.Select(e=>e.id).OrderBy(id=>id).ToArray())).Distinct().Count()==30,"all thirty species compositions differ even without cells and modifiers");
        foreach(string id in TrialsCatalog.NewEnemyIds)
        {
            string attribute=id=="monochromon"||id=="meramon"?"데이터":id=="leomon"||id=="andromon"?"백신":"바이러스";
            Require(Meta(id).attr==attribute&&Meta(id).family!="악당","enemy inspection uses its explicit attribute and species "+id);
            Require(RosterById.ContainsKey(id)&&TeamPlan.Roster.Any(u=>u.id==id&&u.cost==RosterById[id].cost),"encounter species is recruitable at its true cost: "+id);
            Require(TrialsCatalog.Waves.Any(w=>w.enemies.Any(e=>e.id==id))&&Array.IndexOf(DigimonModelLibrary.Roster,id)>=0,"new enemy has actual encounter and render model: "+id);
            for(int star=1;star<=3;star++)
            {
                artPack=0;fighters.Clear();skillCasts.Clear();
                var caster=CreateFighter(new Unit(new UnitDef(id,TrialEnemyName(id),1,id,"적")){star=star},true,new Vector2(3,3));
                var victim=CreateFighter(new Unit(RosterById["koromon"]),false,new Vector2(3,4));
                fighters.Add(caster);fighters.Add(victim);InitializeBuildBonuses();
                victim.maxHp=victim.hp=100000;victim.build=new DigimonBuildCatalog.Bonus();victim.shield=0;
                var skill=DigimonSkillCatalog.Find(id);float expected=skill.Damage(star,AttackDamage(caster),100*caster.attackScale);
                Require(StartDigimonSkill(caster,victim),"enemy technique starts "+id);
                UpdateDigimonSkills(skill.windup+.001f);Require(victim.hp==100000,"enemy damage waits for travel "+id);
                UpdateDigimonSkills(skill.Duration);Require(Mathf.Abs((100000-victim.hp)-expected)<.03f,"all enemy projectiles deal exact catalog total "+id+":"+star);
                Require(Mathf.Abs(caster.skillDamageDone-expected)<.03f&&caster.basicDamageDone==0,"enemy combat report damage is exact "+id);
            }
        }
        fighters.Clear();skillCasts.Clear();combatPopups.Clear();
        Require(TrialsCatalog.Waves.Length==30&&TrialsCatalog.Waves.Count(w=>w.boss)==6,"thirty waves with six bosses");
        Require(TrialsCatalog.Waves.Select(w=>w.name).Distinct().Count()==30,"every trial has a distinct named encounter");
        var signatures=new HashSet<string>();
        foreach(var wave in TrialsCatalog.Waves)
        {
            Require(wave.number>=1&&wave.number<=30&&wave.chapter==(wave.number-1)/5+1&&wave.boss==(wave.number%5==0),"wave order and boss cadence "+wave.number);
            Require(wave.timeLimit>=45&&wave.timeLimit<=60&&wave.gold>0&&wave.xp>0&&wave.item<28,"wave timer and reward catalog "+wave.number);
            Require(wave.enemies.Length>=2&&wave.enemies.Length<=7&&wave.enemies.Select(e=>e.col+e.row*7).Distinct().Count()==wave.enemies.Length,"valid distinct enemy seats "+wave.number);
            Require(signatures.Add(JsonUtility.ToJson(new TrialEnemySignature{enemies=wave.enemies})),"unique enemy composition and modifiers "+wave.number);
            foreach(var enemy in wave.enemies)Require(DigimonSkillCatalog.Find(enemy.id)!=null&&enemy.col>=0&&enemy.col<7&&enemy.row>=0&&enemy.row<4&&enemy.star>=1&&enemy.star<=3,"valid model/skill enemy "+wave.number+" "+enemy.id);
        }
        for(int mode=0;mode<3;mode++)
        {
            trialsMode=true;trialDifficulty=mode;ResetTrialCore();
            for(int n=1;n<=30;n++)
            {
                round=n;trialFinished=trialComplete=false;SetupBattle();var wave=CurrentTrial;var enemies=fighters.Where(f=>f.enemy).ToArray();
                Require(enemies.Length==wave.enemies.Length&&fighters.Count(f=>!f.enemy)==3,"actual trial setup "+mode+":"+n);
                for(int i=0;i<enemies.Length;i++)
                {
                    var profile=wave.enemies[i];var actual=enemies[i];float hp=DigimonSkillCatalog.Find(profile.id).baseHealth*Mathf.Pow(1.8f,profile.star-1)*profile.healthScale*TrialsCatalog.HealthScale[mode]*(1+actual.build.hp);
                    Require(Mathf.Abs(actual.maxHp-hp)<.02f&&Mathf.Abs(actual.attackScale-profile.attackScale*TrialsCatalog.AttackScale[mode])<.0001f,"enemy explicit difficulty scaling "+mode+":"+n);
                    Require(actual.build.armor>=profile.armor&&actual.build.magicResist>=profile.magicResist&&actual.build.speed>=profile.speed,"enemy resistance/speed modifiers "+mode+":"+n);
                    Require(actual.shield<=actual.maxHp*.5f+.01f,"enemy shield cap "+mode+":"+n);
                    var randomBefore=UnityEngine.Random.state;
                    var preview=InspectTrialEnemy(n,i,mode);
                    Require(UnityEngine.Random.state.Equals(randomBefore),"scouting consumes no gameplay randomness");
                    Require(Mathf.Abs(preview.hp-actual.maxHp)<.05f&&Mathf.Abs(preview.attack-AttackDamage(actual))<.01f,"scouted health/attack match actual combat "+mode+":"+n+":"+i);
                    Require(Mathf.Abs(preview.armor-actual.build.armor)<.01f&&Mathf.Abs(preview.magicResist-actual.build.magicResist)<.01f&&Mathf.Abs(preview.shield-actual.shield)<.05f,"scouted defenses/shield match individual combat enemy");
                    Require(Mathf.Abs(preview.speed-DigimonCombatMath.AttackSpeed(Meta(actual.unit.def.id).speed,actual.build.speed,actual.build.rampSpeed,0))<.001f,"scouted starting attack speed matches combat");
                    bool wasBattling=battling;battling=true;
                    try
                    {
                        var detail=InspectStats(actual.unit);
                        Require(Mathf.Abs(detail.health-actual.maxHp)<.01f&&Mathf.Abs(detail.health-preview.hp)<.05f,"boss/escort detail uses actual health multiplier instead of attack multiplier "+mode+":"+n+":"+i);
                        Require(Mathf.Abs(detail.attack-preview.attack)<.01f&&Mathf.Abs(detail.armor-preview.armor)<.01f,"boss/escort detail agrees with scouted attack and armor");
                        Require(Mathf.Abs(DigimonSkillCatalog.Find(actual.unit.def.id).Damage(actual.unit.star,detail.attack,detail.abilityPower)-preview.skillDamage)<.05f,"boss/escort detailed skill matches encounter profile");
                    }
                    finally{battling=wasBattling;}
                    if(n==30&&i==0)
                    {
                        var history=actual.Snapshot();lastBattleReport.Add(history);battling=false;
                        try{Require(Mathf.Abs(InspectStats(history.unit).health-history.maxHp)<.01f,"historical inspection retains encounter health instead of reconstructing attack scale");}
                        finally{lastBattleReport.Remove(history);battling=wasBattling;}
                    }
                }
                int beforeGold=gold,beforeInventory=inventory.Count,priorWins=trialCleared,oldLevel=level;int earned=wave.gold+Mathf.Min(5,gold/10);
                trialBattlePending=true;ResolveTrialOutcome(true,10);
                Require(round==n+1&&gold==beforeGold+earned&&trialCleared==priorWins+1&&trialLives==3,"single win grants income and advances "+mode+":"+n);
                Require(inventory.Count==beforeInventory+(wave.item>=0?1:0)&&level>=oldLevel,"equipment and xp reward "+mode+":"+n);
                int onceGold=gold,onceScore=trialScore;ResolveTrialOutcome(true,10);Require(gold==onceGold&&trialScore==onceScore,"settlement cannot be replayed");
            }
            Require(TrialComplete&&!TrialFailed&&TrialStage==30&&hp==0,"final victory completes and locks run at thirty");
            Require(LoadTrial()&&TrialComplete&&TrialCleared==30,"finished trial roundtrip "+mode);
            ResetTrialCore();for(int loss=1;loss<=3;loss++){int startGold=gold;trialBattlePending=true;ResolveTrialOutcome(false,CurrentTrial.timeLimit);Require(trialLives==3-loss&&gold==startGold+3&&round==loss+1&&trialScore==0,"loss advances once with no victory reward");}
            Require(TrialFailed&&!TrialComplete&&TrialStage==3&&hp==0,"three defeats end on exact failed wave");
            ResetTrialCore();round=30;trialBattlePending=true;ResolveTrialOutcome(false,60);Require(TrialFailed&&trialLives==2&&TrialStage==30,"last boss loss ends even with lives remaining");
        }
        trialDifficulty=1;ResetTrialCore();SetupBattle();
        Require(!TrialBattleWon(CurrentTrial.timeLimit)&&!TrialBattleWon(1),"live enemies cannot be defeated by health comparison or timeout");
        trialBattlePending=true;SaveTrial();Require(LoadTrial()&&round==2&&trialLives==2&&!trialBattlePending,"interrupted combat restores as one loss and advances");
        int resumedGold=gold;Require(LoadTrial()&&round==2&&trialLives==2&&gold==resumedGold,"interruption settlement is idempotent");
        trialBattlePending=true;battling=true;TrialOnDisable();Require(!battling&&round==3&&trialLives==1&&!trialBattlePending,"leaving the active game settles its stopped coroutine once");
        int leaveGold=gold;TrialOnDisable();Require(gold==leaveGold&&trialLives==1,"repeated disable does not repeat loss");
        ValidateTrialCombatEnding();
        var proofs=new List<TrialCombatProof>();for(int mode=0;mode<3;mode++){proofs.Add(SimulateTrialValidation(mode,false));proofs.Add(SimulateTrialValidation(mode,true));}
        File.WriteAllText(Path.Combine(Application.dataPath,"../TrialsCombatEvidence.json"),JsonUtility.ToJson(new TrialProofDocument{combats=proofs.ToArray()},true));
        fighters.Clear();skillCasts.Clear();combatPopups.Clear();
    }
    IEnumerator TrialsSmoke()
    {
        Application.runInBackground=true;yield return null;
        Require(PortablePreview.HasArgument("--trials-smoke"),"trials validation uses isolated portable preference namespace");
        var random=UnityEngine.Random.state;string soloStored=PortablePreviewPrefs.GetString(SaveKey,"");
        string[] stored=new string[3],storedRecords=new string[3];bool[] existed=new bool[3],existedRecords=new bool[3];
        int preferred=PortablePreviewPrefs.GetInt("multi.trials.difficulty",0);bool hadPreferred=PortablePreviewPrefs.HasKey("multi.trials.difficulty");
        for(int i=0;i<3;i++){existed[i]=PortablePreviewPrefs.HasKey(TrialSaveKey(i));stored[i]=PortablePreviewPrefs.GetString(TrialSaveKey(i),"");existedRecords[i]=PortablePreviewPrefs.HasKey(TrialRecordKey(i));storedRecords[i]=PortablePreviewPrefs.GetString(TrialRecordKey(i),"");}
        try
        {
            ValidateTrialsRules();
            ValidateTrialPolishRecords();
            trialsMode=false;PortablePreviewPrefs.SetString(SaveKey,soloStored);Require(Load(),"solo fixture remains loadable after trial rules");
            EnterTrials(0,false);gold=29;board[2].star=2;SaveTrial();string soloAfter=PortablePreviewPrefs.GetString(SaveKey,"");
            EnsureArena();lobby=true;yield return new WaitForSecondsRealtime(.35f);yield return new WaitForEndOfFrame();CaptureRuntime("trials-01-preparation");
            yield return ValidateTrialDossierInput();
            yield return SoloLobbyClick(SoloDifficultyRect(2));Require(TrialsMode&&TrialDifficulty==2&&lobby,"GUI changes trial difficulty and stays in preparation");
            gold=41;SaveTrial();yield return SoloLobbyClick(SoloDifficultyRect(0));
            Require(gold==29&&board[2].star==2&&TrialDifficulty==0,"difficulty has independent saved formation and currency");
            Require(PortablePreviewPrefs.GetString(SaveKey,"")==soloAfter,"trial changes never overwrite solo save");
            yield return SoloLobbyClick(SoloLobbyResetRect);Require(confirmTrialReset,"new trial confirmation opens");
            yield return new WaitForEndOfFrame();CaptureRuntime("trials-06-reset-confirmation");
            yield return SoloLobbyClick(TrialEnterRect);Require(confirmTrialReset&&lobby&&gold==29,"trial confirmation blocks underlying start");
            yield return ModalValidationKey(KeyCode.Escape);Require(!confirmTrialReset&&gold==29,"trial reset escape preserves run");
            yield return SoloLobbyClick(TrialEnterRect);Require(!lobby&&TrialsMode,"GUI enters trial field");
            yield return new WaitForSecondsRealtime(.5f);yield return new WaitForEndOfFrame();CaptureRuntime("trials-02-formation");
            yield return ValidateTrialDossierFieldInput();
            yield return ModalValidationKey(KeyCode.Return);Require(battling&&trialBattlePending,"actual enter starts trial combat and records interruption guard");
            float deadline=Time.unscaledTime+65;while(battling&&Time.unscaledTime<deadline)yield return null;
            Require(!battling&&TrialStage==2&&TrialLives==3&&TrialCleared==1,"actual starter battle wins and advances once");
            showCombatReport=false;resultNoticeUntil=Time.unscaledTime+5;
            Require(LastTrialRound!=null&&LastTrialRound.outcome==TrialRoundOutcome.Victory&&LastTrialRound.stage==1,"actual starter fight creates the visible reward recap");
            yield return new WaitForEndOfFrame();CaptureRuntime("polish-03-round-recap");
            // Capture a real high-tier fight state after its damage/movement update, then settle from a complete actual simulation.
            trialDifficulty=2;round=30;trialFinished=trialComplete=trialBattlePending=false;trialLives=3;hp=100;TrialValidationFormation(true);SetupBattle();
            inspectedUnit=fighters.First(f=>f.enemy).unit;
            Require(InspectionOrigin(inspectedUnit,fighters.First(f=>f.enemy))=="보스","trial boss inspection replaces dummy recruit cost with boss identity");
            Require(InspectionOrigin(fighters.Last(f=>f.enemy).unit,fighters.Last(f=>f.enemy))=="시험 적","trial escort inspection identifies enemy instead of recruit cost");
            Require(InspectionOrigin(board.First(u=>u!=null),null).EndsWith(" G"),"owned recruit still displays its real cost");
            for(int i=0;i<180;i++){UpdateCombat(1f/30);TickCombatPresentation(1f/30);}battling=true;battleTimeRemaining=54;showCombatReport=false;
            yield return new WaitForEndOfFrame();CaptureRuntime("trials-03-boss-combat");
            var boss=fighters.First(f=>f.enemy);var bossStats=InspectStats(boss.unit);
            Require(Mathf.Abs(bossStats.health-boss.maxHp)<.01f,"actual boss modal uses live health after six seconds of combat");
            OpenSkillDetails(boss.unit);yield return new WaitForEndOfFrame();CaptureRuntime("inspection-boss-actual-health");skillDetailUnit=null;
            int originalWidth=Screen.width,originalHeight=Screen.height;
            Screen.SetResolution(1280,720,false);yield return new WaitForSecondsRealtime(.4f);
            OpenSkillDetails(boss.unit);yield return new WaitForEndOfFrame();CaptureRuntime("inspection-boss-720");skillDetailUnit=null;
            Screen.SetResolution(originalWidth,originalHeight,false);yield return new WaitForSecondsRealtime(.4f);battling=false;
            Debug.Log("INSPECTION BOSS PARITY COMPLETE: all 30 waves x 3 difficulties, actual boss modal and 720p capture");
            var final=SimulateTrialValidation(2,true);trialBattlePending=true;trialCleared=29;FinishTrialBattle(final.won,final.seconds);
            Require(TrialComplete&&TrialCleared==30,"real final combat reaches clear result");
            yield return new WaitForEndOfFrame();CaptureRuntime("trials-04-clear");
            CaptureRuntime("polish-04-records");
            yield return SoloLobbyClick(new Rect(974,720,391,61));Require(lobby&&TrialComplete,"result preparation button preserves score");
            yield return SoloLobbyClick(TrialEnterRect);Require(confirmTrialReset,"finished run asks before restart");
            yield return SoloLobbyClick(SoloResetConfirmRect);Require(!lobby&&!TrialFinished&&TrialStage==1&&TrialLives==3&&TrialScore==0,"confirmed restart makes a fresh isolated trial");
            trialLives=1;trialBattlePending=true;ResolveTrialOutcome(false,CurrentTrial.timeLimit);
            yield return new WaitForSecondsRealtime(.5f);yield return new WaitForEndOfFrame();CaptureRuntime("trials-05-failure");
            yield return SoloLobbyClick(new Rect(554,720,391,61));Require(!TrialFinished&&TrialLives==3&&!lobby&&trialScore==0,"failure retry button starts a new run");
            EnterSoloMode();Require(!TrialsMode&&lobby&&PortablePreviewPrefs.GetString(SaveKey,"")==soloAfter,"return to solo restores its own stored run");
        }
        finally
        {
            battling=false;trialsMode=false;fighters.Clear();skillCasts.Clear();combatPopups.Clear();confirmTrialReset=false;soloLobbyClickStep=0;CloseTrialDossier();
            for(int i=0;i<3;i++){if(existed[i])PortablePreviewPrefs.SetString(TrialSaveKey(i),stored[i]);else PortablePreviewPrefs.DeleteKey(TrialSaveKey(i));if(existedRecords[i])PortablePreviewPrefs.SetString(TrialRecordKey(i),storedRecords[i]);else PortablePreviewPrefs.DeleteKey(TrialRecordKey(i));}ClearTrialRecordCache();
            if(hadPreferred)PortablePreviewPrefs.SetInt("multi.trials.difficulty",preferred);else PortablePreviewPrefs.DeleteKey("multi.trials.difficulty");
            PortablePreviewPrefs.SetString(SaveKey,soloStored);Load();PortablePreviewPrefs.Save();UnityEngine.Random.state=random;
        }
        Debug.Log("TRIALS SMOKE COMPLETE: "+validationChecks+" checks / 30 waves / 3 difficulties / 6 real combat simulations / isolated saves / actual UI input");Application.Quit();
    }
}
#endif
