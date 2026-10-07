"""Contact-aware Kuwagamon motion and smoothly recovered Birdramon actions."""
import math,copy
import numpy as np
from faithful_motion_styles import curve,smooth01,style_for


def pose(m,ident,name,u,base,height):
    root=m.names['_rootJoint'];wave=math.sin(math.tau*u)
    if ident=='birdramon':
        # The source attack/win clips contain very fast wing reversals. Use
        # smooth flight key poses for the common actions, keeping the original
        # source clips available separately and unchanged.
        if name in ('Idle','Walk','Run'):
            state=m.sample('idle' if name=='Idle' else 'move',u)
        else:
            state=copy.deepcopy(base)
        wind=curve(u,[(0,0),(.25,1),(.46,0),(1,0)]) if name in ('Attack','Skill') else 0
        strike=curve(u,[(0,0),(.28,0),(.48,1),(.64,.65),(.94,0),(1,0)]) if name=='Attack' else curve(u,[(0,0),(.40,0),(.62,1),(.76,.7),(1,0)]) if name=='Skill' else 0
        guard=curve(u,[(0,0),(.30,1),(.68,1),(1,0)]) if name=='Guard' else 0
        hit=curve(u,[(0,0),(.23,1),(.48,.35),(1,0)]) if name=='Hit' else 0
        victory=curve(u,[(0,0),(.3,1),(.65,1),(1,0)]) if name=='Victory' else 0
        if name in ('Attack','Skill'):
            state=m.blend(base,m.sample('idle',.34 if name=='Attack' else .66),strike*.55)
        elif guard:state=m.blend(base,m.sample('guard',.43),guard*.40)
        elif name=='Victory':state=m.sample('idle',u)
        def bird_rot(node,degrees,axis=(1,0,0)):m.rotate(state,m.names.get(node),degrees,axis)
        bird_rot('J_body_06',-wind*5+strike*12+guard*3-hit*10)
        bird_rot('J_kubiA_019',wind*3-strike*6+hit*4-victory*4)
        bird_rot('J_head_021',-strike*3+hit*3)
        bird_rot('J_ago_022',strike*(18 if name=='Skill' else 12)+victory*5)
        bird_rot('J_tailA_036',wind*4-strike*5+hit*3)
        m.move(state,root,np.array([0,wind*.025+victory*.07-hit*.012,strike*.075-wind*.025-hit*.04])*height)
        bank=curve(u,[(0,0),(.12,0),(.40,1),(.65,1),(1,0)]) if name=='Dodge' else 0
        if bank:
            m.move(state,root,np.array([.15*bank,.025*bank,-.02*bank])*height)
            m.rotate(state,root,-bank*14,(0,0,1))
        fall=curve(u,[(0,0),(.25,0),(.78,1),(.90,.98),(1,1)]) if name=='Down' else 0
        if name=='Down':
            fold=curve(u,[(0,0),(.30,.52),(.75,.52),(1,.52)])
            state=m.blend(base,m.sample('guard',.43),fold)
            m.rotate(state,root,fall*74,(1,0,0));m.rotate(state,root,fall*9,(0,0,1))
            m.move(state,root,np.array([0,-.56*fall,.04*fall])*height)
        # Give the flight cycle enough clearance that wingtips never make the
        # automatic floor correction lift the entire bird on a single frame.
        m.move(state,root,np.array([0,.14*(1-fall),0])*height)
        return state

    if not hasattr(m,'natural_base_world'):m.natural_base_world=m.world(base)
    rest=m.natural_base_world;state=copy.deepcopy(base);names=m.names;style=style_for(ident)
    def rot(node,degrees,axis=(1,0,0)):m.rotate(state,names.get(node),degrees,axis)
    walk=name in ('Walk','Run');run=name=='Run'
    wind=curve(u,[(0,0),(.27,1),(.44,0),(1,0)]) if name in ('Attack','Skill') else 0
    strike=curve(u,[(0,0),(.28,0),(.46,1),(.58,.8),(.85,0),(1,0)]) if name=='Attack' else curve(u,[(0,0),(.42,0),(.61,1),(.73,.7),(1,0)]) if name=='Skill' else 0
    guard=curve(u,[(0,0),(.25,1),(.70,1),(1,0)]) if name=='Guard' else 0
    hit=curve(u,[(0,0),(.16,1),(.40,.2),(.62,-.05),(1,0)]) if name=='Hit' else 0
    victory=curve(u,[(0,0),(.3,1),(.68,1),(1,0)]) if name=='Victory' else 0
    dodge=curve(u,[(0,0),(.10,0),(.38,1),(.62,1),(1,0)]) if name=='Dodge' else 0
    crouch=curve(u,[(0,0),(.25,1),(.55,1),(1,.4)]) if name=='Down' else 0
    fall=curve(u,[(0,0),(.28,0),(.79,1),(.89,.98),(1,1)]) if name=='Down' else 0
    release=curve(u,[(0,0),(.22,0),(.46,1),(1,1)]) if name=='Down' else 0
    breath=wave if name=='Idle' else wave*.35 if walk else 0
    rot('SPINE2_02',breath*1.5-wind*5+strike*9+guard*6-hit*10)
    rot('NECK1_06',-breath*.7+wind*4-strike*5+hit*4-victory*7)
    if name=='Idle':rot('NECK1_06',math.sin(math.tau*u)*7,(0,1,0))
    m.move(state,root,np.array([wave*.007 if walk else breath*.0015,
        breath*.002-(.006*(1-math.cos(math.tau*u*2)) if walk else 0)-crouch*.045,
        strike*.025-wind*.012-hit*.02])*height)
    if dodge:m.move(state,root,np.array([.08*dodge,0,0])*height);rot('_rootJoint',-dodge*4,(0,0,1))
    if name=='Down':
        rot('_rootJoint',fall*64);rot('_rootJoint',fall*5,(0,0,1));m.move(state,root,np.array([0,-.15*fall,.03*fall])*height)
    for side,label in [(1,'L'),(-1,'R')]:
        for upper,lower,hand,rear in [
            (label+'FSHOULDER_'+('023' if label=='L' else '068'),'joint27_'+('024' if label=='L' else '069'),'joint28_'+('025' if label=='L' else '070'),False),
            (label+'BSHOULDER_'+('038' if label=='L' else '053'),'joint27_'+('039' if label=='L' else '054'),'joint28_'+('040' if label=='L' else '055'),True)]:
            amplitude=(18 if run else 11 if walk else 2)*side*wave*(-.6 if rear else 1)
            rot(upper,amplitude)
            lead=.55 if rear else 1 if side>0 or name=='Skill' else .35
            offset=np.array([-side*guard*.018,
                strike*.06*lead+guard*(.09 if rear else .15)+victory*(.10 if rear else .20),
                strike*.11*lead-wind*.035+guard*.06])*height
            amount=max(wind,strike,guard,victory)
            if amount:
                current=m.world(state)[names[hand]][:3,3];target=current*(1-amount)+(rest[names[hand]][:3,3]+offset)*amount
                m.ik(state,names[upper],names[lower],names[hand],target,m.bend(state,names[upper],names[lower],names[hand],(side,.15,-.4)))
            if name=='Down':rot(upper,crouch*8-fall*3)
        for node in ([('joint38_015',1),('joint36_017',1)] if side>0 else [('joint90_019',-1),('joint93_021',-1)]):
            rot(node[0],node[1]*(breath*.8+strike*3+victory*4),(0,0,1))

        upper=names[label+'HIP_'+('083' if label=='L' else '098')]
        lower=names['joint18_'+('084' if label=='L' else '099')];foot=names['joint19_'+('085' if label=='L' else '0100')]
        target=rest[foot][:3,3].copy()
        if walk:
            phase=(u+(0 if side>0 else .5))%1;stance=.63 if not run else .51
            stride=height*(.041 if not run else .062);lift=height*(.024 if not run else .032)
            if phase<stance:forward=-stride+2*stride*phase/stance;up=0
            else:
                v=(phase-stance)/(1-stance);tangent=2*stride*(1-stance)/stance
                forward=(2*v**3-3*v*v+1)*stride+(v**3-2*v*v+v)*tangent+(-2*v**3+3*v*v)*-stride+(v**3-v*v)*tangent
                up=lift*math.sin(math.pi*v)**2
            target+=np.array([0,up,-forward])
        if name=='Dodge':
            a,b=(.08,.38) if side>0 else (.23,.53);ra,rb=(.68,1) if side>0 else (.53,.85)
            target[0]+=height*.08*(smooth01((u-a)/(b-a))-smooth01((u-ra)/(rb-ra)))
            for start,stop in [(a,b),(ra,rb)]:
                if start<u<stop:target[1]+=height*.025*math.sin(math.pi*(u-start)/(stop-start))**2
        if release:target=target*(1-release)+m.world(state)[foot][:3,3]*release
        m.ik(state,upper,lower,foot,target,m.bend(state,upper,lower,foot,(side*.35,0,1)))
        m.orient(state,foot,rest[foot][:3,:3],1-release)
    return state
