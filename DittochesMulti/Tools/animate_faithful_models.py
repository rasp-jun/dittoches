"""Bind real source meshes to anatomical rigs and author editable motion clips.

Blender --background --threads 4 --python-exit-code 1 --python
Tools/animate_faithful_models.py -- --ids agumon,wargreymon
Outputs go to AnimatedReview first; the inspected gallery is never overwritten.
"""
import sys,math,json,hashlib,argparse,shutil
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import numpy as np
from mathutils import Euler,Quaternion,Vector,Matrix
from faithful_rig_common import *
from faithful_rig_profiles import PROFILES,skeleton
from faithful_skin_topology import refine
from faithful_sculpt import refine_agumon

CLIPS=[('Idle',2.8,True),('Walk',1.0,True),('Run',.70,True),
       ('Attack',1.1,False),('Hit',.6,False),('Victory',2.8,False)]


def smooth(a,b,x):
    t=np.clip((x-a)/(b-a),0,1);return t*t*(3-2*t)


def mesh_positions(mesh):
    data=np.empty(len(mesh.vertices)*3,dtype=np.float32);mesh.vertices.foreach_get('co',data)
    return data.reshape(-1,3)


def prepare_shape(ident,meshes):
    changes=[];unique=[];seen=set()
    if ident=='greymon':changes.append('Rebuilt continuous surface from the immutable dense source: joined and welded export chunks before polygon reduction.')
    if ident=='holyangemon':changes.append('Centred the skeleton on the torso; lowered T-pose arms and unfolded all eight wings with independent front and rear controls.')
    if ident=='seraphimon':changes.append('Lowered T-pose arms into a relaxed stance and retained rigid gold wing panels.')
    for obj in meshes:
        digest=hashlib.sha256()
        digest.update(mesh_positions(obj.data).tobytes())
        loops=np.empty(len(obj.data.loops),dtype=np.int32);obj.data.loops.foreach_get('vertex_index',loops)
        digest.update(loops.tobytes())
        for layer in obj.data.uv_layers:
            uv=np.empty(len(layer.data)*2,dtype=np.float32);layer.data.foreach_get('uv',uv);digest.update(uv.tobytes())
        digest.update(str([m.name for m in obj.data.materials]).encode())
        key=digest.hexdigest()
        if key in seen:
            changes.append('Removed coincident duplicate mesh: '+obj.name)
            bpy.data.objects.remove(obj,do_unlink=True);continue
        seen.add(key);unique.append(obj)
        if ident=='agumon':
            for v in obj.data.vertices:
                z=v.co.z
                if z>1.72:v.co.z=1.72+(z-1.72)*.88
                v.co.x*=1+.055*float(smooth(1.42,1.67,z))
            if obj.data.has_custom_normals:obj.data.normals_split_custom_set([(0,0,0)]*len(obj.data.loops))
            obj.data.update()
    if ident=='agumon':changes.append('Reshaped original upper skull 12% lower and 5.5% wider; UVs and eye attachments retained.')
    if ident=='agumon':changes+=refine_agumon(unique)
    return unique,changes


def make_rig(ident,defs):
    arm=bpy.data.armatures.new(ident+' anatomy');rig=bpy.data.objects.new(ident+'_Rig',arm)
    bpy.context.scene.collection.objects.link(rig)
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
    bpy.ops.object.mode_set(mode='EDIT')
    for name,parent,h,t in defs:
        bone=arm.edit_bones.new(name);bone.head=h;bone.tail=t
        if parent:bone.parent=arm.edit_bones[parent]
    bpy.ops.object.mode_set(mode='OBJECT');rig.show_in_front=True
    for b in rig.pose.bones:b.rotation_mode='QUATERNION'
    return rig


def skin(ident,meshes,rig,p,defs):
    names=[d[0] for d in defs];index={n:i for i,n in enumerate(names)}
    heat={}
    if ident=='greymon':
        # The posed sculpt's hand overlaps its thigh in all three coordinate
        # ranges. Heat weights follow the actual connected surface there.
        bpy.ops.object.select_all(action='DESELECT')
        for obj in meshes:obj.select_set(True)
        rig.select_set(True);bpy.context.view_layer.objects.active=rig
        bpy.ops.object.parent_set(type='ARMATURE_AUTO')
        for obj in meshes:
            w=np.zeros((len(obj.data.vertices),len(names)))
            for v in obj.data.vertices:
                for g in v.groups:
                    name=obj.vertex_groups[g.group].name
                    if name in index:w[v.index,index[name]]=g.weight
            valid=w.sum(axis=1)>1e-6
            print('HEAT COVERAGE',obj.name,float(valid.mean()),flush=True)
            if valid.mean()>.8:
                w[valid]/=w[valid].sum(axis=1,keepdims=True);heat[obj.name]=w
            for mod in list(obj.modifiers):
                if mod.type=='ARMATURE':obj.modifiers.remove(mod)
    starts=np.array([d[2] for d in defs]);ends=np.array([d[3] for d in defs]);axes=ends-starts
    lengths=np.sum(axes*axes,axis=1)
    count=0;rigid=0;details={}
    for obj in meshes:
        for group in list(obj.vertex_groups):obj.vertex_groups.remove(group)
        for name in names:obj.vertex_groups.new(name=name)
        xyz=mesh_positions(obj.data).astype(np.float64);x,y,z=xyz.T;ax=np.abs(x)
        to=xyz[:,None,:]-starts[None,:,:]
        t=np.clip(np.sum(to*axes[None,:,:],axis=2)/lengths[None,:],0,1)
        dist=np.sum((to-t[:,:,None]*axes[None,:,:])**2,axis=2)
        allow=np.zeros(dist.shape,dtype=bool)
        def permit(mask,bones):
            ids=[index[n] for n in bones if n in index]
            for k in ids:allow[mask,k]=True
        def replace(mask,bones):
            allow[mask,:]=False;permit(mask,bones)
        permit(np.ones(len(x),dtype=bool),['Hips','Spine','Head'])
        kind=p['kind']
        if kind=='baby':
            replace(np.ones(len(x),dtype=bool),['Head'])
            if ident=='koromon':
                for side,s in [(-1,'L'),(1,'R')]:
                    mask=(x*side>.22)&(z>1.28)
                    replace(mask,['Head','Ear'+s,'EarTip'+s])
            if 'flower' in p:replace(z>1.16,['Head','Flower','Petals'])
            if 'arms' in p:
                for side,s in [(-1,'L'),(1,'R')]:replace((x*side>.64)&(z<1.44)&(z>.48),['Head','UpperArm'+s,'Forearm'+s,'Hand'+s])
        elif kind=='quadruped':
            replace((y<p['head_cut_y'])&(z>.65),['Head'])
            for side,s in [(-1,'L'),(1,'R')]:
                for prefix,front in [('Front',True),('Rear',False)]:
                    mask=(x*side>.19)&(z<p['leg_top'])&((y<-.28) if front else (y>.12))
                    replace(mask,[prefix+'Upper'+s,prefix+'Lower'+s,prefix+'Foot'+s,'Spine' if front else 'Hips'])
                    replace(mask&(z<.25),[prefix+'Foot'+s])
            if 'wing_root' in p:
                for side,s in [(-1,'L'),(1,'R')]:replace((x*side>.20)&(z>1.62),['Wing'+s])
        else:
            replace(z>p['head_cut'],['Head'])
            for side,s in [(-1,'L'),(1,'R')]:
                mask=(x*side>.08)&(z<p['leg_cut'])
                replace(mask,['Hips','Thigh'+s,'Shin'+s,'Foot'+s])
                replace(mask&(z<.23),['Foot'+s])
                arm=(x*side>p['arm_cut'])&(z>.29)
                arm_ids=[index[n+s] for n in ['UpperArm','Forearm','Hand','LowerArm','LowerForearm','LowerHand'] if n+s in index]
                leg_ids=[index[n+s] for n in ['Thigh','Shin','Foot'] if n+s in index]
                if arm_ids and leg_ids:
                    # Hanging hands overlap the legs in height. Distinguish
                    # them anatomically instead of cutting every outer knee
                    # into the arm region.
                    arm &= (z>p['leg_cut']+.15)|(dist[:,arm_ids].min(axis=1)<dist[:,leg_ids].min(axis=1))
                if ident=='agumon':arm=((x*side>.37)&(z>.80))|((x*side>.50)&(z>.32))
                if ident=='greymon':
                    arm=((x<-.50)&(z>1.08))|((x<-.68)&(y<-.12)&(z>.68)) if side<0 else (x>.28)&(z>1.20)
                if kind=='insect':arm&=(z>.63)
                replace(arm,['Spine','UpperArm'+s,'Forearm'+s,'Hand'+s])
                if 'extra_arms' in p:
                    permit(arm,['LowerArm'+s,'LowerForearm'+s,'LowerHand'+s])
                if kind=='angel':
                    # Wings lie behind the chest; arm and blade geometry stays
                    # on the forward plane and follows the shoulder/hand rig.
                    wing=(x*side>.10)&(y>p['wing_root'][1]-.02)&(z>.92)
                    replace(wing,['Wing'+s])
                if kind=='insect':
                    wing=(y>.31)&(z>1.06)
                    replace(wing&(x*side>=0),['Wing'+s])
            if p.get('shield'):replace((y>.13)&(z>.77)&(ax<.63),['Spine'])
            if kind=='angel':replace((ax<.10)&(z<1.1)&(y<-.08),['Hips'])
        if 'tail' in p:
            cutoff=p['tail'][0][1]+.06
            replace((y>cutoff)&(z>.47)&(ax<(.65 if ident!='greymon' else 1.2)),['Hips','Tail','TailTip'])
        if 'jaw' in p:
            if ident=='agumon':jaw=(z>1.47)&(z<1.72)&(y<-.16)&(ax<.52)
            else:jaw=(z>1.39)&(z<1.82)&(y<-.44)&(np.abs(x+.28)<.36)
            replace(jaw,['Jaw'])
        dist[~allow]=1e8
        weights=1/(dist+.01)**2
        weights[dist>1e7]=0;weights/=weights.sum(axis=1,keepdims=True)
        if obj.name in heat:
            valid=heat[obj.name].sum(axis=1)>1e-6
            weights[valid]=heat[obj.name][valid]
        weights,details[obj.name]=refine(ident,obj,xyz,weights,names,starts,ends)
        nearest=np.argsort(-weights,axis=1)[:,:4]
        weights=np.take_along_axis(weights,nearest,axis=1)
        weights/=weights.sum(axis=1,keepdims=True)
        quant=np.rint(weights*255).astype(np.int16);quant[:,0]+=255-quant.sum(axis=1)
        assert np.all(quant.sum(axis=1)==255) and np.all(quant>=0)
        for j in range(4):
            for bone in range(len(names)):
                used=(nearest[:,j]==bone)&(quant[:,j]>0)
                for value in np.unique(quant[used,j]):
                    ids=np.flatnonzero(used&(quant[:,j]==value)).tolist()
                    obj.vertex_groups[bone].add(ids,int(value)/255.,'REPLACE')
        obj.parent=rig;mod=obj.modifiers.new('Anatomical skin','ARMATURE');mod.object=rig
        # glTF/Three.js uses linear skinning; render the same deformation here.
        mod.use_deform_preserve_volume=False
        count+=len(x);rigid+=int(np.count_nonzero(quant[:,0]==255))
    return {'weighted_vertices':count,'rigid_vertices':rigid,'max_influences':4,'method':'anatomical weights, rigid detached details, welded-surface smoothing; original topology retained','heat_bound_meshes':list(heat),'components':details}


def reset(rig):
    for b in rig.pose.bones:b.rotation_quaternion=Quaternion();b.location=(0,0,0);b.scale=(1,1,1)


def rotate(rig,name,x=0,y=0,z=0):
    if name not in rig.pose.bones:return
    b=rig.pose.bones[name];q=b.bone.matrix_local.to_quaternion()
    b.rotation_quaternion=q.inverted()@Euler(tuple(math.radians(v) for v in (x,y,z)),'XYZ').to_quaternion()@q


def translate(rig,name,vector):
    b=rig.pose.bones[name];b.location=b.bone.matrix_local.to_quaternion().inverted()@Vector(vector)


def envelope(t,keys):
    for (a,va),(b,vb) in zip(keys,keys[1:]):
        if a<=t<=b:return va+(vb-va)*float(smooth(a,b,t))
    return keys[0][1] if t<keys[0][0] else keys[-1][1]


def ik(rig,upper,lower,foot,target,bend_sign=-1,use_rest_bend=False):
    bpy.context.view_layer.update();a=rig.pose.bones[upper];b=rig.pose.bones[lower];f=rig.pose.bones[foot]
    start=a.head.copy();target=Vector(target);v=target-start;d=v.normalized()
    l1=a.bone.length;l2=b.bone.length;distance=max(.001,min(v.length,l1+l2-.0001))
    along=(l1*l1-l2*l2+distance*distance)/(2*distance)
    bend=(b.head-start) if use_rest_bend else Vector((0,bend_sign,0))
    bend=bend-d*bend.dot(d)
    if bend.length<.0001:bend=Vector((0,bend_sign,0));bend-=d*bend.dot(d)
    bend.normalize()
    knee=start+d*along+bend*math.sqrt(max(0,l1*l1-along*along))
    for bone,end in [(a,knee),(b,target)]:
        bpy.context.view_layer.update();q=(bone.tail-bone.head).normalized().rotation_difference((end-bone.head).normalized())@bone.matrix.to_quaternion()
        bone.matrix=Matrix.Translation(bone.head)@q.to_matrix().to_4x4()
    bpy.context.view_layer.update();f.matrix=Matrix.Translation(f.head)@f.bone.matrix_local.to_quaternion().to_matrix().to_4x4()


def gait(u,run):
    stance=.62 if not run else .52;stride=.12 if not run else .19
    if u<stance:return -stride+2*stride*u/stance,0
    v=(u-stance)/(1-stance)
    tangent=2*stride/stance*(1-stance)
    # Match stance velocity at both swing boundaries to avoid a stop/start
    # hitch when the foot leaves or returns to the ground.
    forward=(2*v**3-3*v*v+1)*stride+(v**3-2*v*v+v)*tangent+(-2*v**3+3*v*v)*-stride+(v**3-v*v)*tangent
    return forward,math.sin(math.pi*v)**2*(.105 if not run else .18)


def foot_roll(u,run):
    stance=.52 if run else .62
    if u<stance:
        s=u/stance
        return -5*(1-float(smooth(0,.18,s)))+12*float(smooth(.73,1,s))
    v=(u-stance)/(1-stance)
    return envelope(v,[(0,12),(.58,-9),(1,-5)])


def pose(ident,rig,p,mode,t,duration):
    reset(rig);phase=t/duration*math.tau;kind=p['kind'];walk=mode in ('Walk','Run');run=mode=='Run'
    beat=math.sin(phase);breath=math.sin(phase)*.45
    attack=envelope(t,[(0,0),(.24,-.35),(.42,1),(.56,.85),(.82,.12),(duration,0)]) if mode=='Attack' else 0
    hit=envelope(t,[(0,0),(.10,1),(.24,.4),(duration,0)]) if mode=='Hit' else 0
    win=math.sin(math.pi*t/duration)**2 if mode=='Victory' else 0
    if kind=='baby':
        hop=max(0,math.sin(phase))**2 if walk else 0
        translate(rig,'Hips',(0,0,hop*(.22 if run else .12)))
        squash=(-.025*math.cos(phase*2) if walk else .008*beat)-.07*max(0,-attack)
        rig.pose.bones['Hips'].scale=(1-squash*.4,1-squash*.4,1+squash)
        rotate(rig,'Head',x=breath+attack*12-hit*13,y=hit*5,z=beat*(1.6 if walk else .7))
        for side,s in [(-1,'L'),(1,'R')]:
            rotate(rig,'Ear'+s,x=math.sin(phase-.35)*4+attack*14,z=side*math.sin(phase)*3)
            rotate(rig,'EarTip'+s,x=math.sin(phase-.7)*6-attack*10)
            rotate(rig,'UpperArm'+s,x=attack*-25+side*beat*(13 if walk else 3),y=-side*win*30)
            rotate(rig,'Forearm'+s,x=-win*20)
        rotate(rig,'Flower',x=math.sin(phase-.3)*3+attack*8,z=math.sin(phase)*2)
        rotate(rig,'Petals',x=math.sin(phase-.65)*4,z=-math.sin(phase)*2)
        return
    sway=math.sin(phase)*(.8 if walk else .3)
    ready=-.025 if ident=='wargreymon' else 0
    translate(rig,'Hips',(-math.sin(phase)*(.018 if walk else .003),0,ready+(math.cos(phase*2)*(.012 if run else .008) if walk else .003*beat)))
    rotate(rig,'Hips',z=beat*1.5 if walk else 0)
    rotate(rig,'Spine',x=breath+attack*8-hit*10+(3 if ident=='wargreymon' else 0),y=attack*9,z=-sway)
    rotate(rig,'Head',x=-breath*.5-attack*3+hit*5,z=-sway*.45)
    if 'jaw' in p:rotate(rig,'Jaw',x=(-14 if ident=='greymon' else 0)+max(0,attack)*13)
    for side,s in [(-1,'L'),(1,'R')]:
        swing=side*beat*(13 if walk else 1.2)
        base_arm=-20 if ident=='agumon' else (-6 if kind=='human' else 0)
        base_fore=-50 if ident=='agumon' else (-15 if kind=='human' else 0)
        # Lower the source T pose around the shoulder, retaining rigid weapons.
        lower=side*53 if kind=='angel' else 0
        rotate(rig,'UpperArm'+s,x=base_arm+swing-attack*(35 if side<0 else 12)-hit*10,
               y=lower+side*win*20,z=attack*side*9)
        rotate(rig,'Forearm'+s,x=base_fore-max(0,attack)*15-win*10)
        rotate(rig,'Hand'+s,x=10 if ident=='agumon' else 0)
        rotate(rig,'LowerArm'+s,x=-swing*.55-attack*22,z=side*win*8)
        rotate(rig,'LowerForearm'+s,x=-attack*13)
        rotate(rig,'Wing'+s,x=math.sin(phase-.2)*1.4,y=side*(math.sin(phase)*2+max(0,attack)*7+win*8),
               z=-side*62 if ident=='holyangemon' else 0)
        rotate(rig,'FrontUpperWing'+s,y=side*(45+beat*2+win*8),z=side*30)
        rotate(rig,'FrontLowerWing'+s,y=-side*(45+beat*2+win*8),z=side*30)
    rotate(rig,'Tail',x=breath,y=math.sin(phase-.3)*(4 if walk else 2)-attack*5,z=math.sin(phase-.3)*(3 if walk else 1.5))
    rotate(rig,'TailTip',x=math.sin(phase-.8)*2,z=math.sin(phase-.65)*(5 if walk else 2)-attack*6)
    if kind=='quadruped':
        for side,s in [(-1,'L'),(1,'R')]:
            for prefix,key,offset,bend in [('Front','front',0,1),('Rear','rear',.5,-1)]:
                # Diagonal pairs advance together, with delayed hind follow-through.
                u=(t/duration+(.5 if side>0 else 0)+offset)%1
                forward,lift=gait(u,run) if walk else (0,0)
                rest=rig.data.bones[prefix+'Foot'+s].head_local
                target=(rest.x,rest.y+forward,rest.z+lift)
                ik(rig,prefix+'Upper'+s,prefix+'Lower'+s,prefix+'Foot'+s,target,bend)
    else:
        for side,s in [(-1,'L'),(1,'R')]:
            if 'Thigh'+s not in rig.pose.bones:continue
            u=(t/duration+(.5 if side>0 else 0))%1
            forward,lift=gait(u,run) if walk else (0,0)
            rest=rig.data.bones['Foot'+s].head_local
            rolling=walk and (ident=='agumon' or kind in ('human','angel'))
            pitch=math.radians(foot_roll(u,run)) if rolling else 0
            support=0
            if rolling and 'Foot'+s in p.get('_foot_surface',{}):
                points=p['_foot_surface']['Foot'+s]
                # Raise the ankle around the actual heel/toe support point;
                # this lets the other planted foot remain on the ground.
                support=max(0,-float((points[:,1]*math.sin(pitch)+points[:,2]*math.cos(pitch)+rest.z).min()))
            ik(rig,'Thigh'+s,'Shin'+s,'Foot'+s,(rest.x,rest.y+forward,rest.z+lift+support),use_rest_bend=ident=='greymon')
            if rolling:
                foot=rig.pose.bones['Foot'+s]
                orientation=Euler((pitch,0,0),'XYZ').to_quaternion()@foot.bone.matrix_local.to_quaternion()
                foot.matrix=Matrix.Translation(foot.head)@orientation.to_matrix().to_4x4()


def evaluated_bounds(meshes):
    graph=bpy.context.evaluated_depsgraph_get();lo=np.full(3,np.inf);hi=np.full(3,-np.inf)
    for obj in meshes:
        e=obj.evaluated_get(graph);mesh=e.to_mesh();v=mesh_positions(mesh)
        matrix=np.array(e.matrix_world);v=v@matrix[:3,:3].T+matrix[:3,3]
        lo=np.minimum(lo,v.min(axis=0));hi=np.maximum(hi,v.max(axis=0));e.to_mesh_clear()
    return lo,hi


def author(ident,render_images=True,reuse=False):
    p=PROFILES[ident];defs=skeleton(p)
    if reuse:
        saved=ROOT/'ArtSource/NaturalPassBackup-20261001/review'/ident
        bpy.ops.wm.open_mainfile(filepath=str(saved/(ident+'.blend')))
        rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
        meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
        previous=json.loads((saved/'animation-report.json').read_text(encoding='utf-8'))
        binding=previous['binding'];changes=previous['shape_changes']
        rig.animation_data_clear()
        for action in list(bpy.data.actions):bpy.data.actions.remove(action)
        for obj in list(bpy.context.scene.objects):
            if obj!=rig and obj not in meshes:bpy.data.objects.remove(obj,do_unlink=True)
        reset(rig)
        if ident=='agumon':changes+=refine_agumon(meshes)
    else:
        meshes=import_static(ident);meshes,changes=prepare_shape(ident,meshes)
        rig=make_rig(ident,defs);binding=skin(ident,meshes,rig,p,defs)
    changes.append('Removed the extra leading frame from all six exported clips so each loop starts at zero.')
    if p['kind']!='baby':changes.append('Continuous foot trajectories, weight transfer and torso counter-rotation.')
    if ident in ('agumon','wargreymon'):changes.append('Bent arms into a relaxed forward stance.')
    if ident=='agumon' or p['kind'] in ('human','angel'):changes.append('Added heel contact and toe-off with ankle support measured from the original foot surface.')
    p['_foot_surface']={}
    for foot_name in ['FootL','FootR']:
        if foot_name not in rig.data.bones:continue
        vertices=[]
        for obj in meshes:
            group=obj.vertex_groups.get(foot_name)
            if group:
                vertices.extend(tuple(v.co-rig.data.bones[foot_name].head_local) for v in obj.data.vertices if any(g.group==group.index and g.weight>.5 for g in v.groups))
        if vertices:p['_foot_surface'][foot_name]=np.array(vertices)
    folder=OUT/ident;folder.mkdir(parents=True,exist_ok=True)
    scene=bpy.context.scene;scene.render.fps=30;clips=[];samples=[]
    for name,duration,loop in CLIPS:
        rig.animation_data_create();action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
        frames=round(duration*30)
        for i in range(frames+1):
            scene.frame_set(i+1);pose(ident,rig,p,name,i/30,duration)
            if p['kind']=='baby' or ident in ('agumon','greymon','wargreymon','holyangemon','seraphimon'):
                bpy.context.view_layer.update();ground,_=evaluated_bounds(meshes)
                if ground[2]<0:
                    translate(rig,'Root',(0,0,-float(ground[2])))
            if i==0 or i%max(1,frames//8)==0 or i==frames:
                bpy.context.view_layer.update();lo,hi=evaluated_bounds(meshes)
                samples.append({'clip':name,'frame':i,'min':lo.tolist(),'max':hi.tolist()})
            for b in rig.pose.bones:
                b.keyframe_insert('location',frame=i+1,group=b.name)
                b.keyframe_insert('rotation_quaternion',frame=i+1,group=b.name)
                b.keyframe_insert('scale',frame=i+1,group=b.name)
        # Linear interpolation of sampled 30 fps poses avoids spline overshoot.
        if hasattr(action,'fcurves'):
            for fc in action.fcurves:
                for key in fc.keyframe_points:key.interpolation='LINEAR'
        clips.append((name,duration,loop,action,frames))
    rig.animation_data.action=clips[0][3];scene.frame_start=1;scene.frame_end=85;scene.frame_set(1)
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
    for obj in meshes:obj.select_set(True)
    bpy.context.view_layer.objects.active=rig
    bpy.ops.export_scene.gltf(filepath=str(folder/'model.glb'),export_format='GLB',use_selection=True,
        export_animations=True,export_animation_mode='ACTIONS',export_yup=True,
        export_all_vertex_colors=True,export_force_sampling=True,export_anim_slide_to_zero=True)
    camera=studio(meshes)
    bpy.ops.wm.save_as_mainfile(filepath=str(folder/(ident+'.blend')))
    if render_images:
        for mode,time in [('Idle',0),('Walk',.27),('Attack',.44)]:
            clip=next(c for c in clips if c[0]==mode);rig.animation_data.action=clip[3];scene.frame_set(round(time*30)+1)
            render(folder/(mode+'.png'),camera)
        shutil.copy2(folder/'Idle.png',folder/'preview.png')
    source_path=BACKUP/ident/'model.glb'
    if ident=='greymon':source_path=Path(json.loads((BACKUP/ident/'source.json').read_text(encoding='utf-8-sig'))['original_mesh_file'])
    report={'id':ident,'source':str(source_path.relative_to(ROOT)),
        'source_sha256':hashlib.sha256(source_path.read_bytes()).hexdigest(),
        'output_sha256':hashlib.sha256((folder/'model.glb').read_bytes()).hexdigest(),
        'bones':len(defs),'binding':binding,'shape_changes':changes,
        'refinement_pass':2,
        'clips':[{'name':n,'duration':d,'loop':l,'frames':f+1} for n,d,l,a,f in clips],
        'pose_bounds':samples,'motion_provenance':'New local animation studies; not extracted Digimon Masters motion',
        'status':'requires actual render and deformation review before gallery promotion'}
    (folder/'animation-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    source=json.loads((BACKUP/ident/'source.json').read_text(encoding='utf-8-sig'))
    source['local_modifications']={'date':'2026-10-01','changes':changes,'rig':'anatomical skeletal binding','motions':[c[0] for c in clips],
        'model_sha256':report['output_sha256'],'motion_provenance':report['motion_provenance'],'canonical_front':True}
    (folder/'source.json').write_text(json.dumps(source,ensure_ascii=False,indent=2),encoding='utf-8')
    print('RIGGED REVIEW READY',ident,len(defs),'bones',len(clips),'clips',flush=True)


if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--ids',default=','.join(PROFILES));parser.add_argument('--no-render',action='store_true');parser.add_argument('--reuse-bind',action='store_true')
    args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    for ident in args.ids.split(','):author(ident,not args.no_render,args.reuse_bind)
