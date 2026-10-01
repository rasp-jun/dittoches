"""Run with Blender --background --python Tools/validate_eye_winding.py.

Exercise the actual eye generator against the preserved skull without saving
or regenerating assets. Check outward faces and unchanged vertex positions.
"""
import ast
import math
from pathlib import Path

import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

root = Path(__file__).resolve().parent.parent
bpy.ops.wm.open_mainfile(filepath=str(root / 'ArtSource/Agumon/Agumon.blend'))
head = bpy.data.objects['Agumon skull and muzzle'].data
surface = BVHTree.FromPolygons(
    [v.co for v in head.vertices], [list(p.vertices) for p in head.polygons])
source = ast.parse((root / 'Tools/author_agumon.py').read_text(encoding='utf-8'))
function = next(n for n in source.body if isinstance(n, ast.FunctionDef) and n.name == 'eye_patch')
loop = next(n for n in source.body if isinstance(n, ast.For)
            and isinstance(n.target, ast.Tuple)
            and [v.id for v in n.target.elts] == ['side', 'suffix']
            and any(isinstance(call, ast.Call) and isinstance(call.func, ast.Name)
                    and call.func.id == 'eye_patch' for call in ast.walk(n)))
# Run the real six layer calls on both sides; exclude the later nose/teeth work.
stop = next(i for i, n in enumerate(loop.body) if isinstance(n, ast.Assign)
            and isinstance(n.targets[0], ast.Name) and n.targets[0].id == 'nostril_start')
loop.body = loop.body[:stop]
counts = {'layers': 0, 'faces': 0, 'preserved_inward_faces': 0}


def inspect(name, vertices, faces, material, bone):
    original = bpy.data.objects[name].data
    assert bone == 'Head', name
    assert len(vertices) == len(original.vertices), name + ': vertex count'
    assert len(faces) == len(original.polygons), name + ': face count'
    for position, vertex in zip(vertices, original.vertices):
        assert (Vector(position) - vertex.co).length < 1e-6, name + ': geometry changed'
    outward = Vector((-.8 if name.endswith('L') else .8, .6, 0)).normalized()
    mesh = bpy.data.meshes.new('Eye winding regression')
    try:
        mesh.from_pydata(vertices, [], faces)
        mesh.update()
        for face in mesh.polygons:
            assert face.normal.dot(outward) > .1, name + ': inward or degenerate face'
        counts['preserved_inward_faces'] += sum(p.normal.dot(outward) < 0 for p in original.polygons)
        counts['layers'] += 1
        counts['faces'] += len(mesh.polygons)
    finally:
        bpy.data.meshes.remove(mesh)


namespace = dict(Vector=Vector, math=math, head_surface=surface, mesh_obj=inspect)
namespace.update(dict.fromkeys(('ink', 'white', 'iris', 'iris_light', 'highlight')))
exec(compile(ast.Module(body=[function, loop], type_ignores=[]), '<eye generator>', 'exec'), namespace)
assert counts['layers'] == 12 and counts['faces'] == 2880, counts
print('EYE WINDING PASS:', counts, '; positions unchanged; no assets saved')
