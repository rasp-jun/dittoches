"""Index inspected source meshes; do not count unavailable species as finished."""
from pathlib import Path
import json
import struct
import hashlib
from roster_designs import DESIGNS, OFFICIAL

ROOT=Path(__file__).resolve().parent.parent
OUT=ROOT/'ArtSource/FaithfulGallery'


def glb_info(path):
    data=path.read_bytes()
    magic,version,total=struct.unpack_from('<III',data)
    assert (magic,version,total)==(0x46546c67,2,len(data)),path
    length,kind=struct.unpack_from('<II',data,12)
    assert kind==0x4e4f534a
    doc=json.loads(data[20:20+length])
    # All dependencies must be embedded: the gallery works offline.
    assert all('uri' not in b or b['uri'].startswith('data:') for b in doc.get('buffers',[]))
    assert all('uri' not in b or b['uri'].startswith('data:') for b in doc.get('images',[]))
    triangles=0
    for mesh in doc['meshes']:
        for p in mesh['primitives']:
            if p.get('mode',4)==4:
                triangles+=doc['accessors'][p['indices']]['count']//3 if 'indices' in p else doc['accessors'][p['attributes']['POSITION']]['count']//3
    return dict(triangles=triangles,bytes=len(data),sha256=hashlib.sha256(data).hexdigest(),
                materials=len(doc.get('materials',[])),images=len(doc.get('images',[])),
                skins=len(doc.get('skins',[])),animations=[a.get('name','Motion '+str(i+1)) for i,a in enumerate(doc.get('animations',[]))])


def main():
    entries=[];pending=[]
    for id,name,*_ in DESIGNS:
        folder=OUT/id
        if not (folder/'model.glb').exists() or not (folder/'source.json').exists():
            pending.append({'id':id,'name':name,'status':'원작 외형에 맞는 모델 준비 중'})
            continue
        source=json.loads((folder/'source.json').read_text(encoding='utf-8-sig'))
        info=glb_info(folder/'model.glb')
        license=source.get('license','출처 확인 중')
        if isinstance(license,dict):license=license.get('label',license.get('fullName',''))
        url=source.get('source_url',source.get('source',source.get('url','')))
        if not isinstance(url,str): url=source.get('model_page','')
        notes='공개 외부 모델 · 원작 외형 비교 중'
        animation_report=folder/'animation-report.json'
        authored=json.loads(animation_report.read_text(encoding='utf-8')) if animation_report.exists() else None
        if authored:notes+=' · 대기·걷기·달리기·공격·피격·승리 6개 동작 추가'
        if id=='agumon':notes+=' · 머리 비율과 팔 자세 조정'
        if not info['animations']:notes+=' · 모션 준비 중'
        entry=dict(id=id,name=name,model=id+'/model.glb',preview=id+'/preview.png',
                            reference='../Roster/ReferencesV2/'+id+'.jpg',
                            referenceSource='https://digimon.net/reference/detail.php?directory_name='+OFFICIAL.get(id,id),
                            author=source.get('author',source.get('creator','')),license=license,source=url,
                            notes=notes,**info)
        camera={'tsunomon':([3,.9,1],90),'pyocomon':([-3,.9,1.3],-90)}
        if authored:
            entry['motionClips']=authored['clips']
            entry['motionOrigin']='새로 제작한 동작 · 원본 게임 모션과 다를 수 있음'
        if (OUT/'previous'/(id+'.glb')).exists():entry['previousModel']='previous/'+id+'.glb'
        if id in camera and not authored:
            entry['cameraDirection'],entry['frontAngle']=camera[id]
        entries.append(entry)
    # The default view starts with the clearest finished silhouette, not an empty T-pose.
    order=['wargreymon','agumon','greymon','garurumon','kabuterimon','atlur']
    entries.sort(key=lambda e: order.index(e['id']) if e['id'] in order else len(order))
    result={'title':'디지몬 원작 외형 비교','status':'모션·외형 검토본 · 게임 적용 전',
            'entries':entries,'pending':pending,'scope':len(DESIGNS),'prepared':len(entries)}
    OUT.mkdir(parents=True,exist_ok=True)
    (OUT/'manifest.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    (OUT/'validation.json').write_text(json.dumps({'status':'passed','models':len(entries),'remaining':len(pending),
        'checks':['GLB header and length','Embedded buffers and textures','Attribution record present'],
        'limitations':['Structure checks do not certify original likeness or motion quality.'],
        'assets':[{k:e[k] for k in ['id','sha256','triangles','images','skins','animations']} for e in entries]},
        ensure_ascii=False,indent=2),encoding='utf-8')
    print('Prepared',len(entries),'of',len(DESIGNS),'species;',len(pending),'pending')


if __name__=='__main__':main()
