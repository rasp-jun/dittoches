import bpy,json
from pathlib import Path
from mathutils import Vector
base=Path(__file__).resolve().parents[1]/'ArtSource/LightLine-20261013/ophanimon'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(next((base/'source').rglob('*Animations.fbx'))))
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
out={}
for name in ['md149_yari','md149_tate','J_hand_L','J_hand_R','J_wrist_L','J_wrist_R']:
    b=rig.pose.bones[name]
    pts=[]
    for obj in bpy.context.scene.objects:
        if obj.type!='MESH' or name not in obj.vertex_groups:continue
        group=obj.vertex_groups[name].index
        for v in obj.data.vertices:
            if any(g.group==group and g.weight>.5 for g in v.groups):pts.append(rig.matrix_world.inverted()@obj.matrix_world@v.co)
    out[name]=dict(rest=[list(v) for v in b.bone.matrix_local],pose=[list(v) for v in b.matrix],loc=list(b.location),parent=b.parent.name if b.parent else None,points=len(pts))
    if pts:out[name].update(min=[min(p[i] for p in pts) for i in range(3)],max=[max(p[i] for p in pts) for i in range(3)])
(base/'prop-inspection-fbx.json').write_text(json.dumps(out,indent=2))
