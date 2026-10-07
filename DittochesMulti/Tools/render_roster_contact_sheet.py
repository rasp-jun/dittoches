"""Create a labeled overview from the reviewed local model previews."""
import json,sys,base64,html
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT.parent/'tmp/gallery-test-tools-py312'))
import greenlet
sys.path.insert(0,str(ROOT.parent/'tmp/gallery-test-tools'))
from playwright.sync_api import sync_playwright
gallery=ROOT/'ArtSource/FaithfulGallery'
manifest=json.loads((gallery/'manifest.json').read_text(encoding='utf-8'))
clip_count=sum(len(entry['animations']) for entry in manifest['entries'])
cards=[]
for entry in manifest['entries']:
    data=base64.b64encode((gallery/entry['preview']).read_bytes()).decode()
    cards.append('<figure><img src="data:image/png;base64,'+data+'"><figcaption>'+html.escape(entry['name'])+'</figcaption></figure>')
document='''<!doctype html><meta charset="utf-8"><style>
*{box-sizing:border-box}body{margin:0;padding:24px;background:#1b222a;color:white;font-family:"Malgun Gothic",sans-serif}
h1{margin:0;font-size:30px}p{color:#bad0d7;margin:10px 0 26px;font-size:17px}
main{display:grid;grid-template-columns:repeat(7,1fr);gap:16px 12px}figure{margin:0;border-radius:8px;overflow:hidden;background:#26323d}
img{width:100%;height:204px;object-fit:contain;background:#778089}figcaption{padding:12px;text-align:center;font-size:17px}
</style><h1>디지몬 3D · 전체 34종</h1><p>모든 종에 공통 10동작 · 총 '''+str(clip_count)+'''개 모션 · 2026.10.02</p><main>'''+''.join(cards)+'</main>'
out=ROOT/'Builds/FaithfulFullRosterValidation';out.mkdir(exist_ok=True)
with sync_playwright() as pw:
    browser=pw.chromium.launch(executable_path='C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless=True)
    page=browser.new_page(viewport={'width':1680,'height':1440},device_scale_factor=1)
    page.set_content(document,wait_until='load')
    page.screenshot(path=str(out/'all-34-species.png'),full_page=True)
    browser.close()
print(out/'all-34-species.png')
