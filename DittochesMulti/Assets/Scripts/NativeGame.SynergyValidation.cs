#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.IO;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    [Serializable] sealed class SynergyFixture { public SynergyRow[] rows; }
    [Serializable] sealed class SynergyRow { public string id;public string[] team;public int[] items;public DigimonBuildCatalog.Bonus bonus; }
    void ValidateSynergyRules()
    {
        artPack=0;fighters.Clear();
        var seen=new System.Collections.Generic.HashSet<string>();
        foreach(var trait in DigimonBuildCatalog.Data.traits)
        {
            var icon=DigimonTraitUI.Icon(trait.id);
            Require(icon.width==160&&ReferenceEquals(icon,DigimonTraitUI.Icon(trait.id)),"cached synergy icon "+trait.id);
            Require(seen.Add(Convert.ToBase64String(icon.EncodeToPNG())),"distinct synergy silhouette "+trait.id);
            Require(!string.IsNullOrEmpty(trait.identity)&&!string.IsNullOrEmpty(trait.usage),"synergy guidance "+trait.id);
        }
        string file=Path.Combine(Application.dataPath,"../SynergyResolverFixture.json");
        if(File.Exists(file))
        {
            var fixture=JsonUtility.FromJson<SynergyFixture>(File.ReadAllText(file));int checks=0;
            foreach(var row in fixture.rows)
            {
                var actual=DigimonBuildCatalog.Resolve(row.id,row.team,row.items);
                foreach(var field in typeof(DigimonBuildCatalog.Bonus).GetFields())
                    Require(Mathf.Abs((float)field.GetValue(actual)-(float)field.GetValue(row.bonus))<.001f,"server resolver parity "+row.id+" "+field.Name);
                checks++;
            }
            Debug.Log("SYNERGY RESOLVER PARITY: "+checks+" cases");
        }
        var source=CreateFighter(new Unit(RosterById["agumon"]),true,new Vector2(3,3));
        var target=CreateFighter(new Unit(RosterById["patamon"]),false,new Vector2(3,4));
        source.build=new DigimonBuildCatalog.Bonus();target.maxHp=target.hp=1000;target.shield=0;
        target.build=new DigimonBuildCatalog.Bonus{lowHeal=.08f,lowShield=.1f,healPower=.25f};
        DealDamage(source,target,650);
        Require(Mathf.Abs(target.hp-450)<.001f&&target.shield==100&&target.lowShieldUsed&&target.crisisAt==target.combatAge,"hope threshold amplified recovery and event timestamp");
        DealDamage(source,target,250);Require(target.hp==300&&target.shield==0,"hope triggers once");
        target.lowShieldUsed=false;DealDamage(source,target,9999);
        Require(target.dead&&target.hp==0&&!target.lowShieldUsed,"hope cannot resurrect lethal damage");
        target=CreateFighter(new Unit(RosterById["gabumon"]),false,new Vector2(3,4));
        target.build=new DigimonBuildCatalog.Bonus{speed=.08f,rampSpeed=.04f};
        for(int i=0;i<300;i++)TickBuild(target,.05f);
        Require(Mathf.Abs(DigimonCombatMath.RampSpeed(target.build.rampSpeed,target.combatAge)-.2f)<.001f,"friendship five stacks at 15 seconds");
        target.dead=true;float age=target.combatAge;TickBuild(target,9);Require(target.combatAge==age,"dead unit stops synergy clock");
        var bonuses=new DigimonBuildCatalog.Bonus{rampSpeed=.04f,thirdHit=.3f,thirdStun=.4f,lowHeal=.08f,lowShield=.1f};
        var before=CombatStatusUI.Describe(bonuses,99,99,true,false,false);
        Require(before.Length==3&&before.All(r=>r.progress==0)&&!before[2].spent,"preparation cannot leak previous battle state");
        var liveRows=CombatStatusUI.Describe(bonuses,6,2,false,true,false);
        Require(Mathf.Abs(liveRows[0].progress-.4f)<.001f&&liveRows[1].text.Contains("다음 타격")&&!liveRows[2].spent,"live stacks and next empowered attack");
        var completed=CombatStatusUI.Describe(bonuses,30,3,true,true,true);
        Require(completed[0].progress==1&&completed[1].progress==0&&completed[2].spent,"capped stacks, reset attack cycle and used crisis");
        Require(completed.All(r=>!r.detail.Contains("다음 중첩")),"completed reports never show a running countdown");
        Require(CombatStatusUI.Describe(new DigimonBuildCatalog.Bonus(),0,0,false,false,false).Length==0,"no fabricated dynamic effects");
        Require(CombatSignals.Evaluate(10,false,10,false).kind==0&&CombatSignals.Evaluate(9,true,10,false).kind==0,"untriggered and future crisis have no signal");
        Require(CombatSignals.Evaluate(10,true,10,false).kind==1&&CombatSignals.Evaluate(11.2f,true,10,false).kind==0,"crisis starts at event time and expires");
        Require(CombatSignals.Evaluate(14.99f,false,-1,true).kind==0&&CombatSignals.Evaluate(15,false,-1,true).kind==2&&CombatSignals.Evaluate(16.4f,false,-1,true).kind==0,"friendship only signals at maximum stacks");
        Require(CombatSignals.Evaluate(15.1f,true,15,true).kind==1,"crisis caption has priority over friendship");
        Require(CombatSignals.Evaluate(15.1f,true,15,true,false).kind==0&&CombatSignals.Evaluate(20,true,10,true).kind==0,"dead units and late reconnect never restart signals");
        string captures=Path.Combine(Application.dataPath,"../CombatSignalCaptures");Directory.CreateDirectory(captures);
        using(var review=new TacticalArena(new Rect(0,0,960,600),true))
        {
            foreach(int kind in new[]{1,2})foreach(float ageInEffect in new[]{.35f,.85f,2f})
            {
                review.BeginFrame(-1,-1,-1,false);Vector3 point=TacticalArena.CellWorld(3,4);
                review.SetActor("signal-review",point,FaithfulPortraits.Get("agumon",true),Color.green);
                review.FaceActor("signal-review",Vector3.forward);review.PoseDigimon("signal-review","agumon",0,0,ageInEffect);
                var signal=CombatSignals.Evaluate((kind==1?10:15)+ageInEffect,kind==1,10,kind==2);
                review.DrawCombatSignal(point,signal);review.Render();
                Require(ageInEffect<1?review.EffectCount>0&&review.EffectCount<=7:review.EffectCount==0,"signal effect pool bounds and expiry "+kind);
                RenderTexture old=RenderTexture.active;RenderTexture.active=review.Texture;
                var picture=new Texture2D(review.Texture.width,review.Texture.height,TextureFormat.RGB24,false);
                picture.ReadPixels(new Rect(0,0,picture.width,picture.height),0,0);picture.Apply();
                File.WriteAllBytes(Path.Combine(captures,"signal-"+kind+"-"+ageInEffect.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+".png"),picture.EncodeToPNG());
                Destroy(picture);RenderTexture.active=old;
            }
        }
        fighters.Clear();combatPopups.Clear();
    }
}
#endif
