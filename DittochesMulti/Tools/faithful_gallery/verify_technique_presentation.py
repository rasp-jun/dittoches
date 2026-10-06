"""Verify final effect shaders and capture the complete published technique roster."""
import sys,json,hashlib,argparse
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools'))
from faithful_effect_runtime import effect_runtime_hashes
sys.path.insert(0,str(ROOT.parent/'tmp/gallery-test-tools'))
from playwright.sync_api import sync_playwright


def main():
    parser=argparse.ArgumentParser();parser.add_argument('--review',action='store_true');parser.add_argument('--ids',default='');parser.add_argument('--release-only',action='store_true');args=parser.parse_args()
    runtime_digest=hashlib.sha256((ROOT/'Tools/faithful_gallery/technique-effects.js').read_bytes()).hexdigest()
    runtime_files=effect_runtime_hashes(ROOT)
    output=ROOT/'Builds/TechniqueReview/frames';output.mkdir(parents=True,exist_ok=True)
    manifest=json.loads((ROOT/'ArtSource/FaithfulGallery/manifest.json').read_text(encoding='utf-8-sig'))
    for entry in manifest['entries']:
        if args.review:
            report=json.loads((ROOT/'ArtSource/AnimatedReview'/entry['id']/'animation-report.json').read_text(encoding='utf-8'))
            entry['techniques']=report['techniques']
    errors=[];models=[]
    with sync_playwright() as pw:
        browser=pw.chromium.launch(executable_path='C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless=True,args=['--use-angle=swiftshader','--enable-unsafe-swiftshader'])
        page=browser.new_page(viewport={'width':1440,'height':950})
        page.on('pageerror',lambda error:errors.append(str(error)))
        page.on('console',lambda msg:errors.append(msg.text) if msg.type=='error' else None)
        page.route('**/manifest.json',lambda route:route.fulfill(json=manifest))
        if args.review:
            page.route('**/FaithfulGallery/*/model.glb',lambda route:route.fulfill(path=ROOT/'ArtSource/AnimatedReview'/route.request.url.split('/')[-2]/'model.glb',content_type='model/gltf-binary'))
        page.goto('http://127.0.0.1:8766/',wait_until='networkidle',timeout=90000)
        page.wait_for_function('window.faithfulGallery?.state.ready',timeout=90000)
        for entry in manifest['entries']:
            if args.ids and entry['id'] not in args.ids.split(','):continue
            ident=entry['id'];page.evaluate('(id)=>faithfulGallery.selectModel(id)',ident)
            page.wait_for_function('(id)=>faithfulGallery.state.ready&&faithfulGallery.state.selected===id',arg=ident,timeout=90000)
            observations=[]
            for mode in ['Attack','Skill']:
                contact=entry['techniques'][mode]['effect'] in ('impact','arc','horn','bite','pincers','claws','needles')
                for stage,u in [('charge',.3 if mode=='Attack' else .42),('release',.51 if mode=='Attack' else .67 if contact else .76)]:
                    effect=page.evaluate('''([mode,u])=>{const g=faithfulGallery;g.playClip(g.state.clips.indexOf(mode),false);g.seekMotion(g.state.animationDuration*u);return g.state.techniqueEffect;}''',[mode,u])
                    if stage=='release':assert effect['active'],(ident,mode,'no effect')
                    assert page.locator('#technique-'+mode.lower()+' strong').inner_text()==entry['techniques'][mode]['name']
                    if not args.release_only or stage=='release':page.screenshot(path=str(output/(ident+'-'+mode+'-'+stage+'.png')))
                    observations.append(dict(clip=mode,stage=stage,**effect))
                page.evaluate('faithfulGallery.seekMotion(faithfulGallery.state.animationDuration*.99)')
                assert not page.evaluate('faithfulGallery.state.techniqueEffect.active')
            path=ROOT/('ArtSource/AnimatedReview' if args.review else 'ArtSource/FaithfulGallery')/ident/'model.glb'
            models.append(dict(id=ident,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),poses=observations))
            print('FINAL TECHNIQUE PRESENTATION',ident,flush=True)
        browser.close()
    assert runtime_digest==hashlib.sha256((ROOT/'Tools/faithful_gallery/technique-effects.js').read_bytes()).hexdigest(),'Effect runtime changed during verification'
    assert runtime_files==effect_runtime_hashes(ROOT),'Effect dependency changed during verification'
    result=dict(passed=not errors,errors=errors,models=models,runtime_sha256=runtime_digest,runtime_files=runtime_files)
    (output.parent/'presentation-report.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    assert result['passed'],errors


if __name__=='__main__':main()
