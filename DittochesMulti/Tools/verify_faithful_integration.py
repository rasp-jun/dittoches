"""Audit source -> packed asset -> player identity and collect real test evidence."""
import hashlib,json
from pathlib import Path
from faithful_effect_runtime import effect_runtime_hashes
ROOT=Path(__file__).resolve().parent.parent
def read(path):return json.loads(path.read_text(encoding='utf-8-sig'))
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def main():
    output=ROOT/'Builds/MotionEngine-20261006'
    player=ROOT/'Builds/FaithfulPreview'
    packed=ROOT/'Assets/StreamingAssets/FaithfulModels'
    deployed=player/'DittochesMulti_Data/StreamingAssets/FaithfulModels'
    original=read(ROOT/'ArtSource/FaithfulGallery/manifest.json')
    manifest=read(packed/'manifest.json')
    assert len(original['entries'])==len(manifest['models'])==34
    runtime=read(player/'FaithfulValidation/runtime-report.json')
    combat=read(player/'FaithfulCombatValidation/combat-report.json')
    replay=read(player/'FaithfulReplayValidation/replay-report.json')
    gallery=read(output/'gallery/connection-report.json')
    engine=read(output/'engine-report.json')
    effects=read(ROOT/'Builds/TechniqueReview/presentation-report.json')
    for report in (runtime,combat,replay,gallery,engine,effects):assert report['passed']
    assert len(runtime['models'])==len(gallery['models'])==len(effects['models'])==34
    assert runtime['clips']==manifest['clips']==352
    assert effects['runtime_files']==effect_runtime_hashes(ROOT)
    sources={r['id']:r for r in original['entries']}
    rendered={r['id']:r for r in runtime['models']}
    for row in manifest['models']:
        ident=row['id'];file=ident+'.bytes'
        assert row['source_sha256']==sources[ident]['sha256']==rendered[ident]['sha256']==sha(ROOT/'ArtSource/FaithfulGallery'/ident/'model.glb')
        assert row['sha256']==sha(packed/file)==sha(deployed/file)
    assert sha(packed/'techniques.json')==sha(deployed/'techniques.json')
    code=list((ROOT/'Assets/Scripts').glob('*.cs'))+[ROOT/'Assets/Resources/FaithfulCharacter.shader',ROOT/'Tools/export_faithful_runtime.py',ROOT/'Tools/build_portable_preview.py']
    result=dict(passed=True,models=34,clips=352,runtime_checks=runtime['checks'],combat=combat,replay=replay,
                gallery_engine=engine,player_assembly_sha256=sha(player/'DittochesMulti_Data/Managed/Assembly-CSharp.dll'),
                runtime_source_sha256={str(p.relative_to(ROOT)):sha(p) for p in code},
                gallery_runtime_sha256=effect_runtime_hashes(ROOT),
                technique_metadata_sha256=sha(packed/'techniques.json'),
                verified_shaders=sorted({r['shader'] for r in runtime['models']}),
                limitations=['Full IMGUI smoke did not repaint in a hidden window; battlefield rendering was tested directly.',
                'Unity Editor unavailable: new FaithfulCharacter shader and regular Editor build are not verified; player uses the existing imported ArenaSurface shader.',
                'Online evidence is a production-simulator fixture rendered offline from both sides, not a live multiplayer session.'])
    (output/'integration-report.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    print('INTEGRATION PASS: 34 models / 352 clips; source, runtime package and deployed player match')
if __name__=='__main__':main()
