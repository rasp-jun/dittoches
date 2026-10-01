"""Reviewable proportion study of a CC-BY fan mesh, never primitive replacement.

Blender --background --disable-autoexec --threads 3 --python-exit-code 1
  --python Tools/refine_faithful_agumon.py
"""
import hashlib
import json
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parent.parent
SOURCE=ROOT/'ArtSource/ThirdPartyCandidates/agumon/2ca5f3152c004734b00a71efee87960a/model.glb'
OUT=ROOT/'ArtSource/AgumonRefinement'
OUT.mkdir(parents=True,exist_ok=True)

def smooth(a,b,x):
    t=max(0.,min(1.,(x-a)/(b-a)))
    return t*t*(3.-2.*t)

def digest(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()

def render(name):
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    points=[o.matrix_world@Vector(p) for o in meshes for p in o.bound_box]
    lo=Vector(tuple(min(p[i] for p in points) for i in range(3)))
    hi=Vector(tuple(max(p[i] for p in points) for i in range(3)))
    center=(lo+hi)/2.; size=max(hi-lo)
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12
    scene.cycles.use_denoising=True
    scene.render.resolution_x=640;scene.render.resolution_y=640
    scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG'
    scene.view_settings.view_transform='Standard'
    world=bpy.data.worlds.new('Neutral inspection');world.use_nodes=True
    world.node_tree.nodes['Background'].inputs[0].default_value=(.40,.43,.49,1.)
    world.node_tree.nodes['Background'].inputs[1].default_value=.55;scene.world=world
    for i,(direction,power) in enumerate([((2,3,4),65),((-3,1,2),30),((0,-3,3),45)]):
        data=bpy.data.lights.new('Inspection '+str(i),'AREA')
        data.energy=power*size*size;data.shape='DISK';data.size=size*2
        obj=bpy.data.objects.new(data.name,data);scene.collection.objects.link(obj)
        obj.location=center+Vector(direction)*size*.8
        obj.rotation_euler=(center-obj.location).to_track_quat('-Z','Y').to_euler()
    data=bpy.data.cameras.new('Inspection camera');cam=bpy.data.objects.new(data.name,data)
    scene.collection.objects.link(cam);data.type='ORTHO';data.ortho_scale=size*1.32
    scene.camera=cam;cam.location=center+Vector((1.4,-3.,.9))*size
    cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(OUT/name);bpy.ops.render.render(write_still=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(SOURCE))
original_hash=digest(SOURCE)
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
vertices=sum(len(o.data.vertices) for o in meshes)
uv_before={o.name:[tuple(d.uv) for d in o.data.uv_layers.active.data] for o in meshes if o.data.uv_layers.active}
# Make the comparison from precisely the same renderer as the resulting study.
render('before.png')
for obj in list(bpy.context.scene.objects):
    if obj.type in {'CAMERA','LIGHT'}:bpy.data.objects.remove(obj,do_unlink=True)
for obj in meshes:
    inverse=obj.matrix_world.inverted()
    for vertex in obj.data.vertices:
        p=obj.matrix_world@vertex.co
        # Preserve attachment to the neck, with a smooth transition into the
        # broad, lower skull. Mouth, gums and teeth receive the same field.
        original_z=p.z
        skull=smooth(.65,1.65,original_z)
        p.x*=1.+.13*skull
        if original_z>1.25:p.z=1.25+(original_z-1.25)*.77
        # A little more chest and abdomen volume without moving feet or tail.
        torso=smooth(-3.55,-2.15,original_z)*(1.-smooth(.2,1.2,original_z))
        central=1.-smooth(1.35,2.2,abs(p.x))
        p.x*=1.+.10*torso*central
        # The eyes are a separate original mesh. Shrink their vertical and
        # longitudinal extent around the original surface centers; no new eyes.
        if obj.name=='Object_3':
            eye_z=1.25+(3.367-1.25)*.77
            p.z=eye_z+(p.z-eye_z)*.85
            p.y=-.315+(p.y+.315)*.90
        vertex.co=inverse@p
    if obj.data.has_custom_normals:
        obj.data.normals_split_custom_set([(0.,0.,0.)]*len(obj.data.loops))
    obj.data.update()
for obj in meshes:
    if obj.name in uv_before:
        assert uv_before[obj.name]==[tuple(d.uv) for d in obj.data.uv_layers.active.data]
bpy.ops.export_scene.gltf(filepath=str(OUT/'model.glb'),export_format='GLB',export_animations=True,export_yup=True)
assert original_hash==digest(SOURCE)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(OUT/'model.glb'))
render('preview.png')
source=json.loads(SOURCE.with_name('source.json').read_text(encoding='utf-8'))
source.update({
    'modified_by':'Codex, local noncommercial proportion study, 2026-10-01',
    'license_url':'https://creativecommons.org/licenses/by/4.0/',
    'attribution':'Agumon by pokedigilucas, CC BY 4.0. Proportion study modified by Codex.',
    'source_path':str(SOURCE.relative_to(ROOT)),
    'source_sha256':original_hash,
    'output_sha256':digest(OUT/'model.glb'),
    'status':'Experimental comparison; parent review required before replacing gallery model.',
    'reference':'Official Digimon Encyclopedia Agumon image at ArtSource/Roster/ReferencesV2/agumon.jpg',
    'alternative_review':{'author':'aaandro','uid':'00bafb51a08946ffbc35d49fd1946f17','decision':'Not chosen: taller forehead, larger eyes and exaggerated hands/claws diverge further from the official silhouette.'},
    'changes':['Original fan mesh retained: no replacement spheres or generated body parts.',
        'Upper head height reduced 23 percent above a neck transition; skull and muzzle widened 13 percent.',
        'Original eye meshes reduced vertically 15 percent and longitudinally 10 percent.',
        'Central abdomen/chest widened gently up to 10 percent.',
        'Existing topology, UV coordinates, textures and materials retained. Normals refreshed after deformation.'],
    'triangles':triangles,'vertices':vertices,'animations':[],
    'limitations':['A modest proportion study, not an exact original or Digimon Masters reproduction.',
        'Source remains static and unrigged.',
        'Forward bent arms, mouth corners, tooth scale, cranial contour and original foot anatomy still need manual sculpting and retopology.',
        'No new game motion or motion provenance is claimed.',
        'Digimon character rights remain with their respective owners.'],
    'source_file_preserved':True,'uv_coordinates_preserved':True
})
(OUT/'source.json').write_text(json.dumps(source,ensure_ascii=False,indent=2),encoding='utf-8')
print('AGUMON REFINEMENT READY',triangles,flush=True)
