"""Capture the actual old/new Greymon skins with matching camera and phase."""
import io,json,sys,argparse
from pathlib import Path
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT.parent/'tmp/gallery-test-tools'))
from playwright.sync_api import sync_playwright

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--motion',default='Attack');args=parser.parse_args();motion=args.motion
    out=ROOT/'Builds/FaithfulNaturalValidation';out.mkdir(exist_ok=True)
    with sync_playwright() as pw:
        browser=pw.chromium.launch(executable_path='C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless=True,
            args=['--use-angle=swiftshader','--enable-unsafe-swiftshader'])
        page=browser.new_page(viewport={'width':600,'height':600})
        page.goto('http://127.0.0.1:8766/#greymon',wait_until='networkidle',timeout=90000)
        page.wait_for_function('window.faithfulGallery?.state.ready&&faithfulGallery.state.selected==="greymon"',timeout=90000)
        page.add_style_tag(content='''.topbar,.roster,.comparison,.studio-heading,.view-tools,.demo-bar,.motion-buttons,.animation-bar,.motion-timeline{display:none!important}
            .layout{display:block;height:600px;min-height:0}.studio{height:600px;min-height:0}#viewport{height:600px;min-height:600px}''')
        captures={}
        for previous in [False,True]:
            if previous:
                page.evaluate('document.querySelector("#compare-before").click()')
                page.wait_for_function('faithfulGallery.state.ready&&faithfulGallery.state.showPrevious',timeout=90000)
            for mode in ['Idle',motion]:
                page.evaluate('(n)=>{faithfulGallery.playClip(faithfulGallery.state.clips.indexOf(n),false);faithfulGallery.seekMotion(0);faithfulGallery.frame(n==="Idle"?"front":"three-quarter");}',mode)
                duration=page.evaluate('faithfulGallery.state.animationDuration')
                frames=[]
                for i in range(30 if mode==motion else 1):
                    page.evaluate('(t)=>faithfulGallery.seekMotion(t)',duration*i/30)
                    page.wait_for_timeout(45)
                    frames.append(Image.open(io.BytesIO(page.locator('#canvas').screenshot())).convert('RGB'))
                captures[(previous,mode)]=frames
        browser.close()
    def pair(before,after,mode):
        sheet=Image.new('RGB',(1200,654),'#e9edef');draw=ImageDraw.Draw(sheet)
        draw.text((25,16),'BEFORE / '+mode,fill='#647785',font_size=24)
        draw.text((625,16),'UPDATED / '+mode,fill='#264b56',font_size=24)
        sheet.paste(before,(0,54));sheet.paste(after,(600,54));return sheet
    pair(captures[(True,'Idle')][0],captures[(False,'Idle')][0],'GREYMON').save(out/'greymon-stance-before-after.png')
    frames=[pair(a,b,motion.upper()) for a,b in zip(captures[(True,motion)],captures[(False,motion)])]
    frames[0].save(out/('greymon-'+motion.lower()+'-before-after.gif'),save_all=True,append_images=frames[1:],duration=50,loop=0,optimize=False)
    print('GREYMON STANCE AND',motion,'COMPARISON CAPTURED')

if __name__=='__main__':main()
