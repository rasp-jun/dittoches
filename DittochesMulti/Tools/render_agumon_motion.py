"""Render the real Blender armature actions into a short review video."""
import math
import argparse
import sys
from pathlib import Path
import bpy
from mathutils import Vector

root=Path(__file__).resolve().parent.parent
parser=argparse.ArgumentParser()
parser.add_argument('--quick',action='store_true',help='512px geometry review using Blender Workbench')
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
scene=bpy.context.scene
rig=bpy.data.objects['Agumon_Rig']
names=['Idle','Walk','Run','Attack','PepperBreath','Hit','Defeat','Turn']
fps=12 if args.quick else 24
frames_per_clip=30 if args.quick else 60
samples=[]
for name in names:
    action=bpy.data.actions[name]
    rig.animation_data.action=action
    first,last=action.frame_range
    duration=(last-first)/30
    for frame in range(frames_per_clip):
        age=frame/fps
        if name in ('Idle','Walk','Run','Turn'):age%=duration
        else:age=min(age,duration)
        sample=first+age*30
        scene.frame_set(math.floor(sample),subframe=sample%1)
        bpy.context.view_layer.update()
        samples.append([(p.location.copy(),p.rotation_quaternion.copy()) for p in rig.pose.bones])
action=bpy.data.actions.new('Review reel')
rig.animation_data.action=action
for frame,values in enumerate(samples,1):
    for bone,(position,rotation) in zip(rig.pose.bones,values):
        bone.location=position;bone.rotation_quaternion=rotation
        bone.keyframe_insert('location',frame=frame,group=bone.name)
        bone.keyframe_insert('rotation_quaternion',frame=frame,group=bone.name)
scene.frame_start=1;scene.frame_end=len(samples);scene.render.fps=fps
scene.render.resolution_x=512 if args.quick else 720
scene.render.resolution_y=scene.render.resolution_x;scene.render.resolution_percentage=100
scene.render.use_freestyle=False
if args.quick:
    scene.render.engine='BLENDER_WORKBENCH'
    scene.display.shading.light='STUDIO'
    scene.display.shading.color_type='MATERIAL'
    scene.display.shading.show_shadows=True
    scene.display.shading.show_cavity=True
    scene.display.shading.cavity_type='WORLD'
    scene.display.shading.show_specular_highlight=False
if hasattr(scene,'eevee'):scene.eevee.taa_render_samples=24
camera=scene.camera
camera.location=(3.7,6.2,3)
camera.rotation_euler=(Vector((0,.025,1.13))-camera.location).to_track_quat('-Z','Y').to_euler()
text_mat=bpy.data.materials['Warm ink']
for i,name in enumerate(names):
    curve=bpy.data.curves.new('Motion label '+name,'FONT');curve.body=name
    curve.align_x='CENTER';curve.size=.11
    obj=bpy.data.objects.new('Label '+name,curve);scene.collection.objects.link(obj)
    obj.parent=camera;obj.location=(0,-1.31,-4);obj.data.materials.append(text_mat)
    for frame,hide in ((1,True),(i*frames_per_clip+1,False),((i+1)*frames_per_clip+1,True)):
        obj.hide_render=hide;obj.keyframe_insert('hide_render',frame=frame)
scene.render.image_settings.file_format='FFMPEG'
scene.render.ffmpeg.format='MPEG4';scene.render.ffmpeg.codec='H264';scene.render.ffmpeg.constant_rate_factor='MEDIUM'
scene.render.filepath=str(root/'ArtSource/Agumon'/('Agumon-motion-review-20261001.mp4' if args.quick else 'Agumon-motion-review.mp4'))
# Refresh the eight stills from these exact saved Actions before the review reel.
for i,name in enumerate(names):
    scene.frame_set(i*frames_per_clip+max(1,round((.32 if name=='Attack' else .4 if name=='PepperBreath' else 1.1 if name=='Defeat' else .2)*fps)))
    scene.render.image_settings.file_format='PNG'
    scene.render.filepath=str(root/'ArtSource/Agumon'/('review-'+name+'.png'))
    bpy.ops.render.render(write_still=True)
scene.render.image_settings.file_format='FFMPEG'
scene.render.filepath=str(root/'ArtSource/Agumon'/('Agumon-motion-review-20261001.mp4' if args.quick else 'Agumon-motion-review.mp4'))
bpy.ops.render.render(animation=True)
print('AGUMON_MOTION_REEL_COMPLETE',scene.render.filepath)
