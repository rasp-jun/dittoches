"""Specific aerial and pincer actions on the two preserved native skeletons."""
import copy,math
import numpy as np
from faithful_motion_styles import curve
from faithful_motion_personality import pulse

def pose(m,ident,name,u,base,height):
    if u<=0 or u>=1:return copy.deepcopy(base)
    skill=name=='Skill';root=m.names['_rootJoint']
    wind=pulse(u,.03,.31 if skill else .23,.39 if skill else .28,.61 if skill else .47)
    hit=pulse(u,.40 if skill else .28,.57 if skill else .43,.68 if skill else .49,.94 if skill else .88)
    hold=pulse(u,.04,.28,.73,.98)
    if ident=='birdramon':
        # Reuse articulated source wing poses; all entry/recovery frames remain
        # the last accepted Idle pose, including its flight clearance.
        up=m.sample('idle',.66);down=m.sample('idle',.34)
        for flight in (up,down):m.move(flight,root,np.array([0,.14,0])*height)
        state=m.blend(base,up,wind*(.86 if skill else .40))
        state=m.blend(state,down,hit*(.86 if skill else .40))
        def rot(node,angle,axis=(1,0,0)):m.rotate(state,m.names.get(node),angle,axis)
        rot('J_body_06',-wind*8+hit*(19 if not skill else 13))
        rot('J_kubiA_019',wind*5-hit*9);rot('J_head_021',-hit*5)
        rot('J_ago_022',hit*15);rot('J_tailA_036',wind*7-hit*10)
        m.move(state,root,np.array([0,wind*.08-hit*(.06 if not skill else 0),hit*(.17 if not skill else .04)])*height)
        return state
    state=copy.deepcopy(base);names=m.names;rest=m.world(base)
    def rot(node,angle,axis=(1,0,0)):m.rotate(state,names.get(node),angle,axis)
    rot('SPINE2_02',-wind*7+hit*13);rot('NECK1_06',wind*5-hit*7)
    rot('joint8_07',-wind*6+hit*7)
    if skill:rot('joint14_012',wind*9-hit*3)
    m.move(state,root,np.array([0,-wind*.017,hit*(.065 if skill else .025)])*height)
    for side,label in [(1,'L'),(-1,'R')]:
        for upper,lower,hand,rear in [
            (label+'FSHOULDER_'+('023' if label=='L' else '068'),'joint27_'+('024' if label=='L' else '069'),'joint28_'+('025' if label=='L' else '070'),False),
            (label+'BSHOULDER_'+('038' if label=='L' else '053'),'joint27_'+('039' if label=='L' else '054'),'joint28_'+('040' if label=='L' else '055'),True)]:
            strike=hit if skill else pulse(u,.27 if side>0 else .46,.43 if side>0 else .61,.48 if side>0 else .66,.81 if side>0 else .94)
            amount=.55 if rear else 1
            offset=np.array([side*(wind*.03-strike*.032),wind*.035+strike*.058,strike*.095-wind*.02])*height*amount
            target=rest[names[hand]][:3,3]+offset
            m.ik(state,names[upper],names[lower],names[hand],target,m.bend(state,names[upper],names[lower],names[hand],(side,.15,-.4)))
        upper=names[label+'HIP_'+('083' if label=='L' else '098')];lower=names['joint18_'+('084' if label=='L' else '099')];foot=names['joint19_'+('085' if label=='L' else '0100')]
        target=rest[foot][:3,3].copy();target[2]+=hit*height*.012
        m.ik(state,upper,lower,foot,target,m.bend(state,upper,lower,foot,(side*.1,.15,1)),rest[foot][:3,:3])
    return state
