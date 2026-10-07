"""Append combat clips while preserving native source GLB data and animations.

Birdramon's authored clips use its source actions, retimed with recovery blends.
Kuwagamon gets joint animation on its existing four-arm skeleton. Source bytes
remain as an unchanged prefix of the BIN chunk; no mesh or skin is rebuilt.
"""
import json,struct,hashlib,math,shutil,copy
from pathlib import Path
import numpy as np
from faithful_motion_catalog import CLIPS,BASELINE,clips_for
from verify_faithful_glb_motion import read
ROOT=Path(__file__).resolve().parents[1]

def unit(q):return q/max(np.linalg.norm(q),1e-12)
def mul(a,b):
    ax,ay,az,aw=a;bx,by,bz,bw=b
    return np.array([aw*bx+ax*bw+ay*bz-az*by,aw*by-ax*bz+ay*bw+az*bx,aw*bz+ax*by-ay*bx+az*bw,aw*bw-ax*bx-ay*by-az*bz])
def qmatrix(q):
    x,y,z,w=unit(q)
    return np.array([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],[2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]])
def slerp(a,b,t):
    a=unit(a);b=unit(b);dot=np.dot(a,b)
    if dot<0:b=-b;dot=-dot
    if dot>.9995:return unit(a+(b-a)*t)
    angle=math.acos(min(1,dot));return (a*math.sin((1-t)*angle)+b*math.sin(t*angle))/math.sin(angle)

class Native:
    def __init__(self,path):
        self.doc,self.accessor,_=read(path);self.raw=path.read_bytes();size=struct.unpack_from('<I',self.raw,12)[0]
        self.binary=self.raw[28+size:];self.nodes=self.doc['nodes'];self.names={n.get('name'):i for i,n in enumerate(self.nodes)}
        self.parents={c:i for i,n in enumerate(self.nodes) for c in n.get('children',[])}
        self.rest=[{k:np.array(n.get(k,v),dtype=float) for k,v in [('translation',[0,0,0]),('rotation',[0,0,0,1]),('scale',[1,1,1])]} for n in self.nodes]
        self.actions={}
        for a in self.doc.get('animations',[]):
            tracks=[]
            for channel in a['channels']:
                target=channel['target'];sampler=a['samplers'][channel['sampler']]
                assert sampler.get('interpolation','LINEAR') in ('LINEAR','STEP')
                tracks.append((target['node'],target['path'],np.array(self.accessor(sampler['input'])).ravel(),np.array(self.accessor(sampler['output'])),sampler.get('interpolation','LINEAR')))
            self.actions[a['name']]=tracks
        self.primitives=[]
        for ni,node in enumerate(self.nodes):
            if 'mesh' not in node:continue
            skin=self.doc.get('skins',[])[node['skin']] if 'skin' in node else None
            for prim in self.doc['meshes'][node['mesh']]['primitives']:
                attrs=prim['attributes'];xyz=np.array(self.accessor(attrs['POSITION']));xyz=np.c_[xyz,np.ones(len(xyz))]
                indices=np.array(self.accessor(prim['indices'])).ravel().reshape(-1,3)
                edges=np.unique(np.sort(np.r_[indices[:,[0,1]],indices[:,[1,2]],indices[:,[2,0]]],axis=1),axis=0)
                if skin:
                    joints=np.array(self.accessor(attrs['JOINTS_0']),dtype=int);weights=np.array(self.accessor(attrs['WEIGHTS_0']))
                    bind=np.array(self.accessor(skin['inverseBindMatrices'])).reshape(-1,4,4).transpose(0,2,1)
                    self.primitives.append((ni,xyz,edges,np.array(skin['joints']),bind,joints,weights))
                else:self.primitives.append((ni,xyz,edges,None,None,None,None))
    def sample(self,name,u):
        state=copy.deepcopy(self.rest);tracks=self.actions[name];duration=max(t[-1] for _,_,t,_,_ in tracks)
        time=np.clip(u,0,1)*duration
        for ni,path,times,values,interpolation in tracks:
            i=min(max(0,int(np.searchsorted(times,time,side='right'))-1),len(times)-1);j=min(i+1,len(times)-1)
            v=0 if i==j or interpolation=='STEP' else np.clip((time-times[i])/(times[j]-times[i]),0,1)
            state[ni][path]=slerp(values[i],values[j],v) if path=='rotation' else values[i]*(1-v)+values[j]*v
        return state
    def world(self,state):
        if getattr(self,'_world_state',None) is not state:
            self._world_state=state;self._world_cache={}
        cache=self._world_cache
        def one(i):
            if i in cache:return cache[i]
            n=self.nodes[i];s=state[i]
            if 'matrix' in n:m=np.array(n['matrix']).reshape(4,4).T
            else:
                m=np.eye(4);m[:3,:3]=qmatrix(s['rotation'])@np.diag(s['scale']);m[:3,3]=s['translation']
            cache[i]=one(self.parents[i])@m if i in self.parents else m
            return cache[i]
        return np.array([one(i) for i in range(len(state))])
    def invalidate(self,index):
        self._world_cache.pop(index,None)
        for child in self.nodes[index].get('children',[]):self.invalidate(child)
    def surfaces(self,state):
        world=self.world(state);results=[]
        for ni,xyz,edges,bones,bind,joints,weights in self.primitives:
            if bones is None:points=(world[ni]@xyz.T).T[:,:3]
            else:
                matrices=world[bones]@bind
                points=sum((np.einsum('vij,vj->vi',matrices[joints[:,k]],xyz)[:,:3]*weights[:,k,None] for k in range(weights.shape[1])))
            results.append(points)
        return results
    def rotate(self,state,index,degrees,axis):
        if index is None or not degrees:return
        world=self.world(state);parent=world[self.parents[index]] if index in self.parents else np.eye(4)
        direction=np.linalg.solve(parent[:3,:3],np.array(axis,dtype=float));direction/=np.linalg.norm(direction)
        angle=math.radians(degrees)/2;delta=np.r_[direction*math.sin(angle),math.cos(angle)]
        state[index]['rotation']=unit(mul(delta,state[index]['rotation']))
        self.invalidate(index)
    def move(self,state,index,delta):
        world=self.world(state);parent=world[self.parents[index]] if index in self.parents else np.eye(4)
        state[index]['translation']+=np.linalg.solve(parent[:3,:3],delta)
        self.invalidate(index)

    def blend(self,a,b,amount):
        state=copy.deepcopy(a)
        for i in range(len(state)):
            for key in ('translation','scale'):state[i][key]=a[i][key]*(1-amount)+b[i][key]*amount
            state[i]['rotation']=slerp(a[i]['rotation'],b[i]['rotation'],amount)
        return state

    def aim(self,state,bone,child,target):
        world=self.world(state);head=world[bone][:3,3];old=world[child][:3,3]-head;new=np.asarray(target)-head
        old=old/max(np.linalg.norm(old),1e-12);new=new/max(np.linalg.norm(new),1e-12)
        axis=np.cross(old,new);length=np.linalg.norm(axis)
        if length>1e-8:self.rotate(state,bone,math.degrees(math.atan2(length,np.dot(old,new))),axis/length)

    def bend(self,state,upper,lower,end,fallback):
        w=self.world(state);a=w[upper][:3,3];pole=w[lower][:3,3]-a;d=w[end][:3,3]-a
        d=d/max(np.linalg.norm(d),1e-9);length=np.linalg.norm(pole);pole-=d*np.dot(pole,d)
        return pole/np.linalg.norm(pole) if np.linalg.norm(pole)>max(1e-6,length*.02) else np.array(fallback,dtype=float)

    def ik(self,state,upper,lower,foot,target,pole,orientation=None):
        world=self.world(state);a=world[upper][:3,3];b=world[lower][:3,3];c=world[foot][:3,3]
        v=np.asarray(target)-a;l1=np.linalg.norm(b-a);l2=np.linalg.norm(c-b);distance=max(abs(l1-l2)+1e-5,min(np.linalg.norm(v),(l1+l2)*.998))
        d=v/max(np.linalg.norm(v),1e-9);along=(l1*l1-l2*l2+distance*distance)/(2*distance)
        bend=np.array(pole,dtype=float,copy=True);bend-=d*np.dot(bend,d)
        if np.linalg.norm(bend)<1e-7:bend=b-a; bend-=d*np.dot(bend,d)
        knee=a+d*along+bend/max(np.linalg.norm(bend),1e-8)*math.sqrt(max(0,l1*l1-along*along))
        self.aim(state,upper,lower,knee);self.aim(state,lower,foot,a+d*distance)
        if orientation is not None:self.orient(state,foot,orientation)

    def orient(self,state,index,orientation,amount=1):
        def rotation(matrix):
            u,_,vt=np.linalg.svd(matrix);return u@vt
        current=rotation(self.world(state)[index][:3,:3]);delta=rotation(orientation)@current.T
        axis=np.array([delta[2,1]-delta[1,2],delta[0,2]-delta[2,0],delta[1,0]-delta[0,1]])
        norm=np.linalg.norm(axis)
        if norm>1e-8:self.rotate(state,index,amount*math.degrees(math.atan2(norm,np.trace(delta)-1)),axis/norm)

def extend(ident):
    source=ROOT/BASELINE/'gallery'/ident;out=ROOT/'ArtSource/AnimatedReview'/ident;out.mkdir(exist_ok=True)
    technique_pass=BASELINE=='ArtSource/TechniqueMotionBackup-20261002'
    m=Native(source/'model.glb');base=m.sample('Idle' if technique_pass else 'idle' if ident=='birdramon' else 'Take 001',0)
    rest_surface=m.surfaces(base);allpoints=np.concatenate(rest_surface);height=float(np.ptp(allpoints[:,1]));floor=float(np.concatenate(m.surfaces(m.rest))[:,1].min())
    assert height>0
    payload=bytearray(m.binary);doc=copy.deepcopy(m.doc);checks=[];authored=[]
    common={name for name,_,_ in CLIPS}
    replaced={'Attack','Skill'} if technique_pass else common
    doc['animations']=[a for a in doc['animations'] if a['name'] not in replaced]
    from faithful_native_motion import pose as natural_pose
    if technique_pass:
        from faithful_native_techniques import pose as natural_pose
        old_report=json.loads((source/'animation-report.json').read_text(encoding='utf-8'))
        old_checks=json.loads((source/'deformation-report.json').read_text(encoding='utf-8'))
        checks=[c for c in old_checks['clips'] if c['clip'] not in replaced]
        authored=[c for c in old_report['clips'] if c['name'] not in replaced]
    def accessor(rows,kind):
        values=np.asarray(rows,dtype='<f4');payload.extend(b'\0'*((-len(payload))%4));offset=len(payload);payload.extend(values.tobytes())
        view=len(doc['bufferViews']);doc['bufferViews'].append({'buffer':0,'byteOffset':offset,'byteLength':values.nbytes})
        index=len(doc['accessors']);record={'bufferView':view,'componentType':5126,'count':len(values),'type':kind}
        if kind=='SCALAR':record.update(min=[float(values.min())],max=[float(values.max())])
        doc['accessors'].append(record);return index
    for name,duration,loop in clips_for(ident):
        if name not in replaced:continue
        frames=round(duration*30);poses=[];worst=0;floor_error=0;sampled=[]
        for frame in range(frames+1):
            state=natural_pose(m,ident,name,frame/frames,base,height)
            if loop and frame==frames:state=copy.deepcopy(poses[0])
            surfaces=m.surfaces(state);low=min(v[:,1].min() for v in surfaces)
            if low<floor or name=='Down':
                from faithful_motion_styles import smooth01
                contact=1 if low<floor else smooth01((frame/frames-.15)/.57)
                m.move(state,m.names['_rootJoint'],np.array([0,(floor-low)*contact,0]));surfaces=m.surfaces(state)
            if frame in {round(i*frames/12) for i in range(13)}:
                for points,rest,primitive in zip(surfaces,rest_surface,m.primitives):
                    a,b=primitive[2].T;original=np.linalg.norm(rest[a]-rest[b],axis=1);deformed=np.linalg.norm(points[a]-points[b],axis=1)
                    extension=(deformed-original)*2.4/height
                    bad=(extension>.075)&(deformed>original*3)
                    worst+=int(bad.sum());assert np.isfinite(points).all()
                floor_error=min(floor_error,(min(v[:,1].min() for v in surfaces)-floor)*2.4/height)
                sampled.append(np.concatenate(surfaces)[::max(1,len(allpoints)//500)].tolist())
            poses.append(state)
        motion=max(float(np.abs(np.array(v)-np.array(sampled[0])).max()) for v in sampled)*2.4/height
        assert motion>1e-5,(ident,name,'no motion')
        checks.append({'clip':name,'gross_stretch_samples':worst,'floor':floor_error,'motion':motion})
        animation={'name':name,'channels':[],'samplers':[]};times=accessor(np.linspace(0,duration,frames+1),'SCALAR')
        for ni,node in enumerate(m.nodes):
            if 'matrix' in node:continue
            for path in ('translation','rotation','scale'):
                values=np.array([s[ni][path] for s in poses]);rest=m.rest[ni][path]
                if np.max(np.abs(values-rest))<1e-7:continue
                if path=='rotation':
                    for i in range(1,len(values)):
                        if np.dot(values[i-1],values[i])<0:values[i]*=-1
                output=accessor(values,'VEC4' if path=='rotation' else 'VEC3');index=len(animation['samplers'])
                animation['samplers'].append({'input':times,'output':output,'interpolation':'LINEAR'})
                animation['channels'].append({'sampler':index,'target':{'node':ni,'path':path}})
        doc['animations'].append(animation);authored.append({'name':name,'duration':duration,'loop':loop,'frames':frames+1})
        print('NATIVE CLIP',ident,name,'stretch',worst,flush=True)
    doc['buffers'][0]['byteLength']=len(payload);payload.extend(b'\0'*((-len(payload))%4))
    encoded=json.dumps(doc,separators=(',',':')).encode();encoded+=b' '*((-len(encoded))%4)
    result=struct.pack('<4sII',b'glTF',2,28+len(encoded)+len(payload))+struct.pack('<II',len(encoded),0x4e4f534a)+encoded+struct.pack('<II',len(payload),0x004e4942)+payload
    digest=hashlib.sha256(result).hexdigest();(out/'model.glb').write_bytes(result)
    assert bytes(payload[:len(m.binary)])==m.binary
    source_record=json.loads((source/'source.json').read_text(encoding='utf-8'))
    source_record['local_modifications'].update(date='2026-10-02',model_sha256=digest,motions=[c['name'] for c in authored],original_animation_buffers_preserved=True)
    source_record['local_modifications']['changes'].append('Replaced Attack and Skill with technique-specific choreography; eight common motions, original geometry, skin and all source animation bytes retained.' if technique_pass else 'Reworked ten common motions with weight transfer, foot contact, anticipation and recovery; original geometry, skin and source animation bytes retained.')
    (out/'source.json').write_text(json.dumps(source_record,ensure_ascii=False,indent=2),encoding='utf-8')
    report={'id':ident,'source':str((source/'model.glb').relative_to(ROOT)),'source_sha256':hashlib.sha256(m.raw).hexdigest(),
        'output_sha256':digest,'native_extension':True,'source_clips':[n for n in m.actions if n not in common],'clips':authored,'refinement_pass':4,'motion_revision':'Natural contact and recovery revision',
        'comparison_source':BASELINE+'/gallery/'+ident+'/model.glb','shape_changes':source_record['local_modifications']['changes'],
        'motion_provenance':'Source flight cycles and key poses with new body motion, recovery, bank and gradual landing' if ident=='birdramon' else 'New local joint animation with foot contact, phased stepping and four-arm IK on original source rig',
        'original_binary_sha256':hashlib.sha256(m.binary).hexdigest(),'status':'requires visual and browser review'}
    if technique_pass:
        from faithful_techniques import TECHNIQUES,REVISION
        report.update(techniques=TECHNIQUES[ident],motion_revision=REVISION,preserved_noncombat_clips=True,
            motion_provenance='Technique-specific local choreography on the original native skeleton; eight common clips and all source clips retained unchanged.')
        source_record['local_modifications']['technique_source']=TECHNIQUES[ident]['source']
        (out/'source.json').write_text(json.dumps(source_record,ensure_ascii=False,indent=2),encoding='utf-8')
    (out/'animation-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    passed=all(c['gross_stretch_samples']==0 and c['floor']>-.035 for c in checks)
    (out/'deformation-report.json').write_text(json.dumps({'id':ident,'passed':passed,'sha256':digest,'clips':checks,'faults':[c['clip']+' gross stretch' for c in checks if c['gross_stretch_samples']],'limits':'13 sampled poses per new clip, original source clips retained byte-for-byte'},indent=2),encoding='utf-8')
    assert passed,(ident,'native deformation check failed')

if __name__=='__main__':
    import argparse
    parser=argparse.ArgumentParser();parser.add_argument('--ids',default='birdramon,kuwagamon');args=parser.parse_args()
    for ident in args.ids.split(','):
        assert ident in ('birdramon','kuwagamon')
        extend(ident)
