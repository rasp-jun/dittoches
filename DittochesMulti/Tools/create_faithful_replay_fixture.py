"""Produce offline replay evidence with the production server combat simulator."""
import sys,json
from pathlib import Path
ROOT=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(ROOT/'Server'))
from server import DEFS,SKILLS
from combat_skills import simulate
ids=['agumon','metalgreymon','wargreymon','rosemon','seraphimon','birdramon','metalgarurumon']
fighters=[]
for side in (0,1):
    for slot,ident in enumerate(ids):
        hp=SKILLS[ident]['baseHealth']
        fighters.append(dict(key=len(fighters),side=side,slot=slot,id=ident,star=1,x=float(slot),y=float(5 if side==0 else 2),hp=hp,maxHp=hp,cooldown=0,items=[]))
frames,events,duration=simulate(fighters,DEFS)
fixture=dict(phase='battle',side=0,battleDuration=duration,frames=frames,skillEvents=events)
folder=ROOT/'Builds/FaithfulPreview/FaithfulReplayValidation';folder.mkdir(parents=True,exist_ok=True)
(folder/'fixture.json').write_text(json.dumps(fixture),encoding='utf-8')
print('SERVER FIXTURE',len(frames),'frames',len(events),'skill events',duration,'seconds')
