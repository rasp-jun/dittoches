import copy
import json
import unittest
from pathlib import Path
from unittest.mock import patch
import combat_builds as b
import combat_stats as stats
from combat_skills import simulate
from test_combat_builds import fighter

DEFS = {u['id']: u for u in json.loads((Path(__file__).parent/'roster.json').read_text(encoding='utf-8'))}


class SynergyEvents(unittest.TestCase):
    def pair(self):
        a, t = fighter(), fighter(1, side=1)
        a['build'] = {}; t['build'] = {}
        return a, t

    def test_burn_three_ticks_typed_mitigation_and_dead_owner_credit(self):
        a,t=self.pair();a['build']={'burn':.02,'killMana':35};t['build']={'magicResist':100}
        b.on_skill_hit(a,t);a['hp']=0
        for step in range(60):b.tick_effects([a,t],.05,(step+1)*.05)
        self.assertAlmostEqual(t['hp'],970);self.assertAlmostEqual(a['skillDamageDone'],30)
        self.assertAlmostEqual(t['damageTaken'],30);self.assertEqual(t['burnRate'],0)
        b.damage(a,t,100000);self.assertEqual(a['mana'],0)

    def test_burn_refresh_does_not_postpone_ticks_or_extend_stronger_effect(self):
        a,t=self.pair();a['build']={'burn':.04};weak=fighter(2);weak['build']={'burn':.01}
        b.apply_burn(a,t)
        for step in range(30):
            b.tick_effects([a,t,weak],.1,(step+1)*.1);b.apply_burn(weak,t)
        self.assertAlmostEqual(a['damageDone'],120)
        self.assertEqual(t['burnRate'],.01)
        b.apply_burn(a,t)
        for step in range(20):
            b.tick_effects([a,t],.1,3+(step+1)*.1);b.apply_burn(a,t)
        self.assertAlmostEqual(a['damageDone'],200)

    def test_shred_strongest_four_seconds_and_physical_only(self):
        a,t=self.pair();a['build']={'armorShred':.5};t['build']={'armor':100,'magicResist':100}
        b.on_skill_hit(a,t);b.damage(a,t,150)
        self.assertAlmostEqual(t['hp'],900)
        b.damage(a,t,100,damage_type='magic');self.assertAlmostEqual(t['hp'],850)
        b.tick_effects([a,t],3,3);a['build']['armorShred']=.1;b.on_skill_hit(a,t)
        self.assertEqual(t['shredRemaining'],1)
        b.tick_effects([a,t],1,4);b.damage(a,t,100);self.assertAlmostEqual(t['hp'],800)

    def test_opening_wall_expires_at_eight_seconds_and_caps_with_items(self):
        a,t=self.pair();t['build']={'openingReduction':.45,'reduction':.3}
        b.damage(a,t,100);self.assertAlmostEqual(t['hp'],960)
        b.tick_effects([a,t],8,8);b.damage(a,t,100);self.assertAlmostEqual(t['hp'],890)

    def test_pursuit_boundary_and_kill_mana_once_capped(self):
        a,t=self.pair();a['build']={'lowHealthDamage':.35,'killMana':35};a['mana']=90;t['hp']=350
        b.damage(a,t,100);self.assertAlmostEqual(t['hp'],215)
        b.damage(a,t,1000);self.assertEqual(a['mana'],100)
        a['mana']=0;b.damage(a,t,1000);self.assertEqual(a['mana'],0)

    def test_cast_refund_and_lowest_mana_two_allies_excludes_self_dead_full(self):
        a=fighter();a['build']={'castMana':30,'castAllyMana':14}
        allies=[fighter(i+1) for i in range(4)]
        allies[0].update(mana=95);allies[1].update(mana=10);allies[2].update(mana=20,maxMana=200);allies[3].update(hp=0)
        b.on_cast(a,[a]+allies)
        self.assertEqual([a['mana']]+[f['mana'] for f in allies],[30,95,24,34,0])
        a['mana']=99;b.on_cast(a,[a]);self.assertEqual(a['mana'],100)

    def test_machine_three_slots_and_removal_recompute_without_opening_refill(self):
        team=['andromon','machinedramon','metalgreymon','metalgarurumon']
        two=b.resolve('andromon',team,[0,3]);three=b.resolve('andromon',team,[0,3,8])
        self.assertAlmostEqual(two.get('castShield',0),0);self.assertAlmostEqual(three['castShield'],.12)
        self.assertAlmostEqual(three['hp']-two['hp'],.1)
        fs=[fighter(i,uid,items=[0,3,8] if i==0 else []) for i,uid in enumerate(team)];b.initialize(fs)
        f=fs[0];f.update(hp=f['maxHp']*.4,mana=7,synergyUsed=7)
        b.change_equipment(f,[],fs)
        self.assertAlmostEqual(f['hp']/f['maxHp'],.4);self.assertEqual(f['mana'],7)
        self.assertEqual(f['synergyUsed'],7);self.assertEqual(b.value(f,'castShield'),0)

    def test_third_splash_nearest_two_only_no_recursion_or_main_target_repeat(self):
        a,t=self.pair();a.update(attacks=2,build={'thirdSplash':.5,'thirdStun':1,'lifesteal':1},hp=500)
        victims=[fighter(i+2,side=1) for i in range(4)]
        t.update(x=0,y=0)
        for i,f in enumerate(victims):f.update(x=.2*(i+1),y=0)
        victims[3]['x']=5
        fs=[a,t]+victims;b.third_splash(a,t,fs,1);self.assertTrue(all(f['hp']==1000 for f in victims))
        a['attacks']=3;b.third_splash(a,t,fs,2)
        self.assertAlmostEqual(victims[0]['hp'],1000-stats.attack(a)*.5)
        self.assertEqual(victims[2]['hp'],1000);self.assertEqual(t['hp'],1000)
        self.assertEqual(a['hp'],500);self.assertEqual(victims[0]['stun'],0)

    def crisis_team(self, trait_id):
        trait=next(t for t in b.TRAITS if t['id']==trait_id)
        fs=[fighter(i,uid) for i,uid in enumerate(trait['members'][:9])]
        fs.append(fighter(100,'machinedramon',1));b.initialize(fs)
        for f in fs[:-1]:f.update(hp=f['maxHp']*.4,mana=0,stun=2)
        return fs

    def test_nine_beasts_exact_boundary_once_cleanse_and_shield_cap(self):
        fs=self.crisis_team('friendship');team=fs[:-1]
        for f in team:f['hp']=f['maxHp']*.40001
        b.team_crisis(fs,DEFS,5);self.assertFalse(any(f['synergyUsed'] for f in team))
        for f in team:f.update(hp=f['maxHp']*.4,shield=f['maxHp']*.4)
        b.team_crisis(fs,DEFS,6)
        for f in team:
            self.assertAlmostEqual(f['hp']/f['maxHp'],.7);self.assertAlmostEqual(f['shield']/f['maxHp'],.5)
            self.assertEqual(f['mana'],30);self.assertEqual(f['stun'],0);self.assertEqual(f['synergyUsed'],1)
            f['hp']=f['maxHp']*.1
        b.team_crisis(fs,DEFS,7);self.assertAlmostEqual(team[0]['hp']/team[0]['maxHp'],.1)

    def test_nine_flame_blast_report_resistance_shield_and_burn(self):
        fs=self.crisis_team('love');enemy=fs[-1];enemy.update(build={'magicResist':100},shield=20)
        b.team_crisis(fs,DEFS,4)
        self.assertAlmostEqual(enemy['hp'],930);self.assertEqual(enemy['shieldAbsorbed'],20)
        self.assertAlmostEqual(sum(f['skillDamageDone'] for f in fs[:-1]),70)
        self.assertEqual(enemy['burnRate'],.035)
        self.assertTrue(all(f['synergyUsed']==2 for f in fs[:-1]))

    def test_nine_sanctuary_revives_highest_cost_then_star_once(self):
        fs=self.crisis_team('light');team=fs[:-1]
        for f in team:
            if f['id'] in ('seraphimon','ophanimon','angewomon'):f['hp']=0
        oph=next(f for f in team if f['id']=='ophanimon');oph['star']=2
        b.team_crisis(fs,DEFS,9)
        self.assertEqual(oph['revivedAt'],9);self.assertEqual(sum(f['synergyKind']==5 for f in team),1)
        self.assertTrue(oph['revived']);self.assertAlmostEqual(oph['hp']/oph['maxHp'],.4)
        self.assertEqual(oph['mana'],0);self.assertEqual(oph['stun'],0);self.assertEqual(oph['castUntil'],9)
        self.assertAlmostEqual(sum(f['healingDone'] for f in team),oph['hp'])
        self.assertAlmostEqual(oph['shield']/oph['maxHp'],.15)
        oph['hp']=0;b.team_crisis(fs,DEFS,10);self.assertEqual(oph['hp'],0)

    def test_full_wipe_cannot_rescue_and_dead_carrier_does_not_disable_survivor_team(self):
        for trait in ('friendship','love','light'):
            fs=self.crisis_team(trait)
            for f in fs[:-1]:f['hp']=0
            b.team_crisis(fs,DEFS,10);self.assertTrue(all(f['synergyUsed']==0 for f in fs))
        fs=self.crisis_team('light');fs[0]['hp']=0;b.team_crisis(fs,DEFS,5)
        self.assertTrue(fs[0]['revived'])

    def test_both_sides_latch_before_damage_so_order_does_not_suppress_crisis(self):
        left=self.crisis_team('love')[:-1];right=copy.deepcopy(left)
        for f in right:f.update(key=f['key']+100,side=1)
        fs=left+right
        for f in fs:f['hp']=1
        b.team_crisis(fs,DEFS,5)
        self.assertTrue(all(f['synergyUsed']==2 for f in fs));self.assertTrue(all(f['hp']==0 for f in fs))

    def test_real_simulation_records_crisis_without_mutating_earlier_frames(self):
        # Give both formations a lethal opponent so the 40% event is exercised by real damage.
        trait=next(t for t in b.TRAITS if t['id']=='friendship')
        fs=[fighter(i,uid) for i,uid in enumerate(trait['members'])]
        fs += [fighter(100,'machinedramon',1)]
        for f in fs:
            f.update(x=float(f['key']%7),y=1. if f['side'] else 4.,star=3 if f['side'] else 1)
            f['maxHp']=stats.SKILLS[f['id']]['baseHealth']*1.8**(f['star']-1)
        frames,events,_=simulate(fs,DEFS)
        self.assertTrue(any(f['synergyUsed']&1 for frame in frames for f in frame['units']))
        self.assertTrue(all(f['synergyUsed']==0 for f in frames[0]['units']))
        frozen=json.dumps(frames[0],sort_keys=True);fs[0]['hp']=0;fs[0]['synergyUsed']=7
        self.assertEqual(json.dumps(frames[0],sort_keys=True),frozen)
        json.dumps((frames,events))

    def test_all_three_nine_traits_can_turn_a_loss_into_a_win(self):
        for trait_id,enemy_id,count,star in (('friendship','machinedramon',1,3),('love','machinedramon',6,1),('light','wargreymon',5,1)):
            with self.subTest(trait=trait_id):
                ids=next(t for t in b.TRAITS if t['id']==trait_id)['members'][:9]
                original=[fighter(i,uid) for i,uid in enumerate(ids)]+[fighter(100+i,enemy_id,1) for i in range(count)]
                for f in original:
                    f.update(x=float(f['key']%7),y=1. if f['side'] else 4.,star=star if f['side'] else 1)
                    f['maxHp']=stats.SKILLS[f['id']]['baseHealth']*1.8**(f['star']-1)
                enabled=copy.deepcopy(original);simulate(enabled,DEFS)
                control=copy.deepcopy(original)
                with patch.object(b,'team_crisis',lambda *args:None):simulate(control,DEFS)
                self.assertTrue(any(f['hp']>0 for f in enabled if not f['side']))
                self.assertFalse(any(f['hp']>0 for f in enabled if f['side']))
                self.assertFalse(any(f['hp']>0 for f in control if not f['side']))
                self.assertTrue(any(f['hp']>0 for f in control if f['side']))


if __name__=='__main__':unittest.main()
