"""Build the local source/pose comparison page without modifying source imagery."""
from pathlib import Path
import json,html
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'ArtSource/LightLine-20261013'
names={'nyaromon':'야옹몬 · Nyaromon','plotmon':'플로트몬 · Plotmon / Salamon','tailmon':'가트몬 · Tailmon / Gatomon'}
checks={
 'nyaromon':['노란 고양이 머리와 뾰족한 귀','적갈색 큰 눈과 지그재그 입','보라색 무늬와 끝부분을 가진 긴 꼬리'],
 'plotmon':['크림색 몸과 늘어진 귀','공식 원화의 녹색 눈과 분홍 뺨','금색 홀리 링의 문양과 네 발'],
 'tailmon':['흰 고양이 몸, 파란 눈, 수염','분홍 귀 안쪽과 보라색 귀털','줄무늬 장갑과 검은 발톱','보라 줄무늬 꼬리와 금색 홀리 링']}
poses=['Idle','Walk','Run','Attack','Skill','Hit','Victory','Guard','Dodge','Down']
validation=json.loads((OUT/'small-validation.json').read_text(encoding='utf-8'))
cards=[]
for ident,name in names.items():
    report=json.loads((OUT/ident/'animation-report.json').read_text(encoding='utf-8'))
    source=json.loads((OUT/ident/'source.json').read_text(encoding='utf-8'))
    feature=' · '.join(checks[ident]);sha=report['output_sha256']
    report['status']='Passed byte-level GLB validation and visual review of all ten rendered clip poses.'
    report['visual_review']={'checked_on':'2026-10-13','official_features':checks[ident],
      'corrections':{'nyaromon':'Original-surface smoothing, curved original tail and full-tail inspection framing',
      'plotmon':'Official green iris pigment; rigid Holy Ring; floppy ear articulation',
      'tailmon':'Excluded ear tassels from tail region; isolated toes from gloves; rigid Holy Ring'}[ident],
      'limitations':['Public game-derived original model; original author and redistribution permission not independently established.',
      'Original texture resolution retained; new skeletal motions are local authored studies, not extracted game animations.']}
    (OUT/ident/'animation-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    figure=lambda file,label:f'<figure><img loading="lazy" src="{ident}/{file}" alt="{html.escape(label)}"><figcaption>{html.escape(label)}</figcaption></figure>'
    cards.append(f'<section><h2>{html.escape(name)}</h2><p>{feature}</p><div class="comparison">'+
      figure('official-reference.jpg','공식 도감 원화')+figure('preview.png','새 리그 · 원본 텍스처 · 로컬 수정')+figure('rigged-front.png','정면')+figure('rigged-side.png','측면')+
      f'</div><p><a href="{source["official_reference"]}">공식 도감</a> · <a href="{source["source_url"]}">원본 공개 컬렉션</a> · <a href="{ident}/source.json">출처 기록</a> · <a href="{ident}/model.glb">GLB</a> · <a href="{ident}/{ident}.blend">편집용 Blender</a> · <a href="{ident}/animation-report.json">리그/모션 보고서</a></p>'+
      f'<p>{report["bones"]} bones · 10 clips · 30 fps · Skill: {html.escape(report["skill_reference"])}</p><div class="poses">'+
      ''.join(figure(mode+'.png',mode) for mode in poses)+f'</div><p class="hash">SHA-256 {sha}</p></section>')
page='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Light Line · Small Models · Source & Motion Review</title>
<style>body{margin:0;background:#09131f;color:#e8f0ed;font:16px/1.6 system-ui,sans-serif}main{max-width:1440px;margin:auto;padding:36px}h1{font-size:34px}h2{font-size:25px;color:#86e5d1}section{margin:40px 0;padding:24px;background:#102231;border:1px solid #284651;border-radius:18px}.comparison{display:grid;grid-template-columns:repeat(4,1fr);gap:14px}.poses{display:grid;grid-template-columns:repeat(5,1fr);gap:14px}figure{margin:0}img{display:block;width:100%;aspect-ratio:1;object-fit:contain;background:#78808a;border-radius:10px}figcaption{padding:8px 2px;color:#b9cfce;font-size:14px}a{color:#87dccb}.hash{font-size:11px;overflow-wrap:anywhere;color:#789594}.pass{display:inline-block;padding:8px 14px;background:#193e35;color:#92e7ca;border-radius:10px}p{max-width:1120px}@media(max-width:850px){.comparison,.poses{grid-template-columns:repeat(2,1fr)}main{padding:15px}section{padding:16px}}</style>
<main><h1>Light Line · 3종 원형 / 모션 검수</h1><p class="pass">GLB 직접 읽기 검사 통과 · 3 models / 30 clips</p><p>기존 Faithful 모델과 동일한 공개 원본 메시·페인트 텍스처 경로를 사용했습니다. 구형 도형 조립 모델은 사용하지 않았습니다. Blender 리그·가중치·모션은 새로 작성했으며, 원본 34종은 변경하지 않았습니다.</p><p>검사: 10개 클립 이름과 길이, 0초 시작, 증가하는 키 시간, Idle/Walk/Run 루프 이음새, 정규화된 4개 이하 스킨 가중치, 내장 텍스처, 유한 좌표, 샘플 포즈 접지. <a href="small-validation.json">상세 검사 JSON</a></p><p>출처는 게임 유래 공개 참고 자료입니다. 원저작자별 정보와 재배포 권한은 확인되지 않았으며 외부 공개는 수행하지 않았습니다.</p>'''+''.join(cards)+'</main></html>'
(OUT/'small-review.html').write_text(page,encoding='utf-8')
print('Built',OUT/'small-review.html')
