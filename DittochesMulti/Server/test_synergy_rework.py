import copy
import math
import unittest
import combat_builds as b
import combat_stats as stats
from test_combat_builds import fighter


class SynergyReworkTests(unittest.TestCase):
    def test_friendship_boundaries_additive_equipment_and_cap(self):
        f=fighter(unit='gabumon',items=[2])
        b.initialize([f,fighter(1,'tsunomon')])
        base=stats.SKILLS['gabumon']['attackSpeed']
        for age,stacks in ((0,0),(2.99,0),(3,1),(6,2),(9,3),(12,4),(15,5),(30,5)):
            f['combatAge']=age
            self.assertAlmostEqual(stats.attack_speed(f),base*(1+.08+b.ITEMS[2]['bonus']['speed']+.04*stacks))

    def test_friendship_clock_is_independent_of_tick_size_and_stops_when_dead(self):
        a=fighter(unit='gabumon');b.initialize([a,fighter(1,'tsunomon')]);c=copy.deepcopy(a)
        b.tick(a,15)
        for _ in range(300):b.tick(c,.05)
        self.assertAlmostEqual(stats.attack_speed(a),stats.attack_speed(c))
        age=c['combatAge'];c['hp']=0;b.tick(c,10)
        self.assertEqual(c['combatAge'],age)
        fresh=fighter(unit='gabumon');fresh['combatAge']=99;b.initialize([fresh])
        self.assertEqual(fresh['combatAge'],0)
        self.assertEqual(fresh['crisisAt'],-1)
        self.assertFalse(fresh['friendshipActive'])
        self.assertTrue(a['friendshipActive'])

    def test_hope_heals_and_shields_once_and_combines_equipment(self):
        source=fighter(side=1);target=fighter(1,'patamon',items=[8])
        b.initialize([source,target,fighter(2,'tokomon')])
        target['build'].update(healPower=.25,armor=0,magicResist=0,reduction=0)
        target['shield']=0
        shield=target['maxHp']*b.value(target,'lowShield')
        b.damage(source,target,target['maxHp']*.65)
        self.assertAlmostEqual(target['hp']/target['maxHp'],.35+.08*1.25)
        self.assertAlmostEqual(target['shield'],min(shield,target['maxHp']*.5))
        healed=target['healingDone'];protected=target['shieldingDone']
        b.damage(source,target,target['shield']+target['maxHp']*.15)
        self.assertEqual(target['healingDone'],healed)
        self.assertEqual(target['shieldingDone'],protected)
        b.damage(source,target,1e9)
        self.assertEqual(target['hp'],0)

    def test_hope_does_not_rescue_lethal_hit_or_trigger_above_threshold(self):
        for amount in (649,1000):
            source,target=fighter(),fighter(1,side=1)
            target['build']={'lowHeal':.22,'lowShield':.28}
            b.damage(source,target,amount)
            self.assertFalse(target.get('lowShieldUsed',False))
            self.assertEqual(target['hp'],1000-amount)

    def test_harmonizer_aura_only_once_for_members_and_outsiders_at_every_tier(self):
        trait=next(t for t in b.TRAITS if t['id']=='harmonizer')
        for tier in trait['tiers']:
            team=trait['members'][:tier['count']]
            for uid in ('agumon','patamon'):
                result=b.resolve(uid,team+team)
                self.assertEqual(result['armor'],tier['bonus']['teamResist'])
                self.assertEqual(result['magicResist'],tier['bonus']['teamResist'])
                self.assertEqual(result['startShield'],tier['bonus']['teamShield'])

    def test_replay_crisis_state_is_per_frame_not_future_final_value(self):
        import json
        from pathlib import Path
        from combat_skills import simulate
        defs={u['id']:u for u in json.loads((Path(__file__).parent/'roster.json').read_text(encoding='utf-8'))}
        fs=[fighter(0,'patamon',items=[8]),fighter(1,'tokomon'),fighter(2,'wargreymon',side=1)]
        fs[2]['hp']=fs[2]['maxHp']=10000
        frames,_,_=simulate(fs,defs)
        self.assertFalse(frames[0]['units'][0]['lowShieldUsed'])
        triggered=[f for f in frames if f['units'][0]['lowShieldUsed']]
        self.assertTrue(triggered)
        self.assertGreater(triggered[0]['time'],0)
        self.assertTrue(frames[-1]['units'][0]['lowShieldUsed'])
        self.assertFalse(frames[0]['units'][0]['lowShieldUsed'])
        self.assertEqual(frames[0]['units'][0]['combatAge'],0)
        self.assertEqual(frames[0]['units'][0]['crisisAt'],-1)
        at=triggered[0]['units'][0]['crisisAt']
        self.assertGreater(at,0)
        self.assertLessEqual(at,triggered[0]['time'])
        self.assertEqual({f['units'][0]['crisisAt'] for f in triggered},{at})
        self.assertEqual(triggered[0]['units'][0]['combatStatsVersion'],2)

    def test_every_tier_has_finite_nonnegative_effects_and_guidance(self):
        self.assertEqual(b.DATA['version'],5)
        for t in b.TRAITS:
            self.assertTrue(t['identity'] and t['usage'] and t['description'])
            self.assertEqual([r['count'] for r in t['tiers']],sorted(r['count'] for r in t['tiers']))
            for tier in t['tiers']:
                self.assertTrue(tier['text'])
                self.assertTrue(all(math.isfinite(v) and v>=0 for v in tier['bonus'].values()))


if __name__=='__main__':unittest.main()
