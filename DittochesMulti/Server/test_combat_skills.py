import json
import math
import unittest
from pathlib import Path
from combat_skills import SKILLS, begin_cast, advance_cast, contains, duration, simulate


def fighter(key, unit='agumon', side=0, x=3, y=4):
    return dict(key=key, id=unit, side=side, star=1, x=x, y=y, hp=10000., maxHp=10000.,
                mana=100, maxMana=100, cooldown=0, stun=0, castUntil=0, target=-1)


class CanonicalSkillTests(unittest.TestCase):
    def test_every_playable_unit_and_creep_has_valid_skill(self):
        roster=json.loads((Path(__file__).parent/'roster.json').read_text(encoding='utf-8'))
        self.assertEqual(set(SKILLS), {u['id'] for u in roster}|{'kuwagamon','shellmon','devimon','etemon'})
        for s in SKILLS.values():
            with self.subTest(unit=s['id']):
                self.assertGreater(s['windup'],0)
                self.assertGreater(s['travel'],0)
                self.assertGreater(s['shots'],0)
                self.assertGreater(s['targets'],0)
                self.assertLess(duration(s),3)
                self.assertTrue(s['source'].startswith('https://wikimon.net/'))
                self.assertIn(s['shape'],('single','splash','line','cone','radial'))

    def test_all_skills_damage_only_on_impact_and_never_heal_or_hit_ally(self):
        for unit,s in SKILLS.items():
            with self.subTest(unit=unit):
                source=fighter(0,unit)
                target=fighter(1,side=1,y=4.4)
                ally=fighter(2,y=4.4)
                cast=begin_cast(source,target,0,1,100)
                fighters=[source,target,ally]
                advance_cast(cast,fighters,s['windup']+.001)
                self.assertEqual(target['hp'],10000)
                advance_cast(cast,fighters,duration(s))
                self.assertEqual(cast['hits'],s['shots'])
                self.assertAlmostEqual(target['hp'],9900)
                self.assertEqual(ally['hp'],10000)
                self.assertEqual(source['hp'],10000)
                advance_cast(cast,fighters,duration(s)+1)
                self.assertAlmostEqual(target['hp'],9900)

    def test_seven_heavens_has_seven_separate_impacts(self):
        s=SKILLS['seraphimon'];a=fighter(0,'seraphimon');b=fighter(1,side=1,y=4.5)
        c=begin_cast(a,b,0,1,70)
        for i in range(7):
            advance_cast(c,[a,b],s['windup']+s['travel']+i*s['interval'])
            self.assertEqual(c['hits'],i+1)
            self.assertAlmostEqual(b['hp'],10000-10*(i+1))

    def test_death_or_stun_interrupts_only_before_release(self):
        for cause in ('hp','stun'):
            for released in (False,True):
                with self.subTest(cause=cause,released=released):
                    a=fighter(0);b=fighter(1,side=1,y=4.4);c=begin_cast(a,b,0,1,100)
                    if released:advance_cast(c,[a,b],SKILLS['agumon']['windup'])
                    a[cause]=0 if cause=='hp' else 1
                    advance_cast(c,[a,b],1)
                    self.assertEqual(c['cancelled'],not released)
                    self.assertEqual(b['hp']<10000,released)

    def test_line_and_cone_do_not_hit_behind_or_outside(self):
        for shape in ('line','cone'):
            self.assertTrue(contains(shape,3,4,3,2,3,2,1,4))
            self.assertFalse(contains(shape,3,4,3,2,3,5,1,4))
            self.assertFalse(contains(shape,3,4,3,2,5,3,1,4))
            self.assertFalse(contains(shape,3,4,3,2,3,-1,1,4))
        self.assertTrue(contains('radial',3,4,3,2,3,5,1,0))
        self.assertFalse(contains('radial',3,4,3,2,3,5.1,1,0))

    def test_dead_target_retargets_during_windup_but_not_after_release(self):
        a=fighter(0);b=fighter(1,side=1,y=4.4);d=fighter(2,side=1,x=6,y=1)
        c=begin_cast(a,b,0,1,100);b['hp']=0
        advance_cast(c,[a,b,d],.1)
        self.assertEqual(c['target'],d['key'])
        a=fighter(0);b=fighter(1,side=1,y=4.4);d=fighter(2,side=1,x=6,y=1)
        c=begin_cast(a,b,0,1,100);advance_cast(c,[a,b,d],.33);b['hp']=0
        advance_cast(c,[a,b,d],1)
        self.assertEqual(c['target'],b['key'])
        self.assertEqual(d['hp'],10000)

    def test_simulation_snapshots_are_independent_deterministic_and_include_skills(self):
        defs={u['id']:u for u in json.loads((Path(__file__).parent/'roster.json').read_text(encoding='utf-8'))}
        def run():return simulate([fighter(0),fighter(1,'gabumon',1,3,3)],defs)
        frames,events,seconds=run()
        self.assertEqual((frames,events,seconds),run())
        self.assertTrue(events)
        self.assertGreater(seconds,frames[-1]['time'])
        self.assertEqual(frames[0]['units'][0]['hp'],10000)
        self.assertLess(frames[-1]['units'][0]['hp'],10000)
        self.assertTrue(all(math.isfinite(f['hp']) and 0<=f['mana']<=f['maxMana'] for frame in frames for f in frame['units']))
        self.assertEqual(len({e['serial'] for e in events}),len(events))


if __name__=='__main__':
    unittest.main()
