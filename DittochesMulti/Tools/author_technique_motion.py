"""Author only Attack and Skill on the inspected rig, retaining eight actions."""
import sys,json,copy,hashlib,argparse,shutil
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import bpy
from mathutils import Vector
from faithful_rig_common import ROOT,OUT
from animate_faithful_models import evaluated_bounds
from faithful_rig_profiles import PROFILES
from faithful_motion_catalog import BASELINE,clips_for
from faithful_techniques import TECHNIQUES,REVISION
from faithful_technique_poses import setup,pose
from merge_technique_clips import merge

def author(ident):
    saved=ROOT/BASELINE/'review'/ident;gallery=ROOT/BASELINE/'gallery'/ident;folder=OUT/ident;folder.mkdir(exist_ok=True)
    if ident=='metalgreymon':
        sculpted=ROOT/'ArtSource/MetalGreymonShoulderApproved-20261006'
        if sculpted.exists():saved=sculpted/'review';gallery=sculpted/'gallery'
        elif (folder/'shoulder-report.json').exists():
            raise RuntimeError('Missing approved shoulder baseline; restore it before reauthoring MetalGreymon techniques')
    bpy.ops.wm.open_mainfile(filepath=str(saved/(ident+'.blend')))
    rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    previous=json.loads((saved/'animation-report.json').read_text(encoding='utf-8'))
    scene=bpy.context.scene;scene.render.fps=30
    rig.animation_data.action=bpy.data.actions['Idle'];scene.frame_set(1);bpy.context.view_layer.update()
    profile=copy.deepcopy(PROFILES[ident]);setup(rig,profile)
    rig.animation_data_clear()
    for action in list(bpy.data.actions):
        if action.name in ('Attack','Skill'):bpy.data.actions.remove(action)
    samples=[s for s in previous.get('pose_bounds',[]) if s['clip'] not in ('Attack','Skill')]
    for name,duration,loop in clips_for(ident):
        if name not in ('Attack','Skill'):continue
        rig.animation_data_create();action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action;frames=round(duration*30)
        for i in range(frames+1):
            scene.frame_set(i+1);pose(ident,rig,profile,name,i/frames)
            bpy.context.view_layer.update();lo,hi=evaluated_bounds(meshes)
            if lo[2]<0:
                root=rig.pose.bones['Root'];root.location+=root.bone.matrix_local.to_quaternion().inverted()@Vector((0,0,-float(lo[2])))
            if i in {round(j*frames/12) for j in range(13)}:samples.append(dict(clip=name,frame=i,min=lo.tolist(),max=hi.tolist()))
            for bone in rig.pose.bones:
                for prop in ('location','rotation_quaternion','scale'):bone.keyframe_insert(prop,frame=i+1,group=bone.name)
        if hasattr(action,'fcurves'):
            for fc in action.fcurves:
                for key in fc.keyframe_points:key.interpolation='LINEAR'
    rig.animation_data.action=bpy.data.actions['Idle'];scene.frame_set(1)
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
    for obj in meshes:obj.select_set(True)
    bpy.context.view_layer.objects.active=rig
    exported=folder/'technique-export.glb'
    bpy.ops.export_scene.gltf(filepath=str(exported),export_format='GLB',use_selection=True,export_animations=True,export_animation_mode='ACTIONS',export_yup=True,export_all_vertex_colors=True,export_force_sampling=True,export_anim_slide_to_zero=True)
    digest=merge(gallery/'model.glb',exported,folder/'model.glb')
    bpy.ops.wm.save_as_mainfile(filepath=str(folder/(ident+'.blend')))
    report=copy.deepcopy(previous);report.update(output_sha256=digest,refinement_pass=previous.get('refinement_pass',1)+1,motion_revision=REVISION,
        comparison_source=str((gallery/'model.glb').relative_to(ROOT)).replace('\\','/'),pose_bounds=samples,techniques=TECHNIQUES[ident],
        preserved_noncombat_clips=True,motion_provenance='Local choreography based on official technique descriptions; not extracted game animations',status='requires deformation and browser review')
    report['clips']=[dict(name=n,duration=duration,loop=loop,frames=round(duration*30)+1) for n,duration,loop in clips_for(ident)]
    report['shape_changes'].append('Replaced only Attack and Skill with technique-specific choreography; preserved all original mesh/skin/texture bytes and the other eight common animation definitions and buffers.')
    (folder/'animation-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    source=json.loads((gallery/'source.json').read_text(encoding='utf-8'));source['local_modifications'].update(model_sha256=digest,motion_provenance=report['motion_provenance'],technique_source=TECHNIQUES[ident]['source'])
    (folder/'source.json').write_text(json.dumps(source,ensure_ascii=False,indent=2),encoding='utf-8')
    for name in ('preview.png','stance-report.json'):
        if (gallery/name).exists():shutil.copy2(gallery/name,folder/name)
    print('TECHNIQUES AUTHORED',ident,flush=True)

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--ids',default=','.join(PROFILES));args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    for ident in args.ids.split(','):author(ident)
