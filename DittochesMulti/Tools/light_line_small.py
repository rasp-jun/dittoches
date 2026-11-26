"""Preserve, rig and animate the original textured small light-line models.

Independent additions only. Reuses inspected Faithful export/binding primitives,
never creates substitute assembled body shapes or changes the prior 34 species.
Run with Blender --background --python Tools/light_line_small.py -- --prepare.
"""
import sys, json, math, argparse, hashlib
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import bpy, numpy as np
from mathutils import Vector, Matrix, Euler
from faithful_rig_common import bounds, studio, render
from faithful_rig_profiles import skeleton
from animate_faithful_models import make_rig,reset,pose as neutral_pose,evaluated_bounds
from faithful_natural_motion import setup as natural_setup,pose as natural_pose
from faithful_skin_topology import refine,topology
from faithful_motion_styles import GROUPS,curve
from faithful_motion_personality import MOTIFS,pulse
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'ArtSource/LightLine-20261013'
IDS=['nyaromon','plotmon','tailmon']
PROFILES={
 'nyaromon':dict(kind='baby',hip=(0,0,.22),chest=(0,0,.7),head=(0,-.2,1.0),head_top=(0,-.2,1.5),
   ears=[(.76,-.08,1.63),(1.0,-.02,2.02),(1.32,-.08,2.36)],
   tail=[(0,1.08,.75),(.43,1.95,.75),(1.43,2.12,.75)]),
 'plotmon':dict(kind='quadruped',hip=(0,.60,.76),chest=(0,-.20,.85),head=(0,-.16,1.17),head_top=(0,-.16,2.02),
   head_cut_y=-.75,leg_top=.76,front=[(.28,-.27,.73),(.29,-.28,.39),(.30,-.38,.13)],
   rear=[(.30,.65,.73),(.32,.68,.39),(.34,.57,.12)],
   ears=[(.60,-.18,2.05),(.84,-.17,1.54),(1.01,-.19,.98)],step_scale=.68),
 'tailmon':dict(kind='biped',hip=(0,0,.45),chest=(0,-.01,.85),head=(0,-.08,1.18),head_top=(0,-.10,1.67),
   head_cut=1.12,arm_cut=.29,leg_cut=.47,arm_top=1.07,
   arms=[(.25,-.03,1.01),(.41,-.08,.70),(.53,-.12,.44)],hand_tip=(.67,-.19,.15),
   legs=[(.16,.02,.46),(.18,.01,.27),(.20,-.05,.11)],
   ears=[(.35,-.02,1.65),(.81,.04,1.92),(1.28,.09,2.32)],
   tail=[(0,.23,.61),(-.38,.68,1.38),(-1.17,.95,1.61)],step_scale=.66),
}
for group,ident in [('baby','nyaromon'),('quadruped','plotmon'),('light','tailmon')]:GROUPS[group].add(ident)
MOTIFS.update(nyaromon='bounce',plotmon='pounce',tailmon='combo')
CLIPS=[('Idle',4.0,True),('Walk',1.2,True),('Run',.8,True),('Attack',1.3,False),('Skill',2.0,False),
       ('Hit',.8,False),('Victory',3.2,False),('Guard',1.8,False),('Dodge',1.0,False),('Down',2.2,False)]

def curve_tail(s):
    a=max(0,s)/1.12
    return Vector((1.12*(1-math.cos(a)),1.08+1.12*math.sin(a),.75))

def refine_source(ident,meshes):
    changes=[]
    offset={'nyaromon':(0,1.25,0),'plotmon':(-.025,0,0),'tailmon':(-.19548,.30,0)}[ident]
    for obj in meshes:
        # UV-seam duplicates are welded geometrically, while per-corner UVs stay intact.
        bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
        bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.remove_doubles(threshold=.00001);bpy.ops.object.mode_set(mode='OBJECT')
        for v in obj.data.vertices:
            v.co+=Vector(offset)
            if ident=='nyaromon' and v.co.y>1.08:
                x,y,z=v.co;arc=(y-1.08)/1.12;center=curve_tail(y-1.08)
                v.co=center+Vector((x*math.cos(arc),-x*math.sin(arc),z-.75))
        if obj.data.has_custom_normals:obj.data.normals_split_custom_set([(0,0,0)]*len(obj.data.loops))
        if ident=='nyaromon':
            # One subdivision of this original source rounds its coarse silhouette.
            mod=obj.modifiers.new('Preserved-source silhouette refinement','SUBSURF');mod.levels=1
            bpy.ops.object.modifier_apply(modifier=mod.name)
        obj.data.update()
    changes.append('Recentered original anatomy, welded coincident position seams retaining corner UVs and rebuilt smooth normals.')
    if ident=='nyaromon':changes.append('Curved the original straight tail around its length into the official curled silhouette; one original-surface subdivision improves the coarse head outline.')
    if ident=='plotmon':
        image=next(n.image for m in bpy.data.materials if m.use_nodes for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image)
        px=np.array(image.pixels[:],dtype=np.float32).reshape(-1,4)
        mask=(px[:,2]>px[:,0]*1.3)&(px[:,2]>px[:,1]*1.05)&(px[:,2]>.14)
        # Retain painted pupil/highlights and only shift blue iris pigment to green.
        old=px[mask,:3].copy();px[mask,0]=old[:,0]*.72;px[mask,1]=old[:,2]*.92;px[mask,2]=old[:,1]*.73
        image.pixels[:]=px.ravel();image.filepath_raw=str(OUT/ident/'diffuse-faithful.png');image.file_format='PNG';image.save()
        # Use a fresh image datablock: repacking an already packed source can
        # retain its old blue-iris bytes despite the live viewport showing green.
        corrected=bpy.data.images.load(str(OUT/ident/'diffuse-faithful.png'),check_existing=False);corrected.pack()
        for material in bpy.data.materials:
            if material.use_nodes:
                for node in material.node_tree.nodes:
                    if node.type=='TEX_IMAGE' and node.image==image:node.image=corrected
        changes.append('Shifted only the original blue iris pigment to the official green, keeping UVs, highlights, pupil, cheeks and Holy Ring lettering.')
    bpy.context.view_layer.update()
    return changes

def bind(ident,meshes,rig,defs):
    names=[d[0] for d in defs];index={n:i for i,n in enumerate(names)}
    starts=np.array([d[2] for d in defs]);ends=np.array([d[3] for d in defs]);axes=ends-starts
    details={};count=0;rigid=0
    for obj in meshes:
        xyz=np.array([tuple(v.co) for v in obj.data.vertices]);x,y,z=xyz.T;ax=abs(x)
        to=xyz[:,None,:]-starts[None,:,:]
        t=np.clip(np.sum(to*axes[None,:,:],axis=2)/np.sum(axes*axes,axis=1)[None,:],0,1)
        dist=np.sum((to-t[:,:,None]*axes[None,:,:])**2,axis=2)
        allowed=np.zeros(dist.shape,dtype=bool)
        def assign(mask,bones):
            allowed[mask]=False
            for bone in bones:
                if bone in index:allowed[mask,index[bone]]=True
        assign(np.ones(len(x),dtype=bool),['Head'] if ident=='nyaromon' else ['Hips','Spine'])
        if ident=='plotmon':
            assign(z>1.10,['Head'])
            for sign,s in [(-1,'L'),(1,'R')]:
                for prefix,mask in [('Front',(y<.13)),('Rear',(y>=.13))]:
                    limb=(x*sign>.12)&(z<.75)&mask
                    assign(limb,[prefix+'Upper'+s,prefix+'Lower'+s,prefix+'Foot'+s])
                    assign(limb&(z<.19),[prefix+'Foot'+s])
                assign((x*sign>.65)&(y>-.38)&(y<.23)&(z>.90),['Head','Ear'+s,'EarTip'+s])
        elif ident=='tailmon':
            assign(z>1.10,['Head'])
            for sign,s in [(-1,'L'),(1,'R')]:
                assign((x*sign>.04)&(z<.48),['Hips','Thigh'+s,'Shin'+s,'Foot'+s])
                assign((x*sign>.04)&(z<.19),['Foot'+s])
                assign((x*sign>.28)&(z<1.05)&((z>.35)|(x*sign>.48)),['UpperArm'+s,'Forearm'+s,'Hand'+s])
                assign((x*sign>.46)&(z<.63),['Hand'+s])
                assign((x*sign>.04)&(x*sign<.44)&(z<.28),['Foot'+s])
                assign((x*sign>.36)&(z>1.63),['Head','Ear'+s,'EarTip'+s])
            assign((y>.36)&(z>.57)&(z<1.76),['Tail','TailTip','TailEnd'])
        else:
            for sign,s in [(-1,'L'),(1,'R')]:assign((x*sign>.7)&(z>1.67),['Head','Ear'+s,'EarTip'+s])
            assign(y>1.10,['Tail','TailTip','TailEnd'])
        dist[~allowed]=1e8;weights=1/(dist+.015)**2;weights[~allowed]=0;weights/=weights.sum(axis=1,keepdims=True)
        weights,details[obj.name]=refine(ident,obj,xyz,weights,names,starts,ends)
        if ident=='tailmon':
            # Skin diffusion must not let the low hanging gloves pull toe vertices.
            for sign,s in [(-1,'L'),(1,'R')]:
                foot=(x*sign>.015)&(x*sign<.44)&(z<.29)
                weights[foot]=0;weights[foot,index['Foot'+s]]=1
            # The ear tassels lie behind the head but are never tail geometry.
            ear=z>1.78
            for bone in ('Tail','TailTip','TailEnd'):weights[ear,index[bone]]=0
            weights/=weights.sum(axis=1,keepdims=True)
        # Keep the inscriptions on both holy rings as a rigid, intact accessory.
        points,inv,edges,labels=topology(obj.data,xyz)
        for label in np.unique(labels):
            ids=np.flatnonzero(labels==label);pts=points[ids];span=np.ptp(pts,axis=0);center=pts.mean(axis=0)
            bone=None
            if ident=='plotmon' and len(ids)==48:bone='Head'
            if ident=='tailmon' and len(ids)==32 and center[1]>.7:bone='TailTip'
            if bone:
                selected=np.isin(inv,ids);weights[selected]=0;weights[selected,index[bone]]=1
        for name in names:obj.vertex_groups.new(name=name)
        nearest=np.argsort(-weights,axis=1)[:,:4];top=np.take_along_axis(weights,nearest,axis=1);top/=top.sum(axis=1,keepdims=True)
        quant=np.rint(top*255).astype(np.int16);quant[:,0]+=255-quant.sum(axis=1)
        assert np.all(quant>=0) and np.all(quant.sum(axis=1)==255)
        for j in range(4):
            for bone in range(len(names)):
                used=(nearest[:,j]==bone)&(quant[:,j]>0)
                for value in np.unique(quant[used,j]):obj.vertex_groups[bone].add(np.flatnonzero(used&(quant[:,j]==value)).tolist(),int(value)/255,'REPLACE')
        obj.parent=rig;mod=obj.modifiers.new('Faithful anatomical skin','ARMATURE');mod.object=rig;mod.use_deform_preserve_volume=False
        count+=len(xyz);rigid+=int(np.count_nonzero(quant[:,0]==255))
    return dict(weighted_vertices=count,rigid_vertices=rigid,max_influences=4,method='Original surface anatomical masks; welded-surface diffusion; rigid Holy Rings',components=details)

def authored_pose(ident,rig,p,name,t,duration):
    natural_pose(ident,rig,p,name,t,duration)
    u=t/duration;c=p['_natural']['pose'];phase=math.tau*u
    wind=pulse(u,.02,.28,.37,.56) if name=='Skill' else 0
    impact=pulse(u,.38,.56,.70,.96) if name=='Skill' else 0
    if ident=='nyaromon':
        c.rotate('TailEnd',z=3*(math.sin(phase-.8)+math.sin(.8)))
        if name=='Skill':
            # Fox Tail: coil backwards, whip around, then settle through each joint.
            c.rotate('Hips',z=-wind*15+impact*28)
            c.rotate('Tail',z=wind*28-impact*65,x=wind*8-impact*4)
            c.rotate('TailTip',z=wind*18-impact*38)
            c.rotate('TailEnd',z=-wind*10+impact*26)
    elif ident=='plotmon' and name=='Skill':
        # Puppy Howl: head lifts and the floppy ears trail the sustained voice.
        c.rotate('Head',x=-wind*13-impact*23)
        c.rotate('Spine',x=-wind*4-impact*3)
        c.rotate('EarL',x=impact*13);c.rotate('EarR',x=impact*13)
        c.rotate('EarTipL',x=-impact*9);c.rotate('EarTipR',x=-impact*9)
    elif ident=='tailmon':
        c.rotate('TailEnd',x=(math.sin(phase-.65)+math.sin(.65))*3)
        if name=='Skill':
            # Cat Punch: strong alternating glove thrusts, distinct from the
            # short normal-attack rake. Gameplay release markers are .46/.69.
            charge=pulse(u,.03,.25,.34,.48)
            left=pulse(u,.31,.46,.49,.64);right=pulse(u,.53,.69,.73,.94)
            c.rotate('Spine',z=-charge*5+left*8-right*8,x=charge*-4+(left+right)*3)
            c.rotate('Head',x=charge*2-(left+right)*2,z=-left*3+right*3)
            for side,s,strike in [(-1,'L',left),(1,'R',right)]:
                hand='Hand'+s;rest=p['_natural']['hands'][hand]
                goal=rest+Vector((-side*charge*.045,charge*.11-strike*.44,charge*.15+strike*.24))
                wrist=Euler((math.radians(-32*strike),0,math.radians(side*strike*9)),'XYZ').to_quaternion()@p['_natural']['orientations'][hand]
                c.ik('UpperArm'+s,'Forearm'+s,hand,goal,c.bend('UpperArm'+s,'Forearm'+s,hand,(side,.25,-.6)),wrist)
                c.rotate('Ear'+s,z=side*(charge*3-strike*5))

def author(ident):
    folder=OUT/ident;bpy.ops.wm.open_mainfile(filepath=str(folder/'neutral.blend'))
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];changes=refine_source(ident,meshes)
    p=PROFILES[ident];defs=skeleton(p)
    if ident=='nyaromon':defs.append(('TailEnd','TailTip',tuple(curve_tail(1.80)),tuple(curve_tail(2.75))))
    if ident=='tailmon':defs.append(('TailEnd','TailTip',(-1.17,.95,1.61),(-1.58,.55,1.12)))
    rig=make_rig(ident,defs);binding=bind(ident,meshes,rig,defs)
    natural_setup(ident,rig,p,neutral_pose)
    scene=bpy.context.scene;scene.render.fps=30;clips=[];samples=[]
    for name,duration,loop in CLIPS:
        rig.animation_data_create();action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
        frames=round(duration*30)
        for i in range(frames+1):
            scene.frame_set(i+1);authored_pose(ident,rig,p,name,i/30,duration)
            bpy.context.view_layer.update();lo,hi=evaluated_bounds(meshes)
            if lo[2]<0 or name=='Down' and i/frames>.35:
                b=rig.pose.bones['Root'];b.location+=b.bone.matrix_local.to_quaternion().inverted()@Vector((0,0,-float(lo[2])))
                bpy.context.view_layer.update();lo,hi=evaluated_bounds(meshes)
            if i%max(1,frames//8)==0 or i==frames:samples.append(dict(clip=name,frame=i,min=lo.tolist(),max=hi.tolist()))
            for b in rig.pose.bones:
                for channel in ('location','rotation_quaternion','scale'):b.keyframe_insert(channel,frame=i+1,group=b.name)
        if hasattr(action,'fcurves'):
            for fc in action.fcurves:
                for key in fc.keyframe_points:key.interpolation='LINEAR'
        clips.append((name,duration,loop,action,frames))
    rig.animation_data.action=clips[0][3];scene.frame_start=1;scene.frame_end=121;scene.frame_set(1)
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
    for obj in meshes:obj.select_set(True)
    bpy.context.view_layer.objects.active=rig
    bpy.ops.export_scene.gltf(filepath=str(folder/'model.glb'),export_format='GLB',use_selection=True,export_animations=True,
      export_animation_mode='ACTIONS',export_yup=True,export_force_sampling=True,export_anim_slide_to_zero=True)
    camera=studio(meshes);scene.cycles.samples=16;scene.render.resolution_x=800;scene.render.resolution_y=800
    if ident=='nyaromon':camera.data.ortho_scale=5.6
    bpy.ops.wm.save_as_mainfile(filepath=str(folder/(ident+'.blend')))
    for mode,u in [('Idle',0),('Walk',.25),('Run',.35),('Attack',.42),('Skill',.57),('Hit',.18),('Victory',.43),('Guard',.48),('Dodge',.4),('Down',1)]:
        clip=next(c for c in clips if c[0]==mode);rig.animation_data.action=clip[3];scene.frame_set(round(u*clip[1]*30)+1)
        render_pose(folder/(mode+'.png'),camera,meshes,ident,mode)
    rig.animation_data.action=clips[0][3];scene.frame_set(1)
    render(folder/'preview.png',camera,(4,-6,3.5) if ident=='nyaromon' else (3,-6,3),(.4,.6,1.2) if ident=='nyaromon' else (0,0,1.2))
    render(folder/'rigged-front.png',camera,(0,-8,1.2),(.4,.6,1.2) if ident=='nyaromon' else (0,0,1.2));render(folder/'rigged-side.png',camera,(8,0,1.2),(.4,.6,1.2) if ident=='nyaromon' else (0,0,1.2))
    bpy.ops.wm.save_as_mainfile(filepath=str(folder/(ident+'.blend')))
    report={'id':ident,'bones':len(rig.data.bones),'binding':binding,'shape_changes':changes,'motion_provenance':'New local authored skeletal motions; not original game motion',
      'clips':[dict(name=n,duration=d,loop=l,frames=f+1) for n,d,l,a,f in clips],
      'source_sha256':hashlib.sha256((folder/'source.obj').read_bytes()).hexdigest(),'output_sha256':hashlib.sha256((folder/'model.glb').read_bytes()).hexdigest(),
      'pose_bounds':samples,'skill_reference':{'nyaromon':'Fox Tail','plotmon':'Puppy Howl','tailmon':'Cat Punch / Lightning Paw; alternating glove thrusts'}[ident],
      'status':'Exported; rendered every authored clip; awaiting final inspection'}
    (folder/'animation-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    source=json.loads((folder/'source.json').read_text(encoding='utf-8'));source['local_modifications']=dict(date='2026-10-13',changes=changes,rig='New anatomical bone rig',motions=[c[0] for c in clips],canonical_front=True)
    (folder/'source.json').write_text(json.dumps(source,ensure_ascii=False,indent=2),encoding='utf-8')
    print('LIGHT LINE READY',ident,len(rig.data.bones),'bones',len(clips),'clips',flush=True)

def render_pose(path,camera,meshes,ident,mode):
    target=Vector((.4,.6,1.2) if ident=='nyaromon' else (0,0,1.2))
    position=Vector((4,-6,3.5) if ident=='nyaromon' else (3,-6,3))
    if mode in ('Down','Dodge'):
        lo,hi=evaluated_bounds(meshes);target=Vector((lo+hi)*.5)
        position=target+Vector((3,-6,2.0))
        camera.data.ortho_scale=max(3.6,float(max(hi-lo))*1.30)
    else:camera.data.ortho_scale=5.6 if ident=='nyaromon' else 3.5
    render(path,camera,position,target)

def rerender(ident):
    folder=OUT/ident;bpy.ops.wm.open_mainfile(filepath=str(folder/(ident+'.blend')))
    rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    camera=bpy.context.scene.camera
    for mode in ('Down','Dodge'):
        rig.animation_data.action=bpy.data.actions[mode]
        duration=next(d for n,d,l in CLIPS if n==mode)
        bpy.context.scene.frame_set(round(duration*30*(1 if mode=='Down' else .4))+1)
        render_pose(folder/(mode+'.png'),camera,meshes,ident,mode)
    if ident=='tailmon':
        rig.animation_data.action=bpy.data.actions['Skill']
        for label,u in [('Skill-release-1',.46),('Skill-release-2',.69)]:
            bpy.context.scene.frame_set(round(2.0*30*u)+1)
            render_pose(folder/(label+'.png'),camera,meshes,ident,'Skill')

def prepare(ident):
    folder=OUT/ident
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.wm.obj_import(filepath=str(folder/'source.obj'),forward_axis='NEGATIVE_Z',up_axis='Y')
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    teximage=bpy.data.images.load(str(folder/'diffuse.png'));teximage.pack()
    mat=bpy.data.materials.new(ident+' original painted diffuse');mat.use_nodes=True
    shader=mat.node_tree.nodes.get('Principled BSDF');shader.inputs['Roughness'].default_value=.82
    tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=teximage
    mat.node_tree.links.new(tex.outputs['Color'],shader.inputs['Base Color'])
    mat.node_tree.links.new(tex.outputs['Color'],shader.inputs['Emission Color'])
    shader.inputs['Emission Strength'].default_value=.12
    for obj in meshes:
        obj.data.transform(obj.matrix_world);obj.matrix_world=Matrix.Identity(4)
        obj.data.materials.clear();obj.data.materials.append(mat)
        for poly in obj.data.polygons:poly.material_index=0;poly.use_smooth=True
    bpy.context.view_layer.update();lo,hi=bounds(meshes)
    scale=2.4/(hi.z-lo.z);center=(lo+hi)*.5;center.z=lo.z
    for obj in meshes:
        for v in obj.data.vertices:v.co=(v.co-center)*scale
        obj.data.update()
    bpy.context.view_layer.update()
    bpy.ops.wm.save_as_mainfile(filepath=str(folder/'neutral.blend'))
    camera=studio(meshes);bpy.context.scene.cycles.samples=8
    bpy.context.scene.render.resolution_x=640;bpy.context.scene.render.resolution_y=640
    for label,pos in [('front',(0,-8,1.2)),('side',(8,0,1.2)),('source-preview',(3,-6,3))]:render(folder/(label+'.png'),camera,pos)
    from faithful_skin_topology import topology
    components=[]
    for obj in meshes:
        xyz=np.array([tuple(v.co) for v in obj.data.vertices]);points,inv,edges,labels=topology(obj.data,xyz)
        for label in np.unique(labels):
            pts=points[labels==label]
            components.append({'object':obj.name,'label':int(label),'vertices':len(pts),'min':pts.min(axis=0).tolist(),'max':pts.max(axis=0).tolist(),'center':pts.mean(axis=0).tolist()})
    (folder/'anatomy-components.json').write_text(json.dumps(components,indent=2),encoding='utf-8')
    print('PREPARED',ident,flush=True)

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--ids',default=','.join(IDS));parser.add_argument('--prepare',action='store_true');parser.add_argument('--rerender',action='store_true')
    args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    for ident in args.ids.split(','):
        if args.prepare:prepare(ident)
        elif args.rerender:rerender(ident)
        else:author(ident)
