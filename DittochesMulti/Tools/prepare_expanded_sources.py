"""Import downloaded, attributed OBJ candidates without replacing their geometry.

The first pass saves neutral, textured source files for anatomical inspection.
It does not publish models or treat a successful import as quality approval.
"""
import sys, json, hashlib, shutil, argparse, math
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import bpy
from mathutils import Vector, Matrix
from faithful_rig_common import bounds, studio, render
from fetch_workshop_candidates import NAMES, PACKAGE, ROOT

OUT=ROOT/'ArtSource/ExpandedSources-20261002'
TURNS={}

def prepare(ident):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    source=PACKAGE/ident
    bpy.ops.wm.obj_import(filepath=str(source/'source.obj'),forward_axis='NEGATIVE_Z',up_axis='Y')
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    image=bpy.data.images.load(str(source/'diffuse.png'));image.pack()
    mat=bpy.data.materials.new(ident+' original diffuse');mat.use_nodes=True
    bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.82
    tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=image
    mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
    # Keep the painted game texture readable in the same neutral gallery light.
    mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Emission Color'])
    bs.inputs['Emission Strength'].default_value=.12
    rotation=Matrix.Rotation(math.radians(TURNS.get(ident,0)),4,'Z')
    for obj in meshes:
        obj.data.transform(rotation@obj.matrix_world)
        obj.matrix_world=Matrix.Identity(4)
        obj.data.materials.clear();obj.data.materials.append(mat)
        for poly in obj.data.polygons:poly.material_index=0;poly.use_smooth=True
    bpy.context.view_layer.update()
    lo,hi=bounds(meshes);scale=2.4/(hi.z-lo.z);center=(lo+hi)*.5;center.z=lo.z
    for obj in meshes:
        for vertex in obj.data.vertices:vertex.co=(vertex.co-center)*scale
        obj.data.update()
    bpy.context.view_layer.update()
    folder=OUT/ident;folder.mkdir(parents=True,exist_ok=True)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in meshes:obj.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(folder/'model.glb'),export_format='GLB',use_selection=True,export_animations=False)
    record=json.loads((source/'source.json').read_text(encoding='utf-8'))
    record['original_mesh_file']=str((source/'source.obj').relative_to(ROOT))
    record['original_mesh_sha256']=hashlib.sha256((source/'source.obj').read_bytes()).hexdigest()
    record['import_notes']=['Original OBJ geometry and UVs retained','Original diffuse texture embedded','Y-up source converted to Z-up and normalised to 2.4 units']
    (folder/'source.json').write_text(json.dumps(record,ensure_ascii=False,indent=2),encoding='utf-8')
    lo,hi=bounds(meshes)
    camera=studio(meshes);scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=8
    scene.render.resolution_x=480;scene.render.resolution_y=480
    bpy.ops.wm.save_as_mainfile(filepath=str(folder/(ident+'.blend')))
    for name,position in [('front',(0,-8,1.2)),('side',(8,0,1.2)),('preview',(3,-6,3))]:
        render(folder/(name+'.png'),camera,position)
    print('EXPANDED SOURCE',ident,'bounds',tuple(lo),tuple(hi),flush=True)

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--ids',default=','.join(NAMES))
    args=p.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    for ident in args.ids.split(','):prepare(ident)
