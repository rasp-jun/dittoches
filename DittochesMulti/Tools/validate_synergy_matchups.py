"""Structural combat smoke matrix, not an equal-budget win-rate balance study."""
import argparse
import json
import math
import sys
from pathlib import Path

ROOT=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(ROOT/'Server'))
import combat_builds as b
import combat_stats as stats
from combat_skills import simulate


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output',type=Path,default=ROOT/'Builds/SynergyRework-20261006/matchups.json')
    args=parser.parse_args()
    defs={u['id']:u for u in json.loads((ROOT/'Server/roster.json').read_text(encoding='utf-8'))}
    results=[]
    for left in b.TRAITS:
        for right in b.TRAITS:
            if left==right:continue
            fighters=[]
            for side,trait in enumerate((left,right)):
                for i,uid in enumerate(trait['members']):
                    skill=stats.SKILLS[uid];y=4 if skill['attackRange']==1 else 6
                    fighters.append(dict(key=len(fighters),id=uid,side=side,star=1,items=[8 if i==0 else 11] if i<2 else [],
                        x=float(i if side==0 else 6-i),y=float(y if side==0 else 7-y),
                        hp=skill['baseHealth'],maxHp=skill['baseHealth'],mana=0,maxMana=skill['maxMana'],cooldown=0,stun=0))
            frames,events,duration=simulate(fighters,defs)
            assert 0<duration<=24.65+.001 and frames
            for frame in frames:
                for f in frame['units']:
                    assert all(math.isfinite(v) for v in f.values() if isinstance(v,(int,float)))
                    assert 0<=f['hp']<=f['maxHp']+.001
                    assert 0<=f['shield']<=f['maxHp']*.5+.001
                    assert abs(f['damageDone']-f['basicDamageDone']-f['skillDamageDone'])<.001
            assert abs(sum(f['damageDone'] for f in fighters)-sum(f['damageTaken'] for f in fighters))<.001
            living=[sum(f['hp'] for f in fighters if f['side']==s) for s in (0,1)]
            results.append(dict(left=left['id'],right=right['id'],duration=duration,remainingHealth=living,casts=len(events)))
    args.output.parent.mkdir(parents=True,exist_ok=True)
    args.output.write_text(json.dumps(dict(purpose=__doc__,cases=len(results),results=results),indent=2),encoding='utf-8')
    print(f'SYNERGY MATCHUPS: {len(results)} simulations passed; finite values, health/shield caps, damage accounting, termination.')


if __name__=='__main__':main()
