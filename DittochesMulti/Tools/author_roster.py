"""Author the complete playable/creep roster as editable Blender rigs and DTM3.

Run Blender --background --python-exit-code 1 --python Tools/author_roster.py
-- --ids koromon,gabumon (omit --ids for all non-Agumon characters).
All geometry is local, informed by the linked official visual references.
"""
import argparse
import json
import math
import struct
import sys
from types import SimpleNamespace
from pathlib import Path
import bpy
from mathutils import Vector,Quaternion,Euler,Matrix

ROOT=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(ROOT/'Tools'))
from roster_designs import DESIGNS,CLIPS,OFFICIAL
from model_surface_quality import bake_contact_ao,engine_mesh
parser=argparse.ArgumentParser()
parser.add_argument('--ids',default='')
parser.add_argument('--render',action='store_true')
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
INK='#242333';IVORY='#f5eddb';GOLD='#daba58';GREEN='#539b68'
parts=[];definitions=[];materials={};rig=None


def color(value):
    rgb=[int(value[i:i+2],16)/255 for i in (1,3,5)]
    return tuple(v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4 for v in rgb)+(1,)


def material(value):
    if value not in materials:
        m=bpy.data.materials.new(value);m.diffuse_color=color(value);m.use_nodes=True
        bsdf=m.node_tree.nodes.get('Principled BSDF');bsdf.inputs['Base Color'].default_value=color(value)
        bsdf.inputs['Roughness'].default_value=.62
        materials[value]=m
    return materials[value]


def finish(obj,name,tint,bone):
    obj.name=name;obj.data.materials.append(material(tint))
    bpy.context.view_layer.objects.active=obj
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    for p in obj.data.polygons:p.use_smooth=True
    parts.append((obj,bone))
    return obj


def ball(name,p,r,tint,bone='Spine',rotation=None):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=12,location=p)
    obj=bpy.context.object;obj.scale=r
    if rotation:obj.rotation_mode='QUATERNION';obj.rotation_quaternion=rotation
    return finish(obj,name,tint,bone)


def capsule(name,a,b,width,tint,bone='Spine',depth=None):
    a,b=Vector(a),Vector(b)
    return ball(name,(a+b)/2,(width,depth or width,(b-a).length/2+width*.35),tint,bone,
                Vector((0,0,1)).rotation_difference((b-a).normalized()))


def tube(name,points,radii,tint,bone='Head',sides=12):
    pts=[Vector(p) for p in points];verts=[];faces=[];previous_u=None
    for j,p in enumerate(pts):
        tangent=(pts[min(j+1,len(pts)-1)]-pts[max(j-1,0)]).normalized()
        # Transport the cross-section continuously around curved horns, veins
        # and tendrils; reselecting a world axis at each ring causes a twist.
        u=previous_u-tangent*previous_u.dot(tangent) if previous_u is not None else Vector()
        if u.length_squared<1e-10:u=tangent.cross(Vector((0,0,1)) if abs(tangent.z)<.9 else Vector((0,1,0)))
        u.normalize();previous_u=u.copy()
        v=tangent.cross(u).normalized()
        for k in range(sides):
            angle=math.tau*k/sides
            verts.append(p+radii[j]*(u*math.cos(angle)+v*math.sin(angle)))
        if j:
            for k in range(sides):
                a=(j-1)*sides+k;b=(j-1)*sides+(k+1)%sides
                faces.append((a,b,b+sides,a+sides))
    faces.extend([tuple(reversed(range(sides))),tuple((len(pts)-1)*sides+k for k in range(sides))])
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
    # Recalculate consistently, including the two closed caps.
    import bmesh
    bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free()
    return finish(obj,name,tint,bone)


def horn(name,a,b,tip,r,tint=IVORY,bone='Head'):
    return tube(name,[a,b,tip],[r,r*.6,.002],tint,bone)


def leaf(name,a,b,width,tint,bone='Head'):
    # Thick, tapered leaf/feather, with a raised central ridge.
    a,b=Vector(a),Vector(b);axis=b-a
    lateral=axis.cross(Vector((0,1,0))).normalized()*width
    mid=a+axis*.45;ridge=mid+Vector((0,.035,0))
    verts=[a,mid+lateral,b,mid-lateral,ridge,mid-Vector((0,.022,0))]
    faces=[(0,1,4),(1,2,4),(2,3,4),(3,0,4),(1,0,5),(2,1,5),(3,2,5),(0,3,5)]
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
    return finish(obj,name,tint,bone)


def bone(name,parent,p,end):
    definitions.append((name,parent,Vector(p),Vector(end)))


def plate(name,outline,thickness,tint,binding='Head'):
    """Bevelled solid armour with a deliberate angular silhouette."""
    verts=[tuple(p) for p in outline]+[(x,y-thickness,z) for x,y,z in outline]
    n=len(outline)
    faces=[tuple(range(n)),tuple(reversed(range(n,n*2)))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
    import bmesh
    bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free()
    finish(obj,name,tint,binding)
    bevel=obj.modifiers.new('Forged edge','BEVEL');bevel.width=.012;bevel.segments=2
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    for p in obj.data.polygons:p.use_smooth=False
    return obj


def eyes(center,width,height,forward,iris=GREEN,scale=1):
    x,y,z=center
    for side in (-1,1):
        c=(x+side*width,y+forward,z)
        ball('Eye border',c,(.092*scale,.015*scale,.113*scale),INK,'Head')
        ball('Eye white',(c[0],c[1]+.009*scale,c[2]),(.078*scale,.012*scale,.097*scale),IVORY,'Head')
        ball('Iris',(c[0]-side*.009*scale,c[1]+.018*scale,c[2]),(.054*scale,.008*scale,.076*scale),iris,'Head')
        ball('Pupil',(c[0]-side*.010*scale,c[1]+.024*scale,c[2]),(.029*scale,.005*scale,.058*scale),INK,'Head')
        ball('Eye shine',(c[0]-side*.023*scale,c[1]+.029*scale,c[2]+.037*scale),(.017*scale,.004*scale,.020*scale),IVORY,'Head')


def mouth(center,width=.18):
    x,y,z=center
    ball('Mouth line',(x,y,z),(width,.022,.039),INK,'Jaw')


def claws(center,spread,bone_name,count=3,length=.13,tint=IVORY):
    x,y,z=center
    for i in range(count):
        a=(x+(i-(count-1)/2)*spread,y,z)
        horn('Claw',a,(a[0],y+length*.6,z-.01),(a[0],y+length,z-.05),spread*.36,tint,bone_name)


def setup_skeleton(kind):
    if kind=='baby':hip=.36;chest=.48;head=.58;spread=.25;ankle=.11
    elif kind=='winged_blob':hip=.46;chest=.60;head=.62;spread=.27;ankle=.14
    elif kind=='quadruped':hip=.82;chest=.92;head=1.08;spread=.36;ankle=.16
    elif kind=='insect':hip=.70;chest=1.02;head=1.30;spread=.34;ankle=.16
    elif kind=='bird':hip=.88;chest=1.18;head=1.60;spread=.25;ankle=.18
    elif kind=='humanoid':hip=.94;chest=1.39;head=1.91;spread=.21;ankle=.13
    elif kind=='shell':hip=.50;chest=.70;head=1.05;spread=.40;ankle=.14
    else:hip=.70;chest=1.07;head=1.49;spread=.30;ankle=.15
    bone('Hips',None,(0,-.08,hip),(0,0,chest))
    bone('Spine','Hips',(0,0,chest),(0,.03,head-.10))
    bone('Head','Spine',(0,.04,head-.16),(0,.06,head+.10))
    bone('Jaw','Head',(0,.15,head-.14),(0,.40,head-.14))
    bone('Tail','Hips',(0,-.25,hip),(0,-.52,hip-.10))
    bone('TailMid','Tail',(0,-.52,hip-.10),(0,-.75,hip-.02))
    bone('TailTip','TailMid',(0,-.75,hip-.02),(0,-.94,hip+.08))
    for side,suffix in [(-1,'L'),(1,'R')]:
        shoulder=(side*.29,.01,chest+.11);elbow=(side*.43,.06,chest-.16);hand=(side*.49,.22,chest-.29)
        thigh=(side*spread,-.08,hip);knee=(side*spread,.045,(hip+ankle)/2);foot=(side*spread,.10,ankle)
        if kind=='quadruped':
            shoulder=(side*spread,.48,chest);elbow=(side*spread,.54,.49);hand=(side*spread,.60,.16)
            thigh=(side*spread,-.49,hip);knee=(side*spread,-.45,.43);foot=(side*spread,-.42,.16)
        bone('UpperArm'+suffix,'Spine',shoulder,elbow);bone('Forearm'+suffix,'UpperArm'+suffix,elbow,hand)
        bone('Hand'+suffix,'Forearm'+suffix,hand,Vector(hand)+Vector((0,.17,-.03)))
        bone('Thigh'+suffix,'Hips',thigh,knee);bone('Shin'+suffix,'Thigh'+suffix,knee,foot)
        bone('Foot'+suffix,'Shin'+suffix,foot,Vector(foot)+Vector((0,.21,-.03)))
        bone('Wing'+suffix,'Spine',(side*.19,-.08,chest+.15),(side*.68,-.14,chest+.38))
        bone('WingTip'+suffix,'Wing'+suffix,(side*.68,-.14,chest+.38),(side*1.15,-.18,chest+.20))
        bone('Ear'+suffix,'Head',(side*.22,.02,head+.16),(side*.28,-.04,head+.53))
        if kind=='insect':
            bone('MidLeg'+suffix,'Spine',(side*.28,-.10,.92),(side*.66,-.02,.52))
    return hip,chest,head,spread


def point(name):return next(p for n,_,p,_ in definitions if n==name)


def limbs(tint,kind,secondary,feature=''):
    for side,suffix in [(-1,'L'),(1,'R')]:
        if kind not in ('baby','winged_blob','bird','insect','shell'):
            for a,b,r in [('UpperArm','Forearm',.095),('Forearm','Hand',.10),('Thigh','Shin',.16),('Shin','Foot',.10)]:
                capsule(a+suffix,point(a+suffix),point(b+suffix),r,tint,a+suffix)
            ball('Hand',point('Hand'+suffix),(.125,.14,.075),tint,'Hand'+suffix)
            if kind=='humanoid' and feature not in ('dragon_armor','wolf_fighter','devil_wings','eagle_warrior'):
                for finger in range(4):
                    a=point('Hand'+suffix)+Vector(((finger-1.5)*.044,.07,0))
                    capsule('Finger',a,a+Vector((0,.115,-.045)),.024,tint,'Hand'+suffix)
            else:claws(point('Hand'+suffix)+Vector((0,.10,0)),.075,'Hand'+suffix)
        if kind not in ('baby','winged_blob','insect','shell'):
            if kind=='bird':
                capsule('Bird shin',point('Thigh'+suffix),point('Foot'+suffix),.085,secondary,'Shin'+suffix)
            foot=point('Foot'+suffix)
            ball('Foot',foot+Vector((0,.10,-.04)),(.16,.25,.105),secondary if kind=='humanoid' else tint,'Foot'+suffix)
            if kind!='humanoid' or feature in ('dragon_armor','wolf_fighter','devil_wings','eagle_warrior'):
                claws(foot+Vector((0,.28,-.03)),.095,'Foot'+suffix,length=.16)


def feather_wings(tint,accent,pairs=1,membrane=False):
    chest=point('Spine').z
    for side,suffix in [(-1,'L'),(1,'R')]:
        for pair in range(pairs):
            z=chest+.32-pair*(.18 if pairs>2 else .24)
            a=(side*.20,-.16-pair*.03,z);b=(side*(.77-pair*.055),-.16,z+.44-pair*.17)
            capsule('Wing leading edge',a,b,.065,tint,'Wing'+suffix)
            for feather in range(7):
                u=feather/6
                start=Vector(a).lerp(Vector(b),.20+.75*u)
                tip=(side*(.62+.77*u-pair*.06),-.23-.08*u,z-(.33 if pairs>2 else .43)+.72*u-pair*(.03 if pairs>2 else .10))
                if membrane:
                    leaf('Wing membrane',start,tip,.19,accent,'Wing'+suffix)
                    horn('Wing finger',start,Vector(start).lerp(Vector(tip),.6),tip,.021,tint,'Wing'+suffix)
                else:
                    leaf('Primary feather',start,tip,.085,accent if feather%3==0 else tint,'Wing'+suffix if feather<3 else 'WingTip'+suffix)


def flower(center,size,tint,bone_name='Head',count=7):
    x,y,z=center
    for i in range(count):
        a=math.tau*i/count
        tip=(x+math.cos(a)*size,y+math.sin(a)*size,z-.04)
        leaf('Flower petal',(x,y,z+.10),tip,size*.37,tint,bone_name)
    ball('Flower centre',(x,y,z+.13),(size*.18,size*.18,.07),GOLD,bone_name)


def baby(ident,tint,accent,feature):
    ball('Soft body',(0,0,.42),(.48,.36,.37),tint,'Hips')
    eyes((0,.03,.50),.20,0,.30,INK if ident!='pyocomon' else GREEN,1.1)
    mouth((0,.355,.30),.13)
    for side,suffix in [(-1,'L'),(1,'R')]:
        if feature in ('long_ears','tiny_legs'):
            tube('Long soft ear',[(side*.23,-.02,.65),(side*.27,-.04,.86),(side*.33,-.10,1.08),(side*.42,-.18,1.15),(side*.50,-.22,1.10)],[.08,.065,.049,.033,.016],tint,'Ear'+suffix)
        if feature in ('mitten_arms','tiny_legs','sprout'):
            ball('Soft foot',(side*.27,.12,.10),(.13,.16,.085),tint,'Foot'+suffix)
        if feature=='mitten_arms':
            capsule('Mitten arm',(side*.36,0,.45),(side*.60,.05,.40),.10,tint,'UpperArm'+suffix)
        if feature=='sprout':leaf('Sprout leaf',(0,0,.73),(side*.48,0,1.19),.16,accent,'Ear'+suffix)
    if feature=='single_horn':
        ball('White face',(0,.23,.42),(.38,.16,.24),IVORY,'Head')
        eyes((0,.08,.50),.20,0,.31,'#c94444',1.0)
        horn('Single horn',(0,-.02,.69),(0,.03,1.04),(0,.06,1.33),.11,'#c7ccd3')
    if feature=='flower':
        tube('Flower stalk',[(0,0,.65),(0,0,.89),(0,0,1.10)],[.055,.04,.025],GREEN)
        flower((0,0,1.02),.46,accent)


def quadruped(ident,tint,accent):
    metal=ident=='metalgarurumon'
    ball('Long torso',(0,-.01,.86),(.36,.73,.36),tint)
    ball('Wolf chest',(0,.43,.96),(.38,.31,.41),tint)
    ball('Wolf skull',(0,.62,1.21),(.28,.32,.27),tint,'Head')
    ball('Long muzzle',(0,.90,1.10),(.20,.30,.14),tint,'Head')
    ball('Nose',(0,1.16,1.14),(.13,.07,.08),INK,'Head')
    mouth((0,1.12,1.03),.16);eyes((0,.55,1.28),.19,0,.30,'#d6b947',.8)
    limbs(tint,'quadruped',accent)
    for side,suffix in [(-1,'L'),(1,'R')]:
        horn('Wolf ear',(side*.17,.53,1.38),(side*.24,.40,1.61),(side*.32,.32,1.75),.11,tint,'Ear'+suffix)
        for i in range(5):
            leaf('Flank stripe',(side*.30,.43-i*.23,1.08),(side*.355,.34-i*.23,.65),.07,accent)
        for i in range(3):
            horn('Shoulder mane',(side*.25,.32-i*.08,1.07),(side*.42,.19-i*.07,1.15),(side*.62,.02-i*.08,1.22),.085,accent)
        if metal:
            ball('Shoulder armour',(side*.37,.42,.95),(.16,.25,.25),accent,'UpperArm'+suffix)
            capsule('Missile pod',(side*.42,.01,.95),(side*.42,-.33,.95),.12,accent)
            leaf('Metal wing',(side*.16,-.15,1.09),(side*.95,-.54,1.53),.22,GOLD,'Wing'+suffix)
    tube('Wolf tail',[(0,-.58,.92),(0,-.93,1.04),(0,-1.15,1.31),(0,-1.18,1.46)],[.16,.13,.09,.006],accent,'Tail')


def insect(ident,tint,accent,feature):
    ball('Abdomen',(0,-.16,.92),(.40,.43,.50),accent,'Hips')
    ball('Shell',(0,-.25,1.20),(.48,.33,.43),tint)
    ball('Thorax',(0,.06,1.03),(.29,.24,.36),accent)
    ball('Head',(0,.19,1.42),(.29,.25,.29),tint,'Head')
    for side,suffix in [(-1,'L'),(1,'R')]:
        ball('Compound eye',(side*.21,.39,1.43),(.13,.075,.18),GREEN if ident=='tentomon' else '#83bfad','Head')
        for label,y,z in [('UpperArm',.24,1.13),('MidLeg',-.05,.91),('Thigh',-.27,.71)]:
            end=(side*.64,y+.23,z-.35)
            tube('Segmented insect limb',[(side*.27,y,z),(side*.52,y-.08,z-.04),end],[.09,.065,.037],tint,label+suffix)
            claws(Vector(end),.09,label+suffix,count=2,length=.17,tint=accent)
        if feature=='ladybird':
            for i in range(3):ball('Shell spot',(side*(.30 if i!=1 else .40),-.47,1.07+i*.14),(.09,.035,.10),INK)
            tube('Antenna',[(side*.15,.15,1.64),(side*.22,.17,1.94),(side*.34,.20,2.04)],[.03,.02,.014],GOLD,'Ear'+suffix)
        else:
            leaf('Insect wing',(side*.20,-.33,1.22),(side*.98,-.49,1.91),.21,'#bbd5d6','Wing'+suffix)
            if feature in ('stag_beetle','gold_beetle'):
                horn('Great mandible',(side*.20,.29,1.56),(side*.46,.39,1.96),(side*.24,.45,2.30),.115,tint)
                horn('Mandible tooth',(side*.36,.39,1.92),(side*.27,.42,1.91),(side*.18,.44,1.85),.055,accent)
    if feature in ('rhino_beetle','red_beetle','gold_beetle'):
        horn('Rhino horn',(0,.15,1.60),(0,.26,2.02),(0,.46,2.20),.13,tint)
        if feature=='red_beetle':horn('Fork horn',(0,.26,1.91),(-.22,.30,2.12),(-.25,.34,2.26),.075,tint)
    mouth((0,.421,1.25),.17)
    for i in range(4):capsule('Abdominal rib',(-.19,.26,.83+i*.075),(.19,.26,.83+i*.075),.024,accent)


def bird(ident,tint,accent,feature):
    ball('Bird body',(0,-.04,1.05),(.33,.31,.46),tint)
    ball('Bird head',(0,.05,1.60),(.28,.26,.30),tint,'Head')
    horn('Beak',(0,.24,1.58),(0,.43,1.56),(0,.53,1.48),.115,GOLD)
    eyes((0,.07,1.68),.15,0,.21,'#527eaa',.78)
    limbs(tint,'bird',GOLD)
    feather_wings(tint,accent,2 if feature=='four_wing_phoenix' else 1)
    for i in range(5):
        x=(i-2)*.09
        leaf('Tail feather',(x,-.22,1.03),(x*2,-.85-.12*(2-abs(i-2)),.45),.11,accent,'Tail')
    if feature=='pink_bird':
        tube('Curled head feather',[(0,0,1.84),(0,-.02,2.12),(0,.12,2.25),(0,.23,2.15),(0,.15,2.07)],[.08,.065,.05,.035,.01],tint)
    else:
        for side in (-1,0,1):horn('Flame crown',(side*.10,0,1.80),(side*.13,-.08,2.06),(side*.17,-.14,2.23),.075,accent)
        for side,suffix in [(-1,'L'),(1,'R')]:
            for i in range(3):leaf('Long flame plume',(side*.58,-.18,1.48),(side*(.95+i*.16),-.26,1.95+i*.14),.105,accent,'WingTip'+suffix)


def biped(ident,tint,accent,feature,kind):
    hip=point('Hips').z;chest=point('Spine').z;head=point('Head').z+.16
    slender=kind=='humanoid'
    ball('Torso',(0,0,chest-.07),(.25 if slender else .33,.22 if slender else .30,.34),tint)
    ball('Pelvis',(0,-.04,hip),(.25 if slender else .33,.23,.24),tint,'Hips')
    capsule('Neck',(0,.02,chest+.15),(0,.04,head-.12),.13,tint,'Head')
    face_tint='#f0c6aa' if feature in ('flower_fairy','rose_whip') else tint
    ball('Skull',(0,.05,head),(.22 if slender else .35,.20 if slender else .30,.27),face_tint,'Head')
    limbs(tint,kind,accent,feature)
    if feature not in ('flower_fairy','rose_whip','dragon_armor'):
        eyes((0,.045,head+.04),.115 if slender else .21,0,.18 if slender else .25,GREEN,.70 if slender else .95)
        mouth((0,.25 if slender else .36,head-.13),.10 if slender else .20)
    if kind=='dinosaur' or feature in ('fur_horn','wolf_fighter','monkey_sunglasses'):
        ball('Muzzle',(0,.27,head-.08),(.27 if not slender else .16,.27 if not slender else .17,.14),tint,'Head')
        ball('Jaw',(0,.29,head-.22),(.25 if not slender else .14,.25 if not slender else .15,.075),tint,'Jaw')
        tube('Tail',[(0,-.18,hip),(0,-.49,hip-.08),(0,-.80,hip-.15),(0,-1.02,hip-.04)],[.20,.14,.08,.004],tint,'Tail')
    if feature in ('horned_helmet','cyborg_dinosaur'):
        ball('Bone helmet',(0,.01,head+.08),(.365,.31,.28),accent,'Head')
        for side in (-1,1):
            plate('Helmet brow',[(side*.02,.365,head+.20),(side*.28,.30,head+.25),(side*.36,.29,head+.04),(side*.27,.355,head-.005),(side*.13,.377,head+.07)],.06,accent)
            plate('Helmet cheek',[(side*.28,.35,head+.01),(side*.36,.29,head+.10),(side*.35,.30,head-.19),(side*.26,.39,head-.23)],.05,accent)
        for side,suffix in [(-1,'L'),(1,'R')]:
            horn('Helmet horn',(side*.24,-.01,head+.23),(side*.42,-.13,head+.47),(side*.55,-.20,head+.64),.11,IVORY)
            for i in range(3):leaf('Tiger stripe',(side*.26,.04,chest+.10-i*.16),(side*.31,.16,chest-i*.16),.07,'#46658b')
            ball('Helmet eye',(side*.20,.28,head+.03),(.075,.04,.055),'#d66532','Head')
        horn('Nose horn',(0,.29,head+.09),(0,.39,head+.34),(0,.44,head+.48),.115,IVORY)
        if feature=='cyborg_dinosaur':
            capsule('Metal claw arm',point('UpperArmL'),point('HandL'),.16,'#aebbc4','ForearmL')
            claws(point('HandL')+Vector((0,.12,0)),.10,'HandL',length=.30,tint='#c7d4db')
            for side in (-1,1):ball('Chest missile',(side*.12,.285,chest),(.10,.10,.14),'#a9b5b9')
            feather_wings('#b3a6c0','#cabbc7',1,True)
    if feature=='fur_horn':
        ball('Blue fur hood',(0,-.035,head+.09),(.39,.28,.34),accent,'Head')
        eyes((0,.04,head+.04),.22,0,.265,'#c95257',1.0)
        horn('Forehead horn',(0,.02,head+.30),(0,.15,head+.60),(0,.21,head+.81),.11,IVORY)
        ball('Belly',(0,.27,chest-.03),(.22,.055,.26),'#82b8b2')
        for side in (-1,1):
            for i in range(4):leaf('Fur stripe',(side*.18,-.03,head+.30-i*.12),(side*.38,.03,head+.21-i*.12),.055,IVORY,'Head')
            horn('Fur ear',(side*.29,-.01,head+.21),(side*.48,-.10,head+.35),(side*.67,-.13,head+.41),.12,IVORY)
            for i in range(3):leaf('Fur fringe',(side*.27,-.08,chest+.08-i*.15),(side*.43,-.05,chest-.06-i*.15),.10,accent)
    if feature in ('six_wing_staff','eight_wing_sword','ten_wing_armor'):
        pairs={'six_wing_staff':3,'eight_wing_sword':4,'ten_wing_armor':5}[feature]
        feather_wings(IVORY,GOLD if pairs==5 else '#d3d0d8',pairs)
        ball('Helmet',(0,0,head+.03),(.245,.21,.245),accent,'Head')
        capsule('Visor',(-.19,.204,head+.01),(.19,.204,head+.01),.044,'#aebbc4','Head')
        plate('Opaque helmet visor',[(-.235,.268,head+.15),(-.19,.287,head+.24),(.19,.287,head+.24),(.235,.268,head+.15),(.20,.275,head-.065),(0,.30,head-.11),(-.20,.275,head-.065)],.04,'#9dabb8')
        for side,suffix in [(-1,'L'),(1,'R')]:
            capsule('Holy boot',point('Shin'+suffix),point('Foot'+suffix),.122,IVORY,'Shin'+suffix)
            tube('Helmet engraved line',[(side*.02,.304,head+.18),(side*.075,.31,head+.025),(side*.16,.295,head-.035)],[.009]*3,GOLD,'Head')
        for side in (-1,1):horn('Helmet crest',(side*.11,.01,head+.18),(side*.18,-.01,head+.40),(side*.23,-.03,head+.57),.052,GOLD)
        capsule('Chest harness',(-.20,.21,chest+.14),(.16,.24,chest-.20),.04,GOLD)
        if pairs==3:
            tube('Holy staff',[(.59,.17,.16),(.59,.17,1.40),(.59,.17,2.26)],[.032,.032,.032],GOLD,'HandR')
            ball('Staff finial',(.59,.17,2.27),(.075,.065,.095),GOLD,'HandR')
        elif pairs==4:leaf('Holy sword',point('HandR'),point('HandR')+Vector((.18,.14,.95)),.12,'#bb91e9','HandR')
        else:
            for side,suffix in [(-1,'L'),(1,'R')]:
                ball('Shoulder plate',point('UpperArm'+suffix),(.23,.24,.16),GOLD,'UpperArm'+suffix)
                ball('Knee armour',point('Shin'+suffix),(.14,.13,.16),'#9bacce','Shin'+suffix)
    if feature in ('flower_fairy','rose_whip'):
        ball('Human face',(0,.088,head-.015),(.175,.182,.215),'#f0c6aa','Head')
        eyes((0,.078,head+.04),.095,0,.19,GREEN,.55)
        mouth((0,.277,head-.09),.055)
        flower((0,0,head+.21),.36,tint)
        for side in (-1,1):leaf('Leaf collar',(0,-.04,head-.21),(side*.47,-.07,head+.15),.14,accent)
        for i in range(7):
            a=math.tau*i/7
            leaf('Petal skirt',(math.cos(a)*.18,math.sin(a)*.18,hip+.10),(math.cos(a)*.36,math.sin(a)*.30,hip-.26),.14,tint,'Hips')
        if feature=='flower_fairy':feather_wings(accent,'#82b55b',2)
        else:
            tube('Rose whip',[tuple(point('HandR')), (.69,.24,.89),(.87,.30,.43),(.98,.44,.22),(.82,.58,.19)],[.026,.022,.018,.014,.005],accent,'HandR')
            for side,suffix in [(-1,'L'),(1,'R')]:capsule('Dark boot',point('Shin'+suffix),point('Foot'+suffix),.105,INK,'Shin'+suffix)
    if feature=='wolf_fighter':
        for side,suffix in [(-1,'L'),(1,'R')]:
            horn('Wolf ear',(side*.14,.01,head+.16),(side*.20,-.04,head+.38),(side*.25,-.08,head+.46),.085,tint)
            capsule('Trousers',point('Thigh'+suffix),point('Shin'+suffix),.17,accent,'Thigh'+suffix)
            for i in range(3):leaf('Fur marking',(side*.19,.07,chest+.12-i*.12),(side*.25,.15,chest+.03-i*.12),.045,accent)
    if feature=='dragon_armor':
        # The silver dragon mask, red mane and gold gauntlets define this form.
        ball('Dark undersuit',(0,.035,chest),(.265,.235,.29),'#333b43')
        plate('Dragon face mask',[(-.23,.265,head+.17),(-.13,.31,head+.31),(0,.34,head+.21),(.13,.31,head+.31),(.23,.265,head+.17),(.22,.32,head-.10),(0,.465,head-.24),(-.22,.32,head-.10)],.07,accent)
        for side in (-1,1):
            plate('Dark eye slit',[(side*.045,.355,head+.085),(side*.19,.325,head+.12),(side*.15,.365,head+.018)],.009,INK)
            plate('Green eye slit',[(side*.065,.365,head+.078),(side*.171,.342,head+.10),(side*.14,.372,head+.041)],.005,'#58b78a')
            for i in range(4):
                leaf('Red mane',(side*.14,-.08,head+.13-i*.06),(side*(.38+i*.015),-.19,head+.15-i*.13),.065,'#b3442f')
        horn('Nose blade',(0,.34,head+.18),(0,.41,head+.34),(0,.44,head+.49),.062,accent)
        for i in range(3):
            for side in (-1,1):
                capsule('Chest rib',(side*.04,.278,chest+.15-i*.12),(side*.205,.215,chest+.105-i*.12),.038,accent)
        for side,suffix in [(-1,'L'),(1,'R')]:
            ball('Shoulder armour',point('UpperArm'+suffix),(.27,.25,.23),GOLD,'UpperArm'+suffix)
            horn('Shoulder spike',point('UpperArm'+suffix)+Vector((side*.09,0,.16)),point('UpperArm'+suffix)+Vector((side*.13,-.01,.30)),point('UpperArm'+suffix)+Vector((side*.16,-.02,.41)),.065,accent,'UpperArm'+suffix)
            capsule('Gold gauntlet',point('Forearm'+suffix),point('Hand'+suffix)+Vector((0,.06,0)),.18,GOLD,'Forearm'+suffix)
            ball('Gauntlet',point('Hand'+suffix),(.20,.23,.12),GOLD,'Hand'+suffix)
            claws(point('Hand'+suffix)+Vector((0,.16,0)),.105,'Hand'+suffix,length=.37,tint=accent)
            leaf('Brave shield',(side*.04,-.24,1.55),(side*.56,-.28,.92),.30,GOLD)
            horn('Dragon horn',(side*.14,.02,head+.19),(side*.24,-.04,head+.40),(side*.31,-.12,head+.48),.07,accent)
            ball('Knee guard',point('Shin'+suffix)+Vector((0,.065,.035)),(.16,.14,.15),accent,'Shin'+suffix)
            capsule('Shin guard',point('Shin'+suffix)+Vector((0,.075,-.09)),point('Foot'+suffix)+Vector((0,.05,.08)),.11,'#56616b','Shin'+suffix)
        ball('Chest emblem',(0,.29,chest+.065),(.085,.035,.085),'#bf5538')
    if feature=='eagle_warrior':
        feather_wings('#ac4c30','#dfb16a',1)
        horn('Eagle beak',(0,.20,head),(0,.38,head-.02),(0,.41,head-.13),.09,GOLD)
        for i in range(5):leaf('Headdress',(0,-.04,head+.18),((i-2)*.13,-.11,head+.49-abs(i-2)*.05),.065,'#cb4235')
        for side,suffix in [(-1,'L'),(1,'R')]:
            capsule('Feather trousers',point('Thigh'+suffix),point('Shin'+suffix),.21,'#a33d31','Thigh'+suffix)
            ball('Taloned foot',point('Foot'+suffix)+Vector((0,.08,.01)),(.19,.28,.13),GOLD,'Foot'+suffix)
    if feature=='devil_wings':
        feather_wings(INK,'#544655',1,True)
        for side in (-1,1):horn('Devil horn',(side*.14,-.01,head+.19),(side*.22,-.09,head+.40),(side*.29,-.18,head+.48),.066,INK)
        leaf('Chest red emblem',(-.15,.225,chest+.12),(.17,.235,chest-.18),.085,'#b54449')
    if feature=='monkey_sunglasses':
        for side in (-1,1):
            ball('Round ear',(side*.24,.015,head),(.115,.065,.14),tint,'Head')
            ball('Sunglass lens',(side*.11,.257,head+.04),(.105,.035,.065),INK,'Head')
        capsule('Glasses bridge',(-.07,.28,head+.04),(.07,.28,head+.04),.018,INK,'Head')
        ball('Belly patch',(0,.215,chest-.06),(.19,.045,.24),IVORY)
        ball('Belt buckle',(0,.246,hip+.03),(.075,.025,.066),GOLD,'Hips')


def plant(ident,tint,accent,feature):
    if feature=='cactus_boxer':
        ball('Cactus trunk',(0,0,1.07),(.43,.31,.76),tint)
        for side,suffix in [(-1,'L'),(1,'R')]:
            tube('Cactus arm',[(side*.32,0,1.12),(side*.61,0,1.02),(side*.67,.04,1.47)],[.15,.14,.13],tint,'UpperArm'+suffix)
            ball('Boxing glove',(side*.67,.065,1.54),(.22,.20,.22),accent,'Hand'+suffix)
            capsule('Cactus foot',(side*.23,0,.47),(side*.28,.14,.17),.18,tint,'Foot'+suffix)
        for x,z in [(-.15,1.39),(.15,1.39),(0,1.16)]:ball('Cactus face hole',(x,.31,z),(.055,.02,.075),INK,'Head')
        for i in range(7):
            x=-.34+i*.113
            tube('Cactus rib',[(x,.22,.55),(x,.30,1.08),(x,.23,1.65)],[.01,.01,.01],'#376831')
            for j in range(4):horn('Spine',(x,.285,.68+j*.23),(x+.025,.34,.71+j*.23),(x+.04,.38,.73+j*.23),.012,IVORY)
        flower((0,0,1.79),.23,'#d79b44')
    else:
        biped(ident,tint,accent,feature,'biped')
        flower((0,.01,1.75),.55,accent)
        for side,suffix in [(-1,'L'),(1,'R')]:
            for i in range(3):
                p=point('Hand'+suffix)+Vector(((i-1)*.07,.04,0))
                leaf('Vine finger',p,p+Vector((side*.10,.26,-.22)),.055,'#75934c','Hand'+suffix)
        for side in (-1,1):leaf('Plant cheek',(side*.18,.16,1.52),(side*.41,.14,1.34),.095,tint,'Head')


def other(ident,tint,accent,feature,kind):
    if kind=='winged_blob':
        ball('Patamon body',(0,-.02,.52),(.43,.48,.33),tint)
        ball('White belly',(0,.06,.39),(.405,.43,.18),accent)
        eyes((0,.04,.65),.22,0,.365,'#57afb3',.9)
        mouth((0,.45,.44),.11)
        for side,suffix in [(-1,'L'),(1,'R')]:
            for i in range(3):leaf('Ear wing',(side*.27,-.02,.71),(side*(.66+.17*i),-.08,.98+.26*i),.16,tint,'Wing'+suffix)
            ball('Little foot',(side*.30,.23,.19),(.11,.14,.07),INK,'Foot'+suffix)
    elif kind=='shell':
        ball('Shellmon body',(0,.02,.53),(.49,.53,.34),tint)
        ball('Long neck',(0,.27,.88),(.23,.25,.40),tint,'Head')
        ball('Face',(0,.35,1.17),(.34,.28,.29),tint,'Head')
        eyes((0,.34,1.28),.20,0,.23,'#568ac4',1.15)
        mouth((0,.635,1.03),.25)
        ball('Great shell',(0,-.41,.90),(.63,.60,.66),accent,'Hips')
        for i in range(5):
            z=.60+i*.17
            tube('Spiral shell ridge',[(-.52,-.36,z),(-.40,-.81,z+.03),(0,-1.0,z+.07),(.40,-.81,z+.10),(.52,-.36,z+.12)],[.045]*5,'#777b8f','Hips')
        for side,suffix in [(-1,'L'),(1,'R')]:
            capsule('Foreleg',(side*.32,.30,.54),(side*.54,.56,.19),.15,tint,'UpperArm'+suffix)
            ball('Webbed foot',(side*.54,.62,.15),(.23,.25,.11),tint,'Hand'+suffix)
            horn('Shell spike',(side*.40,-.62,1.14),(side*.62,-.81,1.35),(side*.70,-.91,1.45),.11,accent,'Hips')
            for i in range(3):tube('Head feeler',[(side*.09*i,.26,1.39),(side*(.20+.09*i),.30,1.63),(side*(.32+.10*i),.39,1.57)],[.04,.028,.012],GREEN,'Head')


def smooth_organic():
    """Unify connected torso/limbs, with weights interpolated from source segments."""
    global parts
    prefixes=('Torso','Pelvis','UpperArm','Forearm','Thigh','Shin','Hand','Foot','Long torso','Wolf chest','Cactus trunk','Cactus arm','Cactus foot','Patamon body','Soft body','Shellmon body','Foreleg','Webbed foot')
    selected=[(obj,binding) for obj,binding in parts if obj.name.startswith(prefixes)]
    if len(selected)<2:return
    # Original segment ownership provides smooth weights without nearest-bone
    # leakage into unrelated wings, ears or shell ornaments.
    segments=[]
    for obj,binding in selected:
        bounds=[obj.matrix_world@Vector(c) for c in obj.bound_box]
        center=sum(bounds,Vector())/8
        radius=max((v-center).length for v in bounds)
        segments.append((binding,center,max(.08,radius*.65)))
    bpy.ops.object.select_all(action='DESELECT')
    for obj,_ in selected:obj.select_set(True)
    active=selected[0][0];bpy.context.view_layer.objects.active=active
    bpy.ops.object.join();active.name='Continuous organic skin'
    mod=active.modifiers.new('Unified volume','REMESH');mod.mode='VOXEL';mod.voxel_size=.025;mod.use_smooth_shade=True
    bpy.ops.object.modifier_apply(modifier=mod.name)
    mod=active.modifiers.new('Soft anatomical transitions','SMOOTH');mod.factor=.62;mod.iterations=4
    bpy.ops.object.modifier_apply(modifier=mod.name)
    mod=active.modifiers.new('Game density','DECIMATE');mod.ratio=.42;bpy.ops.object.modifier_apply(modifier=mod.name)
    for p in active.data.polygons:p.use_smooth=True
    # Store per-vertex ownership now; make_rig attaches the modifier later.
    group_map={name:active.vertex_groups.new(name=name) for name in set(s[0] for s in segments)}
    for vertex in active.data.vertices:
        scores={}
        for name,center,radius in segments:
            distance=(vertex.co-center).length
            score=math.exp(-3*(distance/radius)**2)
            scores[name]=max(scores.get(name,0),score)
        top=sorted(scores.items(),key=lambda v:-v[1])[:4];total=sum(score for _,score in top)
        if total<1e-15:
            name=min(segments,key=lambda s:(vertex.co-s[1]).length)[0];group_map[name].add([vertex.index],1,'REPLACE')
        else:
            for name,score in top:
                if score/total>.00001:group_map[name].add([vertex.index],score/total,'REPLACE')
    chosen_ids={id(obj) for obj,_ in selected}
    parts=[(obj,b) for obj,b in parts if id(obj) not in chosen_ids]+[(active,None)]


def make_rig():
    global rig
    bpy.ops.object.select_all(action='DESELECT')
    data=bpy.data.armatures.new('Anatomical skeleton');rig=bpy.data.objects.new('Rig',data);bpy.context.collection.objects.link(rig)
    rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
    for name,parent,p,end in definitions:
        b=data.edit_bones.new(name);b.head=p;b.tail=end
        if parent:b.parent=data.edit_bones[parent]
    bpy.ops.object.mode_set(mode='OBJECT')
    for obj,name in parts:
        if name:
            group=obj.vertex_groups.new(name=name);group.add(list(range(len(obj.data.vertices))),1,'REPLACE')
        obj.parent=rig;modifier=obj.modifiers.new('Skeleton','ARMATURE');modifier.object=rig


def reset():
    for b in rig.pose.bones:b.rotation_mode='QUATERNION';b.rotation_quaternion=Quaternion();b.location=(0,0,0);b.scale=(1,1,1)


def rotate(name,x=0,y=0,z=0):
    b=rig.pose.bones.get(name)
    if not b:return
    basis=b.bone.matrix_local.to_quaternion();q=Euler(tuple(math.radians(v) for v in (x,y,z))).to_quaternion()
    b.rotation_quaternion=basis.inverted()@q@basis


def translate(name,v):
    b=rig.pose.bones[name];b.location=b.bone.matrix_local.to_quaternion().inverted()@Vector(v)


def smooth(t):t=max(0,min(1,t));return t*t*(3-2*t)


def pulse(t,start,peak,end):
    if t<start or t>end:return 0
    return smooth((t-start)/(peak-start)) if t<peak else 1-smooth((t-peak)/(end-peak))


def ik(upper_name,lower_name,foot_name,target):
    bpy.context.view_layer.update()
    upper=rig.pose.bones[upper_name];lower=rig.pose.bones[lower_name];foot=rig.pose.bones[foot_name]
    origin=upper.head.copy();delta=Vector(target)-origin
    l1=upper.bone.length;l2=lower.bone.length;distance=max(.02,min(delta.length,l1+l2-.001));direction=delta.normalized()
    along=(l1*l1-l2*l2+distance*distance)/(2*distance)
    bend=Vector((0,1,0));bend=(bend-direction*bend.dot(direction)).normalized()
    knee=origin+direction*along+bend*math.sqrt(max(0,l1*l1-along*along))
    for b,end in [(upper,knee),(lower,Vector(target))]:
        bpy.context.view_layer.update();rotation=b.matrix.to_quaternion()
        q=(b.tail-b.head).normalized().rotation_difference((end-b.head).normalized())@rotation
        b.matrix=Matrix.Translation(b.head)@q.to_matrix().to_4x4()
    bpy.context.view_layer.update()
    foot.matrix=Matrix.Translation(foot.head)@foot.bone.matrix_local.to_quaternion().to_matrix().to_4x4()


def gait(u,stride):
    stance=.62
    if u<stance:return stride*(1-2*u/stance),0
    s=(u-stance)/(1-stance);tangent=-2*stride*(1-stance)/stance
    forward=-stride*(2*s**3-3*s*s+1)+stride*(-2*s**3+3*s*s)+tangent*(2*s**3-3*s*s+s)
    return forward,.095*math.sin(math.pi*s)**2


def pose(kind,feature,mode,t,duration):
    reset();p=math.tau*t/duration;flying=kind in ('bird','winged_blob','insect') or feature in ('flower_fairy','six_wing_staff','eight_wing_sword','ten_wing_armor','devil_wings','eagle_warrior')
    breath=math.sin(p)
    if mode in ('Walk','Run'):
        run=mode=='Run';amplitude=27 if run else 17
        if kind=='baby':
            translate('Hips',(0,0,.085*(1-math.cos(p*2))));rotate('Hips',y=4*math.sin(p))
            for side,suffix in [(-1,'L'),(1,'R')]:rotate('Ear'+suffix,x=9*math.sin(p-.4),y=side*5*math.sin(p))
        else:
            translate('Hips',(.015*math.sin(p),0,.014*(1-math.cos(p*2))))
            rotate('Spine',x=5 if run else 2,z=2*math.sin(p))
            rotate('Head',x=-3 if run else -1,z=-1.5*math.sin(p-.2))
            for side,suffix in [(-1,'L'),(1,'R')]:
                q=p+(0 if side<0 else math.pi)
                rotate('Thigh'+suffix,x=math.sin(q)*amplitude)
                rotate('Shin'+suffix,x=max(0,-math.sin(q))*amplitude*.8)
                rotate('Foot'+suffix,x=-math.sin(q)*amplitude*.4)
                rotate('UpperArm'+suffix,x=-math.sin(q)*(amplitude if kind=='quadruped' else amplitude*.6))
                rotate('Forearm'+suffix,x=max(0,math.sin(q))*amplitude*.6)
                rotate('MidLeg'+suffix,x=math.sin(q+math.pi)*18)
        rotate('Tail',z=-math.sin(p)*7);rotate('TailMid',z=-math.sin(p-.5)*9);rotate('TailTip',z=-math.sin(p-.9)*11)
    elif mode=='Idle':
        translate('Hips',(0,0,.004*breath));rotate('Spine',x=breath*.8);rotate('Head',z=.8*math.sin(p-.3))
        rotate('Tail',z=3*breath);rotate('TailMid',z=4*math.sin(p-.5));rotate('TailTip',z=5*math.sin(p-1))
    elif mode=='Attack':
        wind=pulse(t,0,.18,.34);hit=pulse(t,.18,.34,.8)
        rotate('Spine',x=-wind*6+hit*12,z=-wind*9+hit*14)
        rotate('Head',x=wind*3-hit*5)
        rotate('UpperArmR',x=wind*33+hit*24,z=-hit*20);rotate('ForearmR',x=-hit*10)
        rotate('Tail',z=wind*6-hit*9)
        if kind in ('baby','quadruped','insect','shell','winged_blob'):
            translate('Hips',(0,hit*.10,.015*hit));rotate('Head',x=-hit*17)
    elif mode=='PepperBreath':
        wind=pulse(t,0,.30,.48);fire=pulse(t,.26,.40,.94)
        rotate('Spine',x=-wind*8+fire*9);rotate('Head',x=-wind*6-fire*4);rotate('Jaw',x=-fire*24)
        if feature in ('six_wing_staff','eight_wing_sword','ten_wing_armor','dragon_armor'):
            rotate('UpperArmL',x=fire*98);rotate('UpperArmR',x=fire*98)
        elif feature in ('rose_whip','palm_flower','cactus_boxer','flower_fairy'):
            rotate('UpperArmR',x=fire*65,z=-fire*20);rotate('ForearmR',x=-fire*8)
        elif kind=='insect':rotate('Spine',x=fire*18)
        rotate('Tail',x=wind*4-fire*3)
    elif mode=='Hit':
        hit=pulse(t,0,.09,.55);rotate('Spine',x=-hit*13,z=hit*6);rotate('Head',x=-hit*8)
    elif mode=='Defeat':
        fall=smooth(t/.9);rotate('Hips',y=fall*74)
        # Keep the low side above the floor during collapse.
        translate('Hips',(-.12*fall,0,.22*fall));rotate('Head',x=fall*8)
    elif mode=='Turn':
        rotate('Spine',z=7*math.sin(p));rotate('Head',z=15*math.sin(p+.3));rotate('Tail',z=-5*math.sin(p-.3))
    if flying and mode not in ('Defeat','Hit'):
        flutter=math.sin(t*math.tau*(3 if kind=='insect' else 1.6))
        # Use integral cycles for clips declared looping.
        if mode in ('Idle','Walk','Run','Turn'):flutter=math.sin(p*(4 if kind=='insect' else 2))
        for side,suffix in [(-1,'L'),(1,'R')]:
            rotate('Wing'+suffix,y=side*(8+flutter*(18 if kind!='insect' else 10)))
            rotate('WingTip'+suffix,y=side*flutter*12)
    grounded=kind in ('biped','humanoid','dinosaur','plant','quadruped') and feature!='cactus_boxer'
    if grounded and mode!='Defeat':
        moving=mode in ('Walk','Run')
        translate('Hips',(.018*math.sin(p) if moving else 0,0,-.07+(.008*math.cos(p*2) if moving else 0)))
        for side,suffix in [(-1,'L'),(1,'R')]:
            u=(t/duration+(0 if side<0 else .5))%1
            stride=.15 if kind=='quadruped' else .12
            forward,lift=gait(u,stride) if moving else (0,0)
            ik('Thigh'+suffix,'Shin'+suffix,'Foot'+suffix,point('Foot'+suffix)+Vector((0,forward,lift)))
            if kind=='quadruped':
                forward,lift=gait((u+.5)%1,stride) if moving else (0,0)
                ik('UpperArm'+suffix,'Forearm'+suffix,'Hand'+suffix,point('Hand'+suffix)+Vector((0,forward,lift)))
    bpy.context.view_layer.update()
    if mode in ('Defeat','Hit'):
        # Ground the deformed geometry, including long claws, whips and shells.
        deps=bpy.context.evaluated_depsgraph_get()
        lowest=min((obj.evaluated_get(deps).matrix_world@Vector(c)).z
                   for obj,_ in parts for c in obj.evaluated_get(deps).bound_box)
        correction=.012-lowest
        if mode=='Defeat':
            if correction<0:correction*=smooth(smooth(t/.9)/.65)
            translate('Hips',(-.12*smooth(t/.9),0,.22*smooth(t/.9)+correction))
        elif correction>0:
            base=rig.pose.bones['Hips'].bone.matrix_local.to_quaternion()@rig.pose.bones['Hips'].location
            translate('Hips',base+Vector((0,0,correction)))
        bpy.context.view_layer.update()


def export(ident,kind,feature,out):
    clips=[]
    for name,duration,loop in CLIPS:
        rig.animation_data_create();action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
        count=round(duration*30)
        for frame in range(count+1):
            bpy.context.scene.frame_set(frame+1);pose(kind,feature,name,frame/30,duration)
            for b in rig.pose.bones:
                b.keyframe_insert('rotation_quaternion',frame=frame+1,group=b.name)
                b.keyframe_insert('location',frame=frame+1,group=b.name)
        clips.append((name,duration,loop,action,count))
    # A constant per-clip correction preserves stance trajectories and removes
    # small residual penetrations from skinned toes and dense ornaments.
    for name,duration,loop,action,count in clips:
        rig.animation_data.action=action;minimum=100
        for step in range(9):
            frame=1+count*step/8;bpy.context.scene.frame_set(math.floor(frame),subframe=frame%1)
            deps=bpy.context.evaluated_depsgraph_get()
            minimum=min(minimum,min((obj.evaluated_get(deps).matrix_world@Vector(c)).z
                        for obj,_ in parts for c in obj.evaluated_get(deps).bound_box))
        if minimum<.004:
            hips=rig.pose.bones['Hips'];shift=hips.bone.matrix_local.to_quaternion().inverted()@Vector((0,0,.008-minimum))
            for frame in range(count+1):
                bpy.context.scene.frame_set(frame+1);hips.location+=shift;hips.keyframe_insert('location',frame=frame+1,group='Hips')
    rig.animation_data.action=None;reset();bpy.context.view_layer.update()
    C=Matrix(((1,0,0,0),(0,0,1,0),(0,1,0,0),(0,0,0,1)))
    names=[d[0] for d in definitions];indices={n:i for i,n in enumerate(names)}
    surface=bake_contact_ao([obj for obj,_ in parts])
    vertices,triangles=engine_mesh([obj for obj,_ in parts],indices,C)
    destination=ROOT/'Assets/Resources/Models/Roster'/(ident+'.bytes');destination.parent.mkdir(parents=True,exist_ok=True)
    with destination.open('wb') as f:
        def ints(*v):f.write(struct.pack('<'+'i'*len(v),*v))
        def floats(*v):f.write(struct.pack('<'+'f'*len(v),*v))
        def string(v):b=v.encode();ints(len(b));f.write(b)
        f.write(b'DTM3');ints(1,len(names),len(vertices),len(triangles),len(clips))
        for n,parent,p,end in definitions:string(n);ints(indices[parent] if parent else -1);floats(*(C@p))
        for p,n,c,weights in vertices:
            weights=weights+[(0,0)]*(4-len(weights))
            floats(*p,*n,*c);ints(*(i for i,_ in weights));floats(*(w for _,w in weights))
        for tri in triangles:ints(*tri)
        for name,duration,loop,action,count in clips:
            string(name);floats(duration);ints(int(loop),count+1);rig.animation_data.action=action
            for frame in range(count+1):
                bpy.context.scene.frame_set(frame+1);bpy.context.view_layer.update();worlds={}
                for n,parent,p,end in definitions:
                    pb=rig.pose.bones[n];world=C@(pb.matrix@pb.bone.matrix_local.inverted())@Matrix.Translation(p)@C
                    worlds[n]=world;local=worlds[parent].inverted()@world if parent else world;q=local.to_quaternion()
                    floats(*local.to_translation(),q.x,q.y,q.z,q.w)
    rig.animation_data.action=None;reset();bpy.context.view_layer.update()
    # Scene contains only this character at export, keeping GLB animation ownership clear.
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
    for obj,_ in parts:obj.select_set(True)
    bpy.context.view_layer.objects.active=rig
    bpy.ops.export_scene.gltf(filepath=str(out/(ident+'.glb')),export_format='GLB',use_selection=True,
        export_animations=True,export_animation_mode='ACTIONS',export_yup=True,
        export_vertex_color='NAME',export_vertex_color_name='ContactAO',export_all_vertex_colors=False)
    rig.animation_data.action=clips[0][3];bpy.context.scene.frame_set(1)
    # Build a neutral studio for the editable source and review still.
    scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.render.resolution_x=512;scene.render.resolution_y=512
    scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG'
    scene.display.shading.light='STUDIO';scene.display.shading.color_type='MATERIAL'
    scene.display.shading.show_cavity=True;scene.display.shading.show_shadows=True
    scene.display.shading.background_type='WORLD';scene.world.color=(.16,.18,.22)
    scene.view_settings.view_transform='Standard'
    positions=[p for p,_,_,_ in vertices]
    maxheight=max(p.y for p in positions);extent=max(max(p.x for p in positions)-min(p.x for p in positions),maxheight)
    data=bpy.data.cameras.new('Review camera');camera=bpy.data.objects.new('Review camera',data);scene.collection.objects.link(camera)
    camera.location=(3.5,6,2.8);focus=Vector((0,0,maxheight*.48));camera.rotation_euler=(focus-camera.location).to_track_quat('-Z','Y').to_euler()
    data.type='ORTHO';data.ortho_scale=max(2.1,extent*1.35);scene.camera=camera
    scene.render.filepath=str(out/'preview.png')
    bpy.ops.wm.save_as_mainfile(filepath=str(out/(ident+'.blend')))
    if args.render:bpy.ops.render.render(write_still=True)
    return {'id':ident,'anatomy':kind,'feature':feature,'vertices':len(vertices),'triangles':len(triangles),'bones':len(names),
            'clips':8,'height':maxheight,'stage':'reference-based silhouette revision; manual review required','surface':surface,
            'reference':'https://digimon.net/reference/detail.php?directory_name='+OFFICIAL.get(ident,ident)}


results=[]
for ident,label,kind,tint,accent,feature in DESIGNS:
    if ident=='agumon' or (args.ids and ident not in args.ids.split(',')):continue
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    for action in list(bpy.data.actions):bpy.data.actions.remove(action)
    parts=[];definitions=[];materials={};setup_skeleton(kind)
    from importlib import import_module
    module_name=('roster_humanoid_quality' if kind=='humanoid' else
                 'roster_creature_quality' if ident in {'gabumon','greymon','metalgreymon','garurumon','metalgarurumon','tentomon','kabuterimon','atlur','herakle','kuwagamon','shellmon'} else
                 'roster_small_quality')
    module=import_module(module_name)
    module.build(SimpleNamespace(**globals()),ident,tint,accent,feature,kind)
    make_rig();out=ROOT/'ArtSource/Roster'/ident;out.mkdir(parents=True,exist_ok=True)
    result=export(ident,kind,feature,out);results.append(result)
    (out/'model-report.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    print('ROSTER MODEL COMPLETE',ident,len(parts),flush=True)
all_reports=[json.loads(p.read_text(encoding='utf-8')) for p in sorted((ROOT/'ArtSource/Roster').glob('*/model-report.json'))]
(ROOT/'ArtSource/Roster/build-report.json').write_text(json.dumps(all_reports,indent=2),encoding='utf-8')
print('ROSTER AUTHORING COMPLETE',len(results),flush=True)
