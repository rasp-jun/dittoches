import bpy,json,sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parent.parent
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/MetalGreymonShoulderBackup-20261006/review/metalgreymon.blend'))
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
rig.animation_data.action=bpy.data.actions['Idle'];bpy.context.scene.frame_set(1)
data={'bones':{b.name:{'head':list(b.head_local),'tail':list(b.tail_local),'pose_head':list(rig.pose.bones[b.name].head),'parent':b.parent.name if b.parent else None} for b in rig.data.bones},'meshes':[]}
for obj in bpy.context.scene.objects:
    if obj.type!='MESH':continue
    groups={g.index:g.name for g in obj.vertex_groups};bounds={}
    for vertex in obj.data.vertices:
        for group in vertex.groups:
            if group.weight<.5:continue
            bounds.setdefault(groups[group.group],[]).append(list(vertex.co))
    region=[v for v in obj.data.vertices if v.co.x<-.3 and .8<v.co.z<1.6 and v.co.y<.2]
    data['meshes'].append({'name':obj.name,'matrix':list(map(list,obj.matrix_world)),'vertices':len(obj.data.vertices),'left_region':{'count':len(region),'weights':{g.name:sum(next((w.weight for w in v.groups if w.group==g.index),0) for v in region) for g in obj.vertex_groups}},'bounds':{name:{'count':len(points),'min':[min(p[i] for p in points) for i in range(3)],'max':[max(p[i] for p in points) for i in range(3)]} for name,points in bounds.items()}})
(ROOT/'Builds/MetalGreymonShoulder-20261006/inspection.json').write_text(json.dumps(data,indent=2),encoding='utf-8')
print(json.dumps(data))
