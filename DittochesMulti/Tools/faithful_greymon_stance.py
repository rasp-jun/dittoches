"""Repose the existing asymmetrical Greymon sculpt into a usable rest stance."""
import bpy
import numpy as np
from mathutils import Vector
from faithful_pose_math import Pose


def rebase(rig,meshes):
    from faithful_skin_topology import topology
    tongue_vertices=0
    for obj in meshes:
        xyz=np.empty(len(obj.data.vertices)*3,dtype=np.float32);obj.data.vertices.foreach_get('co',xyz)
        points,inverse,edges,labels=topology(obj.data,xyz.reshape(-1,3))
        for label in np.unique(labels):
            ids=np.flatnonzero(labels==label);center=points[ids].mean(0)
            if len(ids)==944 and 1.65<center[2]<1.8 and center[1]<-.5:
                vertices=np.flatnonzero(np.isin(inverse,ids)).tolist()
                for group in obj.vertex_groups:group.remove(vertices)
                obj.vertex_groups['Jaw'].add(vertices,1,'REPLACE');tongue_vertices+=len(vertices)
    assert tongue_vertices==944,'Expected original detached tongue component'
    p=Pose(rig);p.reset()
    before={b.name:list(b.head_local) for b in rig.data.bones}
    p.move('Hips',(0,0,.04))
    # Undo the lateral torso lean before placing the four limbs. Preserve the
    # dinosaur's forward chest angle rather than forcing a human vertical spine.
    for upper,child in [('Hips','Spine'),('Spine','Head')]:
        start=p.point(upper);end=p.point(child);end.x=start.x
        p.aim(upper,child,end)
    for side,suffix in [(-1,'L'),(1,'R')]:
        p.ik('Thigh'+suffix,'Shin'+suffix,'Foot'+suffix,(side*.49,-.22,.14),
             pole=(side*.25,-1,0),orientation=rig.data.bones['Foot'+suffix].matrix_local.to_quaternion())
        p.ik('UpperArm'+suffix,'Forearm'+suffix,'Hand'+suffix,(side*.54,-.65,1.14),pole=(side*1,.25,-.6))
        # Both claws point forward/down without keeping the source's raised palm.
        hand='Hand'+suffix;head=p.point(hand)
        current=p.world(hand).to_3x3()@Vector((0,1,0))
        p.rotate_quat(hand,current.rotation_difference(Vector((side*.12,-.72,-.68))))
    p.rotate('Jaw',x=-27)
    p.rotate('Tail',z=22,x=-5);p.rotate('TailTip',z=8,x=-5)
    modifiers=[m for obj in meshes for m in obj.modifiers if m.type=='ARMATURE']
    for modifier in modifiers:modifier.use_deform_preserve_volume=True
    bpy.context.view_layer.update();graph=bpy.context.evaluated_depsgraph_get()
    deformed={}
    for obj in meshes:
        evaluated=obj.evaluated_get(graph);mesh=evaluated.to_mesh()
        assert len(mesh.vertices)==len(obj.data.vertices),'Repose must preserve render topology'
        xyz=np.empty(len(mesh.vertices)*3,dtype=np.float32);mesh.vertices.foreach_get('co',xyz)
        deformed[obj.name]=xyz.reshape(-1,3).copy();evaluated.to_mesh_clear()
    lowest=min(float(v[:,2].min()) for v in deformed.values())
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
    bpy.ops.object.mode_set(mode='POSE');bpy.ops.pose.armature_apply(selected=False);bpy.ops.object.mode_set(mode='OBJECT')
    for obj in meshes:
        xyz=deformed[obj.name];xyz[:,2]-=lowest
        # Smooth the small compression folds introduced by unposing the old
        # bent ankles. Weld only for this calculation, retaining the render UVs.
        points,inverse,edges,labels=topology(obj.data,xyz)
        a,b=edges.T;degree=np.bincount(np.r_[a,b],minlength=len(points))
        x,y,z=points.T
        smooth=lambda t:np.clip(t,0,1)**2*(3-2*np.clip(t,0,1))
        mask=smooth((z-.23)/.10)*(1-smooth((z-.61)/.15))*smooth((np.abs(x)-.25)/.12)
        large=np.bincount(labels)[labels]>5000;mask*=large
        original=points.copy()
        for _ in range(18):
            for amount in (.38,-.40):
                neighbor=np.column_stack([(np.bincount(a,weights=points[b,k],minlength=len(points))+np.bincount(b,weights=points[a,k],minlength=len(points)))/np.maximum(degree,1) for k in range(3)])
                points+=(neighbor-points)*(mask*amount)[:,None]
        xyz=points[inverse].astype(np.float32)
        obj.data.vertices.foreach_set('co',xyz.ravel())
        if obj.data.has_custom_normals:obj.data.normals_split_custom_set([(0,0,0)]*len(obj.data.loops))
        obj.data.update()
    for modifier in modifiers:modifier.use_deform_preserve_volume=False
    bpy.ops.object.mode_set(mode='EDIT')
    for bone in rig.data.edit_bones:bone.head.z-=lowest;bone.tail.z-=lowest
    bpy.ops.object.mode_set(mode='OBJECT')
    # Keep the motion root at floor level after rebasing all anatomical bones.
    bpy.ops.object.mode_set(mode='EDIT')
    root=rig.data.edit_bones['Root'];root.head=(0,0,0);root.tail=(0,0,.2)
    bpy.ops.object.mode_set(mode='OBJECT');Pose(rig).reset();bpy.context.view_layer.update()
    return {'before_joints':before,'after_joints':{b.name:list(b.head_local) for b in rig.data.bones},
            'tongue_vertices_rebound_to_jaw':tongue_vertices,
            'method':'Reposed the original skin with volume preservation, smoothed compressed ankle folds, and applied the pose as rest; same vertices, faces and UVs; tongue binding corrected.'}
