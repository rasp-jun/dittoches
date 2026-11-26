import copy
import secrets
import tempfile
import unittest
from pathlib import Path
from server import Game, Rejected, DEFS, POOL


class LeagueTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.now = 1000.
        self.game = Game(Path(self.temp.name)/'league.sqlite', clock=lambda:self.now)
        self.keys = [secrets.token_hex(32) for _ in range(8)]
        self.tokens = [self.game.request('/login',dict(name='테이머'+str(i),key=k))['token'] for i,k in enumerate(self.keys)]

    def tearDown(self):
        self.game.db.close()
        self.temp.cleanup()

    def request(self, side, path='/state', data=None):
        self.now += .1
        return self.game.request(path,data or {},self.tokens[side])

    def match(self, mode='normal8'):
        for i in range(7):
            response = self.request(i,'/queue',dict(mode=mode))
            self.assertIsNone(response['room'])
            self.assertEqual(response['queueTarget'],8)
        response=self.request(7,'/queue',dict(mode=mode))
        return self.game.rooms[response['room']['id']]

    def heartbeat(self):
        for token in self.tokens:
            self.game.sessions[token]['seen']=self.now

    def ready(self, room):
        for i,p in enumerate(room['players']):
            if not p.get('placement'):
                self.request(i,'/action',dict(action='ready'))
        self.assertEqual(room['phase'],'battle')

    def settle(self, room):
        self.now=room['deadline']+.01
        self.heartbeat()
        self.game.tick()

    def test_eight_unique_players_and_private_economy(self):
        room=self.match()
        self.assertEqual(len(room['players']),8)
        for i in range(8):
            view=self.request(i)['room']
            self.assertEqual(view['side'],i)
            self.assertEqual(view['aliveCount'],8)
            self.assertNotEqual(view['opponentSide'],i)
            for j,p in enumerate(view['players']):
                self.assertEqual(len(p['shop']),5 if i==j else 0)
                if i!=j:
                    self.assertEqual(p['gold'],0)
                    self.assertEqual(p['inventory'],[])

    def test_seven_round_pairings_have_no_repeat_and_ghosts_are_isolated(self):
        room=self.match()
        seen=[set() for _ in range(8)]
        for rnd in range(1,8):
            room['round']=rnd
            self.game.schedule_league(room)
            self.assertEqual(len(room['pairings']),4)
            for i,opponent in room['opponents'].items():
                self.assertNotIn(opponent,seen[i])
                seen[i].add(opponent)
        self.game.league_eliminate(room,[7])
        self.game.schedule_league(room)
        self.ready(room)
        ghosts=[d for d in room['battles'] if d['ghost']]
        self.assertEqual(len(ghosts),1)
        ghost=ghosts[0]
        self.assertIsNot(ghost['players'][1],room['players'][ghost['leagueSides'][1]])
        participants=[s for d in room['battles'] for s in d['leagueSides'][:1 if d['ghost'] else 2]]
        self.assertEqual(sorted(participants),list(range(7)))

    def test_parallel_replays_use_global_sides_and_equipment_is_local(self):
        room=self.match()
        room['players'][0]['board'][3]['items']=[4,12]
        room['players'][0]['inventory']=[8]
        self.ready(room)
        self.assertEqual(len(room['battles']),4)
        before=[copy.deepcopy(d['frames']) for d in room['battles']]
        for i in range(8):
            view=self.request(i)['room']
            sides={u['side'] for f in view['frames'] for u in f['units']}
            self.assertEqual(sides,{i,view['opponentSide']})
            self.assertEqual(view['battleSide'],self.game.league_battle(room,i)['leagueSides'].index(i))
        self.request(0,'/action',dict(action='equip',area='board',slot=3,itemSlot=0,inventoryRevision=0))
        affected=self.game.league_battle(room,0)
        self.assertTrue(affected['equipmentEvents'])
        self.assertEqual(room['players'][0]['board'][3]['items'],[4,12,8])
        self.assertEqual(affected['equipmentEvents'][-1]['items'],[4,12,8])
        for old,duel in zip(before,room['battles']):
            if duel is not affected:
                self.assertEqual(old,duel['frames'])
        self.assertEqual(room['deadline'],max(d['deadline'] for d in room['battles']))

    def test_forfeit_only_eliminates_one_and_pool_is_returned_once(self):
        room=self.match()
        self.request(3,'/leave')
        self.assertEqual(room['phase'],'prepare')
        self.assertEqual(room['players'][3]['placement'],8)
        pool=copy.deepcopy(room['pool'])
        self.game.league_eliminate(room,[3])
        self.assertEqual(pool,room['pool'])
        for uid,definition in DEFS.items():
            live=[p for p in room['players'] if not p.get('poolReleased')]
            held=sum(3**(u['star']-1) for p in live for u in p['board']+p['bench'] if u and u['id']==uid)
            offers=sum(p['shop'].count(uid) for p in live)
            self.assertEqual(pool[uid]+held+offers,POOL[definition['cost']])

    def test_eliminated_spectator_cannot_mutate_but_can_reconnect(self):
        room=self.match()
        self.game.league_eliminate(room,[6])
        for action in ('buy','ready','shop_lock','xp'):
            with self.assertRaises(Rejected):
                self.request(6,'/action',dict(action=action,slot=0,shopLocked=True))
        with self.assertRaises(Rejected):
            self.request(6,'/tamer-move',dict(x=3,y=6))
        recovered=self.game.request('/login',dict(name='테이머6',key=self.keys[6]))
        self.assertEqual(recovered['room']['placement'],8)
        self.assertEqual(len(recovered['room']['players']),8)

    def test_timeout_eliminates_absent_player_without_ending_league(self):
        room=self.match()
        self.now+=61
        for i,t in enumerate(self.tokens):
            if i!=5:self.game.sessions[t]['seen']=self.now
        self.game.tick()
        self.assertNotEqual(room['phase'],'finished')
        self.assertEqual(room['players'][5]['placement'],8)
        self.assertEqual(sum(not p.get('placement') for p in room['players']),7)

    def test_full_ranked_tournament_finishes_and_pays_once(self):
        room=self.match('ranked8')
        for _ in range(30):
            self.heartbeat()
            self.ready(room)
            self.settle(room)
            if room['phase']=='finished':break
        self.assertEqual(room['phase'],'finished')
        self.assertEqual(sorted(p['placement'] for p in room['players']),list(range(1,9)))
        ratings=[p['rating'] for p in room['players']]
        self.assertEqual(sum(ratings),8000)
        self.game.finish(room,room['winner'])
        self.assertEqual(ratings,[p['rating'] for p in room['players']])
        for i in range(8):
            view=self.request(i)['room']
            self.assertEqual(view['placement'],room['players'][i]['placement'])
            self.assertEqual(view['ratingDelta'],ratings[i]-1000)
            self.assertEqual(len(view['lastCombat'])>0,True)

    def test_simultaneous_elimination_has_unique_deterministic_places(self):
        room=self.match()
        for p in room['players']:p['hp']=1
        self.ready(room)
        self.settle(room)
        self.assertEqual(room['phase'],'finished')
        self.assertEqual(sorted(p['placement'] for p in room['players']),list(range(1,9)))

    def test_odd_survivors_receive_one_settlement_even_for_ghost_donor(self):
        room=self.match()
        room['players'][0]['hp']=1
        room['players'][0]['board']=[None]*28
        self.ready(room)
        self.settle(room)
        self.assertEqual(room['players'][0]['placement'],8)
        self.assertEqual(len(room['ghostOpponents']),1)
        before=[p['hp'] for p in room['players']]
        self.ready(room)
        self.settle(room)
        for i in range(1,8):
            self.assertEqual(room['players'][i]['hp'],before[i]-15)
            self.assertEqual(room['leagueResults'][i]['healthLost'],15)
        self.assertEqual(room['players'][0]['hp'],0)


if __name__=='__main__':
    unittest.main()
