"""Check published before/after models at a paused pose in real WebGL."""
import json,sys,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT.parent/'tmp/gallery-test-tools'))
from playwright.sync_api import sync_playwright


def main():
    out=ROOT/'Builds/FaithfulNaturalValidation';out.mkdir(exist_ok=True)
    errors=[];checks=[]
    with sync_playwright() as pw:
        browser=pw.chromium.launch(executable_path='C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless=True,
            args=['--use-angle=swiftshader','--enable-unsafe-swiftshader'])
        page=browser.new_page(viewport={'width':1440,'height':950})
        page.on('pageerror',lambda e:errors.append(str(e)))
        page.goto('http://127.0.0.1:8766/?motion=Walk#agumon',wait_until='networkidle',timeout=90000)
        page.wait_for_function('faithfulGallery.state.ready',timeout=90000)
        assert page.evaluate('faithfulGallery.state.activeClip')=='Walk'
        for ident in ['agumon','wargreymon','holyangemon']:
            page.evaluate('(id)=>faithfulGallery.selectModel(id)',ident)
            page.wait_for_function('(id)=>faithfulGallery.state.ready&&faithfulGallery.state.selected===id',arg=ident,timeout=90000)
            page.evaluate('faithfulGallery.seekMotion(.35)')
            after=page.evaluate('faithfulGallery.poseSnapshot().positions')
            page.screenshot(path=str(out/(ident+'-after.png')))
            page.locator('#compare-before').click()
            page.wait_for_function('faithfulGallery.state.ready&&faithfulGallery.state.showPrevious===true',timeout=90000)
            page.wait_for_function('faithfulGallery.state.animationPaused===true',timeout=90000)
            assert page.evaluate('faithfulGallery.state.activeClip')=='Walk'
            before=page.evaluate('faithfulGallery.poseSnapshot().positions')
            assert len(before)==len(after)
            difference=max(abs(a-b) for a,b in zip(after,before))
            assert difference>.001,(ident,'same model loaded for both views')
            assert abs(page.evaluate('faithfulGallery.state.animationTime/faithfulGallery.state.animationDuration')-.35)<.0001
            page.screenshot(path=str(out/(ident+'-before.png')))
            page.locator('#compare-before').click()
            page.wait_for_function('faithfulGallery.state.ready&&faithfulGallery.state.showPrevious===false',timeout=90000)
            page.wait_for_function('faithfulGallery.state.animationPaused===true',timeout=90000)
            restored=page.evaluate('faithfulGallery.poseSnapshot().positions')
            assert max(abs(a-b) for a,b in zip(after,restored))<.0001,(ident,'comparison changed paused pose')
            checks.append({'id':ident,'sampled_pose_difference':difference,'restored_pose':True})
        page.evaluate('faithfulGallery.selectModel("birdramon")')
        page.wait_for_function('faithfulGallery.state.ready&&faithfulGallery.state.selected==="birdramon"',timeout=90000)
        assert page.locator('#compare-before').is_hidden()
        assert len(page.evaluate('faithfulGallery.state.clips'))==11
        browser.close()
    report={'passed':not errors,'comparisons':checks,'errors':errors,'checks':['real previous and updated assets differ','paused clip and normalized time preserved','current pose restored exactly','unchanged original clips remain available']}
    (out/'comparison-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    assert report['passed'],report
    print('PUBLISHED BEFORE/AFTER PASS',len(checks))

if __name__=='__main__':main()
