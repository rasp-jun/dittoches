import bpy,json
from pathlib import Path
base=Path(__file__).resolve().parents[1]/'ArtSource/LightLine-20261013/angewomon'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(next((base/'source').rglob('*animations.fbx'))))
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');rows=[]
for a in bpy.data.actions:
 if '.001' in a.name:continue
 rig.animation_data.action=a;rig.animation_data.action_slot=a.slots[0]
 scales=[];lo,hi=a.frame_range
 for t in [0,.2,.4,.6,.8,1]:
  f=lo+(hi-lo)*t;bpy.context.scene.frame_set(int(f),subframe=f-int(f));bpy.context.view_layer.update()
  b=rig.pose.bones['J_bow_C'];scales.append(list(b.scale))
 rows.append(dict(action=a.name,scales=scales))
(base/'bow-inspection.json').write_text(json.dumps(rows,indent=2))
