import copy
import secrets
import tempfile
import unittest
from pathlib import Path
import combat_builds as builds
from combat_skills import simulate
from server import Game, DEFS


def fighter(key=0, side=0, uid='agumon', items=()):
    return dict(key=key, side=side, id=uid, star=1, slot=3, x=3., y=4.-side, hp=100., maxHp=100.,
                mana=0., maxMana=100., shield=0., cooldown=0., items=list(items), build={})


class CombatReportTests(unittest.TestCase):
    def test_typed_damage_shield_absorption_and_overkill_accounting(self):
        source, target = fighter(), fighter(1, 1)
        target.update(shield=40, build={'armor': 100})
        self.assertEqual(builds.damage(source, target, 120, basic=True), 20)
        self.assertEqual((source['damageDone'], source['basicDamageDone']), (20, 20))
        self.assertEqual((target['damageTaken'], target['shieldAbsorbed']), (20, 40))
        self.assertEqual(builds.damage(source, target, 500, damage_type='magic'), 80)
        self.assertEqual((source['damageDone'], source['basicDamageDone'], source['skillDamageDone']), (100, 20, 80))
        self.assertEqual(target['damageTaken'], 100)
        before = copy.deepcopy((source, target))
        builds.damage(source, target, 999, basic=True)
        self.assertEqual((source, target), before)

    def test_absorbed_damage_is_not_health_damage(self):
        source, target = fighter(), fighter(1, 1)
        target['shield'] = 90
        builds.damage(source, target, 80, basic=True)
        self.assertEqual(source['damageDone'], 0)
        self.assertEqual(source['basicDamageDone'], 0)
        self.assertEqual((target['damageTaken'], target['shieldAbsorbed']), (0, 80))

    def test_healing_and_shield_creation_do_not_count_overflow(self):
        source, target = fighter(), fighter(1, 0)
        target['hp'] = 90
        builds.heal(source, target, 100)
        builds.heal(source, target, 100)
        builds.shield(source, target, 100)
        builds.shield(source, target, 100)
        self.assertEqual((source['healingDone'], source['shieldingDone']), (10, 50))

    def test_simulation_resets_counters_and_snapshots_preserve_totals(self):
        source, target = fighter(items=[13]), fighter(1, 1, 'weregarurumon', [8])
        source['damageDone'] = source['casts'] = 999
        frames, events, _ = simulate([source, target], DEFS)
        self.assertEqual(frames[0]['units'][0]['damageDone'], 0)
        self.assertEqual(frames[0]['units'][0]['casts'], 0)
        self.assertGreater(sum(f['damageDone'] for f in frames[-1]['units']), 0)
        for frame in frames:
            units = frame['units']
            self.assertAlmostEqual(sum(f['damageDone'] for f in units), sum(f['damageTaken'] for f in units))
            self.assertLessEqual(sum(f['shieldAbsorbed'] for f in units), sum(f['shieldingDone'] for f in units)+1e-6)
            for f in units:
                self.assertAlmostEqual(f['damageDone'], f['basicDamageDone']+f['skillDamageDone'])
                self.assertGreaterEqual(f['casts'], 0)


class ReportPersistenceTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.game = Game(Path(self.temp.name)/'report.sqlite3', clock=lambda:1000.)
        self.keys = [secrets.token_hex(32) for _ in range(2)]
        self.tokens = [self.game.request('/login', {'key':key, 'name':'report'})['token'] for key in self.keys]
        for token in self.tokens:
            response = self.game.request('/queue', {'mode':'normal'}, token)
        self.room = self.game.rooms[response['room']['id']]

    def tearDown(self):
        self.game.db.close()
        self.temp.cleanup()

    def snapshot(self):
        return self.game.request('/state', {}, self.tokens[0])['room']

    def test_completed_report_survives_prepare_and_reconnect_without_private_data(self):
        self.game.fight(self.room)
        self.assertEqual(self.snapshot()['lastCombat'], [])
        self.game.settle(self.room)
        snapshot = self.snapshot()
        self.assertEqual((snapshot['phase'], snapshot['round'], snapshot['reportRound']), ('prepare', 2, 1))
        self.assertEqual(snapshot['frames'], [])
        report = copy.deepcopy(snapshot['lastCombat'])
        self.assertEqual(len(report), 2)
        self.assertGreater(sum(f['damageDone'] for f in report), 0)
        for f in report:
            self.assertTrue(set(f).isdisjoint({'items','inventory','bench','build','cooldown'}))
        self.room['players'][0]['board'][3] = None
        reconnected = self.game.request('/login', {'key':self.keys[0], 'name':'returned'})['room']
        self.assertEqual(reconnected['lastCombat'], report)
        self.game.fight(self.room)
        self.assertEqual(self.snapshot()['lastCombat'], report)

    def test_final_round_keeps_report_and_repeated_settlement_does_not_change_it(self):
        self.room['round'] = 10
        self.game.fight(self.room)
        self.game.settle(self.room)
        snapshot = self.snapshot()
        self.assertEqual(snapshot['phase'], 'finished')
        self.assertEqual(snapshot['reportRound'], 10)
        self.game.settle(self.room)
        self.assertEqual(self.snapshot()['lastCombat'], snapshot['lastCombat'])


if __name__ == '__main__':
    unittest.main()
