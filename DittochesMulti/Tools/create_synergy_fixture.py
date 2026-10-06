"""Produce independent server resolver results for Unity's arena smoke test."""
import argparse
import json
import sys
from pathlib import Path

root=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(root/'Server'))
import combat_builds as b


def generate(destination):
    rows=[]
    for trait in b.TRAITS:
        for count in range(len(trait['members'])+1):
            team=trait['members'][:count]
            for duplicate in (False,True):
                ids=team+team[:1] if duplicate else team
                for uid in dict.fromkeys(trait['members']+['agumon','patamon']):
                    for items in ([],[0,3],[8,12]):
                        rows.append(dict(id=uid,team=ids,items=items,bonus=b.resolve(uid,ids,items)))
    destination.parent.mkdir(parents=True,exist_ok=True)
    destination.write_text(json.dumps(dict(rows=rows),ensure_ascii=False),encoding='utf-8')
    print(f'SYNERGY FIXTURE: {len(rows)} resolver cases -> {destination}')


if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--preview',type=Path,default=root/'Builds/SynergyPreview')
    generate(parser.parse_args().preview/'SynergyResolverFixture.json')
