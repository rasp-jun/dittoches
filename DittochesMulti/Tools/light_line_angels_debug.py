import bpy,json,sys
from pathlib import Path
base=Path(__file__).resolve().parents[1]/'ArtSource/LightLine-20261013'
out=[]
for ident in ['angewomon','ophanimon']:
    for anim in [False,True]:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        sources=list((base/ident/'source').rglob('*.fbx'))
        source=next(p for p in sources if ('animations' in p.name.lower())==anim)
        if anim:bpy.ops.import_scene.fbx(filepath=str(source))
        else:
            source=next((base/ident/'source').rglob('*.dae'))
            bpy.ops.wm.collada_import(filepath=str(source))
        rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
        if anim:
            a=next(a for a in bpy.data.actions if '|idle|' in a.name)
            rig.animation_data.action=a;rig.animation_data.action_slot=a.slots[0]
            bpy.context.scene.frame_set(15)
        out.append({'id':ident,'source':str(source),'rigmatrix':[list(v) for v in rig.matrix_world],
            'bones':[{'name':b.name,'head':list(b.bone.head_local),'tail':list(b.bone.tail_local),'loc':list(b.location),'rot':list(b.rotation_quaternion),'scale':list(b.scale),'posed_head':list(b.head)} for b in rig.pose.bones]})
(base/'angels-debug.json').write_text(json.dumps(out,indent=2))
