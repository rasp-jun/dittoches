"""Audit deployed portraits, evolution sizes and the actual player's UI smoke results."""
import hashlib
import json
import re
from pathlib import Path


def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    root=Path(__file__).resolve().parent.parent
    player=root/'Builds/FaithfulPreview'
    assets=root/'Assets/StreamingAssets/FaithfulPortraits'
    deployed=player/'DittochesMulti_Data/StreamingAssets/FaithfulPortraits'
    manifest=json.loads((assets/'manifest.json').read_text(encoding='utf-8'))
    models=json.loads((root/'Assets/StreamingAssets/FaithfulModels/manifest.json').read_text(encoding='utf-8'))
    expected={r['id']:r['source_sha256'] for r in models['models']}
    assert manifest['passed'] and len(manifest['files'])==68
    for row in manifest['models']:assert row['sourceHash']==expected[row['id']]
    for name,digest in manifest['files'].items():assert sha(assets/name)==sha(deployed/name)==digest,name
    scale=json.loads((player/'EvolutionScaleValidation/scale-report.json').read_text(encoding='utf-8'))
    ui=json.loads((player/'PortraitValidation/ui-report.json').read_text(encoding='utf-8'))
    assert scale['passed'] and len(scale['models'])==34 and ui['passed'] and ui['portraits']==34
    log=(player/'PortraitArena.log').read_text(encoding='utf-8')
    checks=re.search(r'ARENA SMOKE COMPLETE: (\d+) checks',log)
    assert checks and 'Exception:' not in log
    files=list((root/'Assets/Scripts').glob('*.cs'))
    report={'passed':True,'portraits':68,'models':34,'evolution_checks':scale['checks'],'ui_checks':int(checks[1]),
            'source_hashes':{str(p.relative_to(root)):sha(p) for p in files},
            'assembly_sha256':sha(player/'DittochesMulti_Data/Managed/Assembly-CSharp.dll'),
            'limitations':['Existing Mono player with imported ArenaSurface shader; regular Unity Editor build is still unavailable.']}
    output=root/'Builds/PortraitReview-20261006';output.mkdir(exist_ok=True)
    (output/'integration-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(f"PASS: 68 matching portraits, 34 models, {scale['checks']} size checks, {checks[1]} UI/game checks.")


if __name__=='__main__':main()
