"""Rebuild the neck, shoulder/pelvis alignment and neutral skin of Greymon."""
import bpy,math
import numpy as np
from mathutils import Vector
from faithful_pose_math import Pose
from faithful_skin_topology import topology

def rebuild(rig,meshes):
    c=Pose(rig);c.reset();before={b.name:list(b.head_local) for b in rig.data.bones}
    obj=meshes[0];xyz=np.array([v.co[:] for v in obj.data.vertices]);pts,inv,edges,labels=topology(obj.data,xyz)
    eyes=[]
    for label in np.unique(labels):
        ids=np.flatnonzero(labels==label)
        if len(ids) in (723,734):eyes.append(pts[ids].mean(0))
    assert len(eyes)==2,'Expected the preserved pair of eye surfaces'
    eyes.sort(key=lambda v:v[0]);axis=Vector(eyes[1]-eyes[0])
    correction=axis.rotation_difference(Vector((1,0,0)))
    c.rotate_quat('Head',correction)
    c.move('Head',Vector((0,-.56,1.91))-c.point('Head'))
    for side,s in [(-1,'L'),(1,'R')]:
        c.move('Thigh'+s,Vector((side*.35,.025,.91))-c.point('Thigh'+s))
        c.move('UpperArm'+s,Vector((side*.34,-.35,1.58))-c.point('UpperArm'+s))
        c.ik('Thigh'+s,'Shin'+s,'Foot'+s,(side*.44,-.28,.19),(side*.12,-1,0),rig.data.bones['Foot'+s].matrix_local.to_quaternion())
        c.ik('UpperArm'+s,'Forearm'+s,'Hand'+s,(side*.46,-.70,1.23),(side*.2,.25,-1))
        hand='Hand'+s
        direction=c.world(hand).to_3x3()@Vector((0,1,0))
        c.rotate_quat(hand,direction.rotation_difference(Vector((side*.08,-.65,-.75))))
    c.move('Tail',(-c.point('Tail').x,0,0))
    c.rotate('Tail',z=8,x=-7);c.rotate('TailTip',z=4,x=-5)
    modifiers=[m for o in meshes for m in o.modifiers if m.type=='ARMATURE']
    for mod in modifiers:mod.use_deform_preserve_volume=True
    bpy.context.view_layer.update();graph=bpy.context.evaluated_depsgraph_get();deformed={}
    for o in meshes:
        e=o.evaluated_get(graph);me=e.to_mesh();deformed[o.name]=np.array([v.co[:] for v in me.vertices]);e.to_mesh_clear()
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
    bpy.ops.object.mode_set(mode='POSE');bpy.ops.pose.armature_apply(selected=False);bpy.ops.object.mode_set(mode='OBJECT')
    lowest=min(v[:,2].min() for v in deformed.values())
    for o in meshes:
        v=deformed[o.name];v[:,2]-=lowest
        # Broaden the thin neck locally, without pulling the jaw or shoulder.
        w=np.clip((v[:,2]-1.55)/.14,0,1)*np.clip((1.92-v[:,2])/.13,0,1)
        w*=np.clip((.29-np.abs(v[:,0]))/.09,0,1)*np.clip((-.16-v[:,1])/.16,0,1)
        v[:,0]*=1+.16*w
        o.data.vertices.foreach_set('co',v.astype(np.float32).ravel());o.data.update()
        if o.data.has_custom_normals:o.data.normals_split_custom_set([(0,0,0)]*len(o.data.loops))
    bpy.ops.object.mode_set(mode='EDIT')
    for b in rig.data.edit_bones:b.head.z-=lowest;b.tail.z-=lowest
    neck=rig.data.edit_bones.new('Neck');neck.head=(0,-.37,1.59-lowest);neck.tail=rig.data.edit_bones['Head'].head.copy();neck.parent=rig.data.edit_bones['Spine']
    rig.data.edit_bones['Spine'].tail=neck.head;rig.data.edit_bones['Head'].parent=neck
    root=rig.data.edit_bones['Root'];root.head=(0,0,0);root.tail=(0,0,.2)
    bpy.ops.object.mode_set(mode='OBJECT')
    count=0
    for o in meshes:
        neckgroup=o.vertex_groups.new(name='Neck');head=o.vertex_groups['Head'];spine=o.vertex_groups['Spine']
        for v in o.data.vertices:
            groups={g.group:g.weight for g in v.groups};total=groups.get(head.index,0)+groups.get(spine.index,0)
            z=v.co.z+lowest
            if total<.7 or not (1.51<z<1.95) or abs(v.co.x)>.3 or v.co.y>-.15:continue
            t=max(0,min(1,(z-1.57)/.35));n=math.sin(math.pi*t)**2*.9
            h=max(0,min(1,(z-1.78)/.15));h*=h*(3-2*h)
            remaining=1-n;head.add([v.index],total*remaining*h,'REPLACE');spine.add([v.index],total*remaining*(1-h),'REPLACE');neckgroup.add([v.index],total*n,'REPLACE');count+=1
    for mod in modifiers:mod.use_deform_preserve_volume=False
    Pose(rig).reset();bpy.context.view_layer.update()
    return dict(before_joints=before,after_joints={b.name:list(b.head_local) for b in rig.data.bones},
                eye_axis_before=list(axis),head_alignment_degrees=math.degrees(correction.angle),neck_vertices=count,
                method='Aligned the actual eye axis, rebuilt a separate neck joint and smooth neck binding, thickened the neck locally, and aligned shoulder/pelvis pivots before rebaking the preserved mesh.')
