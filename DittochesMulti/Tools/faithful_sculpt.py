"""Local edits to existing source surfaces, retaining UVs and attachments."""
import bpy
import numpy as np
from faithful_skin_topology import topology


def smoothstep(a,b,x):
    t=np.clip((x-a)/(b-a),0,1);return t*t*(3-2*t)


def repair_agumon_face_weights(meshes):
    """Keep upper-face vertices on Head, away from the nearby arm region."""
    changed=0
    for obj in meshes:
        head=obj.vertex_groups.get('Head')
        if not head:continue
        for vertex in obj.data.vertices:
            blend=float(smoothstep(1.34,1.50,vertex.co.z))
            if not blend:continue
            original={g.group:g.weight for g in vertex.groups if g.weight>0}
            weights=original.copy()
            for group_index,weight in original.items():
                name=obj.vertex_groups[group_index].name.lower()
                if not any(part in name for part in ('arm','hand')):continue
                amount=weight*blend
                weights[group_index]-=amount
                weights[head.index]=weights.get(head.index,0)+amount
            # Blend through the neck/lower jaw, then keep the complete skull
            # rigid. Detached eyes/brows also need Head instead of Spine.
            # The tongue remains free to follow the lower jaw.
            rigid=float(smoothstep(1.52,1.78,vertex.co.z)) if obj.name!='Object_5' else 0
            if obj.name in ('Object_3','Object_6'):rigid=1
            weights={group:weight*(1-rigid) for group,weight in weights.items()}
            weights[head.index]=weights.get(head.index,0)+rigid
            weights=dict(sorted(((g,w) for g,w in weights.items() if w>1e-8),key=lambda pair:-pair[1])[:4])
            total=sum(weights.values());weights={g:w/total for g,w in weights.items()}
            if original.keys()==weights.keys() and all(abs(original[g]-w)<1e-7 for g,w in weights.items()):continue
            for group_index in original:obj.vertex_groups[group_index].remove([vertex.index])
            for group_index,weight in weights.items():obj.vertex_groups[group_index].add([vertex.index],weight,'REPLACE')
            changed+=1
    return changed


def refine_agumon(meshes):
    for obj in meshes:
        flat=np.empty(len(obj.data.vertices)*3,dtype=np.float32)
        obj.data.vertices.foreach_get('co',flat);xyz=flat.reshape(-1,3).astype(float)
        x,y,z=xyz.T.copy();front=1-smoothstep(-.82,-.35,y)
        face=smoothstep(1.48,1.67,z)*(1-smoothstep(2.05,2.16,z))
        # Round the broad front across its width, rather than leaving a plane
        # between two abrupt corners. Teeth/gums undergo the same deformation.
        # Keep the falloff across the entire muzzle: clamping at .30 left
        # a flat strip at either side of the wider source face.
        edge=np.clip(np.abs(x)/.46,0,1)
        xyz[:,1]+=front*face*(.23*edge**2-.008*(1-edge**2))
        xyz[:,1]+=front*face*.035*np.clip((z-1.80)/.28,-1,1)**2
        xyz[:,2]-=front*.048*smoothstep(1.76,1.99,z)*(1-smoothstep(2.05,2.16,z))
        xyz[:,2]-=front*face*.04*edge**2*smoothstep(1.79,1.90,z)
        xyz[:,0]*=1-.025*front*face
        # Turn the complete eye region gently forward, including the skin
        # socket and detached eye surface. One spatial warp keeps their seam
        # aligned and exposes the eyes at three-quarter/front review angles.
        eye=smoothstep(.20,.36,np.abs(x))
        eye*=smoothstep(-.52,-.30,y)*(1-smoothstep(.04,.28,y))
        eye*=smoothstep(1.72,1.86,z)*(1-smoothstep(2.11,2.26,z))
        angle=np.sign(x)*np.radians(-18)*eye
        dx=xyz[:,0]-np.sign(x)*.31;dy=xyz[:,1]+.08
        xyz[:,0]=np.sign(x)*.31+dx*np.cos(angle)-dy*np.sin(angle)
        xyz[:,1]=-.08+dx*np.sin(angle)+dy*np.cos(angle)
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
    return ['Rounded the full muzzle width and vertical front profile with a mild taper; warped teeth and gums coherently; preserved UVs and original topology.',
            'Turned both eye regions gently forward with a shared smooth deformation of the eyes and surrounding skin.',
            'Darkened the oral cavity and softened the saturated tongue colour for an enclosed mouth in the real-time viewer.']
