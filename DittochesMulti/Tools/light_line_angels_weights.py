import bpy,json
from pathlib import Path
base=Path(__file__).resolve().parents[1]/'ArtSource/LightLine-20261013'
out={}
for ident in ['angewomon','ophanimon']:
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.gltf(filepath=str(base/ident/'model.glb'))
 out[ident]=[]
 for obj in bpy.context.scene.objects:
  if obj.type!='MESH':continue
  bad=[v for v in obj.data.vertices if sum(g.weight for g in v.groups)<.001]
  out[ident].append(dict(name=obj.name,bad=len(bad),coords=[list(v.co) for v in bad],indices=[v.index for v in bad],groups=[dict(name=g.name,count=sum(any(vg.group==g.index for vg in v.groups) for v in obj.data.vertices)) for g in obj.vertex_groups]))
(base/'angels-weight-inspection.json').write_text(json.dumps(out,indent=2))
