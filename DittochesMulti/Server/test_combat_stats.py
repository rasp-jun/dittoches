import copy
import math
import unittest
import combat_builds as builds
import combat_stats as stats
from combat_skills import begin_cast,advance_cast,duration,simulate
from server import DEFS


def unit(uid,items=(),star=1,team=None):
    return dict(key=0,id=uid,star=star,side=0,x=3.,y=4.,hp=10000.,maxHp=10000.,mana=100.,maxMana=100.,cooldown=0.,stun=0.,
                build=builds.resolve(uid,team or [uid],items))


class StatScalingTests(unittest.TestCase):
    def test_all_profiles_are_explicit_and_complete(self):
        self.assertEqual(len(stats.SKILLS),34)
        for s in stats.SKILLS.values():
            self.assertIn(s['damageType'],('physical','magic'))
            self.assertNotIn('multiplier',s)
            self.assertEqual(len(s['adRatio']),3);self.assertEqual(len(s['apRatio']),3)
            self.assertTrue(all(math.isfinite(v) and v>=0 for v in s['adRatio']+s['apRatio']))
            self.assertTrue(all(a+p>0 for a,p in zip(s['adRatio'],s['apRatio'])))
            self.assertIn(s['attackRange'],range(1,6));self.assertTrue(s['attackStyle'])
            for key in ('baseHealth','baseAttack','attackSpeed','maxMana'):self.assertGreater(s[key],0)
        for values in [i['bonus'] for i in builds.ITEMS]+[t['bonus'] for trait in builds.TRAITS for t in trait['tiers']]:
            self.assertNotIn('skill',values)

    def test_same_star_magic_changes_with_ap_but_not_ad(self):
        plain=unit('agumon');ad=unit('agumon',[0]);ap=unit('agumon',[3])
        self.assertEqual(stats.skill_damage(plain),120)
        self.assertEqual(stats.skill_damage(ad),120)
        self.assertAlmostEqual(stats.skill_damage(ap),134.4)
        self.assertAlmostEqual(stats.attack(ad),48*1.12)
        self.assertEqual(stats.attack(ap),48)

    def test_same_star_physical_changes_with_ad_but_not_ap(self):
        plain=unit('weregarurumon');ad=unit('weregarurumon',[0]);ap=unit('weregarurumon',[3])
        self.assertAlmostEqual(stats.skill_damage(plain),349.8)
        self.assertAlmostEqual(stats.skill_damage(ad),391.776)
        self.assertAlmostEqual(stats.skill_damage(ap),349.8)

    def test_hybrid_and_additive_item_synergy_stacking(self):
        base=unit('wargreymon');ad=unit('wargreymon',[0]);ap=unit('wargreymon',[3])
        self.assertEqual(stats.skill_damage(base),606)
        self.assertAlmostEqual(stats.skill_damage(ad),636.72)
        self.assertEqual(stats.skill_damage(ap),648)
        # Courage + Fighter and the two separate flat AP sources are additive.
        buff=unit('agumon',[0,3],team=['agumon','koromon'])
        self.assertAlmostEqual(stats.attack(buff),48*1.34)
        mage=unit('tentomon',[3],team=['tentomon','mochimon'])
        self.assertEqual(stats.ability_power(mage),122)
        self.assertAlmostEqual(stats.skill_damage(mage),109.8)

    def test_star_scaling_keeps_base_ap_at_100(self):
        self.assertEqual([stats.ability_power(unit('agumon',star=s)) for s in (1,2,3)],[100]*3)
        self.assertEqual([stats.skill_damage(unit('agumon',star=s)) for s in (1,2,3)],[120,180,270])
        self.assertAlmostEqual(stats.attack(unit('weregarurumon',star=3)),106*2.25)

    def test_actual_cast_uses_frozen_coefficients_and_typed_resistance(self):
        for uid,item in [('agumon',3),('weregarurumon',0),('wargreymon',3)]:
            source=unit(uid,[item]);target=unit('koromon');target.update(key=1,side=1,y=4.4,build={'armor':100,'magicResist':50})
            raw=stats.skill_damage(source);kind=stats.SKILLS[uid]['damageType']
            cast=begin_cast(source,target,0,1)
            source['build']['abilityPower']=9999;source['build']['attack']=99
            advance_cast(cast,[source,target],duration(stats.SKILLS[uid]))
            self.assertAlmostEqual(10000-target['hp'],raw/(2 if kind=='physical' else 1.5))

    def test_basic_attacks_stay_physical_with_ap_equipment(self):
        source=unit('agumon',[3]);target=unit('koromon');target.update(key=1,side=1,build={'armor':100,'magicResist':500})
        builds.damage(source,target,stats.attack(source),basic=True)
        self.assertEqual(target['hp'],9976)

    def test_hex_range_covers_six_neighbors_and_is_symmetric(self):
        for x,y in [(2,2),(4,2),(2,1),(3,1),(2,3),(3,3)]:
            self.assertEqual(stats.hex_distance(3,2,x,y),1)
        for x in range(7):
            for y in range(8):
                self.assertEqual(stats.hex_distance(3,2,x,y),stats.hex_distance(x,y,3,2))
        source=unit('wargreymon');target=unit('koromon');target.update(x=6,y=4)
        self.assertFalse(stats.in_attack_range(source,target))
        source['id']='metalgarurumon';self.assertTrue(stats.in_attack_range(source,target))

    def test_simulation_attacks_only_after_entering_its_own_range(self):
        for uid in ('weregarurumon','palmon','gabumon','metalgarurumon','hououmon'):
            source=unit(uid);target=unit('koromon');source.update(x=0,y=7);target.update(key=1,side=1,x=6,y=0)
            frames,_,_=simulate([source,target],DEFS)
            attacks=[frame for frame in frames if frame['units'][0]['attackAt']>=0]
            self.assertTrue(attacks,uid)
            # Check the first recorded basic attack, allowing the target's 200ms snapshot movement.
            first=attacks[0]['units'];a,b=first
            self.assertLessEqual(stats.hex_distance(a['x'],a['y'],b['x'],b['y']),stats.SKILLS[uid]['attackRange']+.5)


if __name__=='__main__':unittest.main()
