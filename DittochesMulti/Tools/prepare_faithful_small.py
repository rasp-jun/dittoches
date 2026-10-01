"""Prepare five attributed source models for the separate faithful GLB gallery.

Run with Blender --background --disable-autoexec --python-exit-code 1
  --python Tools/prepare_faithful_small.py
Original candidate files are never modified. No existing game asset is written.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import shutil
import struct
import sys

import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parent.parent
CANDIDATES=ROOT/'ArtSource/ThirdPartyCandidates'
DESTINATION=ROOT/'ArtSource/FaithfulGallery'
TARGETS=[
    ('koromon','koromon_alternative',(1.3,-3,.9)),
    ('tsunomon','tsunomon',(3,-1,.9)),
    ('pyocomon','pyocomon',(-3,-1.3,.9)),
    ('mochimon','mochimon',(1.3,-3,.9)),
    ('birdramon','birdramon',(1.3,-3,.9)),
]
parser=argparse.ArgumentParser()
parser.add_argument('--max-triangles',type=int,default=120000)
parser.add_argument('--ids',default='')
parser.add_argument('--preview-only',action='store_true')
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])


def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()


def glb_metadata(path):
    data=path.read_bytes()
    assert data[:4]==b'glTF' and struct.unpack_from('<I',data,4)[0]==2
    offset=12;document=None
    while offset<len(data):
        length,kind=struct.unpack_from('<II',data,offset);offset+=8
        if kind==0x4E4F534A:document=json.loads(data[offset:offset+length].decode('utf-8'))
        offset+=length
    assert document is not None
    return {'animations':[a.get('name','unnamed') for a in document.get('animations',[])],
            'animation_channels':[len(a.get('channels',[])) for a in document.get('animations',[])],
            'skins':len(document.get('skins',[])),
            'skin_joint_counts':[len(s.get('joints',[])) for s in document.get('skins',[])],
            'materials':len(document.get('materials',[])),
            'images':len(document.get('images',[])),
            'textures':len(document.get('textures',[]))}


def mesh_counts():
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    return {'meshes':len(meshes),'vertices':sum(len(o.data.vertices) for o in meshes),
            'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)}


def texture_pixels():
    """Decoded pixel digests detect changes independently of PNG compression."""
    from array import array
    hashes=[]
    for img in bpy.data.images:
        if img.type not in {'IMAGE','UV_TEST'} or not img.has_data:continue
        values=array('f',[0])*len(img.pixels);img.pixels.foreach_get(values)
        hashes.append({'size':list(img.size),'sha256_decoded_float_pixels':hashlib.sha256(values.tobytes()).hexdigest()})
    return sorted(hashes,key=lambda row:(row['size'],row['sha256_decoded_float_pixels']))


def render_preview(out,offset):
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    points=[o.matrix_world@Vector(p) for o in meshes for p in o.bound_box]
    lo=Vector(tuple(min(p[i] for p in points) for i in range(3)))
    hi=Vector(tuple(max(p[i] for p in points) for i in range(3)))
    center=(lo+hi)/2;size=max(hi-lo)
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=16;scene.cycles.use_denoising=True
    scene.render.resolution_x=640;scene.render.resolution_y=640;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='Standard'
    world=bpy.data.worlds.new('Faithful inspection studio');world.use_nodes=True
    world.node_tree.nodes['Background'].inputs[0].default_value=(.40,.43,.49,1)
    world.node_tree.nodes['Background'].inputs[1].default_value=.55;scene.world=world
    for i,(direction,power) in enumerate([((2,3,4),65),((-3,1,2),30),((0,-3,3),45)]):
        data=bpy.data.lights.new('Preview area '+str(i),'AREA');data.energy=power*size*size;data.shape='DISK';data.size=size*2
        obj=bpy.data.objects.new(data.name,data);scene.collection.objects.link(obj)
        obj.location=center+Vector(direction)*size*.8;obj.rotation_euler=(center-obj.location).to_track_quat('-Z','Y').to_euler()
    data=bpy.data.cameras.new('Faithful preview camera');cam=bpy.data.objects.new(data.name,data);scene.collection.objects.link(cam)
    data.type='ORTHO';data.ortho_scale=size*1.32;scene.camera=cam
    cam.location=center+Vector(offset)*size;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(out/'preview.png');bpy.ops.render.render(write_still=True)


reports=[]
for ident,candidate,camera_offset in TARGETS:
    if args.ids and ident not in args.ids.split(','):continue
    folder=CANDIDATES/candidate;source=folder/(candidate+'.glb')
    out=DESTINATION/ident;out.mkdir(parents=True,exist_ok=True)
    target=out/'model.glb';source_hash=sha(source);source_glb=glb_metadata(source)
    if args.preview_only:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.gltf(filepath=str(target));render_preview(out,camera_offset)
        print('FAITHFUL PREVIEW UPDATED',ident,flush=True)
        continue
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(source))
    before=mesh_counts();pixels_before=texture_pixels();operations=[]
    if ident=='birdramon' or before['triangles']<=args.max_triangles:
        shutil.copy2(source,target)
        operations.append('Byte-identical copy: source materials, UVs, skeleton and animation streams preserved.')
    else:
        if ident=='koromon':
            # The original exporter split one sculpt at its 16-bit vertex
            # limit into four interleaved chunks. Decimating each chunk in
            # isolation creates cracks over the whole face. Rejoin only
            # matching-material chunks and weld coincident boundary vertices.
            by_material={}
            for obj in [o for o in bpy.context.scene.objects if o.type=='MESH']:
                key=tuple(m.name for m in obj.data.materials)
                by_material.setdefault(key,[]).append(obj)
            for group in by_material.values():
                if len(group)<2:continue
                bpy.ops.object.select_all(action='DESELECT')
                for obj in group:obj.select_set(True)
                obj=group[0];bpy.context.view_layer.objects.active=obj;bpy.ops.object.join()
                import bmesh
                bm=bmesh.new();bm.from_mesh(obj.data)
                span=max(max(v.co[i] for v in bm.verts)-min(v.co[i] for v in bm.verts) for i in range(3))
                bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=span*1e-7)
                bm.to_mesh(obj.data);bm.free();obj.data.update()
            operations.append('Rejoined original 16-bit sculpt chunks sharing a material and welded exact seam duplicates before reduction; corner colours retained.')
        # Leave a small allowance for exporter seam splitting and triangulation.
        ratio=(args.max_triangles-4000)/before['triangles']
        for obj in [o for o in bpy.context.scene.objects if o.type=='MESH']:
            triangles=sum(len(p.vertices)-2 for p in obj.data.polygons)
            if triangles<400:continue
            bpy.context.view_layer.objects.active=obj
            mod=obj.modifiers.new('Gallery triangle budget','DECIMATE');mod.decimate_type='COLLAPSE'
            mod.ratio=ratio;mod.use_collapse_triangulate=True;mod.delimit={'UV','MATERIAL','SEAM'}
            bpy.ops.object.modifier_apply(modifier=mod.name)
            # Source split normals describe the dense surface. Interpolating
            # them onto collapsed faces can cause dark self-shadow speckles.
            # Zero custom normals ask Blender to derive them from the new mesh.
            if obj.data.has_custom_normals:
                obj.data.normals_split_custom_set([(0.0,0.0,0.0)]*len(obj.data.loops))
            obj.data.update()
        after=mesh_counts()
        assert after['triangles']<=args.max_triangles,(ident,after)
        bpy.ops.export_scene.gltf(filepath=str(target),export_format='GLB',export_animations=True,
                                  export_animation_mode='ACTIONS',export_yup=True)
        operations.append('Static mesh decimation with UV, material and seam boundaries protected. No palette or texture edits.')
    # Re-open the delivered file, so verification and previews reflect delivery.
    bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(target))
    after=mesh_counts();pixels_after=texture_pixels();delivered=glb_metadata(target)
    assert pixels_before==pixels_after,(ident,'Decoded embedded texture pixels changed')
    assert source_glb['animations']==delivered['animations']
    assert source_glb['animation_channels']==delivered['animation_channels']
    assert source_glb['skin_joint_counts']==delivered['skin_joint_counts']
    assert sha(source)==source_hash,'Original candidate was unexpectedly changed'
    report={'id':ident,'candidate':candidate,'source_path':str(source),'source_sha256':source_hash,
      'output_path':str(target),'output_sha256':sha(target),'source':before,'output':after,
      'triangles_before':before['triangles'],'triangles_after':after['triangles'],
      'animations':delivered['animations'],'skin_joint_counts':delivered['skin_joint_counts'],
      'source_glb':source_glb,'output_glb':delivered,'texture_pixels_preserved':True,
      'source_file_preserved':True,'byte_identical':source_hash==sha(target),'operations':operations,
      'limitations':[] if delivered['animations'] else ['Source is a static model; no authored animation has been claimed.']}
    provenance=json.loads((folder/'source.json').read_text(encoding='utf-8'))
    provenance['id']=ident;provenance['source_candidate_id']=candidate
    provenance['status']='Prepared for a separate local visual gallery; existing game assets remain unchanged.'
    provenance['optimization_report']='optimize-report.json'
    (out/'source.json').write_text(json.dumps(provenance,indent=2,ensure_ascii=False),encoding='utf-8')
    (out/'optimize-report.json').write_text(json.dumps(report,indent=2,ensure_ascii=False),encoding='utf-8')
    render_preview(out,camera_offset);reports.append(report)
    print('FAITHFUL SMALL READY',ident,before['triangles'],'->',after['triangles'],len(delivered['animations']),'clips',flush=True)
all_reports=[json.loads((DESTINATION/ident/'optimize-report.json').read_text(encoding='utf-8'))
             for ident,_,_ in TARGETS if (DESTINATION/ident/'optimize-report.json').exists()]
(DESTINATION/'small-optimize-report.json').write_text(json.dumps(all_reports,indent=2,ensure_ascii=False),encoding='utf-8')
