"""Widen the actual upper-body mesh and shoulder pivots from a saved baseline.

Run in Blender. Source topology, UVs, textures and ten actions are retained.
Always reads the dated backup so rerunning never compounds the sculpt.
"""
import bpy,json,hashlib,sys,copy
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(ROOT/'Tools'))
from animate_faithful_models import evaluated_bounds
from faithful_motion_catalog import clips_for

BASE=ROOT/'ArtSource/MetalGreymonShoulderBackup-20261006'
OUT=ROOT/'ArtSource/AnimatedReview/metalgreymon'

def smooth(a,b,x):
    t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)

def main():
    bpy.ops.wm.open_mainfile(filepath=str(BASE/'review/metalgreymon.blend'))
    scene=bpy.context.scene;rig=next(o for o in scene.objects if o.type=='ARMATURE')
    meshes=[o for o in scene.objects if o.type=='MESH']
    rest={b.name:(b.head_local.copy(),b.tail_local.copy()) for b in rig.data.bones}
    offsets={}
    for side,s in [(-1,'L'),(1,'R')]:
        for prefix in ('UpperArm','Forearm','Hand'):offsets[prefix+s]=Vector((side*.19,.015,.070))
        offsets['Wing'+s]=Vector((side*.085,0,.035))
    changed=0;largest=0;head_error=0;leg_error=0
    for obj in meshes:
        names={g.index:g.name for g in obj.vertex_groups}
        for vert in obj.data.vertices:
            p=vert.co.copy();weights={names[g.group]:g.weight for g in vert.groups}
            delta=Vector((0,0,0))
            for name,shift in offsets.items():delta+=shift*weights.get(name,0)
            # Build the upper rib cage into the shoulder roots. The lower belly
            # keeps its taper and neither helmet nor feet are scaled globally.
            torso=weights.get('Spine',0)+weights.get('Hips',0)
            upper=smooth(.88,1.43,p.z)
            delta.x+=p.x*.46*torso*upper
            delta.y+=(p.y+.20)*.20*torso*upper
            delta.z+=.035*torso*upper
            # Thicken the upper arms radially around their original bone axes;
            # the mechanical arm has more mass without lengthening its claws.
            for name,amount in [('UpperArmL',.26),('UpperArmR',.34),('ForearmL',.12),('ForearmR',.14)]:
                weight=weights.get(name,0)
                if weight<1e-5:continue
                a,b=rest[name];axis=b-a
                t=max(0,min(1,(p-a).dot(axis)/axis.length_squared))
                radial=p-(a+axis*t)
                delta+=radial*amount*weight
            # Preserve fully attached face and feet exactly.
            if weights.get('Head',0)>.99999 or any(weights.get('Foot'+s,0)>.99999 for s in ('L','R')):delta=Vector()
            vert.co=p+delta;largest=max(largest,delta.length)
            if delta.length>1e-7:changed+=1
        obj.data.update()
    bpy.context.view_layer.objects.active=rig
    bpy.ops.object.mode_set(mode='EDIT')
    for name,delta in offsets.items():
        bone=rig.data.edit_bones[name];bone.head+=delta;bone.tail+=delta
    bpy.ops.object.mode_set(mode='OBJECT')
    scene.render.fps=30
    # The wider mechanical arm becomes the lowest contact during the fall.
    # Sample the original curve first so inserted keys cannot affect later samples.
    down=bpy.data.actions['Down'];rig.animation_data.action=down
    root=rig.pose.bones['Root'];grounding=[]
    for frame in range(round(down.frame_range[0]),round(down.frame_range[1])+1):
        scene.frame_set(frame);bpy.context.view_layer.update()
        lo,_=evaluated_bounds(meshes)
        lift=max(0,-float(lo[2]))
        grounding.append((frame,root.location.copy()+root.bone.matrix_local.to_quaternion().inverted()@Vector((0,0,lift)),lift))
    for frame,location,lift in grounding:
        root.location=location;root.keyframe_insert('location',frame=frame,group=root.name)
    for curve in down.fcurves:
        if curve.data_path=='pose.bones["Root"].location':
            for key in curve.keyframe_points:key.interpolation='LINEAR'
    samples=[]
    for name,duration,loop in clips_for('metalgreymon'):
        rig.animation_data.action=bpy.data.actions[name]
        for j in range(13):
            frame=round(j*round(duration*30)/12)+1;scene.frame_set(frame);bpy.context.view_layer.update()
            lo,hi=evaluated_bounds(meshes)
            samples.append(dict(clip=name,frame=frame-1,min=lo.tolist(),max=hi.tolist()))
    rig.animation_data.action=bpy.data.actions['Idle'];scene.frame_set(1);bpy.context.view_layer.update()
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
    for obj in meshes:obj.select_set(True)
    bpy.context.view_layer.objects.active=rig
    bpy.ops.export_scene.gltf(filepath=str(OUT/'model.glb'),export_format='GLB',use_selection=True,
        export_animations=True,export_animation_mode='ACTIONS',export_yup=True,
        export_all_vertex_colors=True,export_force_sampling=True,export_anim_slide_to_zero=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'metalgreymon.blend'))
    digest=hashlib.sha256((OUT/'model.glb').read_bytes()).hexdigest()
    report=json.loads((BASE/'review/animation-report.json').read_text(encoding='utf-8'))
    report.update(output_sha256=digest,pose_bounds=samples,preserved_noncombat_clips=False,
        refinement_pass=report.get('refinement_pass',0)+1,
        comparison_source='ArtSource/MetalGreymonShoulderBackup-20261006/gallery/model.glb',
        shape_revision='MetalGreymon broad rib cage, raised shoulders and arm mass / 2026-10-06',
        status='requires deformation and browser review')
    report['shape_changes'].append('Widened and deepened the upper rib cage, moved both shoulder chains outward/up, thickened upper-arm surfaces and shifted wing attachments; retained topology, UVs and ten motions, with Down root height corrected for the wider arm contact.')
    (OUT/'animation-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    source=json.loads((BASE/'gallery/source.json').read_text(encoding='utf-8'))
    source['local_modifications'].update(model_sha256=digest,shoulder_refinement=report['shape_revision'])
    (OUT/'source.json').write_text(json.dumps(source,ensure_ascii=False,indent=2),encoding='utf-8')
    sculpt=dict(id='metalgreymon',sha256=digest,baseline_sha256=hashlib.sha256((BASE/'gallery/model.glb').read_bytes()).hexdigest(),
        baseline=str(BASE.relative_to(ROOT)),changed_vertices=changed,max_rest_displacement=largest,
        shoulder_pivot_width_before=rest['UpperArmR'][0].x-rest['UpperArmL'][0].x,
        shoulder_pivot_width_after=rig.data.bones['UpperArmR'].head_local.x-rig.data.bones['UpperArmL'].head_local.x,
        shoulder_raise=.070,upper_chest_width_gain=.46,upper_chest_depth_gain=.20,
        down_max_ground_lift=max(row[2] for row in grounding),
        vertices=sum(len(o.data.vertices) for o in meshes),polygons=sum(len(o.data.polygons) for o in meshes))
    (OUT/'shoulder-report.json').write_text(json.dumps(sculpt,indent=2),encoding='utf-8')
    print('SHOULDER SCULPT',json.dumps(sculpt),flush=True)

if __name__=='__main__':main()
