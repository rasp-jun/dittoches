#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    Fighter EventFighter(string id,bool enemy=false,int star=1,int index=0)
    {
        var f=CreateFighter(new Unit(RosterById[id]){star=star},enemy,new Vector2(index%7,enemy?1:4));
        f.attackScale=1;f.maxHp=f.hp=Meta(id).hp*Mathf.Pow(1.8f,star-1);f.cooldown=0;
        fighters.Add(f);return f;
    }
    void ClearEventCombat(){fighters.Clear();skillCasts.Clear();combatPopups.Clear();synergyBattleAge=0;}
    Fighter[] CrisisTestTeam(string id)
    {
        ClearEventCombat();int i=0;foreach(string uid in DigimonBuildCatalog.Find(id).members.Take(9))EventFighter(uid,false,1,i++);
        EventFighter("machinedramon",true,1,100);InitializeBuildBonuses();
        foreach(var f in fighters.Where(f=>!f.enemy)){f.hp=f.maxHp*.4f;f.mana=0;f.stun=2;}
        return fighters.Where(f=>!f.enemy).ToArray();
    }
    [Serializable] sealed class ComebackProof
    {public string trait,opponent;public int count,star;public float enabledHealth,controlHealth;}
    [Serializable] sealed class ComebackDocument{public ComebackProof[] combats;}
    bool SimulateComeback(string trait,string opponent,int count,int star,bool enable,out float health)
    {
        ClearEventCombat();int index=0;foreach(string id in DigimonBuildCatalog.Find(trait).members.Take(9))EventFighter(id,false,1,index++);
        for(int i=0;i<count;i++)EventFighter(opponent,true,star,100+i);
        InitializeBuildBonuses();
        if(!enable)foreach(var f in fighters)f.build.teamRally=f.build.teamInferno=f.build.teamSalvation=0;
        float time=0;
        while(time<60&&((fighters.Any(f=>!f.dead&&!f.enemy)&&fighters.Any(f=>!f.dead&&f.enemy))||skillCasts.Any(c=>c.released&&c.hits<c.skill.shots)))
        {UpdateCombat(.05f);time+=.05f;}
        health=fighters.Where(f=>!f.enemy).Sum(f=>f.hp);
        return health>0&&!fighters.Any(f=>f.enemy&&!f.dead);
    }
    void ValidateSynergyEvents()
    {
        int before=validationChecks;artPack=0;trialsMode=battling=false;ClearEventCombat();
        var source=EventFighter("agumon");var target=EventFighter("koromon",true);
        target.hp=target.maxHp=1000;target.build=new DigimonBuildCatalog.Bonus{magicResist=100};
        source.build=new DigimonBuildCatalog.Bonus{burn=.02f,killMana=35};source.mana=0;
        ApplySkillTraits(source,target);source.hp=0;source.dead=true;
        for(int i=0;i<60;i++)TickSynergyEffects(.05f);
        Require(Mathf.Abs(target.hp-970)<.01f&&Mathf.Abs(source.skillDamageDone-30)<.01f&&target.burnRate==0,"burn three ticks, magic resistance, expiry and dead source skill credit (hp="+target.hp+", damage="+source.skillDamageDone+", rate="+target.burnRate+", remaining="+target.burnRemaining+")");
        SynergyDamage(source,target,100000);Require(source.mana==0,"dead source never gains kill mana");
        ClearEventCombat();source=EventFighter("agumon");target=EventFighter("koromon",true);var weak=EventFighter("meramon");
        target.hp=target.maxHp=1000;source.build=new DigimonBuildCatalog.Bonus{burn=.04f};weak.build=new DigimonBuildCatalog.Bonus{burn=.01f};
        ApplyBurn(source,target);
        for(int i=0;i<30;i++){TickSynergyEffects(.1f);ApplyBurn(weak,target);}
        Require(Mathf.Abs(source.skillDamageDone-120)<.01f&&target.burnRate==.01f,"weak burn never extends strong burn");
        ApplyBurn(source,target);
        for(int i=0;i<20;i++){TickSynergyEffects(.1f);ApplyBurn(source,target);}
        Require(Mathf.Abs(source.skillDamageDone-200)<.01f,"refresh never postpones burn tick");
        target.hp=1000;target.burnRemaining=target.burnRate=target.burnClock=0;target.build=new DigimonBuildCatalog.Bonus{armor=100,magicResist=100};
        source.build=new DigimonBuildCatalog.Bonus{armorShred=.5f};ApplySkillTraits(source,target);
        SynergyDamage(source,target,150);SynergyDamage(source,target,100,"magic");Require(Mathf.Abs(target.hp-850)<.01f,"shred affects physical armor only");
        TickSynergyEffects(3);source.build.armorShred=.1f;ApplySkillTraits(source,target);Require(target.shredRemaining==1,"weak shred never extends stronger expiry");
        TickSynergyEffects(1);SynergyDamage(source,target,100);Require(Mathf.Abs(target.hp-800)<.01f&&target.armorShred==0,"shred expires at four seconds");
        target.hp=1000;target.battleAge=0;target.build=new DigimonBuildCatalog.Bonus{reduction=.3f,openingReduction=.45f};source.build=new DigimonBuildCatalog.Bonus();
        SynergyDamage(source,target,100);target.battleAge=8;SynergyDamage(source,target,100);Require(Mathf.Abs(target.hp-890)<.01f,"opening wall shares 60 percent cap and expires at eight seconds");
        target.build=new DigimonBuildCatalog.Bonus();target.hp=350;source.build=new DigimonBuildCatalog.Bonus{lowHealthDamage=.35f,killMana=35};source.mana=source.maxMana-10;
        SynergyDamage(source,target,100);Require(Mathf.Abs(target.hp-215)<.01f,"pursuit includes exact 35 percent threshold");
        SynergyDamage(source,target,1000);Require(source.mana==source.maxMana,"kill mana caps");source.mana=0;SynergyDamage(source,target,1000);Require(source.mana==0,"dead target cannot award kill twice");
        ClearEventCombat();source=EventFighter("etemon");source.mana=0;source.build=new DigimonBuildCatalog.Bonus{castMana=30,castAllyMana=14};
        var a=EventFighter("agumon");var b=EventFighter("gabumon");var c=EventFighter("patamon");
        a.maxMana=b.maxMana=c.maxMana=100;a.mana=95;b.mana=10;c.mana=20;c.maxMana=200;
        OnBuildCast(source);Require(source.mana==30&&a.mana==95&&b.mana==24&&c.mana==34,"mana refund and two lowest fraction allies with stable tie break");
        ClearEventCombat();source=EventFighter("machinedramon");target=EventFighter("koromon",true);target.pos=Vector2.zero;
        source.build=new DigimonBuildCatalog.Bonus{thirdSplash=.5f,lifesteal=1,thirdStun=1};source.hp=500;source.attacks=2;
        var neighbors=new List<Fighter>();for(int i=0;i<4;i++){var f=EventFighter("koromon",true);f.pos=new Vector2(i==3?5:.2f*(i+1),0);f.hp=f.maxHp=1000;neighbors.Add(f);}
        ApplyArtillerySplash(source,target);Require(neighbors.All(f=>f.hp==1000),"only third attack can splash");source.attacks=3;ApplyArtillerySplash(source,target);
        Require(Mathf.Abs(neighbors[0].hp-(1000-AttackDamage(source)*.5f))<.01f&&neighbors[1].hp<1000&&neighbors[2].hp==1000&&neighbors[3].hp==1000,"splash hits nearest two in one hex only");
        Require(source.hp==500&&neighbors.All(f=>f.stun==0),"splash cannot recurse, steal health or trigger basic stun");
        var team=CrisisTestTeam("friendship");foreach(var f in team)f.hp=f.maxHp*.40001f;
        ResolveTeamCrisis();Require(team.All(f=>f.synergyUsed==0),"team crisis does not trigger above forty percent");
        foreach(var f in team){f.hp=f.maxHp*.4f;f.shield=f.maxHp*.4f;}
        ResolveTeamCrisis();foreach(var f in team)Require(Mathf.Abs(f.hp/f.maxHp-.7f)<.001f&&Mathf.Abs(f.shield/f.maxHp-.5f)<.001f&&f.stun==0&&f.mana==30&&f.synergyUsed==1,"nine beasts boundary cleanse heal mana shield cap "+f.unit.def.id);
        foreach(var f in team)f.hp=f.maxHp*.1f;ResolveTeamCrisis();Require(Mathf.Abs(team[0].hp/team[0].maxHp-.1f)<.001f,"rally can only fire once");
        team=CrisisTestTeam("love");target=fighters.Last();target.maxHp=target.hp=1000;target.shield=20;target.build=new DigimonBuildCatalog.Bonus{magicResist=100};
        ResolveTeamCrisis();Require(Mathf.Abs(target.hp-930)<.01f&&target.shieldAbsorbed==20&&Mathf.Abs(team.Sum(f=>f.skillDamageDone)-70)<.01f&&target.burnRate==.035f,"inferno magic shield mitigation, report and burn");
        team=CrisisTestTeam("light");foreach(var f in team.Where(f=>new[]{"seraphimon","ophanimon","angewomon"}.Contains(f.unit.def.id))){f.dead=true;f.hp=0;}
        var chosen=team.First(f=>f.unit.def.id=="ophanimon");chosen.unit.star=2;ResolveTeamCrisis();
        Require(chosen.revived&&!chosen.dead&&Mathf.Abs(chosen.hp/chosen.maxHp-.4f)<.001f&&chosen.mana==0&&chosen.stun==0&&chosen.cooldown>0,"miracle highest price then star, resets cast and resumes next frame");
        Require(team.Count(f=>f.synergyKind==5)==1&&chosen.synergyKind==5&&chosen.revivedAt==synergyBattleAge,"one miracle announcement on revived hero with authoritative timestamp");
        Require(Mathf.Abs(chosen.shield/chosen.maxHp-.15f)<.001f&&Mathf.Abs(team.Sum(f=>f.healingDone)-chosen.hp)<.01f,"miracle shield and exact revived HP credit");
        chosen.hp=0;chosen.dead=true;ResolveTeamCrisis();Require(chosen.dead,"miracle does not revive a second time");
        foreach(string id in new[]{"friendship","love","light"}){team=CrisisTestTeam(id);foreach(var f in team){f.hp=0;f.dead=true;}ResolveTeamCrisis();Require(team.All(f=>f.synergyUsed==0),"full wipe prevents "+id);}
        var stateRows=CombatStatusUI.Describe(new DigimonBuildCatalog.Bonus{teamSalvation=.4f,openingReduction=.2f},10,0,false,true,false,new CombatStatusUI.State{synergyUsed=4,burnRemaining=2,shredRemaining=3,battleAge=10});
        Require(stateRows.Length==4&&stateRows[0].spent&&stateRows[1].text.Contains("작열")&&stateRows[2].text.Contains("파쇄")&&stateRows[3].spent,"public status includes spent miracle, debuffs and expired opening wall");
        for(int kind=3;kind<=5;kind++)Require(CombatSignals.Evaluate(12,false,-1,false,true,kind,10).kind==kind&&CombatSignals.Evaluate(13,false,-1,false,true,kind,10).kind==0,"nine trait replay signal expires "+kind);
        var proofs=new List<ComebackProof>();
        foreach(var fixture in new[]{new ComebackProof{trait="friendship",opponent="piedmon",count=5,star=2},new ComebackProof{trait="love",opponent="machinedramon",count=9,star=1},new ComebackProof{trait="light",opponent="wargreymon",count=9,star=1}})
        {
            float enabled,control;
            Require(SimulateComeback(fixture.trait,fixture.opponent,fixture.count,fixture.star,true,out enabled)&&fighters.Any(f=>f.synergyUsed>0),"actual UpdateCombat wins after nine trait trigger "+fixture.trait);
            Require(!SimulateComeback(fixture.trait,fixture.opponent,fixture.count,fixture.star,false,out control)&&control==0,"same formation loses with only crisis disabled "+fixture.trait);
            fixture.enabledHealth=enabled;fixture.controlHealth=control;proofs.Add(fixture);
            Debug.Log("COMEBACK PROOF: "+JsonUtility.ToJson(fixture));
        }
        File.WriteAllText(Path.Combine(Application.dataPath,"../SynergyComebackEvidence.json"),JsonUtility.ToJson(new ComebackDocument{combats=proofs.ToArray()},true));
        ClearEventCombat();Debug.Log("SYNERGY EVENTS COMPLETE: "+(validationChecks-before)+" checks / 3 actual loss-to-win simulations");
    }
    IEnumerator CaptureNewSynergyReview()
    {
        var storedBoard=board.ToArray();var storedShop=shop.ToArray();int oldLevel=level;bool oldReport=showCombatReport;
        try
        {
            level=9;showCombatReport=false;selectedBoard=selectedBench=-1;
            string[] offers={"piedmon","machinedramon","andromon","devimon","meramon"};
            for(int i=0;i<shop.Length;i++)shop[i]=RosterById[offers[i%offers.Length]];
            yield return new WaitForEndOfFrame();CaptureRuntime("open-roster-shop");
            validationShopInputPending=true;yield return new WaitForEndOfFrame();
            Require(!validationShopInputPending&&skillDetailUnit!=null&&skillDetailUnit.def.id=="piedmon","purchasable enemy shop right-click opens its real skill");
            CaptureRuntime("open-roster-piedmon-skill");skillDetailUnit=null;
            foreach(var fixture in new[]{new ComebackProof{trait="friendship",opponent="piedmon",count=5,star=2},new ComebackProof{trait="love",opponent="machinedramon",count=9,star=1},new ComebackProof{trait="light",opponent="wargreymon",count=9,star=1}})
            {
                ClearEventCombat();Array.Clear(board,0,board.Length);int index=0;
                foreach(string id in DigimonBuildCatalog.Find(fixture.trait).members.Take(9)){var f=EventFighter(id,false,1,index);f.sourceSlot=index;board[index++]=f.unit;}
                for(int n=0;n<fixture.count;n++)EventFighter(fixture.opponent,true,fixture.star,100+n);
                InitializeBuildBonuses();OpenTraitGuide(fixture.trait);yield return new WaitForEndOfFrame();CaptureRuntime("nine-"+fixture.trait+"-guide");traitFocus=null;
                int steps=0;while(steps++<1200&&!fighters.Any(f=>!f.enemy&&f.synergyUsed>0)){TickCombatPresentation(.05f);UpdateCombat(.05f);}
                Require(fighters.Any(f=>!f.enemy&&f.synergyUsed>0),"review captures actual health-triggered comeback "+fixture.trait);
                foreach(var f in fighters)f.renderPos=f.pos;
                inspectedUnit=fighters.First(f=>!f.enemy&&!f.dead&&(fixture.trait!="light"||f.revived)).unit;
                battleTimeRemaining=Mathf.Max(0,60-synergyBattleAge);battling=true;yield return new WaitForEndOfFrame();CaptureRuntime("nine-"+fixture.trait+"-trigger");battling=false;inspectedUnit=null;
            }
        }
        finally{battling=false;traitFocus=null;skillDetailUnit=inspectedUnit=null;ClearEventCombat();Array.Copy(storedBoard,board,board.Length);Array.Copy(storedShop,shop,shop.Length);level=oldLevel;showCombatReport=oldReport;}
    }

}
#endif
