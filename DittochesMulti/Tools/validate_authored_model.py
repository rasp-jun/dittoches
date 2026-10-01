"""Validate the authored game asset and its independently exported GLB clips."""
import json
import math
import struct
from pathlib import Path

root=Path(__file__).resolve().parent.parent
blob=(root/'Assets/Resources/Models/Agumon/Agumon.bytes').read_bytes()
offset=0
checks=0


def check(condition,message):
    global checks
    if not condition:raise AssertionError(message)
    checks+=1


def read(fmt):
    global offset
    result=struct.unpack_from('<'+fmt,blob,offset)
    offset+=struct.calcsize('<'+fmt)
    return result


def name():
    global offset
    length,=read('i');value=blob[offset:offset+length].decode('utf-8');offset+=length
    return value


check(blob[:4]==b'DTM3','Asset magic')
offset=4
version,bone_count,vertex_count,triangle_count,clip_count=read('5i')
check(version==1,'Version')
check(bone_count==19,'Expected anatomical skeleton')
check(1000<vertex_count<65536,'Single 16-bit mesh vertex budget')
check(0<triangle_count<75000,'Triangle budget')
bones=[]
for i in range(bone_count):
    bone=name();parent,=read('i');position=read('3f');bones.append(bone)
    check(-1<=parent<i,'Parent precedes child: '+bone)
    check(all(math.isfinite(v) for v in position),'Finite bind position')
check(len(bones)==len(set(bones)),'Unique bones')
for i in range(vertex_count):
    values=read('10f');indices=read('4i');weights=read('4f')
    check(all(math.isfinite(v) for v in values),'Finite vertex data')
    check(all(0<=v<bone_count for v in indices),'Bone indices')
    check(all(w>=0 for w in weights) and abs(sum(weights)-1)<1e-5,'Normalized weights')
    length=sum(v*v for v in values[3:6])
    check(.95<length<1.05,'Unit vertex normal')
for i in range(triangle_count):
    tri=read('3i')
    check(all(0<=v<vertex_count for v in tri),'Triangle index bounds')
clips={}
for i in range(clip_count):
    clip=name();duration,=read('f');loop,frames=read('2i');first=None;last=None
    check(duration>0 and frames>1,'Clip timeline: '+clip)
    for frame in range(frames):
        values=read('7f'*bone_count)
        check(all(math.isfinite(v) for v in values),'Finite motion values')
        for bone in range(bone_count):
            q=values[bone*7+3:bone*7+7]
            check(abs(sum(v*v for v in q)-1)<1e-4,'Unit rotation')
        if frame==0:first=values
        last=values
    if loop:
        check(max(abs(a-b) for a,b in zip(first,last))<.0001,'Seamless loop: '+clip)
    clips[clip]=duration
check(offset==len(blob),'Entire model parsed')
expected={'Idle','Walk','Run','Attack','PepperBreath','Hit','Defeat','Turn'}
check(set(clips)==expected,'All required clips')
glb=(root/'ArtSource/Agumon/Agumon.glb').read_bytes()
magic,version,total=struct.unpack_from('<4sII',glb)
check(magic==b'glTF' and version==2 and total==len(glb),'Valid GLB header')
size,chunk_type=struct.unpack_from('<II',glb,12)
check(chunk_type==0x4e4f534a,'GLB JSON chunk')
document=json.loads(glb[20:20+size])
animations={a['name'] for a in document.get('animations',[])}
check(animations==expected,'GLB preserves eight distinct animation clips: '+str(animations))
check(len(document.get('skins',[]))>=1,'GLB skin exists')
check(all(len(s['joints'])==bone_count for s in document['skins']),'GLB skeleton matches engine skeleton')
report=json.loads((root/'ArtSource/Agumon/model-report.json').read_text())
check(len(report['pose_bounds'])>=60,'Evaluated Blender poses present')
check(min(p['minimum_z'] for p in report['pose_bounds'])>=-.015,'No sampled ground penetration')
print(f'AUTHORED MODEL PASS: {checks} structural checks; {len(report["pose_bounds"])} evaluated pose bounds; 8 GLB clips; {vertex_count} vertices / {triangle_count} triangles.')
