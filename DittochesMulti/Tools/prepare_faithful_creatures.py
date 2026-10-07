"""Preserve and optimise independently sourced creature candidates for review.

Run with Blender, e.g. blender --background --python-exit-code 1
--python Tools/prepare_faithful_creatures.py -- --ids greymon,garurumon

Source GLBs are never edited. Original scale, pivot, materials, textures,
vertex colours and existing animation are retained. No motion is fabricated.
"""
import argparse
import hashlib
import json
import math
import shutil
import struct
import sys
from pathlib import Path

import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parent.parent


def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()


def write_source_record(candidate,out,report):
    folder=Path(candidate['local_file']).parent;original=folder/'source.json'
    source=json.loads(original.read_text(encoding='utf-8'))
    source['source_url']=source.get('model_url','https://sketchfab.com/3d-models/'+candidate['uid'])
    source['author']=source.get('author',candidate.get('author'))
    metadata=folder/'public-metadata.json'
    if metadata.is_file():source['license']=json.loads(metadata.read_text(encoding='utf-8')).get('license',source.get('license'))
    source.update({'original_source_record':str(original),'original_mesh_file':candidate['local_file'],
        'original_mesh_sha256':report['input_sha256'],'optimized_mesh_sha256':report['output_sha256'],
        'original_mesh_statistics':report['original_glb'],'optimized_mesh_statistics':report['output_glb'],
        'optimization_report':'optimize-report.json'})
    (out/'source.json').write_text(json.dumps(source,ensure_ascii=False,indent=2),encoding='utf-8')


def inspect_glb(path):
    blob=path.read_bytes();magic,version,length=struct.unpack_from('<4sII',blob)
    assert magic==b'glTF' and version==2 and length==len(blob),'Invalid GLB: '+str(path)
    size=struct.unpack_from('<I',blob,12)[0];doc=json.loads(blob[20:20+size]);triangles=0;vertices=0;colours=0;uv=0
    for mesh in doc.get('meshes',[]):
        for primitive in mesh.get('primitives',[]):
            attrs=primitive['attributes'];count=doc['accessors'][attrs['POSITION']]['count'];vertices+=count
            if primitive.get('mode',4)==4:
                triangles+=(doc['accessors'][primitive['indices']]['count'] if 'indices' in primitive else count)//3
            colours+='COLOR_0' in attrs;uv+='TEXCOORD_0' in attrs
    return {'bytes':len(blob),'triangles':triangles,'vertices':vertices,'meshes':len(doc.get('meshes',[])),
            'materials':len(doc.get('materials',[])),'textures':len(doc.get('textures',[])),
            'images':len(doc.get('images',[])),'vertex_colour_primitives':colours,'uv_primitives':uv,
            'skins':len(doc.get('skins',[])),'animations':[a.get('name','') for a in doc.get('animations',[])]}


def mesh_stats(meshes):
    result={'triangles':0,'vertices':0,'materials':set(),'uv_layers':0,'colour_layers':0,
            'colour_min':[1,1,1],'colour_max':[0,0,0],'chromatic_colour_samples':0}
    for obj in meshes:
        data=obj.data;data.calc_loop_triangles();result['triangles']+=len(data.loop_triangles);result['vertices']+=len(data.vertices)
        result['materials'].update(mat.name for mat in data.materials if mat)
        result['uv_layers']+=len(data.uv_layers);result['colour_layers']+=len(data.color_attributes)
        for attr in data.color_attributes:
            # Sample each source and output layer throughout its domain. This
            # catches vertex colours being dropped or replaced by flat white.
            step=max(1,len(attr.data)//4096)
            for index in range(0,len(attr.data),step):
                values=attr.data[index].color[:3]
                assert all(math.isfinite(v) for v in values),'Non-finite source colour'
                for k in range(3):result['colour_min'][k]=min(result['colour_min'][k],values[k]);result['colour_max'][k]=max(result['colour_max'][k],values[k])
                result['chromatic_colour_samples']+=max(values)-min(values)>.025
    result['materials']=sorted(result['materials']);return result


def rejoin_export_chunks(meshes):
    """Join same-material static chunks before reduction, never after it.

    These sculpts were split near a 16-bit index limit. Their chunk boundaries
    cross the face and coat, so isolated decimation opens tiny surface cracks.
    Only coincident positions at a scale-relative tolerance are welded.
    """
    import bmesh
    groups={}
    for obj in meshes:
        key=(tuple(mat.name if mat else None for mat in obj.data.materials),
             tuple((attr.name,attr.data_type,attr.domain) for attr in obj.data.color_attributes),
             tuple(layer.name for layer in obj.data.uv_layers))
        groups.setdefault(key,[]).append(obj)
    repairs=[]
    for group in groups.values():
        if len(group)<2:continue
        source_objects=[{'name':obj.name,'vertices':len(obj.data.vertices)} for obj in group]
        bpy.ops.object.select_all(action='DESELECT')
        for obj in group:obj.select_set(True)
        obj=group[0];bpy.context.view_layer.objects.active=obj;bpy.ops.object.join()
        bm=bmesh.new();bm.from_mesh(obj.data);before=len(bm.verts)
        span=max(max(v.co[i] for v in bm.verts)-min(v.co[i] for v in bm.verts) for i in range(3))
        tolerance=max(1e-10,span*1e-7)
        bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=tolerance)
        after=len(bm.verts);bm.to_mesh(obj.data);bm.free();obj.data.update()
        repairs.append({'source_objects':source_objects,'joined_object':obj.name,'vertices_before_weld':before,
                        'vertices_after_weld':after,'coincident_vertices_welded':before-after,'weld_distance':tolerance,
                        'near_16bit_limit_chunks':sum(row['vertices']>=65000 for row in source_objects)})
    return [obj for obj in bpy.context.scene.objects if obj.type=='MESH'],repairs


def studio_preview(meshes,out,ident):
    points=[obj.matrix_world@Vector(corner) for obj in meshes for corner in obj.bound_box]
    lower=Vector(tuple(min(p[i] for p in points) for i in range(3)))
    upper=Vector(tuple(max(p[i] for p in points) for i in range(3)))
    center=(lower+upper)/2;extent=max(upper-lower);scene=bpy.context.scene
    # Standard glTF front is +Z, which imports as Blender -Y.
    direction=Vector((.52,-1,.38)).normalized()
    camera_data=bpy.data.cameras.new('Review camera');camera=bpy.data.objects.new('Review camera',camera_data);scene.collection.objects.link(camera)
    camera.location=center+direction*extent*3;camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler()
    camera_data.type='ORTHO';camera_data.clip_start=max(.001,extent*.001);camera_data.clip_end=extent*30;scene.camera=camera
    bpy.context.view_layer.update();inverse=camera.matrix_world.inverted();projected=[inverse@point for point in points]
    minx=min(point.x for point in projected);maxx=max(point.x for point in projected)
    miny=min(point.y for point in projected);maxy=max(point.y for point in projected)
    camera_data.ortho_scale=max(maxx-minx,maxy-miny)*1.14
    camera.location+=camera.rotation_euler.to_quaternion()@Vector(((minx+maxx)/2,(miny+maxy)/2,0))
    world=bpy.data.worlds.new('Neutral review studio');scene.world=world;world.use_nodes=True
    world.node_tree.nodes['Background'].inputs['Color'].default_value=(.19,.21,.24,1)
    world.node_tree.nodes['Background'].inputs['Strength'].default_value=.35
    for name,offset,power,size in [('Key',(-2,-3,4),150,2.5),('Fill',(3,-1,2),60,3.0),('Rim',(0,3,3),180,2.0)]:
        data=bpy.data.lights.new(name,'AREA');data.energy=power*extent*extent;data.shape='DISK';data.size=size*extent
        light=bpy.data.objects.new(name,data);scene.collection.objects.link(light);light.location=center+Vector(offset)*extent
        light.rotation_euler=(center-light.location).to_track_quat('-Z','Y').to_euler()
    scene.render.engine='CYCLES';scene.cycles.samples=16;scene.cycles.use_denoising=True
    scene.render.resolution_x=720;scene.render.resolution_y=720;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='Standard';scene.render.film_transparent=False
    scene.render.filepath=str(out/'preview.png');bpy.ops.render.render(write_still=True)
    return {'bounds_min':list(lower),'bounds_max':list(upper),'camera_direction':list(direction)}


def prepare(candidate,maximum,render):
    ident=candidate['character_id'];source=Path(candidate['local_file']);out=ROOT/'ArtSource/FaithfulGallery'/ident;out.mkdir(parents=True,exist_ok=True)
    destination=out/'model.glb';source_hash=sha(source);original_glb=inspect_glb(source)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(source))
    meshes=[obj for obj in bpy.context.scene.objects if obj.type=='MESH'];before=mesh_stats(meshes)
    preserve_original=original_glb['skins']>0 or bool(original_glb['animations']) or original_glb['triangles']<=maximum
    edits=[];chunk_repairs=[];comparison=before
    if preserve_original:
        shutil.copy2(source,destination)
    else:
        if ident in {'garurumon','kabuterimon','atlur'}:
            meshes,chunk_repairs=rejoin_export_chunks(meshes)
            comparison=mesh_stats(meshes)
        ratio=min(1,maximum*.985/max(1,before['triangles']))
        for obj in meshes:
            obj.data.calc_loop_triangles();count=len(obj.data.loop_triangles)
            if count<128:continue
            # COLLAPSE interpolates UV and colour custom data. Modifier apply
            # preserves sharp face flags; export emits the resulting normals.
            bpy.context.view_layer.objects.active=obj;obj.select_set(True)
            mod=obj.modifiers.new('Review polygon budget','DECIMATE');mod.decimate_type='COLLAPSE';mod.ratio=ratio;mod.use_collapse_triangulate=True;mod.delimit={'UV','MATERIAL','SEAM'}
            bpy.ops.object.modifier_apply(modifier=mod.name)
            # The source's split normals describe its dense triangulation.
            # Recompute after collapse instead of interpolating stale normals.
            if obj.data.has_custom_normals:
                obj.data.normals_split_custom_set([(0.,0.,0.)]*len(obj.data.loops))
            obj.data.update()
            obj.data.calc_loop_triangles();edits.append({'object':obj.name,'triangles_before':count,'triangles_after':len(obj.data.loop_triangles)})
            obj.select_set(False)
        after=mesh_stats(meshes)
        assert after['triangles']<=maximum+512,(ident,'triangle budget',after['triangles'])
        assert after['materials']==before['materials'],ident+': material assignments changed'
        assert after['uv_layers']==comparison['uv_layers'],ident+': UV layers lost'
        assert after['colour_layers']==comparison['colour_layers'],ident+': original vertex colours lost'
        if before['chromatic_colour_samples']:
            assert after['chromatic_colour_samples']>0,ident+': vertex colour palette washed out'
        bpy.ops.object.select_all(action='DESELECT')
        for obj in bpy.context.scene.objects:
            if obj.type in ('MESH','ARMATURE'):obj.select_set(True)
        bpy.ops.export_scene.gltf(filepath=str(destination),export_format='GLB',use_selection=True,
            export_animations=True,export_animation_mode='ACTIONS',export_yup=True,
            export_vertex_color='MATERIAL',export_all_vertex_colors=True,export_materials='EXPORT')
    final=inspect_glb(destination)
    # Verify and render the actual delivered GLB, not a pre-export scene.
    bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(destination))
    meshes=[obj for obj in bpy.context.scene.objects if obj.type=='MESH'];after=mesh_stats(meshes)
    assert final['images']>=original_glb['images'],ident+': embedded texture images were dropped'
    if original_glb['vertex_colour_primitives']:assert final['vertex_colour_primitives']>0,ident+': GLB vertex colours were dropped'
    assert final['animations']==original_glb['animations'],ident+': animation set changed'
    assert sha(source)==source_hash,ident+': source file was modified'
    report={'id':ident,'source_uid':candidate['uid'],'input_file':str(source),'output_file':str(destination),
            'input_sha256':source_hash,'output_sha256':sha(destination),'operation':'byte-preserving copy' if preserve_original else 'Blender collapse decimation',
            'target_triangle_max':maximum,'original_glb':original_glb,'output_glb':final,'source_mesh':before,'output_mesh':after,
            'decimation':edits,'chunk_repairs':chunk_repairs,'source_license':candidate.get('license_from_public_metadata'),'source_author':candidate.get('author'),
            'scale_and_pivot':'Original world placement and scale preserved; equivalent static mesh chunks consolidated under an existing object transform.' if chunk_repairs else 'Original node transforms preserved; no normalization or axis edits.',
            'animation_status':'Original animation retained.' if final['animations'] else 'Static sculpt; no invented animation.',
            'preview':studio_preview(meshes,out,ident) if render else None}
    (out/'optimize-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    write_source_record(candidate,out,report)
    print('FAITHFUL CREATURE READY',ident,original_glb['triangles'],'->',final['triangles'],'triangles',flush=True)
    return report


def main():
    parser=argparse.ArgumentParser();parser.add_argument('--ids',default='');parser.add_argument('--max-triangles',type=int,default=120000);parser.add_argument('--no-render',action='store_true');parser.add_argument('--render-only',action='store_true')
    args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    candidates=json.loads((ROOT/'ArtSource/ThirdPartyCandidates/creature-downloads.json').read_text(encoding='utf-8'))
    reports=[]
    for candidate in candidates:
        if args.ids and candidate['character_id'] not in args.ids.split(','):continue
        if args.render_only:
            ident=candidate['character_id'];out=ROOT/'ArtSource/FaithfulGallery'/ident
            report=json.loads((out/'optimize-report.json').read_text(encoding='utf-8'))
            bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(out/'model.glb'))
            meshes=[obj for obj in bpy.context.scene.objects if obj.type=='MESH']
            report['preview']=studio_preview(meshes,out,ident)
            (out/'optimize-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
            write_source_record(candidate,out,report)
            reports.append(report);print('FAITHFUL CREATURE PREVIEW',ident,flush=True)
        else:reports.append(prepare(candidate,args.max_triangles,not args.no_render))
    all_reports=[]
    for candidate in candidates:
        report_path=ROOT/'ArtSource/FaithfulGallery'/candidate['character_id']/'optimize-report.json'
        if report_path.is_file():all_reports.append(json.loads(report_path.read_text(encoding='utf-8')))
    (ROOT/'ArtSource/FaithfulGallery/creature-optimization-report.json').write_text(json.dumps(all_reports,ensure_ascii=False,indent=2),encoding='utf-8')


if __name__=='__main__':main()
