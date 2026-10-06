"""Compare the exact shoulder sculpt against its pre-edit model in WebGL."""
import json,sys,hashlib,argparse
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT.parent/f'tmp/gallery-test-tools-py{sys.version_info.major}{sys.version_info.minor}'))
import greenlet
from PIL import Image,ImageDraw,ImageFont
sys.path.insert(0,str(ROOT.parent/'tmp/gallery-test-tools'))
from playwright.sync_api import sync_playwright

out=ROOT/'Builds/MetalGreymonShoulder-20261006';out.mkdir(exist_ok=True)
paths={'before':ROOT/'ArtSource/MetalGreymonShoulderBackup-20261006/gallery/model.glb',
       'after':ROOT/'ArtSource/AnimatedReview/metalgreymon/model.glb'}
variant='before';errors=[];poses=[]
parser=argparse.ArgumentParser();parser.add_argument('--published',action='store_true');args=parser.parse_args()
shots=[('Idle',0,'front'),('Idle',0,'three-quarter'),('Attack',.50,'three-quarter'),('Skill',.48,'front'),('Down',1,'three-quarter')]
with sync_playwright() as pw:
    browser=pw.chromium.launch(executable_path='C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',
        headless=True,args=['--use-angle=swiftshader','--enable-unsafe-swiftshader'])
    page=browser.new_page(viewport={'width':1440,'height':950})
    page.on('pageerror',lambda e:errors.append(str(e)))
    page.on('console',lambda m:errors.append(m.text) if m.type=='error' else None)
    if not args.published:
        page.route('**/FaithfulGallery/metalgreymon/model.glb',lambda r:r.fulfill(path=paths[variant],content_type='model/gltf-binary'))
    page.goto('http://127.0.0.1:8766/#metalgreymon',wait_until='networkidle',timeout=90000)
    page.wait_for_function('window.faithfulGallery?.state.ready',timeout=90000)
    page.locator('#technique-effects').uncheck()
    if args.published:
        for relative,expected in [('metalgreymon/model.glb',paths['after']),('previous/metalgreymon.glb',paths['before'])]:
            response=page.request.get('http://127.0.0.1:8766/ArtSource/FaithfulGallery/'+relative)
            assert response.ok and hashlib.sha256(response.body()).digest()==hashlib.sha256(expected.read_bytes()).digest()
    for variant in paths:
        page.evaluate('faithfulGallery.selectModel("metalgreymon")')
        page.wait_for_function('faithfulGallery.state.ready&&faithfulGallery.state.selected==="metalgreymon"')
        if args.published and page.evaluate('faithfulGallery.state.showPrevious')!=(variant=='before'):
            page.locator('#compare-before').click()
            page.wait_for_function('(before)=>faithfulGallery.state.ready&&faithfulGallery.state.showPrevious===before',arg=variant=='before')
        for clip,u,view in shots:
            page.evaluate('''([clip,u,view])=>{const g=faithfulGallery;
              g.playClip(g.state.clips.indexOf(clip),false);g.seekMotion(g.state.animationDuration*u);g.frame(view);
            }''',[clip,u,view])
            frame=page.evaluate('faithfulGallery.state.frames')
            page.wait_for_function('(f)=>faithfulGallery.state.frames>f+1',arg=frame)
            snapshot=page.evaluate('faithfulGallery.poseSnapshot()')
            assert snapshot['bones']>0 and snapshot['positions']
            page.screenshot(path=str(out/f'{variant}-{clip}-{view}.png'))
            poses.append(dict(variant=variant,clip=clip,view=view,positions=snapshot['positions']))
    browser.close()
assert not errors,errors
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',20)
sheet=Image.new('RGB',(1200,452*len(shots)),'#f6f8fa');draw=ImageDraw.Draw(sheet)
for row,(clip,u,view) in enumerate(shots):
    for column,variant in enumerate(paths):
        tile=Image.open(out/f'{variant}-{clip}-{view}.png').convert('RGB').crop((218,82,1159,757)).resize((586,420),Image.Resampling.LANCZOS)
        x,y=column*600+7,row*452
        draw.text((x,y+2),('수정 전' if column==0 else '수정 후')+' · '+clip+' · '+view,font=font,fill='#264854')
        sheet.paste(tile,(x,y+28))
sheet.save(out/'shoulder-before-after.jpg',quality=94)
report=dict(passed=True,errors=errors,sha256={v:hashlib.sha256(p.read_bytes()).hexdigest() for v,p in paths.items()},poses=poses)
report['published']=args.published
(out/'comparison-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('SHOULDER COMPARISON PASS',len(shots),'poses',flush=True)
