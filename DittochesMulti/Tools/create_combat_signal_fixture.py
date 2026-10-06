"""Production simulation of equipped teams for both-side signal rendering QA."""
import argparse
import json
import sys
from pathlib import Path

root=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(root/'Server'))
from server import DEFS,SKILLS
from combat_skills import simulate

parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--preview',type=Path,default=root/'Builds/CombatReadabilityPreview')
args=parser.parse_args()
ids=['tsunomon','gabumon','garurumon','weregarurumon','metalgarurumon','tokomon','patamon']
fighters=[]
for side in (0,1):
    for slot,uid in enumerate(ids):
        skill=SKILLS[uid];row=4 if skill['attackRange']==1 else 6
        fighters.append(dict(key=len(fighters),side=side,slot=slot,id=uid,star=1,
            x=float(slot if side==0 else 6-slot),y=float(row if side==0 else 7-row),
            hp=skill['baseHealth'],maxHp=skill['baseHealth'],cooldown=0,items=[8,8]))
frames,events,duration=simulate(fighters,DEFS)
assert any(f['lowShieldUsed'] for frame in frames for f in frame['units'])
assert any(frame['time']>=15 and f['hp']>0 and f['friendshipActive'] for frame in frames for f in frame['units'])
folder=args.preview/'CombatSignalCaptures';folder.mkdir(parents=True,exist_ok=True)
(folder/'fixture.json').write_text(json.dumps(dict(id='signal-test',phase='battle',side=0,battleDuration=duration,frames=frames,skillEvents=events)),encoding='utf-8')
print(f'SIGNAL FIXTURE: {len(frames)} frames, {len(events)} skills, {duration:.2f}s, real catalog stats/equipment.')
