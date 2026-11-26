"""Rebuild 34 earlier assets into an isolated review directory, with pose evidence."""
import sys,math,json,copy,argparse,hashlib
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import bpy,numpy as np
from mathutils import Vector,Quaternion
from faithful_rig_common import bounds,studio,render
from faithful_rig_profiles import skeleton
from animate_faithful_models import make_rig,reset,evaluated_bounds,mesh_positions
from faithful_skin_topology import refine,topology
from faithful_natural_motion import setup as setup_motion,pose as motion_pose
from faithful_technique_poses import setup as setup_technique,pose as technique_pose
from faithful_motion_catalog import clips_for
from roster_rebuild_profiles import PROFILES
from roster_rebuild_sources import NAMES,OUT,GAME
from roster_rebuild_prepare import surface
from trials_enemy_art import PROFILES as ENEMY_PROFILES
from faithful_pose_math import Pose
PROFILES.update(ENEMY_PROFILES)

def reopen_wings(ident,meshes):
    if ident!='holyangemon':return
    for obj in meshes:
        xyz=mesh_positions(obj.data);points,inverse,edges,labels=topology(obj.data,xyz)
        for label in np.unique(labels):
            ids=np.flatnonzero(labels==label);pts=points[ids];center=pts.mean(axis=0);span=np.ptp(pts,axis=0)
            if 700<len(ids)<900 and span[0]<.65 and span[1]>.4:
                side=1 if center[0]>0 else -1
                pivot=Vector((side*.10,.44,1.55 if center[2]>1.5 else 1.43))
                rotation=Quaternion((0,0,1),math.radians(side*65))
                for index in np.flatnonzero(np.isin(inverse,ids)):
                    v=obj.data.vertices[int(index)];v.co=pivot+rotation@(v.co-pivot)
        obj.data.update()

EXCLUDED={'nyaromon','plotmon','tailmon','angewomon','ophanimon'}
GALLERY=GAME/'ArtSource/FaithfulGallery'
SHA=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()

def assign(rig,action):
    rig.animation_data_create();rig.animation_data.action=action
    if action.slots:rig.animation_data.action_slot=action.slots[0]

def bind(ident,meshes,rig,p,defs):
    names=[d[0] for d in defs];index={n:i for i,n in enumerate(names)}
    starts=np.array([d[2] for d in defs]);ends=np.array([d[3] for d in defs]);axes=ends-starts
    lengths=np.sum(axes*axes,axis=1);total=0
    for obj in meshes:
        for n in names:obj.vertex_groups.new(name=n)
        xyz=mesh_positions(obj.data).astype(np.float64);x,y,z=xyz.T;ax=np.abs(x)
        delta=xyz[:,None,:]-starts[None,:,:];t=np.clip(np.sum(delta*axes[None,:,:],axis=2)/lengths[None,:],0,1)
        distance=np.sum((delta-t[:,:,None]*axes[None,:,:])**2,axis=2)
        allowed=np.zeros(distance.shape,dtype=bool)
        def region(mask,bones):
            allowed[mask]=False
            for n in bones:
                if n in index:allowed[mask,index[n]]=True
        region(np.ones(len(x),dtype=bool),['Hips','Spine','Head'])
        if p['kind']=='baby':
            region(np.ones(len(x),dtype=bool),['Head'])
            if 'ears' in p:
                for side,s in [(-1,'L'),(1,'R')]:region((x*side>.25)&(z>p.get('ear_cut',1.3)),['Ear'+s,'EarTip'+s,'Head'])
            if 'flower' in p:region(z>p.get('flower_cut',1.3),['Flower','Petals'])
            if 'arms' in p:
                for side,s in [(-1,'L'),(1,'R')]:region((x*side>p.get('arm_cut',.6))&(z>.4)&(z<1.45),['UpperArm'+s,'Forearm'+s,'Hand'+s])
        elif p['kind']=='quadruped':
            region((y<p['head_cut_y'])&(z>.62),['Head'])
            for side,s in [(-1,'L'),(1,'R')]:
                for prefix,front in [('Front',True),('Rear',False)]:
                    mask=(x*side>.18)&(z<p['leg_top'])&((y<-.2) if front else (y>.05))
                    region(mask,[prefix+'Upper'+s,prefix+'Lower'+s,prefix+'Foot'+s,'Spine' if front else 'Hips'])
                    region(mask&(z<.25),[prefix+'Foot'+s])
        else:
            region(z>p['head_cut'],['Head'])
            for side,s in [(-1,'L'),(1,'R')]:
                leg=(x*side>.06)&(z<p['leg_cut']);region(leg,['Hips','Thigh'+s,'Shin'+s,'Foot'+s])
                region(leg&(z<.22),['Foot'+s])
                arms=[n+s for n in ['UpperArm','Forearm','Hand','LowerArm','LowerForearm','LowerHand'] if n+s in index]
                legs=[n+s for n in ['Thigh','Shin','Foot'] if n+s in index]
                mask=(x*side>p['arm_cut'])&(z>.25)&(z<p.get('arm_top',p['head_cut']))
                if arms and legs:mask&=(z>p['leg_cut']+.13)|(distance[:,[index[n] for n in arms]].min(axis=1)<distance[:,[index[n] for n in legs]].min(axis=1))
                if arms:region(mask,arms+['Spine'])
        if 'tail' in p:
            region((y>p['tail'][0][1]+.08)&(ax<p.get('tail_width',.6)),['Tail','TailTip','Hips'])
        if 'wing_root' in p:
            for side,s in [(-1,'L'),(1,'R')]:
                mask=(x*side>p.get('wing_x',.18))&(y>p.get('wing_y',p['wing_root'][1]))&(z>p.get('wing_z',.85))
                region(mask,['Wing'+s])
                if 'extra_wings' in p:region(mask&(z<1.45),['LowerWing'+s])
        if p.get('shield'):region((y>.15)&(z>.75)&(ax<.66),['Spine'])
        if ident=='machinedramon':region(z>1.7,['Spine'])
        if ident=='leomon':
            close=distance[:,index['HandR']] < distance[:,index['Spine']]
            region((y<-.08)&(z>1.25)&(z<1.90)&close,['UpperArmR','ForearmR','HandR'])
        distance[~allowed]=1e8;weights=1/(distance+.012)**2;weights[~allowed]=0
        weights/=weights.sum(axis=1,keepdims=True)
        weights,_=refine('rebuild-'+ident,obj,xyz,weights,names,starts,ends)
        if ident in ('wargreymon','holyangemon','andromon','ogremon','leomon','piedmon','machinedramon'):
            points,inverse,edges,labels=topology(obj.data,xyz)
            for label in np.unique(labels):
                ids=np.flatnonzero(labels==label);pts=points[ids];center=pts.mean(axis=0);span=np.ptp(pts,axis=0);bone=None
                s='R' if center[0]>0 else 'L'
                if ident=='wargreymon' and 400<len(ids)<900 and abs(center[0])>.28:
                    bone=('UpperArm' if center[2]>1.6 else 'Hand')+s
                if ident=='holyangemon' and 700<len(ids)<950 and span[0]>.35:
                    bone=('Wing' if center[2]>1.5 else 'LowerWing')+s
                if ident=='ogremon' and center[0]<-.72 and center[1]<-.40 and center[2]>.9:bone='HandL'
                if ident=='leomon' and label in (46,53,61):bone='HandR'
                if ident=='andromon':
                    if abs(center[0])>.30 and .55<center[2]<2.05:
                        bone=('Hand' if center[2]<.93 else 'Forearm' if center[2]<1.45 else 'UpperArm')+s
                    if center[1]>.27 and center[2]>1.3 and abs(center[0])<.17:bone='Spine'
                if ident=='piedmon' and label in (0,3,5,6,7,8,9,10,11,12,13,14,15,22,23,24,25,26,27,28):bone='Spine'
                if ident=='machinedramon' and center[2]>1.7:bone='Spine'
                if bone:
                    mask=np.isin(inverse,ids);weights[mask]=0;weights[mask,index[bone]]=1
        nearest=np.argsort(-weights,axis=1)[:,:4];selected=np.take_along_axis(weights,nearest,axis=1);selected/=selected.sum(axis=1,keepdims=True)
        quant=np.rint(selected*255).astype(np.int16);quant[:,0]+=255-quant.sum(axis=1)
        assert (quant>=0).all() and (quant.sum(axis=1)==255).all()
        for k in range(4):
            for bone in range(len(names)):
                mask=(nearest[:,k]==bone)&(quant[:,k]>0)
                for value in np.unique(quant[mask,k]):obj.vertex_groups[bone].add(np.flatnonzero(mask&(quant[:,k]==value)).tolist(),int(value)/255,'REPLACE')
        obj.parent=rig;mod=obj.modifiers.new('Anatomical binding','ARMATURE');mod.object=rig
        total+=len(x)
    return dict(vertices=total,bones=len(names),maxInfluences=4,normalized=True)

def neutral(ident,rig,p,*args):
    reset(rig)
    if ident=='mochimon':
        pose=Pose(rig)
        for side,s in [(-1,'L'),(1,'R')]:pose.rotate('UpperArm'+s,y=side*40)
    bpy.context.view_layer.update()

def render_views(ident,rig,meshes,folder):
    scene=bpy.context.scene
    actions={a.name.split('|')[-1]:a for a in bpy.data.actions if a.slots}
    assign(rig,actions['Idle']);scene.frame_set(1);bpy.context.view_layer.update()
    camera=studio(meshes);scene.cycles.samples=10;scene.render.resolution_x=640;scene.render.resolution_y=640
    samples=[]
    for clip,u,name,pos in [('Idle',0,'preview',(3,-6,2.8)),('Idle',0,'front',(0,-8,1.2)),('Idle',0,'side',(8,0,1.2)),('Walk',.35,'walk',(3,-6,2.8)),('Attack',.43,'attack',(3,-6,2.8)),('Skill',.57,'skill',(3,-6,2.8))]:
        action=actions[clip];assign(rig,action);a,b=action.frame_range;frame=a+(b-a)*u
        scene.frame_set(int(frame),subframe=frame-int(frame));bpy.context.view_layer.update()
        lo,hi=evaluated_bounds(meshes);target=Vector((lo+hi)*.5);camera.data.ortho_scale=max(3.1,float(max(hi-lo))*1.18)
        render(folder/(name+'.png'),camera,target+Vector(pos)-Vector((0,0,1.2)),target)
        samples.append(dict(clip=clip,progress=u,min=lo.tolist(),max=hi.tolist()))
    assign(rig,actions['Idle']);scene.frame_set(1)
    return samples

def build(ident):
    assert ident not in EXCLUDED
    folder=OUT/ident;folder.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    fresh=ident in NAMES;source=folder/'static.glb' if fresh else GALLERY/ident/'model.glb'
    bpy.ops.import_scene.gltf(filepath=str(source))
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    if fresh:
        reopen_wings(ident,meshes)
        p=copy.deepcopy(PROFILES[ident]);defs=skeleton(p)
        # Author the launch socket at the twin barrel mouths, rigid with their mount.
        if ident=='machinedramon':defs.append(('Cannon','Spine',(0,-.46,2.31),(0,-.55,2.36)))
        rig=make_rig(ident,defs);binding=bind(ident,meshes,rig,p,defs)
        setup_motion(ident,rig,p,neutral);neutral(ident,rig,p);setup_technique(rig,p)
        scene=bpy.context.scene;scene.render.fps=30
        for name,duration,loop in clips_for(ident):
            action=bpy.data.actions.new(name);action.use_fake_user=True;assign(rig,action);frames=round(duration*30)
            for i in range(frames+1):
                scene.frame_set(i+1)
                if name in ('Attack','Skill'):technique_pose(ident,rig,p,name,i/frames)
                else:motion_pose(ident,rig,p,name,i/30,duration)
                bpy.context.view_layer.update();lo,_=evaluated_bounds(meshes)
                if lo[2]<0:
                    bone=rig.pose.bones['Root'];bone.location+=bone.bone.matrix_local.to_quaternion().inverted()@Vector((0,0,-float(lo[2])))
                for bone in rig.pose.bones:
                    for prop in ('location','rotation_quaternion','scale'):bone.keyframe_insert(prop,frame=i+1,group=bone.name)
    else:
        rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
        for obj in meshes:surface(obj)
        binding=dict(vertices=sum(len(o.data.vertices) for o in meshes),bones=len(rig.data.bones),maxInfluences=4,preservedAuthoredRig=True)
        for action in bpy.data.actions:action.name=action.name.split('|')[-1];action.use_fake_user=True
    actions={a.name.split('|')[-1]:a for a in bpy.data.actions if a.slots}
    assert set(['Idle','Walk','Run','Attack','Skill','Guard','Dodge','Hit','Victory','Down'])<=set(actions)
    assign(rig,actions['Idle']);bpy.context.scene.frame_set(1)
    # Runtime materials share a stable rough surface while retaining original paint.
    for material in bpy.data.materials:
        if material.use_nodes:
            for node in material.node_tree.nodes:
                if node.type=='BSDF_PRINCIPLED':node.inputs['Roughness'].default_value=.78
    bpy.ops.object.select_all(action='DESELECT')
    for obj in list(bpy.context.scene.objects):
        if obj.type in ('MESH','ARMATURE','EMPTY'):obj.select_set(True)
    bpy.context.view_layer.objects.active=rig
    bpy.ops.export_scene.gltf(filepath=str(folder/'model.glb'),export_format='GLB',use_selection=True,export_animations=True,
      export_animation_mode='ACTIONS',export_force_sampling=True,export_frame_range=False,export_anim_slide_to_zero=True,
      export_skins=True,export_all_influences=False,export_all_vertex_colors=True,export_yup=True)
    samples=render_views(ident,rig,meshes,folder)
    bpy.ops.wm.save_as_mainfile(filepath=str(folder/'model.blend'))
    record=json.loads((folder/'source/source.json' if fresh else GALLERY/ident/'source.json').read_text(encoding='utf-8-sig'))
    record['rebuild_20261013']=dict(source=str(source.relative_to(GAME)),sourceSha256=SHA(source),outputSha256=SHA(folder/'model.glb'),
      geometry='Original attributed surface rebuilt with UV-aware triangle joining, seam welding, feature creases and silhouette subdivision.',
      binding=binding,motion='Fresh anatomical rig and ten technique-aware clips' if fresh else 'Existing authored anatomy and clips retained on rebuilt surface',
      status='Unpublished review; requires visual, skinning and player verification',samples=samples)
    (folder/'source.json').write_text(json.dumps(record,ensure_ascii=False,indent=2),encoding='utf-8')
    print('REBUILT REVIEW',ident,binding,flush=True)

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--ids',required=True);args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
    for ident in args.ids.split(','):build(ident)
