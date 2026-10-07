"""Load the preserved Rosemon public workshop model with the free SourceIO importer."""
import sys,json,os
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT.parent/'tmp'))
sys.path.insert(0,str(ROOT/'Tools'))
import bpy
import SourceIO
from SourceIO.blender_bindings.bindings import register
from SourceIO.library.shared.content_manager import ContentManager
from SourceIO.library.shared.content_manager.providers.loose_files import LooseFilesContentProvider
from SourceIO.library.utils.tiny_path import TinyPath

bpy.ops.wm.read_factory_settings(use_empty=True)
register()
folder=ROOT/'ArtSource/ThirdPartyCandidates/RosemonWorkshop-2451134533/data'
manager=ContentManager();manager.add_child(LooseFilesContentProvider(TinyPath(str(folder))))
result=bpy.ops.sourceio.mdl(filepath=str(folder/'models/debiddo/rosemon/pm.mdl'),
    directory=str(folder/'models/debiddo/rosemon'),files=[{'name':'pm.mdl'}],
    discover_resources=False,import_textures=True,import_animations=False,import_physics=False,
    create_flex_drivers=False)
print('ROSEMON IMPORT',result)
for obj in bpy.context.scene.objects:
    print('ROSEMON OBJECT',obj.name,obj.type,tuple(obj.dimensions))
for mat in bpy.data.materials:
    print('ROSEMON MATERIAL',mat.name,[(n.name,n.image.name if n.type=='TEX_IMAGE' and n.image else '') for n in mat.node_tree.nodes] if mat.use_nodes else [])
bpy.ops.wm.save_as_mainfile(filepath=str(folder.parent/'rosemon-import.blend'))
