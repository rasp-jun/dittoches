"""Check exported animation timing, skin weights and preserved render geometry."""
import json,struct,hashlib,argparse
from pathlib import Path
from faithful_rig_profiles import PROFILES
from faithful_motion_catalog import DURATIONS,BASELINE,clips_for

ROOT=Path(__file__).resolve().parent.parent


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
    parser=argparse.ArgumentParser();parser.add_argument('--ids',default=','.join(list(PROFILES)+['birdramon','kuwagamon']));args=parser.parse_args()
    results=[]
    for ident in args.ids.split(','):
        path=ROOT/'ArtSource/AnimatedReview'/ident/'model.glb'
        doc,accessor,digest=read(path)
        report=json.loads((path.parent/'animation-report.json').read_text(encoding='utf-8'))
        baseline=ROOT/report.get('comparison_source',BASELINE+'/gallery/'+ident+'/model.glb')
        previous,_,_=read(baseline)
        native=report.get('native_extension',False)
        durations={name:duration for name,duration,_ in clips_for(ident)}
        assert {c['name']:c['duration'] for c in report['clips']}==durations,(ident,'stale authored timing report')
        assert triangles(doc)==triangles(previous),(ident,'render topology changed')
        assert len(doc.get('images',[]))==len(previous.get('images',[])),(ident,'embedded texture loss')
        originals=[a for a in previous['animations'] if a['name'] not in durations] if native else []
        source_clips={a['name'] for a in originals}
        assert {a['name'] for a in doc['animations']}==set(durations)|source_clips,(ident,'missing clips')
        if report.get('preserved_noncombat_clips'):
            from merge_technique_clips import chunks
            old_doc,old_binary=chunks(baseline);new_doc,new_binary=chunks(path)
            assert new_binary[:len(old_binary)]==old_binary,(ident,'original binary bytes changed')
            for key in ('meshes','skins','materials','images','nodes'):assert doc.get(key)==previous.get(key),(ident,key+' changed')
            old_actions={a['name']:a for a in previous['animations']}
            for action in doc['animations']:
                if action['name'] not in ('Attack','Skill'):assert action==old_actions[action['name']],(ident,action['name'],'preserved clip changed')
        if native:
            assert doc['animations'][:len(originals)]==originals,(ident,'source animation definitions changed')
            original=baseline.read_bytes();updated=path.read_bytes()
            old_start=28+struct.unpack_from('<I',original,12)[0];new_start=28+struct.unpack_from('<I',updated,12)[0]
            assert updated[new_start:new_start+len(original)-old_start]==original[old_start:],(ident,'source binary changed')
            for key in ('meshes','skins','materials','images','nodes'):assert doc.get(key)==previous.get(key),(ident,key+' source data changed')
        for mesh in doc['meshes']:
            for primitive in mesh['primitives']:
                weights=accessor(primitive['attributes']['WEIGHTS_0'])
                assert all(abs(sum(v)-1)<.001 and min(v)>=0 for v in weights),(ident,'invalid skin weights')
        checked=[]
        for animation in doc['animations']:
            name=animation['name']
            if name not in durations:
                checked.append({'clip':name,'original_source':True});continue
            duration=durations[name]
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
    (out/'glb-report.json').write_text(json.dumps({'passed':True,'models':results,'checks':['zero leading frame','exact common clip duration','exported loop endpoints','normalized skin weights','original triangle and texture counts retained','native source animations and binary bytes preserved']},indent=2),encoding='utf-8')
    print('NATURAL GLB PASS',len(results),'models',sum(len(r['clips']) for r in results),'clips')

if __name__=='__main__':main()
