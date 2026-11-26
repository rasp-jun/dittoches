import sys,json
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import bpy,numpy as np
from faithful_skin_topology import topology
from animate_faithful_models import mesh_positions
from roster_rebuild_sources import OUT
for ident in sys.argv[sys.argv.index('--')+1:]:
    bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(OUT/ident/'static.glb'))
    rows=[]
    for obj in [o for o in bpy.context.scene.objects if o.type=='MESH']:
        points,inverse,edges,labels=topology(obj.data,mesh_positions(obj.data))
        for label in np.unique(labels):
            ids=np.flatnonzero(labels==label);p=points[ids]
            if len(ids)<15:continue
            rows.append(dict(object=obj.name,island=int(label),vertices=len(ids),center=p.mean(axis=0).tolist(),min=p.min(axis=0).tolist(),max=p.max(axis=0).tolist(),span=np.ptp(p,axis=0).tolist()))
    (OUT/ident/'islands.json').write_text(json.dumps(rows,indent=2),encoding='utf-8')
    print(ident,json.dumps(rows),flush=True)
