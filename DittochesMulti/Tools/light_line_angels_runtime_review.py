import bpy,sys
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from faithful_rig_common import studio,render
BASE=Path(__file__).resolve().parents[1]/'ArtSource/LightLine-20261013'
for ident in ['angewomon','ophanimon']:
 folder=BASE/ident;bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.gltf(filepath=str(folder/'model.glb'))
 rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
 meshes=[o for o in bpy.context.scene.objects if o.type=='MESH' and any(m.type=='ARMATURE' for m in o.modifiers)]
 camera=studio(meshes);scene=bpy.context.scene;scene.cycles.samples=8;scene.render.resolution_x=640;scene.render.resolution_y=640
 for clip,progress in [('Idle',0),('Attack',.5),('Skill',.35),('Skill',.65),('Skill',.85)]:
  action=next(a for a in bpy.data.actions if a.name==clip or a.name.endswith('|'+clip))
  rig.animation_data.action=action;rig.animation_data.action_slot=action.slots[0]
  lo,hi=action.frame_range;f=lo+(hi-lo)*progress;scene.frame_set(int(f),subframe=f-int(f));bpy.context.view_layer.update()
  graph=bpy.context.evaluated_depsgraph_get();pts=[o.matrix_world@Vector(v) for mesh in meshes for o in [mesh.evaluated_get(graph)] for v in o.bound_box]
  low=Vector([min(v[i] for v in pts) for i in range(3)]);high=Vector([max(v[i] for v in pts) for i in range(3)]);target=(low+high)*.5
  camera.data.ortho_scale=max(3.1,max(high-low)*1.25)
  render(folder/(f'runtime-{clip.lower()}-{round(progress*100):02}.png'),camera,target+Vector((3,-6,2.1)),target)
