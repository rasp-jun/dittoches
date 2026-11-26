import copy
import json
import math
import secrets
import tempfile
import unittest
from pathlib import Path
from server import Game, Rejected
from tamer_motion import cell_world, position


class TamerSyncTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.now = 1000.
        self.game = Game(Path(self.temp.name)/'movement.sqlite', clock=lambda: self.now)
        self.keys = [secrets.token_hex(32) for _ in range(8)]
        self.tokens = [self.game.request('/login', dict(name=f'Tamer {i}', key=k))['token'] for i,k in enumerate(self.keys)]
        for t in self.tokens:
            self.game.request('/queue', dict(mode='normal8'), t)
        self.room = next(iter(self.game.rooms.values()))

    def tearDown(self):
        self.game.db.close()
        self.temp.cleanup()

    def sync(self, side=7, **data):
        return self.game.request('/tamer-sync', dict(expectedRoom=self.room['id'], **data), self.tokens[side])

    def move(self, side=7, sequence=1, x=6, y=4):
        return self.sync(side, tamerMove=True, tamerSequence=sequence, x=x, y=y)

    def test_eight_independent_live_paths_and_private_state(self):
        before=copy.deepcopy(self.room)
        for i in range(8):
            self.move(i, x=i%7, y=4+i%4)
        self.now+=.2
        packet=self.sync()
        self.assertEqual(packet['side'], 7)
        for p in packet['players']:
            side=p['side']; player=self.room['players'][side]
            self.assertEqual((p['x'], p['y']), (side%7, 4+side%4))
            self.assertEqual((p['worldX'], p['worldZ']), position(player, self.now))
            start=cell_world(3,7)
            self.assertLessEqual(math.dist(start, (p['worldX'],p['worldZ'])), .800001)
            expected=before['players'][side]
            for key in ('tamerX','tamerY','tamerSequence','_tamerMotion'):expected[key]=player[key]
        self.assertEqual(before,self.room)
        self.assertLess(len(json.dumps(packet).encode()), 2200)
        self.assertFalse({'token','frames','shop','inventory','gold'} & set(packet))
        self.assertTrue(all(set(p)=={'side','sequence','x','y','worldX','worldZ'} for p in packet['players']))

    def test_turning_duplicate_and_out_of_order_delivery(self):
        self.move(x=0,y=4)
        self.now+=.25
        at_turn=position(self.room['players'][7],self.now)
        self.move(sequence=3,x=6,y=7)
        self.assertEqual(at_turn,position(self.room['players'][7],self.now))
        motion=self.room['players'][7]['_tamerMotion']
        self.now+=.25
        self.move(sequence=3,x=6,y=7)  # retry after a lost response
        self.move(sequence=2,x=0,y=4)  # older destination cannot win
        self.assertEqual(self.room['players'][7]['_tamerMotion'],motion)
        self.assertEqual(self.sync()['players'][7]['x'],6)

    def test_phase_change_does_not_drop_valid_movement_and_reconnect_keeps_it(self):
        self.move(x=0,y=4)
        self.now+=.2
        self.room['phase']='battle';self.room['deadline']=self.now+40
        self.move(sequence=2,x=6,y=7)
        response=self.game.request('/login',dict(name='Reconnect',key=self.keys[7]))
        self.assertEqual(response['tamers']['players'][7]['sequence'],2)
        self.assertEqual(response['tamerSyncVersion'],1)
        self.assertEqual(response['tamers']['room'],self.room['id'])

    def test_stale_room_bad_sequence_bounds_and_elimination(self):
        before=copy.deepcopy(self.room)
        for data in (dict(expectedRoom='old',tamerMove=True,tamerSequence=1,x=6,y=4),
                     *[dict(expectedRoom=self.room['id'],tamerMove=True,tamerSequence=v,x=6,y=4) for v in (0,-1,True,'1',2147483647)],
                     dict(expectedRoom=self.room['id'],tamerMove=True,tamerSequence=1,x=float('nan'),y=4)):
            with self.assertRaises(Rejected):self.game.request('/tamer-sync',data,self.tokens[7])
            self.assertEqual(before,self.room)
        self.room['players'][7]['placement']=8
        with self.assertRaises(Rejected):self.move()
        self.assertEqual(self.sync()['players'][7]['sequence'],0) # spectators still receive motion

    def test_rate_limit_and_independent_economy_lane(self):
        self.move()
        with self.assertRaises(Rejected):self.move(sequence=2,x=0,y=4)
        # Movement and economy throttle independently; neither consumes the other.
        self.game.request('/action',dict(action='buy',slot=0),self.tokens[7])
        self.now+=.12
        self.move(sequence=2,x=0,y=4)
        self.assertEqual(self.sync()['players'][7]['sequence'],2)

    def test_arrival_stops_exactly_at_target(self):
        self.move(x=0,y=4)
        self.now+=4
        p=self.sync()['players'][7]
        self.assertEqual((p['worldX'],p['worldZ']),cell_world(0,4))


if __name__=='__main__':unittest.main()
