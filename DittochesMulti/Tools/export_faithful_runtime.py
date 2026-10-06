"""Pack the reviewed GLBs losslessly for the offline Unity runtime (FDM1).

No mesh simplification, re-rigging or old procedural models. Numeric attributes,
embedded images and original animation keys are retained; reflect Z for Unity.
"""
import hashlib,json,struct,math,io
from faithful_techniques import TECHNIQUES
from pathlib import Path
ROOT=Path(__file__).resolve().parent.parent
OUT=ROOT/'Assets/StreamingAssets/FaithfulModels'

def convert(path,target):
    blob=path.read_bytes();size=struct.unpack_from('<I',blob,12)[0]
    doc=json.loads(blob[20:20+size]);binary=blob[28+size:]
    def read(index):
        a=doc['accessors'][index];v=doc['bufferViews'][a['bufferView']]
        assert 'sparse' not in a
        count={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4,'MAT4':16}[a['type']]
        fmt={5120:'b',5121:'B',5122:'h',5123:'H',5125:'I',5126:'f'}[a['componentType']]
        stride=v.get('byteStride',struct.calcsize('<'+fmt*count));offset=v.get('byteOffset',0)+a.get('byteOffset',0)
        rows=[struct.unpack_from('<'+fmt*count,binary,offset+i*stride) for i in range(a['count'])]
        if a.get('normalized'):
            divisor={5120:127,5121:255,5122:32767,5123:65535}[a['componentType']]
            rows=[tuple(max(-1,x/divisor) for x in row) for row in rows]
        return rows
    stream=io.BytesIO();stream.write(b'FDM1')
    def ints(*values):stream.write(struct.pack('<'+'i'*len(values),*values))
    def floats(*values):
        assert all(math.isfinite(x) for x in values)
        stream.write(struct.pack('<'+'f'*len(values),*values))
    def string(text):
        b=text.encode('utf-8');ints(len(b));stream.write(b)
    def vector(v):floats(v[0],v[1],-v[2])
    def rotation(q):floats(-q[0],-q[1],q[2],q[3])
    string(path.parent.name);string(hashlib.sha256(blob).hexdigest())
    parents={c:i for i,n in enumerate(doc['nodes']) for c in n.get('children',[])}
    ints(len(doc['nodes']))
    for i,node in enumerate(doc['nodes']):
        string(node.get('name',str(i)));ints(parents.get(i,-1))
        ints(int('matrix' in node))
        if 'matrix' in node:floats(*(x*(-1 if (j%4==2)!=(j//4==2) else 1) for j,x in enumerate(node['matrix'])))
        else:
            vector(node.get('translation',[0,0,0]));rotation(node.get('rotation',[0,0,0,1]));floats(*node.get('scale',[1,1,1]))
    images=doc.get('images',[]);ints(len(images))
    for image in images:
        view=doc['bufferViews'][image['bufferView']];start=view.get('byteOffset',0)
        data=binary[start:start+view['byteLength']];ints(len(data));stream.write(data)
    materials=doc.get('materials',[]);ints(len(materials))
    for mat in materials:
        pbr=mat.get('pbrMetallicRoughness',{});texture=pbr.get('baseColorTexture',{})
        string(mat.get('name','material'));floats(*pbr.get('baseColorFactor',[1,1,1,1]))
        floats(pbr.get('metallicFactor',1),pbr.get('roughnessFactor',1),mat.get('alphaCutoff',.5))
        ints(doc['textures'][texture['index']]['source'] if texture else -1,
             {'OPAQUE':0,'MASK':1,'BLEND':2}[mat.get('alphaMode','OPAQUE')],int(mat.get('doubleSided',False)))
    parts=[(i,node,primitive) for i,node in enumerate(doc['nodes']) if 'mesh' in node for primitive in doc['meshes'][node['mesh']]['primitives']]
    ints(len(parts));triangles=0
    for node_index,node,part in parts:
        assert part.get('mode',4)==4
        a=part['attributes'];positions=read(a['POSITION']);count=len(positions)
        skin=doc['skins'][node['skin']] if 'skin' in node else None
        joints=skin['joints'] if skin else []
        mat=part.get('material',-1)
        tex=materials[mat].get('pbrMetallicRoughness',{}).get('baseColorTexture',{}) if mat>=0 else {}
        assert 'KHR_texture_transform' not in tex.get('extensions',{})
        def attr(name,default):return read(a[name]) if name in a else [default]*count
        normals=attr('NORMAL',[0,1,0]);uv=attr('TEXCOORD_'+str(tex.get('texCoord',0)),[0,0])
        colors=attr('COLOR_0',[1,1,1,1]);weights=attr('WEIGHTS_0',[1,0,0,0]);indices=attr('JOINTS_0',[0,0,0,0])
        faces=[v[0] for v in read(part['indices'])] if 'indices' in part else list(range(count))
        ints(node_index,mat,count,len(faces),len(joints));ints(*joints)
        if skin:
            binds=read(skin['inverseBindMatrices'])
            for matrix in binds:floats(*(x*(-1 if (j%4==2) != (j//4==2) else 1) for j,x in enumerate(matrix)))
        for i in range(count):
            vector(positions[i]);vector(normals[i]);floats(uv[i][0],1-uv[i][1]);floats(*(list(colors[i])+[1])[:4])
            ints(*map(int,indices[i]));floats(*weights[i])
            if skin:assert abs(sum(weights[i])-1)<.002 and all(0<=j<len(joints) for j in indices[i])
        for i in range(0,len(faces),3):ints(faces[i],faces[i+2],faces[i+1])
        triangles+=len(faces)//3
    animations=doc['animations'];ints(len(animations));key_count=0
    for clip in animations:
        string(clip['name']);duration=max(read(s['input'])[-1][0] for s in clip['samplers']);floats(duration)
        ints(len(clip['channels']))
        for channel in clip['channels']:
            sampler=clip['samplers'][channel['sampler']];kind={'translation':0,'rotation':1,'scale':2}[channel['target']['path']]
            interpolation=sampler.get('interpolation','LINEAR');assert interpolation in ('LINEAR','STEP')
            times=read(sampler['input']);values=read(sampler['output']);assert len(times)==len(values)
            ints(channel['target']['node'],kind,int(interpolation=='STEP'),len(times));key_count+=len(times)
            for time,value in zip(times,values):
                floats(time[0]);rotation(value) if kind==1 else vector(value) if kind==0 else floats(*value)
    target.write_bytes(stream.getvalue())
    return dict(id=path.parent.name,source_sha256=hashlib.sha256(blob).hexdigest(),sha256=hashlib.sha256(target.read_bytes()).hexdigest(),
                nodes=len(doc['nodes']),parts=len(parts),triangles=triangles,images=len(images),clips=len(animations),keys=key_count,bytes=target.stat().st_size)

def main():
    OUT.mkdir(parents=True,exist_ok=True)
    manifest=json.loads((ROOT/'ArtSource/FaithfulGallery/manifest.json').read_text(encoding='utf-8'))
    rows=[]
    for entry in manifest['entries']:
        path=ROOT/'ArtSource/FaithfulGallery'/entry['model']
        assert hashlib.sha256(path.read_bytes()).hexdigest()==entry['sha256']
        row=convert(path,OUT/(entry['id']+'.bytes'));rows.append(row);print('PACKED',row['id'],row['clips'],flush=True)
        (OUT/(entry['id']+'-source.json')).write_bytes((path.parent/'source.json').read_bytes())
    result=dict(format='FDM1',passed=True,models=rows,clips=sum(r['clips'] for r in rows))
    (OUT/'manifest.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    (OUT/'techniques.json').write_text(json.dumps(dict(models=[dict(id=r['id'],emitter=TECHNIQUES[r['id']]['Skill']['origin'],attackRelease=TECHNIQUES[r['id']]['Attack']['release'],skillRelease=TECHNIQUES[r['id']]['Skill']['release']) for r in rows]),indent=2),encoding='utf-8')
    out=ROOT/'Builds/MotionEngine-20261006';out.mkdir(exist_ok=True,parents=True)
    (out/'export-report.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    print('PACKED',len(rows),'MODELS',result['clips'],'CLIPS')

if __name__=='__main__':main()
