"""Exercise the published ten-motion showcase through its visible controls."""
import sys,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT.parent/'tmp/gallery-test-tools-py312'))
import greenlet
sys.path.insert(0,str(ROOT.parent/'tmp/gallery-test-tools'))
from playwright.sync_api import sync_playwright

def main():
    errors=[]
    out=ROOT/'Builds/FaithfulCombatValidation';out.mkdir(exist_ok=True)
    with sync_playwright() as pw:
        browser=pw.chromium.launch(executable_path='C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless=True,
            args=['--use-angle=swiftshader','--enable-unsafe-swiftshader'])
        page=browser.new_page(viewport={'width':1440,'height':950})
        page.on('pageerror',lambda e:errors.append(str(e)))
        page.goto('http://127.0.0.1:8766/?demo=1#agumon',wait_until='networkidle',timeout=90000)
        page.wait_for_function('window.faithfulGallery?.state.ready&&faithfulGallery.state.demo.active',timeout=90000)
        demo=page.evaluate('faithfulGallery.state.demo')
        assert len(demo['ids'])==len(set(demo['ids']))==34
        assert page.locator('#motion-buttons button').count()==10
        assert page.locator('#demo-toggle').get_attribute('aria-pressed')=='true'
        page.wait_for_function('faithfulGallery.state.activeClip==="Walk"',timeout=30000)
        page.locator('#animation-play').click()
        before=page.evaluate('faithfulGallery.state.demo.elapsed')
        page.wait_for_timeout(350)
        assert page.evaluate('faithfulGallery.state.demo.elapsed')==before,'Paused showcase timer advanced'
        page.locator('#animation-play').click()
        page.wait_for_function('(t)=>faithfulGallery.state.demo.elapsed>t',arg=before)
        before_next=page.evaluate('({selected:faithfulGallery.state.selected,demo:faithfulGallery.state.demo,errors:faithfulGallery.state.errors})')
        print('BEFORE NEXT',json.dumps(before_next),flush=True)
        page.locator('#demo-next').click()
        try:
            page.wait_for_function('(id)=>faithfulGallery.state.ready&&faithfulGallery.state.selected===id',arg=demo['ids'][1],timeout=30000)
        except Exception:
            print('DEMO DIAGNOSTIC',page.evaluate('({selected:faithfulGallery.state.selected,ready:faithfulGallery.state.ready,demo:faithfulGallery.state.demo,errors:faithfulGallery.state.errors})'),errors,flush=True)
            page.screenshot(path=str(out/'demo-failure.png'))
            raise
        assert page.evaluate('faithfulGallery.state.demo.active')
        page.screenshot(path=str(out/'demo-playing.png'))
        page.locator('[data-motion="Skill"]').click()
        assert not page.evaluate('faithfulGallery.state.demo.active')
        assert page.evaluate('faithfulGallery.state.activeClip')=='Skill'
        assert page.locator('[data-motion="Skill"]').get_attribute('aria-pressed')=='true'
        page.locator('#demo-toggle').click()
        page.locator('.model-item[data-id="kuwagamon"]').click()
        page.wait_for_function('faithfulGallery.state.ready&&faithfulGallery.state.selected==="kuwagamon"',timeout=90000)
        assert not page.evaluate('faithfulGallery.state.demo.active')
        assert page.locator('#motion-buttons button').count()==10
        page.locator('[data-motion="Guard"]').click()
        page.evaluate('faithfulGallery.seekMotion(.7)')
        page.wait_for_timeout(120)
        page.screenshot(path=str(out/'kuwagamon-guard.png'))
        page.set_viewport_size({'width':780,'height':980})
        page.wait_for_timeout(200)
        assert page.evaluate('document.documentElement.scrollWidth<=window.innerWidth'),'Narrow viewport overflow'
        assert page.locator('#demo-toggle').is_visible()
        page.screenshot(path=str(out/'narrow-gallery.png'),full_page=True)
        browser.close()
    report={'passed':not errors,'species':34,'common_motions':10,'errors':errors,
        'checks':['demo URL starts playback','all 34 unique species included','timed transition to walking','pause freezes demo timer','resume advances timer',
            'next character keeps demo running','manual motion selection stops demo','manual character selection stops demo','ten quick motion buttons','narrow viewport has no horizontal overflow']}
    (out/'demo-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    assert report['passed'],report
    print('SHOWCASE CONTROLS PASS')

if __name__=='__main__':main()
