"""Validate saved Blender Actions: gait contact, loop closure and motion bounds."""
import json
import math
from pathlib import Path
import bpy
from mathutils import Vector

root=Path(__file__).resolve().parent.parent
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/Agumon/Agumon.blend'))
scene=bpy.context.scene
rig=bpy.data.objects['Agumon_Rig']
checks=0
report={}


def require(condition,message):
    global checks
    if not condition:raise AssertionError(message)
    checks+=1


def sample(age):
    frame=1+age*30
    scene.frame_set(math.floor(frame),subframe=frame%1)
    bpy.context.view_layer.update()


for name,duration,stride,stance in [('Walk',.9,.15,.62),('Run',.6,.20,.54)]:
    rig.animation_data.action=bpy.data.actions[name]
    speed=2*stride/(stance*duration)
    worst=0
    for suffix,offset in [('L',0),('R',.5)]:
        foot=rig.pose.bones['Foot'+suffix]
        for step in range(121):
            age=duration*step/120
            u=(age/duration+offset)%1
            sample(age)
            if .04<u<stance-.04:
                wanted=Vector((-.31 if suffix=='L' else .31,.13+stride*(1-2*u/stance),.16))
                error=(foot.head-wanted).length
                worst=max(worst,error)
                require(error<.008,f'{name}/{suffix}: contact error {error:.6f}')
            require(all(math.isfinite(v) for v in foot.head),name+': finite foot position')
    sample(0);first=[bone.matrix.copy() for bone in rig.pose.bones]
    sample(duration)
    for bone,start in zip(rig.pose.bones,first):
        require(max(abs(bone.matrix[r][c]-start[r][c]) for r in range(4) for c in range(4))<1e-5,name+': loop '+bone.name)
    report[name]={'contact_error_model_units':worst,'model_speed':speed,'world_speed_at_1star':speed*.66}

# Both feet stay in place while the body anticipates, attacks and recovers.
for name in ['Idle','Attack','PepperBreath','Hit','Turn']:
    action=bpy.data.actions[name];rig.animation_data.action=action
    duration=(action.frame_range[1]-1)/30
    worst=0
    for step in range(41):
        sample(duration*step/40)
        for suffix in ['L','R']:
            wanted=Vector((-.31 if suffix=='L' else .31,.13,.16))
            error=(rig.pose.bones['Foot'+suffix].head-wanted).length
            worst=max(worst,error)
            require(error<.008,f'{name}/{suffix}: planted foot {error:.6f}')
    report[name]={'contact_error_model_units':worst}
report['checks']=checks
(root/'ArtSource/Agumon/motion-validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('AGUMON MOTION PASS',json.dumps(report))
