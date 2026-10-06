"""Audit the expanded published catalog and refresh material-only thumbnails."""
import sys,json,hashlib,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT.parent/'tmp/gallery-test-tools-py312'))
import greenlet
sys.path.insert(0,str(ROOT.parent/'tmp/gallery-test-tools'))
from playwright.sync_api import sync_playwright

def main():
    gallery=ROOT/'ArtSource/FaithfulGallery'
    manifest=json.loads((gallery/'manifest.json').read_text(encoding='utf-8'))
    report=json.loads((ROOT/'Builds/FaithfulMotionValidation/published-report.json').read_text(encoding='utf-8'))
    assert report['passed'] and len(manifest['entries'])==manifest['scope']==34 and not manifest['pending']
    verified={r['id']:r for r in report['models']}
    for entry in manifest['entries']:
        digest=hashlib.sha256((gallery/entry['model']).read_bytes()).hexdigest()
        assert digest==entry['sha256']==verified[entry['id']]['sha256']
    out=ROOT/'Builds/FaithfulFullRosterValidation';out.mkdir(exist_ok=True)
    errors=[]
    with sync_playwright() as pw:
        browser=pw.chromium.launch(executable_path='C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless=True,args=['--use-angle=swiftshader','--enable-unsafe-swiftshader'])
        page=browser.new_page(viewport={'width':1440,'height':950},device_scale_factor=1)
        page.on('pageerror',lambda e:errors.append(str(e)))
        page.goto('http://127.0.0.1:8766/',wait_until='networkidle')
        page.wait_for_function('window.faithfulGallery?.state.ready',timeout=90000)
        assert page.locator('.model-item').count()==34 and page.locator('.pending-item').count()==0
        assert '34' in page.locator('#total-label').inner_text()
        page.locator('#search').fill('로제몬');assert page.locator('.model-item').count()==1
        page.locator('.model-item').click();page.wait_for_function('faithfulGallery.state.ready&&faithfulGallery.state.selected==="rosemon"')
        assert page.locator('#reference-image').evaluate('(e)=>e.complete&&e.naturalWidth>0')
        page.locator('#search').fill('');page.screenshot(path=str(out/'rosemon-published.png'))
        for ident in ['birdramon','kuwagamon','angemon']:
            page.evaluate('(i)=>faithfulGallery.selectModel(i)',ident)
            page.wait_for_function('(i)=>faithfulGallery.state.ready&&faithfulGallery.state.selected===i',arg=ident)
            page.evaluate('''()=>{const i=Math.max(0,faithfulGallery.state.clips.findIndex(n=>n.toLowerCase()==="idle"));
                faithfulGallery.playClip(i,false);faithfulGallery.seekMotion(0);faithfulGallery.frame("three-quarter");}''')
            page.wait_for_timeout(120)
            page.locator('.studio-heading').evaluate('(e)=>e.style.visibility="hidden"')
            page.locator('#canvas').screenshot(path=str(gallery/ident/'preview.png'))
            page.locator('.studio-heading').evaluate('(e)=>e.style.visibility=""')
            shutil.copy2(gallery/ident/'preview.png',ROOT/'ArtSource/AnimatedReview'/ident/'preview.png')
        browser.close()
    assert not errors,errors
    (out/'catalog-report.json').write_text(json.dumps({'passed':True,'species':34,'clips':report['all_clips'],'pending':0,
        'checks':['Every published model SHA matches its browser-verified candidate','34 selectable models and no pending placeholder','New Korean-name search, selection and official reference','Updated material thumbnails captured from actual WebGL'],'errors':errors},ensure_ascii=False,indent=2),encoding='utf-8')
    print('FULL CATALOG PASS 34 species /',report['all_clips'],'clips / 0 pending')
if __name__=='__main__':main()
