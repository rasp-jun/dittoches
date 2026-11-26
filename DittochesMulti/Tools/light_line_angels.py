"""Preserve original Links DAE geometry, UVs, weights and retarget its FBX motion.

The public animated FBX has an incorrect inch/cm centre bind transform in Blender.
The DAE has the correct bind pose. Only bone-local rotations and relative centre
motion are transferred; structural .3937008 scale and shifted bind translation
are deliberately excluded. No generated primitive replaces source geometry.
"""
import bpy,sys,json,math,hashlib
from pathlib import Path
from mathutils import Vector,Quaternion,Matrix
sys.path.insert(0,str(Path(__file__).resolve().parent))
from faithful_rig_common import bounds,studio,render
ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/'ArtSource/LightLine-20261013'
CLIPS=[('Idle','idle',2.0),('Walk','move',40/30),('Run','move',26/30),('Attack','attack01',40/30),('Skill','special01',2.4),('Hit','damage',.8),('Victory','win',3.0),('Guard','guard',1.4),('Dodge','idle',.9),('Down','down',1.5)]

def assign(rig,action):
    rig.animation_data_create();rig.animation_data.action=action
    if action.slots:rig.animation_data.action_slot=action.slots[0]

def native_motion(folder):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    source=next(p for p in (folder/'source').rglob('*.fbx') if 'animations' in p.name.lower())
    bpy.ops.import_scene.fbx(filepath=str(source))
    rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
    result={}
    for name,source_name,duration in CLIPS:
        action=next(a for a in bpy.data.actions if '|'+source_name+'|' in a.name and not a.name.endswith('.001'))
        assign(rig,action)
        lo,hi=action.frame_range
        samples=[]
        for index in range(round(duration*30)+1):
            t=index/round(duration*30)
            f=lo+(hi-lo)*t
            bpy.context.scene.frame_set(int(f),subframe=f-int(f));bpy.context.view_layer.update()
            samples.append({b.name:dict(rotation=list(b.rotation_quaternion),location=list(b.location),scale=list(b.scale)) for b in rig.pose.bones})
        result[name]=dict(source_action=action.name,duration=duration,samples=samples)
    return result

def main(ident):
    folder=BASE/ident;motion=native_motion(folder)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    dae=next((folder/'source').rglob('*.dae'))
    bpy.ops.wm.collada_import(filepath=str(dae),fix_orientation=False,find_chains=False,auto_connect=False)
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
    rig.name=ident+'_Rig'
    for image in bpy.data.images:
        name=Path(image.filepath.replace('\\','/')).name
        match=list((folder/'source').rglob(name))
        if match:image.filepath=str(match[0]);image.reload();image.pack()
    for obj in meshes:
        for polygon in obj.data.polygons:polygon.use_smooth=True
        for mat in obj.data.materials:
            mat.use_nodes=True;nodes=mat.node_tree.nodes
            p=next((n for n in nodes if n.type=='BSDF_PRINCIPLED'),None)
            if p:
                p.inputs['Roughness'].default_value=.78;p.inputs['Metallic'].default_value=0
                tex=next((n for n in nodes if n.type=='TEX_IMAGE'),None)
                if tex:
                    mat.node_tree.links.new(tex.outputs['Color'],p.inputs['Base Color'])
                    mat.node_tree.links.new(tex.outputs['Color'],p.inputs['Emission Color']);p.inputs['Emission Strength'].default_value=.08
                    # Original source uses polygon silhouettes, not alpha feathers.
                mat.diffuse_color=(1,1,1,1)
    bpy.context.view_layer.update()
    lo,hi=bounds(meshes)
    root=bpy.data.objects.new(ident,None);bpy.context.scene.collection.objects.link(root)
    for obj in list(bpy.context.scene.objects):
        if obj!=root and obj.parent is None:obj.parent=root
    scale=2.4/(hi.z-lo.z)
    root.scale=(scale,)*3;root.location=(-(lo.x+hi.x)*.5*scale,-(lo.y+hi.y)*.5*scale,-lo.z*scale)
    actions={};grip_reference={}
    for name,source_name,duration in CLIPS:
        action=bpy.data.actions.new(name);action.use_fake_user=True;assign(rig,action)
        record=motion[name];frames=len(record['samples'])-1
        base_center=Vector(record['samples'][0]['J_center']['location'])
        for index,sample in enumerate(record['samples']):
            t=index/frames;pulse=math.sin(math.pi*t)**2
            for bone in rig.pose.bones:
                data=sample.get(bone.name)
                bone.rotation_mode='QUATERNION';bone.location=(0,0,0);bone.rotation_quaternion=(1,0,0,0);bone.scale=(1,1,1)
                if data:
                    bone.rotation_quaternion=Quaternion(data['rotation']).normalized()
                    if bone.name.startswith(('J_','IG_','md149_')):
                        bone.location=Vector(data['location'])/2.54
                    if bone.name in ('md149_yari','md149_tate'):
                        # These independently animated prop controls were
                        # exported in metre units, unlike the body hierarchy.
                        bone.location=Vector(data['location'])
                    if bone.name=='J_center':
                        # Native FBX centre coordinates are 2.54 times DAE units.
                        bone.location=(Vector(data['location'])-base_center)/2.54
                    if bone.name=='J_bow_C':bone.scale=[max(.0001,v) for v in data['scale']]
                if name=='Dodge' and bone.name=='J_center':
                    bone.location.x+=.16*pulse;bone.location.y-=.07*pulse
                    bone.rotation_quaternion=Quaternion((0,0,1),-.22*pulse)@bone.rotation_quaternion
                if name=='Run' and bone.name=='J_center':
                    bone.rotation_quaternion=Quaternion((1,0,0),-.10)@bone.rotation_quaternion
                bone.keyframe_insert('location',frame=index+1,group=bone.name)
                bone.keyframe_insert('rotation_quaternion',frame=index+1,group=bone.name)
                bone.keyframe_insert('scale',frame=index+1,group=bone.name)
            if ident=='ophanimon':
                scene=bpy.context.scene;scene.frame_set(index+1);bpy.context.view_layer.update()
                for prop,hand,offset in [('md149_yari','J_hand_R',(-.02,0,0)),('md149_tate','J_wrist_L',(.035,0,.07))]:
                    control=rig.pose.bones[prop];grip=rig.pose.bones[hand]
                    if hand not in grip_reference:grip_reference[hand]=grip.matrix.to_quaternion()
                    # Source props have independent incompatible root units.
                    # Match their original meshes to the corresponding hand,
                    # preserving native hand motion and a correct weapon axis.
                    rotation=grip.matrix.to_quaternion()@grip_reference[hand].inverted()@Quaternion((1,0,0),(-1 if prop=='md149_yari' else 1)*math.pi/2)
                    control.matrix=Matrix.Translation(grip.matrix@Vector(offset))@rotation.to_matrix().to_4x4()
                    for path in ['location','rotation_quaternion','scale']:control.keyframe_insert(path,frame=index+1,group=prop)
        for curve in action.fcurves:
            for key in curve.keyframe_points:key.interpolation='LINEAR'
        actions[name]=action
    scene=bpy.context.scene;scene.render.fps=30;scene.frame_start=1;scene.frame_end=61
    assign(rig,actions['Idle']);scene.frame_set(1);bpy.context.view_layer.update()
    # The DAE import's bind-preview bounds do not include the evaluated skin.
    # Normalize the actual evaluated idle skin once, keeping every clip on the
    # same root transform rather than independently centring each animation.
    root.scale=(1,1,1);root.location=(0,0,0);bpy.context.view_layer.update()
    graph=bpy.context.evaluated_depsgraph_get()
    points=[o.matrix_world@Vector(p) for mesh in meshes for o in [mesh.evaluated_get(graph)] for p in o.bound_box]
    lo=Vector([min(v[i] for v in points) for i in range(3)]);hi=Vector([max(v[i] for v in points) for i in range(3)])
    scale=2.4/(hi.z-lo.z);root.scale=(scale,)*3
    root.location=(-(lo.x+hi.x)*.5*scale,-(lo.y+hi.y)*.5*scale,-lo.z*scale)
    print('NORMALIZED',ident,list(lo),list(hi),scale,flush=True)
    bpy.context.view_layer.update()
    cam=studio(meshes);scene.cycles.samples=12;scene.render.resolution_x=768;scene.render.resolution_y=768
    render(folder/'model-front.png',cam,(0,-8,1.4))
    render(folder/'model-side.png',cam,(8,0,1.4))
    render(folder/'model-back.png',cam,(0,8,1.4))
    render(folder/'model-preview.png',cam,(3,-6,2.9))
    bpy.ops.wm.save_as_mainfile(filepath=str(folder/'model.blend'))
    for name in ['Attack','Skill','Guard','Down']:
        assign(rig,actions[name]);scene.frame_set(round(actions[name].frame_range[1]*.55))
        bpy.context.view_layer.update();graph=bpy.context.evaluated_depsgraph_get()
        pts=[o.matrix_world@Vector(p) for mesh in meshes for o in [mesh.evaluated_get(graph)] for p in o.bound_box]
        low=Vector([min(v[i] for v in pts) for i in range(3)]);high=Vector([max(v[i] for v in pts) for i in range(3)])
        target=(low+high)*.5;cam.data.ortho_scale=max(3.1,max(high-low)*1.25)
        render(folder/('motion-'+name.lower()+'.png'),cam,target+Vector((3,-6,2.1)),target)
    assign(rig,actions['Idle']);scene.frame_set(1)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in [root,rig]+meshes:obj.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(folder/'model.glb'),export_format='GLB',use_selection=True,export_animations=True,export_animation_mode='ACTIONS',export_force_sampling=True,export_frame_range=False,export_optimize_animation_size=True,export_skins=True,export_all_influences=False,export_yup=True)
    record={'id':ident,'source_url':'https://models.spriters-resource.com/mobile/digimonlinks/asset/'+('303995' if ident=='angewomon' else '304164')+'/',
        'source_game':'Digimon Links','uploaded_by':'Hallow','original_creator':'Original game creators; individual artist not identified by source',
        'provenance':'Public game-derived source, not original geometry authored by this project. Original ZIP/DAE/FBX/SMD/PNG retained.',
        'license':'Redistribution permission not established; retained for this local project. No external publication performed.',
        'official_reference':'https://digimon.net/reference_en/detail.php?directory_name='+('angewomon' if ident=='angewomon' else 'ofanimon'),
        'date':'2026-10-13','geometry':'Original DAE meshes, UVs and skin weights retained without simplification; normalized to 2.4 scene units.',
        'motion':'Original FBX bone rotations retargeted onto matching original DAE bind skeleton; erroneous FBX .3937008 centre-scale and static bind offset excluded. Ophanimon original prop meshes are aligned to the matching animated hands because FBX prop controls use incompatible independent root units; no weapon geometry replacement. Angewomon hidden bow scale clamped to .0001 to avoid singular bind matrices. Walk/Run use native flying move with distinct speed; Dodge adds authored lateral evasion to native idle.',
        'clips':[{'name':n,'source':s,'duration':d} for n,s,d in CLIPS],
        'files':[{'path':str(p.relative_to(folder)).replace('\\','/'),'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted((folder/'source').rglob('*')) if p.is_file()]}
    (folder/'source.json').write_text(json.dumps(record,ensure_ascii=False,indent=2),encoding='utf-8')
    print('ANGEL COMPLETE',ident,flush=True)

for ident in (sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else ['angewomon','ophanimon']):main(ident)
