import copy
import json
import unittest
from pathlib import Path
import combat_builds as b
from combat_skills import simulate


def fighter(key=0, unit='agumon', side=0, items=()):
    return dict(key=key,id=unit,side=side,star=1,x=3.,y=4.-side,hp=1000.,maxHp=1000.,
                mana=0.,maxMana=100.,cooldown=0.,items=list(items),stun=0.)


class BuildTests(unittest.TestCase):
    def test_roster_has_two_traits_and_all_breakpoints_are_attainable(self):
        roster=json.loads((Path(__file__).parent/'roster.json').read_text(encoding='utf-8'))
        ids={u['id'] for u in roster}
        self.assertEqual(len(b.TRAITS),11)
        for unit in ids:
            tags=[t for t in b.TRAITS if unit in t['members']]
            self.assertEqual({t['category'] for t in tags},{'문장','전투'})
            self.assertEqual(len(tags),2)
        for t in b.TRAITS:
            self.assertTrue(set(t['members'])<=ids)
            self.assertEqual(len(t['members']),len(set(t['members'])))
            for i,tier in enumerate(t['tiers']):
                self.assertLessEqual(tier['count'],len(t['members']))
                self.assertEqual(b.level(t,t['members'][:tier['count']]),i+1)
                self.assertEqual(b.level(t,t['members'][:tier['count']-1]),i)

    def test_duplicates_and_star_levels_do_not_increase_trait_count(self):
        t=b.TRAITS[0]
        self.assertEqual(b.level(t,['agumon']*9),0)
        self.assertEqual(b.resolve('agumon',['agumon','koromon']),b.resolve('agumon',['agumon']*4+['koromon']))

    def test_all_ten_recipes_are_symmetric_and_exclude_capsules(self):
        results=[]
        for a in range(4):
            for c in range(a,4):
                result=b.combine(a,c);results.append(result)
                self.assertEqual(result,b.combine(c,a))
                self.assertEqual(b.ITEMS[result]['kind'],'completed')
        self.assertEqual(set(results),set(range(4,14)))
        self.assertEqual(len(results),len(set(results)))
        for item in b.ITEMS[:14]:
            self.assertNotIn('캡슐',item['name'])
            self.assertNotIn('문장',item['name'])
            self.assertTrue(item['source'].startswith('https://digimon.net/'))
            self.assertTrue(item['bonus'])
        for a,c in ((-1,0),(4,1),(14,0),(True,0),(0,'1')):
            self.assertIsNone(b.combine(a,c))

    def test_teams_resolve_independently_and_harmonizer_aura_is_once(self):
        fs=[fighter(0,'palmon'),fighter(1,'patamon'),fighter(2),fighter(3,side=1)]
        b.initialize(fs)
        self.assertAlmostEqual(fs[2]['shield'],50)
        self.assertEqual(fs[3]['shield'],0)
        self.assertEqual(b.value(fs[2],'castHeal'),0)
        self.assertEqual(b.value(fs[0],'castHeal'),.04)
        self.assertEqual(b.value(fs[2],'attack'),0)

    def test_equipment_flat_health_stacks_before_trait_multiplier(self):
        fs=[fighter(0,'greymon',items=[8]),fighter(1,'togemon')]
        b.initialize(fs)
        self.assertAlmostEqual(fs[0]['maxHp'],1300*1.15)
        self.assertAlmostEqual(b.value(fs[0],'reduction'),.17)

    def test_low_health_shield_once_and_no_resurrection(self):
        source,target=fighter(),fighter(1,side=1)
        target['build']={'lowShield':.25}
        b.damage(source,target,700)
        self.assertEqual(target['shield'],250)
        b.damage(source,target,260)
        self.assertEqual(target['shield'],0)
        self.assertEqual(target['hp'],290)
        b.damage(source,target,500)
        self.assertEqual(target['hp'],0)
        self.assertEqual(b.heal(source,target,1000),0)
        self.assertEqual(b.shield(source,target,1000),0)

    def test_lifesteal_uses_actual_health_loss_not_shields_or_overkill(self):
        source,target=fighter(),fighter(1,side=1)
        source['hp']=500;source['build']={'lifesteal':.5};target['shield']=100
        b.damage(source,target,100,basic=True)
        self.assertEqual(source['hp'],500)
        target['hp']=20
        b.damage(source,target,1000,basic=True)
        self.assertEqual(source['hp'],510)
        target['hp']=100
        b.damage(source,target,50,basic=False)
        self.assertEqual(source['hp'],510)

    def test_reduction_and_shield_caps_and_full_health_bonus(self):
        source,target=fighter(),fighter(1,side=1)
        source['build']={'highHealthDamage':.5};target['build']={'reduction':.9}
        b.shield(target,target,9999)
        self.assertEqual(target['shield'],500)
        b.damage(source,target,1000)
        self.assertAlmostEqual(target['hp'],900)
        self.assertEqual(target['shield'],0)

    def test_regen_heal_amplification_and_cast_targeting(self):
        source,ally,enemy=fighter(),fighter(1),fighter(2,side=1)
        source['build']={'regen':.01,'healPower':.2,'castHeal':.1,'castShield':.12}
        source['hp']=800;ally['hp']=200;enemy['hp']=1
        for _ in range(20):b.tick(source,.05)
        self.assertAlmostEqual(source['hp'],812)
        b.on_cast(source,[source,ally,enemy])
        self.assertEqual(ally['hp'],320)
        self.assertEqual(source['shield'],120)
        self.assertEqual(enemy['hp'],1)

    def test_third_shot_stun_only_for_basic_attacks(self):
        source,target=fighter(),fighter(1,side=1)
        source['build']={'thirdStun':.4}
        source['attacks']=2;b.damage(source,target,1,True)
        self.assertEqual(target['stun'],0)
        source['attacks']=3;b.damage(source,target,1,False)
        self.assertEqual(target['stun'],0)
        b.damage(source,target,1,True)
        self.assertEqual(target['stun'],.4)

    def test_frozen_builds_and_deterministic_simulation(self):
        roster=json.loads((Path(__file__).parent/'roster.json').read_text(encoding='utf-8'))
        defs={u['id']:u for u in roster}
        fs=[fighter(0,'koromon',items=[4]),fighter(1,'agumon'),fighter(2,'gabumon',1),fighter(3,'tsunomon',1)]
        original=copy.deepcopy(fs)
        result=simulate(fs,defs)
        self.assertEqual(result,simulate(copy.deepcopy(original),defs))
        self.assertGreater(b.value(fs[0],'attack'),.35)
        fs[1]['hp']=0
        self.assertGreater(b.value(fs[0],'attack'),.35)
        self.assertGreater(result[0][0]['units'][2]['shield'],0)
        self.assertNotIn('build',result[0][0]['units'][0])


if __name__=='__main__':unittest.main()
