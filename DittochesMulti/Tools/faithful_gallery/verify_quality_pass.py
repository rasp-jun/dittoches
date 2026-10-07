"""Render the quality pass and check deterministic technique silhouettes."""
import argparse
import hashlib
import json
import math
import sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools'))
from faithful_effect_runtime import effect_runtime_hashes
sys.path.insert(0,str(ROOT.parent/f'tmp/gallery-test-tools-py{sys.version_info.major}{sys.version_info.minor}'))
import greenlet
sys.path.insert(0,str(ROOT.parent/'tmp/gallery-test-tools'))
from playwright.sync_api import sync_playwright


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--ids',default='')
    parser.add_argument('--baseline',action='store_true')
    args=parser.parse_args()
    out=ROOT/'Builds/QualityPass-20261006'
    dest=out/('baseline' if args.baseline else 'current');dest.mkdir(parents=True,exist_ok=True)
    entries=json.loads((ROOT/'ArtSource/FaithfulGallery/manifest.json').read_text(encoding='utf-8'))['entries']
    if args.ids:entries=[e for e in entries if e['id'] in args.ids.split(',')]
    assert entries
    errors=[];results=[]
    runtime_files=effect_runtime_hashes(ROOT)
    with sync_playwright() as pw:
        browser=pw.chromium.launch(executable_path='C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',
            headless=True,args=['--use-angle=swiftshader','--enable-unsafe-swiftshader'])
        page=browser.new_page(viewport={'width':1440,'height':950},device_scale_factor=1)
        page.on('pageerror',lambda e:errors.append(str(e)))
        page.on('console',lambda m:errors.append(m.text) if m.type=='error' else None)
        if args.baseline:
            for name in ['gallery.js','technique-effects.js']:
                page.route('**/'+name,lambda route:route.fulfill(path=out/'before'/route.request.url.split('/')[-1],content_type='text/javascript'))
        page.goto('http://127.0.0.1:8766/',wait_until='networkidle',timeout=90000)
        page.wait_for_function('window.faithfulGallery?.state.ready',timeout=90000)
        for entry in entries:
            ident=entry['id']
            page.evaluate('(id)=>faithfulGallery.selectModel(id)',ident)
            page.wait_for_function('(id)=>faithfulGallery.state.ready&&faithfulGallery.state.selected===id',arg=ident,timeout=90000)
            observations=[]
            for mode,u in [('Idle',0),('Attack',.51),('Skill',.35),('Skill',.67),('Skill',.81)]:
                effect=page.evaluate('''([mode,u])=>{
                  const g=faithfulGallery;g.playClip(g.state.clips.indexOf(mode),false);
                  g.seekMotion(g.state.animationDuration*u);return g.state.techniqueEffect;
                }''',[mode,u])
                if u in (.51,.67):assert effect['active'],(ident,mode,'no visible release',effect)
                assert all(math.isfinite(x) for x in effect.get('emitter',[]))
                if not args.baseline and mode=='Idle':assert page.evaluate('faithfulGallery.state.surfaceFinish.materials')>0
                if mode!='Idle':
                    # Reconstruct a frame after a different time and confirm both
                    # shape counts and positions are deterministic at the source.
                    again=page.evaluate('''(u)=>{const g=faithfulGallery;g.seekMotion(.1);g.seekMotion(g.state.animationDuration*u);return g.state.techniqueEffect;}''',u)
                    assert effect==again,(ident,mode,'nondeterministic effect')
                    if not args.baseline:
                        assert page.evaluate('''(u)=>{
                          const g=faithfulGallery,expected=JSON.stringify(g.effectSnapshot());
                          g.seekMotion(.1);g.seekMotion(g.state.animationDuration*u);
                          return expected===JSON.stringify(g.effectSnapshot());
                        }''',u),(ident,mode,'particle matrices/colors/opacity changed after seeking')
                frame=page.evaluate('faithfulGallery.state.frames')
                page.wait_for_function('(f)=>faithfulGallery.state.frames>f',arg=frame)
                page.screenshot(path=str(dest/f'{ident}-{mode}-{round(u*100):02d}.png'))
                observations.append(dict(clip=mode,**effect))
            page.locator('#technique-effects').uncheck()
            page.wait_for_function('!faithfulGallery.state.techniqueEffect.active')
            page.locator('#technique-effects').check()
            page.evaluate('faithfulGallery.seekMotion(faithfulGallery.state.animationDuration*.99)')
            assert not page.evaluate('faithfulGallery.state.techniqueEffect.active')
            digest=hashlib.sha256((ROOT/'ArtSource/FaithfulGallery'/entry['model']).read_bytes()).hexdigest()
            assert digest==entry['sha256'],'Source model changed'
            results.append(dict(id=ident,sha256=digest,observations=observations))
            print('QUALITY PASS',ident,'baseline' if args.baseline else 'current',flush=True)
        browser.close()
    assert args.baseline or runtime_files==effect_runtime_hashes(ROOT),'Runtime changed during verification'
    report=dict(passed=not errors,errors=errors,baseline=args.baseline,models=results,
                runtime_sha256=runtime_files['technique-effects.js'],runtime_files=runtime_files)
    (dest/'report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    assert not errors,errors


if __name__=='__main__':main()
