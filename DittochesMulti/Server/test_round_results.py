import copy
import secrets
import tempfile
import unittest
from pathlib import Path
from server import Game


class RoundResultTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory()
        self.game=Game(Path(self.temp.name)/'result.sqlite3',clock=lambda:1000.)
        self.keys=[secrets.token_hex(32) for _ in range(2)]
        self.tokens=[self.game.request('/login',dict(key=k,name='result'))['token'] for k in self.keys]
        for token in self.tokens:
            response=self.game.request('/queue',dict(mode='normal'),token)
        self.room=self.game.rooms[response['room']['id']]

    def tearDown(self):
        self.game.db.close();self.temp.cleanup()

    def snapshot(self,side=0):
        return self.game.request('/state',{},self.tokens[side])['room']

    def settle(self,winner=0):
        self.room.update(phase='battle',roundWinner=winner)
        self.game.settle(self.room)

    def test_victory_loss_draw_account_for_real_health_and_income(self):
        for winner in (0,1,-1):
            with self.subTest(winner=winner):
                self.room['round']=1
                for side,p in enumerate(self.room['players']):p.update(hp=100,gold=(19,55)[side])
                self.settle(winner)
                for side in (0,1):
                    snapshot=self.snapshot(side);r=snapshot['roundResult'];p=self.room['players'][side]
                    lost=0 if side==winner else 15 if winner<0 else 22
                    bonus=int(side==winner);interest=(1,5)[side]
                    self.assertEqual(r,dict(version=1,round=1,winner=winner,hpBefore=100,hpAfter=100-lost,
                        healthLost=lost,opponentHealthLost=0 if 1-side==winner else 15 if winner<0 else 22,
                        baseIncome=5,interest=interest,winBonus=bonus,income=5+interest+bonus))
                    self.assertEqual(p['gold'],(19,55)[side]+r['income'])
                    self.assertEqual(p['hp'],r['hpAfter'])
                    self.assertEqual(snapshot['roundWinner'],winner)
                    self.assertNotIn('roundResults',snapshot)
                    self.assertEqual(snapshot['players'][1-side]['gold'],0)

    def test_overkill_reports_actual_health_lost_and_final_round_is_stable(self):
        self.room['players'][1]['hp']=3
        self.settle(0)
        snapshot=self.snapshot(1);r=snapshot['roundResult']
        self.assertEqual(snapshot['phase'],'finished')
        self.assertEqual((r['hpBefore'],r['hpAfter'],r['healthLost']),(3,0,3))
        before=copy.deepcopy(self.room);self.game.settle(self.room)
        self.assertEqual(before,self.room)

    def test_new_battle_never_publishes_future_winner_or_replaces_prior_result(self):
        self.game.fight(self.room);self.room['roundWinner']=1
        self.assertIsNone(self.snapshot()['roundResult'])
        self.assertEqual(self.snapshot()['roundWinner'],-1)
        self.game.settle(self.room);completed=copy.deepcopy(self.snapshot()['roundResult'])
        self.game.fight(self.room);self.room['roundWinner']=0
        self.assertEqual(self.snapshot()['roundResult'],completed)
        self.assertEqual(self.snapshot()['roundWinner'],1)
        self.assertEqual(self.snapshot()['reportRound'],1)
        self.game.settle(self.room)
        self.assertEqual(self.snapshot()['roundResult']['round'],2)
        self.assertEqual(self.snapshot()['roundWinner'],0)

    def test_spending_and_reconnect_preserve_frozen_own_result(self):
        self.settle(0);original=copy.deepcopy(self.snapshot()['roundResult'])
        self.room['players'][0]['gold']=1
        reconnect=self.game.request('/login',dict(key=self.keys[0],name='again'))['room']
        self.assertEqual(reconnect['roundResult'],original)
        first=self.snapshot();first['roundResult']['income']=999
        self.assertEqual(self.snapshot()['roundResult'],original)

    def test_match_end_without_settlement_does_not_invent_result(self):
        self.game.fight(self.room);self.game.finish(self.room,self.room['players'][0]['id'])
        self.assertIsNone(self.snapshot()['roundResult'])
        self.assertEqual(self.snapshot()['roundWinner'],-1)


if __name__=='__main__':unittest.main()
