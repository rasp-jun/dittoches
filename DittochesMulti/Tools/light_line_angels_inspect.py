"""Inspect untouched, public Digimon Links FBXs in an isolated workspace."""
import bpy, sys, json
from pathlib import Path
from mathutils import Vector
sys.path.insert(0, str(Path(__file__).resolve().parent))
from faithful_rig_common import bounds, studio, render
ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/'ArtSource/LightLine-20261013'
ids=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else ['angewomon','ophanimon']
for ident in ids:
    folder=BASE/ident
    source=next((folder/'source').rglob('*[Aa]nimations.fbx'))
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(source), use_anim=True)
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    rigs=[o for o in bpy.context.scene.objects if o.type=='ARMATURE']
    rows={'source':str(source), 'objects':[], 'actions':[], 'images':[]}
    for obj in bpy.context.scene.objects:
        row={'name':obj.name,'type':obj.type,'location':list(obj.location),'scale':list(obj.scale)}
        if obj.type=='ARMATURE':
            row['bones']=[{'name':b.name,'parent':b.parent.name if b.parent else None,'head':list(b.head_local),'tail':list(b.tail_local)} for b in obj.data.bones]
        if obj.type=='MESH':row.update(vertices=len(obj.data.vertices),faces=len(obj.data.polygons),materials=[m.name for m in obj.data.materials],groups=[g.name for g in obj.vertex_groups])
        rows['objects'].append(row)
    for a in bpy.data.actions:
        rows['actions'].append({'name':a.name,'range':list(a.frame_range),'curves':len(a.fcurves),'channels':[f.data_path for f in a.fcurves][:30]})
    for im in bpy.data.images:rows['images'].append({'name':im.name,'path':im.filepath,'size':list(im.size)})
    (folder/'source-inspection.json').write_text(json.dumps(rows,indent=2),encoding='utf-8')
    for rig in rigs:
        rig.animation_data_clear()
        for bone in rig.pose.bones:
            bone.location=(0,0,0);bone.rotation_quaternion=(1,0,0,0);bone.scale=(1,1,1)
        rig.animation_data_create()
        rig.animation_data.action=next(a for a in bpy.data.actions if '|idle|' in a.name)
        if rig.animation_data.action.slots:rig.animation_data.action_slot=rig.animation_data.action.slots[0]
    bpy.context.scene.frame_set(1)
    bpy.context.view_layer.update()
    lo,hi=bounds(meshes)
    print(ident, 'bounds',list(lo),list(hi),'actions',rows['actions'],flush=True)
    # Normalize whole root hierarchy for inspection; original mesh and weights remain untouched.
    root=bpy.data.objects.new('ReviewScale',None);bpy.context.scene.collection.objects.link(root)
    for obj in list(bpy.context.scene.objects):
        if obj!=root and obj.parent is None:obj.parent=root
    scale=2.4/(hi.z-lo.z);root.scale=(scale,)*3
    root.location=(-(lo.x+hi.x)*.5*scale,-(lo.y+hi.y)*.5*scale,-lo.z*scale)
    for im in bpy.data.images:
        if im.source=='FILE':
            name=Path(im.filepath.replace('\\','/')).name
            candidates=list((folder/'source').rglob(name))
            if candidates:im.filepath=str(candidates[0]);im.reload();im.pack()
    for m in bpy.data.materials:
        if m.use_nodes:
            p=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
            if p:p.inputs['Roughness'].default_value=.8;p.inputs['Metallic'].default_value=0
    bpy.context.view_layer.update()
    cam=studio(meshes);bpy.context.scene.cycles.samples=8
    bpy.context.scene.render.resolution_x=768;bpy.context.scene.render.resolution_y=768
    render(folder/'source-front.png',cam,(0,-8,1.4))
    render(folder/'source-side.png',cam,(8,0,1.4))
    render(folder/'source-back.png',cam,(0,8,1.4))
    bpy.ops.wm.save_as_mainfile(filepath=str(folder/'source-inspection.blend'))
