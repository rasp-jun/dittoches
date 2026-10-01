"""Validate actual skinned vertex motion and controls in installed Edge.

--review routes candidate GLBs into this test browser without publishing them.
"""
import argparse,json,sys,hashlib,math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT.parent/'tmp/gallery-test-tools'))
from playwright.sync_api import sync_playwright


def main():
    parser=argparse.ArgumentParser();parser.add_argument('--review',action='store_true');parser.add_argument('--ids',default='');parser.add_argument('--group',default='')
    args=parser.parse_args()
    manifest=json.loads((ROOT/'ArtSource/FaithfulGallery/manifest.json').read_text(encoding='utf-8-sig'))
    entries=manifest['entries']
    if args.ids:entries=[e for e in entries if e['id'] in args.ids.split(',')]
    for e in manifest['entries']:
        if (ROOT/'ArtSource/AnimatedReview'/e['id']/'model.glb').exists():
            e.pop('cameraDirection',None);e.pop('frontAngle',None)
    assert not args.group or args.group.isalnum()
    output=ROOT/'Builds/FaithfulMotionValidation'/(('review' if args.review else 'published')+('-'+args.group if args.group else ''));output.mkdir(parents=True,exist_ok=True)
    errors=[];external=[];results=[]
    with sync_playwright() as pw:
        browser=pw.chromium.launch(executable_path='C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless=True,
            args=['--use-angle=swiftshader','--enable-unsafe-swiftshader'])
        page=browser.new_page(viewport={'width':1440,'height':950},device_scale_factor=1)
        page.on('pageerror',lambda e:errors.append(str(e)))
        page.on('request',lambda r:external.append(r.url) if not r.url.startswith(('http://127.0.0.1:8766/','blob:','data:')) else None)
        page.route('**/manifest.json',lambda r:r.fulfill(json=manifest))
        if args.review:
            def candidate(route):
                ident=route.request.url.split('/')[-2];path=ROOT/'ArtSource/AnimatedReview'/ident/'model.glb'
                if path.exists():route.fulfill(path=path,content_type='model/gltf-binary')
                else:route.continue_()
            page.route('**/FaithfulGallery/*/model.glb',candidate)
        page.goto('http://127.0.0.1:8766/',wait_until='networkidle',timeout=90000)
        page.wait_for_function('window.faithfulGallery?.state.ready',timeout=90000)
        for entry in entries:
            ident=entry['id'];page.evaluate('(id)=>faithfulGallery.selectModel(id)',ident)
            page.wait_for_function('(id)=>faithfulGallery.state.ready&&faithfulGallery.state.selected===id',arg=ident,timeout=90000)
            state=page.evaluate('faithfulGallery.state');assert state['clips'],ident+' has no motion'
            assert page.locator('#reference-image').evaluate('(img)=>img.complete&&img.naturalWidth>0')
            rows=[]
            for i,name in enumerate(state['clips']):
                start=page.evaluate('''(i)=>{faithfulGallery.playClip(i,false);faithfulGallery.seekMotion(0);
                    return {pose:faithfulGallery.poseSnapshot(), duration:faithfulGallery.state.animationDuration||0};}''',i)
                duration=page.locator('#animation-select').evaluate('()=>faithfulGallery.state.animationDuration')
                # state duration is refreshed in the render loop; wait one frame
                # after selecting a clip before using its timing in the UI.
                page.wait_for_timeout(60)
                duration=page.evaluate('faithfulGallery.state.animationDuration')
                end=page.evaluate('(t)=>{faithfulGallery.seekMotion(t);return faithfulGallery.poseSnapshot();}',duration*.43)
                values=end['positions'];assert values and all(math.isfinite(v) for v in values),ident+' invalid skin'
                movement=max(abs(a-b) for a,b in zip(start['pose']['positions'],values))
                assert movement>.00001,(ident,name,'vertices did not move')
                rows.append({'clip':name,'duration':duration,'max_sampled_vertex_change':movement,'bones':end['bones']})
                if name.lower() in ('idle','walk','attack','take 001'):
                    page.screenshot(path=str(output/(ident+'-'+name.replace(' ','_')+'.png')))
            # Drive the actual timeline and frame buttons, not just test APIs.
            idle=next((i for i,n in enumerate(state['clips']) if 'idle' in n.lower()),0)
            page.evaluate('(i)=>faithfulGallery.playClip(i,false)',idle)
            page.locator('#animation-seek').fill('0.2');page.locator('#animation-seek').dispatch_event('input')
            before=page.evaluate('faithfulGallery.state.animationTime')
            page.locator('#frame-next').click();after=page.evaluate('faithfulGallery.state.animationTime')
            assert abs(after-before-1/30)<.0001,(ident,'frame step')
            page.wait_for_timeout(80);assert page.evaluate('faithfulGallery.state.animationPaused')
            page.locator('#animation-play').click()
            page.wait_for_function('(t)=>faithfulGallery.state.animationTime!==t',arg=after,timeout=10000)
            if 'Attack' in state['clips']:
                attack=state['clips'].index('Attack');page.evaluate('(i)=>faithfulGallery.playClip(i,false)',attack)
                page.wait_for_function('faithfulGallery.state.activeClip==="Idle"',timeout=15000)
                page.locator('#animation-repeat').check();page.evaluate('(i)=>faithfulGallery.playClip(i,false)',attack)
                page.wait_for_timeout(1400);assert page.evaluate('faithfulGallery.state.activeClip')=='Attack'
                page.locator('#animation-repeat').uncheck()
            path=(ROOT/'ArtSource/AnimatedReview'/ident/'model.glb') if args.review and (ROOT/'ArtSource/AnimatedReview'/ident/'model.glb').exists() else ROOT/'ArtSource/FaithfulGallery'/ident/'model.glb'
            results.append({'id':ident,'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'clips':rows,'mesh_count':state['meshCount']})
            print('SKINNED BROWSER PASS',ident,len(rows),'clips',flush=True)
        browser.close()
    report={'passed':not errors and not external,'models':results,'errors':errors,'external_requests':external,
        'checks':['real skinned vertex changes in every clip','finite deformed vertices','official reference loads','timeline scrub and frame step','pause and resume','attack returns to idle','explicit repeat keeps attack playing']}
    (output/'browser-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    assert report['passed'],report
    print('MOTION BROWSER PASS',len(results),flush=True)

if __name__=='__main__':main()
