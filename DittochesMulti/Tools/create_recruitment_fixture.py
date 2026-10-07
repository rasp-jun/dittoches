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
    return dict(id='recruit-fixture',phase='prepare',mode='normal',round=1,side=0,remaining=60,players=[me,enemy],frames=[],lastCombat=[])

parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--preview',type=Path,default=root/'Builds/CombatReadabilityPreview')
args=parser.parse_args();rows=[]
for case in ['plain','board_pair','split_pair','bench_pair','cascade','no_pair','one_copy','no_gold']:
    test=RecruitmentTests();test.setUp()
    try:
        expected=0;area='';slot=-1;returned=0
        if case=='plain':test.p['bench'][0]=None;expected=1;area='bench';slot=0
        if case in ('board_pair','split_pair','bench_pair'):
            a,b=('board','board') if case=='board_pair' else ('board','bench') if case=='split_pair' else ('bench','bench')
            test.p[a][0]=test.unit(items=(8,12));test.p[b][1]=test.unit(items=(0,1))
            expected=2;area=a;slot=0;returned=2
        if case=='cascade':
            test.p['board'][0]=test.unit(2,(8,12));test.p['board'][1]=test.unit(2,(13,6))
            test.p['board'][2]=test.unit(1,(0,1));test.p['bench'][0]=test.unit(1,(2,3))
            test.p['shop'][1]='koromon';expected=3;area='board';slot=0;returned=6
        if case=='one_copy':test.p['board'][0]=test.unit()
        if case=='no_gold':test.p['gold']=0;test.p['bench'][0]=None
        before=snapshot(test)
        try:test.buy();assert expected>0
        except Rejected:assert expected==0
        after=snapshot(test)
        if not expected:assert before==after
        rows.append(dict(name=case,before=before,after=after,star=expected,area=area,slot=slot,returnedItems=returned))
    finally:test.tearDown()
folder=args.preview/'RecruitmentCaptures';folder.mkdir(parents=True,exist_ok=True)
(folder/'fixture.json').write_text(json.dumps(dict(rows=rows)),encoding='utf-8')
print(f'RECRUITMENT FIXTURE: {len(rows)} production purchase results; full bench, cascades, equipment overflow and rejection.')
