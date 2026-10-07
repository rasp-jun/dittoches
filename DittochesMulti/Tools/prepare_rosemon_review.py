"""Keep the native Rosemon rig; select the normal form and portable materials."""
import sys,json,hashlib,math
from pathlib import Path
import bpy
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'Tools'))
from faithful_rig_common import bounds,studio,render
source=ROOT/'ArtSource/ThirdPartyCandidates/RosemonWorkshop-2451134533'
bpy.ops.wm.open_mainfile(filepath=str(source/'rosemon-import.blend'))
for obj in list(bpy.context.scene.objects):
    if obj.type=='EMPTY' or obj.name in ('mesh/Whip2','mesh/Whip3','mesh/Tiferets'):
        bpy.data.objects.remove(obj,do_unlink=True)
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
for mat in {m for obj in meshes for m in obj.data.materials if m}:
    tex=next((n.image for n in mat.node_tree.nodes if n.type=='TEX_IMAGE' and n.name=='$basetexture'),None)
    mat.node_tree.nodes.clear()
    out=mat.node_tree.nodes.new('ShaderNodeOutputMaterial');bs=mat.node_tree.nodes.new('ShaderNodeBsdfPrincipled')
    mat.node_tree.links.new(bs.outputs['BSDF'],out.inputs['Surface']);bs.inputs['Roughness'].default_value=.72
    if tex and not tex.name.startswith('missing_'):
        node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=tex;tex.pack()
        mat.node_tree.links.new(node.outputs['Color'],bs.inputs['Base Color'])
        mat.node_tree.links.new(node.outputs['Color'],bs.inputs['Emission Color']);bs.inputs['Emission Strength'].default_value=.12
    else:bs.inputs['Base Color'].default_value=(.08,.015,.025,1)
lo,hi=bounds(meshes);scale=2.4/(hi.z-lo.z);center=(lo+hi)*.5;center.z=lo.z
transform=Matrix.Scale(scale,4)@Matrix.Translation(-center)
# Apply one shared transform to mesh data and the native rest skeleton.
for obj in meshes:
    world=obj.matrix_world.copy();obj.parent=None
    obj.data.transform(transform@world);obj.matrix_world=Matrix.Identity(4)
rig.data.transform(transform@rig.matrix_world);rig.matrix_world=Matrix.Identity(4)
for obj in meshes:obj.parent=rig
for bone in rig.pose.bones:bone.rotation_mode='QUATERNION'
bpy.context.view_layer.update()
out=ROOT/'ArtSource/ExpandedSources-20261002/rosemon';out.mkdir(parents=True,exist_ok=True)
record={'id':'rosemon','title':'Rosemon (normal form)','source_url':'https://steamcommunity.com/sharedfiles/filedetails/?id=2451134533',
 'author':'BANDAI NAMCO Entertainment (game); Debiddo (model port); Impmon (workshop publication)',
 'license':'Game-derived reference asset; redistribution permission not established',
 'original_mesh_file':str((source/'data/models/debiddo/rosemon/pm.mdl').relative_to(ROOT)),
 'original_mesh_sha256':hashlib.sha256((source/'data/models/debiddo/rosemon/pm.mdl').read_bytes()).hexdigest(),
 'provenance':'Public workshop file downloaded successfully with official SteamCMD anonymous access; preserved native rig and textures. Not original project artwork.',
 'usage':'Local appearance and animation review only; no external publication.',
 'import_notes':['Normal form: one whip, cape, chest orb; alternate whip meshes and Burst Mode orbs excluded','Native skin, bones, UVs and face morphs retained','Source shader converted to embedded glTF-compatible original diffuse textures']}
(out/'source.json').write_text(json.dumps(record,indent=2),encoding='utf-8')
print('NATIVE BONES',[(b.name,tuple(b.head_local),tuple(b.tail_local)) for b in rig.data.bones],flush=True)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for obj in meshes:obj.select_set(True)
bpy.ops.export_scene.gltf(filepath=str(out/'model.glb'),use_selection=True,export_format='GLB',export_animations=False)
camera=studio(meshes);bpy.context.scene.cycles.samples=8
bpy.ops.wm.save_as_mainfile(filepath=str(out/'rosemon.blend'))
for name,pos in [('front',(0,-8,1.2)),('side',(8,0,1.2)),('preview',(3,-6,3))]:render(out/(name+'.png'),camera,pos)
