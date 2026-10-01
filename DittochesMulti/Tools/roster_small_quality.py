"""Species-specific organic modelling for the twelve small/plant/bird Digimon.

Local editable geometry, hand-authored from the official reference illustrations.
The host passes its modelling namespace to build(); no assets are read or written
here.  Deliberate curved silhouettes replace the original shared primitive kit.
"""
import math
import bpy
from mathutils import Vector

IDS = {'koromon','tsunomon','mochimon','tanemon','pyocomon','tokomon',
       'piyomon','birdramon','hououmon','patamon','palmon','togemon'}
INK = '#252335'
CREAM = '#fff4db'
WHITE = '#fff9ed'


def _mesh(a,name,verts,faces,tint,binding):
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
    import bmesh
    bm=bmesh.new();bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    bm.to_mesh(mesh);bm.free()
    return a.finish(obj,name,tint,binding)


def _catmull(points,steps=5):
    p=[Vector(v) for v in points];out=[]
    for i in range(len(p)-1):
        p0=p[max(0,i-1)];p1=p[i];p2=p[i+1];p3=p[min(len(p)-1,i+2)]
        for j in range(steps):
            t=j/steps
            out.append(.5*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t))
    return out+[p[-1]]


def curve(a,name,points,radii,tint,binding='Head',steps=5,sides=12):
    points2=_catmull(points,steps);r=[]
    for i in range(len(radii)-1):
        for j in range(steps):r.append(radii[i]+(radii[i+1]-radii[i])*j/steps)
    r.append(radii[-1])
    return a.tube(name,points2,r,tint,binding,sides)


def loft(a,name,profile,tint,binding='Head',ribs=0):
    """Smooth elliptical cross sections: z, x-radius, y-radius, y-centre."""
    prof=_catmull(profile,4);verts=[];faces=[];sides=40
    for p in prof:
        z,rx,ry,y=p
        for k in range(sides):
            t=math.tau*k/sides;rib=1+.035*math.cos(t*ribs) if ribs else 1
            verts.append((max(.003,rx)*math.cos(t)*rib,y+max(.003,ry)*math.sin(t)*rib,z))
    for j in range(len(prof)-1):
        for k in range(sides):
            n=j*sides+k;nn=j*sides+(k+1)%sides
            faces.append((n,nn,nn+sides,n+sides))
    faces.extend([tuple(reversed(range(sides))),tuple((len(prof)-1)*sides+k for k in range(sides))])
    return _mesh(a,name,verts,faces,tint,binding)


def petal(a,name,base,control,tip,width,tint,binding='Head',thickness=.028,ripple=0):
    """Closed, lenticular, curved feather/petal; rounded transverse section."""
    p0,p1,p2=map(Vector,(base,control,tip));verts=[];faces=[];rows=20;sides=8
    for i in range(rows+1):
        t=i/rows;center=(1-t)**2*p0+2*t*(1-t)*p1+t*t*p2
        axis=(2*(1-t)*(p1-p0)+2*t*(p2-p1)).normalized()
        lateral=axis.cross(Vector((0,1,0)))
        if lateral.length<.1:lateral=axis.cross(Vector((1,0,0)))
        lateral.normalize();normal=axis.cross(lateral).normalized()
        w=max(.003,width*math.sin(math.pi*t)**.68*(1-.18*t))
        w*=1+ripple*math.sin(t*math.pi*7)
        h=max(.002,thickness*math.sin(math.pi*t)**.5)
        for k in range(sides):
            theta=math.tau*k/sides
            verts.append(center+lateral*w*math.cos(theta)+normal*h*math.sin(theta))
        if i:
            for k in range(sides):
                n=(i-1)*sides+k;nn=(i-1)*sides+(k+1)%sides
                faces.append((n,nn,nn+sides,n+sides))
    faces.extend([tuple(reversed(range(sides))),tuple(rows*sides+k for k in range(sides))])
    return _mesh(a,name,verts,faces,tint,binding)


def _rig(a,points):
    """Keep standard names/parents while placing joints for this anatomy."""
    for i,(name,parent,start,end) in enumerate(a.definitions):
        if name in points:
            start,end=points[name];a.definitions[i]=(name,parent,Vector(start),Vector(end))


def _face(a,x,y,z,rx=.09,rz=.12,iris='#973b57',tilt=0,black=False,binding='Head'):
    q=None
    if tilt:
        from mathutils import Quaternion
        q=Quaternion((0,1,0),tilt)
    a.ball('Expressive eye rim',(x,y,z),(rx,.016,rz),INK,binding,q)
    if black:
        a.ball('Deep glossy eye',(x,y+.008,z),(rx*.82,.013,rz*.85),INK,binding,q)
        a.ball('Large eye catchlight',(x-rx*.28,y+.020,z+rz*.36),(rx*.25,.007,rz*.25),WHITE,binding)
        a.ball('Small reflected glint',(x+rx*.3,y+.021,z-rz*.25),(rx*.11,.005,rz*.11),WHITE,binding)
        return
    a.ball('Eye white',(x,y+.008,z),(rx*.86,.013,rz*.86),WHITE,binding,q)
    a.ball('Coloured iris',(x,y+.019,z-rz*.02),(rx*.65,.011,rz*.78),iris,binding,q)
    a.ball('Vertical pupil',(x,y+.027,z),(rx*.32,.007,rz*.60),INK,binding,q)
    a.ball('Large eye catchlight',(x-rx*.23,y+.034,z+rz*.36),(rx*.20,.005,rz*.22),WHITE,binding)
    a.ball('Small reflected glint',(x+rx*.28,y+.034,z-rz*.3),(rx*.08,.004,rz*.09),WHITE,binding)


def smile(a,y,z,width=.15,binding='Head',fangs=False):
    curve(a,'Curved smile',[(-width,y,z+.020),(-width*.58,y+.018,z-.006),(0,y+.027,z-.018),
                           (width*.58,y+.018,z-.006),(width,y,z+.020)],[.009]*5,INK,binding,4,8)
    if fangs:
        for s in (-1,1):
            curve(a,'Little upper fang',[(s*width*.65,y+.022,z+.025),(s*width*.62,y+.036,z-.007),
                                        (s*width*.53,y+.038,z-.021)],[.024,.018,.002],WHITE,binding,4,10)


def _baby_rig(a):
    _rig(a,{'Hips':((0,0,.35),(0,0,.43)),'Spine':((0,0,.43),(0,0,.51)),
            'Head':((0,0,.43),(0,0,.57)),'Jaw':((0,.22,.30),(0,.39,.30))})


def _ear(a,name,points,width,tint,binding,inside=None):
    # A broad ribbon (rather than a cylindrical antenna) for soft ear anatomy.
    p0=Vector(points[0]);p2=Vector(points[-1]);p1=Vector(points[len(points)//2])
    obj=petal(a,name,p0,p1,p2,width,tint,binding,.028)
    if inside:
        petal(a,name+' warm inner skin',p0+Vector((0,.025,.015)),p1+Vector((0,.025,0)),
              p2+Vector((0,.018,-.03)),width*.53,inside,binding,.008)
    return obj


def _koromon(a):
    _baby_rig(a)
    pink='#f0a9c2'
    loft(a,'Koromon flattened pear body',[(.10,.10,.09,0),(.15,.34,.28,0),(.30,.51,.36,0),
             (.49,.50,.35,-.015),(.64,.37,.26,-.035),(.70,.06,.06,-.025)],pink)
    for s,suf in [(-1,'L'),(1,'R')]:
        _rig(a,{'Ear'+suf:((s*.23,-.025,.63),(s*.30,-.06,.99))})
        # Wavy ends are part of Koromon's identifying soft antenna silhouette.
        curve(a,'Koromon ear ribbon',[(s*.22,-.03,.61),(s*.26,-.025,.84),(s*.30,-.075,1.05),
                   (s*.43,-.11,1.13),(s*.54,-.08,1.07),(s*.60,-.065,1.09)],
              [.07,.067,.055,.050,.036,.004],pink,'Ear'+suf,6,14)
        _face(a,s*.205,.315,.465,.100,.073,'#ad314e',tilt=-s*.30)
        curve(a,'Koromon upper brow',[(s*.09,.327,.497),(s*.20,.326,.549),(s*.295,.298,.519)],
              [.007,.009,.002],INK,'Head',5,8)
    smile(a,.354,.248,.24,fangs=True)


def _tsunomon(a):
    _baby_rig(a)
    orange='#ed9b31';cream='#f7ead2'
    loft(a,'Tsunomon fuzzy round body',[(.105,.06,.07,0),(.14,.29,.26,0),(.32,.43,.36,-.015),
              (.53,.42,.32,-.025),(.67,.27,.24,-.04),(.72,.03,.03,-.05)],orange)
    # Cream cheek plates meet at the small central muzzle, not a huge white sphere.
    for s in (-1,1):
        a.ball('Tsunomon cream cheek',(s*.17,.309,.36),(.238,.054,.19),cream,'Head')
        for i in range(4):
            ang=.10+i*.36
            petal(a,'Tsunomon cheek fur',(s*(.29-.025*i),.255,.42+i*.034),
                  (s*(.37-.024*i),.285,.445+i*.036),(s*(.41-.026*i),.26,.435+i*.036),
                  .026,cream,thickness=.014)
        _face(a,s*.18,.358,.405,.090,.071,'#ba354d',tilt=-s*.22)
    a.ball('Tsunomon muzzle',(0,.344,.264),(.14,.040,.064),cream,'Head')
    smile(a,.39,.255,.10)
    curve(a,'Tsunomon great curved horn',[(0,-.07,.59),(0,-.055,.87),(0,.015,1.10),(0,.067,1.30)],
          [.15,.115,.054,.002],'#b9bbc6','Head',7,20)
    for i in range(24):
        theta=math.tau*i/24;x=math.cos(theta);y=math.sin(theta)
        z=.49+.10*math.sin(theta*3)
        petal(a,'Tsunomon orange fur tuft',(x*.34,y*.27-.02,z),(x*.43,y*.33-.03,z+.08),
              (x*.44,y*.35-.03,z+.11),.024,'#f6b13d',thickness=.011)


def _mochimon(a):
    _baby_rig(a)
    pink='#efb6d3'
    # Tall droplet silhouette with a scalloped skirt and rounded mitten arms.
    loft(a,'Mochimon pudding torso',[(.13,.20,.18,0),(.23,.34,.25,0),(.43,.30,.25,0),
             (.64,.34,.28,-.005),(.82,.25,.23,-.025),(.89,.07,.06,-.04)],pink)
    for i in range(7):
        theta=math.tau*i/7
        a.ball('Mochimon soft skirt lobe',(math.cos(theta)*.24,math.sin(theta)*.17,.157),(.12,.10,.09),pink,'Head')
    for s,suf in [(-1,'L'),(1,'R')]:
        _rig(a,{'UpperArm'+suf:((s*.26,0,.50),(s*.41,.02,.46)),
                'Forearm'+suf:((s*.41,.02,.46),(s*.52,.07,.52)),'Hand'+suf:((s*.52,.07,.52),(s*.57,.09,.61))})
        curve(a,'Mochimon short arm',[(s*.245,0,.52),(s*.40,.025,.45),(s*.52,.05,.50)],
              [.11,.105,.10],pink,'UpperArm'+suf,6,16)
        a.ball('Mochimon soft hand',(s*.53,.054,.53),(.115,.105,.125),pink,'Hand'+suf)
        for i in range(3):
            p=(s*(.475+i*.045),.07,.62+(1-abs(i-1))*.025)
            a.ball('Mochimon grey fingertips',p,(.024,.034,.029),'#7e777e','Hand'+suf)
        _face(a,s*.135,.258,.686,.074,.084,black=True)
    a.ball('Mochimon laughing mouth',(0,.266,.466),(.068,.019,.10),'#6e2739','Head')
    a.ball('Mochimon tongue',(0,.285,.428),(.046,.013,.047),'#e15872','Head')
    curve(a,'Mochimon upper lip',[(-.10,.279,.56),(-.065,.291,.521),(0,.295,.544),(.065,.291,.521),(.10,.279,.56)],
          [.007]*5,INK,'Head',5,8)


def _tanemon(a):
    _baby_rig(a)
    green='#71972f';cream='#f4e8b7'
    loft(a,'Tanemon seed bulb',[(.12,.12,.13,0),(.21,.31,.26,0),(.41,.40,.32,-.015),
            (.59,.33,.29,-.03),(.75,.14,.16,-.035),(.85,.065,.08,-.01)],green)
    a.ball('Tanemon broad cream face',(0,.246,.40),(.351,.105,.257),cream,'Head')
    for s,suf in [(-1,'L'),(1,'R')]:
        _face(a,s*.17,.337,.454,.085,.101,'#78472e',black=True)
        a.ball('Tanemon pink cheek',(s*.265,.311,.346),(.052,.013,.033),'#dca293','Head')
        a.ball('Tanemon root foot',(s*.245,.065,.14),(.114,.14,.068),cream,'Foot'+suf)
        for i in range(3):
            curve(a,'Tanemon root toe',[(s*.245+(i-1)*.056,.16,.14),(s*.245+(i-1)*.063,.235,.095),
                    (s*.245+(i-1)*.067,.247,.081)],[.025,.019,.002],cream,'Foot'+suf,4,10)
        _rig(a,{'Ear'+suf:((s*.025,-.03,.72),(s*.25,-.04,.98))})
        curve(a,'Tanemon curled leaf stem',[(s*.025,-.025,.73),(s*.04,-.015,.98),(s*.16,-.03,1.17),
               (s*.24,-.02,1.17)],[.049,.034,.020,.009],green,'Ear'+suf,5,12)
        petal(a,'Tanemon broad curled sprout',(s*.13,-.01,1.14),(s*.38,-.025,1.34),
              (s*.58,.02,.98),.16,'#81b63d','Ear'+suf,.028,.08)
        curve(a,'Tanemon sprout midrib',[(s*.14,.005,1.14),(s*.34,.022,1.23),(s*.56,.04,1.005)],
              [.013,.010,.002],'#bbd16e','Ear'+suf,8,8)
    smile(a,.359,.307,.12)


def _bloom(a,center,size,tint,stem_tint,binding='Head',blue=False):
    x,y,z=center
    for i in range(6):
        t=math.tau*i/6+.25;dx=math.cos(t);dy=math.sin(t)
        base=(x+dx*.03,y+dy*.03,z+.12)
        mid=(x+dx*size*.68,y+dy*size*.70,z+.17)
        tip=(x+dx*size,y+dy*size,z-.15)
        petal(a,'Broad drooping flower petal',base,mid,tip,size*.31,tint,binding,.045,.12)
        # A narrower ridge adds a subtle alternating colour without faceted planes.
        petal(a,'Flower petal central fold',(base[0],base[1],base[2]+.012),
              (mid[0],mid[1],mid[2]+.032),(tip[0]*.985,tip[1]*.985,tip[2]+.012),
              size*.085,'#4e73b6' if blue else '#ef98bf',binding,.012,.05)
    a.ball('Flower golden throat',(x,y,z+.14),(.105,.10,.059),'#edcb45',binding)
    for i in range(5):
        t=math.tau*i/5;dx=math.cos(t);dy=math.sin(t)
        curve(a,'Curled flower stamen',[(x,y,z+.16),(x+dx*.12,y+dy*.11,z+.39),
              (x+dx*.20,y+dy*.18,z+.40),(x+dx*.20,y+dy*.18,z+.32)],
              [.018,.014,.012,.008],stem_tint,binding,6,9)
    curve(a,'Flower spiral pistil',[(x,y,z+.15),(x,y,z+.43),(x+.06,y,z+.56),
             (x+.15,y,z+.54),(x+.16,y,z+.45),(x+.09,y,z+.42),(x+.07,y,z+.47)],
          [.028,.028,.029,.029,.025,.020,.004],'#e1943e',binding,6,12)


def _pyocomon(a):
    _baby_rig(a)
    pink='#efbbda'
    loft(a,'Pyocomon onion shaped body',[(.10,.08,.08,0),(.17,.23,.20,0),(.34,.35,.28,0),
             (.51,.29,.25,-.01),(.66,.12,.12,-.02),(.83,.04,.05,-.035)],pink)
    for i in range(6):
        t=math.tau*i/6
        a.ball('Pyocomon soft root petals',(math.cos(t)*.16,math.sin(t)*.14,.105),(.09,.09,.043),pink,'Head')
    for s in (-1,1):
        _face(a,s*.155,.257,.345,.070,.078,'#55a367')
        for q in (-1,1):
            curve(a,'Pyocomon eyelashes',[(s*.155+q*.043,.269,.399),(s*.155+q*.061,.272,.435)],
                  [.008,.002],INK,'Head',5,8)
    smile(a,.275,.231,.090)
    _bloom(a,(0,-.025,.88),.57,'#3f79bb','#e6d348',blue=True)
    for i in range(7):
        t=math.tau*i/7
        curve(a,'Pyocomon fluted flower stem',[(math.cos(t)*.022,-.025+math.sin(t)*.022,.66),
              (math.cos(t)*.047,-.025+math.sin(t)*.047,.80),(math.cos(t)*.10,-.025+math.sin(t)*.10,.90)],
              [.008,.011,.007],'#f5d3df','Head',5,8)


def _tokomon(a):
    _baby_rig(a)
    ivory='#fff1db'
    loft(a,'Tokomon squat soft body',[(.14,.10,.09,0),(.20,.29,.28,0),(.41,.37,.31,0),
             (.60,.29,.26,-.015),(.69,.10,.10,-.03)],ivory)
    for s,suf in [(-1,'L'),(1,'R')]:
        for y in (-.16,.17):
            a.ball('Tokomon short soft paw',(s*.225,y,.15),(.090,.096,.096),ivory,'Foot'+suf if y>.0 else 'Hips')
        _rig(a,{'Ear'+suf:((s*.16,-.02,.60),(s*.23,-.045,.95))})
        curve(a,'Tokomon rippling long ear',[(s*.16,-.02,.60),(s*.20,-.04,.86),(s*.19,-.03,1.04),
                (s*.29,-.025,1.12),(s*.32,-.06,1.24)],
              [.055,.052,.045,.028,.003],ivory,'Ear'+suf,7,16)
        _face(a,s*.147,.278,.483,.046,.067,black=True)
    # A restrained neutral smile; tooth rows articulate with the existing jaw clip.
    smile(a,.316,.345,.13,fangs=True)
    a.ball('Tokomon lower jaw',(0,.226,.272),(.217,.108,.067),ivory,'Jaw')
    for i in range(6):
        x=(i-2.5)*.044
        curve(a,'Tokomon small lower tooth',[(x,.309,.302),(x,.32,.325),(x,.323,.339)],
              [.011,.009,.001],WHITE,'Jaw',3,8)


def _wing_surface(a,name,outline,tint,binding,side=1):
    # Smooth fan rings yield a convex, closed membrane with authored scallops.
    boundary=[];base=[Vector((side*x,y,z)) for x,y,z in outline];n=len(base)
    for i in range(n):
        # Quadratic rounding preserves long tips and deep notches.
        prev=base[(i-1)%n];cur=base[i];nxt=base[(i+1)%n]
        start=cur.lerp(prev,.13);end=cur.lerp(nxt,.13)
        for j in range(4):
            t=j/4;boundary.append((1-t)**2*start+2*t*(1-t)*cur+t*t*end)
    center=sum(boundary,Vector())/len(boundary);verts=[];faces=[];rings=5;count=len(boundary)
    for face in (1,-1):
        for r in range(rings+1):
            u=max(.001,r/rings)
            for p in boundary:
                v=center.lerp(p,u);v.y+=face*.026*math.sin(math.pi*u*.5)+.035*(1-u*u)
                verts.append(v)
        offset=0 if face==1 else (rings+1)*count
        for r in range(rings):
            for i in range(count):
                j=offset+r*count+i;k=offset+r*count+(i+1)%count
                faces.append((j,k,k+count,j+count))
    back=(rings+1)*count
    for i in range(count):
        j=rings*count+i;k=rings*count+(i+1)%count
        faces.append((j,k,back+k,back+j))
    faces.extend([tuple(reversed(range(count))),tuple(back+i for i in range(count))])
    return _mesh(a,name,verts,faces,tint,binding)


def _patamon(a):
    orange='#edab49';cream='#fff0cc'
    _rig(a,{'Hips':((0,-.02,.37),(0,0,.48)),'Spine':((0,0,.48),(0,.02,.57)),
             'Head':((0,.06,.44),(0,.13,.63)),'Jaw':((0,.29,.29),(0,.47,.29))})
    # The head and barrel body form one compact loaf, with four short legs.
    a.ball('Patamon long loaf body',(0,-.055,.40),(.42,.48,.305),orange,'Head')
    a.ball('Patamon cream underside',(0,.02,.275),(.401,.437,.177),cream,'Head')
    a.ball('Patamon broad smiling muzzle',(0,.302,.33),(.31,.126,.151),cream,'Head')
    for s,suf in [(-1,'L'),(1,'R')]:
        _face(a,s*.209,.331,.495,.069,.081,'#3eabb2')
        for y,label in [(.24,'Foot'+suf),(-.32,'Hand'+suf)]:
            a.ball('Patamon cream short leg',(s*.292,y,.175),(.07,.077,.116),cream,label)
            a.ball('Patamon dark paw',(s*.292,y+.022,.099),(.080,.088,.043),'#464751',label)
            for i in range(3):
                curve(a,'Patamon toe separation',[(s*.292+(i-1)*.03,y+.077,.110),(s*.292+(i-1)*.03,y+.10,.097)],
                      [.004,.003],'#292b34',label,3,6)
        _rig(a,{'Wing'+suf:((s*.23,-.05,.55),(s*.66,-.085,.94)),
                'WingTip'+suf:((s*.66,-.085,.94),(s*1.12,-.13,1.33))})
        outline=[(.245,-.07,.57),(.40,-.07,.95),(.79,-.10,1.47),(1.03,-.12,1.66),
                 (.92,-.12,1.25),(.83,-.12,1.03),(1.23,-.14,1.32),(1.43,-.15,1.38),
                 (1.13,-.12,1.02),(.99,-.10,.91),(1.16,-.11,.96),(.83,-.09,.73),(.47,-.07,.58)]
        _wing_surface(a,'Patamon sculpted bat ear',outline,orange,'Wing'+suf,s)
        curve(a,'Patamon ear leading edge',[(s*.26,-.033,.61),(s*.43,-.04,.99),(s*.75,-.06,1.43),(s*1.025,-.09,1.647)],
              [.022,.019,.012,.002],'#d88c2f','Wing'+suf,6,10)
        curve(a,'Patamon ear inner vein',[(s*.31,-.023,.61),(s*.67,-.042,.86),(s*1.13,-.082,1.17)],
              [.017,.012,.002],'#f6c36a','Wing'+suf,6,8)
    smile(a,.432,.295,.094)


def _bird_rig(a,small=False):
    if small:hip=.51;chest=.77;head=1.12;spread=.19;foot=.12
    else:hip=.81;chest=1.10;head=1.55;spread=.25;foot=.17
    _rig(a,{'Hips':((0,-.06,hip),(0,0,chest)),'Spine':((0,0,chest),(0,.015,head-.12)),
             'Head':((0,.03,head-.16),(0,.07,head+.06)),
             'Jaw':((0,.20,head-.13),(0,.45,head-.15))})
    for s,suf in [(-1,'L'),(1,'R')]:
        _rig(a,{'Thigh'+suf:((s*spread,-.06,hip),(s*spread,.025,(hip+foot)/2)),
                'Shin'+suf:((s*spread,.025,(hip+foot)/2),(s*spread,.105,foot)),
                'Foot'+suf:((s*spread,.105,foot),(s*spread,.28,foot-.015)),
                'Wing'+suf:((s*.23,-.10,chest+.08),(s*(.43 if small else .80),-.12,chest+.20)),
                'WingTip'+suf:((s*(.43 if small else .80),-.12,chest+.20),(s*(.64 if small else 1.25),-.16,chest+.23))})
    return hip,chest,head


def _talons(a,s,suf,footz=.13,spread=.21,tint='#edc65d',large=False):
    x=s*spread;y=.13
    curve(a,'Scaled bird lower leg',[(x,-.04,.40 if not large else .64),(x,.025,.29),(x,y,footz+.035)],
          [.065 if not large else .092,.060,.065],tint,'Shin'+suf,6,14)
    a.ball('Bird talon palm',(x,y+.04,footz),(.13,.135,.073),tint,'Foot'+suf)
    for i in range(3):
        dx=(i-1)*.088;end=(x+dx*1.4,y+.25+(1-abs(i-1))*.045,footz-.015)
        curve(a,'Articulated bird toe',[(x+dx*.5,y+.035,footz),(x+dx,y+.16,footz+.005),end],
              [.041,.033,.018],tint,'Foot'+suf,5,12)
        curve(a,'Curved pointed bird claw',[end,(end[0],end[1]+.058,end[2]-.004),
                (end[0],end[1]+.070,end[2]-.044)],
              [.026,.020,.002],'#8c4540' if not large else '#f4ead4','Foot'+suf,5,12)
    curve(a,'Bird rear toe',[(x,y,footz),(x,y-.15,footz+.015),(x,y-.22,footz-.02)],
          [.032,.024,.002],tint,'Foot'+suf,5,10)
    for ring in range(4):
        z=footz+.07+ring*.045
        curve(a,'Bird leg scale edge',[(x-.047,.10,z),(x,.13,z-.008),(x+.047,.10,z)],
              [.006]*3,'#b68f3d','Shin'+suf,4,6)


def _feather_thigh(a,s,suf,tint,large=False):
    x=s*(.25 if large else .185);z=.69 if large else .43
    a.ball('Feathered bird thigh',(x,-.045,z),(.15 if large else .11,.17 if large else .12,.20 if large else .12),tint,'Thigh'+suf)
    for i in range(5):
        theta=math.tau*i/5
        root=(x+math.cos(theta)*.04,-.045+math.sin(theta)*.05,z+.10)
        petal(a,'Layered bird thigh feather',root,
              (x+math.cos(theta)*.14,-.045+math.sin(theta)*.15,z),
              (x+math.cos(theta)*.075,-.04+math.sin(theta)*.08,z-.16),.053,tint,'Thigh'+suf,.024)


def _piyomon(a):
    _bird_rig(a,True)
    pink='#ec94c1';blue='#5367a7'
    loft(a,'Piyomon compact pear breast',[(.37,.08,.075,-.03),(.43,.23,.21,-.03),(.65,.30,.27,-.03),
             (.85,.245,.235,-.035),(.98,.13,.14,-.03)],pink,'Spine')
    a.ball('Piyomon broad cheeked head',(0,.035,1.123),(.315,.273,.288),pink,'Head')
    for s,suf in [(-1,'L'),(1,'R')]:
        _face(a,s*.181,.252,1.145,.075,.083,'#408bb1',tilt=-s*.09)
        for i in range(3):
            base=(s*.18,-.055-i*.04,1.27-i*.09)
            petal(a,'Piyomon swept cheek feathers',base,(s*.33,-.13-i*.035,1.35-i*.11),
                  (s*.45,-.26-i*.055,1.36-i*.12),.063,pink,'Head',.035)
        # Short wing-arms, with three conspicuous red hooked finger claws.
        curve(a,'Piyomon curved wing arm',[(s*.24,.015,.87),(s*.38,.04,.71),(s*.42,.20,.73)],
              [.105,.107,.091],pink,'Wing'+suf,6,16)
        for i in range(3):
            x=s*(.37+i*.057);z=.78-i*.065
            petal(a,'Piyomon folded arm feather',(s*.29,-.015,.86-i*.065),(s*.46,-.04,.74-i*.02),
                  (s*.46,-.10,.61-i*.05),.053,pink,'Wing'+suf,.026)
            curve(a,'Piyomon red hooked hand claw',[(x,.215,z),(x+s*.042,.293,z+.015),
                  (x+s*.038,.35,z-.022)],[.042,.034,.002],'#b6474a','Wing'+suf,6,12)
        _feather_thigh(a,s,suf,pink)
        _talons(a,s,suf,.105,.185)
    # Broad red conical beak; upper and lower lobes articulate separately.
    curve(a,'Piyomon red upper beak',[(0,.255,1.08),(0,.399,1.015),(0,.456,.946)],
          [.143,.101,.012],'#cd634e','Head',8,20)
    curve(a,'Piyomon lower beak',[(0,.264,.946),(0,.372,.908),(0,.424,.94)],
          [.100,.072,.003],'#db7c5d','Jaw',7,16)
    for i in range(7):
        theta=math.tau*i/7
        petal(a,'Piyomon scalloped neck ruff',(math.cos(theta)*.12,math.sin(theta)*.12,.99),
              (math.cos(theta)*.26,math.sin(theta)*.24,.91),
              (math.cos(theta)*.29,math.sin(theta)*.255,.82),.073,'#f4b2d3','Spine',.024)
    # The characteristic spiral crest and swept tail are not generic fan wings.
    crest=[(0,-.06,1.35),(-.035,-.10,1.56),(.035,-.12,1.74),(.16,-.10,1.80),
           (.25,-.065,1.74),(.255,-.04,1.63),(.17,-.02,1.57),(.12,-.015,1.63)]
    curve(a,'Piyomon spiral head plume',crest,[.063,.062,.059,.058,.05,.045,.032,.002],pink,'Head',6,16)
    for i in (1,3,5):
        p0=Vector(crest[i]);p1=Vector(crest[i+1]);mid=p0.lerp(p1,.25);end=p0.lerp(p1,.58)
        curve(a,'Piyomon blue crest band',[mid,end],[.062-i*.004,.061-i*.004],blue,'Head',5,14)
    for i in range(5):
        x=(i-2)*.105;base=(x*.45,-.19,.65);mid=(x,-.52,.78+abs(i-2)*.04);tip=(x*1.65,-.91,.91+abs(i-2)*.065)
        petal(a,'Piyomon swept tail feather',base,mid,tip,.104,pink,'Tail',.040)
        b=Vector(base);c=Vector(mid);t=Vector(tip)
        p=.16*b+.48*c+.36*t
        petal(a,'Piyomon blue tail tip',p,c.lerp(t,.72),tip,.066,blue,'Tail',.030)


def _flame_wing(a,side,suf,gold=False,pair=0):
    tint='#efd35c' if gold else '#e68b24';light='#fff0a2' if gold else '#ffbe3d'
    z=1.35-pair*.34;root=(side*.22,-.13-pair*.13,z)
    elbow=(side*.85,-.18-pair*.12,z+.62-pair*.10)
    wrist=(side*1.45,-.26-pair*.15,z+.98-pair*.22)
    curve(a,'Phoenix wing shoulder' if gold else 'Birdramon flame wing bone',[root,elbow,wrist],
          [.14,.13,.056],tint,'Wing'+suf,7,16)
    for i in range(11):
        u=i/10;base=Vector(root).lerp(Vector(wrist),.15+.76*u)
        # Long overlapping curved primaries, with a tiered leading edge.
        if gold:
            tip=(side*(.55+1.20*u+pair*.10),-.33-pair*.20,z-.47+.90*u-pair*.12)
            control=(side*(.76+1.02*u),-.245-pair*.17,z-.08+.99*u-pair*.06)
        else:
            # Birdramon's feathers rise as individual flame tongues, unlike
            # the hanging feather fan of the evolved golden phoenix.
            tip=(side*(.55+1.10*u),-.34,z+.03+1.60*u)
            control=(side*(.60+1.05*u),-.27,z+.02+1.20*u)
        binding='Wing'+suf if i<4 else 'WingTip'+suf
        petal(a,'Phoenix layered primary' if gold else 'Birdramon living flame feather',base,control,tip,
              .112 if gold else .094,light if i%3==0 else tint,binding,.037,.04 if gold else .10)
        if gold:
            petal(a,'Phoenix ivory feather core',base+Vector((0,.023,-.015)),Vector(control)+Vector((0,.025,0)),
                  Vector(tip)+Vector((0,.024,.018)),.043,'#fff3b8',binding,.012)
        else:
            petal(a,'Birdramon red flame border',base+Vector((0,-.012,0)),Vector(control)+Vector((side*.022,-.022,.04)),
                  Vector(tip)+Vector((side*.065,-.008,-.075)),.034,'#bc4930',binding,.020,.13)
            if i%2==0:
                petal(a,'Birdramon forked flame lick',base.lerp(Vector(control),.65),
                      Vector(control)+Vector((side*.05,.005,.10)),Vector(tip)+Vector((side*.14,0,.10)),
                      .044,light,binding,.018,.11)
    for row in range(2):
        for i in range(6):
            u=i/5;start=Vector(root).lerp(Vector(wrist),.15+.65*u)+Vector((0,.045,-row*.08))
            petal(a,'Layered wing covert',start,start+Vector((side*.10,.01,-.15)),
                  start+Vector((side*.05,.01,-.24)),.067,tint,'Wing'+suf,.025)


def _birdramon(a):
    _bird_rig(a)
    orange='#e68127';dark='#8d4933'
    loft(a,'Birdramon tapered feather torso',[(.66,.08,.075,-.08),(.84,.25,.24,-.08),(1.09,.31,.29,-.075),
              (1.30,.23,.24,-.05),(1.45,.15,.15,0)],orange,'Spine')
    curve(a,'Birdramon arched neck',[(0,0,1.24),(0,.09,1.43),(0,.14,1.61)],
          [.17,.15,.135],orange,'Head',7,18)
    a.ball('Birdramon narrow predatory head',(0,.17,1.58),(.215,.28,.195),orange,'Head')
    for s,suf in [(-1,'L'),(1,'R')]:
        _face(a,s*.148,.356,1.625,.067,.045,'#68a0aa',tilt=-s*.28)
        curve(a,'Birdramon strong flame brow',[(s*.07,.356,1.68),(s*.18,.347,1.70),(s*.27,.24,1.70)],
              [.019,.027,.003],'#b94827','Head',5,10)
        _flame_wing(a,s,suf)
        _feather_thigh(a,s,suf,orange,True)
        _talons(a,s,suf,.15,.26,dark,large=True)
        for i in range(5):
            petal(a,'Birdramon crest flame',(s*.07,-.01,1.64-i*.045),
                  (s*(.13+i*.03),-.19,1.86-i*.035),
                  (s*(.16+i*.053),-.39,2.01-i*.09),.063,'#edab30' if i%2 else '#bf4e28','Head',.027,.12)
    # Open toothy snout, a defining feature that a small yellow bird beak misses.
    curve(a,'Birdramon long upper snout',[(0,.30,1.57),(0,.53,1.55),(0,.70,1.45)],
          [.145,.12,.017],'#e8c590','Head',8,18)
    a.ball('Birdramon dark mouth interior',(0,.46,1.37),(.145,.232,.068),'#592e32','Head')
    curve(a,'Birdramon long lower jaw',[(0,.25,1.38),(0,.49,1.31),(0,.67,1.35)],
          [.129,.087,.012],'#d69c65','Jaw',8,18)
    for s in (-1,1):
        for i in range(5):
            y=.31+i*.071;x=s*(.121-.011*i)
            curve(a,'Birdramon upper needle tooth',[(x,y,1.47-i*.01),(x,y+.017,1.40-i*.012),
                  (x*.93,y+.023,1.37-i*.011)],[.024,.014,.002],WHITE,'Head',4,10)
            curve(a,'Birdramon lower needle tooth',[(x*.93,y,1.34),(x*.88,y+.01,1.39),
                  (x*.83,y+.014,1.42)],[.020,.012,.001],WHITE,'Jaw',4,10)
    for i in range(7):
        x=(i-3)*.071
        petal(a,'Birdramon long flame tail',(x*.5,-.26,.97),
              (x,-.64,.91),(x*1.8,-1.04,.53+abs(i-3)*.042),.089,
              '#f2b637' if i%2 else '#d96028','Tail',.033,.13)


def _hououmon(a):
    _bird_rig(a)
    yellow='#e9ce58';cream='#fff0ba';brown='#79614d'
    loft(a,'Hououmon strong phoenix breast',[(.63,.10,.11,-.04),(.80,.29,.26,-.05),
             (1.04,.37,.34,-.05),(1.29,.28,.30,-.015),(1.49,.14,.16,.02)],yellow,'Spine')
    a.ball('Hououmon crested eagle head',(0,.10,1.64),(.24,.265,.235),yellow,'Head')
    for s,suf in [(-1,'L'),(1,'R')]:
        _face(a,s*.165,.287,1.665,.069,.054,'#a9333d',tilt=-s*.23)
        _flame_wing(a,s,suf,True,0);_flame_wing(a,s,suf,True,1)
        _feather_thigh(a,s,suf,yellow,True)
        _talons(a,s,suf,.16,.27,brown,True)
        for i in range(4):
            petal(a,'Hououmon red gold crest',(s*.045,-.025,1.78),(s*(.09+i*.032),-.22,2.04+i*.04),
                  (s*(.13+i*.064),-.46,2.13+i*.025),.072,'#ac4247' if i%2 else '#eac553','Head',.031)
        for i in range(7):
            t=i/6
            petal(a,'Hououmon layered breast feather',(s*.06,.20,1.40-t*.55),
                  (s*(.17+t*.10),.30,1.30-t*.51),
                  (s*(.21+t*.10),.21,1.16-t*.49),.079,cream,'Spine',.026)
        # Holy rings distinguish the evolved phoenix from a generic yellow bird.
        pts=[]
        for i in range(25):
            t=math.tau*i/24;pts.append((s*.27+.10*math.cos(t),.025+.10*math.sin(t),.36))
        curve(a,'Hououmon ankle holy ring',pts,[.026]*len(pts),'#d9a330','Shin'+suf,2,10)
    curve(a,'Hououmon hooked eagle beak',[(0,.28,1.62),(0,.49,1.57),(0,.53,1.40)],
          [.125,.10,.008],'#b69155','Head',8,20)
    curve(a,'Hououmon lower beak',[(0,.27,1.47),(0,.42,1.45),(0,.47,1.46)],
          [.075,.061,.003],'#b78f54','Jaw',6,16)
    for i in range(9):
        x=(i-4)*.105
        base=(x*.42,-.24,.89);control=(x,-.76,.94);tip=(x*1.40,-1.22,.48+abs(i-4)*.071)
        petal(a,'Hououmon long golden tail plume',base,control,tip,.093,yellow,'Tail',.031)
        q=Vector(base)*.09+Vector(control)*.42+Vector(tip)*.49
        petal(a,'Hououmon ruby tail eye',q,Vector(control).lerp(Vector(tip),.84),tip,.054,'#b4444d','Tail',.032)


def _palmon(a):
    green='#8fbb57';dark='#4c853f';purple='#8d739b'
    _rig(a,{'Hips':((0,-.015,.62),(0,0,.91)),'Spine':((0,0,.91),(0,.005,1.16)),
             'Head':((0,.02,1.12),(0,.025,1.35)),'Jaw':((0,.23,1.105),(0,.31,1.105))})
    loft(a,'Palmon slender plant torso',[(.46,.11,.12,0),(.59,.21,.16,0),(.78,.16,.15,0),
             (.99,.23,.19,0),(1.13,.22,.19,.005)],green,'Spine')
    loft(a,'Palmon tapering bulb head',[(1.06,.16,.16,.005),(1.12,.27,.24,.01),(1.30,.285,.25,.015),
             (1.46,.17,.18,-.005),(1.54,.08,.08,-.005)],green,'Head')
    for s,suf in [(-1,'L'),(1,'R')]:
        _rig(a,{'UpperArm'+suf:((s*.22,0,1.02),(s*.35,.015,.82)),
                'Forearm'+suf:((s*.35,.015,.82),(s*.40,.075,.61)),
                'Hand'+suf:((s*.40,.075,.61),(s*.45,.15,.48)),
                'Thigh'+suf:((s*.16,-.01,.64),(s*.17,.015,.39)),
                'Shin'+suf:((s*.17,.015,.39),(s*.20,.08,.14)),
                'Foot'+suf:((s*.20,.08,.14),(s*.22,.25,.10))})
        curve(a,'Palmon supple leaf arm',[(s*.22,0,1.015),(s*.355,.01,.84),(s*.40,.065,.62)],
              [.090,.076,.080],green,'UpperArm'+suf,7,16)
        for i in range(3):
            x=s*(.34+i*.063)
            curve(a,'Palmon long leaf finger',[(x,.065,.65),(x+s*.045,.115,.48),
                  (x+s*.053,.18,.36)],[.040,.033,.014],green,'Hand'+suf,7,12)
            curve(a,'Palmon violet finger tip',[(x+s*.049,.145,.43),(x+s*.068,.20,.33),
                  (x+s*.071,.23,.29)],[.028,.019,.002],purple,'Hand'+suf,6,12)
            petal(a,'Palmon long arm leaf',(s*.24,0,.985-i*.04),(s*.43,-.022,.84-i*.055),
                  (s*.48,-.03,.64-i*.061),.052,dark,'UpperArm'+suf,.021)
        curve(a,'Palmon root leg',[(s*.16,-.01,.62),(s*.17,.01,.39),(s*.20,.08,.15)],
              [.111,.091,.090],green,'Thigh'+suf,7,16)
        for i in range(3):
            x=s*.20+(i-1)*.073
            curve(a,'Palmon spreading root toe',[(x,.09,.15),(x+(i-1)*.032,.25,.095),
                  (x+(i-1)*.046,.33,.071)],[.051,.036,.003],'#bdab68','Foot'+suf,6,12)
        _face(a,s*.137,.242,1.297,.085,.078,black=True,tilt=-s*.13)
        petal(a,'Palmon cheek leaf',(s*.17,.14,1.29),(s*.30,.17,1.25),(s*.35,.12,1.14),.059,green,'Head',.020)
    smile(a,.275,1.13,.15,fangs=True)
    _bloom(a,(0,-.015,1.51),.57,'#d965a4','#e1c541')
    # Winding shallow veins follow the torso and roots instead of torso stripes.
    for i in range(7):
        x=(i-3)*.043
        curve(a,'Palmon natural trunk vein',[(x*.9,.152,.55),(x,.165,.74),
              (x*.73,.186,.96)],
              [.006,.007,.002],dark,'Spine',6,7)


def _togemon(a):
    green='#85af42';dark='#527831';red='#cb5247'
    _rig(a,{'Hips':((0,0,.58),(0,0,.92)),'Spine':((0,0,.92),(0,0,1.32)),
             'Head':((0,0,1.25),(0,0,1.57)),'Jaw':((0,.28,1.05),(0,.35,1.05))})
    loft(a,'Togemon ribbed cactus trunk',[(.36,.20,.20,0),(.51,.32,.26,0),
             (1.05,.40,.29,0),(1.48,.37,.28,0),(1.72,.25,.22,-.01),(1.81,.05,.06,-.015)],
         green,'Spine',ribs=10)
    for s,suf in [(-1,'L'),(1,'R')]:
        _rig(a,{'UpperArm'+suf:((s*.31,0,1.08),(s*.54,0,.99)),
                'Forearm'+suf:((s*.54,0,.99),(s*.65,.035,1.34)),
                'Hand'+suf:((s*.65,.035,1.34),(s*.65,.06,1.51)),
                'Thigh'+suf:((s*.19,0,.61),(s*.21,.015,.34)),
                'Shin'+suf:((s*.21,.015,.34),(s*.22,.095,.14)),
                'Foot'+suf:((s*.22,.095,.14),(s*.22,.30,.12))})
        curve(a,'Togemon bent cactus arm',[(s*.30,0,1.12),(s*.51,-.005,1.0),
                (s*.64,.01,1.10),(s*.65,.03,1.37)],
              [.135,.132,.13,.12],green,'UpperArm'+suf,8,18)
        curve(a,'Togemon cactus leg',[(s*.19,0,.59),(s*.22,.01,.33),
                (s*.23,.12,.15)],[.175,.155,.16],green,'Foot'+suf,7,18)
        a.ball('Togemon broad cactus foot',(s*.23,.14,.14),(.19,.23,.115),green,'Foot'+suf)
        a.ball('Togemon boxing glove',(s*.65,.065,1.52),(.22,.19,.21),red,'Hand'+suf)
        a.ball('Togemon boxing glove thumb',(s*.52,.175,1.44),(.094,.075,.10),'#ba493e','Hand'+suf)
        a.capsule('Togemon glove cuff',(s*.65,.03,1.32),(s*.65,.045,1.395),.136,'#b84a3f','Hand'+suf)
        curve(a,'Togemon glove seam',[(s*.53,.218,1.49),(s*.65,.251,1.45),
                (s*.76,.21,1.50)],[.007]*3,'#963d37','Hand'+suf,6,8)
    # Face holes remain simple and slightly sunken, not protruding cartoon eyes.
    for x,z,rx,rz in [(-.15,1.48,.055,.080),(.15,1.48,.055,.080),(0,1.21,.056,.090)]:
        a.ball('Togemon dark cactus face opening',(x,.283 if z>1.3 else .30,z),(rx,.012,rz),'#27301f','Spine')
    for i in range(9):
        theta=-math.pi*.04+i*math.pi*1.08/8
        x=math.cos(theta);y=math.sin(theta)
        curve(a,'Togemon recessed cactus groove',[(x*.22,y*.21,.40),
              (x*.36,y*.285,.90),(x*.36,y*.27,1.40),(x*.20,y*.18,1.74)],
              [.008,.009,.009,.003],dark,'Spine',8,8)
        for j in range(5):
            z=.61+j*.20;radius=.36 if z<1.51 else .31
            p=Vector((x*radius,y*.282,z));d=Vector((x,y,.20)).normalized()
            a.ball('Togemon cactus areole',p,(.022,.017,.015),'#758b39','Spine')
            curve(a,'Togemon fine cactus spine',[p,p+d*.065,p+d*.105],
                  [.009,.006,.001],'#d9cb91','Spine',3,8)
    for i in range(7):
        theta=math.tau*i/7
        petal(a,'Togemon orange crown leaf',(0,-.01,1.78),(math.cos(theta)*.16,math.sin(theta)*.14,1.95),
              (math.cos(theta)*.27,math.sin(theta)*.21,2.04),.047,
              '#dd9c4f' if i%2 else '#be7d39','Head',.023)


def _join_skin(a,names,label):
    """Union selected matching skin, retaining four interpolated bone weights.

    Facial inlays, feathers, petals, claws and different colours stay separate.
    This removes cylinder end-caps and sphere intersections at organic joints.
    """
    selected=[(obj,binding) for obj,binding in a.parts if any(obj.name.startswith(prefix) for prefix in names)]
    if len(selected)<2:return
    # Separate palettes before union: remeshing must never recolour the design.
    groups={}
    for obj,binding in selected:
        key=tuple(round(x,6) for x in obj.data.materials[0].diffuse_color)
        groups.setdefault(key,[]).append((obj,binding))
    for index,items in enumerate(groups.values()):
        if len(items)<2:continue
        segments=[]
        for obj,binding in items:
            bounds=[obj.matrix_world@Vector(v) for v in obj.bound_box]
            center=sum(bounds,Vector())/8
            radius=max(.055,max((p-center).length for p in bounds)*.65)
            segments.append((binding,center,radius))
        bpy.ops.object.select_all(action='DESELECT')
        for obj,_ in items:obj.select_set(True)
        active=items[0][0];bpy.context.view_layer.objects.active=active
        chosen={id(obj) for obj,_ in items}
        bpy.ops.object.join();active.name=label+' continuous skin '+str(index)
        mod=active.modifiers.new('Connected organic volume','REMESH');mod.mode='VOXEL';mod.voxel_size=.014
        mod.use_smooth_shade=True;bpy.ops.object.modifier_apply(modifier=mod.name)
        mod=active.modifiers.new('Soft joint transition','SMOOTH');mod.factor=.63;mod.iterations=4
        bpy.ops.object.modifier_apply(modifier=mod.name)
        mod=active.modifiers.new('Review mesh density','DECIMATE');mod.ratio=.62;bpy.ops.object.modifier_apply(modifier=mod.name)
        for face in active.data.polygons:face.use_smooth=True
        bindings={name:active.vertex_groups.new(name=name) for name in set(n for n,_,_ in segments)}
        for vertex in active.data.vertices:
            scores={}
            for name,center,radius in segments:
                score=math.exp(-3*((vertex.co-center).length/radius)**2)
                scores[name]=max(scores.get(name,0),score)
            top=sorted(scores.items(),key=lambda p:-p[1])[:4];total=sum(score for _,score in top)
            if total<1e-15:
                name=min(segments,key=lambda item:(vertex.co-item[1]).length)[0]
                bindings[name].add([vertex.index],1,'REPLACE')
            else:
                for name,score in top:
                    if score/total>1e-5:bindings[name].add([vertex.index],score/total,'REPLACE')
        a.parts[:]=[(obj,binding) for obj,binding in a.parts if id(obj) not in chosen]+[(active,None)]


def build(a,ident,tint,accent,feature,kind):
    """Build the complete geometry; return False only for another author's IDs."""
    builders={'koromon':_koromon,'tsunomon':_tsunomon,'mochimon':_mochimon,'tanemon':_tanemon,
              'pyocomon':_pyocomon,'tokomon':_tokomon,'patamon':_patamon,'piyomon':_piyomon,
              'birdramon':_birdramon,'hououmon':_hououmon,'palmon':_palmon,'togemon':_togemon}
    if ident not in builders:return False
    builders[ident](a)
    names={
       'koromon':['Koromon flattened pear body','Koromon ear ribbon'],
       'tsunomon':['Tsunomon cream cheek','Tsunomon cheek fur','Tsunomon muzzle'],
       'mochimon':['Mochimon pudding torso','Mochimon soft skirt lobe','Mochimon short arm','Mochimon soft hand'],
       'pyocomon':['Pyocomon onion shaped body','Pyocomon soft root petals'],
       'tokomon':['Tokomon squat soft body','Tokomon short soft paw','Tokomon rippling long ear'],
       'patamon':['Patamon cream underside','Patamon broad smiling muzzle','Patamon cream short leg'],
       'piyomon':['Piyomon compact pear breast','Piyomon broad cheeked head','Piyomon curved wing arm','Feathered bird thigh'],
       'palmon':['Palmon slender plant torso','Palmon tapering bulb head','Palmon supple leaf arm','Palmon long leaf finger','Palmon root leg'],
       'togemon':['Togemon ribbed cactus trunk','Togemon bent cactus arm','Togemon cactus leg','Togemon broad cactus foot'],
       'birdramon':['Birdramon tapered feather torso','Birdramon arched neck','Birdramon narrow predatory head','Feathered bird thigh'],
       'hououmon':['Hououmon strong phoenix breast','Hououmon crested eagle head','Feathered bird thigh'],
    }.get(ident,[])
    _join_skin(a,names,ident)
    return True
