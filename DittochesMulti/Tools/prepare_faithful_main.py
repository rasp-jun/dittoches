"""Prepare selected public, attributed GLBs for the local appearance gallery.

Run with Blender --background --python Tools/prepare_faithful_main.py.
Original downloads are immutable. This does not replace game resources.
"""
from pathlib import Path
import json
import shutil
import hashlib
import math
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parent.parent
SOURCES = ROOT / 'ArtSource/ThirdPartyCandidates'
OUT = ROOT / 'ArtSource/FaithfulGallery'
SELECTED = {
    'agumon': 'agumon/2ca5f3152c004734b00a71efee87960a',
    'holyangemon': 'holyangemon/023f6b60',
    'seraphimon': 'seraphimon/fe7ec900',
    'wargreymon': 'wargreymon/61d5dc0d',
}


def render_model(model, output):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(model))
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    bpy.context.view_layer.update()
    pts = [o.matrix_world @ Vector(v) for o in meshes for v in o.bound_box]
    lo = Vector(tuple(min(v[i] for v in pts) for i in range(3)))
    hi = Vector(tuple(max(v[i] for v in pts) for i in range(3)))
    size = hi-lo
    center = (hi+lo)*.5
    # Imported glTF is Z-up in Blender. Preserve its proportions and materials.
    extent = max(size)
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.device = 'CPU'
    scene.cycles.samples = 24
    scene.cycles.use_denoising = True
    scene.render.resolution_x = 800
    scene.render.resolution_y = 800
    scene.render.resolution_percentage = 100
    scene.view_settings.view_transform = 'Standard'
    scene.view_settings.look = 'None'
    scene.world = bpy.data.worlds.new('Neutral studio')
    scene.world.use_nodes = True
    scene.world.node_tree.nodes['Background'].inputs[0].default_value = (.34,.37,.42,1)
    scene.world.node_tree.nodes['Background'].inputs[1].default_value = .65
    scene.render.image_settings.file_format = 'PNG'
    for name, offset, strength in [('Key',(-2,-3,4),110),('Fill',(3,-1,2),40)]:
        data = bpy.data.lights.new(name,'AREA'); data.energy = strength*extent*extent
        data.shape='DISK';data.size = extent*2
        light = bpy.data.objects.new(name,data);scene.collection.objects.link(light)
        light.location = center + Vector(offset)*extent
        light.rotation_euler = (center-light.location).to_track_quat('-Z','Y').to_euler()
    cd = bpy.data.cameras.new('Review camera');cam=bpy.data.objects.new('Review camera',cd)
    scene.collection.objects.link(cam);scene.camera=cam;cd.type='ORTHO';cd.ortho_scale=extent*1.30
    # glTF forward +Z becomes Blender -Y.
    cam.location=center+Vector((1.8,-4.5,1.1))*extent
    cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(output)
    bpy.ops.render.render(write_still=True)
    triangles = 0
    for o in meshes:
        o.data.calc_loop_triangles();triangles+=len(o.data.loop_triangles)
    return {'triangles':triangles, 'mesh_objects':len(meshes),
            'bounds_blender':{'min':list(lo),'max':list(hi)},
            'materials':len(bpy.data.materials),'images':len(bpy.data.images)}


def main():
    for id, relative in SELECTED.items():
        src=SOURCES/relative;target=OUT/id;target.mkdir(parents=True,exist_ok=True)
        model = src/'model.glb'
        if not model.exists(): model=src/(id+'.glb')
        shutil.copy2(model,target/'model.glb')
        shutil.copy2(src/'source.json',target/'source.json')
        report=render_model(target/'model.glb',target/'preview.png')
        report.update(id=id,source=str(model.relative_to(ROOT)),
                      sha256=hashlib.sha256((target/'model.glb').read_bytes()).hexdigest(),
                      modified=False,animations=[],status='local appearance review; rigging pending')
        (target/'optimize-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
        print('FAITHFUL MAIN PREPARED',id,report,flush=True)


if __name__ == '__main__':
    main()
