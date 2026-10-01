"""Build a local, searchable reference/before/after review from actual renders."""
import json
from pathlib import Path
from roster_designs import DESIGNS

root=Path(__file__).resolve().parent.parent
out=root/'ArtSource/Roster/QualityReview';out.mkdir(parents=True,exist_ok=True)
notes={}
for file in (root/'Tools').glob('roster_*_quality_notes.json'):
    value=json.loads(file.read_text(encoding='utf-8'))
    entries=value.get('characters',{})
    if isinstance(entries,dict):notes.update(entries)
rows=[]
for ident,label,kind,*_ in DESIGNS:
    before='../../QualityBackup-20261001/ArtSource/Roster/'+ident+'/preview.png'
    after='../'+ident+'/preview.png'
    if ident=='agumon':
        before='../../QualityBackup-20261001/ArtSource/Agumon/quality-before.png'
        after='../../Agumon/quality-review.png'
    rows.append(dict(id=ident,name=label,kind=kind,reference='../ReferencesV2/'+ident+'.jpg',before=before,after=after))
html='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>디지몬 34종 · 원작 비교</title><style>
*{box-sizing:border-box}body{margin:0;background:#121821;color:#edf1f5;font:15px/1.6 system-ui,sans-serif}
main{max-width:1440px;margin:auto;padding:32px}h1{font-size:27px;margin:0 0 8px}p{color:#afbdce;margin:6px 0 22px}
input{width:100%;max-width:430px;background:#202b39;color:#fff;border:1px solid #4c6176;border-radius:8px;padding:12px;font:inherit}
nav{display:flex;flex-wrap:wrap;gap:7px;margin:22px 0}button{background:#222e3e;border:1px solid #45586b;color:#dce5ef;padding:8px 13px;border-radius:7px;cursor:pointer;font:inherit}
button.active{background:#397b72;border-color:#70c5b1;color:white}.images{display:grid;grid-template-columns:repeat(3,1fr);gap:16px}
figure{margin:0;background:#1e2835;border-radius:12px;overflow:hidden}figure img{width:100%;aspect-ratio:1;object-fit:contain;display:block;background:#69717d}figure:first-child img{background:white}
figcaption{padding:10px 14px;color:#cfdae8}.motions{display:grid;grid-template-columns:repeat(4,1fr);gap:10px;margin-top:24px}.motions img{width:100%;aspect-ratio:1.88;object-fit:contain}.motions figcaption{font-size:13px}
h2{font-size:23px}.note{margin-top:25px;border-top:1px solid #354455;padding-top:18px;font-size:13px;color:#99a9bb}
@media(max-width:800px){main{padding:16px}.images{grid-template-columns:1fr}.motions{grid-template-columns:repeat(2,1fr)}}
</style><main><h1>34종 원작 비교</h1><p>공식 기본형 참고 이미지와 실제 Blender 수정 전·후 렌더입니다. 아래 네 장은 최신 실행 프로그램에서 캡처한 모션입니다.</p>
<input id="search" placeholder="디지몬 이름 검색" aria-label="디지몬 검색"><nav id="list"></nav><h2 id="name"></h2>
<section class="images"><figure><img id="reference" alt="공식 원작 참고"><figcaption>공식 원작 참고</figcaption></figure><figure><img id="before" alt="수정 전 3D"><figcaption>수정 전 3D</figcaption></figure><figure><img id="after" alt="수정 후 3D"><figcaption>수정 후 3D · 실제 렌더</figcaption></figure></section>
<section class="motions" id="motions"></section><p class="note">이번 수정은 34종의 비율·실루엣·얼굴·의상·장식과 표면 음영을 보강한 단계입니다. 전문 수작업 조형 수준의 표면 디테일, 종별 원작 기술 모션, 정식 Unity 셰이더 검증은 남아 있습니다. 원작 이미지는 비교 참고용이며 모델에 붙인 텍스처가 아닙니다.</p></main>
<script>const rows=__DATA__;let active='agumon';const list=document.getElementById('list');
function choose(row){active=row.id;document.getElementById('name').textContent=row.name+' · '+row.id;for(const key of ['reference','before','after'])document.getElementById(key).src=row[key];
const area=document.getElementById('motions');area.replaceChildren();for(const [clip,label] of [['Idle','대기'],['Walk','걷기'],['Attack','공격'],['PepperBreath','고유 기술']]){const figure=document.createElement('figure');const img=document.createElement('img');img.src='../../../Builds/PortablePreview/RosterCaptures/'+row.id+'-'+clip+'.png';img.alt=row.name+' '+label;figure.append(img);const caption=document.createElement('figcaption');caption.textContent=label;figure.append(caption);area.append(figure)}render()}
function render(){const search=document.getElementById('search').value.trim().toLowerCase();list.replaceChildren();for(const row of rows.filter(x=>(x.name+' '+x.id).toLowerCase().includes(search))){const button=document.createElement('button');button.textContent=row.name;button.className=row.id===active?'active':'';button.onclick=()=>choose(row);list.append(button)}}
document.getElementById('search').oninput=render;choose(rows.find(x=>x.id===active));</script></html>'''
(out/'index.html').write_text(html.replace('__DATA__',json.dumps(rows,ensure_ascii=False)),encoding='utf-8')
(out/'review-manifest.json').write_text(json.dumps({'characters':rows,'notes':notes},ensure_ascii=False,indent=2),encoding='utf-8')
print('ROSTER REVIEW:',out/'index.html')
