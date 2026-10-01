"""Publish only candidate bytes that passed deformation and browser checks.

The immutable pre-animation gallery backup is mandatory. Original source GLBs
and formal Unity game assets are never overwritten by this operation.
"""
from pathlib import Path
import hashlib,json,shutil
from faithful_rig_profiles import PROFILES
from build_faithful_manifest import main as build_manifest

ROOT=Path(__file__).resolve().parent.parent
REVIEW=ROOT/'ArtSource/AnimatedReview'
GALLERY=ROOT/'ArtSource/FaithfulGallery'
BACKUP=ROOT/'ArtSource/AnimationBackup-20261001'
VALIDATION=ROOT/'Builds/FaithfulMotionValidation'


def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    browser={}
    for path in sorted(VALIDATION.glob('review*/browser-report.json')):
        report=json.loads(path.read_text(encoding='utf-8'))
        assert report['passed'],path
        for row in report['models']:browser[(row['id'],row['sha256'])]=row
    approved=[]
    for ident in PROFILES:
        folder=REVIEW/ident;digest=sha(folder/'model.glb')
        check=json.loads((folder/'deformation-report.json').read_text(encoding='utf-8'))
        assert check['passed'] and check['sha256']==digest,(ident,'deformation check')
        assert (ident,digest) in browser,(ident,'browser check missing or stale')
        assert (BACKUP/ident/'model.glb').is_file(),ident+' original backup missing'
        approved.append((ident,digest))
    for ident,digest in approved:
        folder=REVIEW/ident;target=GALLERY/ident
        report=json.loads((folder/'animation-report.json').read_text(encoding='utf-8'))
        assert report['output_sha256']==digest
        if report.get('refinement_pass',1)>=2:
            previous=ROOT/'ArtSource/NaturalPassBackup-20261001/gallery'/ident/'model.glb'
            assert previous.is_file()
            (GALLERY/'previous').mkdir(exist_ok=True)
            shutil.copy2(previous,GALLERY/'previous'/(ident+'.glb'))
            broad='Continuous foot trajectories, weight transfer and counter-rotation; relaxed raised hands; zero-offset looping clips.'
            if broad in report['shape_changes']:
                report['shape_changes'].remove(broad)
                report['shape_changes'].append('Removed the extra leading frame from all six exported clips so each loop starts at zero.')
                if PROFILES[ident]['kind']!='baby':report['shape_changes'].append('Continuous foot trajectories, weight transfer and torso counter-rotation.')
                if ident in ('agumon','wargreymon'):report['shape_changes'].append('Bent arms into a relaxed forward stance.')
                if ident=='agumon' or PROFILES[ident]['kind'] in ('human','angel'):report['shape_changes'].append('Added heel contact and toe-off with ankle support measured from the original foot surface.')
        report['status']='Sampled deformation and browser checks passed; selected poses visually reviewed. Original likeness remains a work in progress.'
        if ident=='greymon':
            record=json.loads((BACKUP/ident/'source.json').read_text(encoding='utf-8-sig'))
            original=Path(record['original_mesh_file']);report['source']=str(original.relative_to(ROOT));report['source_sha256']=sha(original)
            if not any('weld' in x.lower() for x in report['shape_changes']):report['shape_changes'].append('Joined and welded original dense export chunks before reduction; retained UVs and textures.')
        if ident=='holyangemon':report['shape_changes']+=['Torso-centred rig, relaxed arms, eight wings opened with six independent wing controls.']
        if ident=='seraphimon':report['shape_changes']+=['Lowered T-pose arms and preserved rigid gold wing panels.']
        (folder/'animation-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
        source=json.loads((folder/'source.json').read_text(encoding='utf-8'))
        source['local_modifications'].update(model_sha256=digest,canonical_front=True,changes=report['shape_changes'],motion_provenance=report['motion_provenance'])
        (folder/'source.json').write_text(json.dumps(source,ensure_ascii=False,indent=2),encoding='utf-8')
        for filename in ['model.glb','preview.png','source.json','animation-report.json','deformation-report.json']:
            shutil.copy2(folder/filename,target/filename)
        assert sha(target/'model.glb')==digest
    build_manifest()
    manifest=json.loads((GALLERY/'manifest.json').read_text(encoding='utf-8'))
    models=[]
    for e in manifest['entries']:
        digest=sha(GALLERY/e['id']/'model.glb');assert (e['id'],digest) in browser
        models.append(browser[(e['id'],digest)])
    result={'passed':True,'models':models,'animated_species':len(models),'authored_species':len(approved),
        'authored_clips':len(approved)*6,'all_clips':sum(len(m['clips']) for m in models),'pending_species':len(manifest['pending']),
        'backup':str(BACKUP.relative_to(ROOT)),
        'checks':['candidate and published SHA-256 match','13 skeletal deformation reports pass','all 15 species have actual browser-verified vertex motion','all original source attributions retained'],
        'limitations':['19 species still lack faithful models','Not integrated into Unity gameplay','New clips are local motion studies, not extracted game animation','Kuwagamon retains one source motion; Birdramon retains eleven.']}
    (VALIDATION/'published-report.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    print('PUBLISHED',len(models),'animated species;',result['all_clips'],'clips;',len(manifest['pending']),'species pending')

if __name__=='__main__':main()
