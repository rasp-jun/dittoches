"""Weight, contact, anticipation and follow-through on the preserved character rigs."""
import math
import numpy as np
from mathutils import Vector,Euler
from faithful_pose_math import Pose
from faithful_motion_styles import style_for,curve,smooth01
from faithful_motion_personality import phrase,arm_delta,pulse


def setup(ident,rig,profile,neutral):
    if ident=='greymon':Pose(rig).reset()
    else:neutral(ident,rig,profile,'Idle',0,2.8)
    context=Pose(rig)
    feet={n:context.point(n) for n in context.bones.keys() if 'Foot' in n}
    hands={n:context.point(n) for n in context.bones.keys() if 'Hand' in n}
    profile['_natural']={'pose':context,'base':context.capture(),'feet':feet,'hands':hands,
        'orientations':{n:context.world(n).to_quaternion() for n in feet|hands},'style':style_for(ident)}


def gait(u,stride,lift,stance):
    u%=1
    if u<stance:return -stride+2*stride*u/stance,0,0
    v=(u-stance)/(1-stance);tangent=2*stride*(1-stance)/stance
    forward=(2*v**3-3*v*v+1)*stride+(v**3-2*v*v+v)*tangent+(-2*v**3+3*v*v)*-stride+(v**3-v*v)*tangent
    return forward,lift*math.sin(math.pi*v)**2,v


def pose(ident,rig,p,mode,t,duration):
    data=p['_natural'];c=data['pose'];c.reset(data['base']);style=data['style']
    u=max(0,min(1,t/duration));phase=math.tau*u;kind=p['kind'];baby=kind=='baby';quad=kind=='quadruped'
    flying=kind=='bird';walk=mode in ('Walk','Run');run=mode=='Run';feet=data['feet'];hands=data['hands']
    wave=math.sin(phase);double=math.cos(phase*2)
    acting=phrase(ident,mode,u);motif=acting['motif'];glance=acting['glance']
    # Non-cyclic actions enter and recover to the same neutral pose, including
    # jaw, wings and accessory joints. No idle breathing is baked into the end.
    breath=math.sin(phase*2) if mode=='Idle' else wave*.35 if walk else 0
    guard=curve(u,[(0,0),(.25,1),(.70,1),(1,0)]) if mode=='Guard' else 0
    victory=curve(u,[(0,0),(.27,1),(.67,1),(1,0)]) if mode=='Victory' else 0
    hit=curve(u,[(0,0),(.16,1),(.35,.35),(.58,-.08),(.85,0),(1,0)]) if mode=='Hit' else 0
    wind,strike=acting['wind'],acting['strike'];second=acting['second']
    attack_scale=1.15 if mode=='Skill' else 1
    impact=(strike+second*.8)*attack_scale
    dodge=curve(u,[(0,0),(.10,0),(.38,1),(.62,1),(.98,0),(1,0)]) if mode=='Dodge' else 0
    crouch=curve(u,[(0,0),(.24,1),(.52,1),(.82,.45),(1,.45)]) if mode=='Down' else 0
    fall=curve(u,[(0,0),(.25,0),(.75,1),(.87,.975),(1,1)]) if mode=='Down' else 0
    # Release contact as balance is lost, before a large torso rotation can
    # pull a planted ankle away from its knee. Short legs crouch less deeply.
    release=curve(u,[(0,0),(.22,0),(.46,1),(1,1)]) if mode=='Down' else 0
    leg_height=min((abs(c.point('Hips').z-v.z) for v in feet.values()),default=1)
    crouch_depth=min(.12,max(.025,leg_height*.10))

    lean=breath*1.2-wind*7+impact*(13 if kind=='dinosaur' else 9)+guard*7-hit*13-victory*5
    if mode=='Skill' and motif in ('magic','flower','spore'):lean=-wind*7+impact*4
    c.move('Hips',((wave*style['sway'] if walk else breath*.004),wind*.022-impact*.045+hit*.035,
                   breath*.012-(style['bob']*(1-double) if walk else 0)-wind*.055-guard*.045-crouch*crouch_depth))
    c.rotate('Hips',z=wave*(3.8 if walk else .4)+acting['twist']*.4,y=glance*1.8)
    c.rotate('Spine',x=lean+(7 if run and not quad else 0),z=-wave*(4.5 if walk else .2)-dodge*8+acting['twist']*.7)
    c.rotate('Neck',x=-wind*5+impact*4-victory*7,z=glance*4-acting['twist']*.12)
    c.rotate('Head',x=-lean*.38+hit*5-victory*7,y=breath*.7,z=wave*(1.2 if walk else .2)+dodge*4+glance*(5 if ident=='greymon' else 9)-acting['twist']*.4)
    c.rotate('Jaw',x=impact*(23 if kind=='dinosaur' else 12)+victory*(20 if kind=='dinosaur' else 0))
    if ident=='greymon' and mode=='Skill':
        c.rotate('Neck',x=-wind*10-impact*6);c.rotate('Head',x=-wind*4-impact*5)
    if ident=='wargreymon' and mode=='Skill':
        c.rotate('Spine',x=-wind*5);c.rotate('Head',x=-wind*9)
    if mode=='Idle':
        c.move('Hips',(glance*.015,0,0));c.rotate('Spine',z=-glance*2)
    if baby:
        hop=max(0,math.sin(phase))**2*(.27 if run else .16) if walk else 0
        celebration=max(0,math.sin(phase*3))**2*.22*victory
        c.move('Hips',(0,-impact*.16,hop+celebration+impact*(.28 if mode=='Skill' else .19)))
        c.rotate('Hips',x=-wind*7+impact*12,y=glance*4)
        squash=(.055*math.sin(phase*2) if walk else 0)-wind*.10-guard*.065
        c.scale('Hips',(1-squash*.35,1-squash*.35,1+squash))
    if flying and walk:
        c.move('Root',(0,0,.18+.045*wave));c.rotate('Spine',x=5*wave)
    if flying and mode in ('Attack','Skill','Victory'):
        c.move('Root',(0,-impact*.16,wind*.07+impact*.16+victory*.15))
    if quad and mode in ('Attack','Skill'):
        c.move('Root',(0,-impact*.16,impact*.055));c.rotate('Spine',x=wind*-5+impact*6)
    if mode=='Dodge':
        c.move('Root',(.30*dodge,0,(.12*math.sin(math.pi*u)**2 if baby or flying else -.025*dodge)))
        if flying:c.rotate('Spine',z=-dodge*10)
    if mode=='Down':
        c.rotate('Root',x=fall*(12 if quad else 14 if baby else 42),y=fall*(68 if quad else -62 if baby else 48),z=fall*8)
        c.move('Root',(0,.055*fall,-.12*fall))
        c.rotate('Head',x=6*crouch-3*fall)

    delayed=lambda offset:math.sin(phase-offset)+math.sin(offset)
    for side,s in [(-1,'L'),(1,'R')]:
        swing=side*wave*(27 if run else 17 if walk else 1.5)
        # Pendulum motion comes from the shoulder, followed by a smaller elbow
        # response, rather than rotating every limb by the same sine wave.
        c.rotate('UpperArm'+s,x=swing+wind*5-impact*3,z=side*breath*.4)
        c.rotate('Forearm'+s,x=-abs(wave)*(11 if run else 6) if walk else 0)
        c.rotate('LowerArm'+s,x=-swing*.6-wind*3+impact*2)
        flap=(35 if run else 24 if walk else 5)*min(1.35,p.get('wing_flap',1)) if flying else (5 if walk else 1.8)
        c.rotate('Wing'+s,y=-side*(wave*flap+impact*12+victory*18-guard*8),x=-fall*12)
        c.rotate('LowerWing'+s,y=-side*(delayed(.4)*flap*.6+victory*4))
        c.rotate('FrontUpperWing'+s,y=-side*(delayed(.2)*.8+victory*3))
        c.rotate('FrontLowerWing'+s,y=-side*(delayed(.4)*.6+victory*2))
        c.rotate('Ear'+s,x=delayed(.30)*(9 if walk else 3)+wind*8-impact*14,z=side*(victory*8-guard*6)+glance*(3 if side<0 else 6))
        c.rotate('EarTip'+s,x=delayed(.55)*(12 if walk else 4)-impact*9)

        # Aim hands in the current parent's frame. This handles native Rosemon
        # bones and the lowered angel T-pose without changing their skin weights.
        for upper,lower,hand,secondary in [('UpperArm','Forearm','Hand',False),('LowerArm','LowerForearm','LowerHand',True)]:
            name=hand+s
            if name not in hands:continue
            rest=hands[name];current=c.point(name);amount=1 if not secondary else .55
            reach=style['reach']*amount
            if p.get('staff') or p.get('microphone'):reach*=.55
            delta=arm_delta(acting,mode,side,secondary,reach)
            delta+=Vector((-side*guard*.085,-guard*.15,guard*(.34 if kind in ('human','angel') else .16)*amount))
            salute=1 if side<0 or kind not in ('human','angel') else .35
            delta+=Vector((side*victory*.14,-victory*.09,victory*(.62*salute if kind in ('human','angel') else .24)*amount))
            delta+=Vector((side*hit*.04,hit*.075,hit*.08))
            intensity=max(wind,strike,second,guard,victory,abs(hit))
            if intensity>1e-6:
                goal=current.lerp(rest+delta,min(1,intensity))
                # Preserve the model's elbow plane, including asymmetrical
                # shoulder armour, instead of snapping to a generic pole.
                pole=c.bend(upper+s,lower+s,name,(side,.25,-.6))
                orientation=data['orientations'][name] if (p.get('staff') or p.get('microphone')) and side<0 else None
                if ident=='rosemon' and mode in ('Attack','Skill','Guard','Hit'):
                    wrist=Euler((math.radians(-wind*10+strike*14),0,math.radians(-wind*10+strike*15)),'XYZ').to_quaternion()
                    orientation=wrist@data['orientations'][name]
                c.ik(upper+s,lower+s,name,goal,pole,orientation)
            if mode=='Down':c.rotate(upper+s,x=-crouch*12+fall*5)

    follow=curve(max(0,u-.055),[(0,0),(.29,0),(.43,1),(.52,.8),(.72,.15),(.91,0),(1,0)]) if mode=='Attack' else impact
    endmask=1-smooth01((u-.86)/.14) if not walk and mode!='Idle' else 1
    c.rotate('Tail',z=delayed(.30)*(7 if walk else 2)-follow*10*endmask-acting['twist']*.6,x=-wind*5+impact*5-fall*25)
    c.rotate('TailTip',z=delayed(.60)*(10 if walk else 3)-follow*14*endmask,x=-fall*12)
    c.rotate('Flower',x=delayed(.25)*(5 if walk else 1.8)-wind*9+impact*12,z=impact*10)
    c.rotate('Petals',x=delayed(.50)*(7 if walk else 2.5)-wind*6+impact*16,z=impact*18)
    # Suppress ambient secondary waves in one-shots; their endpoints are neutral.
    if mode not in ('Idle','Walk','Run'):
        for name in ['Tail','TailTip','Flower','Petals']:
            # Endpoint zeroing is handled by periodic waves and recovery mask.
            if u==0 or u==1 and mode!='Down':
                loc,rotation,scale=data['base'][name] if name in data['base'] else (None,None,None)
                if rotation is not None:c.bones[name].rotation_quaternion=rotation;c.cache.clear()
    if ident=='rosemon':
        for index in range(1,7):
            trailing=pulse(max(0,u-index*.016),.25,.43,.48,.88) if mode in ('Attack','Skill') else 0
            c.rotate('ValveBiped.Bip01_L_Whip'+str(index),z=trailing*(5 if index<4 else 7),x=delayed(index*.18)*.65)
        for index,name in enumerate(['Hair0','Hair01','Hair02']):c.rotate(name,x=delayed(index*.2)*(.9 if walk else .3)-impact*(index+1)*.5)
        for s in ['L','R']:
            c.rotate('Cape0_'+s,x=delayed(.25)*.4+impact*1.2)
            c.rotate('Cape1_'+s,x=delayed(.50)*.6+impact*2)

    # Keep supporting feet in world space as the pelvis transfers weight. The
    # stepping foot follows a continuous swing arc with independent toe roll.
    for name,anchor in feet.items():
        side=-1 if name.endswith('L') else 1;s=name[-1]
        if name.startswith('Front'):upper,lower,pole='FrontUpper'+s,'FrontLower'+s,(0,1,0)
        elif name.startswith('Rear'):upper,lower,pole='RearUpper'+s,'RearLower'+s,(0,-1,0)
        else:upper,lower,pole='Thigh'+s,'Shin'+s,(side*.12,-1,0)
        goal=anchor.copy();pitch=0
        if walk and not flying:
            if quad:
                offset=({'FrontL':0,'RearR':.25,'FrontR':.5,'RearL':.75} if not run else {'RearL':0,'RearR':.09,'FrontL':.48,'FrontR':.57})[name.replace('Foot','')]
            else:offset=.5 if side>0 else 0
            phasefoot=(u+offset)%1;stance=.64 if not run else .50 if not quad else .43
            forward,lift,swingphase=gait(phasefoot,style['stride']*(1.45 if run else 1)*p.get('step_scale',1),style['lift']*(1.25 if run else 1)*p.get('step_scale',1),stance)
            goal.y+=forward;goal.z+=lift
            if not quad:
                pitch=curve(phasefoot,[(0,-3),(.15,0),(stance-.13,0),(stance,7),(.82,-6),(1,-3)])
        if mode=='Dodge' and not flying:
            a,b=(.08,.38) if side>0 else (.23,.53)
            ra,rb=(.68,1) if side>0 else (.53,.85)
            shift=smooth01((u-a)/(b-a))-smooth01((u-ra)/(rb-ra))
            goal.x+=.30*shift
            for start,stop in [(a,b),(ra,rb)]:
                if start<u<stop:goal.z+=.065*math.sin(math.pi*(u-start)/(stop-start))**2
        if mode in ('Attack','Skill') and not flying:
            if quad:
                front=name.startswith('Front');goal.y-=impact*(.22 if front else .08)
                goal.z+=impact*(.12 if front else .025)
            elif side<0:
                goal.y-=acting['step']*.13
                goal.z+=pulse(u,.10,.23,.26,.37)*.085+pulse(u,.76,.87,.90,.99)*.055
        if mode=='Victory' and quad:
            goal.z+=victory*.14 if name.startswith('Front') else 0
        if mode=='Down':goal=goal.lerp(c.point(name),release)
        if flying and walk:goal=c.point(name)
        orientation=Euler((math.radians(pitch),0,0),'XYZ').to_quaternion()@data['orientations'][name]
        if release:
            orientation=orientation.slerp(c.world(name).to_quaternion(),release)
        pole=c.bend(upper,lower,name,pole)
        if pitch and name in p.get('_foot_surface',{}):
            delta=orientation@rig.data.bones[name].matrix_local.to_quaternion().inverted()
            low=float((p['_foot_surface'][name]@np.array(delta.to_matrix()).T)[:,2].min())+goal.z
            if low<0:goal.z-=low
        c.ik(upper,lower,name,goal,pole,orientation)
