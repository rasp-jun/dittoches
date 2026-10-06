import copy
import tempfile
import unittest
from pathlib import Path
from server import Game, DEFS, Rejected


class RecruitmentTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.game = Game(Path(self.temp.name) / 'recruit.sqlite3')
        self.session = dict(id='test', name='test', rating=1000, room='room')
        self.p = self.game.player(self.session)
        self.p['board'] = [None] * 28
        filler = [i for i in DEFS if i != 'koromon'][:9]
        self.p['bench'] = [dict(id=i, star=1, items=[]) for i in filler]
        self.p['shop'] = ['koromon', None, None, None, None]
        self.room = dict(phase='prepare', players=[self.p, dict(ready=False)],
                         pool={i: 10 for i in DEFS})
        self.game.rooms['room'] = self.room

    def tearDown(self):
        self.game.db.close()
        self.temp.cleanup()

    def unit(self, star=1, items=()):
        return dict(id='koromon', star=star, items=list(items))

    def buy(self):
        self.game.action(self.session, dict(action='buy', slot=0))

    def test_full_bench_pair_locations_and_equipment(self):
        for areas in (('board','board'), ('board','bench'), ('bench','bench')):
            with self.subTest(areas=areas):
                before = copy.deepcopy(self.p)
                self.p[areas[0]][0] = self.unit(items=(8,12))
                self.p[areas[1]][1] = self.unit(items=(0,1))
                pool = self.room['pool'].copy()
                self.buy()
                self.assertEqual(self.p[areas[0]][0], self.unit(2, (8,12)))
                self.assertIsNone(self.p[areas[1]][1])
                self.assertEqual(self.p['gold'], 10-DEFS['koromon']['cost'])
                self.assertEqual(self.p['inventory'][-2:], [0,1])
                self.assertEqual(self.p['inventoryRevision'], 1)
                self.assertEqual(len(self.p['bench']), 9)
                self.assertEqual(self.room['pool'], pool)
                self.assertIsNone(self.p['shop'][0])
                settled = copy.deepcopy(self.p)
                with self.assertRaises(Rejected): self.buy()
                self.assertEqual(self.p, settled)
                self.p.clear(); self.p.update(before)

    def test_full_bench_cascades_to_three_stars_and_returns_reserved_offers(self):
        self.p['board'][0] = self.unit(2, (8,12))
        self.p['board'][1] = self.unit(2, (13,6))
        self.p['board'][2] = self.unit(1, (0,1))
        self.p['bench'][0] = self.unit(1, (2,3))
        self.p['shop'][1] = 'koromon'
        inventory = self.p['inventory'].copy()
        self.buy()
        self.assertEqual(self.p['board'][0], self.unit(3, (8,12)))
        self.assertIsNone(self.p['board'][1]); self.assertIsNone(self.p['board'][2])
        self.assertIsNone(self.p['bench'][0])
        self.assertCountEqual(self.p['inventory'], inventory+[0,1,2,3,13,6])
        self.assertEqual(self.p['shop'], [None]*5)
        self.assertEqual(self.room['pool']['koromon'], 11)
        self.assertEqual(len(self.p['bench']), 9)

    def test_rejected_purchase_does_not_mutate_player_or_pool(self):
        for reason in ('no_pair', 'only_one', 'wrong_star', 'no_gold', 'ready'):
            with self.subTest(reason=reason):
                self.p['board'] = [None]*28; self.p['gold']=10; self.p['ready']=False
                if reason != 'no_pair': self.p['board'][0]=self.unit()
                if reason in ('wrong_star', 'no_gold', 'ready'):
                    self.p['board'][1]=self.unit(2 if reason=='wrong_star' else 1)
                if reason=='no_gold': self.p['gold']=0
                if reason=='ready': self.p['ready']=True
                before=copy.deepcopy(self.room)
                with self.assertRaises(Rejected): self.buy()
                self.assertEqual(self.room, before)

    def test_open_bench_still_uses_normal_purchase_and_merge(self):
        self.p['board'][3]=self.unit(items=(8,))
        self.p['bench'][0]=self.unit(items=(12,))
        self.p['bench'][1]=None
        self.buy()
        self.assertEqual(self.p['board'][3], self.unit(2,(8,12)))
        self.assertIsNone(self.p['bench'][0]); self.assertIsNone(self.p['bench'][1])


if __name__ == '__main__': unittest.main()
