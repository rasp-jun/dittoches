"""Contract coverage for former NPCs becoming independent shop champions."""
import copy
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch
import combat_builds as builds
import combat_stats as stats
from server import Game, DEFS, POOL, Rejected

COSTS=dict(kuwagamon=3,shellmon=3,devimon=3,monochromon=3,ogremon=3,meramon=3,leomon=3,etemon=4,andromon=4,skullgreymon=4,piedmon=5,machinedramon=5)

class OpenRosterTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory()
        self.game=Game(Path(self.temp.name)/'roster.sqlite3')
        self.session=dict(id='roster',name='Roster',rating=1000,room='room')
        self.player=self.game.player(self.session)
        self.player.update(gold=100,level=9,board=[None]*28,bench=[None]*9,shop=[None]*5)
        self.room=dict(phase='prepare',players=[self.player,dict(ready=False)],pool={uid:POOL[d['cost']] for uid,d in DEFS.items()})
        self.game.rooms['room']=self.room
    def tearDown(self):
        self.game.db.close();self.temp.cleanup()
    def test_stage_prices_and_complete_playable_profiles(self):
        self.assertEqual({uid:DEFS[uid]['cost'] for uid in COSTS},COSTS)
        self.assertEqual(set(DEFS),set(stats.SKILLS))
        for uid,cost in COSTS.items():
            s=stats.SKILLS[uid]
            self.assertGreaterEqual(s['baseHealth'],700 if cost==3 else 1000)
            self.assertGreater(s['startMana'],0)
            tags=[t for t in builds.TRAITS if uid in t['members']]
            self.assertEqual(sum(t['category']=='전투' for t in tags),1)
            self.assertGreaterEqual(sum(t['category']=='계열' for t in tags),1)
    def test_every_champion_can_roll_at_its_cost_and_reserves_one_copy(self):
        for uid,cost in COSTS.items():
            self.player['shop']=[None]*5
            self.room['pool']={key:0 for key in DEFS};self.room['pool'][uid]=1
            with patch('server.random.choices',side_effect=lambda population,**kwargs:[cost] if isinstance(population,range) else [uid]):
                self.game.roll(self.room,self.player)
            self.assertEqual(self.player['shop'].count(uid),1)
            self.assertEqual(self.room['pool'][uid],0)
    def test_nine_direct_purchases_merge_keep_gear_and_sell_returns_nine(self):
        for uid,cost in COSTS.items():
            p=self.player;p.update(gold=100,board=[None]*28,bench=[None]*9,inventory=[])
            supply=POOL[cost];self.room['pool'][uid]=supply
            for n in range(9):
                p['shop']=[uid,None,None,None,None];self.room['pool'][uid]-=1
                self.game.action(self.session,dict(action='buy',slot=0))
                self.assertEqual(p['gold'],100-cost*(n+1))
                if n==0:next(u for u in p['bench'] if u)['items']=[0,3,8]
                old=copy.deepcopy(self.room)
                with self.assertRaises(Rejected):self.game.action(self.session,dict(action='buy',slot=0))
                self.assertEqual(self.room,old)
            survivors=[u for u in p['bench'] if u]
            self.assertEqual(survivors,[dict(id=uid,star=3,items=[0,3,8])])
            self.assertEqual(self.room['pool'][uid],supply-9)
            self.game.action(self.session,dict(action='sell',area='bench',slot=p['bench'].index(survivors[0])))
            self.assertEqual(p['gold'],100);self.assertEqual(self.room['pool'][uid],supply)
            self.assertEqual(p['inventory'],[0,3,8])
    def test_full_bench_merge_and_rejection_are_atomic_for_every_champion(self):
        for uid,cost in COSTS.items():
            p=self.player;p.update(gold=10,board=[None]*28,bench=[dict(id='koromon',star=1,items=[]) for _ in range(9)],shop=[uid,None,None,None,None])
            old=copy.deepcopy(self.room)
            with self.assertRaises(Rejected):self.game.action(self.session,dict(action='buy',slot=0))
            self.assertEqual(self.room,old)
            p['board'][:2]=[dict(id=uid,star=1,items=[0]),dict(id=uid,star=1,items=[3])]
            self.game.action(self.session,dict(action='buy',slot=0))
            self.assertEqual(p['board'][0],dict(id=uid,star=2,items=[0,3]))
            self.assertIsNone(p['board'][1]);self.assertEqual(p['gold'],10-cost)

if __name__=='__main__':unittest.main()
