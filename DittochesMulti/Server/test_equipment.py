import copy
import secrets
import tempfile
import unittest
from pathlib import Path
from server import Game, Rejected, MAX_EQUIPMENT
from combat_builds import combine


class OnlineEquipmentTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.now=1000.
        self.game=Game(Path(self.temp.name)/'equipment.sqlite3',clock=lambda:self.now)
        self.keys=[secrets.token_hex(32),secrets.token_hex(32)]
        self.tokens=[self.game.request('/login',{'name':str(i),'key':key})['token'] for i,key in enumerate(self.keys)]
        self.request('/queue',{'mode':'normal'})
        match=self.request('/queue',{'mode':'normal'},1)
        self.room=self.game.rooms[match['room']['id']];self.p,self.enemy=self.room['players']

    def tearDown(self):
        self.game.db.close();self.temp.cleanup()

    def request(self,path,data=None,who=0):
        self.now+=.1
        return self.game.request(path,data or {},self.tokens[who])

    def equip(self,slot=0,area='board',unit_slot=3,**extra):
        data=dict(action='equip',itemSlot=slot,area=area,slot=unit_slot,inventoryRevision=self.p['inventoryRevision'])
        data.update(extra)
        return self.request('/action',data)

    def test_equal_initial_supplies_private_inventory_public_equipment(self):
        self.assertEqual(self.p['inventory'],[0,1,2,3,14,15,16])
        self.assertEqual(self.p['inventory'],self.enemy['inventory'])
        self.equip()
        snapshot=self.request('/state',who=1)
        visible=snapshot['room']['players'][0]
        self.assertEqual(visible['inventory'],[])
        self.assertEqual(visible['inventoryRevision'],0)
        self.assertEqual(visible['bench'],[])
        self.assertEqual(visible['board'][0]['items'],[0])
        visible['board'][0]['items'].clear()
        self.assertEqual(self.p['board'][3]['items'],[0])

    def test_scout_bench_is_public_detached_and_actions_remain_owned(self):
        self.enemy['bench'][8]=dict(id='agumon',star=2,items=[8,12])
        self.enemy['gold']=37
        view=self.request('/state')['room']['players'][1]
        self.assertTrue(view['publicBench'])
        self.assertEqual(view['bench'],[dict(id='agumon',star=2,slot=8,items=[8,12])])
        self.assertEqual(view['shop'],[])
        self.assertEqual(view['inventory'],[])
        self.assertEqual(view['gold'],0)
        view['bench'][0]['items'].clear()
        self.assertEqual(self.enemy['bench'][8]['items'],[8,12])
        before=copy.deepcopy(self.enemy)
        with self.assertRaises(Rejected):
            self.request('/action',dict(action='sell',area='bench',slot=8))
        self.assertEqual(before,self.enemy)
        self.request('/action',dict(action='move',area='bench',slot=8,targetArea='bench',targetSlot=7),who=1)
        view=self.request('/state')['room']['players'][1]
        self.assertEqual(view['bench'][0]['slot'],7)
        reconnect=self.game.request('/login',dict(name='same',key=self.keys[0]))
        self.assertEqual(reconnect['room']['players'][1]['bench'],view['bench'])

    def test_all_component_pairs_combine_in_both_orders(self):
        for a in range(4):
            for b in range(4):
                self.p['inventory']=[a,b]
                self.request('/action',dict(action='combine_items',itemSlot=0,targetItemSlot=1,inventoryRevision=self.p['inventoryRevision']))
                self.assertEqual(self.p['inventory'],[combine(a,b)])

    def test_invalid_combinations_are_atomic(self):
        for inventory,slots in (([0,1],(0,0)),([0,4],(0,1)),([14,0],(0,1)),([0,1],(-1,1)),([0,1],(True,1)),([0,1],(0,'1'))):
            self.p['inventory']=inventory.copy();before=copy.deepcopy(self.p)
            with self.assertRaises(Rejected):
                self.request('/action',dict(action='combine_items',itemSlot=slots[0],targetItemSlot=slots[1],inventoryRevision=self.p['inventoryRevision']))
            self.assertEqual(self.p,before)

    def test_replayed_or_stale_revision_cannot_equip_another_item(self):
        command=dict(action='equip',itemSlot=0,area='board',slot=3,inventoryRevision=0)
        self.request('/action',command);before=copy.deepcopy(self.p)
        for revision in (0,None,True,'1',-1):
            command['inventoryRevision']=revision
            with self.assertRaises(Rejected):self.request('/action',command)
            self.assertEqual(self.p,before)

    def test_third_slot_is_public_and_fourth_rejected_atomically_on_board_and_bench(self):
        self.assertEqual(MAX_EQUIPMENT,3)
        for area,slot in (('board',3),('bench',8)):
            with self.subTest(area=area):
                self.p[area][slot]=dict(id='agumon',star=1,items=[4,12])
                self.p['inventory']=[8,13]
                revision=self.p['inventoryRevision']
                self.equip(area=area,unit_slot=slot)
                self.assertEqual(self.p[area][slot]['items'],[4,12,8])
                self.assertEqual(self.p['inventory'],[13])
                self.assertEqual(self.p['inventoryRevision'],revision+1)
                visible=next(u for u in self.request('/state',who=1)['room']['players'][0][area] if u['slot']==slot)
                self.assertEqual(visible['items'],[4,12,8])
                visible['items'].pop()
                self.assertEqual(self.p[area][slot]['items'],[4,12,8])
                before=copy.deepcopy(self.p)
                with self.assertRaises(Rejected):self.equip(area=area,unit_slot=slot)
                self.assertEqual(self.p,before)
                reconnect=self.game.request('/login',dict(name='again',key=self.keys[0]))
                resumed=next(u for u in reconnect['room']['players'][0][area] if u['slot']==slot)
                self.assertEqual(resumed['items'],[4,12,8])

    def test_auto_combine_when_all_three_slots_are_full(self):
        unit=self.p['board'][3];unit['items']=[0,5,8];self.p['inventory']=[2,3]
        self.equip()
        self.assertEqual(unit['items'],[6,5,8]);self.assertEqual(self.p['inventory'],[3])
        before=copy.deepcopy(self.p)
        with self.assertRaises(Rejected):self.equip()
        self.assertEqual(self.p,before)

    def test_remover_consumed_only_when_equipment_exists(self):
        self.p['inventory']=[14]
        with self.assertRaises(Rejected):self.equip()
        self.assertEqual(self.p['inventory'],[14])
        self.p['board'][3]['items']=[12,13,8]
        self.equip()
        self.assertEqual(self.p['inventory'],[12,13,8]);self.assertEqual(self.p['board'][3]['items'],[])

    def test_invalid_targets_and_client_forged_ids_are_not_trusted(self):
        for area,slot in (('opponent',3),('board',-1),('board',28),('bench',9),('board',True),('board',0)):
            before=copy.deepcopy(self.p)
            with self.assertRaises(Rejected):self.equip(area=area,unit_slot=slot)
            self.assertEqual(self.p,before)
        self.equip(itemId=13,items=[13,13],player=self.enemy['id'])
        self.assertEqual(self.p['board'][3]['items'],[0])
        self.assertEqual(self.enemy['board'][3]['items'],[])

    def test_ready_locks_equipment_but_battle_allows_it(self):
        self.request('/action',{'action':'ready'});before=copy.deepcopy(self.p)
        for action in ('equip','combine_items'):
            with self.assertRaises(Rejected):
                self.request('/action',dict(action=action,itemSlot=0,targetItemSlot=1,area='board',slot=3,inventoryRevision=0))
        self.assertEqual(self.p,before)
        self.request('/action',{'action':'ready'},1)
        self.equip()
        self.assertEqual(self.p['board'][3]['items'],[0])

    def test_sale_returns_equipment_and_reconnect_keeps_it(self):
        self.p['board'][3]['items']=[8,12,4]
        self.request('/action',dict(action='sell',area='board',slot=3))
        self.assertEqual(self.p['inventory'][-3:],[8,12,4])
        reconnect=self.game.request('/login',dict(name='again',key=self.keys[0]))
        self.assertEqual(reconnect['token'],self.tokens[0])
        self.assertEqual(reconnect['room']['players'][0]['inventory'],self.p['inventory'])

    def test_star_merge_retains_three_and_returns_overflow_without_silent_crafting(self):
        self.p['board'][3]['items']=[0,8]
        self.p['bench'][0]=dict(id='koromon',star=1,items=[1,12])
        self.p['bench'][1]=dict(id='koromon',star=1,items=[13,6])
        before=self.p['inventory'].copy();self.game.merge(self.room,self.p)
        self.assertEqual(self.p['board'][3],dict(id='koromon',star=2,items=[0,8,1]))
        self.assertEqual(self.p['inventory'],before+[12,13,6])
        self.assertIsNone(self.p['bench'][0]);self.assertIsNone(self.p['bench'][1])

    def test_equipment_reaches_authoritative_combat_and_is_not_consumed(self):
        self.p['board'][3]['items']=[8,12,4];inventory=self.p['inventory'].copy()
        self.game.fight(self.room)
        start=self.room['frames'][0]['units'];own=next(f for f in start if f['side']==0);enemy=next(f for f in start if f['side']==1)
        self.assertAlmostEqual(own['maxHp'],enemy['maxHp']+300)
        self.assertAlmostEqual(own['mana'],enemy['mana']+20)
        self.assertGreater(own['attackDamage'],enemy['attackDamage'])
        self.assertEqual(self.p['board'][3]['items'],[8,12,4]);self.assertEqual(self.p['inventory'],inventory)
        self.assertNotIn('items',own)

    def test_next_round_supplies_are_equal_and_settled_only_once(self):
        self.game.fight(self.room);self.game.settle(self.room)
        self.assertEqual(self.room['round'],2)
        self.assertEqual(self.p['inventory'],[0,1,2,3,14,15,16,0])
        before=copy.deepcopy(self.room)
        self.game.settle(self.room)
        self.assertEqual(self.room,before)
        self.game.fight(self.room);self.game.settle(self.room)
        self.assertEqual(self.room['round'],3)
        self.assertEqual(self.p['inventory'][-3:],[1,15,14])
        self.assertEqual(self.p['inventory'],self.enemy['inventory'])


if __name__=='__main__':unittest.main()
