"""Read-only audit of original preservation and the current expanded asset graph."""
import hashlib,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
CHECKS=ROOT.parents[1]/'03_Checks/LightLine-20261013'
sys.path.insert(0,str(ROOT/'Tools'))
from faithful_techniques import TECHNIQUES
from build_faithful_manifest import build_catalog
load=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
packed=ROOT/'Assets/StreamingAssets/FaithfulModels'
baseline=load(CHECKS/'before/model-hashes.json')
preserved=[n for n in baseline if n.endswith('.bytes') or n.endswith('-source.json')]
assert len(preserved)==68
assert all(sha(packed/n).upper()==baseline[n] for n in preserved)
manifest=load(packed/'manifest.json');gallery=load(ROOT/'ArtSource/FaithfulGallery/manifest.json')
ids={r['id'] for r in manifest['models']};sources={r['id']:r for r in gallery['entries']}
assert len(manifest['models'])==len(ids)==39 and manifest['clips']==402
assert ids==set(sources)==set(TECHNIQUES)
for row in manifest['models']:
    assert sha(packed/(row['id']+'.bytes'))==row['sha256'].lower(),row['id']
    assert sha(ROOT/'ArtSource/FaithfulGallery'/sources[row['id']]['model'])==row['source_sha256'].lower(),row['id']
old=load(CHECKS/'before/techniques-original34.json')
assert all(TECHNIQUES[k]==v for k,v in old.items())
small=load(ROOT/'ArtSource/LightLine-20261013/small-techniques.json')
records={k:v for k,v in small.items() if k in TECHNIQUES}
records.update(load(ROOT/'ArtSource/LightLine-20261013/angel-techniques.json'))
for ident,record in records.items():
    for mode in ['Attack','Skill']:
        for field in ['duration','release','origin']:
            if field in record[mode]:assert TECHNIQUES[ident][mode][field]==record[mode][field],(ident,mode,field)
regenerated=build_catalog()
assert regenerated['scope']==regenerated['prepared']==len(regenerated['entries'])==39 and not regenerated['pending']
issues=[]
if gallery['scope']!=39 or gallery['prepared']!=39:issues.append('Published gallery scope/prepared does not match 39 model entries')
capture_checks=[]
for ident in records:
    folder=CHECKS/'Preview/FaithfulValidation';idle=folder/(ident+'-Idle.png');skill=folder/(ident+'-Skill.png')
    if idle.exists() and skill.exists():
        different=sha(idle)!=sha(skill);capture_checks.append(dict(id=ident,idle_skill_distinct=different))
        if not different:issues.append(ident+' Idle/Skill captures are identical; a GPU skin cache may have reused the first pose')
result=dict(passed=not issues,models=39,clips=402,preserved_original_files=68,original_techniques_unchanged=34,
    gallery_scope=gallery['scope'],gallery_prepared=gallery['prepared'],captures=capture_checks,issues=issues,
    checks=['All 39 source GLB and runtime byte hashes match manifests','Original 34 packed models and source records: all 68 hashes unchanged',
      'Original 34 technique specifications unchanged','New five technique timings/origins match authored motion metadata',
      'Generic catalog regeneration retains all 39 source paths without writing manifest or assets'])
(CHECKS/'independent-model-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(result,ensure_ascii=False,indent=2))
