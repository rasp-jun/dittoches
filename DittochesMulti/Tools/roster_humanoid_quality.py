"""Individually sculpted, reference-led humanoids for the editable roster.

This module deliberately does not reuse the roster's generic toy biped.  It
builds tapered anatomy, fitted clothing, bevelled armour and layered wings.
The public entry point receives the authoring module's live helper namespace.
"""
import math
import bpy
import bmesh
from mathutils import Vector

IDS = {'angemon', 'holyangemon', 'seraphimon', 'wargreymon',
       'weregarurumon', 'lilimon', 'rosemon', 'garudamon', 'devimon', 'etemon'}
WHITE = '#f1f0e5'
SILVER = '#b6c7d5'
DARK = '#242b39'
GOLD = '#ddb63f'
SKIN = '#e3b496'


def mesh(a, name, vertices, faces, tint, binding, smooth=True):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    bm = bmesh.new()
    bm.from_mesh(data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(data)
    bm.free()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    a.finish(obj, name, tint, binding)
    for polygon in obj.data.polygons:
        polygon.use_smooth = smooth
    return obj


def loft(a, name, rings, tint, binding='Spine', sides=28):
    """Anatomical elliptical cross sections: (x,y,z,width,depth)."""
    vertices = []
    for x, y, z, width, depth in rings:
        for index in range(sides):
            angle = math.tau * index / sides
            vertices.append((x + width * math.cos(angle), y + depth * math.sin(angle), z))
    faces = [tuple(reversed(range(sides)))]
    for row in range(len(rings)-1):
        for index in range(sides):
            nxt = (index+1) % sides
            faces.append((row*sides+index, row*sides+nxt, (row+1)*sides+nxt, (row+1)*sides+index))
    faces.append(tuple((len(rings)-1)*sides+index for index in range(sides)))
    return mesh(a, name, vertices, faces, tint, binding)


def shaped_limb(a, name, p, q, radius, tint, binding, profile=None, depth=.9):
    p, q = Vector(p), Vector(q)
    axis = (q-p).normalized()
    u = axis.cross(Vector((0, 1, 0))).normalized()
    v = axis.cross(u).normalized()
    profile = profile or [(0, .75), (.12, .97), (.32, 1.08), (.58, .94), (.82, .70), (1, .61)]
    vertices = []
    sides = 20
    for t, amount in profile:
        c = p.lerp(q, t)
        for index in range(sides):
            ang = math.tau*index/sides
            vertices.append(c + radius*amount*(u*math.cos(ang)+v*math.sin(ang)*depth))
    faces = [tuple(reversed(range(sides)))]
    for row in range(len(profile)-1):
        for index in range(sides):
            nxt = (index+1) % sides
            faces.append((row*sides+index, row*sides+nxt, (row+1)*sides+nxt, (row+1)*sides+index))
    faces.append(tuple((len(profile)-1)*sides+i for i in range(sides)))
    return mesh(a, name, vertices, faces, tint, binding)


def curve(a, name, points, width, tint, binding='Spine'):
    return a.tube(name, points, [width]*len(points), tint, binding, sides=10)


def band(a, name, p, q, radius, tint, binding, width=.06):
    p, q = Vector(p), Vector(q)
    direction = (q-p).normalized()
    return shaped_limb(a, name, p, p+direction*width, radius, tint, binding,
                       [(0,1), (.15,1.035), (.85,1.035), (1,1)], 1)


def inset_plate(a, name, outline, tint, binding='Spine', edge=GOLD, thickness=.027):
    a.plate(name+' rim', outline, thickness, edge, binding)
    center = sum((Vector(v) for v in outline), Vector())/len(outline)
    inside = [tuple(center+(Vector(v)-center)*.86+Vector((0,.012,0))) for v in outline]
    a.plate(name+' face', inside, thickness*.65, tint, binding)


def skeleton(a, ident):
    female = ident in ('lilimon', 'rosemon')
    ape = ident == 'etemon'
    broad = ident in ('wargreymon', 'weregarurumon', 'garudamon')
    hip = 1.24 if not ape else .87
    chest = 1.77 if not ape else 1.35
    head = 2.38 if not ape else 1.88
    shoulder = .32 if female else (.47 if broad else .38)
    if ape: shoulder = .38
    elbow_x = shoulder + (.13 if female else .15)
    hand_x = elbow_x + .08
    elbow_z = chest - .10
    hand_z = hip + (.12 if not ape else .015)
    spread = .17 if female else (.25 if broad else .205)
    if ident == 'devimon':
        hand_z = 1.02
        elbow_z = 1.54
        hand_x = .65
    a.definitions[:] = []
    a.bone('Hips', None, (0,-.025,hip), (0,0,chest))
    a.bone('Spine','Hips',(0,0,chest),(0,.025,head-.23))
    a.bone('Head','Spine',(0,.025,head-.20),(0,.04,head+.16))
    a.bone('Jaw','Head',(0,.12,head-.13),(0,.32,head-.13))
    a.bone('Tail','Hips',(0,-.14,hip),(0,-.50,hip-.03))
    a.bone('TailMid','Tail',(0,-.50,hip-.03),(0,-.82,hip+.08))
    a.bone('TailTip','TailMid',(0,-.82,hip+.08),(0,-1.02,hip+.22))
    for sign, suffix in ((-1,'L'),(1,'R')):
        p=(sign*shoulder,0,chest+.20)
        e=(sign*elbow_x,.015,elbow_z)
        h=(sign*hand_x,.10,hand_z)
        k=(sign*spread,.04,(hip+.16)*.5)
        f=(sign*spread,.03,.16)
        a.bone('UpperArm'+suffix,'Spine',p,e)
        a.bone('Forearm'+suffix,'UpperArm'+suffix,e,h)
        a.bone('Hand'+suffix,'Forearm'+suffix,h,Vector(h)+Vector((0,.13,-.05)))
        a.bone('Thigh'+suffix,'Hips',(sign*spread,-.015,hip),k)
        a.bone('Shin'+suffix,'Thigh'+suffix,k,f)
        a.bone('Foot'+suffix,'Shin'+suffix,f,Vector(f)+Vector((0,.23,-.02)))
        a.bone('Wing'+suffix,'Spine',(sign*.21,-.16,chest+.18),(sign*.70,-.20,chest+.50))
        a.bone('WingTip'+suffix,'Wing'+suffix,(sign*.70,-.20,chest+.50),(sign*1.40,-.25,chest+.30))
        a.bone('Ear'+suffix,'Head',(sign*.14,.01,head+.14),(sign*.25,-.01,head+.46))
    return hip, chest, head


def torso(a, ident, tint, hip, chest, female=False):
    broad = ident in ('wargreymon','weregarurumon','garudamon')
    waist = .155 if female else (.245 if broad else .205)
    width = .28 if female else (.44 if broad else .34)
    depth = .16 if female else (.245 if broad else .205)
    loft(a,'Sculpted torso',[(0,0,hip-.06,waist*1.25,depth*.90),
         (0,0,hip+.10,waist*1.2,depth*.92),(0,0,hip+.29,waist,depth*.76),
         (0,0,chest-.10,width*.79,depth*.95),(0,-.012,chest+.04,width,depth),
         (0,-.024,chest+.17,width*.92,depth*.91),(0,-.018,chest+.25,width*.55,depth*.62)],tint)
    pelvis_tint=WHITE if ident in ('angemon','holyangemon','seraphimon') else tint
    loft(a,'Anatomical pelvis',[(0,-.012,hip-.19,waist*.78,depth*.78),
         (0,-.012,hip-.02,waist*1.22,depth),(0,-.012,hip+.12,waist*1.2,depth*.90)],pelvis_tint,'Hips')
    if not female and ident not in ('angemon','holyangemon','seraphimon','etemon'):
        for sign in (-1,1):
            a.ball('Pectoral anatomy',(sign*width*.43,depth*.86,chest+.06),
                   (width*.43,.028,.083),tint,'Spine')
        for row in range(3):
            for sign in (-1,1):
                a.ball('Abdominal anatomy',(sign*.078,depth*.74,hip+.30+row*.115),(.071,.034,.060),tint,'Spine')


def hands(a, suffix, tint, scale=1, claw=False, claw_tint=WHITE):
    p = a.point('Hand'+suffix)
    s = -1 if suffix=='L' else 1
    a.ball('Carpal palm',p+Vector((0,.035,-.015)),(.085*scale,.055*scale,.102*scale),tint,'Hand'+suffix)
    for index in range(4):
        x = (index-1.5)*.044*scale
        base = p+Vector((x,.048,-.075*scale))
        length=(.135-abs(index-1.5)*.013)*scale
        tip=base+Vector((x*.22,.047,-length))
        shaped_limb(a,'Articulated finger',base,tip,.023*scale,tint,'Hand'+suffix,
                    [(0,1),(.25,1.03),(.53,.9),(.72,.91),(1,.55)])
        if claw:
            a.horn('Curved nail',tip,tip+Vector((0,.055,-.020)),tip+Vector((0,.075,-.065)),.022*scale,claw_tint,'Hand'+suffix)
        else:
            a.ball('Finger nail',tip+Vector((0,.012,.012)),(.012*scale,.008,.022*scale),'#e6c5b1','Hand'+suffix)
    base=p+Vector((-s*.072,.055,.009))
    a.tube('Opposed thumb',[base,base+Vector((-s*.055,.047,-.049)),base+Vector((-s*.05,.075,-.108))],
           [.033*scale,.03*scale,.019*scale],tint,'Hand'+suffix)


def limbs(a, ident, tint, leg_tint, female=False, claws=False):
    broad=ident in ('wargreymon','weregarurumon','garudamon')
    for suffix in ('L','R'):
        shoulder=a.point('UpperArm'+suffix);elbow=a.point('Forearm'+suffix);hand=a.point('Hand'+suffix)
        thigh=a.point('Thigh'+suffix);knee=a.point('Shin'+suffix);foot=a.point('Foot'+suffix)
        arm=.085 if female else (.14 if broad else .103)
        leg=.115 if female else (.19 if broad else .143)
        shaped_limb(a,'Anatomical upper arm',shoulder,elbow,arm,tint,'UpperArm'+suffix)
        a.ball('Elbow articulation',elbow,(arm*.70,arm*.70,arm*.72),tint,'Forearm'+suffix)
        shaped_limb(a,'Tapered forearm',elbow,hand,arm*.88,tint,'Forearm'+suffix,
                    [(0,.82),(.19,1.06),(.40,.93),(.73,.61),(1,.53)])
        shaped_limb(a,'Anatomical thigh',thigh,knee,leg,leg_tint,'Thigh'+suffix,
                    [(0,.85),(.15,1.04),(.38,1),(.69,.83),(1,.58)])
        a.ball('Patella',knee,(leg*.57,leg*.60,leg*.60),leg_tint,'Shin'+suffix)
        shaped_limb(a,'Tapered calf',knee,foot,leg*.73,leg_tint,'Shin'+suffix,
                    [(0,.80),(.20,1.10),(.38,1.03),(.68,.62),(1,.46)])
        hands(a,suffix,tint,.82 if female else (1.24 if broad else 1),claws)
        a.ball('Anatomical instep',foot+Vector((0,.10,-.055)),(.095 if female else .132,.215,.087),leg_tint,'Foot'+suffix)


def face(a, ident, head, tint=SKIN, female=False, masked=False):
    width=.145 if female else .166
    loft(a,'Defined jaw and cheek planes',[(0,.02,head-.20,width*.47,.082),
         (0,.035,head-.15,width*.79,.124),(0,.032,head-.065,width,.153),
         (0,.018,head+.06,width*1.04,.151),(0,.003,head+.155,width*.93,.132),
         (0,-.018,head+.21,width*.50,.080)],tint,'Head')
    a.capsule('Slender neck',(0,0,head-.39),(0,.015,head-.13),.075 if female else .101,tint,'Head')
    if masked:
        return
    for s in (-1,1):
        # Almond silhouette, narrow exposed sclera and matching eyebrows.
        outline=[(s*.025,.182,head+.031),(s*.078,.19,head+.074),(s*.134,.165,head+.045),
                 (s*.096,.176,head+.013),(s*.046,.187,head+.007)]
        a.plate('Almond eye outline',outline,.008,DARK,'Head')
        eye_center=sum((Vector(v) for v in outline),Vector())/len(outline)
        a.plate('Flattened almond sclera',[tuple(eye_center+(Vector(v)-eye_center)*.76+Vector((0,.009,0))) for v in outline],.004,WHITE,'Head')
        c=Vector((s*.080,.197,head+.036))
        a.ball('Iris',c,(.017,.004,.021),'#4a9b77','Head')
        a.ball('Pupil',c+Vector((0,.004,0)),(.007,.003,.018),DARK,'Head')
        a.ball('Eye highlight',c+Vector((-.006,.007,.008)),(.004,.002,.005),WHITE,'Head')
        curve(a,'Upper sculpted eyelid',[(s*.027,.190,head+.033),(s*.078,.200,head+.068),(s*.131,.177,head+.047)],.007,tint,'Head')
        curve(a,'Sculpted eyebrow',[(s*.036,.169,head+.09),(s*.083,.173,head+.115),(s*.137,.151,head+.085)],.009,'#73513a','Head')
        a.ball('Ear anatomy',(s*width,.006,head-.014),(.038,.039,.071),tint,'Head')
    a.plate('Nose planes',[(-.019,.162,head+.02),(.019,.162,head+.02),(.026,.191,head-.062),
                         (0,.218,head-.070),(-.026,.191,head-.062)],.024,tint,'Head')
    curve(a,'Upper lip',[(-.055,.166,head-.115),(-.015,.177,head-.112),(0,.179,head-.117),(.018,.177,head-.112),(.055,.166,head-.115)],.008,'#b77370','Jaw')
    curve(a,'Lower lip',[(-.043,.168,head-.122),(0,.178,head-.13),(.043,.168,head-.122)],.006,'#d9958c','Jaw')


def feather(a,name,start,end,width,tint,binding='Spine'):
    """Curved ten-section feather with raised shaft and tapered tip."""
    p,q=Vector(start),Vector(end)
    axis=q-p
    lateral=axis.cross(Vector((0,1,0))).normalized()
    verts=[]
    sections=[(0,.22),(.15,.69),(.35,1),(.59,.92),(.78,.63),(.93,.24),(1,.008)]
    for t,w in sections:
        center=p+axis*t+Vector((0,-.065*math.sin(t*math.pi),0))
        verts.extend((center-lateral*width*w,center+Vector((0,.017,0)),center+lateral*width*w))
    faces=[]
    for row in range(len(sections)-1):
        for strip in range(2):
            i=row*3+strip
            faces.append((i,i+1,i+4,i+3))
    obj=mesh(a,name,verts,faces,tint,binding)
    solid=obj.modifiers.new('Feather thickness','SOLIDIFY');solid.thickness=.011
    bpy.context.view_layer.objects.active=obj
    bpy.ops.object.modifier_apply(modifier=solid.name)
    curve(a,name+' shaft',[p,p+axis*.42+Vector((0,.021,0)),p+axis*.91],.008,tint,binding)


def angel_wings(a,chest,pairs,tint=WHITE):
    # Each pair occupies a distinct fan, keeping the correct 6/8/10 wing count.
    for s,suffix in ((-1,'L'),(1,'R')):
        for pair in range(pairs):
            fan=math.radians(60-pair*(94/max(1,pairs-1)))
            root=Vector((s*.20,-.19-pair*.025,chest+.15-pair*.05))
            length=1.28 if pair in (0,pairs-1) else 1.43
            endpoint=root+Vector((s*math.cos(fan)*length,-.055,math.sin(fan)*length))
            elbow=root.lerp(endpoint,.53)+Vector((0,-.055,.14))
            a.tube('Wing anatomical leading edge',[root,elbow,endpoint],[.055,.051,.017],tint,'Wing'+suffix)
            for j in range(10):
                t=.16+j*.077
                start=root.lerp(endpoint,t)+Vector((0,-.02,math.sin(t*math.pi)*.10))
                length2=.45+.27*math.sin(t*math.pi)
                tip=start+Vector((s*(.30+t*.16),-.09,-length2))
                feather(a,'Layered angel flight feather',start,tip,.103+(1-t)*.025,tint,
                        'Wing'+suffix if j<5 else 'WingTip'+suffix)
            for j in range(7):
                start=root.lerp(endpoint,.10+j*.10)+Vector((0,.028,.025))
                tip=start+Vector((s*.17,.025,-.30))
                feather(a,'Overlapping wing covert',start,tip,.079,'#d8dfe3' if tint==WHITE else '#e8ca6b','Wing'+suffix)


def helmet(a,head,mode):
    silver='#bbc8d0';blue='#466782'
    loft(a,'Helmet fitted crown',[(0,-.008,head+.02,.186,.165),(0,-.018,head+.15,.178,.158),
         (0,-.035,head+.25,.116,.108),(0,-.047,head+.30,.038,.044)],silver,'Head')
    for s in (-1,1):
        a.plate('Separate fitted visor half',[(s*.009,.186,head+.15),(s*.125,.166,head+.195),
             (s*.181,.135,head+.093),(s*.143,.179,head-.061),(s*.053,.206,head-.08),(s*.010,.214,head+.016)],.017,silver,'Head')
        curve(a,'Helmet gold inlay',[(s*.021,.214,head+.139),(s*.038,.221,head+.03),(s*.116,.207,head-.050)],.009,GOLD,'Head')
        a.plate('Helmet temple guard',[(s*.168,.12,head+.13),(s*.211,.045,head+.07),(s*.181,.071,head-.15),(s*.120,.172,head-.19)],.030,blue if mode=='seraphimon' else silver,'Head')
    if mode=='holyangemon':
        a.plate('Violet helmet central crest',[(-.044,.237,head+.32),(0,.267,head+.41),(.044,.237,head+.32),(.028,.261,head-.035),(-.028,.261,head-.035)],.025,'#7e4994','Head')
        for s in (-1,1):
            feather(a,'Helmet swept feather',(s*.12,-.04,head+.18),(s*.31,-.14,head+.48),.10,WHITE,'Head')
    elif mode=='seraphimon':
        for s in (-1,1):
            feather(a,'Crown blade',(s*.02,-.04,head+.25),(s*.19,-.16,head+.64),.066,SILVER,'Head')
        a.plate('Blue facial cruciform',[(-.020,.25,head+.22),(.020,.25,head+.22),(.030,.252,head-.07),(0,.252,head-.13),(-.030,.252,head-.07)],.009,blue,'Head')


def boots(a,tint,edge=GOLD,high=.68):
    for suffix in ('L','R'):
        f=a.point('Foot'+suffix);k=a.point('Shin'+suffix)
        top=f.lerp(k,high)
        shaped_limb(a,'Fitted boot',top,f,.109,tint,'Shin'+suffix,
                    [(0,1.18),(.12,1.18),(.44,.96),(.75,.70),(1,.70)])
        band(a,'Boot upper rim',top,f,.129,edge,'Shin'+suffix,.035)
        a.ball('Boot vamp',f+Vector((0,.095,-.061)),(.125,.216,.086),tint,'Foot'+suffix)
        curve(a,'Boot piping',[f+Vector((-.10,-.02,-.061)),f+Vector((-.10,.21,-.063)),f+Vector((0,.29,-.056)),f+Vector((.10,.21,-.063)),f+Vector((.10,-.02,-.061))],.012,edge,'Foot'+suffix)


def angels(a,ident,hip,chest,head):
    skin=SKIN if ident=='angemon' else '#d0d5d5'
    torso(a,ident,skin,hip,chest)
    limbs(a,ident,skin,WHITE)
    face(a,ident,head,SKIN,masked=True)
    helmet(a,head,ident)
    angel_wings(a,chest,{'angemon':3,'holyangemon':4,'seraphimon':5}[ident],GOLD if ident=='seraphimon' else WHITE)
    boots(a,WHITE if ident!='seraphimon' else '#6685a3',high=.95)
    for suffix in ('L','R'):
        p=a.point('Forearm'+suffix);h=a.point('Hand'+suffix)
        shaped_limb(a,'Holy fitted bracer',p.lerp(h,.32),h,.087,WHITE if ident!='seraphimon' else SILVER,'Forearm'+suffix,
                    [(0,1.25),(.15,1.2),(.65,.9),(1,.85)])
        band(a,'Gold wrist binding',p.lerp(h,.88),h,.085,GOLD,'Forearm'+suffix,.045)
    belt_z=hip+.11
    loft(a,'Wide holy belt',[(0,0,belt_z-.06,.235,.187),(0,0,belt_z+.055,.229,.184)],GOLD,'Hips')
    a.ball('Belt gemstone',(0,.199,belt_z),(.060,.027,.063),'#9666ad','Hips')
    if ident=='angemon':
        for s in (-1,1):
            a.plate('White fitted body garment',[(s*.03,.22,chest+.21),(s*.29,.15,chest+.18),(s*.16,.18,hip+.24),(s*.03,.195,hip+.24)],.021,WHITE)
            curve(a,'Garment gold seam',[(s*.04,.241,chest+.20),(s*.15,.226,chest-.08),(s*.15,.201,hip+.27)],.013,GOLD)
            # The striped hanging cloth is characteristic of the original form.
            a.plate('Long blue holy ribbon',[(s*.15,.212,hip+.08),(s*.23,.215,hip+.04),(s*.19,.25,hip-.70),(s*.12,.235,hip-.80),(s*.11,.22,hip-.43)],.010,'#536b99','Hips')
            for row in range(6):
                z=hip-.04-row*.095
                curve(a,'Ribbon symbolic stitch',[(s*.146,.246,z),(s*.192,.249,z-.028),(s*.156,.247,z-.039)],.006,GOLD,'Hips')
        hand=a.point('HandR')
        x,y=hand.x,hand.y+.06
        a.tube('Golden holy staff',[(x,y,.20),(x,y,1.55),(x,y,2.81)],[.026]*3,GOLD,'HandR')
        for z in (.23,.39,2.63,2.76):
            a.ball('Staff collar',(x,y,z),(.043,.043,.043),GOLD,'HandR')
        a.horn('Staff cap',(x,y,2.76),(x,y,2.84),(x,y,2.88),.048,GOLD,'HandR')
        # Hair visible beneath the helmet, without making a second oversized skull.
        for s in (-1,1):
            for j in range(4):
                feather(a,'Long golden hair',(s*.11,-.105,head+.07),(s*(.15+j*.021),-.15,head-.33-j*.023),.039,'#d1a954','Head')
    elif ident=='holyangemon':
        a.plate('Purple holy tunic',[(-.29,.19,chest+.16),(.29,.19,chest+.16),(.18,.197,hip+.17),(-.18,.197,hip+.17)],.020,'#845ba5')
        curve(a,'Gilded high collar',[(-.31,.145,chest+.24),(-.22,.235,chest+.11),(0,.262,chest+.02),(.22,.235,chest+.11),(.31,.145,chest+.24)],.038,GOLD)
        for s,suffix in ((-1,'L'),(1,'R')):
            p=a.point('UpperArm'+suffix)
            inset_plate(a,'Holy shoulder',[(p.x-s*.08,.12,p.z+.075),(p.x+s*.16,.10,p.z+.055),(p.x+s*.21,.14,p.z-.07),(p.x-s*.01,.21,p.z-.11)],'#7e50a3','UpperArm'+suffix)
            for j in range(4):
                feather(a,'Collar feather',(s*(.08+j*.055),.22,chest+.04),(s*(.12+j*.065),.23,hip+.20-j*.04),.060,WHITE)
        hand=a.point('HandR')
        a.plate('Excalibur luminous blade',[tuple(hand+Vector((-.065,.065,.04))),tuple(hand+Vector((-.050,.065,.92))),tuple(hand+Vector((0,.065,1.12))),tuple(hand+Vector((.055,.065,.92))),tuple(hand+Vector((.065,.065,.04)))],.026,'#a679d2','HandR')
        curve(a,'Excalibur ridge',[hand+Vector((0,.082,.02)),hand+Vector((0,.082,1.06))],.009,'#e5c6fc','HandR')
    else:
        for s,suffix in ((-1,'L'),(1,'R')):
            p=a.point('UpperArm'+suffix)
            inset_plate(a,'Seraph shoulder',[(p.x-s*.10,.17,p.z+.16),(p.x+s*.10,.14,p.z+.22),(p.x+s*.26,.13,p.z+.03),(p.x+s*.18,.22,p.z-.17),(p.x-s*.07,.22,p.z-.10)],'#426184','UpperArm'+suffix)
            for j in range(3):
                z=chest+.12-j*.14
                inset_plate(a,'Silver articulated breastplate',[(s*.015,.246,z+.047),(s*.25,.218,z+.10),(s*.215,.242,z-.025),(s*.023,.26,z-.058)],SILVER)
            inset_plate(a,'Armoured fauld',[(s*.03,.22,hip+.10),(s*.24,.18,hip+.13),(s*.31,.20,hip-.25),(s*.07,.24,hip-.33)],'#7295b4','Hips')
            k=a.point('Shin'+suffix)
            inset_plate(a,'Seraph knee crest',[(k.x-.12,.145,k.z+.11),(k.x,.17,k.z+.19),(k.x+.12,.145,k.z+.11),(k.x+.08,.175,k.z-.07),(k.x-.08,.175,k.z-.07)],SILVER,'Shin'+suffix)
        a.plate('Sacred gold tabard',[(-.105,.254,hip+.09),(.105,.254,hip+.09),(.13,.257,.32),(-.13,.257,.32)],.018,'#efd17b','Hips')
        for row in range(8):
            z=hip-.03-row*.102
            for s in (-1,1):
                curve(a,'Tabard engraved glyph',[(s*.018,.282,z),(s*.084,.282,z-.020),(s*.056,.282,z-.044),(s*.091,.282,z-.069)],.007,'#934643','Hips')


def dragon(a,hip,chest,head):
    orange='#dba043';steel='#aebdc8';gold='#e1b743'
    torso(a,'wargreymon','#3b4148',hip,chest)
    limbs(a,'wargreymon',orange,orange,claws=True)
    face(a,'wargreymon',head,orange,masked=True)
    # The extended faceted snout is not a human mask pasted on a round sphere.
    a.plate('Dragon angular face armour',[(-.18,.14,head+.17),(-.085,.245,head+.28),(0,.29,head+.16),(.085,.245,head+.28),(.18,.14,head+.17),(.205,.18,head-.055),(.123,.38,head-.16),(0,.435,head-.24),(-.123,.38,head-.16),(-.205,.18,head-.055)],.090,steel,'Head')
    for s in (-1,1):
        a.plate('Recessed dragon socket',[(s*.038,.301,head+.068),(s*.173,.229,head+.123),(s*.15,.298,head-.005),(s*.064,.338,head-.007)],.012,DARK,'Head')
        a.plate('Emerald dragon eye',[(s*.068,.32,head+.055),(s*.153,.263,head+.085),(s*.138,.313,head+.020)],.006,'#49b986','Head')
        a.horn('Swept metal dragon horn',(s*.117,-.025,head+.16),(s*.226,-.086,head+.38),(s*.29,-.18,head+.52),.077,steel,'Head')
        for j in range(7):
            feather(a,'Red swept mane',(s*.12,-.08,head+.12-j*.045),(s*(.31+j*.014),-.25,head+.14-j*.085),.042,'#ab4a38','Head')
    a.horn('Central helmet blade',(0,.24,head+.12),(0,.32,head+.34),(0,.32,head+.48),.055,steel,'Head')
    for s,suffix in ((-1,'L'),(1,'R')):
        p=a.point('UpperArm'+suffix);e=a.point('Forearm'+suffix);h=a.point('Hand'+suffix);k=a.point('Shin'+suffix);f=a.point('Foot'+suffix)
        inset_plate(a,'Angular gold pauldron',[(p.x-s*.15,.12,p.z+.17),(p.x+s*.085,.085,p.z+.27),(p.x+s*.26,.10,p.z+.055),(p.x+s*.18,.23,p.z-.18),(p.x-s*.095,.25,p.z-.125)],gold,'UpperArm'+suffix,steel,.07)
        a.horn('Pauldron spike',p+Vector((s*.055,.025,.19)),p+Vector((s*.10,-.025,.34)),p+Vector((s*.15,-.055,.47)),.067,steel,'UpperArm'+suffix)
        for j in range(3):
            z=chest+.13-j*.12
            a.plate('Curved thoracic steel rib',[(s*.022,.278,z+.035),(s*.26,.203,z+.063),(s*.27,.212,z-.008),(s*.05,.281,z-.035)],.043,steel)
        shaped_limb(a,'Gold forearm gauntlet',e.lerp(h,.15),h,.184,gold,'Forearm'+suffix,[(0,1.25),(.17,1.25),(.72,.95),(1,.98)])
        band(a,'Gauntlet steel cuff',e.lerp(h,.16),h,.235,steel,'Forearm'+suffix,.068)
        a.plate('Gauntlet dorsal plate',[tuple(h+Vector((-.19,.13,.09))),tuple(h+Vector((.19,.13,.09))),tuple(h+Vector((.16,.19,-.11))),tuple(h+Vector((-.16,.19,-.11)))],.067,gold,'Hand'+suffix)
        for j in range(3):
            x=h.x+(j-1)*.13
            a.horn('Dramon destroyer blade',(x,h.y+.16,h.z-.025),(x,h.y+.32,h.z-.25),(x,h.y+.43,h.z-.54),.055,steel,'Hand'+suffix)
        inset_plate(a,'Dragon knee armour',[(k.x-.15,.15,k.z+.16),(k.x,.19,k.z+.23),(k.x+.15,.15,k.z+.16),(k.x+.115,.22,k.z-.06),(k.x,.245,k.z-.12),(k.x-.115,.22,k.z-.06)],steel,'Shin'+suffix,DARK)
        a.plate('Dragon shin guard',[(f.x-.105,.13,f.z+.13),(k.x-.11,.14,k.z-.11),(k.x+.11,.14,k.z-.11),(f.x+.105,.13,f.z+.13)],.040,'#505d6e','Shin'+suffix)
        curve(a,'Shin red seam',[(f.x,.185,f.z+.12),(k.x,.20,k.z-.12)],.026,'#ad473a','Shin'+suffix)
        for j in range(3):
            x=f.x+(j-1)*.11
            a.horn('Dragon toe claw',(x,.20,.14),(x,.37,.12),(x,.46,.06),.070,steel,'Foot'+suffix)
        # Two independent shield halves with inset panels on the back.
        outline=[(s*.04,-.265,chest+.44),(s*.52,-.34,chest+.61),(s*.72,-.39,chest+.24),(s*.64,-.43,hip-.02),(s*.17,-.36,hip-.09)]
        a.plate('Brave shield silhouette',outline,.07,DARK,'Spine')
        a.plate('Brave shield gold panel',[(x*.94,y-.083,z) for x,y,z in outline],.025,gold,'Spine')
        for j in range(3):
            a.plate('Brave shield radial engraving',[(s*(.17+j*.12),-.45,chest+.20),(s*(.24+j*.12),-.47,chest+.10),(s*(.29+j*.10),-.48,hip+.08)],.012,'#a5812a','Spine')
    a.ball('Chest red crest',(0,.292,chest+.035),(.075,.038,.085),'#b84536','Spine')


def wolf(a,hip,chest,head):
    fur='#aabdd4';blue='#324c82';cream='#e7e9df';leather='#795739'
    torso(a,'weregarurumon',fur,hip,chest)
    limbs(a,'weregarurumon',fur,blue,claws=True)
    a.capsule('Wolf muscular neck',(0,-.01,chest+.18),(0,.025,head-.12),.14,fur,'Head')
    # Sloped wolf skull, elongated muzzle and large cheek ruffs.
    loft(a,'Lupine skull',[(0,.0,head-.19,.12,.12),(0,.015,head-.10,.22,.18),(0,-.015,head+.12,.205,.18),(0,-.045,head+.23,.13,.13)],fur,'Head')
    mesh(a,'Angular tapered wolf muzzle',[(-.13,.12,head-.015),(.13,.12,head-.015),(-.075,.405,head-.072),(.075,.405,head-.072),
         (-.10,.13,head-.14),(.10,.13,head-.14),(-.065,.399,head-.14),(.065,.399,head-.14)],
         [(0,2,3,1),(0,4,6,2),(1,3,7,5),(4,5,7,6),(2,6,7,3),(0,1,5,4)],cream,'Head')
    mesh(a,'Angular wolf jaw',[(-.095,.16,head-.161),(.095,.16,head-.161),(-.061,.372,head-.156),(.061,.372,head-.156),
         (-.078,.18,head-.206),(.078,.18,head-.206),(-.054,.357,head-.196),(.054,.357,head-.196)],
         [(0,2,3,1),(0,4,6,2),(1,3,7,5),(4,5,7,6),(2,6,7,3),(0,1,5,4)],cream,'Jaw')
    a.plate('Wolf angular black nose',[(-.065,.412,head-.068),(.065,.412,head-.068),(.048,.420,head-.117),(0,.429,head-.131),(-.048,.420,head-.117)],.027,DARK,'Head')
    curve(a,'Wolf jaw separation',[(-.098,.19,head-.148),(-.062,.379,head-.150),(.062,.379,head-.150),(.098,.19,head-.148)],.009,DARK,'Jaw')
    for s,suffix in ((-1,'L'),(1,'R')):
        a.plate('Wolf angular eye socket',[(s*.055,.176,head+.095),(s*.185,.126,head+.093),(s*.174,.162,head+.014),(s*.084,.203,head+.008)],.022,blue,'Head')
        a.plate('Narrow wolf amber eye',[(s*.074,.192,head+.064),(s*.163,.171,head+.067),(s*.141,.186,head+.025),(s*.095,.202,head+.023)],.005,'#d5b959','Head')
        a.plate('Wolf vertical pupil',[(s*.115,.209,head+.059),(s*.124,.206,head+.060),(s*.120,.21,head+.026)],.006,DARK,'Head')
        a.horn('Wolf ear',(s*.13,-.05,head+.135),(s*.205,-.08,head+.37),(s*.26,-.12,head+.54),.105,fur,'Head')
        a.leaf('Wolf blue ear core',(s*.135,.017,head+.20),(s*.235,-.07,head+.49),.042,blue,'Head')
        for j in range(5):
            feather(a,'Sculpted cheek fur',(s*.145,.01,head+.025-j*.042),(s*(.29+j*.007),-.045,head-.05-j*.073),.042,cream,'Head')
        for j in range(3):
            a.plate('Blue face stripe',[(s*.022,.173,head+.18-j*.057),(s*.106,.179,head+.13-j*.057),(s*.084,.190,head+.106-j*.057)],.011,blue,'Head')
            a.leaf('Blue torso tiger marking',(s*.18,.19,chest+.14-j*.135),(s*.36,.145,chest+.09-j*.135),.034,blue,'Spine')
        t=a.point('Thigh'+suffix);k=a.point('Shin'+suffix);f=a.point('Foot'+suffix)
        shaped_limb(a,'Torn denim trouser',t,k+Vector((0,0,-.055)),.202,blue,'Thigh'+suffix,
                    [(0,.94),(.18,1.03),(.45,1.02),(.7,.88),(1,.83)])
        for j in range(4):
            a.leaf('Frayed denim hem',k+Vector(((j-1.5)*.064,.137,.01)),k+Vector(((j-1.5)*.073,.148,-.12-.03*(j%2))),.032,blue,'Thigh'+suffix)
            feather(a,'Ankle wolf fur',f+Vector(((j-1.5)*.055,-.02,.20)),f+Vector(((j-1.5)*.065,.15,.055)),.037,cream,'Shin'+suffix)
        inset_plate(a,'Asymmetric leather knee guard',[(k.x-.15,.175,k.z+.115),(k.x+.15,.175,k.z+.115),(k.x+.14,.18,k.z-.105),(k.x-.14,.18,k.z-.105)],leather,'Shin'+suffix, '#bd954c')
        if suffix=='R':
            for j in range(3):a.horn('Knee spike',(k.x+(j-1)*.10,.19,k.z),(k.x+(j-1)*.13,.27,k.z+.03),(k.x+(j-1)*.14,.33,k.z+.045),.037,SILVER,'Shin'+suffix)
        for j in range(3):
            x=f.x+(j-1)*.11
            a.horn('Magenta wolf talon',(x,.20,.135),(x,.35,.10),(x,.43,.045),.056,'#985887','Foot'+suffix)
        h=a.point('Hand'+suffix)
        a.plate('Brass knuckle guard',[tuple(h+Vector((-.12,.13,-.02))),tuple(h+Vector((.12,.13,-.02))),tuple(h+Vector((.12,.16,-.09))),tuple(h+Vector((-.12,.16,-.09)))],.035,GOLD,'Hand'+suffix)
    # Original asymmetric left sleeve and brown shoulder protector.
    p=a.point('UpperArmL');h=a.point('HandL');e=a.point('ForearmL')
    shaped_limb(a,'Left fitted dark sleeve',p,e,.151,DARK,'UpperArmL')
    shaped_limb(a,'Left lower sleeve',e,h,.125,DARK,'ForearmL')
    inset_plate(a,'Left leather shoulder armour',[(p.x-.19,.10,p.z+.12),(p.x+.14,.12,p.z+.16),(p.x+.12,.21,p.z-.12),(p.x-.16,.18,p.z-.15)],leather,'UpperArmL','#ac854e')
    for t in (.25,.73):band(a,'Leather arm strap',e.lerp(h,t),h,.123,leather,'ForearmL',.052)
    loft(a,'Leather trouser belt',[(0,0,hip+.005,.27,.215),(0,0,hip+.085,.267,.21)],leather,'Hips')
    a.plate('Belt buckle',[(-.045,.225,hip+.005),(.045,.225,hip+.005),(.045,.225,hip+.08),(-.045,.225,hip+.08)],.016,GOLD,'Hips')
    a.ball('White chest ruff',(0,.192,chest-.065),(.19,.039,.245),cream,'Spine')
    curve(a,'Dog tag chain',[(-.13,.16,chest+.20),(-.09,.235,chest-.04),(0,.253,chest-.11),(.09,.235,chest-.04),(.13,.16,chest+.20)],.012,SILVER)
    a.plate('Dog tag',[(-.027,.27,chest-.095),(.031,.27,chest-.095),(.031,.27,chest-.18),(-.027,.27,chest-.18)],.013,SILVER)


def plant_fairy(a,ident,hip,chest,head):
    rose=ident=='rosemon';red='#af334e' if rose else '#d2709e';green='#326d40'
    torso(a,ident,red if rose else green,hip,chest,True)
    limbs(a,ident,red if rose else SKIN,DARK if rose else SKIN,True)
    face(a,ident,head,SKIN,True)
    # A crown made of distinct cupped petals replaces a flat daisy hat.
    petals=7 if rose else 3
    for j in range(petals):
        angle=math.tau*j/petals
        s=math.cos(angle);d=math.sin(angle)
        origin=(s*.05,-.03+d*.06,head+.10)
        tip=(s*.235,-.06+d*.22,head+.45+(j%2)*.065)
        feather(a,'Layered rose crown petal' if rose else 'Lily flower crown petal',origin,tip,.127 if rose else .21,red,'Head')
    for j in range(3 if rose else 1):
        angle=j*2.1
        a.ball('Crown spiral bud',(math.cos(angle)*.058,-.04+math.sin(angle)*.047,head+.34),(.086,.08,.14),red,'Head')
    for s in (-1,1):
        feather(a,'High botanical collar',(s*.055,-.055,head-.23),(s*.39,-.15,head+.12),.127,green,'Spine')
        curve(a,'Gold collar vein',[(s*.075,.02,head-.22),(s*.20,-.005,head-.025),(s*.35,-.085,head+.105)],.009,GOLD)
    for j in range(8):
        angle=math.tau*j/8
        s,d=math.cos(angle),math.sin(angle)
        feather(a,'Overlapping shaped skirt petal',(s*.145,d*.13,hip+.17),(s*.33,d*.29,hip-.35-(.09 if j%2 else 0)),.135,red,'Hips')
    if rose:
        for s in (-1,1):
            inset_plate(a,'Rose fitted bodice',[(s*.025,.188,chest+.18),(s*.185,.176,chest+.20),(s*.13,.18,hip+.33),(s*.023,.19,hip+.13)],red,'Spine',GOLD,.014)
            for j in range(3):
                curve(a,'Bodice golden vine',[(s*.018,.209,hip+.17+j*.14),(s*.075,.21,hip+.25+j*.13),(s*.115,.194,hip+.22+j*.135)],.008,GOLD)
            # Long split white petals trail behind her, leaving the legs legible.
            feather(a,'Long white rose cape',(s*.13,-.14,hip+.15),(s*.71,-.36,.51),.29,WHITE,'Hips')
            feather(a,'Rear rose cape petal',(s*.10,-.23,hip+.09),(s*.38,-.48,.43),.22,'#d9d9db','Hips')
        boots(a,DARK,edge='#53505a',high=.88)
        for suffix in ('L','R'):
            e=a.point('Forearm'+suffix);h=a.point('Hand'+suffix)
            shaped_limb(a,'Rose gauntlet',e.lerp(h,.25),h,.073,red,'Forearm'+suffix)
            for t in (.2,.65,.84):band(a,'Rose gold wrist vine',e.lerp(h,t),h,.080,GOLD,'Forearm'+suffix,.023)
        h=a.point('HandR')
        path=[h+Vector((0,.06,-.09)),Vector((.77,.18,1.02)),Vector((.95,.27,.55)),Vector((.93,.44,.31)),Vector((.76,.51,.21)),Vector((.60,.48,.27))]
        a.tube('Thorn whip',path,[.019,.018,.016,.015,.012,.006],GOLD,'HandR')
        for j in range(1,5):
            p=path[j]
            a.horn('Whip rose thorn',p,p+Vector((.035,0,.045)),p+Vector((.048,0,.08)),.015,green,'HandR')
    else:
        # Fitted opaque leaf bodice covers the chest, sides and pelvis.  Surface
        # ornament sits above the .16-deep torso instead of disappearing inside.
        for s in (-1,1):
            a.plate('Overlapping leaf bodice panel',[(s*.012,.195,hip+.14),(s*.158,.199,hip+.39),
                 (s*.23,.184,chest+.10),(s*.18,.19,chest+.22),(s*.028,.205,chest+.035)],.018,green,'Spine')
            curve(a,'Bodice raised golden vein',[(s*.033,.224,hip+.19),(s*.094,.229,hip+.44),(s*.159,.213,chest+.12)],.009,GOLD)
        for s,suffix in ((-1,'L'),(1,'R')):
            for pair in (0,1):
                root=(s*.15,-.18,chest+.03)
                tip=(s*(.88 if pair==0 else .87),-.29,(head+.32 if pair==0 else hip+.10))
                feather(a,'Lily veined leaf wing',root,tip,.22,green,'Wing'+suffix if pair==0 else 'WingTip'+suffix)
                curve(a,'Leaf wing central vein',[root,Vector(root).lerp(Vector(tip),.5)+Vector((0,.028,0)),tip],.010,'#77a866','Wing'+suffix if pair==0 else 'WingTip'+suffix)
            p=a.point('Foot'+suffix);k=a.point('Shin'+suffix);h=a.point('Hand'+suffix)
            for j in range(3):
                feather(a,'Leaf boot upper',p+Vector(((j-1)*.055,0,.02)),k+Vector(((j-1)*.055,.02,-.07-j*.035)),.077,green,'Shin'+suffix)
            a.ball('Lily slipper',p+Vector((0,.10,-.060)),(.103,.225,.087),green,'Foot'+suffix)
            a.flower(tuple(p+Vector((0,.12,.055))),.105,GOLD,'Foot'+suffix,6)
            a.flower(tuple(h+Vector((0,.01,.04))),.145,GOLD,'Hand'+suffix,6)
        for s in (-1,1):
            curve(a,'Lily bodice vine',[(s*.03,.197,hip+.13),(s*.10,.184,hip+.38),(s*.12,.187,chest-.05),(s*.06,.192,chest+.15)],.012,GOLD)
            for j in range(3):a.leaf('Bodice leaf',(s*.10,.20,hip+.26+j*.135),(s*.20,.179,hip+.30+j*.13),.041,green,'Spine')


def bird_warrior(a,hip,chest,head):
    red='#ae513e';orange='#c77a4f';gold='#d9b252';cream='#e7dfc5'
    torso(a,'garudamon',orange,hip,chest)
    limbs(a,'garudamon',orange,red,claws=True)
    a.capsule('Feathered eagle neck',(0,-.01,chest+.16),(0,.015,head-.12),.135,cream,'Head')
    loft(a,'Eagle cranium',[(0,0,head-.17,.11,.12),(0,0,head+.05,.20,.18),(0,-.07,head+.22,.19,.15),(0,-.12,head+.30,.08,.09)],red,'Head')
    a.horn('Hooked eagle beak',(0,.15,head+.025),(0,.33,head-.01),(0,.31,head-.145),.10,gold,'Head')
    for s,suffix in ((-1,'L'),(1,'R')):
        a.plate('Eagle eye brow',[(s*.03,.193,head+.115),(s*.18,.13,head+.14),(s*.163,.17,head+.044),(s*.069,.218,head+.041)],.019,DARK,'Head')
        a.ball('Eagle blue eye',(s*.11,.184,head+.075),(.037,.012,.029),'#51acb3','Head')
        for j in range(5):
            feather(a,'White throat feather',(s*.055,.16,head-.06-j*.047),(s*(.11+j*.016),.21,chest+.02-j*.035),.059,cream,'Head')
        for j in range(5):
            feather(a,'Swept head crest',(s*.04,-.12,head+.20),(s*(.11+j*.041),-.39,head+.42-j*.047),.053,red,'Head')
        # The arm-adjacent wings are broad red fans edged in alternating gold.
        root=Vector((s*.27,-.17,chest+.24));tip=Vector((s*1.35,-.24,chest+.45))
        a.tube('Eagle wing shoulder',[root,root.lerp(tip,.48)+Vector((0,-.03,.25)),tip],[.13,.13,.045],red,'Wing'+suffix)
        for j in range(12):
            u=j/11
            start=root.lerp(tip,.19+.73*u)+Vector((0,-.03,.13*math.sin(math.pi*u)))
            end=start+Vector((s*(.13+.30*u),-.12,-.63-.28*u))
            feather(a,'Long eagle primary',start,end,.091,red,'Wing'+suffix if j<5 else 'WingTip'+suffix)
            feather(a,'Golden flight feather tip',start.lerp(end,.62),end,.074,gold,'Wing'+suffix if j<5 else 'WingTip'+suffix)
        e=a.point('Forearm'+suffix);h=a.point('Hand'+suffix);t=a.point('Thigh'+suffix);k=a.point('Shin'+suffix);f=a.point('Foot'+suffix)
        for p,q,binding in ((e,h,'Forearm'+suffix),(t,k,'Thigh'+suffix)):
            mid=p.lerp(q,.35)
            band(a,'Tribal white limb band',mid,q,.16 if binding.startswith('Thigh') else .122,cream,binding,.082)
            for j in range(3):
                a.plate('Red triangular band marking',[(mid.x+(j-1)*.06-.024,mid.y+.16,mid.z),(mid.x+(j-1)*.06+.024,mid.y+.16,mid.z),(mid.x+(j-1)*.06,mid.y+.168,mid.z-.052)],.006,red,binding)
        shaped_limb(a,'Golden scaled lower leg',k.lerp(f,.35),f,.10,gold,'Shin'+suffix)
        for j in range(6):band(a,'Raised avian shin scale',k.lerp(f,.42+j*.08),f,.099,gold,'Shin'+suffix,.017)
        a.ball('Large avian foot',f+Vector((0,.12,-.025)),(.20,.28,.10),gold,'Foot'+suffix)
        for j in range(3):
            x=f.x+(j-1)*.155
            a.tube('Segmented talon toe',[(x,.11,.145),(x,.28,.13),(x,.40,.095)],[.066,.070,.052],gold,'Foot'+suffix)
            a.horn('Dark hooked eagle talon',(x,.37,.12),(x,.52,.08),(x,.52,.015),.07,'#3c3e46','Foot'+suffix)
        for j in range(4):a.leaf('Upper arm feather',e+Vector((s*.07,-.045,j*.071)),e+Vector((s*.24,-.05,j*.071-.07)),.062,red,'UpperArm'+suffix)
    for j in range(5):
        feather(a,'Warrior tail plume',((j-2)*.085,-.16,hip+.05),((j-2)*.17,-.54,.50),.10,red,'Tail')
    curve(a,'Chest tribal chevron',[(-.23,.235,chest+.18),(0,.259,chest+.03),(.23,.235,chest+.18)],.022,DARK)
    for s in (-1,1):
        for j in range(3):a.plate('Chest tribal triangles',[(s*(.09+j*.048),.264,chest-.05),(s*(.125+j*.047),.264,chest-.05),(s*(.11+j*.047),.27,chest-.095)],.007,DARK)


def devil(a,hip,chest,head):
    black='#282b35';skin='#40434d';scarlet='#a84445'
    torso(a,'devimon',black,hip,chest)
    limbs(a,'devimon',black,black,claws=True)
    face(a,'devimon',head,skin,masked=True)
    a.plate('Demon skeletal cheek mask',[(-.145,.12,head+.105),(-.075,.179,head+.06),(-.052,.193,head-.064),
            (-.117,.147,head-.122),(-.15,.112,head-.038)],.020,'#727778','Head')
    a.plate('Demon other skeletal cheek mask',[(.145,.12,head+.105),(.075,.179,head+.06),(.052,.193,head-.064),
            (.117,.147,head-.122),(.15,.112,head-.038)],.020,'#727778','Head')
    a.plate('Demon narrow nasal ridge',[(-.022,.18,head+.018),(.022,.18,head+.018),(.028,.21,head-.069),
            (0,.228,head-.092),(-.028,.21,head-.069)],.017,'#73797a','Head')
    curve(a,'Demon severe mouth seam',[(-.091,.155,head-.122),(0,.19,head-.155),(.091,.155,head-.122)],.010,'#161a21','Jaw')
    for s in (-1,1):
        a.horn('Demon visible canine',(s*.062,.178,head-.128),(s*.060,.182,head-.158),(s*.055,.185,head-.181),.013,'#bfc9c0','Jaw')
    # Long narrow face and split pointed crown, unlike the generic horned mascot.
    for s in (-1,1):
        a.horn('Devil long swept horn',(s*.115,-.055,head+.12),(s*.21,-.115,head+.43),(s*.30,-.21,head+.67),.10,black,'Head')
        a.plate('Devil white eye',[(s*.035,.194,head+.055),(s*.131,.164,head+.083),(s*.097,.20,head+.021)],.009,'#d2ddd4','Head')
        a.plate('Devil red pupil',[(s*.069,.211,head+.054),(s*.091,.2,head+.059),(s*.085,.213,head+.030)],.006,'#cb4e53','Head')
        curve(a,'Severe facial crease',[(s*.038,.215,head-.065),(s*.105,.175,head-.109),(s*.14,.142,head-.066)],.008,DARK,'Head')
    a.plate('Devil red chest sigil',[(-.27,.218,chest+.12),(-.11,.259,chest+.15),(0,.278,chest+.035),(.11,.259,chest+.15),(.27,.218,chest+.12),(.13,.263,chest-.055),(0,.281,chest+.005),(-.13,.263,chest-.055)],.012,scarlet)
    for s,suffix in ((-1,'L'),(1,'R')):
        # Bat membrane is continuous scalloped surface, with actual missing panels.
        root=Vector((s*.20,-.17,chest+.15));joint=Vector((s*.84,-.24,head+.47));tip=Vector((s*1.52,-.29,chest+.61))
        a.tube('Bat wing leading arm',[root,joint,tip],[.067,.064,.018],black,'Wing'+suffix)
        fingers=[Vector((s*1.43,-.32,hip+.15)),Vector((s*1.16,-.34,hip-.12)),Vector((s*.75,-.32,hip-.22)),Vector((s*.36,-.23,hip+.20))]
        anchors=[tip]+fingers
        for j in range(4):
            p,q=anchors[j],anchors[j+1]
            indent=(p+q)*.5+Vector((-s*.075,.006,.26))
            # Two triangles leave the curved gap between the pointed fingers.
            verts=[joint, p, p.lerp(indent,.45),indent,indent.lerp(q,.58),q]
            mesh(a,'Tattered bat membrane',verts,[(0,1,2),(0,2,3),(0,3,4),(0,4,5)],'#41414d','Wing'+suffix,False)
            a.tube('Bat extended wing finger',[joint,joint.lerp(q,.58),q],[.031,.019,.006],black,'Wing'+suffix)
        e=a.point('Forearm'+suffix);h=a.point('Hand'+suffix);k=a.point('Shin'+suffix);f=a.point('Foot'+suffix)
        for j in range(7):band(a,'Wrapped forearm strap',e.lerp(h,.16+j*.115),h,.087,'#685748','Forearm'+suffix,.033)
        for j in (0,1):
            band(a,'Gothic knee leather strap',k+Vector((0,0,.04-j*.10)),f,.107,'#796a55','Shin'+suffix,.040)
        for j in range(3):
            x=h.x+(j-1)*.075
            a.tube('Elongated demon claw',[(x,h.y+.07,h.z-.09),(x+s*.014,h.y+.11,h.z-.29),(x+s*.025,h.y+.20,h.z-.40)],[.030,.018,.002],black,'Hand'+suffix)
        inset_plate(a,'Demon shin boot',[(f.x-.10,.15,.23),(k.x-.11,.14,k.z-.12),(k.x+.11,.14,k.z-.12),(f.x+.10,.15,.23)],black,'Shin'+suffix,'#656973')
        a.ball('Boot red flame',(f.x,.232,.20),(.041,.015,.065),scarlet,'Foot'+suffix)
    for row in range(4):
        z=hip+.23+row*.10
        curve(a,'Abdominal belt rib',[(-.11,.183,z),(-.06,.222,z-.02),(.06,.222,z-.02),(.11,.183,z)],.015,'#74777b')
    loft(a,'Demon waist belt',[(0,0,hip+.03,.249,.194),(0,0,hip+.115,.244,.190)],'#595448','Hips')
    a.ball('Belt skull',(0,.215,hip+.08),(.050,.026,.058),'#c5c6bb','Hips')
    for s in (-1,1):a.ball('Belt skull socket',(s*.018,.242,hip+.092),(.013,.007,.014),DARK,'Hips')


def monkey(a,hip,chest,head):
    orange='#c88435';cream='#e3cf9c';black='#24282e'
    torso(a,'etemon',orange,hip,chest)
    limbs(a,'etemon',orange,orange)
    a.capsule('Ape neck',(0,-.025,chest+.16),(0,.005,head-.15),.134,orange,'Head')
    loft(a,'Ape expressive cranium',[(0,.03,head-.21,.15,.12),(0,.03,head-.11,.225,.175),
         (0,-.025,head+.095,.233,.19),(0,-.05,head+.22,.145,.14)],orange,'Head')
    for s in (-1,1):
        a.ball('Ape protruding ear',(s*.265,-.008,head+.005),(.097,.048,.131),orange,'Head')
        a.ball('Ear inner cartilage',(s*.273,.031,head+.006),(.060,.018,.091),cream,'Head')
    a.ball('Ape cream muzzle',(0,.177,head-.112),(.172,.078,.117),cream,'Head')
    a.ball('Broad ape nose',(0,.248,head-.078),(.072,.035,.036),'#735137','Head')
    a.ball('Ape wide grin',(0,.253,head-.151),(.122,.017,.059),black,'Jaw')
    for j in range(6):
        a.ball('Individual grin tooth',((j-2.5)*.031,.269,head-.124),(.014,.009,.027),WHITE,'Jaw')
    for s in (-1,1):
        outline=[(s*.018,.229,head+.061),(s*.199,.187,head+.082),(s*.211,.197,head+.009),
                 (s*.158,.229,head-.042),(s*.072,.247,head-.034)]
        a.plate('Sunglasses thick frame',outline,.033,black,'Head')
        center=sum((Vector(v) for v in outline),Vector())/len(outline)
        a.plate('Sunglasses polished lens',[tuple(center+(Vector(v)-center)*.73+Vector((0,.014,0))) for v in outline],.013,'#293c47','Head')
        curve(a,'Sunglass white reflection',[(s*.063,.253,head+.045),(s*.139,.229,head+.048)],.006,'#a4b0ad','Head')
    curve(a,'Sunglass bridge',[(-.051,.255,head+.041),(0,.272,head+.048),(.051,.255,head+.041)],.013,black,'Head')
    a.tube('Swept ape hair tuft',[(0,-.05,head+.16),(0,-.07,head+.33),(.08,-.06,head+.43),(.145,-.03,head+.39)],[.10,.065,.041,.005],orange,'Head')
    a.ball('Cream abdominal suit patch',(0,.184,chest-.09),(.173,.041,.289),cream,'Spine')
    for s in (-1,1):
        curve(a,'Fur side marking',[(s*.20,.164,chest+.03),(s*.235,.15,chest-.10),(s*.16,.17,chest-.21)],.027,black)
    # The zipper and stitch detail read as the original monkey costume.
    curve(a,'Costume side zipper',[(.245,.052,chest+.02),(.258,.08,chest-.19),(.206,.11,hip+.12)],.008,black)
    for j in range(7):curve(a,'Costume cross stitch',[(.224,.101,chest-.01-j*.052),(.274,.083,chest-.025-j*.052)],.008,cream)
    loft(a,'Ape black belt',[(0,0,hip+.035,.263,.211),(0,0,hip+.107,.25,.208)],black,'Hips')
    a.ball('Gold belt ring',(0,.235,hip+.083),(.083,.035,.078),GOLD,'Hips')
    a.ball('Belt ring centre',(0,.268,hip+.083),(.050,.013,.049),black,'Hips')
    curve(a,'Pendant chain',[(-.13,.18,chest+.20),(-.15,.24,chest-.11),(0,.26,chest-.24),(.10,.234,chest-.07),(.12,.19,chest+.18)],.017,GOLD)
    a.ball('Monkey pendant body',(-.085,.267,chest-.33),(.072,.041,.078),GOLD,'Spine')
    a.ball('Monkey pendant head',(-.085,.273,chest-.25),(.063,.043,.057),GOLD,'Spine')
    for s in (-1,1):a.ball('Pendant ear',(-.085+s*.067,.267,chest-.25),(.027,.022,.032),GOLD,'Spine')
    a.tube('Long expressive monkey tail',[(0,-.16,hip),(0,-.42,hip-.08),(.17,-.66,hip+.02),(.38,-.73,hip+.19),(.51,-.68,hip+.20)],[.065,.057,.045,.037,.02],orange,'Tail')
    a.ball('Cream tail tip',(.51,-.68,hip+.20),(.058,.043,.040),cream,'TailTip')
    for suffix in ('L','R'):
        f=a.point('Foot'+suffix)
        for j in range(4):
            x=f.x+(j-1.5)*.055
            a.ball('Ape individual toe',(x,.252,.094),(.044,.094,.044),orange,'Foot'+suffix)
            a.ball('Ape toe nail',(x,.321,.100),(.028,.039,.015),cream,'Foot'+suffix)


def build(a, ident, tint, accent, feature, kind):
    """Build all visible geometry; preserve the roster's required bone names."""
    if ident not in IDS:
        raise ValueError('Humanoid quality module does not own '+ident)
    hip,chest,head=skeleton(a,ident)
    if ident in ('angemon','holyangemon','seraphimon'):
        angels(a,ident,hip,chest,head)
    elif ident=='wargreymon': dragon(a,hip,chest,head)
    elif ident=='weregarurumon': wolf(a,hip,chest,head)
    elif ident in ('lilimon','rosemon'): plant_fairy(a,ident,hip,chest,head)
    elif ident=='garudamon': bird_warrior(a,hip,chest,head)
    elif ident=='devimon': devil(a,hip,chest,head)
    elif ident=='etemon': monkey(a,hip,chest,head)
