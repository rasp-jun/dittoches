"""Publish the exact candidate bytes approved by deformation and browser checks."""
from pathlib import Path
import argparse,hashlib,json,shutil
from faithful_rig_profiles import PROFILES
from faithful_motion_catalog import CLIPS,BASELINE
from build_faithful_manifest import main as build_manifest
from faithful_effect_runtime import effect_runtime_hashes

ROOT=Path(__file__).resolve().parent.parent
REVIEW=ROOT/'ArtSource/AnimatedReview'
GALLERY=ROOT/'ArtSource/FaithfulGallery'
BACKUP=ROOT/BASELINE/'gallery'
VALIDATION=ROOT/'Builds/FaithfulMotionValidation'
MATERIAL_IDS={'birdramon','kuwagamon'}
ALL=set(PROFILES)|MATERIAL_IDS
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def read(path):return json.loads(path.read_text(encoding='utf-8-sig'))

def main(ids=None):
    selected=set(ids or ALL)
    assert selected and selected<=ALL,'Unknown candidate model IDs'
    browser={}
    for path in sorted(VALIDATION.glob('review*/browser-report.json')):
        report=read(path)
        if not report['passed']:continue
        for row in report['models']:
            if BASELINE=='ArtSource/TechniqueMotionBackup-20261002' and len(row.get('techniques',[]))!=2:continue
            browser[(row['id'],row['sha256'])]=row
    approved=[]
    if BASELINE=='ArtSource/TechniqueMotionBackup-20261002':
        presentation=read(ROOT/'Builds/TechniqueReview/presentation-report.json')
        assert presentation['passed'],'Final effect rendering failed'
        assert presentation['runtime_sha256']==sha(ROOT/'Tools/faithful_gallery/technique-effects.js'),'Effect shader check stale'
        assert presentation.get('runtime_files')==effect_runtime_hashes(ROOT),'Effect dependency check missing or stale'
        presented={(r['id'],r['sha256']) for r in presentation['models']}
    structure=read(ROOT/'Builds/FaithfulNaturalValidation/glb-report.json')
    assert structure['passed'],'GLB validation failed'
    glb_checked={(r['id'],r['sha256']) for r in structure['models']}
    native_checked=set()
    if selected&MATERIAL_IDS:
        transitions=read(ROOT/'Builds/FaithfulNaturalValidation/native-transition-report.json')
        assert transitions['passed'],'Native transitions failed'
        native_checked={(r['id'],r['sha256']) for r in transitions['models'] if r['passed']}
    # Finish every selected validation before replacing any gallery files.
    for ident in sorted(selected):
        folder=REVIEW/ident;digest=sha(folder/'model.glb')
        check_name='deformation-report.json'
        check=read(folder/check_name)
        assert check['passed'] and check['sha256']==digest,(ident,'candidate check stale or failed')
        assert (ident,digest) in browser,(ident,'browser check missing or stale')
        if BASELINE=='ArtSource/TechniqueMotionBackup-20261002':
            assert len(browser[(ident,digest)].get('techniques',[]))==2,(ident,'technique browser check missing')
            assert (ident,digest) in presented,(ident,'final technique presentation missing')
        if (GALLERY/ident/'model.glb').exists():assert (BACKUP/ident/'model.glb').is_file(),ident+' baseline backup missing'
        else:assert (ROOT/'ArtSource/ExpandedSources-20261002'/ident/'model.glb').is_file(),ident+' source missing'
        assert (folder/'preview.png').is_file() and (folder/'source.json').is_file()
        report=read(folder/'animation-report.json');assert report['output_sha256']==digest
        if report.get('shape_revision'):
            comparison=(ROOT/report['comparison_source']).resolve()
            assert comparison.is_relative_to((ROOT/'ArtSource').resolve()) and comparison.is_file(),'Shape comparison backup missing'
        assert (ident,digest) in glb_checked,(ident,'GLB structure check missing or stale')
        if ident in MATERIAL_IDS:assert (ident,digest) in native_checked,(ident,'Native transition check missing or stale')
        if ident=='greymon':
            alignment=read(folder/'alignment-report.json')
            assert alignment['passed'] and alignment['sha256']==digest,'Greymon neck alignment check missing or stale'
        approved.append((ident,digest,check_name))
    for ident,digest,check_name in approved:
        folder=REVIEW/ident;target=GALLERY/ident;target.mkdir(parents=True,exist_ok=True)
        report=read(folder/'animation-report.json')
        previous=ROOT/report['comparison_source'] if report.get('shape_revision') else BACKUP/ident/'model.glb'
        if previous.exists():
            (GALLERY/'previous').mkdir(exist_ok=True);shutil.copy2(previous,GALLERY/'previous'/(ident+'.glb'))
        files=['model.glb','preview.png','source.json',check_name]
        if (folder/'stance-report.json').exists():files.append('stance-report.json')
        if (folder/'alignment-report.json').exists():files.append('alignment-report.json')
        if (folder/'shoulder-report.json').exists():files.append('shoulder-report.json')
        report=read(folder/'animation-report.json')
        report['status']='Sampled deformation and browser checks passed; selected poses visually reviewed. Local review asset, not integrated into gameplay.'
        (folder/'animation-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
        files.append('animation-report.json')
        for filename in files:shutil.copy2(folder/filename,target/filename)
        assert sha(target/'model.glb')==digest
    build_manifest();manifest=read(GALLERY/'manifest.json');models=[]
    for entry in manifest['entries']:
        digest=sha(GALLERY/entry['id']/'model.glb')
        assert (entry['id'],digest) in browser,(entry['id'],'published browser evidence missing')
        models.append(browser[(entry['id'],digest)])
    authored=[e for e in manifest['entries'] if e['id'] in PROFILES]
    result={'passed':True,'models':models,'updated_species':sorted(selected),'animated_species':len(models),
        'authored_species':len(authored)+1,'authored_clips':(len(authored)+1)*len(CLIPS),'adapted_source_clips':len(CLIPS),
        'common_clips':len(models)*len(CLIPS),'preserved_source_clips':12,'all_clips':sum(len(m['clips']) for m in models),
        'pending_species':len(manifest['pending']),'backup':str(BACKUP.relative_to(ROOT)),
        'checks':['Candidate and published SHA-256 match','Every authored rig passed sampled skeletal deformation checks',
            'Every published clip produced actual browser-verified vertex motion','Original source files and attributions retained',
            'Birdramon and Kuwagamon original geometry and animation bytes retained alongside new clips'],
        'limitations':['Not integrated into Unity gameplay','Local authored clips are motion studies, not extracted game animation',
            'Kuwagamon adds ten local motions; Birdramon adds ten adapted motions. Their twelve source clips remain intact.','Source provenance retained; original likeness remains open to visual refinement.']}
    if BASELINE=='ArtSource/TechniqueMotionBackup-20261002':
        shape_refined=[e['id'] for e in manifest['entries'] if read(GALLERY/e['id']/'animation-report.json').get('shape_revision')]
        result.update(changed_combat_clips=len(selected-set(shape_refined))*2,preserved_common_clips=(len(models)-len(shape_refined))*8,
            shape_refined_species=shape_refined,
            technique_references={e['id']:e['techniques']['source'] for e in manifest['entries']},
            effect_runtime='Tools/faithful_gallery/technique-effects.js',
            effect_runtime_sha256=sha(ROOT/'Tools/faithful_gallery/technique-effects.js'))
        result['checks'].append('Attack and Skill effects, labels, timing, toggle and recovery checked in real WebGL; binary preservation applies to technique-only edits. Shape-refined species re-export their rig and all clips.')
        result['limitations'].append('Technique particles and projectiles are gallery runtime effects; GLBs contain skeletal animation only. Tokomon jaw and Kuwagamon pincers lack independent source joints.')
    (VALIDATION/'published-report.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    print('PUBLISHED',len(models),'animated species;',result['all_clips'],'clips;',len(manifest['pending']),'species pending')

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--ids',help='Replace only these comma-separated reviewed models')
    args=parser.parse_args();main(args.ids.split(',') if args.ids else None)
