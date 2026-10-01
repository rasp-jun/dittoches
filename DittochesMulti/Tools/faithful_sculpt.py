"""Local edits to existing source surfaces, retaining UVs and attachments."""
import bpy
import numpy as np
from faithful_skin_topology import topology


def smoothstep(a,b,x):
    t=np.clip((x-a)/(b-a),0,1);return t*t*(3-2*t)


def refine_agumon(meshes):
    for obj in meshes:
        flat=np.empty(len(obj.data.vertices)*3,dtype=np.float32)
        obj.data.vertices.foreach_get('co',flat);xyz=flat.reshape(-1,3).astype(float)
        x,y,z=xyz.T.copy();front=1-smoothstep(-.82,-.35,y)
        face=smoothstep(1.48,1.67,z)*(1-smoothstep(2.05,2.16,z))
        # Round the broad front across its width, rather than leaving a plane
        # between two abrupt corners. Teeth/gums undergo the same deformation.
        edge=np.clip(np.abs(x)/.30,0,1)
        xyz[:,1]+=front*face*(.15*edge**2-.018*(1-edge**2))
        xyz[:,2]-=front*.048*smoothstep(1.76,1.99,z)*(1-smoothstep(2.05,2.16,z))
        xyz[:,2]-=front*face*.04*edge**2*smoothstep(1.79,1.90,z)
        xyz[:,0]*=1+.045*front*face
        if obj.name.startswith('Object_2'):
            # Smooth only the skin of the upper muzzle. Welded calculation
            # keeps both sides of texture seams in exactly the same position.
            points,inverse,edges,_=topology(obj.data,xyz)
            a,b=edges.T;degree=np.bincount(np.r_[a,b],minlength=len(points))
            mask=(1-smoothstep(-.75,-.36,points[:,1]))*smoothstep(1.75,1.83,points[:,2])
            mask*=1-smoothstep(2.04,2.13,points[:,2])
            for _ in range(6):
                neighbor=np.column_stack([(np.bincount(a,weights=points[b,k],minlength=len(points))+
                    np.bincount(b,weights=points[a,k],minlength=len(points)))/np.maximum(degree,1) for k in range(3)])
                points+=(neighbor-points)*(mask*.42)[:,None]
            xyz=points[inverse]
        obj.data.vertices.foreach_set('co',xyz.astype(np.float32).ravel())
        if obj.data.has_custom_normals:obj.data.normals_split_custom_set([(0,0,0)]*len(obj.data.loops))
        obj.data.update()
        for material in obj.data.materials:
            if not material or not material.use_nodes:continue
            colour={'aiStandardSurface4SG':(.025,.003,.006,1),'aiStandardSurface5SG':(.26,.025,.04,1)}.get(material.name)
            if colour:
                for node in material.node_tree.nodes:
                    if node.type=='BSDF_PRINCIPLED' and not node.inputs['Base Color'].is_linked:
                        node.inputs['Base Color'].default_value=colour
    return ['Rounded and softened the original upper muzzle surface; warped teeth and gums coherently; preserved UVs and original topology.',
            'Darkened the oral cavity and softened the saturated tongue colour for an enclosed mouth in the real-time viewer.']
