"""Inspect evaluated skeletal surfaces, loop closure and gross stretch."""
import sys,json,hashlib,argparse
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from animate_faithful_models import *


def check(ident):
    folder=OUT/ident
    bpy.ops.wm.open_mainfile(filepath=str(folder/(ident+'.blend')))
    scene=bpy.context.scene;rig=next(o for o in scene.objects if o.type=='ARMATURE')
    meshes=[o for o in scene.objects if o.type=='MESH']
    original={};edges={}
    for obj in meshes:
        original[obj.name]=mesh_positions(obj.data).astype(float)
        e=np.empty(len(obj.data.edges)*2,dtype=np.int32);obj.data.edges.foreach_get('vertices',e)
        edges[obj.name]=e.reshape(-1,2)
    rows=[];faults=[]
    for name,duration,loop in CLIPS:
        rig.animation_data.action=bpy.data.actions[name]
        frames=round(duration*30);first={};last={};motion=0;max_extension=0;bad_edges=0;floor=0;worst=[]
        for frame in sorted(set([1,frames+1]+[round(x*frames)+1 for x in np.linspace(0,1,13)])):
            scene.frame_set(frame);bpy.context.view_layer.update();graph=bpy.context.evaluated_depsgraph_get()
            for obj in meshes:
                evaluated=obj.evaluated_get(graph);mesh=evaluated.to_mesh();v=mesh_positions(mesh).astype(float)
                assert np.isfinite(v).all(),ident+' non-finite positions'
                if frame==1:first[obj.name]=v.copy()
                if frame==frames+1:last[obj.name]=v.copy()
                if obj.name in first:motion=max(motion,float(np.linalg.norm(v-first[obj.name],axis=1).max()))
                floor=min(floor,float(v[:,2].min()))
                a,b=edges[obj.name].T
                rest=np.linalg.norm(original[obj.name][a]-original[obj.name][b],axis=1)
                length=np.linalg.norm(v[a]-v[b],axis=1)
                extension=length-rest;max_extension=max(max_extension,float(extension.max()))
                bad=(extension>.075)&(length>rest*3)
                bad_edges+=int(bad.sum())
                if np.any(bad):
                    i=int(np.argmax(extension*bad));worst.append({'mesh':obj.name,'frame':frame,'extension':float(extension[i]),'from':original[obj.name][a[i]].tolist(),'to':original[obj.name][b[i]].tolist()})
                evaluated.to_mesh_clear()
        closure=max(float(np.abs(first[n]-last[n]).max()) for n in first)
        if loop and closure>1e-4:faults.append(name+' loop does not close')
        if motion<1e-4:faults.append(name+' surface is static')
        if bad_edges:faults.append(name+' grossly stretched edges')
        if floor<-.035:faults.append(name+' penetrates floor')
        rows.append({'clip':name,'motion':motion,'floor':floor,'loop_error':closure if loop else None,'max_edge_extension':max_extension,'gross_stretch_samples':bad_edges,'worst':sorted(worst,key=lambda v:-v['extension'])[:5]})
    report={'id':ident,'passed':not faults,'faults':faults,'sha256':hashlib.sha256((folder/'model.glb').read_bytes()).hexdigest(),'clips':rows,'limits':'Samples 13 poses per clip; visual inspection is still required.'}
    (folder/'deformation-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print('DEFORMATION',ident,'PASS' if report['passed'] else faults,flush=True)

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--ids',default=','.join(PROFILES))
    args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    for ident in args.ids.split(','):check(ident)
