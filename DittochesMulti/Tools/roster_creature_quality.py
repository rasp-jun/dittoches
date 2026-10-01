"""Species-specific creature sculpting for the original Adventure-era roster.

Local editable geometry, no extracted assets.  +Y is forward.  The supplied
authoring API owns material creation, binding and exports.
"""
import math
from mathutils import Vector

IDS = {'gabumon','greymon','metalgreymon','garurumon','metalgarurumon',
       'tentomon','kabuterimon','atlur','herakle','kuwagamon','shellmon'}
IVORY='#eee8d3'; DARK='#252533'; BLUE='#30447c'; SILVER='#aebec9'


def reshape(a, changes):
    """Keep all semantic joint names while changing the actual proportions."""
    a.definitions[:] = [(name,parent,Vector(changes.get(name,(p,e))[0]),
                        Vector(changes.get(name,(p,e))[1]))
                       for name,parent,p,e in a.definitions]


def mesh(a,name,vertices,faces,tint,binding):
    import bpy, bmesh
    data=bpy.data.meshes.new(name);data.from_pydata(vertices,[],faces);data.update()
    obj=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(obj)
    bm=bmesh.new();bm.from_mesh(data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(data);bm.free()
    return a.finish(obj,name,tint,binding)


def sculpt(a,name,center,radii,tint,binding='Head',exponent=.83):
    """Rounded, broad planes rather than a stack of unmodified spheres."""
    obj=a.ball(name,center,radii,tint,binding)
    c=Vector(center)
    for v in obj.data.vertices:
        d=v.co-c
        for k in range(3):
            q=d[k]/radii[k];d[k]=math.copysign(abs(q)**exponent,q)*radii[k]
        v.co=c+d
    obj.data.update()
    return obj


def eye(a,center,normal,size,iris='#edaa39',slant=0):
    n=Vector(normal).normalized();q=Vector((0,1,0)).rotation_difference(n)
    c=Vector(center)
    # Almond lids and a shallow convex sclera replace protruding ball eyes.
    # Both corners taper to the facial surface; the pupil sits inside the lid.
    for name,factor,offset,tint in [('Almond eyelid',1,0,DARK),('Almond sclera',.85,.012*size,IVORY)]:
        vertices=[tuple(c+n*(offset+.075*size))]
        for k in range(32):
            angle=math.tau*k/32;xx=math.cos(angle)
            zz=math.sin(angle)*(.58-.22*abs(xx))
            local=Vector((xx*size*factor,offset,zz*size*factor+slant*xx*size))
            vertices.append(tuple(c+q@local))
        faces=[(0,1+(k+1)%32,1+k) for k in range(32)]
        mesh(a,name,vertices,faces,tint,'Head')
    for name,offset,scale,tint in [('Iris',.080,(.40,.022,.39),iris),
        ('Pupil',.108,(.16,.016,.31),DARK),('Catchlight',.126,(.105,.013,.10),'#ffffff')]:
        p=c+n*(offset*size)
        if name=='Catchlight':p+=q@Vector((-.018,0,.021))
        a.ball(name+' inset eye',p,tuple(v*size for v in scale),tint,'Head',q)


def stripe(a,name,center,radii,yz,width,tint,binding='Spine',side=1):
    """Flush stripe sampled on an ellipsoid; no floating rectangular fins."""
    cx,cy,cz=center;rx,ry,rz=radii;vertices=[]
    for i,(yy,zz) in enumerate(yz):
        taper=math.sin(math.pi*(i+.10)/(len(yz)-.80)) if len(yz)>2 else 1
        taper=max(.12,taper)
        previous=yz[max(0,i-1)];following=yz[min(len(yz)-1,i+1)]
        dy=following[0]-previous[0];dz=following[1]-previous[1]
        length=max(.0001,math.hypot(dy,dz));ny=-dz/length;nz=dy/length
        for s in (-1,1):
            y=yy+s*width*taper*.5*ny;z=zz+s*width*taper*.5*nz
            val=max(.025,1-((y-cy)/ry)**2-((z-cz)/rz)**2)
            vertices.append((cx+side*(rx*math.sqrt(val)+.007),y,z))
    faces=[(i*2,i*2+1,i*2+3,i*2+2) for i in range(len(yz)-1)]
    obj=mesh(a,name,vertices,faces,tint,binding)
    # Surface decals are two-sided in the export renderer; explicit reverse
    # faces also keep the editable GLB readable from either side.
    return obj


def teeth(a,center,width,depth,binding='Head',count=7,upper=True,length=.085):
    x,y,z=center
    for i in range(count):
        u=(i/(count-1)-.5)*2;xx=x+u*width;yy=y+depth*math.sqrt(max(0,1-u*u))
        direction=-1 if upper else 1
        a.horn('Individual conical tooth',(xx,yy,z),(xx,yy+.012,z+direction*length*.55),
               (xx,yy+.027,z+direction*length),length*.29,IVORY,binding)


def tail(a,tint,start_z=.84,length=1.3):
    coords=[(0,-.22,start_z),(0,-.55,start_z-.07),(0,-.90,start_z-.14),
            (0,-1.25,start_z-.17),(.05,-1.25-length*.35,start_z-.04),(.10,-1.25-length*.65,start_z+.04)]
    radii=[.23,.19,.13,.085,.042,.004]
    for index,(lo,hi,binding) in enumerate([(0,2,'Tail'),(2,4,'TailMid'),(4,5,'TailTip')]):
        a.tube('Tapered articulated tail',coords[lo:hi+1],radii[lo:hi+1],tint,binding,16)


def dinosaur(a,ident):
    metal=ident=='metalgreymon';skin='#e89936';helmet='#674c35' if not metal else '#9faeb7'
    reshape(a,{'Hips':((0,-.17,.86),(0,.03,1.15)),
               'Spine':((0,.03,1.15),(0,.25,1.48)),
               'Head':((0,.24,1.49),(0,.38,1.80)),
               'Jaw':((0,.30,1.58),(0,.80,1.58)),
               'Tail':((0,-.30,.86),(0,-.83,.73)),
               'TailMid':((0,-.83,.73),(0,-1.38,.72)),
               'TailTip':((0,-1.38,.72),(.10,-1.95,.90))})
    for side,s in [(-1,'L'),(1,'R')]:
        reshape(a,{'UpperArm'+s:((side*.32,.15,1.26),(side*.44,.32,1.10)),
                   'Forearm'+s:((side*.44,.32,1.10),(side*.48,.58,1.17)),
                   'Hand'+s:((side*.48,.58,1.17),(side*.48,.78,1.12)),
                   'Thigh'+s:((side*.32,-.17,.87),(side*.39,.12,.53)),
                   'Shin'+s:((side*.39,.12,.53),(side*.36,.05,.19)),
                   'Foot'+s:((side*.36,.05,.19),(side*.36,.39,.16))})
    a.ball('Torso saurian ribcage',(0,-.005,1.02),(.44,.48,.54),skin,'Spine')
    a.ball('Pelvis broad haunch',(0,-.20,.78),(.46,.38,.38),skin,'Hips')
    a.capsule('Torso forward neck',(0,.15,1.32),(0,.32,1.63),.28,skin,'Spine',.29)
    # Pale abdomen remains a flush inset rather than a protruding ball.
    sculpt(a,'Ventral scales',(0,.436,1.035),(.255,.050,.34),'#e8b866','Spine',.87)
    for i in range(5):
        z=.83+i*.083;a.tube('Abdominal scale seam',[(-.19,.466,z),(0,.491,z-.018),(.19,.466,z)],[.006]*3,'#b37d38','Spine')
    for side,s in [(-1,'L'),(1,'R')]:
        for label,nextlabel,r in [('Thigh','Shin',.25),('Shin','Foot',.155),('UpperArm','Forearm',.10),('Forearm','Hand',.098)]:
            if metal and side==-1 and label in ('UpperArm','Forearm'):continue
            a.capsule(label+' shaped muscle',a.point(label+s),a.point(nextlabel+s),r,skin,label+s)
        a.ball('Foot broad dinosaur pad',a.point('Foot'+s)+Vector((0,.10,-.035)),(.205,.30,.15),skin,'Foot'+s)
        a.claws(a.point('Foot'+s)+Vector((0,.32,-.01)),.127,'Foot'+s,length=.23)
        if not(metal and side==-1):
            a.ball('Hand dinosaur palm',a.point('Hand'+s),(.12,.17,.085),skin,'Hand'+s)
            a.claws(a.point('Hand'+s)+Vector((0,.12,-.01)),.095,'Hand'+s,length=.18)
        for i in range(3):
            yy=-.23+i*.20
            stripe(a,'Blue flank tiger marking',(0,-.005,1.02),(.443,.483,.54),[(yy-.08,1.40),(yy-.02,1.29),(yy+.045,1.14),(yy+.12,1.01)],.105,BLUE,'Spine',side)
        stripe(a,'Blue haunch marking',tuple(a.point('Thigh'+s)+Vector((0,.05,-.12))),(.27,.26,.32),[(-.12,.88),(.02,.84),(.17,.71)],.09,BLUE,'Thigh'+s,side)
    tail(a,skin,.86,1.30)
    for i in range(4):
        y=-.54-i*.23;z=.78-i*.014;r=.18-i*.035
        a.tube('Tail blue cross stripe',[(-r*.75,y,z+r*.35),(0,y,z+r), (r*.75,y,z+r*.35)],[.035,.041,.035],BLUE,'Tail' if i<2 else 'TailMid')
    sculpt(a,'Heavy saurian skull',(0,.40,1.73),(.36,.38,.28),skin)
    sculpt(a,'Long squared upper snout',(0,.72,1.66),(.34,.39,.17),skin,exponent=.64)
    sculpt(a,'Mouth dark cavity',(0,.79,1.45),(.325,.355,.12),DARK,'Jaw',.70)
    sculpt(a,'Wide articulated lower jaw',(0,.77,1.365),(.32,.355,.080),skin,'Jaw',.69)
    teeth(a,(0,.87,1.528),.288,.278,count=9,length=.14)
    teeth(a,(0,.87,1.414),.265,.25,'Jaw',7,False,.084)
    a.ball('Dinosaur tongue',(0,1.00,1.418),(.16,.125,.035),'#b04f64','Jaw')
    for side in (-1,1):
        eye(a,(side*.310,.695,1.786),(side*.45,1,.1),.097,'#b83343')
        a.ball('Nostril',(side*.195,1.075,1.735),(.027,.012,.015),DARK)
    # Helmet is an open brow and crown; eyes stay visible underneath it.
    sculpt(a,'Bone crown',(0,.275,1.89),(.365,.35,.16),helmet,exponent=.72)
    for side in (-1,1):
        a.plate('Swept helmet brow',[(side*.03,.74,1.94),(side*.25,.76,1.92),(side*.39,.58,1.91),
                (side*.345,.61,1.805),(side*.26,.73,1.84),(side*.12,.76,1.85)],.045,helmet)
        a.plate('Bone cheek guard',[(side*.345,.59,1.84),(side*.365,.34,1.85),(side*.40,.40,1.60),
                (side*.28,.59,1.54),(side*.28,.66,1.64)],.035,helmet)
        a.horn('Swept rear helmet horn',(side*.265,.22,1.95),(side*.45,.015,2.15),(side*.56,-.12,2.34),.105,IVORY)
        a.tube('Helmet engraved seam',[(side*.06,.742,1.943),(side*.15,.58,2.011),(side*.20,.30,2.027)],[.008]*3,'#463b31' if not metal else '#576570')
    a.horn('Broad nasal horn',(0,.75,1.875),(0,.85,2.10),(0,.97,2.24),.12,IVORY)
    if metal:
        # One mechanical arm, two chest doors and one tattered organic wing.
        for u,v in [('UpperArmL','ForearmL'),('ForearmL','HandL')]:
            a.capsule('Cybernetic arm casing',a.point(u),a.point(v),.165,SILVER,u)
            a.ball('Mechanical arm joint',a.point(u),(.175,.17,.17),'#52677c',u)
        a.ball('Trident claw gauntlet',a.point('HandL')+Vector((0,.04,0)),(.22,.22,.135),SILVER,'HandL')
        a.claws(a.point('HandL')+Vector((0,.17,0)),.14,'HandL',length=.39,tint='#d6e0e4')
        for side in (-1,1):
            a.plate('Giga missile door',[(side*.015,.525,1.35),(side*.27,.50,1.30),(side*.27,.50,1.01),(side*.015,.53,1.01)],.035,SILVER,'Spine')
            a.ball('Chest launcher port',(side*.142,.552,1.17),(.072,.018,.088),'#4c5d68','Spine')
            for z in (1.045,1.29):a.ball('Missile hatch rivet',(side*.24,.536,z),(.016,.012,.016),'#d5dfe2','Spine')
        a.plate('Cybernetic left cranial panel',[(-.01,.773,1.98),(-.29,.713,1.99),(-.40,.38,2.0),(-.36,.27,1.81),(-.26,.58,1.81)],.03,'#8c9fae')
        a.ball('Red cybernetic sensor',(-.304,.687,1.853),(.047,.022,.032),'#dc4236')
        # Membrane fan has a serrated lower edge, not feather-shaped diamonds.
        for side,s in [(-1,'L'),(1,'R')]:
            pts=[(side*.21,-.16,1.48),(side*.74,-.35,2.11),(side*1.17,-.58,2.35),
                 (side*1.03,-.56,1.79),(side*.84,-.48,1.91),(side*.69,-.43,1.48),
                 (side*.57,-.37,1.64),(side*.35,-.27,1.30)]
            a.plate('Tattered purple membrane',pts,.018,'#a298b6','Wing'+s)
            a.tube('Membrane leading bone',[pts[0],pts[1],pts[2]],[.08,.054,.008],'#bfc0c8','Wing'+s)
            for index in (3,5,7):a.tube('Membrane structural rib',[pts[1],pts[index]],[.025,.01],'#c6c4cd','Wing'+s)
        for i in range(4):
            a.ball('Exposed tail vertebra',(0,-1.13-i*.12,.77),(.087-i*.012,.058,.055),'#bbc7cf','TailMid')


def gabumon(a):
    skin='#e9c84e';fur='#d3dbea';stripeblue='#424578';belly='#5ab8af'
    reshape(a,{'Head':((0,.025,1.28),(0,.05,1.55)), 'Jaw':((0,.20,1.22),(0,.54,1.22))})
    a.ball('Torso rounded young reptile',(0,0,.94),(.32,.29,.39),skin)
    a.ball('Pelvis crouched belly',(0,-.06,.67),(.35,.28,.26),skin,'Hips')
    sculpt(a,'Turquoise belly patch',(0,.272,.91),(.235,.055,.29),belly,'Spine',.80)
    # The red hooked marking is an identifying feature, not a plain belly.
    for side in (-1,1):
        a.tube('Red belly sigil',[(side*.05,.331,1.09),(side*.13,.337,1.10),(side*.15,.340,1.01),(side*.08,.342,.955),(side*.14,.329,.89)],[.018]*5,'#bd425f','Spine')
    a.tube('Central belly sigil',[(0,.338,1.10),(0,.349,.98),(0,.344,.84)],[.024,.03,.018],'#bd425f','Spine')
    for side,s in [(-1,'L'),(1,'R')]:
        for label,end,r in [('Thigh','Shin',.175),('Shin','Foot',.13),('UpperArm','Forearm',.12),('Forearm','Hand',.135)]:
            a.capsule(label+' reptile muscle',a.point(label+s),a.point(end+s),r,skin,label+s)
        a.ball('Foot reptile pad',a.point('Foot'+s)+Vector((0,.12,-.025)),(.18,.27,.125),skin,'Foot'+s)
        a.claws(a.point('Foot'+s)+Vector((0,.34,0)),.11,'Foot'+s,length=.18)
        a.ball('Hand beneath pelt',a.point('Hand'+s),(.14,.16,.075),skin,'Hand'+s)
        a.claws(a.point('Hand'+s)+Vector((0,.13,0)),.094,'Hand'+s,length=.20)
    # Yellow face sits inside the wolf pelt's open mouth.
    sculpt(a,'Yellow reptile face',(0,.16,1.33),(.265,.27,.26),skin)
    sculpt(a,'Yellow snout',(0,.32,1.255),(.265,.235,.115),skin,exponent=.72)
    sculpt(a,'Actual mouth gap',(0,.425,1.20),(.205,.125,.044),DARK,'Jaw',.8)
    teeth(a,(0,.38,1.225),.20,.15,count=5,length=.055)
    sculpt(a,'Hood wolf skull',(0,-.035,1.60),(.39,.29,.265),fur,exponent=.74)
    sculpt(a,'Hood wolf upper muzzle',(0,.325,1.555),(.33,.225,.13),fur,exponent=.62)
    a.ball('Wolf pelt black nose',(0,.529,1.603),(.115,.04,.060),DARK)
    for side in (-1,1):
        eye(a,(side*.281,.229,1.639),(side*.53,1,.02),.12,'#ba324a')
        a.horn('Wolf pelt fang',(side*.245,.42,1.495),(side*.25,.45,1.39),(side*.20,.48,1.34),.051,IVORY)
        a.horn('Swept wolf pelt ear',(side*.30,-.045,1.72),(side*.51,-.12,1.81),(side*.75,-.19,1.86),.135,fur)
        a.horn('Dark inner pelt ear',(side*.40,-.013,1.77),(side*.57,-.08,1.80),(side*.70,-.15,1.84),.059,stripeblue)
        # Side drapes have actual ragged fur silhouette and repeated stripes.
        for i in range(6):
            z=1.47-i*.12
            a.tube('Hood hanging fur',[(side*.29,-.10,z+.09),(side*.34,.005,z),(side*(.38+.018*(i%3)),.065,z-.16)],[.13,.11,.005],fur,'Spine' if i>2 else 'Head')
            a.plate('Indigo jagged pelt band',[(side*.285,.107,z+.06),(side*.39,.117,z+.075),(side*.365,.135,z+.015),(side*.40,.134,z-.025),(side*.29,.12,z+.01)],.006,stripeblue,'Spine' if i>2 else 'Head')
        for i in range(4):
            stripe(a,'Hood forehead stripe',(0,-.035,1.60),(.39,.29,.265),[(-.10+i*.06,1.84),(-.035+i*.06,1.76),(.015+i*.06,1.69)],.046,stripeblue,'Head',side)
        for i in range(3):
            a.horn('Pelt hand talon',(side*.47+(i-1)*.075,.25,.83),(side*.47+(i-1)*.075,.30,.70),(side*.47+(i-1)*.075,.32,.58),.045,'#a7396b','Hand'+('L' if side<0 else 'R'))
    a.horn('Single forehead horn',(0,.16,1.815),(0,.275,2.085),(0,.36,2.29),.115,IVORY)
    for i in range(5):
        z=1.88+i*.055;r=.089-i*.013
        a.tube('Horn growth ridge',[(-r,.202+i*.021,z),(0,.221+i*.021,z+.006),(r,.202+i*.021,z)],[.007]*3,'#b8aa82')
    a.tube('Short reptile tail base',[(0,-.19,.69),(0,-.48,.63),(0,-.68,.68)],[.17,.15,.10],skin,'Tail',16)
    a.tube('Short reptile tail middle',[(0,-.68,.68),(0,-.89,.79),(0,-1.00,.89)],[.10,.061,.032],skin,'TailMid',16)
    a.tube('Short reptile tail tip',[(0,-1.00,.89),(0,-1.10,.98)],[.032,.002],skin,'TailTip',12)


def wolf(a,ident):
    metal=ident=='metalgarurumon';skin='#97a5bf' if metal else '#e3e7e5';accent='#45477f'
    reshape(a,{'Hips':((0,-.37,.92),(0,-.04,1.10)),
               'Spine':((0,-.04,1.10),(0,.38,1.29)),
               'Head':((0,.57,1.22),(0,.72,1.49)),
               'Jaw':((0,.73,1.17),(0,1.14,1.17)),
               'Tail':((0,-.74,1.02),(0,-1.07,1.08)),
               'TailMid':((0,-1.07,1.08),(0,-1.38,1.26)),
               'TailTip':((0,-1.38,1.26),(0,-1.55,1.62))})
    for side,s in [(-1,'L'),(1,'R')]:
        reshape(a,{'UpperArm'+s:((side*.33,.42,1.07),(side*.34,.50,.60)),
                   'Forearm'+s:((side*.34,.50,.60),(side*.35,.65,.21)),
                   'Hand'+s:((side*.35,.65,.21),(side*.35,.94,.18)),
                   'Thigh'+s:((side*.33,-.57,.94),(side*.39,-.42,.57)),
                   'Shin'+s:((side*.39,-.42,.57),(side*.37,-.70,.22)),
                   'Foot'+s:((side*.37,-.70,.22),(side*.37,-.40,.19))})
    a.ball('Long torso canine ribcage',(0,-.13,1.03),(.37,.70,.35),skin,'Spine')
    a.ball('Wolf chest deep brisket',(0,.36,.98),(.35,.35,.43),skin,'Spine')
    a.ball('Pelvis powerful hindquarters',(0,-.56,.98),(.37,.36,.31),skin,'Hips')
    a.capsule('Torso raised canine neck',(0,.38,1.17),(0,.63,1.40),.28,skin,'Spine')
    for side,s in [(-1,'L'),(1,'R')]:
        for u,v,r in [('UpperArm','Forearm',.145),('Forearm','Hand',.103),('Thigh','Shin',.19),('Shin','Foot',.099)]:
            a.capsule(u+' canine muscle',a.point(u+s),a.point(v+s),r,skin,u+s)
        for base in ('Hand','Foot'):
            p=a.point(base+s);a.ball(base+' broad canine paw',p+Vector((0,.13,-.04)),(.19,.275,.15),skin,base+s)
            for i in range(3):
                xx=p.x+(i-1)*.105
                a.ball('Individual toe pad',(xx,p.y+.28,p.z-.045),(.075,.117,.091),skin,base+s)
                a.horn('Hooked wolf claw',(xx,p.y+.335,p.z+.01),(xx,p.y+.425,p.z-.025),(xx,p.y+.435,p.z-.10),.055,'#9e325d' if metal else '#78334f',base+s)
    sculpt(a,'Long wolf cranium',(0,.70,1.45),(.265,.35,.23),skin,exponent=.78)
    sculpt(a,'Long tapered wolf muzzle',(0,.976,1.34),(.194,.33,.135),skin,exponent=.69)
    sculpt(a,'Snarling wolf mouth',(0,1.045,1.259),(.172,.267,.055),DARK,'Jaw',.70)
    sculpt(a,'Long wolf lower jaw',(0,1.025,1.205),(.162,.25,.057),skin,'Jaw',.66)
    sculpt(a,'Canine nose',(0,1.294,1.366),(.119,.048,.06),DARK,exponent=.75)
    teeth(a,(0,1.03,1.283),.16,.19,count=7,length=.069)
    for side,s in [(-1,'L'),(1,'R')]:
        eye(a,(side*.236,.906,1.503),(side*.78,1,.06),.081,'#e5ba41' if not metal else '#d9413f')
        if not metal:
            a.plate('Sharp indigo canine brow',[(side*.09,.943,1.568),(side*.29,.85,1.637),(side*.285,.913,1.549),(side*.177,.995,1.522)],.012,accent)
        a.horn('Upper wolf fang',(side*.148,1.115,1.28),(side*.147,1.134,1.16),(side*.12,1.15,1.135),.033,IVORY)
        a.plate('Broad triangular wolf ear',[(side*.095,.555,1.61),(side*.16,.515,1.81),(side*.31,.39,2.055),(side*.39,.51,1.68),(side*.30,.59,1.59)],.045,skin)
        a.plate('Flat indigo inner wolf ear',[(side*.17,.578,1.67),(side*.215,.515,1.82),(side*.302,.428,1.994),(side*.323,.553,1.70)],.009,accent)
        if not metal:
            for i in range(8):
                z=1.61-i*.095+.015*math.sin(i*2.4);length=.14+.037*(i%3)
                a.tube('Layered shaggy cheek',[(side*.23,.57-.025*(i%2),z),(side*(.33+.025*(i%2)),.45,z-.055),(side*(.38+.05*(i%3)),.19,z-length)],[.095,.065,.003],skin,'Head' if i<3 else 'Spine')
                if i%2==0:
                    a.tube('Indigo cheek fur marking',[(side*.265,.672,z),(side*.34,.574,z-.05),(side*.43,.37,z-length+.04)],[.018,.034,.003],accent,'Head' if i<3 else 'Spine')
            for i in range(7):
                y=.35-i*.16
                stripe(a,'Indigo striped wolf coat',(0,-.13,1.03),(.377,.70,.35),[(y-.09,1.325),(y-.015,1.22),(y+.04,1.05),(y+.10,.88)],.08,accent,'Spine',side)
            for i in range(3):
                stripe(a,'Facial indigo slash',(0,.70,1.45),(.27,.35,.23),[(.56+i*.10,1.65),(.64+i*.09,1.58),(.72+i*.07,1.49)],.06,accent,'Head',side)
    # Tapered, curved and articulated bushy tail.
    a.tube('Wolf tail base',[(0,-.72,1.07),(0,-1.03,1.10),(0,-1.25,1.23)],[.17,.16,.15],skin,'Tail',16)
    a.tube('Wolf tail middle',[(0,-1.25,1.23),(0,-1.44,1.45),(0,-1.52,1.65)],[.15,.135,.083],skin,'TailMid',16)
    a.tube('Wolf tail tapered tip',[(0,-1.52,1.65),(.05,-1.53,1.81),(.12,-1.46,1.91)],[.084,.055,.004],accent,'TailTip',16)
    if not metal:
        for i in range(6):
            y=-.92-i*.115;z=1.12+max(0,i-1)*.115
            for side in (-1,1):a.horn('Tail fur tuft',(side*.09,y,z),(side*.18,y-.09,z+.03),(side*.19,y-.18,z+.095),.075,skin,'Tail' if i<2 else 'TailMid')
    else:
        for side,s in [(-1,'L'),(1,'R')]:
            for u,v in [('UpperArm','Forearm'),('Forearm','Hand'),('Thigh','Shin'),('Shin','Foot')]:
                p=a.point(u+s);end=a.point(v+s)
                a.capsule('Segmented mechanical leg armour',p,end,.17 if u in ('UpperArm','Thigh') else .13,SILVER,u+s)
                a.ball('Mechanical joint inset',p,(.19,.18,.18),'#555779',u+s)
                a.ball('Mechanical joint bolt',p+Vector((side*.17,0,0)),(.035,.091,.091),'#bec4d4',u+s)
            # Side missile pods are fitted, not loose balls.
            a.plate('Shoulder armoured plate',[(side*.32,.45,1.40),(side*.49,.23,1.39),(side*.52,.19,1.03),(side*.39,.44,.91)],.12,'#bec9d2','Spine')
            a.ball('Circular shoulder port',(side*.50,.325,1.17),(.035,.12,.13),'#d28245','Spine')
            a.ball('Black recessed port',(side*.536,.325,1.17),(.012,.085,.09),DARK,'Spine')
            a.plate('Gold dorsal blade',[(side*.16,-.30,1.36),(side*.51,-.50,1.59),(side*1.02,-.79,1.77),(side*.79,-.47,1.43),(side*.26,-.12,1.27)],.025,'#dbce6a','Wing'+s)
            for i in range(3):
                x=side*(.41+i*.15);a.ball('Wing vent', (x,-.42-i*.075,1.45+i*.06),(.041,.018,.025),'#525464','Wing'+s)
            a.plate('Angular canine cheek armour',[(side*.12,1.15,1.50),(side*.28,.82,1.63),(side*.31,.65,1.40),(side*.20,1.15,1.25)],.035,'#aebad0')
            a.tube('Muzzle circuit seam',[(side*.06,1.282,1.435),(side*.10,1.11,1.51),(side*.16,.88,1.595)],[.01]*3,'#545875')
        for i in range(8):
            y=-.77-i*.08;z=1.10+max(0,i-2)*.10
            a.ball('Metal tail segment',(0,y,z),(.145-i*.008,.060,.091),'#b9c0cf','Tail' if i<3 else 'TailMid')


def insect_leg(a,start,elbow,end,tint,binding,claw=False,claw_tint='#44465e'):
    a.tube('Segmented insect limb',[start,elbow,end],[.085,.068,.035],tint,binding,12)
    a.ball('Chitin knee joint',elbow,(.094,.085,.085),tint,binding)
    if claw:
        for i in (-1,1):
            p=Vector(end);a.horn('Hooked pincer finger',p,p+Vector((i*.13,.05,-.09)),p+Vector((i*.09,.14,-.17)),.060,claw_tint,binding)
    else:
        a.horn('Insect hooked toe',end,Vector(end)+Vector((0,.12,-.035)),Vector(end)+Vector((0,.18,-.10)),.052,claw_tint,binding)


def insect(a,ident):
    lady=ident=='tentomon';stag=ident=='kuwagamon';gold=ident=='herakle';red=ident=='atlur'
    tint='#dd403a' if lady else '#dd4933' if stag else '#d0b46c' if gold else '#b53737' if red else '#394e94'
    horncolor='#d6b973' if gold else '#505860' if ident=='kabuterimon' else tint;ventral='#b9bdc1' if lady else '#d1cdb6';chest=1.03 if lady else 1.14
    # Raised thorax, tucked abdomen, and six independently bound appendages.
    reshape(a,{'Hips':((0,-.15,.77),(0,0,chest)),
               'Spine':((0,0,chest),(0,.10,chest+.24)),
               'Head':((0,.14,chest+.20),(0,.20,chest+.46)),
               'Jaw':((0,.34,chest+.08),(0,.53,chest+.08))})
    sculpt(a,'Chitin thorax',(0,-.025,chest),(.35,.31,.37),tint,'Spine',.78)
    sculpt(a,'Segmented beetle abdomen',(0,-.035,.79),(.27,.23,.32),ventral,'Hips',.80)
    for i in range(5):
        z=.56+i*.10;w=.20*math.sin((i+1)/6*math.pi)
        a.tube('Ventral segmented rib',[(-w,.178,z),(0,.24,z-.025),(w,.178,z)],[.027]*3,'#515454' if lady else '#ad4c35' if gold else '#626562','Hips')
    # Closed split elytra retain the beetle's heavy carapace silhouette.
    for side,s in [(-1,'L'),(1,'R')]:
        sculpt(a,'Heavy split wing carapace',(side*.19,-.26,chest+.17),(.30,.30,.41),tint,'Spine',.84)
        a.tube('Carapace seam',[(side*.018,-.42,chest+.52),(side*.025,-.55,chest+.18),(side*.015,-.44,chest-.16)],[.016]*3,'#602f33' if lady or red or stag else '#25283d','Spine')
    cranial=horncolor if ident=='kabuterimon' else tint
    sculpt(a,'Beetle armored cranium',(0,.195,chest+.40),(.30,.27,.24),cranial,'Head',.78)
    if lady:
        for side in (-1,1):
            # Oversized compound eyes are correct for Tentomon only.
            a.ball('Large compound eye',(side*.19,.398,chest+.40),(.14,.105,.19),'#4b9d57')
            for row in range(-3,4):
                for col in range(-2,3):
                    dx=col*.035;dz=row*.037
                    if (dx/.125)**2+(dz/.178)**2>.84:continue
                    yy=.398+.10*math.sqrt(1-(dx/.14)**2-(dz/.19)**2)
                    a.ball('Compound eye facet',(side*.19+dx,yy+.001,chest+.40+dz),(.014,.0025,.017),'#62ae5d' if (row+col)%2 else '#428c4d')
            for yy,zz,rr in [(-.34,chest+.38,.095),(-.40,chest+.02,.11),(-.18,chest+.57,.083)]:
                dy=yy+.26;dz=zz-(chest+.17);dx=.30*math.sqrt(max(.02,1-(dy/.30)**2-(dz/.41)**2))
                normal=Vector((side*dx/.30**2,dy/.30**2,dz/.41**2)).normalized()
                point=Vector((side*(.19+dx),yy,zz))+normal*.004
                a.ball('Black ladybird spot',point,(.008,rr,rr),DARK,'Spine',Vector((1,0,0)).rotation_difference(normal))
            a.tube('Long antenna',[(side*.13,.22,chest+.57),(side*.19,.23,chest+.85),(side*.28,.22,chest+1.04),(side*.32,.26,chest+1.13)],[.027,.023,.018,.009],'#c39847','Head')
        a.tube('Tentomon mouth plate',[(-.18,.443,chest+.20),(0,.50,chest+.18),(.18,.443,chest+.20)],[.035]*3,'#858988','Jaw')
        for i in range(3):a.tube('Small mouth ridges',[(-.12,.476,chest+.12-i*.035),(0,.506,chest+.11-i*.035),(.12,.476,chest+.12-i*.035)],[.01]*3,'#575c5b','Jaw')
    else:
        # The mature beetles have helmet recesses/mandibles, never human eyes.
        sculpt(a,'Recessed beetle mouth',(0,.422,chest+.245),(.19,.095,.16),'#393638','Jaw',.75)
        teeth(a,(0,.43,chest+.32),.145,.06,count=7,length=.081)
        teeth(a,(0,.46,chest+.15),.13,.05,'Jaw',7,False,.069)
        for side in (-1,1):
            a.plate('Armored slitted beetle brow',[(side*.015,.445,chest+.49),(side*.23,.40,chest+.57),(side*.32,.32,chest+.34),(side*.20,.45,chest+.37)],.032,cranial)
            a.plate('Dark beetle eye recess',[(side*.05,.475,chest+.415),(side*.20,.46,chest+.45),(side*.21,.459,chest+.397)],.007,DARK)
            a.horn('Mandible tusk',(side*.20,.405,chest+.25),(side*.28,.56,chest+.18),(side*.12,.62,chest+.10),.066,ventral,'Jaw')
            for j in range(3):
                z=chest+.48-j*.095
                a.tube('Ribbed cranial carapace',[(side*.03,.466,z),(side*.16,.46,z+.035),(side*.29,.33,z+.05)],[.018,.025,.028],cranial)
            if ident=='kabuterimon':
                a.horn('Lateral beetle helmet spur',(side*.26,.20,chest+.44),(side*.42,.15,chest+.62),(side*.48,.08,chest+.84),.092,cranial)
        if stag or gold:
            for side in (-1,1):
                if stag:
                    path=[(side*.22,.28,chest+.47),(side*.35,.55,chest+.28),(side*.37,.78,chest+.025),(side*.16,.91,chest-.12)]
                    a.tube('Forward curved stag mandible',path,[.13,.12,.085,.003],horncolor,'Head',18)
                    for i in range(3):
                        yy=.51+i*.13;z=chest+.31-i*.145
                        a.horn('Inner mandible tooth',(side*.34,yy,z),(side*.24,yy+.025,z-.01),(side*.17,yy+.055,z-.04),.049,tint)
                else:
                    path=[(side*.21,.26,chest+.50),(side*.49,.49,chest+.81),(side*.63,.89,chest+.76),(side*.34,1.15,chest+.50)]
                    a.tube('Hercules broad swept mandible',path,[.17,.18,.14,.004],horncolor,'Head',20)
                    for i in range(3):
                        yy=.50+i*.16;xx=side*(.46+i*.035)
                        a.horn('Inner mandible tooth',(xx,yy,chest+.76),(xx-side*.14,yy+.02,chest+.72),(xx-side*.23,yy+.06,chest+.62),.081,horncolor)
        if not stag:
            a.tube('Rhino beetle great horn',[(0,.20,chest+.53),(0,.30,chest+.84),(0,.49,chest+1.07),(0,.76,chest+1.25)],[.145,.12,.08,.003],horncolor,'Head',18)
            if red or gold:
                for side in (-1,1):a.horn('Forked horn crown',(0,.54,chest+1.10),(side*.19,.63,chest+1.26),(side*.23,.75,chest+1.39),.080,horncolor)
        if red:
            sculpt(a,'Emerald dorsal jewel',(0,-.25,chest+.55),(.18,.18,.13),'#57a997','Spine',.82)
        if gold:
            a.plate('Hercules broad crest',[(-.52,.22,chest+.55),(-.70,.29,chest+.69),(-.44,.37,chest+.87),(0,.31,chest+.66),(.44,.37,chest+.87),(.70,.29,chest+.69),(.52,.22,chest+.55)],.09,'#cbbb83')
    # Six limbs have actual segment angles and broad hooked extremities.
    for side,s in [(-1,'L'),(1,'R')]:
        chains=[('UpperArm'+s,(side*.29,.035,chest+.18),(side*.63,.19,chest+.16),(side*.75,.34,chest+.35),True),
                ('MidLeg'+s,(side*.29,-.04,chest-.03),(side*.58,.075,chest-.17),(side*.72,.24,chest-.30),True),
                ('Thigh'+s,(side*.23,-.07,.75),(side*.42,.08,.49),(side*.48,.30,.24),False)]
        if lady:
            chains[0]=('UpperArm'+s,(side*.28,.02,chest+.06),(side*.51,.18,chest-.12),(side*.65,.29,chest-.04),True)
        for binding,p,knee,end,claw in chains:
            insect_leg(a,p,knee,end,tint,binding,claw,'#bba363' if gold else '#44465e')
            if binding.startswith('Thigh'):
                a.ball('Beetle heavy shin',(side*.43,.11,.45),(.14,.15,.21),tint,binding)
                for delta in (-.10,0,.10):a.horn('Beetle toe',(side*.48+delta,.25,.245),(side*.48+delta,.44,.19),(side*.48+delta,.53,.13),.067,'#bba363' if gold else '#474864',binding)
        if not lady:
            if stag:
                # Kuwagamon's red elytra rise above its back; its pincers curve
                # forward/down around the mouth rather than becoming antlers.
                a.plate('Raised orange stag wing cover',[(side*.19,-.26,chest+.14),(side*.35,-.37,chest+.78),(side*.57,-.45,chest+1.25),(side*.67,-.43,chest+1.19),(side*.60,-.31,chest+.58),(side*.34,-.23,chest+.10)],.045,tint,'Wing'+s)
            # Two long veined insect wings on each side, not leaf diamonds.
            for j in range(2):
                base=(side*.20,-.28,chest+.16-j*.10)
                tip=(side*(1.00+j*.16),-.48-j*.19,chest+1.22-j*.37)
                axis=Vector(tip)-Vector(base);lateral=Vector((side*.16,.035,-.025))
                outline=[base,tuple(Vector(base)+axis*.27+lateral),tuple(Vector(base)+axis*.80+lateral*.63),tip,
                         tuple(Vector(base)+axis*.78-lateral*.58),tuple(Vector(base)+axis*.24-lateral*.40)]
                a.plate('Long translucent wing membrane',outline,.009,'#e0e1c7' if not gold else '#c9e1df','Wing'+s)
                a.tube('Wing outer frame',[outline[0],outline[1],outline[2],outline[3]],[.017,.014,.010,.003],tint,'Wing'+s)
                a.tube('Long wing vein',[base,tuple(Vector(base)+axis*.5),tip],[.009,.007,.003],'#7a8b8e','Wing'+s)
                for k in (1,2):a.tube('Wing branch vein',[tuple(Vector(base)+axis*(.24+k*.20)),outline[k+1]],[.005,.004],'#899995','Wing'+s)


def shellmon(a):
    skin='#e57eb3';shell='#9099b6';ridge='#656b8d'
    reshape(a,{'Head':((0,.38,.92),(0,.50,1.30)),'Jaw':((0,.57,1.05),(0,.94,1.05))})
    a.ball('Shellmon body low mantle',(0,-.07,.49),(.56,.51,.32),skin,'Hips')
    a.capsule('Torso thick rising neck',(0,.22,.58),(0,.42,1.11),.255,skin,'Spine')
    sculpt(a,'Broad amphibian skull',(0,.51,1.27),(.38,.31,.28),skin,exponent=.78)
    sculpt(a,'Square amphibian upper lip',(0,.73,1.18),(.37,.26,.16),skin,exponent=.60)
    sculpt(a,'Large gaping oral cavity',(0,.79,1.06),(.305,.18,.17),'#4c213c','Jaw',.65)
    sculpt(a,'Articulated lower jaw',(0,.71,.92),(.31,.25,.09),skin,'Jaw',.73)
    teeth(a,(0,.78,1.155),.29,.17,count=9,length=.15)
    teeth(a,(0,.79,.958),.265,.14,'Jaw',7,False,.105)
    a.ball('Pink tongue',(0,.87,.985),(.12,.055,.05),'#cd457d','Jaw')
    for side,s in [(-1,'L'),(1,'R')]:
        eye(a,(side*.28,.735,1.39),(side*.43,1,.06),.135,'#409ebe')
        start=(side*.36,.22,.55);elbow=(side*.55,.44,.35);hand=(side*.63,.65,.19)
        a.capsule('Foreleg muscled amphibian',start,elbow,.18,skin,'UpperArm'+s)
        a.capsule('Foreleg lower amphibian',elbow,hand,.155,skin,'Forearm'+s)
        a.ball('Webbed foot amphibian',hand,(.255,.24,.13),skin,'Hand'+s)
        for i in range(3):
            xx=side*.63+(i-1)*.17
            a.capsule('Rounded amphibian digit',(xx,.67,.16),(xx,.91,.12),.085,skin,'Hand'+s)
        for i in range(4):
            z=.74+i*.135
            a.ball('Turquoise skin fleck',(side*(.24+i*.016),.32+i*.065,z),(.035,.016,.048),'#87d4cf','Spine' if i<3 else 'Head')
    # Conical coiled shell: a broad aperture grows into a swept tapered apex.
    a.ball('Shell opening dark rim',(0,-.27,.84),(.70,.33,.73),ridge,'Hips')
    a.ball('Main spiral shell',(0,-.55,1.02),(.69,.59,.69),shell,'Hips')
    # True continuous spiral around the side silhouette, three tapering turns.
    points=[];radii=[]
    for i in range(91):
        t=i/90;angle=t*math.tau*2.8;radius=.66*(1-t)+.03
        points.append((radius*math.cos(angle),-.54-.20*t,1.00+radius*math.sin(angle)))
        radii.append(.105*(1-t)+.026)
    a.tube('Continuous shell spiral ridge',points,radii,'#aeb6ca','Hips',14)
    a.tube('Swept conical shell apex',[(0,-.73,1.21),(0,-.99,1.53),(.04,-1.15,1.83),(.09,-1.23,2.07)],[.39,.27,.13,.005],shell,'Hips',20)
    for ring in range(3):
        yy=-.75-ring*.15;zz=1.21+ring*.21;rr=.37-ring*.085
        pts=[(math.cos(k*math.tau/20)*rr,yy+math.sin(k*math.tau/20)*rr*.42,zz+math.sin(k*math.tau/20)*rr*.35) for k in range(21)]
        a.tube('Conch growth lip',pts,[.03]*len(pts),ridge,'Hips')
    for side in (-1,1):
        for i in range(4):
            z=.64+i*.28;x=side*(.62-.06*max(0,i-1));y=-.52-i*.05
            a.horn('Curved conch shell spike',(x,y,z),(x+side*.22,y-.07,z+.12),(x+side*.29,y-.17,z+.33),.125,shell,'Hips')
            a.ball('Small amber shell pore',(side*.43,-.017-i*.035,.85+i*.19),(.050,.014,.035),'#d5c270','Hips')
    # Short fleshy green tentacles arc in different directions over the brow.
    for i in range(11):
        angle=math.tau*i/11;x=.19*math.cos(angle);y=.42+.15*math.sin(angle)
        a.tube('Curled green head tendril',[(x,y,1.47),(x*1.4,y+.02,1.67),(x*2.1,y+.06,1.76),
               (x*2.55,y+.10,1.69),(x*2.62,y+.13,1.60)],[.052,.043,.034,.022,.008],'#80ae3f','Head',12)


def build(a,ident,tint,accent,feature,kind):
    if ident not in IDS:return False
    if ident in ('greymon','metalgreymon'):dinosaur(a,ident)
    elif ident=='gabumon':gabumon(a)
    elif ident in ('garurumon','metalgarurumon'):wolf(a,ident)
    elif ident=='shellmon':shellmon(a)
    else:insect(a,ident)
    # Prefix selection covers only same-colour organic muscles in this module.
    # Keep all markings, armour, teeth, facial planes and shell pieces separate.
    if kind!='insect':a.smooth_organic()
    return True
