"""Build independent textured source studies; never publishes an unreviewed model."""
import bpy,bmesh,sys,math,json,argparse
from pathlib import Path
from mathutils import Matrix,Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from faithful_rig_common import bounds,studio,render
from roster_rebuild_sources import NAMES,OUT

def surface(obj):
    bpy.context.view_layer.objects.active=obj
    # FDM1 uses the neutral surface and skeletal keys, not unused source morphs.
    if obj.data.shape_keys:
        assert all(abs(k.value)<1e-6 for k in obj.data.shape_keys.key_blocks),obj.name
        obj.shape_key_clear()
    mesh=obj.data;bm=bmesh.new();bm.from_mesh(mesh)
    bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000015)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    # Rejoin coplanar triangles only when UVs and material boundaries agree.
    bmesh.ops.join_triangles(bm,faces=list(bm.faces),angle_face_threshold=.25,
      angle_shape_threshold=.5,cmp_uvs=True,cmp_materials=True)
    creases=bm.edges.layers.float.get('crease_edge') or bm.edges.layers.float.new('crease_edge')
    for edge in bm.edges:
        if edge.is_boundary:edge[creases]=1
        elif edge.is_manifold and edge.calc_face_angle()>math.radians(52):edge[creases]=.8
    bm.to_mesh(mesh);bm.free();mesh.update()
    modifier=obj.modifiers.new('Silhouette and joint surface reconstruction','SUBSURF')
    modifier.levels=1;modifier.render_levels=1;modifier.boundary_smooth='PRESERVE_CORNERS'
    while list(obj.modifiers).index(modifier)>0:bpy.ops.object.modifier_move_up(modifier=modifier.name)
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    mesh=obj.data
    for face in mesh.polygons:face.use_smooth=True
    return len(mesh.vertices)

def prepare(ident):
    folder=OUT/ident;source=folder/'source'
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.wm.obj_import(filepath=str(source/'source.obj'),forward_axis='NEGATIVE_Z',up_axis='Y')
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    image=bpy.data.images.load(str(source/'diffuse.png'));image.pack()
    mat=bpy.data.materials.new(ident+' painted source');mat.use_nodes=True
    bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.78
    texture=mat.node_tree.nodes.new('ShaderNodeTexImage');texture.image=image
    mat.node_tree.links.new(texture.outputs['Color'],bs.inputs['Base Color'])
    mat.node_tree.links.new(texture.outputs['Color'],bs.inputs['Emission Color']);bs.inputs['Emission Strength'].default_value=.08
    for obj in meshes:
        obj.data.transform(obj.matrix_world);obj.matrix_world=Matrix.Identity(4)
        obj.data.materials.clear();obj.data.materials.append(mat)
        for face in obj.data.polygons:face.material_index=0
    bpy.context.view_layer.update();lo,hi=bounds(meshes);scale=2.4/(hi.z-lo.z);center=(lo+hi)*.5;center.z=lo.z
    for obj in meshes:
        for v in obj.data.vertices:v.co=(v.co-center)*scale
        obj.data.update();surface(obj)
    bpy.context.view_layer.update()
    bpy.ops.object.select_all(action='DESELECT')
    for obj in meshes:obj.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(folder/'static.glb'),export_format='GLB',use_selection=True,export_animations=False)
    camera=studio(meshes);scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=8
    scene.render.resolution_x=512;scene.render.resolution_y=512
    for name,pos in [('front',(0,-8,1.2)),('side',(8,0,1.2)),('preview',(3,-6,2.8))]:render(folder/('source-'+name+'.png'),camera,pos)
    bpy.ops.wm.save_as_mainfile(filepath=str(folder/'source-study.blend'))
    print('SOURCE STUDY',ident,flush=True)

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--ids',default=','.join(NAMES));a=p.parse_args(sys.argv[sys.argv.index('--')+1:])
    for ident in a.ids.split(','):prepare(ident)
