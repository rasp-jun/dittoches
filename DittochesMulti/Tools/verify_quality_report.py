"""Check final report freshness and the distinguishing technique shapes."""
import hashlib
import json
from pathlib import Path
from faithful_effect_runtime import effect_runtime_hashes

ROOT=Path(__file__).resolve().parent.parent
out=ROOT/'Builds/QualityPass-20261006'
report=json.loads((out/'current/report.json').read_text(encoding='utf-8'))
manifest=json.loads((ROOT/'ArtSource/FaithfulGallery/manifest.json').read_text(encoding='utf-8'))
assert report['passed'] and not report['baseline'] and not report['errors']
assert report['runtime_files']==effect_runtime_hashes(ROOT),'Presentation check is stale'
assert {m['id'] for m in report['models']}=={e['id'] for e in manifest['entries']}
assert len(report['models'])==34
required={
    'breath':{'plume','flame'},'ice':{'crystal','beam'},'water':{'ring','flame','beam'},
    'fireball':{'core','flame'},'spiral':{'core','flame'},'electricorb':{'core','rod'},
    'holybeam':{'ring','beam'},'thunderbeam':{'rod','beam'},'lightning':{'rod'},
    'sun':{'core','slash'},'seven':{'core','ring'},'gate':{'disc','ring','crystal'},
    'missiles':{'core','cone','feather','flame'},'meteors':{'feather','flame'},
    'shadowbird':{'bird','slash'},'flower':{'feather','core'},'air':{'ring','slash'},
    'bubbles':{'ring','slash'},'vines':{'rod','cone'},'whip':{'rod','cone'},
    'darkclaw':{'rod','slash'},'darkorb':{'core','slash','ring'},
    'starlight':{'feather','beam'},'bite':{'slash','cone'},'pincers':{'slash','cone'},
    'claws':{'slash'},'horn':{'cone','ring'},'needles':{'cone','slash'},
}
checks=[]
for entry in manifest['entries']:
    row=next(r for r in report['models'] if r['id']==entry['id'])
    digest=hashlib.sha256((ROOT/'ArtSource/FaithfulGallery'/entry['model']).read_bytes()).hexdigest()
    assert digest==entry['sha256']==row['sha256']
    skill=[p for p in row['observations'] if p['clip']=='Skill']
    assert len(skill)==3 and all(p.get('signature',True) for p in skill)
    shapes=set().union(*(set(p.get('shapes',{})) for p in skill))
    effect=entry['techniques']['Skill']['effect']
    assert required[effect]<=shapes,(entry['id'],effect,'missing signature',shapes)
    if effect=='seven':assert skill[0]['shapes']['core']==7,'Seven Heavens must show seven charges'
    if effect=='missiles':assert skill[1]['shapes']['core']==2,'Giga Destroyer must show two missiles'
    if effect=='gate':assert skill[1]['shapes']['disc']==1,'Gate needs an interior'
    checks.append(dict(id=entry['id'],effect=effect,shapes=sorted(shapes)))
summary=dict(passed=True,species=34,clips=sum(len(e['animations']) for e in manifest['entries']),
             captures=len(list((out/'current').glob('*.png'))),signature_checks=checks,
             runtime_files=report['runtime_files'],source_models_preserved=True)
(out/'summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
print('QUALITY REPORT PASS: 34 species, distinct technique shapes, source hashes, dependency hashes')
