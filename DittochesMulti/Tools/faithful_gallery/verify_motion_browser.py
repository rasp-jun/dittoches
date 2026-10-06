"""Validate actual skinned vertex motion and controls in installed Edge.

--review routes candidate GLBs into this test browser without publishing them.
"""
import argparse,json,sys,hashlib,math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT.parent/'tmp/gallery-test-tools'))
from playwright.sync_api import sync_playwright
sys.path.insert(0,str(Path(__file__).resolve().parent))
from verify_technique_browser import verify_techniques


def main():
    parser=argparse.ArgumentParser();parser.add_argument('--review',action='store_true');parser.add_argument('--ids',default='');parser.add_argument('--group',default='');parser.add_argument('--save-previews',action='store_true');parser.add_argument('--capture',default='Idle,Walk,Attack,Skill,Guard,Dodge,Down,Take 001')
    parser.add_argument('--changed-only',action='store_true',help='Verify Attack/Skill now and retain exact-byte baseline evidence for unchanged clips')
    args=parser.parse_args()
    manifest=json.loads((ROOT/'ArtSource/FaithfulGallery/manifest.json').read_text(encoding='utf-8-sig'))
    if args.review:
        sys.path.insert(0,str(ROOT/'Tools'))
        from build_faithful_manifest import glb_info
        from faithful_motion_catalog import BASELINE
        from roster_designs import DESIGNS,OFFICIAL
        existing={e['id'] for e in manifest['entries']}
        for ident,name,*_ in DESIGNS:
            candidate=ROOT/'ArtSource/AnimatedReview'/ident
            if ident in existing or not (candidate/'model.glb').exists():continue
            source=json.loads((candidate/'source.json').read_text(encoding='utf-8'))
            manifest['entries'].append(dict(id=ident,name=name,model=ident+'/model.glb',preview=ident+'/preview.png',
                reference='../Roster/ReferencesV2/'+ident+'.jpg',referenceSource='https://digimon.net/reference/detail.php?directory_name='+OFFICIAL.get(ident,ident),
                author=source.get('author',''),license=source.get('license',''),source=source.get('source_url',''),notes='신규 모델 모션 검토',**glb_info(candidate/'model.glb')))
        manifest['prepared']=len(manifest['entries'])
        manifest['pending']=[p for p in manifest['pending'] if p['id'] not in {e['id'] for e in manifest['entries']}]
        for entry in manifest['entries']:
            if args.ids and entry['id'] not in args.ids.split(','):continue
            candidate=ROOT/'ArtSource/AnimatedReview'/entry['id']/'model.glb'
            if candidate.exists():
                entry.update(glb_info(candidate))
                report=json.loads((candidate.parent/'animation-report.json').read_text(encoding='utf-8'))
                if report.get('techniques'):entry['techniques']=report['techniques']
    entries=manifest['entries']
    if args.ids:entries=[e for e in entries if e['id'] in args.ids.split(',')]
    assert entries,'No requested models found'
    if args.ids:assert {e['id'] for e in entries}==set(args.ids.split(',')),'Requested models missing from review'
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
            def previous(route):
                path=ROOT/BASELINE/'gallery'/Path(route.request.url).stem/'model.glb'
                route.fulfill(path=path,content_type='model/gltf-binary')
            page.route('**/FaithfulGallery/previous/*.glb',previous)
        page.goto('http://127.0.0.1:8766/',wait_until='networkidle',timeout=90000)
        page.wait_for_function('window.faithfulGallery?.state.ready',timeout=90000)
        for entry in entries:
            ident=entry['id'];page.evaluate('(id)=>faithfulGallery.selectModel(id)',ident)
            page.wait_for_function('(id)=>faithfulGallery.state.ready&&faithfulGallery.state.selected===id',arg=ident,timeout=90000)
            state=page.evaluate('faithfulGallery.state');assert state['clips'],ident+' has no motion'
            page.wait_for_function('(()=>{const img=document.querySelector("#reference-image");return img.complete&&img.naturalWidth>0;})()',timeout=30000)
            rows=[]
            if args.changed_only:
                sys.path.insert(0,str(ROOT/'Tools'))
                from faithful_motion_catalog import BASELINE
                from merge_technique_clips import chunks
                baseline=ROOT/BASELINE/'gallery'/ident/'model.glb'
                path=ROOT/'ArtSource/AnimatedReview'/ident/'model.glb' if args.review else ROOT/'ArtSource/FaithfulGallery'/ident/'model.glb'
                before,oldbin=chunks(baseline);after,newbin=chunks(path)
                assert newbin[:len(oldbin)]==oldbin
                previous={a['name']:a for a in before['animations']}
                for action in after['animations']:
                    if action['name'] not in ('Attack','Skill'):assert previous[action['name']]==action
                oldreport=json.loads((ROOT/BASELINE/'published-report.json').read_text(encoding='utf-8'))
                evidence=next(m for m in oldreport['models'] if m['id']==ident)
                assert evidence['sha256']==hashlib.sha256(baseline.read_bytes()).hexdigest()
                rows=[dict(c,validation_source='unchanged baseline bytes',baseline_sha256=evidence['sha256']) for c in evidence['clips'] if c['clip'] not in ('Attack','Skill')]
            for i,name in enumerate(state['clips']):
                if args.changed_only and name not in ('Attack','Skill'):continue
                start=page.evaluate('''(i)=>{faithfulGallery.playClip(i,false);faithfulGallery.seekMotion(0);
                    return {pose:faithfulGallery.poseSnapshot(), duration:faithfulGallery.state.animationDuration||0};}''',i)
                duration=page.locator('#animation-select').evaluate('()=>faithfulGallery.state.animationDuration')
                # state duration is refreshed in the render loop; wait one frame
                # after selecting a clip before using its timing in the UI.
                page.wait_for_timeout(60)
                duration=page.evaluate('faithfulGallery.state.animationDuration')
                end=page.evaluate('(t)=>{faithfulGallery.seekMotion(t);return faithfulGallery.poseSnapshot();}',duration*(.60 if name=='Skill' else .43))
                values=end['positions'];assert values and all(math.isfinite(v) for v in values),ident+' invalid skin'
                movement=max(abs(a-b) for a,b in zip(start['pose']['positions'],values))
                assert movement>.00001,(ident,name,'vertices did not move')
                rows.append({'clip':name,'duration':duration,'max_sampled_vertex_change':movement,'bones':end['bones'],'validation_source':'current browser'})
                if name in args.capture.split(','):
                    if name=='Down':page.evaluate('(t)=>faithfulGallery.seekMotion(t)',duration*.98)
                    page.wait_for_timeout(70)
                    page.screenshot(path=str(output/(ident+'-'+name.replace(' ','_')+'.png')))
            techniques=verify_techniques(page,entry,output) if entry.get('techniques') else []
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
                attack=state['clips'].index('Attack');page.evaluate('(i)=>{faithfulGallery.playClip(i,false);faithfulGallery.seekMotion(faithfulGallery.state.animationDuration-.12);}',attack)
                page.locator('#animation-play').click()
                page.wait_for_function('faithfulGallery.state.activeClip==="Idle"',timeout=60000)
                page.locator('#animation-repeat').check();page.evaluate('(i)=>{faithfulGallery.playClip(i,false);faithfulGallery.seekMotion(faithfulGallery.state.animationDuration-.12);}',attack)
                page.locator('#animation-play').click()
                page.wait_for_function('faithfulGallery.state.activeClip==="Attack"&&faithfulGallery.state.animationTime<faithfulGallery.state.animationDuration-.2',timeout=60000)
                page.locator('#animation-repeat').uncheck()
            if 'Down' in state['clips']:
                page.locator('[data-motion="Down"]').click()
                page.evaluate('faithfulGallery.seekMotion(faithfulGallery.state.animationDuration-.12)')
                page.locator('#animation-play').click()
                page.wait_for_function('faithfulGallery.state.activeClip==="Down"&&faithfulGallery.state.animationPaused',timeout=60000)
                page.wait_for_timeout(200)
                assert page.evaluate('faithfulGallery.state.activeClip')=='Down',(ident,'down pose not held')
            path=(ROOT/'ArtSource/AnimatedReview'/ident/'model.glb') if args.review and (ROOT/'ArtSource/AnimatedReview'/ident/'model.glb').exists() else ROOT/'ArtSource/FaithfulGallery'/ident/'model.glb'
            if args.save_previews:
                page.evaluate('(i)=>{faithfulGallery.playClip(i,false);faithfulGallery.seekMotion(0);faithfulGallery.frame("three-quarter");}',idle)
                page.wait_for_timeout(120)
                page.locator('.studio-heading').evaluate('(e)=>e.style.visibility="hidden"')
                page.locator('#canvas').screenshot(path=str(path.parent/'preview.png'))
                page.locator('.studio-heading').evaluate('(e)=>e.style.visibility=""')
            results.append({'id':ident,'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'clips':rows,'mesh_count':state['meshCount'],'techniques':techniques})
            (output/'browser-report.json').write_text(json.dumps({'passed':not errors and not external,'models':results,'errors':errors,'external_requests':external,'checks':['Current Attack/Skill skin and effects inspected; preserved clips use verified identical baseline bytes' if args.changed_only else 'Every clip skin inspected in current browser']},ensure_ascii=False,indent=2),encoding='utf-8')
            print('SKINNED BROWSER PASS',ident,len(rows),'clips',flush=True)
        browser.close()
    report={'passed':not errors and not external,'models':results,'errors':errors,'external_requests':external,
        'checks':['real skinned vertex changes in every clip','finite deformed vertices','official reference loads','timeline scrub and frame step','pause and resume','attack returns to idle','explicit repeat keeps attack playing']}
    (output/'browser-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    assert report['passed'],report
    print('MOTION BROWSER PASS',len(results),flush=True)

if __name__=='__main__':main()
