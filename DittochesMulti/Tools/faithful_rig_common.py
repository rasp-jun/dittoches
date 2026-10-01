"""Canonical source import and actual mesh inspection, shared by rig authoring."""
import bpy
import math
from pathlib import Path
from mathutils import Vector, Matrix

ROOT=Path(__file__).resolve().parent.parent
BACKUP=ROOT/'ArtSource/AnimationBackup-20261001'
OUT=ROOT/'ArtSource/AnimatedReview'


def bounds(objects):
    points=[o.matrix_world@Vector(p) for o in objects for p in o.bound_box]
    return (Vector([min(v[i] for v in points) for i in range(3)]),
            Vector([max(v[i] for v in points) for i in range(3)]))


def import_static(ident):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    source=BACKUP/ident/'model.glb'
    if ident=='greymon':
        import json
        source=Path(json.loads((BACKUP/ident/'source.json').read_text(encoding='utf-8-sig'))['original_mesh_file'])
    bpy.ops.import_scene.gltf(filepath=str(source))
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    assert not any(o.type=='ARMATURE' for o in bpy.context.scene.objects),'Keep existing source rigs intact'
    turn={'tsunomon':-90,'pyocomon':90}.get(ident,0)
    rotation=Matrix.Rotation(math.radians(turn),4,'Z')
    for obj in meshes:
        if obj.data.users>1:obj.data=obj.data.copy()
        transform=rotation@obj.matrix_world
        obj.parent=None;obj.matrix_world=Matrix.Identity(4)
        obj.data.transform(transform)
    if ident=='greymon':
        from prepare_faithful_creatures import rejoin_export_chunks
        meshes,_=rejoin_export_chunks(meshes)
        triangles=sum(len(o.data.polygons) for o in meshes)
        for obj in meshes:
            bpy.context.view_layer.objects.active=obj
            dec=obj.modifiers.new('Continuous source reduction','DECIMATE')
            dec.ratio=min(1,118000/triangles)
            bpy.ops.object.modifier_apply(modifier=dec.name)
            if obj.data.has_custom_normals:obj.data.normals_split_custom_set([(0,0,0)]*len(obj.data.loops))
    bpy.context.view_layer.update()
    lo,hi=bounds(meshes);scale=2.4/(hi.z-lo.z)
    center=(lo+hi)*.5;center.z=lo.z
    for obj in meshes:
        for v in obj.data.vertices:v.co=(v.co-center)*scale
        if ident=='holyangemon':
            # The sword shifts the bounds centre sideways; wings shift it
            # backwards. Anatomical placement uses the torso centre instead.
            for v in obj.data.vertices:v.co-=Vector((.44,-.70,0))
        obj.data.update()
    for obj in list(bpy.context.scene.objects):
        if obj not in meshes:bpy.data.objects.remove(obj,do_unlink=True)
    bpy.context.view_layer.update()
    return meshes


def studio(meshes):
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=12
    scene.cycles.use_denoising=True;scene.render.resolution_x=640;scene.render.resolution_y=640
    scene.render.resolution_percentage=100;scene.view_settings.view_transform='Standard'
    scene.world=bpy.data.worlds.new('Neutral studio');scene.world.use_nodes=True
    scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.28,.32,.37,1)
    scene.world.node_tree.nodes['Background'].inputs[1].default_value=.65
    for name,location,power,size in [('Key',(-3,-4,6),280,5),('Fill',(4,-1,3),100,5)]:
        data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size
        light=bpy.data.objects.new(name,data);scene.collection.objects.link(light);light.location=location
        light.rotation_euler=(Vector((0,0,1.2))-light.location).to_track_quat('-Z','Y').to_euler()
    cd=bpy.data.cameras.new('Inspection');cam=bpy.data.objects.new('Inspection',cd)
    scene.collection.objects.link(cam);scene.camera=cam;cd.type='ORTHO';cd.ortho_scale=3.1
    lo,hi=bounds(meshes);cd.ortho_scale=max(3.1,(hi.x-lo.x)*1.15,(hi.y-lo.y)*1.15)
    return cam


def render(path,camera,position=(3,-6,3),target=(0,0,1.2)):
    camera.location=position;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
    bpy.context.scene.render.filepath=str(path);bpy.ops.render.render(write_still=True)
