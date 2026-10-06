"""Technique-directed skeletal choreography in canonical X/-Y/Z coordinates."""
import math
from mathutils import Vector,Euler
from faithful_pose_math import Pose
from faithful_motion_styles import curve,smooth01
from faithful_motion_personality import pulse
from faithful_techniques import TECHNIQUES

def setup(rig,profile):
    c=Pose(rig)
    data=dict(c=c,base=c.capture(),points={b.name:c.point(b.name) for b in rig.pose.bones},
              orientations={b.name:c.world(b.name).to_quaternion() for b in rig.pose.bones})
    data['feet']=[n for n in data['points'] if 'Foot' in n]
    profile['_technique']=data

def pose(ident,rig,p,mode,u):
    d=p['_technique'];c=d['c'];c.reset(d['base']);points=d['points'];kind=p['kind']
    if u<=0 or u>=1:return
    skill=mode=='Skill';spec=TECHNIQUES[ident][mode];pattern=spec['pattern'];baby=kind=='baby';quad=kind=='quadruped'
    wind=pulse(u,.025,.31 if skill else .23,.39 if skill else .28,.61 if skill else .47)
    hit=pulse(u,.40 if skill else .28,.57 if skill else .43,.68 if skill else .49,.94 if skill else .88)
    hold=pulse(u,.05,.29,.70,.96)
    recoil=pulse(u,.57 if skill else .43,.67 if skill else .52,.72 if skill else .57,.94 if skill else .91)
    secondary=pulse(max(0,u-.035),.35,.53,.62,.94)
    left=hit;right=hit*.25;step=0;travel=0;hop=0;twist=0
    goals={};orientations={};foot_goals={};bend_override={}
    def rot(name,**kw):c.rotate(name,**kw)
    def move(name,v):c.move(name,v)
    def hand(s,offset,second=False):
        name=('LowerHand' if second else 'Hand')+s
        if name in points:goals[name]=points[name]+Vector(offset)
    def target(s,pos):
        name='Hand'+s
        if name in points:goals[name]=points[name].lerp(Vector(pos),hold)
    def wings(amount,phase=0):
        for side,s in [(-1,'L'),(1,'R')]:
            rot('Wing'+s,y=-side*amount)
            rot('LowerWing'+s,y=-side*(amount*.7+phase))
            rot('FrontUpperWing'+s,y=-side*amount*.2)
            rot('FrontLowerWing'+s,y=-side*amount*.16)
    move('Hips',(0,wind*.035-hit*.035,-wind*.055-recoil*.025))
    rot('Spine',x=-wind*7+hit*8)
    rot('Head',x=wind*3-hit*3)

    if pattern in ('claw','dragon_claw','metal_claw','talon','petal_slap','vine_slap','insect_rake','four_arm','sword','whip','staff'):
        left=pulse(u,.27,.42,.47,.78);right=pulse(u,.45,.60,.65,.91) if pattern in ('dragon_claw','four_arm','insect_rake') else 0
        dominant='R' if pattern in ('metal_claw','whip') else 'L'
        twist=-wind*15+left*21-right*16
        if dominant=='R':twist*=-1
        step=pulse(u,.1,.29,.64,.98)
        for side,s in [(-1,'L'),(1,'R')]:
            amount=left if s==dominant else right
            hand(s,(side*(wind*.12-amount*.20),wind*.08-amount*.32,wind*.28+amount*.29))
            if pattern in ('four_arm','insect_rake'):hand(s,(side*.03*amount,-.21*secondary,.12*secondary),True)
        if pattern=='staff':
            hand('L',(-.06*wind,.035*wind-.24*left,.08*wind+.12*left))
            # Keep the long staff from sweeping through the floor.
            orientations['HandL']=Euler((math.radians(-left*12),0,math.radians(wind*12-left*16)),'XYZ').to_quaternion()@d['orientations']['HandL']
        if pattern=='sword':hand('L',(-.15*wind+.28*left,-.20*left,.40*wind+.26*left))
        if pattern=='whip':hand('R',(.08*wind-.12*left,.06*wind-.21*left,.22*wind+.16*left))
        if pattern=='talon':hop=.08*hit;wings(wind*7-hit*5)
    elif pattern=='boxing' or pattern=='needle_combo':
        hits=[pulse(u,a,a+.085,a+.115,a+.22) for a in ([.25,.40,.55,.70] if skill else [.26,.48])]
        left=sum(hits[::2]);right=sum(hits[1::2]);twist=left*14-right*14-wind*5
        for s,a in [('L',left),('R',right)]:
            side=-1 if s=='L' else 1
            hand(s,(-side*.08*hold,-.08*hold-.27*a,.16*hold+.07*a))
        step=pulse(u,.1,.29,.73,.98);hit=max(hits);recoil=hit*.6
        if skill:rot('Spine',x=hold*6);rot('Head',x=-hold*3)
    elif pattern in ('bump','horn_bump','root_bump','flower_bump','squash_bump','bite_lunge','peck','wolf_pounce','horn_claw','horn_charge','shell_slam'):
        strength=1.25 if pattern in ('bite_lunge','horn_charge') else 1
        if baby:
            wind=pulse(u,.025,.25,.32,.63)
            hit=pulse(u,.28,.52,.61,.98)
        travel=hit*(.25 if baby else .16)*strength
        hop=hit*(.11 if baby else .07 if pattern=='wolf_pounce' else .025)
        rot('Hips',x=-wind*7+hit*(7 if baby else 4))
        rot('Spine',x=wind*-5+hit*(6 if pattern in ('horn_charge','horn_claw') else 5))
        rot('Head',x=-wind*9+hit*(10 if pattern in ('peck','bite_lunge') else 5))
        rot('Neck',x=-wind*5+hit*8);rot('Jaw',x=wind*12+hit*22)
        if baby:c.scale('Hips',(1+wind*.045,1+wind*.06,1-wind*.10))
        if pattern=='squash_bump':c.scale('Hips',(1+wind*.08,1+hit*.12,1-wind*.14))
        if pattern=='flower_bump':rot('Flower',x=-wind*8+secondary*11);rot('Petals',x=-wind*5+secondary*14)
        if pattern=='wolf_pounce':
            for n in d['feet']:
                if n.startswith('Front'):foot_goals[n]=points[n]+Vector((0,-hit*.28,hit*.17))
        elif not baby:step=pulse(u,.12,.35,.68,.97)
        if pattern=='shell_slam':
            for s in ('L','R'):hand(s,(0,-hit*.16,wind*.08+hit*.10))
        if pattern in ('horn_claw','horn_charge'):
            for side,s in [(-1,'L'),(1,'R')]:hand(s,(side*wind*.06,-hit*.13,wind*.09-hit*.02))
    elif pattern=='ear_slap':
        rot('EarL',x=-wind*14+hit*27,z=wind*12-hit*17)
        rot('EarR',x=-wind*12+hit*24,z=-wind*12+hit*17)
        rot('EarTipL',x=-wind*8+secondary*16);rot('EarTipR',x=-wind*7+secondary*14)
        travel=.1*hit;hop=.08*hit
    elif pattern in ('bubbles','elastic_bubbles','root_bubbles','flower_bubbles','fire_spit','blue_spit','spiral_flame','air_shot','mega_flame','blue_breath','ice_breath','hydro_pressure'):
        breath=pattern in ('mega_flame','blue_breath','ice_breath','hydro_pressure')
        bursts=sum(pulse(u,a,a+.04,a+.065,a+.12) for a in (.49,.60,.71)) if 'bubbles' in pattern else recoil
        rot('Spine',x=-wind*(9 if breath else 5)+hit*(13 if breath else 5)-bursts*3)
        rot('Neck',x=-wind*7+hit*3-bursts*2)
        rot('Head',x=-wind*(7 if baby else 4)+hit*(7 if breath else 3)-bursts*3)
        rot('Jaw',x=wind*9+hit*(29 if ident=='greymon' else 10 if ident=='agumon' else 22)-bursts*5)
        move('Hips',(0,bursts*.045,-hit*.035 if not baby else 0))
        if baby:
            inflate=wind*(.12 if pattern=='air_shot' else .065)
            c.scale('Hips',(1+inflate,1+inflate,1+inflate*.50-bursts*.04))
            rot('Hips',x=-wind*5+bursts*7)
        if pattern=='air_shot':
            for side,s in [(-1,'L'),(1,'R')]:rot('Ear'+s,x=-wind*14+hit*23,z=-side*wind*8)
        if pattern=='flower_bubbles':rot('Flower',x=-wind*6+bursts*7);rot('Petals',x=-wind*4+secondary*9)
        if pattern=='root_bubbles':
            for side,s in [(-1,'L'),(1,'R')]:rot('Ear'+s,x=wind*8-bursts*6,z=side*wind*4)
        if pattern=='spiral_flame':
            for side,s in [(-1,'L'),(1,'R')]:hand(s,(side*.12*hold,-.08*hold,.22*wind+.15*hit))
            twist=wind*9-hit*8
        elif not quad and not baby:
            for side,s in [(-1,'L'),(1,'R')]:hand(s,(side*wind*.055,wind*.09-hit*.045,wind*.055+hit*.015))
    elif pattern in ('electric_orb','giga_blaster','wing_electric'):
        wings(wind*(16 if pattern=='wing_electric' else 10)-recoil*5)
        rot('Head',x=-wind*7+hit*9);rot('Spine',x=-wind*5+hit*10-recoil*6)
        for side,s in [(-1,'L'),(1,'R')]:
            hand(s,(side*.09*wind,-hit*.12,wind*.10+hit*.08))
            hand(s,(side*.035*hold,-hit*.065,.065*wind),True)
        if pattern=='wing_electric':hop=.04*hold
    elif pattern in ('holy_punch','cross_claw','death_claw','vine_bind'):
        dominant='R' if pattern=='holy_punch' else 'L'
        if pattern=='cross_claw':
            left=pulse(u,.37,.52,.57,.77);right=pulse(u,.52,.67,.72,.95);twist=-wind*16+left*24-right*22
        else:left=hit;right=hit;twist=(-wind*14+hit*17) if pattern=='holy_punch' else -wind*6+hit*6
        for side,s in [(-1,'L'),(1,'R')]:
            a=left if s=='L' else right
            if pattern=='holy_punch' and s=='L':
                hand(s,(0,wind*.02,wind*.03));orientations['HandL']=d['orientations']['HandL'];continue
            hand(s,(-side*(wind*.12+a*.18),wind*.04-a*(.40 if pattern=='death_claw' else .33),wind*.34+a*(.48 if pattern=='holy_punch' else .37)))
        if pattern=='vine_bind':
            for s in ('L','R'):rot('Hand'+s,x=-hit*12)
        if pattern=='death_claw':
            # The visible extended dark hands follow the wrists; the original
            # organic mesh remains within its anatomically bound reach.
            wings(wind*8+hit*9)
        step=pulse(u,.14,.38,.75,.99)
    elif pattern=='chest_missiles':
        rot('Spine',x=-wind*11-recoil*9);rot('Head',x=wind*8+recoil*6)
        for side,s in [(-1,'L'),(1,'R')]:hand(s,(side*.13*hold,wind*.08,wind*.15+hit*.08))
        move('Hips',(0,recoil*.07,-wind*.03));wings(wind*5)
    elif pattern in ('flower_cannon','gaia_force','seven_heavens','light_orb','dark_spirits','serenade','heavens_gate'):
        head=points['Head'];spine=points['Spine'];rot('Spine',x=-wind*5-recoil*5)
        if pattern=='flower_cannon':
            for side,s in [(-1,'L'),(1,'R')]:target(s,(side*.11,spine.y-.48,spine.z+.035))
            move('Hips',(0,recoil*.06,0));wings(wind*9-hit*5)
        elif pattern=='gaia_force':
            for side,s in [(-1,'L'),(1,'R')]:
                rest=points['Hand'+s];up=Vector((side*.30,spine.y-.18,head.z+.38));forward=Vector((side*.30,spine.y-.58,spine.z+.24))
                goals['Hand'+s]=rest+(up-rest)*wind+(forward-rest)*hit
            rot('Head',x=-wind*12+hit*6);rot('Spine',x=-wind*7+hit*15);step=pulse(u,.20,.45,.72,.98)
        elif pattern=='seven_heavens':
            for side,s in [(-1,'L'),(1,'R')]:target(s,(side*.61,spine.y-.31,spine.z+.24))
            wings(wind*9+hit*6);hop=.09*hold;rot('Head',x=-wind*5+hit*3)
        elif pattern in ('light_orb','dark_spirits'):
            cast='R' if ident=='etemon' else 'L';side=1 if cast=='R' else -1
            hand(cast,(-side*.16*wind-side*.04*hit,.01*wind-.31*hit,wind*.47+hit*.42))
            if ident=='etemon':hand('L',(0,0,.06*hold));orientations['HandL']=d['orientations']['HandL']
            else:hand('R',(.10*hold,-.08*hold,.23*hold))
            twist=-side*wind*12+side*hit*17
        elif pattern=='serenade':
            hand('L',(.10*hold,-.055*hold,.23*hold));orientations['HandL']=d['orientations']['HandL']
            hand('R',(.13*hit,-.12*hit,.38*hit));twist=math.sin(math.tau*u*2)*hold*8
            rot('Head',x=-hold*7,z=math.sin(math.tau*u*2)*hold*6)
        elif pattern=='heavens_gate':
            angle=math.tau*smooth01((u-.20)/.47)
            hand('L',(.25*math.sin(angle)*hold,-.22*hold,(.47+.21*math.cos(angle))*hold))
            hand('R',(.12*hold,-.06*hold,.28*hold));wings(wind*5+hit*9)
            twist=-wind*7+hit*9;rot('Head',x=-wind*5)
    elif pattern=='electric_whip':
        twist=wind*13-hit*20;hand('R',(.13*wind-.22*hit,.05*wind-.23*hit,.31*wind+.22*hit))
        hand('L',(-.10*hold,-.05*hold,.10*hold));step=pulse(u,.10,.34,.72,.99)
    elif pattern in ('four_wing','shadow_wing','starlight'):
        flap=wind*25-hit*30 if pattern!='starlight' else wind*30-hit*18
        wings(flap,secondary*10);hop=(.18 if skill else .1)*hold
        rot('Spine',x=-wind*10+hit*16);rot('Head',x=wind*4-hit*5)
        if ident=='garudamon':
            for side,s in [(-1,'L'),(1,'R')]:hand(s,(side*wind*.05,-hit*.10,wind*.08+hit*.10))
    else:raise ValueError((ident,mode,'unhandled technique',pattern))

    rot('Hips',z=twist*.36);rot('Spine',z=twist*.64);rot('Head',z=-twist*.43)
    move('Root',(0,-travel,hop))
    # Delayed return in the tail/flower is connected to the strike, not a
    # continuous sine wave shared with every other action.
    rot('Tail',z=-twist*.40-secondary*7,x=-wind*4+recoil*6)
    rot('TailTip',z=-secondary*11,x=recoil*5)
    if ident=='rosemon':
        for s in ('L','R'):
            if 'Hand'+s in points:orientations['Hand'+s]=Euler((math.radians(-wind*6+hit*10),0,math.radians(-wind*6+hit*10)),'XYZ').to_quaternion()@d['orientations']['Hand'+s]
        for index in range(1,7):
            trailing=pulse(max(0,u-index*.018),.29,.49,.56,.89)
            rot('ValveBiped.Bip01_L_Whip'+str(index),z=trailing*(6 if skill else 4),x=trailing*2)
        for s in ('L','R'):rot('Cape0_'+s,x=recoil*1.5);rot('Cape1_'+s,x=secondary*2)
    for name,goal in goals.items():
        if ident=='herakle' and mode=='Attack':goal=points[name].lerp(goal,.52)
        if ident=='etemon' and mode=='Skill':goal=points[name].lerp(goal,.58)
        s=name[-1];extra=name.startswith('LowerHand');upper=('LowerArm' if extra else 'UpperArm')+s;lower=('LowerForearm' if extra else 'Forearm')+s
        if upper not in points or lower not in points:continue
        c.ik(upper,lower,name,goal,c.bend(upper,lower,name,(-1 if s=='L' else 1,.3,-1)),orientations.get(name))
    for name in d['feet']:
        s=name[-1];side=-1 if s=='L' else 1
        if name.startswith('Front'):upper,lower='FrontUpper'+s,'FrontLower'+s
        elif name.startswith('Rear'):upper,lower='RearUpper'+s,'RearLower'+s
        else:upper,lower='Thigh'+s,'Shin'+s
        goal=foot_goals.get(name,points[name]).copy()
        if not quad and s=='L' and step:
            goal.y-=step*.16
            goal.z+=pulse(u,.08,.22,.25,.37)*.075+pulse(u,.76,.88,.90,.99)*.045
        if hop:goal.z+=hop
        if travel:goal.y-=travel*.72
        c.ik(upper,lower,name,goal,c.bend(upper,lower,name,(side*.12,-1,0)),d['orientations'][name])
