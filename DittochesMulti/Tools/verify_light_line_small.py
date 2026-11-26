"""Read back exported GLB bytes: clips, skinning, embedded textures and contacts."""
import argparse,json,math
from pathlib import Path
from verify_faithful_glb_motion import read,triangles
ROOT=Path(__file__).resolve().parents[1]
EXPECTED={'Idle':4.0,'Walk':1.2,'Run':.8,'Attack':1.3,'Skill':2.0,'Hit':.8,'Victory':3.2,'Guard':1.8,'Dodge':1.0,'Down':2.2}

def check(ident):
    folder=ROOT/'ArtSource/LightLine-20261013'/ident
    doc,access,digest=read(folder/'model.glb')
    assert {a['name'] for a in doc['animations']}==set(EXPECTED)
    assert doc.get('images') and all('bufferView' in i for i in doc['images'])
    assert len(doc.get('skins',[]))==1
    assert (folder/(ident+'.blend')).stat().st_size>10000
    weighted=0
    for mesh in doc['meshes']:
        for prim in mesh['primitives']:
            attrs=prim['attributes'];weights=access(attrs['WEIGHTS_0']);joints=access(attrs['JOINTS_0'])
            assert all(abs(sum(v)-1)<.002 and min(v)>=0 for v in weights)
            assert all(max(v)<len(doc['skins'][0]['joints']) and min(v)>=0 for v in joints)
            assert 'TEXCOORD_0' in attrs
            assert all(math.isfinite(x) for key in ('POSITION','NORMAL','TEXCOORD_0') for v in access(attrs[key]) for x in v)
            weighted+=len(weights)
    clips=[]
    for animation in doc['animations']:
        name=animation['name'];changed=0;seam=0
        for channel in animation['channels']:
            sampler=animation['samplers'][channel['sampler']]
            times=[v[0] for v in access(sampler['input'])];vals=access(sampler['output'])
            assert abs(times[0])<1e-7 and abs(times[-1]-EXPECTED[name])<1e-6,(ident,name,times[0],times[-1])
            assert all(a<b for a,b in zip(times,times[1:])) and len(times)==len(vals)
            assert all(math.isfinite(v) for row in vals for v in row)
            delta=max(abs(a-b) for a,b in zip(vals[0],vals[-1]))
            if channel['target']['path']=='rotation':delta=min(delta,max(abs(a+b) for a,b in zip(vals[0],vals[-1])))
            seam=max(seam,delta)
            changed+=int(any(max(abs(a-b) for a,b in zip(vals[0],row))>1e-4 for row in vals))
        assert changed>=3,(ident,name,'no substantial articulation')
        if name in ('Idle','Walk','Run'):assert seam<.0001,(ident,name,'loop seam',seam)
        clips.append({'name':name,'seconds':EXPECTED[name],'moving_channels':changed,'endpoint_delta':seam})
    report=json.loads((folder/'animation-report.json').read_text(encoding='utf-8'))
    assert all(sample['min'][2]>-.0001 for sample in report['pose_bounds']),(ident,'underground pose')
    assert report['output_sha256']==digest
    return dict(id=ident,sha256=digest,bones=report['bones'],triangles=triangles(doc),weighted_vertices=weighted,embedded_images=len(doc['images']),clips=clips,passed=True)

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--ids',default='nyaromon,plotmon,tailmon');args=p.parse_args()
    result={'passed':True,'models':[check(x) for x in args.ids.split(',')]}
    (ROOT/'ArtSource/LightLine-20261013/small-validation.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    print(json.dumps(result,indent=2))
