import copy
import secrets
import tempfile
import unittest
from collections import Counter
from pathlib import Path
from unittest.mock import patch
from server import Game,DEFS,POOL,Rejected


class ShopLockTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.now=1000.
        self.game=Game(Path(self.temp.name)/'shop.sqlite3',clock=lambda:self.now)
        self.keys=[secrets.token_hex(32) for _ in range(2)]
        self.tokens=[self.game.request('/login',dict(key=k,name='shop'))['token'] for k in self.keys]
        for token in self.tokens:s=self.game.request('/queue',dict(mode='normal'),token)
        self.room=self.game.rooms[s['room']['id']];self.p=self.room['players'][0]

    def tearDown(self):
        self.game.db.close();self.temp.cleanup()

    def action(self,**data):
        self.now+=.1
        return self.game.request('/action',data,self.tokens[0])

    def check_pool(self):
        total=Counter(self.room['pool'])
        for p in self.room['players']:
            total.update(u for u in p['shop'] if u)
            for u in p['board']+p['bench']:
                if u:total[u['id']]+=3**(u['star']-1)
        self.assertEqual(total,{uid:POOL[d['cost']] for uid,d in DEFS.items()})

    def test_lock_and_duplicate_request_only_change_requested_flag(self):
        self.assertFalse(self.p['shopLocked'])
        before=copy.deepcopy(self.p);pool=self.room['pool'].copy()
        self.action(action='shop_lock',shopLocked=True)
        self.action(action='shop_lock',shopLocked=True)
        before['shopLocked']=True
        self.assertEqual(self.p,before);self.assertEqual(self.room['pool'],pool)
        self.action(action='shop_lock',shopLocked=False)
        self.assertFalse(self.p['shopLocked'])

    def test_locked_shop_keeps_bought_hole_across_round_and_reserves_pool(self):
        self.action(action='shop_lock',shopLocked=True)
        self.action(action='buy',slot=0);offers=self.p['shop'].copy()
        self.check_pool();self.game.fight(self.room)
        with patch.object(self.game,'roll',wraps=self.game.roll) as roll:
            self.game.settle(self.room)
            self.assertEqual(roll.call_count,1)
        self.assertEqual(self.p['shop'],offers);self.assertIsNone(self.p['shop'][0])
        self.assertTrue(self.p['shopLocked']);self.check_pool()

    def test_unlock_does_not_roll_now_and_next_round_rolls_both(self):
        self.action(action='shop_lock',shopLocked=True);offers=self.p['shop'].copy()
        self.action(action='shop_lock',shopLocked=False)
        self.assertEqual(self.p['shop'],offers)
        self.game.fight(self.room)
        with patch.object(self.game,'roll',wraps=self.game.roll) as roll:
            self.game.settle(self.room)
            self.assertEqual(roll.call_count,2)
        self.check_pool()

    def test_manual_reroll_still_costs_two_and_keeps_lock(self):
        self.action(action='shop_lock',shopLocked=True);gold=self.p['gold']
        with patch.object(self.game,'roll',wraps=self.game.roll) as roll:
            self.action(action='reroll');self.assertEqual(roll.call_count,1)
        self.assertEqual(self.p['gold'],gold-2);self.assertTrue(self.p['shopLocked']);self.check_pool()

    def test_can_set_lock_while_ready_or_battling_without_changing_combat(self):
        self.action(action='ready');self.action(action='shop_lock',shopLocked=True)
        self.assertTrue(self.p['ready'])
        self.game.fight(self.room);frames=copy.deepcopy(self.room['frames']);offers=self.p['shop'].copy()
        self.action(action='shop_lock',shopLocked=False)
        self.assertEqual(self.room['frames'],frames);self.assertEqual(self.p['shop'],offers)
        with self.assertRaises(Rejected):self.action(action='reroll')

    def test_reconnect_retains_lock_and_opponent_cannot_see_it(self):
        self.action(action='shop_lock',shopLocked=True)
        state=self.game.request('/login',dict(key=self.keys[0],name='returned'))
        self.assertTrue(state['room']['players'][0]['shopLocked'])
        other=self.game.request('/state',{},self.tokens[1])['room']['players'][0]
        self.assertFalse(other['shopLocked']);self.assertEqual(other['shop'],[])

    def test_invalid_flags_and_finished_room_do_not_mutate(self):
        for value in (None,0,1,'true',[],{}):
            before=copy.deepcopy(self.p)
            with self.assertRaises(Rejected):self.action(action='shop_lock',shopLocked=value)
            self.assertEqual(self.p,before)
        self.game.finish(self.room,'')
        before=copy.deepcopy(self.p)
        with self.assertRaises(Rejected):self.action(action='shop_lock',shopLocked=True)
        self.assertEqual(self.p,before)


if __name__=='__main__':unittest.main()
