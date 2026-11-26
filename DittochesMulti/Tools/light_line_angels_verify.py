"""Independently reimport delivered GLBs and inspect skin deformation/clips."""
import bpy,sys,json,math,struct,hashlib
from pathlib import Path
from mathutils import Vector
import numpy as np
sys.path.insert(0,str(Path(__file__).resolve().parent))
from verify_faithful_glb_motion import read
ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/'ArtSource/LightLine-20261013'
REQUIRED={'Idle','Walk','Run','Attack','Skill','Hit','Victory','Guard','Dodge','Down'}

def vertices(meshes):
    graph=bpy.context.evaluated_depsgraph_get();out=[]
    for obj in meshes:
        evaluated=obj.evaluated_get(graph);mesh=evaluated.to_mesh()
        out.extend([list(evaluated.matrix_world@v.co) for v in mesh.vertices]);evaluated.to_mesh_clear()
    return np.asarray(out,dtype=float)

results=[]
for ident in ['angewomon','ophanimon']:
    folder=BASE/ident;blob=(folder/'model.glb').read_bytes()
    doc=json.loads(blob[20:20+struct.unpack_from('<I',blob,12)[0]])
    _,accessor,_=read(folder/'model.glb')
    durations={a['name']:max(accessor(s['input'])[-1][0] for s in a['samplers']) for a in doc['animations']}
    for mesh in doc['meshes']:
        for part in mesh['primitives']:
            assert all(abs(sum(w)-1)<.001 and min(w)>=0 for w in accessor(part['attributes']['WEIGHTS_0']))
    assert {a['name'] for a in doc['animations']}==REQUIRED
    assert doc.get('images') and doc.get('skins')
    triangles=sum(doc['accessors'][p['indices']]['count']//3 for m in doc['meshes'] for p in m['primitives'])
    assert triangles==({'angewomon':7648,'ophanimon':6016}[ident]),(ident,triangles)
    bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(folder/'model.glb'))
    rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
    # Blender's GLB importer creates a hidden Icosphere bone-gizmo mesh.
    # Only the actual skinned render meshes belong to the delivered asset.
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH' and any(m.type=='ARMATURE' for m in o.modifiers)]
    assert all(o.data.uv_layers for o in meshes)
    unweighted=0;non_normalized=0;loose_unweighted=0
    for obj in meshes:
        used={index for poly in obj.data.polygons for index in poly.vertices}
        for vertex in obj.data.vertices:
            total=sum(g.weight for g in vertex.groups)
            if vertex.index not in used:
                loose_unweighted+=total<=0;continue
            unweighted+=total<=0;non_normalized+=abs(total-1)>.002
    print('WEIGHTS',ident,unweighted,non_normalized,flush=True)
    if unweighted:
        print('BAD_WEIGHT_DETAIL',[(obj.name,v.index,list(v.co),[(g.group,g.weight) for g in v.groups]) for obj in meshes for v in obj.data.vertices if sum(g.weight for g in v.groups)<=0][:6],flush=True)
    assert unweighted==0 and non_normalized==0
    bpy.context.scene.render.fps=30
    rows=[]
    for action in bpy.data.actions:
        name=action.name.split('_Rig|')[-1]
        if name not in REQUIRED:
            name=next((n for n in REQUIRED if action.name==n or action.name.endswith('|'+n)),action.name)
        assert name in REQUIRED,action.name
        rig.animation_data_create();rig.animation_data.action=action
        if action.slots:rig.animation_data.action_slot=action.slots[0]
        lo,hi=action.frame_range;samples=[]
        for t in [0,.125,.25,.375,.5,.625,.75,.875,1]:
            f=lo+(hi-lo)*t;bpy.context.scene.frame_set(int(f),subframe=f-int(f));bpy.context.view_layer.update()
            points=vertices(meshes);assert np.isfinite(points).all()
            samples.append(points)
        movement=max(float(np.linalg.norm(p-samples[0],axis=1).max()) for p in samples)
        seam=float(np.linalg.norm(samples[-1]-samples[0],axis=1).max())
        assert movement>.006,(ident,name,'static animation',movement)
        allpoints=np.concatenate(samples);extent=allpoints.max(0)-allpoints.min(0)
        assert max(extent)<8,(ident,name,extent.tolist())
        if name in {'Idle','Walk','Run'}:assert seam<.09,(ident,name,'loop seam',seam)
        rows.append(dict(name=name,duration=durations[name],moving_vertex_max=movement,loop_seam=seam,min=allpoints.min(0).tolist(),max=allpoints.max(0).tolist()))
    result=dict(id=ident,passed=True,triangles=triangles,skin_joints=len(doc['skins'][0]['joints']),images=len(doc['images']),unweighted=unweighted,non_normalized_weights=non_normalized,imported_loose_unweighted=loose_unweighted,clips=rows,sha256=hashlib.sha256(blob).hexdigest())
    (folder/'verification.json').write_text(json.dumps(result,indent=2),encoding='utf-8');results.append(result)
    print('VERIFIED',ident,triangles,len(rows),'clips',flush=True)
(BASE/'angels-verification.json').write_text(json.dumps(dict(passed=True,models=results),indent=2),encoding='utf-8')
