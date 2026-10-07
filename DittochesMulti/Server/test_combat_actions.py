import copy
import unittest
import test_equipment as fixture
from server import Rejected, DEFS
from combat_skills import simulate
import combat_builds as builds


class CombatActionsTests(unittest.TestCase):
    setUp = fixture.OnlineEquipmentTests.setUp
    tearDown = fixture.OnlineEquipmentTests.tearDown
    request = fixture.OnlineEquipmentTests.request
    equip = fixture.OnlineEquipmentTests.equip

    def battle(self):
        self.p['gold']=100
        self.request('/action',dict(action='ready'))
        self.request('/action',dict(action='ready'),1)
        self.assertEqual(self.room['phase'],'battle')

    def test_combat_economy_bench_equipment_and_sale(self):
        self.battle()
        frames=copy.deepcopy(self.room['frames'])
        self.request('/action',dict(action='reroll'))
        slot=next(i for i,u in enumerate(self.p['shop']) if u)
        self.request('/action',dict(action='buy',slot=slot))
        seat=next(i for i,u in enumerate(self.p['bench']) if u)
        self.equip(area='bench',unit_slot=seat)
        items=list(self.p['bench'][seat]['items'])
        self.request('/action',dict(action='move',area='bench',slot=seat,targetArea='bench',targetSlot=8))
        self.assertEqual(self.p['bench'][8]['items'],items)
        gold=self.p['gold'];self.request('/action',dict(action='sell',area='bench',slot=8))
        self.assertGreater(self.p['gold'],gold)
        self.assertEqual(self.p['inventory'][-len(items):],items)
        self.request('/action',dict(action='xp'))
        self.request('/action',dict(action='combine_items',itemSlot=0,targetItemSlot=1,inventoryRevision=self.p['inventoryRevision']))
        self.assertEqual(self.room['frames'],frames)
        self.assertEqual(self.room['phase'],'battle')

    def test_board_movement_sale_and_ready_rejected_atomically(self):
        self.p['bench'][0]=dict(id='agumon',star=1,items=[])
        self.battle()
        for command in (dict(action='sell',area='board',slot=3),dict(action='ready'),
                        dict(action='move',area='board',slot=3,targetArea='bench',targetSlot=2),
                        dict(action='move',area='bench',slot=0,targetArea='board',targetSlot=2)):
            before=copy.deepcopy(self.p)
            with self.assertRaises(Rejected):self.request('/action',command)
            self.assertEqual(self.p,before)

    def test_live_equipment_changes_future_preserves_entire_past(self):
        self.p['inventory']=[8]
        self.battle();self.now+=.63
        before=copy.deepcopy(self.room['frames']);start=self.room['battleStartedAt']
        self.equip()
        event=self.room['equipmentEvents'][0]
        self.assertGreater(event['time'],self.now-start)
        self.assertEqual([f for f in before if f['time']<event['time']],
                         [f for f in self.room['frames'] if f['time']<event['time']])
        changed=next(f for f in self.room['frames'] if f['time']>=event['time'])['units'][0]
        original=next(f for f in before if f['time']>=event['time'])['units'][0]
        self.assertGreater(changed['maxHp'],original['maxHp'])
        self.assertEqual(changed['armor'],original['armor']+30)
        self.assertEqual(start,self.room['battleStartedAt'])
        self.assertAlmostEqual(self.room['deadline']-start,self.room['battleDuration'])
        self.assertEqual(self.request('/state')['room']['frames'],self.request('/state',who=1)['room']['frames'])

    def test_live_damage_item_changes_actual_outcome(self):
        self.p['inventory']=[4]
        self.battle()
        before=self.room['roundWinner'];self.equip()
        self.assertEqual(before,-1)
        self.assertEqual(self.room['roundWinner'],0)
        self.now=self.room['deadline']+.01;self.request('/state')
        self.assertEqual(self.room['reportWinner'],0)
        self.assertLess(self.enemy['hp'],self.p['hp'])

    def test_merge_during_combat_keeps_current_fighters_frozen(self):
        uid=self.p['board'][3]['id']
        self.p['bench'][0]=dict(id=uid,star=1,items=[])
        self.p['shop'][0]=uid
        self.battle();frames=copy.deepcopy(self.room['frames'])
        self.request('/action',dict(action='buy',slot=0))
        self.assertEqual(self.p['board'][3]['star'],2)
        self.assertEqual(self.room['frames'],frames)
        self.assertEqual(self.room['initialFighters'][0]['star'],1)

    def test_retried_live_equip_applies_exactly_once(self):
        self.battle()
        command=dict(action='equip',itemSlot=0,area='board',slot=3,inventoryRevision=0,
                     requestId='c'*32,expectedRoom=self.room['id'],expectedRound=1,expectedPhase='battle')
        self.request('/action',command)
        before=copy.deepcopy(self.room['equipmentEvents']);inventory=list(self.p['inventory'])
        self.request('/action',command)
        self.assertEqual(self.room['equipmentEvents'],before)
        self.assertEqual(self.p['inventory'],inventory)

    def test_combat_merge_transfers_equipment_without_duplicate_live_bonuses(self):
        uid=self.p['board'][3]['id']
        self.p['board'][4]=dict(id=uid,star=1,items=[8])
        self.p['shop'][0]=uid
        self.battle();self.request('/action',dict(action='buy',slot=0))
        self.assertEqual(self.p['board'][3]['items'],[8]);self.assertIsNone(self.p['board'][4])
        changes=self.room['equipmentEvents']
        self.assertEqual(len(changes),2)
        self.assertEqual(changes[0]['time'],changes[1]['time'])
        frame=next(f for f in self.room['frames'] if f['time']>=changes[0]['time'])
        own=[f for f in frame['units'] if f['side']==0]
        self.assertEqual(sum(f['armor'] for f in own),30)
        self.assertTrue(all(f['star']==1 for f in own))

    def test_stat_refresh_does_not_reset_damage_death_or_opening_effects(self):
        self.battle()
        f=copy.deepcopy(self.room['initialFighters'][0]);f.update(mana=0,maxMana=100)
        builds.initialize([f]);f.update(hp=f['maxHp']*.4,mana=7,combatAge=3,regenClock=.7,
                                       lowShieldUsed=True,crisisAt=2,attacks=5,damageDone=123,cooldown=.5,castUntil=0)
        baseline=f['hp'];clock=f['regenClock']
        for items in ([9],[12],[],[9],[]):
            builds.change_equipment(f,items,[f['id']])
            self.assertAlmostEqual(f['hp']/f['maxHp'],.4)
            self.assertEqual((f['mana'],f['shield'],f['damageDone'],f['attacks'],f['regenClock']),(7,0,123,5,clock))
            self.assertTrue(f['lowShieldUsed'])
        self.assertAlmostEqual(f['hp'],baseline)
        f['hp']=0;builds.change_equipment(f,[8],[f['id']]);self.assertEqual(f['hp'],0)

    def test_same_time_changes_are_ordered_and_replay_is_deterministic(self):
        self.battle();initial=self.room['initialFighters'];uid=initial[0]['id']
        events=[dict(time=.4,side=0,slot=3,id=uid,items=items) for items in ([1],[8],[])]
        a=simulate(copy.deepcopy(initial),DEFS,events)
        b=simulate(copy.deepcopy(initial),DEFS,events)
        self.assertEqual(a,b)
        self.assertEqual(next(f for f in a[0] if f['time']==.4)['units'][0]['armor'],0)


if __name__=='__main__':unittest.main()
