import json
import secrets
import tempfile
import threading
import unittest
import urllib.error
import urllib.request
from pathlib import Path
from http.server import ThreadingHTTPServer
from server import Game, Handler, Rejected, DEFS, POOL

class ServerTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory()
        self.now=1000.0
        self.game=Game(Path(self.temp.name)/'test.sqlite3',clock=lambda:self.now)
        self.keys=[secrets.token_hex(32) for _ in range(4)]
        self.tokens=[self.game.request('/login',{'name':f'테이머{i}','key':key})['token'] for i,key in enumerate(self.keys)]

    def tearDown(self):
        self.game.db.close()
        self.temp.cleanup()

    def request(self, path, data=None, who=0):
        self.now+=.1
        return self.game.request(path,data or {},self.tokens[who])

    def match(self, mode='normal'):
        self.request('/queue',{'mode':mode})
        result=self.request('/queue',{'mode':mode},1)
        return self.game.rooms[result['room']['id']]

    def test_real_opponents_and_private_state(self):
        self.match()
        a,b=self.request('/state'),self.request('/state',who=1)
        self.assertEqual(a['room']['id'],b['room']['id'])
        self.assertNotEqual(a['room']['side'],b['room']['side'])
        self.assertEqual(a['room']['players'][1]['shop'],[])
        self.assertEqual(len(a['room']['players'][0]['shop']),5)

    def test_queues_are_separate_and_cancel_works(self):
        self.request('/queue',{'mode':'normal'})
        other=self.request('/queue',{'mode':'ranked'},1)
        self.assertIsNone(other['room'])
        self.request('/cancel')
        self.assertEqual(self.game.queues['normal'],[])

    def test_same_identity_and_repeated_queue_cannot_self_match(self):
        reconnect=self.game.request('/login',{'name':'same','key':self.keys[0]})
        self.assertEqual(reconnect['token'],self.tokens[0])
        for _ in range(3):
            result=self.request('/queue',{'mode':'normal'})
        self.assertIsNone(result['room'])
        self.assertEqual(result['waiting'],1)

    def test_unauthorized_and_invalid_input(self):
        with self.assertRaises(Rejected):
            self.game.request('/state',{},'invalid')
        with self.assertRaises(Rejected):
            self.game.request('/login',{'name':'a','key':'guessable'})
        with self.assertRaises(Rejected):
            self.request('/queue',{'mode':'fake'})
        self.match()
        for slot in (-1,5,True,'0'):
            with self.assertRaises(Rejected):
                self.request('/action',{'action':'buy','slot':slot})

    def test_server_owns_gold_and_purchase(self):
        room=self.match()
        player=room['players'][0]
        slot=next(i for i,u in enumerate(player['shop']) if u)
        cost=DEFS[player['shop'][slot]]['cost']
        self.request('/action',{'action':'buy','slot':slot,'gold':999999})
        self.assertEqual(player['gold'],10-cost)
        self.assertEqual(sum(u is not None for u in player['bench']),1)
        with self.assertRaises(Rejected):
            self.request('/action',{'action':'buy','slot':slot})
        player['gold']=0
        with self.assertRaises(Rejected):
            self.request('/action',{'action':'reroll'})

    def test_board_cap_and_ready_lock(self):
        room=self.match()
        p=room['players'][0]
        p['level']=1
        p['bench'][0]={'id':'koromon','star':1}
        with self.assertRaises(Rejected):
            self.request('/action',{'action':'move','area':'bench','slot':0,'targetArea':'board','targetSlot':0})
        self.request('/action',{'action':'move','area':'bench','slot':0,'targetArea':'board','targetSlot':3})
        self.request('/action',{'action':'ready'})
        with self.assertRaises(Rejected):
            self.request('/action',{'action':'sell','area':'board','slot':3})
        self.request('/action',{'action':'ready'})
        self.request('/action',{'action':'sell','area':'board','slot':3})
        self.assertIsNone(p['board'][3])

    def test_pool_conservation_after_roll_buy_merge_sell(self):
        room=self.match()
        p=room['players'][0]
        for _ in range(15):
            p['gold']=100
            self.request('/action',{'action':'reroll'})
            slot=next((i for i,u in enumerate(p['shop']) if u),None)
            if slot is not None and None in p['bench']:
                self.request('/action',{'action':'buy','slot':slot})
        for i,u in enumerate(list(p['bench'])):
            if u:
                self.request('/action',{'action':'sell','area':'bench','slot':i})
        for uid,definition in DEFS.items():
            held=sum(3**(u['star']-1) for p in room['players'] for u in p['board']+p['bench'] if u and u['id']==uid)
            shops=sum(p['shop'].count(uid) for p in room['players'])
            self.assertEqual(room['pool'][uid]+held+shops,POOL[definition['cost']])

    def test_both_ready_server_combat_and_next_round(self):
        room=self.match()
        self.request('/action',{'action':'ready'})
        self.request('/action',{'action':'ready'},1)
        self.assertEqual(room['phase'],'battle')
        a,b=self.request('/state'),self.request('/state',who=1)
        self.assertEqual(a['room']['frames'],b['room']['frames'])
        self.assertGreater(len(room['frames']),1)
        with self.assertRaises(Rejected):
            self.request('/action',{'action':'reroll'})
        self.now=room['deadline']+.1
        self.request('/state')
        self.assertEqual(room['phase'],'prepare')
        self.assertEqual(room['round'],2)
        self.assertTrue(any(p['hp']<100 for p in room['players']))

    def test_prepare_deadline_automatically_starts_combat(self):
        room=self.match()
        self.now+=41
        self.request('/state')
        self.assertEqual(room['phase'],'battle')

    def test_disconnect_timeout_and_queue_expiry(self):
        room=self.match('ranked')
        self.now+=35
        self.request('/state')
        self.now+=27
        self.request('/state')
        self.assertEqual(room['phase'],'finished')
        self.assertEqual(room['winner'],room['players'][0]['id'])
        self.request('/queue',{'mode':'normal'},2)
        self.now+=21
        self.game.tick()
        self.assertEqual(self.game.queues['normal'],[])

    def test_ranked_forfeit_is_applied_once_and_persisted(self):
        room=self.match('ranked')
        self.request('/leave')
        self.assertEqual(self.request('/state')['rating'],984)
        self.assertEqual(self.request('/state',who=1)['rating'],1016)
        self.request('/leave')
        self.game.finish(room,room['players'][0]['id'])
        restarted=Game(Path(self.temp.name)/'test.sqlite3')
        try:
            result=restarted.request('/login',{'name':'returning','key':self.keys[0]})
            self.assertEqual(result['rating'],984)
        finally:
            restarted.db.close()

    def test_normal_forfeit_does_not_change_rating(self):
        self.match()
        self.request('/leave')
        self.assertEqual(self.request('/state')['rating'],1000)
        self.assertEqual(self.request('/state',who=1)['rating'],1000)

    def test_ten_round_limit_and_rematch(self):
        room=self.match('ranked')
        room['round']=10
        self.game.fight(room)
        self.game.settle(room)
        self.assertEqual(room['phase'],'finished')
        self.request('/leave')
        self.request('/leave',who=1)
        new_room=self.match()
        self.assertNotEqual(room['id'],new_room['id'])

    def test_http_two_clients_and_validation(self):
        http=ThreadingHTTPServer(('127.0.0.1',0),Handler)
        http.game=self.game
        worker=threading.Thread(target=http.serve_forever,daemon=True)
        worker.start()
        base=f'http://127.0.0.1:{http.server_port}'
        def post(path,data,token=''):
            request=urllib.request.Request(base+path,data=json.dumps(data).encode(),headers={'Content-Type':'application/json','Authorization':'Bearer '+token})
            with urllib.request.urlopen(request,timeout=3) as response:
                return json.load(response)
        try:
            with urllib.request.urlopen(base+'/health') as response:
                self.assertEqual(json.load(response)['status'],'ok')
            post('/queue',{'mode':'normal'},self.tokens[0])
            match=post('/queue',{'mode':'normal'},self.tokens[1])
            self.assertEqual(post('/state',{},self.tokens[0])['room']['id'],match['room']['id'])
            with self.assertRaises(urllib.error.HTTPError) as caught:
                post('/action',{'action':'buy','slot':-1},self.tokens[0])
            self.assertEqual(caught.exception.code,400)
            caught.exception.close()
            self.assertEqual(post('/state',{},self.tokens[1])['room']['phase'],'prepare')
        finally:
            http.shutdown(); http.server_close(); worker.join(timeout=2)

if __name__=='__main__':
    unittest.main(verbosity=2)
