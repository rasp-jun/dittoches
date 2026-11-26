"""Capture real server purchase/merge results for client receipt verification."""
import argparse,copy,json,sys
from pathlib import Path
root=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(root/'Server'))
from test_recruitment import RecruitmentTests
from server import Game,Rejected

def snapshot(test):
    me=copy.deepcopy(test.p)
    me['board']=Game.units(me['board']);me['bench']=Game.units(me['bench'])
    enemy=copy.deepcopy(me);enemy.update(name='Fixture opponent',board=[],bench=[],shop=[],inventory=[],ready=False)
    if 'board' in test.room['players'][1]:
        enemy['board']=Game.units(copy.deepcopy(test.room['players'][1]['board']))
    duration=test.room.get('battleDuration',60)
    return dict(id='recruit-fixture',phase=test.room['phase'],mode='normal',round=1,side=0,remaining=duration,players=[me,enemy],
                frames=copy.deepcopy(test.room.get('frames',[])),skillEvents=copy.deepcopy(test.room.get('skillEvents',[])),battleDuration=duration,lastCombat=[])

parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--preview',type=Path,default=root/'Builds/CombatReadabilityPreview')
args=parser.parse_args();rows=[]
for case in ['plain','board_pair','split_pair','bench_pair','free_bench_pair','duplicate_emblem','cascade','battle_cascade','no_pair','one_copy','no_gold','ready_locked']:
    test=RecruitmentTests();test.setUp()
    try:
        expected=0;area='';slot=-1;returned=0
        if case=='plain':test.p['bench'][0]=None;expected=1;area='bench';slot=0
        if case=='free_bench_pair':
            test.p['bench'][0]=None;test.p['bench'][6]=test.unit(items=(8,12));test.p['bench'][8]=test.unit(items=(0,1))
            expected=2;area='bench';slot=0;returned=1
        if case=='duplicate_emblem':
            test.p['board'][3]=test.unit(items=(17,8));test.p['bench'][2]=test.unit(items=(17,12))
            expected=2;area='board';slot=3;returned=1
        if case in ('board_pair','split_pair','bench_pair'):
            a,b=('board','board') if case=='board_pair' else ('board','bench') if case=='split_pair' else ('bench','bench')
            test.p[a][0]=test.unit(items=(8,12));test.p[b][1]=test.unit(items=(0,1))
            expected=2;area=a;slot=0;returned=1
        if case in ('cascade','battle_cascade'):
            test.p['board'][0]=test.unit(2,(8,12));test.p['board'][1]=test.unit(2,(13,6))
            test.p['board'][2]=test.unit(1,(0,1));test.p['bench'][0]=test.unit(1,(2,3))
            test.p['shop'][1]='koromon';expected=3;area='board';slot=0;returned=5
            if case=='battle_cascade':
                enemy=copy.deepcopy(test.p);enemy['board']=[None]*28
                enemy['board'][3]=dict(id='machinedramon',star=2,items=[])
                test.room['players'][1]=enemy
                test.game.fight(test.room)
        if case=='one_copy':test.p['board'][0]=test.unit()
        if case=='no_gold':test.p['gold']=0;test.p['bench'][0]=None
        if case=='ready_locked':test.p['ready']=True;test.p['bench'][0]=None
        before=snapshot(test)
        try:test.buy();assert expected>0
        except Rejected:assert expected==0
        after=snapshot(test)
        assert len(after['players'][0]['inventory'])-len(before['players'][0]['inventory'])==returned
        if case=='battle_cascade':
            assert test.p['board'][0]['star']==3 and test.room['initialFighters'][0]['star']==2
        if not expected:assert before==after
        rows.append(dict(name=case,before=before,after=after,star=expected,area=area,slot=slot,returnedItems=returned))
    finally:test.tearDown()
folder=args.preview/'RecruitmentCaptures';folder.mkdir(parents=True,exist_ok=True)
(folder/'fixture.json').write_text(json.dumps(dict(rows=rows)),encoding='utf-8')
print(f'RECRUITMENT FIXTURE: {len(rows)} production purchase results; full bench, cascades, equipment overflow and rejection.')
