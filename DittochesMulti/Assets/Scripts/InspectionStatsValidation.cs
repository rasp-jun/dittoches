#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Linq;
using UnityEngine;

// Differential checks for the values shared within one inspection draw. These
// helpers keep no gameplay cache and never mutate the current formation/replay.
static class InspectionStatsValidation
{
    public static void EqualFields<T>(T expected,T actual,Action<bool,string> require,string label)
    {
        require(expected!=null&&actual!=null,label+" values exist");
        foreach(var field in typeof(T).GetFields())
            require(object.Equals(field.GetValue(expected),field.GetValue(actual)),label+" "+field.Name);
    }
    public static void EqualRows(CombatStatusUI.Row[] expected,CombatStatusUI.Row[] actual,Action<bool,string> require,string label)
    {
        require(expected.Length==actual.Length,label+" row count");
        for(int i=0;i<expected.Length;i++)EqualFields(expected[i],actual[i],require,label+" row "+i);
    }
}

public sealed partial class NativeGame
{
    void ValidateSharedInspectionStats()
    {
        int checkedUnits=0;
        var units=board.Concat(bench).Concat(scoutBoard).Concat(fighters.Select(f=>f.unit)).Concat(lastBattleReport.Select(f=>f.unit)).Where(u=>u!=null).Distinct();
        foreach(var unit in units)
        {
            DigimonBuildCatalog.Bonus bonus;Fighter live;
            var actual=InspectStats(unit,out bonus,out live);var expectedLive=InspectedFighter(unit);
            Require(ReferenceEquals(live,expectedLive),"shared inspection uses the same current or historical fighter");
            var ids=scoutedRival>=0&&scoutBoard.Contains(unit)?InspectedFormationMembers():board.Contains(unit)?BoardBuildMembers():Enumerable.Empty<DigimonBuildCatalog.Member>();
            var expectedBonus=expectedLive!=null?expectedLive.build:DigimonBuildCatalog.Resolve(unit.def.id,ids,unit.items);
            float scale=expectedLive!=null&&expectedLive.enemy?expectedLive.attackScale:scoutedRival>=0&&scoutBoard.Contains(unit)?DifficultyScale[difficulty]*(1+round*.035f):1;
            var expected=new DigimonSkillCatalog.Stats(DigimonSkillCatalog.Find(unit.def.id),unit.star,expectedBonus,scale);
            if(expectedLive!=null)expected.speed=DigimonCombatMath.AttackSpeed(DigimonSkillCatalog.Find(unit.def.id).attackSpeed,expectedBonus.speed,expectedBonus.rampSpeed,expectedLive.combatAge);
            if(expectedLive!=null){expected.health=expectedLive.maxHp;expected.armor=EffectiveArmor(expectedLive);}
            InspectionStatsValidation.EqualFields(expected,actual,Require,"native shared stats");
            InspectionStatsValidation.EqualFields(expectedBonus,bonus,Require,"native shared bonus");
            if(live!=null)Require(ReferenceEquals(bonus,live.build),"native current and historical build ownership is preserved");
            string expectedContext=expectedLive!=null?(battling?(trialsMode&&expectedLive.enemy?"현재 전투 능력치 · 시험 적의 개별 보정 포함":"현재 전투 능력치 · 야수 중첩 포함"):"지난 전투 · 저장된 능력치"):scoutedRival>=0&&scoutBoard.Contains(unit)?"상대 장비 + 상대 전장 시너지 적용":board.Contains(unit)?"장비 + 현재 전장 시너지 적용":"대기석 · 장비만 적용 (시너지 미포함)";
            Require(StatContext(unit,live)==expectedContext&&StatContext(unit)==expectedContext,"shared inspection context and wrapper retain their labels");
            InspectionStatsValidation.EqualFields(expected,InspectStats(unit),Require,"native stats wrapper");
            InspectionStatsValidation.EqualRows(
                CombatStatusUI.Describe(expectedBonus,live!=null?live.combatAge:0,live!=null?live.attacks:0,live!=null&&live.lowShieldUsed,live!=null,live!=null&&(live.dead||!battling)),
                CombatStatusUI.Describe(bonus,live!=null?live.combatAge:0,live!=null?live.attacks:0,live!=null&&live.lowShieldUsed,live!=null,live!=null&&(live.dead||!battling)),Require,"native shared status");
            checkedUnits++;
        }
        Require(checkedUnits>0,"native shared inspection validates populated state");
        Debug.Log("SHARED INSPECTION STATS: native "+checkedUnits+" units verified");
    }
}

public sealed partial class MultiLauncher
{
    void ValidateSharedOnlineInspectionReference()
    {
        var savedState=state;float savedReceivedAt=receivedAt;int savedScoutSide=scoutSide;
        try
        {
            var unit=new Unit{id="gabumon",star=2,slot=3,items=new[]{2}};
            var reserve=new Unit{id="gabumon",star=2,slot=3,items=new[]{12}};
            var opponent=new Unit{id="gabumon",star=2,slot=3,items=new[]{17}};
            var own=new Player{board=new[]{unit,new Unit{id="tsunomon",star=1,slot=4,items=new int[0]}},bench=new[]{reserve}};
            var rival=new Player{board=new[]{opponent},bench=new Unit[0]};
            var expectedLive=new Fighter{id=unit.id,side=1,slot=3,key=73,star=2,hp=137,maxHp=1234,mana=9,maxMana=75,
                attackDamage=321,abilityPower=187,armor=29,magicResist=41,attackRange=5,attackSpeed=1.73f,attacks=4,lowShieldUsed=true};
            // Decoys precede the only exact own side/slot/species match.
            var wrongSide=expectedLive.Snapshot();wrongSide.side=0;
            var wrongSlot=expectedLive.Snapshot();wrongSlot.slot=2;
            var wrongId=expectedLive.Snapshot();wrongId.id="agumon";
            var room=new Room{id="inspection-reference",phase="battle",side=1,remaining=10,battleDuration=10,players=new[]{rival,own},
                frames=new[]{new Frame{time=0,units=new[]{wrongSide,wrongSlot,wrongId,expectedLive}}}};
            state=new State{room=room};receivedAt=Time.unscaledTime;scoutSide=0;
            var expectedBonus=DigimonBuildCatalog.Resolve(unit.id,own.board.Select(BuildMember),unit.items);
            foreach(int version in new[]{0,1,3})
            foreach(float age in new[]{0f,2.999f,3f,15f})
            {
                expectedLive.combatStatsVersion=version;expectedLive.combatAge=age;
                DigimonBuildCatalog.Bonus bonus;Fighter live;var actual=OnlineStats(unit,"board",out bonus,out live);
                OnlineRequire(ReferenceEquals(live,expectedLive),"single-frame inspection selects the independent exact own fighter reference, never null or a decoy");
                var expected=new DigimonSkillCatalog.Stats(DigimonSkillCatalog.Find(unit.id),live!=null?live.star:unit.star,expectedBonus);
                expected.speed=DigimonCombatMath.AttackSpeed(DigimonSkillCatalog.Find(unit.id).attackSpeed,expectedBonus.speed,expectedBonus.rampSpeed,age);
                if(version>=1){expected.attack=321;expected.abilityPower=187;expected.health=1234;expected.armor=29;expected.magicResist=41;expected.range=5;expected.speed=1.73f;}
                InspectionStatsValidation.EqualFields(expected,actual,OnlineRequire,"single-frame independent stats v"+version+" age "+age);
                InspectionStatsValidation.EqualFields(expectedBonus,bonus,OnlineRequire,"single-frame independent bonus");
            }
            DigimonBuildCatalog.Bonus ignoredBonus;Fighter ignoredLive;
            OnlineStats(reserve,"bench",out ignoredBonus,out ignoredLive);OnlineRequire(ignoredLive==null,"same-slot bench inspection never borrows an own board fighter");
            OnlineStats(opponent,"scout-board",out ignoredBonus,out ignoredLive);OnlineRequire(ignoredLive==null,"scout inspection never borrows the identical own side/slot/species fighter");
            room.frames[0].units=new[]{wrongSide,wrongSlot,wrongId};
            OnlineStats(unit,"board",out ignoredBonus,out ignoredLive);OnlineRequire(ignoredLive==null,"missing exact fighter never falls back to side/slot/species decoys");
            room.frames[0].units=new[]{expectedLive};room.phase="prepare";
            OnlineStats(unit,"board",out ignoredBonus,out ignoredLive);OnlineRequire(ignoredLive==null,"preparation never reuses a retained battle frame");
            Debug.Log("SHARED INSPECTION REFERENCE: exact live identity, legacy/authoritative stats and exclusions verified");
        }
        finally{state=savedState;receivedAt=savedReceivedAt;scoutSide=savedScoutSide;}
        OnlineRequire(ReferenceEquals(state,savedState)&&receivedAt==savedReceivedAt&&scoutSide==savedScoutSide,"single-frame inspection validation restores the original state and playhead");
    }
    void ValidateSharedOnlineInspectionStats()
    {
        ValidateSharedOnlineInspectionReference();
        int checkedUnits=0;
        foreach(string area in new[]{"board","bench","scout-board","scout-bench"})
        {
            bool scouting=area.StartsWith("scout-");var owner=scouting?ScoutedPlayer:OnlineMe;
            if(owner==null)continue;var units=area.EndsWith("board")?owner.board:owner.bench;
            if(units==null)continue;
            foreach(var unit in units)
            {
                if(unit==null)continue;
                DigimonBuildCatalog.Bonus bonus;Fighter live;
                var actual=OnlineStats(unit,area,out bonus,out live);
                var ids=area=="board"||area=="scout-board"?owner.board.Select(BuildMember):Enumerable.Empty<DigimonBuildCatalog.Member>();
                var expectedBonus=DigimonBuildCatalog.Resolve(unit.id,ids,unit.items??new int[0]);
                var expected=new DigimonSkillCatalog.Stats(DigimonSkillCatalog.Find(unit.id),live!=null?live.star:unit.star,expectedBonus);
                // Reuse the observed frame for the reference calculation: the
                // real-time playhead could cross a boundary between test calls.
                if(live!=null)
                {
                    expected.speed=DigimonCombatMath.AttackSpeed(DigimonSkillCatalog.Find(unit.id).attackSpeed,expectedBonus.speed,expectedBonus.rampSpeed,live.combatAge);
                    if(live.combatStatsVersion>=1){expected.attack=live.attackDamage;expected.abilityPower=live.abilityPower;expected.health=live.maxHp;expected.armor=live.armor;expected.magicResist=live.magicResist;expected.range=live.attackRange;expected.speed=live.attackSpeed;}
                }
                OnlineRequire(live==null||area=="board"&&live.id==unit.id&&live.slot==unit.slot&&live.side==state.room.side,"shared online fighter belongs to the inspected own board slot");
                OnlineRequire(!scouting||live==null,"scouting never borrows an own fighter");
                InspectionStatsValidation.EqualFields(expected,actual,OnlineRequire,"online shared stats");
                InspectionStatsValidation.EqualFields(expectedBonus,bonus,OnlineRequire,"online shared bonus");
                InspectionStatsValidation.EqualRows(
                    CombatStatusUI.Describe(expectedBonus,live!=null?live.combatAge:0,live!=null?live.attacks:0,live!=null&&live.lowShieldUsed,live!=null,live!=null&&live.hp<=0),
                    CombatStatusUI.Describe(bonus,live!=null?live.combatAge:0,live!=null?live.attacks:0,live!=null&&live.lowShieldUsed,live!=null,live!=null&&live.hp<=0),OnlineRequire,"online shared status");
                // With no live clock dependency the old-signature wrapper can
                // also be compared exactly inside the running player.
                if(area!="board"||state==null||state.room==null||state.room.phase!="battle")
                    InspectionStatsValidation.EqualFields(expected,OnlineStats(unit,area),OnlineRequire,"online stats wrapper");
                checkedUnits++;
            }
        }
        OnlineRequire(checkedUnits>0,"online shared inspection validates populated state");
        Debug.Log("SHARED INSPECTION STATS: online "+checkedUnits+" units verified");
    }
}
#endif
