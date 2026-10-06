"""Semantic animation controls on the preserved native Rosemon skin."""
import bpy
from faithful_rig_common import ROOT

def load_rosemon():
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/ExpandedSources-20261002/rosemon/rosemon.blend'))
    rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    for obj in list(bpy.context.scene.objects):
        if obj!=rig and obj not in meshes:bpy.data.objects.remove(obj,do_unlink=True)
    aliases={'Pelvis':'Hips','Spine2':'Spine','Head1':'Head'}
    for side in ('L','R'):
        target_side='R' if rig.data.bones['ValveBiped.Bip01_'+side+'_UpperArm'].head_local.x>0 else 'L'
        for original,target in [('UpperArm','UpperArm'),('Forearm','Forearm'),('Hand','Hand'),('Thigh','Thigh'),('Calf','Shin'),('Foot','Foot')]:
            aliases[side+'_'+original]=target+target_side
    for original,target in aliases.items():rig.data.bones['ValveBiped.Bip01_'+original].name=target
    bpy.context.view_layer.objects.active=rig;rig.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    root=rig.data.edit_bones.new('Root');root.head=(0,0,0);root.tail=(0,0,.2);root.use_deform=False
    for bone in rig.data.edit_bones:
        if bone!=root and bone.parent is None:bone.parent=root
    bpy.ops.object.mode_set(mode='OBJECT')
    for bone in rig.pose.bones:bone.rotation_mode='QUATERNION'
    # Renaming Blender bones also updates the skinned vertex groups.
    for obj in meshes:
        assert all(g.name in rig.data.bones for g in obj.vertex_groups)
    binding={'weighted_vertices':sum(len(o.data.vertices) for o in meshes),
             'method':'Original game-derived skeletal weights retained; semantic controls added without rebinding geometry',
             'max_influences':max(len(v.groups) for o in meshes for v in o.data.vertices)}
    return rig,meshes,binding,['Retained the native Rosemon rig, finger weights and facial shape keys; selected normal form and portable original texture materials.']
