"""Inspect evaluated Blender meshes at representative poses for all roster rigs."""
import json
import math
from pathlib import Path
import sys
import bpy
from mathutils import Vector

root=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(root/'Tools'))
from roster_designs import DESIGNS,CLIPS
results=[];checks=0;problems=[]
for ident,label,kind,*_ in DESIGNS:
    path=root/'ArtSource'/('Agumon/Agumon.blend' if ident=='agumon' else 'Roster/'+ident+'/'+ident+'.blend')
    bpy.ops.wm.open_mainfile(filepath=str(path))
    rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH' and any(m.type=='ARMATURE' for m in o.modifiers)]
    low=100;high=-100;worst=None
    for name,duration,loop in CLIPS:
        rig.animation_data.action=bpy.data.actions[name]
        for sample in range(5):
            age=duration*sample/4;frame=1+age*30
            bpy.context.scene.frame_set(math.floor(frame),subframe=frame%1)
            deps=bpy.context.evaluated_depsgraph_get();pose_low=100;pose_high=-100
            for obj in meshes:
                evaluated=obj.evaluated_get(deps)
                corners=[evaluated.matrix_world@Vector(c) for c in evaluated.bound_box]
                for v in corners:
                    if not all(math.isfinite(x) for x in v):raise AssertionError(ident+': invalid evaluated geometry')
                pose_low=min(pose_low,min(v.z for v in corners));pose_high=max(pose_high,max(v.z for v in corners));checks+=1
            if pose_low<low:low=pose_low;worst=[name,age]
            high=max(high,pose_high)
            if pose_low<-.005:problems.append({'id':ident,'clip':name,'time':age,'minimum_z':pose_low})
    results.append({'id':ident,'minimum_z':low,'maximum_z':high,'lowest_pose':worst})
    print('ROSTER POSE',ident,round(low,4),flush=True)
report={'checks':checks,'models':results,'ground_penetrations':problems}
(root/'ArtSource/Roster/motion-bounds-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('ROSTER POSE BOUNDS',checks,'checks;',len(problems),'ground penetrations',flush=True)
if problems:raise AssertionError('Ground penetration; see motion-bounds-report.json')
