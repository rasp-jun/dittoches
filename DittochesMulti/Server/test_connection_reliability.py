import copy
import secrets
import tempfile
import unittest
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
from server import Game,Rejected


class ConnectionReliabilityTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.now=1000.
        self.game=Game(Path(self.temp.name)/'network.sqlite3',clock=lambda:self.now)
        self.keys=[secrets.token_hex(32) for _ in range(2)]
        self.tokens=[self.game.request('/login',dict(key=k,name='network'))['token'] for k in self.keys]
        for t in self.tokens:s=self.game.request('/queue',dict(mode='normal'),t)
        self.room=self.game.rooms[s['room']['id']];self.player=self.room['players'][0]

    def tearDown(self):
        self.game.db.close();self.temp.cleanup()

    def command(self,action,**extra):
        return dict(requestId=secrets.token_hex(16),expectedRoom=self.room['id'],expectedRound=self.room['round'],expectedPhase=self.room['phase'],action=action,**extra)

    def send(self,data,path='/action',who=0):
        self.now+=.1
        return self.game.request(path,data,self.tokens[who])

    def test_lost_buy_response_retry_is_acknowledged_without_second_purchase(self):
        data=self.command('buy',slot=0);self.send(data);saved=copy.deepcopy(self.player)
        response=self.send(data)
        self.assertEqual(self.player,saved);self.assertTrue(response['duplicateRequest'])
        self.assertEqual(response['acknowledgedRequestId'],data['requestId'])

    def test_ready_toggle_and_reroll_are_applied_once(self):
        data=self.command('ready');self.send(data);self.send(data)
        self.assertTrue(self.player['ready'])
        self.send(self.command('ready'));data=self.command('reroll');self.send(data)
        before=copy.deepcopy(self.player);self.send(data);self.assertEqual(self.player,before)

    def test_duplicate_is_checked_before_rate_limit(self):
        data=self.command('reroll');self.send(data)
        self.assertTrue(self.game.request('/action',data,self.tokens[0])['duplicateRequest'])

    def test_same_id_cannot_change_payload_or_endpoint(self):
        data=self.command('shop_lock',shopLocked=True);self.send(data)
        with self.assertRaises(Rejected):self.send(dict(data,shopLocked=False))
        with self.assertRaises(Rejected):self.send(data,'/leave')
        self.assertTrue(self.player['shopLocked'])

    def test_old_round_and_phase_are_rejected_before_spending(self):
        data=self.command('reroll');self.room['round']+=1;before=copy.deepcopy(self.player)
        with self.assertRaisesRegex(Rejected,'라운드'):self.send(data)
        self.assertEqual(self.player,before)
        data=self.command('ready');self.room['phase']='battle'
        with self.assertRaisesRegex(Rejected,'라운드'):self.send(data)
        self.assertFalse(self.player['ready'])

    def test_old_room_cannot_forfeit_current_match(self):
        data=self.command('');data['expectedRoom']='previous-room'
        with self.assertRaisesRegex(Rejected,'경기'):self.send(data,'/leave')
        self.assertEqual(self.room['phase'],'prepare')

    def test_retry_returns_current_snapshot_without_replaying_old_ready(self):
        data=self.command('ready');self.send(data)
        self.send(self.command('ready'))
        self.assertFalse(self.player['ready'])
        response=self.send(data)
        self.assertFalse(response['room']['players'][0]['ready'])

    def test_rejection_is_stable_even_if_resources_change(self):
        self.player['gold']=0;data=self.command('reroll')
        with self.assertRaises(Rejected):self.send(data)
        self.player['gold']=20
        with self.assertRaises(Rejected):self.send(data)
        self.assertEqual(self.player['gold'],20)

    def test_concurrent_duplicate_requests_spend_only_once(self):
        data=self.command('reroll');gold=self.player['gold'];self.now+=1
        with ThreadPoolExecutor(max_workers=4) as pool:
            responses=list(pool.map(lambda _:self.game.request('/action',data,self.tokens[0]),range(8)))
        self.assertEqual(self.player['gold'],gold-2)
        self.assertEqual(sum(not r['duplicateRequest'] for r in responses),1)

    def test_presence_grace_and_relogin_preserve_match(self):
        self.now+=10;self.room['deadline']=self.now+100
        snapshot=self.send({},'/state')
        enemy=snapshot['room']['players'][1]
        self.assertTrue(enemy['connectionKnown']);self.assertFalse(enemy['connected'])
        self.assertAlmostEqual(enemy['reconnectRemaining'],49.9)
        response=self.game.request('/login',dict(key=self.keys[1],name='network'))
        self.assertEqual(response['room']['id'],self.room['id'])
        self.assertTrue(self.send({},'/state')['room']['players'][1]['connected'])

    def test_grace_expiry_still_finishes_match(self):
        self.now+=61;self.game.sessions[self.tokens[0]]['seen']=self.now
        response=self.send({},'/state')
        self.assertEqual(response['room']['phase'],'finished')
        self.assertEqual(response['room']['result'],'승리')

    def test_bounded_receipts_and_legacy_client(self):
        for i in range(260):self.send(self.command('shop_lock',shopLocked=bool(i%2)))
        self.assertEqual(len(self.game.sessions[self.tokens[0]]['receipts']),256)
        self.send(dict(action='shop_lock',shopLocked=False))
        self.assertFalse(self.player['shopLocked'])
        self.assertEqual(self.send({},'/state')['reliableCommands'],1)

    def test_invalid_request_id_does_not_mutate(self):
        for invalid in (17,'short','G'*32):
            with self.assertRaises(Rejected):self.send(dict(self.command('ready'),requestId=invalid))
        self.assertFalse(self.player['ready'])

if __name__=='__main__':unittest.main()
