"""Author an editable, rigged Agumon in Blender, without external generation.

Run: blender --background --python Tools/author_agumon.py -- --draft
Coordinates in this source: X right, Y forward, Z up. All geometry is authored
here; no model or animation data is extracted from another game.
"""
import argparse
import json
import math
import struct
import sys
from pathlib import Path

import bpy
import bmesh
from mathutils import Vector, Quaternion, Matrix, Euler
from mathutils.bvhtree import BVHTree

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0,str(ROOT/'Tools'))
from model_surface_quality import bake_contact_ao,engine_mesh
OUT = ROOT / "ArtSource" / "Agumon"
OUT.mkdir(parents=True, exist_ok=True)
parser = argparse.ArgumentParser()
parser.add_argument("--draft", action="store_true")
parser.add_argument("--render-only", action="store_true")
parser.add_argument("--no-render", action="store_true", help="Export and validate assets without studio renders")
ARGS = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.render.engine = "CYCLES" if False else "BLENDER_EEVEE_NEXT"
scene.render.resolution_x = 1000
scene.render.resolution_y = 1000
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
scene.render.fps = 30
scene.world.color = (.22, .22, .22)
scene.view_settings.view_transform = "Standard"
scene.view_settings.look = "Medium High Contrast" if False else "None"
scene.view_settings.exposure = 0
scene.view_settings.gamma = 1


def material(name, rgb, toon=True):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*rgb, 1)
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    if toon:
        geometry = nodes.new("ShaderNodeNewGeometry")
        dot = nodes.new("ShaderNodeVectorMath")
        dot.operation = "DOT_PRODUCT"
        dot.inputs[1].default_value = (-.38,.45,.81)
        ramp = nodes.new("ShaderNodeValToRGB")
        ramp.color_ramp.interpolation = "LINEAR"
        ramp.color_ramp.elements.remove(ramp.color_ramp.elements[1])
        for i, (position, factor) in enumerate(((0, .62), (.42, .89), (.90, 1.0))):
            element = ramp.color_ramp.elements[0] if i == 0 else ramp.color_ramp.elements.new(position)
            element.position = position
            element.color = (*(c * factor for c in rgb), 1)
        emission = nodes.new("ShaderNodeEmission")
        links.new(geometry.outputs["Normal"], dot.inputs[0])
        links.new(dot.outputs["Value"], ramp.inputs[0])
        links.new(ramp.outputs[0], emission.inputs[0])
        links.new(emission.outputs[0], output.inputs[0])
    else:
        emission = nodes.new("ShaderNodeEmission")
        emission.inputs[0].default_value = (*rgb, 1)
        links.new(emission.outputs[0], output.inputs[0])
    return mat


skin = material("Agumon golden yellow | cel", (.90, .47, .025))
ivory = material("Ivory claws and teeth", (.96, .93, .79))
ink = material("Warm ink", (.028, .013, .009), False)
white = material("Eye white", (.97, .97, .89), False)
iris = material("Emerald iris", (.09, .44, .13), False)
iris_light = material("Iris lower green", (.27, .70, .23), False)
mouth_mat = material("Mouth cavity", (.105, .021, .025), False)
tongue_mat = material("Tongue", (.67, .16, .18))
highlight = material("Eye highlight", (1, 1, 1), False)
pieces = []
binding = {}


def activate(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def finish(obj, mat, bone=None):
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    for p in obj.data.polygons:
        p.use_smooth = True
    pieces.append(obj)
    if bone:
        binding[obj.name] = bone
    return obj


def ellipsoid(name, center, scale, mat=skin, bone=None, rotation=None):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=20, location=center)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    if rotation:
        obj.rotation_mode = "QUATERNION"
        obj.rotation_quaternion = rotation
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, mat, bone)


def capsule(name, a, b, width, depth=None):
    a, b = Vector(a), Vector(b)
    return ellipsoid(name, (a+b)/2, (width, depth or width, (b-a).length/2+width*.65),
                     rotation=Vector((0, 0, 1)).rotation_difference(b-a))


def rounded_volume(name,center,scale,exponent=0.76,mat=skin,bone=None):
    obj=ellipsoid(name,center,scale,mat,bone)
    for vertex in obj.data.vertices:
        for axis in range(3):
            n=vertex.co[axis]/scale[axis]
            vertex.co[axis]=scale[axis]*math.copysign(abs(n)**exponent,n)
    obj.data.update()
    return obj


def mesh_obj(name, verts, faces, mat, bone=None, subdiv=0):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    scene.collection.objects.link(obj)
    finish(obj, mat, bone)
    if subdiv:
        activate(obj)
        mod = obj.modifiers.new("Surface refinement", "SUBSURF")
        mod.levels = subdiv
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj


def loft(name, sections, mat, bone, power=2.5):
    """Cross sections: forward Y, width, bottom Z, top Z. Rounded box profile."""
    verts, faces = [], []
    n = 48
    for y, width, bottom, top in sections:
        mid, half = (top+bottom)/2, (top-bottom)/2
        for i in range(n):
            angle = math.tau*i/n
            c, s = math.cos(angle), math.sin(angle)
            profile_power = max(power, 7) if s<0 and name=="Agumon skull and muzzle" else power
            verts.append((width*math.copysign(abs(c)**(2/profile_power), c), y,
                          mid+half*math.copysign(abs(s)**(2/profile_power), s)))
    for j in range(len(sections)-1):
        for i in range(n):
            a, b = j*n+i, j*n+(i+1)%n
            faces.append((a, b, b+n, a+n))
    faces += [tuple(reversed(range(n))), tuple((len(sections)-1)*n+i for i in range(n))]
    # Winding is outward for forward-running cross sections.
    return mesh_obj(name, verts, [tuple(reversed(f)) for f in faces], mat, bone, 1)


def tube(name, points, radii, mat=skin, bone=None, sides=16):
    pts = [Vector(p) for p in points]
    verts, faces = [], [];previous_u=None
    for j, p in enumerate(pts):
        tangent = (pts[min(j+1, len(pts)-1)]-pts[max(0, j-1)]).normalized()
        u=previous_u-tangent*previous_u.dot(tangent) if previous_u is not None else Vector()
        if u.length_squared<1e-10:u=tangent.cross(Vector((0,0,1)) if abs(tangent.z)<.9 else Vector((0,1,0)))
        u.normalize();previous_u=u.copy()
        v = tangent.cross(u).normalized()
        for i in range(sides):
            a = math.tau*i/sides
            verts.append(tuple(p+radii[j]*(u*math.cos(a)+v*math.sin(a))))
    for j in range(len(pts)-1):
        for i in range(sides):
            a, b = j*sides+i, j*sides+(i+1)%sides
            faces.append((a, b, b+sides, a+sides))
    faces += [tuple(reversed(range(sides))), tuple((len(pts)-1)*sides+i for i in range(sides))]
    return mesh_obj(name, verts, faces, mat, bone, 1)


def line(name, points, radius, mat=ink, bone="Head"):
    return tube(name, points, [radius]*len(points), mat, bone, 8)


# A connected skin surface: thick thighs, compact torso, rounded shoulders,
# separated three-finger hands and a short, tapered dinosaur tail.
body_parts = []
body_parts.append(ellipsoid("Pelvis volume", (0, -.055, .72), (.33, .29, .35)))
body_parts.append(ellipsoid("Torso volume", (0, .025, 1.015), (.335, .303, .395)))
body_parts.append(ellipsoid("Neck volume", (0, .055, 1.35), (.23, .235, .24)))
body_parts.append(tube("Tail volume", [(0,-.17,.70),(0,-.35,.58),(0,-.52,.42),(0,-.68,.35),(0,-.80,.40),(0,-.85,.43)], [.22,.185,.13,.073,.032,.004]))
for side, suffix in ((-1,"L"),(1,"R")):
    shoulder = (side*.29, .015, 1.245)
    elbow = (side*.465, .075, 1.015)
    wrist = (side*.55, .29, .92)
    body_parts += [capsule("Upper arm "+suffix, shoulder, elbow, .112),
                   capsule("Forearm "+suffix, elbow, wrist, .123),
                   ellipsoid("Palm "+suffix, (side*.55,.305,.92), (.168,.158,.102)),
                   ellipsoid("Thigh "+suffix, (side*.255,-.005,.565), (.194,.213,.274)),
                   capsule("Ankle "+suffix, (side*.295,.045,.42), (side*.315,.115,.18), .115),
                   ellipsoid("Foot "+suffix, (side*.31,.245,.145), (.211,.295,.13))]
    for finger in range(3):
        x = side*.55+(finger-1)*.106
        extra = .025 if finger == 1 else 0
        body_parts.append(capsule("Finger "+suffix+str(finger), (x,.355,.918), (x,.48+extra,.882), .048))
        tube("Hand claw "+suffix+str(finger), [(x,.457+extra,.89),(x,.535+extra,.883),(x,.59+extra,.85),(x,.635+extra,.815)], [.043,.035,.021,.0015], ivory, "Hand"+suffix)
        tx = side*.31+(finger-1)*.139
        body_parts.append(capsule("Toe "+suffix+str(finger), (tx,.354,.14), (tx,.469+extra,.115), .073))
        tube("Foot claw "+suffix+str(finger), [(tx,.435+extra,.137),(tx,.528+extra,.124),(tx,.615+extra,.073),(tx,.664+extra,.047)], [.065,.048,.027,.002], ivory, "Foot"+suffix)

bpy.ops.object.select_all(action="DESELECT")
for obj in body_parts:
    obj.select_set(True)
    pieces.remove(obj)
bpy.context.view_layer.objects.active = body_parts[0]
bpy.ops.object.join()
body = bpy.context.object
body.name = "Agumon continuous body"
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
remesh = body.modifiers.new("Joined sculpt volume", "REMESH")
remesh.mode = "VOXEL"
remesh.voxel_size = .017
remesh.use_smooth_shade = True
bpy.ops.object.modifier_apply(modifier=remesh.name)
smooth = body.modifiers.new("Smooth anatomical transitions", "SMOOTH")
smooth.factor = .7
smooth.iterations = 10
bpy.ops.object.modifier_apply(modifier=smooth.name)
decimate = body.modifiers.new("Game surface density", "DECIMATE")
decimate.ratio = .28
bpy.ops.object.modifier_apply(modifier=decimate.name)
finish(body, skin)

# Skull, raised orbital ridges, cheek masses and muzzle are sculpted into one
# surface. A mouth plane separates the genuinely hinged jaw from the skull.
skull_parts=[rounded_volume("Cranium",(0,.045,1.841),(.465,.374,.325),.90),
             rounded_volume("Broad muzzle",(0,.489,1.727),(.435,.451,.195),.87)]
for side in (-1,1):
    skull_parts += [ellipsoid("Cheek",(side*.277,.139,1.689),(.213,.279,.194))]
bpy.ops.object.select_all(action="DESELECT")
for obj in skull_parts:
    obj.select_set(True);pieces.remove(obj)
bpy.context.view_layer.objects.active=skull_parts[0]
bpy.ops.object.join()
head=bpy.context.object;head.name="Agumon skull and muzzle"
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
mod=head.modifiers.new("Sculpt unified head","REMESH");mod.mode="VOXEL";mod.voxel_size=.012
bpy.ops.object.modifier_apply(modifier=mod.name)
for vertex in head.data.vertices:
    x,y,z=vertex.co
    influence=max(0,min(1,(z-2.095)/.095))*math.exp(-((y-.12)/.36)**2)
    vertex.co.z+=influence*(.015*math.exp(-((abs(x)-.32)/.12)**2)-.024*math.exp(-(x/.14)**2))
head.data.update()
mod=head.modifiers.new("Sculpt smoothing","SMOOTH");mod.factor=.65;mod.iterations=7
bpy.ops.object.modifier_apply(modifier=mod.name)
mod=head.modifiers.new("Head game density","DECIMATE");mod.ratio=.35
bpy.ops.object.modifier_apply(modifier=mod.name)


def cut_horizontal(obj,height,remove_above=False):
    bm=bmesh.new();bm.from_mesh(obj.data)
    result=bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),
        dist=.00001,plane_co=(0,0,height),plane_no=(0,0,1),
        clear_inner=not remove_above,clear_outer=remove_above)
    boundary=[e for e in bm.edges if e.is_boundary]
    if boundary:bmesh.ops.holes_fill(bm,edges=boundary,sides=0)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    bm.to_mesh(obj.data);bm.free();obj.data.update()


cut_horizontal(head,1.59)
finish(head,skin,"Head")
head_surface=BVHTree.FromPolygons([v.co for v in head.data.vertices],[list(p.vertices) for p in head.data.polygons])
jaw=rounded_volume("Hinged lower jaw",(0,.404,1.526),(.405,.473,.115),.82,skin,"Jaw")
activate(jaw);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
cut_horizontal(jaw,1.568,True)
ellipsoid("Upper mouth cavity",(0,.440,1.589),(.402,.445,.010),mouth_mat,"Head")
ellipsoid("Jaw inner mouth",(0,.451,1.570),(.396,.427,.008),mouth_mat,"Jaw")
ellipsoid("Tongue", (0,.65,1.58), (.19,.205,.008), tongue_mat,"Jaw")


def eye_patch(name, center, normal, width, height, mat, scale=1, offset=0, almond=True):
    n = Vector(normal).normalized()
    u = Vector((n.y,-n.x,0)).normalized()
    v = Vector((0,0,1))
    center = Vector(center)
    def conform(p):
        hit,normal_hit,_,_=head_surface.ray_cast(p+n*1.2,-n,2.5)
        if hit is None:raise RuntimeError("Eye outside skull: "+name)
        return tuple(hit+normal_hit*(.002+offset))
    verts=[conform(center)]
    rings, sides=5,48
    for r in range(1,rings+1):
        k=r/rings
        for i in range(sides):
            a=math.tau*i/sides
            x=math.cos(a)*width*scale
            y=math.sin(a)*height*scale
            if almond:
                y*=.60+.40*abs(math.sin(a))
                y+=x*.28*(1 if center.x>0 else -1)
            verts.append(conform(center+u*x*k+v*y*k))
    faces=[]
    for i in range(sides):
        faces.append((0,1+i,1+(i+1)%sides))
    for r in range(rings-1):
        for i in range(sides):
            a=1+r*sides+i;b=1+r*sides+(i+1)%sides
            # Match the centre fan before the outward reversal below. Walking
            # the inner ring first reverses these quads and hides them with
            # back-face culling (the two-sided portable shader masks it).
            faces.append((a,a+sides,b+sides,b))
    return mesh_obj(name,verts,[tuple(reversed(f)) for f in faces],mat,"Head")


for side,suffix in ((-1,"L"),(1,"R")):
    center=(side*.396,.315,1.927)
    normal=Vector((side*.76,.65,0)).normalized()
    eye_patch("Eye ink "+suffix,center,normal,.133,.084,ink)
    eye_patch("Eye white "+suffix,center,normal,.119,.071,white,offset=.0025)
    pupil_center=Vector(center)+Vector((-side*.012,.02,-.004))
    eye_patch("Iris "+suffix,pupil_center,normal,.067,.062,iris,offset=.005,almond=False)
    eye_patch("Iris glow "+suffix,pupil_center+Vector((0,0,-.026)),normal,.051,.028,iris_light,offset=.0075,almond=False)
    eye_patch("Pupil "+suffix,pupil_center+Vector((0,0,.006)),normal,.033,.043,ink,offset=.010,almond=False)
    eye_patch("Catchlight "+suffix,pupil_center+Vector((-side*.013,.006,.037)),normal,.018,.018,highlight,offset=.013,almond=False)
    # Small, flat nostrils and expressive mouth corners; no separate round nose.
    nostril_start=Vector((side*.192,.845,2.0))
    hit,n,_,_=head_surface.ray_cast(nostril_start,Vector((0,0,-1)),1)
    ellipsoid("Nostril "+suffix,hit+n*.002,(.025,.020,.004),ink,"Head",
              Vector((0,0,1)).rotation_difference(n))
    for i,(y,x) in enumerate(((.365,.374),(.563,.380),(.735,.338))):
        z=1.60
        tube("Upper tooth "+suffix+str(i),[(side*x,y,z),(side*(x-.005),y+.008,z-.032),(side*(x-.013),y+.023,z-.059)], [.032,.021,.001],ivory,"Head")
    tube("Lower tooth "+suffix,[(side*.263,.79,1.560),(side*.259,.789,1.583),(side*.252,.788,1.608)],[.025,.017,.001],ivory,"Jaw")

# Sink the heavy head into the shoulder line instead of leaving a long neck.
for obj in pieces:
    if binding.get(obj.name) in ("Head","Jaw"):
        obj.location.z-=.10

# Skeleton positions and anatomical segment endpoints are independent of mesh.
defs=[("Hips",None,(0,-.035,.73),(0,.015,1.03)),
      ("Spine","Hips",(0,.015,1.03),(0,.045,1.37)),
      ("Head","Spine",(0,.045,1.37),(0,.10,1.81)),
      ("Jaw","Head",(0,.115,1.445),(0,.69,1.445)),
      ("Tail","Hips",(0,-.21,.65),(0,-.44,.47)),
      ("TailMid","Tail",(0,-.44,.47),(0,-.66,.36)),
      ("TailTip","TailMid",(0,-.66,.36),(0,-.835,.425))]
for side,suffix in ((-1,"L"),(1,"R")):
    defs += [("UpperArm"+suffix,"Spine",(side*.29,.015,1.245),(side*.465,.075,1.015)),
             ("Forearm"+suffix,"UpperArm"+suffix,(side*.465,.075,1.015),(side*.55,.29,.92)),
             ("Hand"+suffix,"Forearm"+suffix,(side*.55,.29,.92),(side*.55,.52,.865)),
             ("Thigh"+suffix,"Hips",(side*.24,-.035,.72),(side*.29,.07,.43)),
             ("Shin"+suffix,"Thigh"+suffix,(side*.29,.07,.43),(side*.31,.13,.16)),
             ("Foot"+suffix,"Shin"+suffix,(side*.31,.13,.16),(side*.31,.50,.105))]
arm_data=bpy.data.armatures.new("Agumon skeleton")
rig=bpy.data.objects.new("Agumon_Rig",arm_data)
scene.collection.objects.link(rig)
activate(rig)
bpy.ops.object.mode_set(mode="EDIT")
for name,parent,head_pos,tail_pos in defs:
    bone=arm_data.edit_bones.new(name)
    bone.head=head_pos;bone.tail=tail_pos
    if parent:bone.parent=arm_data.edit_bones[parent]
bpy.ops.object.mode_set(mode="OBJECT")
rig.show_in_front=True

# Automatic heat weights only on the connected body, rigid head/jaw/eye/claw
# assignments everywhere else. Four influences retained for engine export.
activate(body)
rig.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.object.parent_set(type="ARMATURE_AUTO")
for obj in pieces:
    if obj==body:continue
    bone_name=binding.get(obj.name,"Head")
    group=obj.vertex_groups.new(name=bone_name)
    group.add(list(range(len(obj.data.vertices))),1,"REPLACE")
    obj.parent=rig
    mod=obj.modifiers.new("Agumon skeleton", "ARMATURE")
    mod.object=rig
for obj in pieces:
    if not any(m.type=="ARMATURE" for m in obj.modifiers):
        raise RuntimeError("Unbound geometry: "+obj.name)
    if any(not v.groups for v in obj.data.vertices):
        raise RuntimeError("Unweighted vertices in "+obj.name)


def reset_pose():
    for bone in rig.pose.bones:
        bone.rotation_mode="QUATERNION"
        bone.rotation_quaternion=Quaternion()
        bone.location=(0,0,0)
        bone.scale=(1,1,1)


def rotate(name,x=0,y=0,z=0):
    b=rig.pose.bones[name]
    basis=b.bone.matrix_local.to_quaternion()
    delta=Euler(tuple(math.radians(v) for v in (x,y,z)),"XYZ").to_quaternion()
    b.rotation_quaternion=basis.inverted()@delta@basis


def translate(name,value):
    b=rig.pose.bones[name]
    b.location=b.bone.matrix_local.to_quaternion().inverted()@Vector(value)


def ease(t):
    t=max(0,min(1,t));return t*t*(3-2*t)


def envelope(t,keys):
    for i in range(len(keys)-1):
        a,va=keys[i];b,vb=keys[i+1]
        if a<=t<=b:return va+(vb-va)*ease((t-a)/(b-a))
    return keys[0][1] if t<keys[0][0] else keys[-1][1]


def ik_leg(suffix,foot_target,toe_pitch=0):
    """Two-bone sagittal IK, preserving a planted foot orientation."""
    bpy.context.view_layer.update()
    thigh=rig.pose.bones["Thigh"+suffix];shin=rig.pose.bones["Shin"+suffix]
    foot=rig.pose.bones["Foot"+suffix]
    hip=thigh.head.copy();target=Vector(foot_target)
    d=target-hip;l1=thigh.bone.length;l2=shin.bone.length
    dist=max(.02,min(d.length,l1+l2-.001));direction=d.normalized()
    along=(l1*l1-l2*l2+dist*dist)/(2*dist)
    bend=Vector((0,1,0));bend=(bend-direction*bend.dot(direction)).normalized()
    knee=hip+direction*along+bend*math.sqrt(max(0,l1*l1-along*along))
    for bone,end in ((thigh,knee),(shin,target)):
        bpy.context.view_layer.update()
        m=bone.matrix.copy();rot=m.to_quaternion()
        desired=(end-bone.head).normalized()
        q=(bone.tail-bone.head).normalized().rotation_difference(desired)@rot
        bone.matrix=Matrix.Translation(bone.head)@q.to_matrix().to_4x4()
    bpy.context.view_layer.update()
    toe_rotation=Euler((math.radians(toe_pitch),0,0)).to_quaternion()
    foot.matrix=Matrix.Translation(foot.head)@(toe_rotation@foot.bone.matrix_local.to_quaternion()).to_matrix().to_4x4()


def gait(u,run=False):
    """Linear ground contact, velocity-matched swing, soft vertical toe clearance."""
    stance=.62 if not run else .54
    stride=.15 if not run else .20
    if u<stance:return stride*(1-2*u/stance),0,0
    s=(u-stance)/(1-stance)
    # Hermite endpoints match the backward velocity of a planted foot.
    tangent=-2*stride*(1-stance)/stance
    forward=(-stride)*(2*s**3-3*s*s+1)+stride*(-2*s**3+3*s*s)+tangent*(2*s**3-3*s*s+s)
    lift=math.sin(math.pi*s)**2*(.105 if not run else .17)
    return forward,lift,math.sin(math.pi*s)**2*(10 if not run else 15)


def pose(mode,t,duration):
    reset_pose()
    phase=math.tau*t/duration
    if mode=="Idle":
        translate("Hips",(.004*math.sin(phase),0,-.008+.005*math.sin(phase)))
        rotate("Spine",x=math.sin(phase)*1.1,z=.6*math.sin(phase))
        rotate("Head",x=-math.sin(phase-.25)*.7,z=math.sin(phase-.3)*.8)
        for suffix,side in (("L",-1),("R",1)):
            rotate("UpperArm"+suffix,x=6+1.4*math.sin(phase-.25),y=side*2)
            rotate("Forearm"+suffix,x=-4+.7*math.sin(phase-.45))
        rotate("Tail",z=math.sin(phase)*3)
        rotate("TailMid",z=math.sin(phase-.6)*4)
        rotate("TailTip",z=math.sin(phase-1.1)*5)
    elif mode in ("Walk","Run"):
        run=mode=="Run"
        # Keep the ankle targets inside the two-bone reach at heel contact.
        # An overextended leg silently clamps IK and makes the foot slide.
        translate("Hips",(-(.038 if run else .025)*math.sin(phase),0,(-.12 if run else -.085)+.008*math.cos(phase*2)))
        rotate("Spine",x=7 if run else 3,y=-math.sin(phase)*1.5,z=math.sin(phase)*3)
        rotate("Head",x=(-5 if run else -2)-.8*math.cos(phase*2-.3),z=-math.sin(phase-.15)*2.4)
        for suffix,offset in (("L",0),("R",.5)):
            u=(t/duration+offset)%1
            forward,lift,toe=gait(u,run)
            ik_leg(suffix,((-.31 if suffix=="L" else .31),.13+forward,.16+lift),toe)
            arm_phase=phase+(0 if suffix=="L" else math.pi)
            swing=math.sin(arm_phase-.18)
            rotate("UpperArm"+suffix,x=6+swing*(17 if not run else 29))
            rotate("Forearm"+suffix,x=-9-4*math.sin(arm_phase-.5))
            rotate("Hand"+suffix,x=3*math.sin(arm_phase-.75))
        rotate("Tail",z=-math.sin(phase)*7,x=6 if run else 0)
        rotate("TailMid",z=-math.sin(phase-.6)*10)
        rotate("TailTip",z=-math.sin(phase-1)*13)
    elif mode=="Attack":
        wind=envelope(t,[(0,0),(.19,1),(.32,0),(.8,0)])
        strike=envelope(t,[(0,0),(.19,0),(.32,1),(.40,.86),(.58,.22),(.8,0)])
        follow=envelope(t,[(0,0),(.26,0),(.40,1),(.58,.32),(.8,0)])
        rotate("Spine",x=-wind*6+strike*10,z=-wind*17+strike*22)
        rotate("Head",x=wind*3-strike*4,z=wind*9-strike*15)
        # Raise the claw during anticipation, then rake forward/down across
        # the target; the contact pose should not read as an upright wave.
        rotate("UpperArmR",x=6+wind*42+strike*28,y=-strike*22,z=wind*20-strike*26)
        rotate("ForearmR",x=-4+wind*12+strike*4)
        rotate("HandR",x=-wind*10-follow*30)
        rotate("UpperArmL",x=6+wind*10-strike*11)
        rotate("ForearmL",x=-4-strike*9)
        rotate("Tail",z=wind*13-strike*17)
        rotate("TailMid",z=wind*7-follow*12)
        rotate("TailTip",z=-follow*9)
        translate("Hips",(.018*wind-.025*strike,-wind*.025+strike*.045,-.008-wind*.018-strike*.020))
    elif mode=="PepperBreath":
        charge=envelope(t,[(0,0),(.32,1),(.51,0),(.95,0)])
        fire=envelope(t,[(0,0),(.30,0),(.39,1),(.57,.70),(.91,0)])
        translate("Hips",(0,-charge*.045,-charge*.040))
        rotate("Spine",x=-charge*10+fire*12)
        rotate("Head",x=-charge*10-fire*4)
        rotate("Jaw",x=-charge*10-fire*29)
        for suffix,side in (("L",-1),("R",1)):
            rotate("UpperArm"+suffix,x=-charge*15+fire*12,y=side*charge*11)
            rotate("Forearm"+suffix,x=-charge*13)
        recoil=envelope(t,[(0,0),(.38,0),(.46,1),(.65,.25),(.95,0)])
        rotate("Tail",x=charge*9-recoil*4)
        rotate("TailMid",x=charge*5-recoil*6)
        rotate("TailTip",x=-recoil*5)
    elif mode=="Hit":
        hit=envelope(t,[(0,0),(.10,1),(.22,.4),(.55,0)])
        rotate("Spine",x=-hit*17,y=hit*8)
        rotate("Head",x=-hit*13)
        rotate("Jaw",x=-hit*10)
        rotate("UpperArmL",x=-hit*20)
        rotate("UpperArmR",x=-hit*20)
    elif mode=="Defeat":
        collapse=ease(t/.9)
        rotate("Hips",y=collapse*77)
        translate("Hips",(-.13*collapse,0,-.10*collapse+.16*math.sin(math.pi*collapse)))
        rotate("Spine",x=collapse*13)
        rotate("Head",x=collapse*8)
        rotate("Jaw",x=-collapse*14)
        rotate("UpperArmL",x=-collapse*22)
        rotate("UpperArmR",x=collapse*33)
        rotate("ThighL",x=-collapse*23)
        rotate("ThighR",x=collapse*17)
        rotate("Tail",z=-collapse*24)
    elif mode=="Turn":
        translate("Hips",(0,0,-.008))
        rotate("Spine",z=8*math.sin(phase))
        rotate("Head",z=17*math.sin(phase+.35))
        rotate("Tail",z=-7*math.sin(phase-.2))
        rotate("TailMid",z=-5*math.sin(phase-.6))
    if mode in ("Idle","Attack","PepperBreath","Hit","Turn"):
        ik_leg("L",(-.31,.13,.16));ik_leg("R",(.31,.13,.16))
    bpy.context.view_layer.update()


CLIPS=[("Idle",2.4,True),("Walk",.9,True),("Run",.6,True),("Attack",.8,False),
       ("PepperBreath",.95,False),("Hit",.55,False),("Defeat",1.2,False),("Turn",1.8,True)]
clips=[]
for name,duration,loop in CLIPS:
    rig.animation_data_create()
    action=bpy.data.actions.new(name)
    action.use_fake_user=True
    rig.animation_data.action=action
    count=round(duration*30)
    for f in range(count+1):
        scene.frame_set(f+1)
        pose(name,f/30,duration)
        for bone in rig.pose.bones:
            bone.keyframe_insert("rotation_quaternion",frame=f+1,group=bone.name)
            bone.keyframe_insert("location",frame=f+1,group=bone.name)
    clips.append((name,duration,loop,action,count))
rig.animation_data.action=None
reset_pose()
bpy.context.view_layer.update()

# A small engine interchange format: unshaded vertex colours, four weights,
# bind positions and baked local skeletal transforms. Blender remains source.
C=Matrix(((1,0,0,0),(0,0,1,0),(0,1,0,0),(0,0,0,1)))
names=[d[0] for d in defs]
indices={name:i for i,name in enumerate(names)}
surface=bake_contact_ao(pieces)
vertex_records,triangles=engine_mesh(pieces,indices,C)
verts=[tuple(v[0]) for v in vertex_records];normals=[tuple(v[1]) for v in vertex_records]
colors=[v[2] for v in vertex_records];weights=[v[3] for v in vertex_records]

asset=ROOT/"Assets/Resources/Models/Agumon/Agumon.bytes"
asset.parent.mkdir(parents=True,exist_ok=True)
with asset.open("wb") as f:
    def ints(*values):f.write(struct.pack("<"+"i"*len(values),*values))
    def floats(*values):f.write(struct.pack("<"+"f"*len(values),*values))
    def string(value):
        b=value.encode("utf-8");ints(len(b));f.write(b)
    f.write(b"DTM3");ints(1,len(names),len(verts),len(triangles),len(clips))
    for name,parent,head_pos,tail_pos in defs:
        string(name);ints(indices[parent] if parent else -1);floats(*(C@Vector(head_pos)))
    for p,n,col,w in zip(verts,normals,colors,weights):
        floats(*p,*n,*col)
        padded=w+[(0,0)]*(4-len(w))
        ints(*(i for i,_ in padded));floats(*(v for _,v in padded))
    for tri in triangles:ints(*tri)
    for name,duration,loop,action,count in clips:
        string(name);floats(duration);ints(1 if loop else 0,count+1)
        rig.animation_data.action=action
        for frame in range(count+1):
            scene.frame_set(frame+1)
            bpy.context.view_layer.update()
            globals={}
            for bone_name,parent,head_pos,_ in defs:
                pb=rig.pose.bones[bone_name]
                deform=pb.matrix@pb.bone.matrix_local.inverted()
                world=C@deform@Matrix.Translation(head_pos)@C
                globals[bone_name]=world
                local=globals[parent].inverted()@world if parent else world
                q=local.to_quaternion();floats(*local.to_translation(),q.x,q.y,q.z,q.w)
rig.animation_data.action=None
reset_pose()
bpy.context.view_layer.update()

# Render-only outlines: keep the interchange and FBX geometry clean.
for obj in pieces:
    obj.data.materials.append(ink)
    mod=obj.modifiers.new("Ink silhouette (render only)","SOLIDIFY")
    mod.thickness=.005
    mod.offset=1
    mod.use_flip_normals=True
    mod.material_offset=1
    mod.use_rim=False
    mod.show_viewport=False
    mod.show_render=False  # Freestyle draws silhouettes without enclosing the face.
scene.render.use_freestyle=True
scene.render.line_thickness=1
line_style=scene.view_layers[0].freestyle_settings.linesets[0].linestyle
line_set=scene.view_layers[0].freestyle_settings.linesets[0]
line_set.select_border=False
line_set.select_crease=False
line_set.select_material_boundary=False
line_style.color=(.035,.02,.009)
line_style.thickness=1.35

# Studio ground and a restrained, shadow-free background make the actual
# silhouette readable. No composited model image is used in these renders.
floor_mat=material("Studio floor",(.77,.80,.75))
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.006))
floor=bpy.context.object;floor.name="Studio ground";floor.data.materials.append(floor_mat)
for name,position,power,size in (("Key",(-3,4,7),650,5),("Fill",(4,1,4),220,5)):
    data=bpy.data.lights.new(name,"AREA");data.energy=power;data.shape="DISK";data.size=size
    obj=bpy.data.objects.new(name,data);scene.collection.objects.link(obj);obj.location=position
    obj.rotation_euler=(Vector((0,0,1))-obj.location).to_track_quat("-Z","Y").to_euler()
camera_data=bpy.data.cameras.new("Inspection camera")
camera=bpy.data.objects.new("Inspection camera",camera_data)
scene.collection.objects.link(camera);scene.camera=camera
camera_data.type="ORTHO";camera_data.ortho_scale=3.08


def render(name,location,focus=(0,.025,1.13),clip=None,time=0):
    rig.animation_data.action=None
    reset_pose()
    if clip:pose(clip,time,dict((c[0],c[1]) for c in CLIPS)[clip])
    camera.location=location
    camera.rotation_euler=(Vector(focus)-camera.location).to_track_quat("-Z","Y").to_euler()
    scene.render.filepath=str(OUT/(name+".png"))
    bpy.ops.render.render(write_still=True)


rig.animation_data.action=clips[0][3]
scene.frame_start=1;scene.frame_end=clips[0][4]+1;scene.frame_set(1)
camera.location=(3.7,6.2,3)
camera.rotation_euler=(Vector((0,.05,1.14))-camera.location).to_track_quat("-Z","Y").to_euler()
activate(rig)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/"Agumon.blend"))
report={"vertices":len(verts),"triangles":len(triangles),"bones":len(names),
        "clips":[{"name":n,"seconds":d,"loop":l} for n,d,l in CLIPS],
        "asset":str(asset),"source":"Locally authored Blender geometry; no paid generation"}
if not ARGS.draft:
    # Export conventional, editable interchange files with portable matte
    # materials. Toon nodes remain intact in the Blender source file.
    rig.animation_data.action=None;reset_pose();bpy.context.view_layer.update()
    swaps=[];export_materials={}
    for obj in pieces:
        for slot in obj.material_slots:
            original=slot.material
            if original.name not in export_materials:
                mat=bpy.data.materials.new(original.name+" / portable")
                mat.use_nodes=True;mat.diffuse_color=original.diffuse_color
                bsdf=mat.node_tree.nodes.get("Principled BSDF")
                bsdf.inputs["Base Color"].default_value=original.diffuse_color
                bsdf.inputs["Roughness"].default_value=1
                bsdf.inputs["Specular IOR Level"].default_value=.12
                export_materials[original.name]=mat
            swaps.append((slot,original));slot.material=export_materials[original.name]
    bpy.ops.object.select_all(action="DESELECT")
    rig.select_set(True)
    for obj in pieces:obj.select_set(True)
    bpy.context.view_layer.objects.active=rig
    bpy.ops.export_scene.gltf(filepath=str(OUT/"Agumon.glb"),export_format="GLB",use_selection=True,
                              export_animations=True,export_animation_mode="ACTIONS",export_yup=True,
                              export_vertex_color='NAME',export_vertex_color_name='ContactAO',export_all_vertex_colors=False)
    bpy.ops.export_scene.fbx(filepath=str(OUT/"Agumon.fbx"),use_selection=True,add_leaf_bones=False,
                             bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,
                             axis_forward="-Z",axis_up="Y",object_types={"MESH","ARMATURE"})
    for slot,original in swaps:slot.material=original

    # Inspect actual evaluated skinned vertices, including fingers, claws and
    # the mouth, rather than checking only bone transforms.
    validation=[]
    for name,duration,loop in CLIPS:
        sample_times=[duration*i/24 for i in range(25)] if name=="Defeat" else [duration*i/4 for i in range(5)]
        for time in sample_times:
            pose(name,time,duration)
            deps=bpy.context.evaluated_depsgraph_get()
            low=Vector((100,100,100));high=Vector((-100,-100,-100))
            for obj in pieces:
                evaluated=obj.evaluated_get(deps);mesh=evaluated.to_mesh()
                for v in mesh.vertices:
                    p=evaluated.matrix_world@v.co
                    if not all(math.isfinite(x) for x in p):raise RuntimeError("Non-finite skinned vertex")
                    for k in range(3):low[k]=min(low[k],p[k]);high[k]=max(high[k],p[k])
                evaluated.to_mesh_clear()
            validation.append({"clip":name,"time":time,"minimum_z":low.z,"size":list(high-low)})
            if low.z < -.015:raise RuntimeError("Ground penetration: "+name+" at "+str(time))
    report["pose_bounds"]=validation
(OUT/"model-report.json").write_text(json.dumps(report,indent=2),encoding="utf-8")
if not ARGS.no_render:
    render("01-three-quarter",(3.7,6.2,3.0))
    render("02-front",(0,6,2.0))
    render("03-side",(6,.0,2.0))
if not ARGS.draft and not ARGS.no_render:
    render("04-back",(-3,-6,2.8))
    render("05-walk",(3.7,6.2,3.0),clip="Walk",time=.20)
    render("06-attack",(3.7,6.2,3.0),clip="Attack",time=.31)
    render("07-breath",(3.7,6.2,3.0),clip="PepperBreath",time=.41)
    render("08-defeat",(3.7,6.2,3.0),clip="Defeat",time=1.1)
print("AGUMON_AUTHORING_COMPLETE",json.dumps(report))
