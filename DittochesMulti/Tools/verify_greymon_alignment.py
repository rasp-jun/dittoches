"""Check the actual neutral eye line and separate neck binding after export."""
import bpy,sys,json,hashlib
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'Tools'))
from faithful_skin_topology import topology
folder=ROOT/'ArtSource/AnimatedReview/greymon'
bpy.ops.wm.open_mainfile(filepath=str(folder/'greymon.blend'))
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
assert rig.data.bones['Head'].parent.name=='Neck'
assert rig.data.bones['Neck'].parent.name=='Spine'
rig.animation_data.action=bpy.data.actions['Idle'];bpy.context.scene.frame_set(1)
obj=next(o for o in bpy.context.scene.objects if o.type=='MESH')
xyz=np.array([v.co[:] for v in obj.data.vertices]);points,inverse,edges,labels=topology(obj.data,xyz)
evaluated=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=evaluated.to_mesh()
actual=np.array([v.co[:] for v in mesh.vertices]);eyes=[]
for label in np.unique(labels):
    ids=np.flatnonzero(labels==label)
    if len(ids) in (723,734):eyes.append(actual[np.isin(inverse,ids)].mean(0))
assert len(eyes)==2;eyes.sort(key=lambda p:p[0]);axis=eyes[1]-eyes[0]
level_error=float(abs(axis[2])/np.linalg.norm(axis));yaw_error=float(abs(axis[1])/np.linalg.norm(axis))
assert level_error<.015 and yaw_error<.015,('eye axis still tilted',level_error,yaw_error)
report=dict(passed=True,sha256=hashlib.sha256((folder/'model.glb').read_bytes()).hexdigest(),eye_centers=[p.tolist() for p in eyes],
            relative_eye_height_error=level_error,relative_eye_depth_error=yaw_error,neck_parent_chain=['Spine','Neck','Head'])
(folder/'alignment-report.json').write_text(json.dumps(report,indent=2))
print('GREYMON NECK ALIGNMENT PASS',level_error,yaw_error)
