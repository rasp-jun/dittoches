import copy
import unittest
import test_equipment as fixture
import combat_builds as builds
from server import Rejected, formation_limit


class EmblemTests(unittest.TestCase):
    setUp=fixture.OnlineEquipmentTests.setUp
    tearDown=fixture.OnlineEquipmentTests.tearDown
    request=fixture.OnlineEquipmentTests.request
    equip=fixture.OnlineEquipmentTests.equip

    def combine(self,a,b):
        self.p['inventory']=[a,b]
        return self.request('/action',dict(action='combine_items',itemSlot=0,targetItemSlot=1,inventoryRevision=self.p['inventoryRevision']))

    def test_all_material_pairs_and_three_capacity_artifacts(self):
        self.assertEqual(len(builds.ITEMS),28)
        for a in (0,1,2,3,15,16):
            for b in (0,1,2,3,15,16):
                result=builds.combine(a,b)
                self.assertEqual(result,builds.combine(b,a))
                self.assertEqual(sorted(builds.ITEMS[result]['recipe']),sorted([a,b]))
                self.combine(a,b)
                self.assertEqual(self.p['inventory'],[result])
        self.assertEqual([builds.combine(15,15),builds.combine(15,16),builds.combine(16,16)],[25,26,27])
        self.assertIsNone(builds.combine(15,25))

    def test_every_emblem_adds_membership_without_duplicate_species(self):
        roster=set(uid for t in builds.TRAITS for uid in t['members'])
        for item in builds.ITEMS[17:25]:
            trait=next(t for t in builds.TRAITS if t['id']==item['grantsTrait'])
            owner=next(uid for uid in sorted(roster) if uid not in trait['members'])
            team=[dict(id=trait['members'][0],items=[]),dict(id=owner,items=[item['id']])]
            self.assertEqual(builds.level(trait,team),1)
            self.assertEqual(builds.level(trait,team+[team[-1]]),1)
            bonus=builds.resolve(owner,team,[item['id']])
            for key,value in trait['tiers'][0]['bonus'].items():self.assertGreaterEqual(bonus[key],value)
            self.assertGreaterEqual(bonus['health'],100)

    def test_natural_trait_rejection_is_atomic_even_auto_combining(self):
        self.p['board'][3]=dict(id='agumon',star=1,items=[0])
        self.p['inventory']=[15];before=copy.deepcopy(self.p)
        with self.assertRaises(Rejected):self.equip()
        self.assertEqual(self.p,before)
        self.p['board'][3]=dict(id='gabumon',star=1,items=[17]);self.p['inventory']=[17];before=copy.deepcopy(self.p)
        with self.assertRaises(Rejected):self.equip()
        self.assertEqual(self.p,before)

    def test_auto_combine_on_full_slots_and_extractor(self):
        self.p['board'][3]=dict(id='gabumon',star=1,items=[15,8]);self.p['inventory']=[0]
        self.equip();self.assertEqual(self.p['board'][3]['items'],[17,8])
        self.p['inventory']=[14];self.equip();self.assertEqual(self.p['inventory'],[17,8])

    def test_capacity_is_owned_once_in_inventory_board_and_bench(self):
        self.p['inventory']=[25,26,27];self.assertEqual(formation_limit(self.p),self.p['level']+3)
        self.equip();self.assertEqual(formation_limit(self.p),self.p['level']+3)
        self.request('/action',dict(action='move',area='board',slot=3,targetArea='bench',targetSlot=0))
        self.assertEqual(formation_limit(self.p),self.p['level']+3)
        self.request('/action',dict(action='sell',area='bench',slot=0))
        self.assertEqual(formation_limit(self.p),self.p['level']+3)
        self.assertEqual(sorted(self.p['inventory']),[25,26,27])

    def test_extra_placement_is_authoritatively_enforced(self):
        self.combine(15,16)
        self.p['board']=[None]*28
        for slot,uid in enumerate(['agumon','gabumon','patamon']):self.p['board'][slot]=dict(id=uid,star=1,items=[])
        self.p['bench'][0]=dict(id='palmon',star=1,items=[])
        self.request('/action',dict(action='move',area='bench',slot=0,targetArea='board',targetSlot=3))
        self.p['bench'][0]=dict(id='piyomon',star=1,items=[])
        before=copy.deepcopy(self.p)
        with self.assertRaises(Rejected):self.request('/action',dict(action='move',area='bench',slot=0,targetArea='board',targetSlot=4))
        self.assertEqual(self.p,before)

    def test_merge_refunds_duplicate_emblem_instead_of_double_equipping(self):
        self.p['board'][3]=dict(id='gabumon',star=1,items=[17])
        self.p['bench'][0]=dict(id='gabumon',star=1,items=[17])
        self.p['bench'][1]=dict(id='gabumon',star=1,items=[8])
        self.game.merge_slots(self.p,[(self.p['board'],3),(self.p['bench'],0),(self.p['bench'],1)],'gabumon',1)
        self.assertEqual(self.p['board'][3]['items'],[17,8]);self.assertEqual(self.p['inventory'][-1],17)

    def test_live_emblem_updates_whole_team_and_removal_preserves_prefix(self):
        self.p['board'][3]=dict(id='agumon',star=1,items=[])
        self.p['board'][4]=dict(id='gabumon',star=1,items=[])
        self.p['inventory']=[17,14];self.game.fight(self.room)
        old=copy.deepcopy(self.room['frames']);self.now+=.8;self.equip(unit_slot=4)
        when=self.room['equipmentEvents'][-1]['time']
        self.assertEqual([f for f in old if f['time']<when],[f for f in self.room['frames'] if f['time']<when])
        before={f['slot']:f for f in old[0]['units'] if f['side']==0}
        after=next(f for f in self.room['frames'] if f['time']>=when)['units']
        for f in after:
            if f['side']==0:self.assertGreater(f['attackDamage'],before[f['slot']]['attackDamage'])
            if f['side']==0 and f['slot']==4:self.assertGreater(f['maxHp'],before[f['slot']]['maxHp'])
        prefix=copy.deepcopy([f for f in self.room['frames'] if f['time']<when]);self.equip(unit_slot=4)
        self.assertEqual([f for f in self.room['frames'] if f['time']<when],prefix)
        self.assertEqual(self.p['board'][4]['items'],[])

    def test_new_game_and_round_supplies_are_equal(self):
        self.assertEqual(self.p['inventory'][-2:],[15,16]);self.assertEqual(self.p['inventory'],self.enemy['inventory'])
        self.p['hp']=self.enemy['hp']=1000
        for _ in range(4):self.game.fight(self.room);self.game.settle(self.room)
        self.assertEqual(self.room['round'],5)
        self.assertEqual(self.p['inventory'].count(15),2);self.assertEqual(self.p['inventory'].count(16),2)
        self.assertEqual(self.p['inventory'],self.enemy['inventory'])


if __name__=='__main__':unittest.main()
