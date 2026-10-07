"""Deterministic contact shading and faithful per-face engine mesh export.

The AO is deliberately short range: it describes creases and overlapping plates,
not a cast shadow that would remain fixed when the character moves.
"""
import math
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ATTRIBUTE='ContactAO'


def bake_contact_ao(objects,rays=12):
    coords=[];faces=[]
    for obj in objects:
        mesh=obj.data;mesh.calc_loop_triangles();offset=len(coords)
        coords.extend(obj.matrix_world@v.co for v in mesh.vertices)
        faces.extend(tuple(offset+i for i in tri.vertices) for tri in mesh.loop_triangles)
    tree=BVHTree.FromPolygons(coords,faces,all_triangles=True)
    height=max(v.z for v in coords)-min(v.z for v in coords)
    distance=max(.06,height*.075);epsilon=height*.0012
    hemisphere=[]
    for i in range(rays):
        radius=math.sqrt((i+.5)/rays);angle=i*2.399963229728653
        hemisphere.append((radius*math.cos(angle),radius*math.sin(angle),math.sqrt(1-radius*radius)))
    samples=0;minimum=1
    for obj in objects:
        mesh=obj.data
        attr=mesh.color_attributes.get(ATTRIBUTE) or mesh.color_attributes.new(name=ATTRIBUTE,type='FLOAT_COLOR',domain='POINT')
        mesh.color_attributes.active_color=attr
        normal_matrix=obj.matrix_world.to_3x3().inverted().transposed()
        # Thin eye layers must stay legible and cannot shadow each other.
        clean=any(word in obj.name.lower() for word in ('eye','iris','pupil','shine','catchlight','visor slit'))
        for vertex in mesh.vertices:
            amount=1.0
            if not clean:
                p=obj.matrix_world@vertex.co;n=(normal_matrix@vertex.normal).normalized()
                u=n.cross(Vector((0,0,1)) if abs(n.z)<.9 else Vector((0,1,0))).normalized();v=n.cross(u)
                occlusion=0
                for x,y,z in hemisphere:
                    direction=u*x+v*y+n*z
                    hit,_,_,length=tree.ray_cast(p+n*epsilon,direction,distance)
                    if hit is not None:occlusion+=(1-length/distance)**.6
                amount=1-.40*occlusion/rays
            attr.data[vertex.index].color=(amount,amount,amount,1)
            minimum=min(minimum,amount);samples+=1
    return {'vertices':samples,'rays':rays,'minimum_factor':minimum,'radius':distance}


def engine_mesh(objects,indices,conversion):
    """Preserve material boundaries and sharp normals, sharing smooth vertices."""
    vertices=[];triangles=[]
    for obj in objects:
        mesh=obj.data;mesh.calc_loop_triangles();cache={}
        attr=mesh.color_attributes.get(ATTRIBUTE)
        normal_matrix=obj.matrix_world.to_3x3().inverted().transposed()
        for tri in mesh.loop_triangles:
            polygon=mesh.polygons[tri.polygon_index]
            material=obj.data.materials[polygon.material_index]
            tint=material.diffuse_color
            corners=[]
            for index in tri.vertices:
                vertex=mesh.vertices[index]
                normal=(normal_matrix@(vertex.normal if polygon.use_smooth else polygon.normal)).normalized()
                normal=conversion.to_3x3()@normal
                key=(index,polygon.material_index,tuple(round(x,6) for x in normal))
                if key not in cache:
                    weights=sorted([(indices[obj.vertex_groups[g.group].name],g.weight) for g in vertex.groups
                                    if obj.vertex_groups[g.group].name in indices],key=lambda x:-x[1])[:4]
                    total=sum(w for _,w in weights)
                    if total<=0:raise ValueError('Unweighted vertex: '+obj.name)
                    weights=[(i,w/total) for i,w in weights]
                    ao=attr.data[index].color[0] if attr else 1
                    colour=(tint[0]*ao,tint[1]*ao,tint[2]*ao,1)
                    cache[key]=len(vertices)
                    vertices.append((conversion@(obj.matrix_world@vertex.co),normal,colour,weights))
                corners.append(cache[key])
            triangles.append((corners[0],corners[2],corners[1]))
    return vertices,triangles
