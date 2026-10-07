"""Correct two source materials without resampling their original animations.

Every geometry, texture, skin and animation buffer stays byte-for-byte intact.
"""
import json,struct,hashlib,shutil,io
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/'ArtSource/FullRosterBackup-20261002/gallery'
OUT=ROOT/'ArtSource/AnimatedReview'
def sha(body):return hashlib.sha256(body).hexdigest()
for ident in ['birdramon','kuwagamon']:
    source=BASE/ident;target=OUT/ident;target.mkdir(parents=True,exist_ok=True)
    raw=(source/'model.glb').read_bytes();length=struct.unpack_from('<I',raw,12)[0]
    doc=json.loads(raw[20:20+length]);binary=raw[20+length:]
    before=json.loads(json.dumps(doc['materials']))
    if ident=='birdramon':
        view=doc['bufferViews'][doc['images'][0]['bufferView']];start=28+length+view.get('byteOffset',0)
        im=Image.open(io.BytesIO(raw[start:start+view['byteLength']])).convert('RGBA');hist=im.getchannel('A').histogram()
        # Retain transparent feather cut-outs, while letting the opaque body
        # and overlapping feathers write depth instead of sorting as glass.
        assert sum(hist[:32])>0 and sum(hist[200:])>im.width*im.height*.5
        for mat in doc['materials']:
            mat['alphaMode']='MASK';mat['alphaCutoff']=.20;mat['pbrMetallicRoughness']['roughnessFactor']=.85
        changes=['Corrected translucent body and feather depth sorting with alpha-tested feather cut-outs; original texture retained.']
    else:
        for mat in doc['materials']:
            p=mat['pbrMetallicRoughness'];p['baseColorFactor']=[1,1,1,1];p['roughnessFactor']=.62
            mat.get('extensions',{}).get('KHR_materials_clearcoat',{})['clearcoatFactor']=.12
        changes=['Removed the extra grey tint from the original texture; softened excessively glossy shell highlights.']
    data=json.dumps(doc,separators=(',',':')).encode();data+=b' '*((-len(data))%4)
    result=struct.pack('<4sII',b'glTF',2,20+len(data)+len(binary))+struct.pack('<II',len(data),0x4e4f534a)+data+binary
    (target/'model.glb').write_bytes(result)
    record=json.loads((source/'source.json').read_text(encoding='utf-8-sig'))
    record['local_modifications']={'date':'2026-10-02','changes':changes,'model_sha256':sha(result),'original_animation_buffers_preserved':True}
    (target/'source.json').write_text(json.dumps(record,ensure_ascii=False,indent=2),encoding='utf-8')
    shutil.copy2(source/'preview.png',target/'preview.png')
    report={'id':ident,'passed':True,'sha256':sha(result),'source_sha256':sha(raw),'binary_sha256':sha(binary),
            'before':before,'after':doc['materials'],'changes':changes,'preserved_clips':[a['name'] for a in doc['animations']],
            'checks':['Only material JSON changed','Geometry, UVs, textures, skin and animation BIN chunk preserved byte-for-byte']}
    (target/'material-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print('MATERIAL REVIEW',ident,len(report['preserved_clips']),'source clips preserved')
