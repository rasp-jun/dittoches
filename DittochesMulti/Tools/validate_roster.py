"""Validate every expected character file and independent GLB animation export."""
import json
import math
import struct
from pathlib import Path
from roster_designs import DESIGNS,CLIPS

root=Path(__file__).resolve().parent.parent
checks=0;summary=[]
expected={d[0] for d in DESIGNS}
catalog={s['id'] for s in json.loads((root/'Assets/Resources/DigimonSkills.json').read_text(encoding='utf-8-sig'))['skills']}
assert expected==catalog,(expected-catalog,catalog-expected)


def check(value,message):
    global checks
    if not value:raise AssertionError(message)
    checks+=1


for ident,_,kind,*_ in DESIGNS:
    file=root/'Assets/Resources/Models'/('Agumon/Agumon.bytes' if ident=='agumon' else 'Roster/'+ident+'.bytes')
    blob=file.read_bytes();offset=4
    def read(fmt):
        global offset
        out=struct.unpack_from('<'+fmt,blob,offset);offset+=struct.calcsize('<'+fmt);return out
    def name():
        global offset
        n,=read('i');check(0<n<129,ident+': name length');v=blob[offset:offset+n].decode();offset+=n;return v
    check(blob[:4]==b'DTM3',ident+': magic')
    version,bones,vertices,triangles,clips=read('5i')
    check(version==1 and 10<=bones<=64 and 100<vertices<150000 and clips==8,ident+': header')
    bone_names=[]
    for i in range(bones):
        bone_names.append(name());parent,=read('i');p=read('3f')
        check(-1<=parent<i and all(math.isfinite(v) for v in p),ident+': hierarchy')
    check(len(set(bone_names))==bones and {'Hips','Head','Jaw'}.issubset(bone_names),ident+': anatomical bones')
    for i in range(vertices):
        v=read('10f');indices=read('4i');weights=read('4f')
        check(all(math.isfinite(x) for x in v),ident+': finite vertex')
        check(all(0<=b<bones for b in indices) and abs(sum(weights)-1)<1e-4 and min(weights)>=0,ident+': skin')
        check(.9<sum(x*x for x in v[3:6])<1.1,ident+': unit normal')
    for i in range(triangles):check(all(0<=index<vertices for index in read('3i')),ident+': triangles')
    names=[]
    for i in range(clips):
        clip=name();names.append(clip);duration,=read('f');loop,frames=read('2i');first=None;last=None
        check(duration>0 and 2<=frames<1000,ident+': timeline')
        for frame in range(frames):
            values=read('7f'*bones)
            check(all(math.isfinite(v) for v in values),ident+': pose')
            for bone in range(bones):check(abs(sum(v*v for v in values[bone*7+3:bone*7+7])-1)<1e-4,ident+': rotation')
            if first is None:first=values
            last=values
        if loop:check(max(abs(a-b) for a,b in zip(first,last))<1e-4,ident+': closed loop '+clip)
    check(set(names)=={c[0] for c in CLIPS} and offset==len(blob),ident+': complete parse')
    glb_path=root/'ArtSource'/('Agumon/Agumon.glb' if ident=='agumon' else 'Roster/'+ident+'/'+ident+'.glb')
    glb=glb_path.read_bytes();magic,version,length=struct.unpack_from('<4sII',glb)
    size,chunk=struct.unpack_from('<II',glb,12);doc=json.loads(glb[20:20+size])
    check(magic==b'glTF' and length==len(glb),ident+': GLB')
    check({a['name'] for a in doc['animations']}==set(names),ident+': independent GLB clips')
    summary.append({'id':ident,'vertices':vertices,'triangles':triangles,'bones':bones,'clips':clips})
report={'checks':checks,'characters':len(summary),'clips':sum(s['clips'] for s in summary),'models':summary}
(root/'ArtSource/Roster/validation-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('ROSTER STRUCTURE PASS',checks,'checks;',len(summary),'characters;',report['clips'],'clips')
