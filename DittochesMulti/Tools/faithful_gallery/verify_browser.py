"""Exercise the gallery in an actual installed Edge browser with Playwright.

Install Playwright in the isolated workspace test folder, then run:
python Tools/faithful_gallery/verify_browser.py
The server must be running. No production model or manifest is edited.
"""
import argparse
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT.parent / 'tmp/gallery-test-tools'))
from playwright.sync_api import sync_playwright

OUTPUT = ROOT / 'Builds/FaithfulGalleryValidation'
OUTPUT.mkdir(parents=True, exist_ok=True)
NAMES = {'agumon':'아구몬','koromon':'코로몬','tsunomon':'뿔몬','pyocomon':'어니몬','mochimon':'모티몬',
         'greymon':'그레이몬','garurumon':'가루몬','metalgarurumon':'메탈가루몬','kabuterimon':'캅테리몬',
         'birdramon':'버드라몬','angemon':'엔젤몬','holyangemon':'홀리엔젤몬','seraphimon':'세라피몬','wargreymon':'워그레이몬'}

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--ids',default='')
    args=parser.parse_args()
    manifest = ROOT / 'ArtSource/FaithfulGallery/manifest.json'
    fixture = not manifest.exists()
    if fixture:
        entries = [{'id': p.parent.name, 'name': NAMES.get(p.parent.name,p.parent.name),
                    'model': p.parent.name+'/model.glb', 'reference':'../Roster/ReferencesV2/'+p.parent.name+'.jpg',
                    'author':'검증용 임시 목록', 'license':'원본 source.json 참조', 'source':'',
                    'notes':'테스트 페이지 내부에서만 주입한 모델 목록입니다.'}
                   for p in sorted((ROOT/'ArtSource/FaithfulGallery').glob('*/model.glb'))]
    else:
        raw = json.loads(manifest.read_text(encoding='utf-8-sig'))
        entries = raw if isinstance(raw,list) else raw.get('entries',raw.get('models',raw.get('characters',[])))
    assert entries, 'No real GLB files are available for browser testing.'
    if args.ids:
        wanted=set(args.ids.split(','))
        entries=[entry for entry in entries if entry['id'] in wanted]
    errors = []
    external = []
    results = []
    with sync_playwright() as pw:
        browser = pw.chromium.launch(executable_path=r'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',
            headless=True, args=['--use-angle=swiftshader','--enable-unsafe-swiftshader'])
        page = browser.new_page(viewport={'width':1440,'height':950},device_scale_factor=1)
        page.on('pageerror',lambda err: errors.append(str(err)))
        page.on('request',lambda req: external.append(req.url) if not req.url.startswith(('http://127.0.0.1:8766/','blob:','data:')) else None)
        if fixture:
            page.route('**/ArtSource/FaithfulGallery/manifest.json',lambda route:route.fulfill(json={'entries':entries}))
        page.goto('http://127.0.0.1:8766/',wait_until='networkidle',timeout=60000)
        page.wait_for_function('window.faithfulGallery?.state.ready',timeout=60000)
        for i,entry in enumerate(entries):
            if not entry.get('model'): continue
            page.evaluate('(id)=>window.faithfulGallery.selectModel(id)',entry['id'])
            page.wait_for_function('(id)=>window.faithfulGallery.state.ready && window.faithfulGallery.state.selected===id',arg=entry['id'],timeout=60000)
            page.wait_for_timeout(350)
            state=page.evaluate('window.faithfulGallery.state')
            assert state['meshCount']>0 and state['triangles']>0, entry['id']+' empty geometry'
            assert state['bounds']['height']>0,entry['id']+' invalid bounds'
            reference_ok=page.locator('#reference-image').evaluate('(img)=>img.complete&&img.naturalWidth>0') if entry.get('reference') else True
            assert reference_ok,entry['id']+' reference missing'
            if not state['clips']:
                assert page.locator('#animation-select').is_disabled()
                assert '모션 준비 중' in page.locator('#animation-select').inner_text()
            else:
                assert page.locator('#animation-select').is_enabled()
                before=page.evaluate('window.faithfulGallery.state.animationTime')
                page.wait_for_function('(before)=>window.faithfulGallery.state.animationTime!==before',arg=before,timeout=15000)
                after=page.evaluate('window.faithfulGallery.state.animationTime')
                assert before!=after,entry['id']+' animation clock did not advance'
                page.locator('#animation-play').click()
                page.wait_for_function('window.faithfulGallery.state.animationPaused===true',timeout=15000)
                paused=page.evaluate('window.faithfulGallery.state.animationTime')
                page.wait_for_timeout(250)
                assert page.evaluate('window.faithfulGallery.state.animationTime')==paused,entry['id']+' pause failed'
                page.locator('#animation-play').click()
                if len(state['clips'])>1:
                    page.locator('#animation-select').select_option('1')
                    assert page.evaluate('window.faithfulGallery.state.activeClip')==state['clips'][1]
                    page.locator('#animation-select').select_option('0')
            page.screenshot(path=str(OUTPUT/(entry['id']+'.png')))
            results.append({'id':entry['id'],'meshes':state['meshCount'],'triangles':state['triangles'],'clips':state['clips'],
                           'reference':reference_ok,'bounds':state['bounds']})
            print('BROWSER MODEL PASS',entry['id'],state['meshCount'],round(state['triangles']),flush=True)
        page.locator('#front-view').click();page.locator('#side-view').click();page.locator('#back-view').click()
        page.locator('#reset-camera').click()
        page.locator('#wireframe').check();page.wait_for_timeout(100);page.locator('#wireframe').uncheck()
        page.locator('#auto-rotate').check();page.wait_for_timeout(100);page.locator('#auto-rotate').uncheck()
        page.locator('#search').fill(entries[0]['id'])
        assert page.locator('.model-item').count()>=1
        page.locator('#search').fill('this-character-does-not-exist')
        assert page.locator('#list-empty').is_visible()
        page.locator('#search').fill('')
        with page.expect_download() as event:
            page.locator('#screenshot').click()
        download=event.value;download.save_as(str(OUTPUT/'exported-model.png'))
        browser.close()
    report={'passed':not errors and not external,'fixture_manifest':fixture,'models':results,
            'runtime_errors':errors,'external_runtime_requests':external,
            'checks':['real WebGL mesh rendering','official reference loading','no-clip status','camera view controls',
                      'animation time advances','animation pause/resume and clip switching',
                      'wireframe control','auto rotate','search empty/results','PNG download','offline-only runtime requests']}
    (OUTPUT/'browser-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    assert report['passed'],report
    print('FAITHFUL GALLERY BROWSER PASS',len(results),'MODELS',flush=True)

if __name__=='__main__':
    main()
