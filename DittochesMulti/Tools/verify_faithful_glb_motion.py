"""Check exported animation timing, skin weights and preserved render geometry."""
import json,struct,hashlib
from pathlib import Path
from faithful_rig_profiles import PROFILES

ROOT=Path(__file__).resolve().parent.parent
DURATIONS={'Idle':2.8,'Walk':1.0,'Run':.7,'Attack':1.1,'Hit':.6,'Victory':2.8}


def read(path):
    blob=path.read_bytes();length=struct.unpack_from('<I',blob,12)[0]
    doc=json.loads(blob[20:20+length]);start=28+length
    def accessor(index):
        ac=doc['accessors'][index];view=doc['bufferViews'][ac['bufferView']]
        count={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4,'MAT4':16}[ac['type']]
        fmt={5126:'f',5125:'I',5123:'H',5121:'B'}[ac['componentType']]
        size=struct.calcsize('<'+fmt*count);stride=view.get('byteStride',size)
        offset=start+view.get('byteOffset',0)+ac.get('byteOffset',0)
        result=[struct.unpack_from('<'+fmt*count,blob,offset+i*stride) for i in range(ac['count'])]
        if ac.get('normalized'):
            divisor={5121:255,5123:65535}[ac['componentType']]
            result=[tuple(v/divisor for v in row) for row in result]
        return result
    return doc,accessor,hashlib.sha256(blob).hexdigest()


def triangles(doc):
    return sum(doc['accessors'][p['indices']]['count']//3 for m in doc['meshes'] for p in m['primitives'])


def main():
    results=[]
    for ident in PROFILES:
        path=ROOT/'ArtSource/AnimatedReview'/ident/'model.glb'
        doc,accessor,digest=read(path)
        previous,_,_=read(ROOT/'ArtSource/NaturalPassBackup-20261001/gallery'/ident/'model.glb')
        assert triangles(doc)==triangles(previous),(ident,'render topology changed')
        assert len(doc.get('images',[]))==len(previous.get('images',[])),(ident,'embedded texture loss')
        assert {a['name'] for a in doc['animations']}==set(DURATIONS),(ident,'missing clips')
        for mesh in doc['meshes']:
            for primitive in mesh['primitives']:
                weights=accessor(primitive['attributes']['WEIGHTS_0'])
                assert all(abs(sum(v)-1)<.001 and min(v)>=0 for v in weights),(ident,'invalid skin weights')
        checked=[]
        for animation in doc['animations']:
            name=animation['name'];duration=DURATIONS[name]
            for sampler in animation['samplers']:
                times=[v[0] for v in accessor(sampler['input'])]
                assert abs(times[0])<1e-7,(ident,name,'extra leading frame')
                assert abs(times[-1]-duration)<1e-6,(ident,name,'wrong duration')
                assert all(a<b for a,b in zip(times,times[1:])),(ident,name,'non-monotonic sampling')
            if name in ('Idle','Walk','Run'):
                for channel in animation['channels']:
                    sampler=animation['samplers'][channel['sampler']];values=accessor(sampler['output'])
                    first,last=values[0],values[-1]
                    delta=max(abs(a-b) for a,b in zip(first,last))
                    if channel['target']['path']=='rotation':delta=min(delta,max(abs(a+b) for a,b in zip(first,last)))
                    assert delta<.0001,(ident,name,'exported loop seam',delta)
            checked.append({'clip':name,'start':0,'duration':duration})
        results.append({'id':ident,'sha256':digest,'triangles':triangles(doc),'clips':checked})
    out=ROOT/'Builds/FaithfulNaturalValidation';out.mkdir(exist_ok=True)
    (out/'glb-report.json').write_text(json.dumps({'passed':True,'models':results,'checks':['zero leading frame','exact authored clip duration','exported loop endpoints','normalized skin weights','original triangle and texture counts retained']},indent=2),encoding='utf-8')
    print('NATURAL GLB PASS',len(results),'models',sum(len(r['clips']) for r in results),'clips')

if __name__=='__main__':main()
