import copy
import json
import re
import tempfile
import unittest
from collections import Counter
from pathlib import Path

import combat_builds as builds
import combat_stats as stats
from combat_skills import simulate
from server import Game, DEFS, ROSTER, Rejected

LINE = ('nyaromon', 'plotmon', 'tailmon', 'angewomon', 'ophanimon')


def fighter(key, uid='nyaromon', side=0):
    s = stats.SKILLS[uid]
    return dict(key=key, id=uid, side=side, star=1, items=[], x=float(key % 7),
                y=5. if side == 0 else 2., hp=s['baseHealth'], maxHp=s['baseHealth'],
                mana=s['startMana'], maxMana=s['maxMana'], cooldown=0., stun=0.)


class LightLineTests(unittest.TestCase):
    def test_all_catalogs_have_47_players_evolution_costs_and_reachable_traits(self):
        client = json.loads((Path(__file__).resolve().parent.parent/'Assets/Resources/MultiRoster.json').read_text(encoding='utf-8'))['units']
        self.assertEqual(ROSTER, client)
        native=(Path(__file__).resolve().parent.parent/'Assets/Scripts/NativeGame.cs').read_text(encoding='utf-8')
        native=native.split('private static readonly UnitDef[] Roster = {',1)[1].split('};',1)[0]
        definitions=[dict(id=uid,name=name,cost=int(cost),sprite=sprite,role=role) for uid,name,cost,sprite,role in re.findall(r'new UnitDef\("([^\"]+)","([^\"]+)",(\d),"([^\"]+)","([^\"]+)"\)',native)]
        self.assertEqual(ROSTER,definitions)
        self.assertEqual(len(DEFS), 47)
        self.assertEqual(Counter(u['cost'] for u in ROSTER), {1:7,2:7,3:14,4:10,5:9})
        self.assertEqual(DEFS['nyaromon']['name'], '야옹몬')
        self.assertEqual({uid: DEFS[uid]['cost'] for uid in LINE}, dict(zip(LINE, range(1, 6))))
        for trait in builds.TRAITS:
            self.assertGreaterEqual(len(trait['members']), trait['tiers'][-1]['count'])
            self.assertEqual([r['count'] for r in trait['tiers']], sorted(set(r['count'] for r in trait['tiers'])))

    def test_light_thresholds_apply_to_members_and_exclude_duplicates(self):
        light = next(t for t in builds.TRAITS if t['id']=='light')
        for count, mana, shield in ((1,0,0),(2,0,0),(3,10,.035),(4,10,.035),(5,10,.035),(6,25,.07),(8,25,.07),(9,45,.12)):
            team = light['members'][:count]
            bonus = builds.resolve(team[0], team)
            self.assertEqual(bonus.get('startMana', 0), mana)
            self.assertAlmostEqual(bonus.get('castAllyShield', 0), shield)
            self.assertEqual(builds.level(light, team*3), builds.level(light, team))
            self.assertEqual(builds.resolve('agumon', team).get('castAllyShield', 0), 0)

    def test_cast_shield_targets_lowest_fraction_living_ally_and_caps(self):
        caster, ally, low_raw, dead, enemy = [fighter(i) for i in range(5)]
        enemy['side'] = 1
        units = [caster, ally, low_raw, dead, enemy]
        builds.initialize(units)
        caster['build'] = dict(castAllyShield=.11, healPower=5)
        caster.update(maxHp=1000., hp=1000., shield=0.)
        ally.update(maxHp=2000., hp=600., shield=990.)
        low_raw.update(maxHp=500., hp=200., shield=0.)
        dead.update(hp=0., shield=0.)
        enemy.update(hp=1., shield=0.)
        builds.on_cast(caster, units)
        self.assertEqual(ally['shield'], 1000.)
        self.assertEqual(caster['shieldingDone'], 10.)
        self.assertEqual(ally['hp'], 600.)
        self.assertEqual([u['shield'] for u in (caster, low_raw, dead, enemy)], [0]*4)
        builds.on_cast(caster, units)
        self.assertEqual(caster['shieldingDone'], 10.)
        caster['hp']=0.
        ally['shield']=0.
        builds.on_cast(caster, units)
        self.assertEqual(ally['shield'], 0.)

    def test_shield_can_protect_self_and_ties_use_stable_key(self):
        caster, ally = fighter(0), fighter(1)
        builds.initialize([caster, ally])
        caster['build'] = dict(castAllyShield=.04)
        builds.on_cast(caster, [ally, caster])
        self.assertAlmostEqual(caster['shield'], caster['maxHp']*.04)
        self.assertEqual(ally['shield'], 0.)

    def test_heal_then_ally_shield_and_self_shield_stack_in_order(self):
        caster, a, b = [fighter(i) for i in range(3)]
        units=[caster, a, b]
        builds.initialize(units)
        for u in units:
            u.update(maxHp=1000., hp=1000., shield=0.)
        a['hp']=100.; b['hp']=150.
        caster['build']=dict(castHeal=.1, castShield=.05, castAllyShield=.07)
        builds.on_cast(caster, units)
        self.assertEqual(a['hp'], 200.)
        self.assertEqual(a['shield'], 0.)
        self.assertEqual(b['shield'], 70.)
        self.assertEqual(caster['shield'], 50.)
        self.assertEqual(caster['shieldingDone'], 120.)

    def test_new_skill_damage_uses_shared_ad_ap_and_stars(self):
        expected = (64., 100., 216., 325.6, 440.)
        for uid, base in zip(LINE, expected):
            for star in (1, 2, 3):
                f = fighter(0, uid); f.update(star=star, build={})
                self.assertAlmostEqual(stats.skill_damage(f), base*1.5**(star-1))
                s = stats.SKILLS[uid]
                self.assertEqual(s['source'].split('?')[0], 'https://digimon.net/reference_en/detail.php')

    def test_light_team_combat_reports_real_shields_and_is_deterministic(self):
        units = [fighter(i, uid) for i, uid in enumerate(LINE)]
        units += [fighter(i+5, uid, 1) for i, uid in enumerate(('greymon','weregarurumon','metalgreymon','hououmon','seraphimon'))]
        # Durable targets exercise every low-tier cast before a high-tier opponent can end combat.
        for f in units:
            f['hp']=f['maxHp']=10000.
        original = copy.deepcopy(units)
        result = simulate(units, DEFS)
        self.assertEqual(result, simulate(copy.deepcopy(original), DEFS))
        self.assertGreater(sum(u['shieldingDone'] for u in units[:5]), 0)
        self.assertGreater(sum(u['skillDamageDone'] for u in units[:5]), 0)
        self.assertEqual({e['id'] for e in result[1] if e['caster']<5}, set(LINE))
        for f in units:
            self.assertLessEqual(f['shield'], f['maxHp']*.5+.0001)
        self.assertAlmostEqual(sum(u['damageDone'] for u in units), sum(u['damageTaken'] for u in units))

    def test_each_new_unit_can_be_bought_and_merged_with_gear_preserved(self):
        with tempfile.TemporaryDirectory() as directory:
            game = Game(Path(directory)/'light.sqlite3')
            try:
                session = dict(id='light', name='Light', rating=1000, room='room')
                for uid in LINE:
                    p=game.player(session); p.update(gold=20, board=[None]*28, bench=[None]*9, shop=[uid,None,None,None,None])
                    p['board'][0]=dict(id=uid,star=1,items=[8])
                    p['bench'][0]=dict(id=uid,star=1,items=[12])
                    room=dict(phase='prepare',players=[p,dict(ready=False)],pool={i:10 for i in DEFS})
                    game.rooms['room']=room
                    game.action(session,dict(action='buy',slot=0))
                    self.assertEqual(p['board'][0],dict(id=uid,star=2,items=[8,12]))
                    self.assertIsNone(p['bench'][0])
                    self.assertEqual(p['gold'],20-DEFS[uid]['cost'])
                    snapshot=copy.deepcopy(room)
                    with self.assertRaises(Rejected): game.action(session,dict(action='buy',slot=0))
                    self.assertEqual(room,snapshot)
            finally:
                game.db.close()


if __name__=='__main__': unittest.main()
