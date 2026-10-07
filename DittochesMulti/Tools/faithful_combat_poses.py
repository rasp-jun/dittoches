"""Additional combat poses layered onto each inspected rig's neutral stance."""
import math
from mathutils import Quaternion,Vector

def combat_pose(ident,rig,p,mode,t,duration,neutral,rotate,translate,envelope):
    neutral(ident,rig,p,'Idle',0,2.8)
    u=t/duration;kind=p['kind'];baby=kind=='baby'
    pulse=envelope(u,[(0,0),(.22,1),(.68,1),(1,0)])
    if mode=='Skill':
        charge=envelope(u,[(0,0),(.28,1),(.43,1),(.61,0),(1,0)])
        blast=envelope(u,[(0,0),(.40,0),(.57,1),(.68,.85),(1,0)])
        rotate(rig,'Spine',x=-charge*5+blast*9,y=blast*8)
        rotate(rig,'Head',x=-charge*6+blast*7)
        translate(rig,'Hips',(0,charge*.025-blast*.09,-charge*(.035 if not baby else .045)))
        if baby:
            rig.pose.bones['Hips'].scale=(1+charge*.025,1+charge*.025,1-charge*.04)
        for side,s in [(-1,'L'),(1,'R')]:
            # Continue from the current resting arms, preserving relaxed angel poses.
            add_rotation(rig,'UpperArm'+s,(-charge*8-blast*24,side*charge*10,side*blast*7))
            add_rotation(rig,'Forearm'+s,(-charge*18+blast*8,0,0))
            add_rotation(rig,'Wing'+s,(0,side*(charge*5+blast*10),0))
            add_rotation(rig,'LowerArm'+s,(-blast*18,0,side*charge*7))
            add_rotation(rig,'Ear'+s,(charge*8-blast*12,0,side*blast*5))
        add_rotation(rig,'Jaw',(blast*15,0,0))
        add_rotation(rig,'Tail',(charge*4-blast*7,0,0))
        add_rotation(rig,'Flower',(-charge*6+blast*8,0,0))
    elif mode=='Guard':
        # These connected shoulder shells need a shallower defensive fold.
        pulse*=.50 if ident in ('gabumon','garudamon') else 1
        rotate(rig,'Spine',x=pulse*7)
        rotate(rig,'Head',x=pulse*7)
        translate(rig,'Hips',(0,pulse*.03,-pulse*.025))
        for side,s in [(-1,'L'),(1,'R')]:
            add_rotation(rig,'UpperArm'+s,(-pulse*26,side*pulse*7,-side*pulse*10))
            add_rotation(rig,'Forearm'+s,(-pulse*27,0,0))
            add_rotation(rig,'LowerArm'+s,(-pulse*16,0,0))
            add_rotation(rig,'Wing'+s,(0,-side*pulse*6,0))
            add_rotation(rig,'Ear'+s,(pulse*8,0,-side*pulse*5))
        if baby:rig.pose.bones['Hips'].scale=(1+pulse*.02,1+pulse*.02,1-pulse*.04)
    elif mode=='Dodge':
        slide=math.sin(math.pi*u)**2
        translate(rig,'Root',(slide*.28,slide*.09,math.sin(math.pi*u)*.035))
        rotate(rig,'Spine',z=slide*-8,x=slide*-4)
        rotate(rig,'Head',z=slide*3)
        for side,s in [(-1,'L'),(1,'R')]:
            add_rotation(rig,'UpperArm'+s,(slide*8,0,-side*slide*8))
            add_rotation(rig,'Wing'+s,(0,side*slide*5,0))
        add_rotation(rig,'Tail',(0,0,slide*10))
    elif mode=='Down':
        collapse=envelope(u,[(0,0),(.18,.08),(.55,.9),(.78,1),(1,1)])
        # Rotate the whole articulated pose around a body support point. The
        # evaluated surface is subsequently grounded by the baker.
        rig.pose.bones['Root'].rotation_mode='QUATERNION'
        rotate(rig,'Root',x=collapse*(38 if kind=='quadruped' else 62 if baby else 76),z=collapse*8)
        translate(rig,'Root',(0,collapse*.12,-collapse*.40))
        add_rotation(rig,'Head',(collapse*8,0,0))
        for side,s in [(-1,'L'),(1,'R')]:
            add_rotation(rig,'UpperArm'+s,(-collapse*12,side*collapse*10,0))
            add_rotation(rig,'Forearm'+s,(-collapse*15,0,0))
            add_rotation(rig,'Wing'+s,(0,-side*collapse*5,0))

def add_rotation(rig,name,angles):
    from mathutils import Euler
    if name not in rig.pose.bones:return
    bone=rig.pose.bones[name];basis=bone.bone.matrix_local.to_quaternion()
    delta=basis.inverted()@Euler(tuple(math.radians(v) for v in angles),'XYZ').to_quaternion()@basis
    bone.rotation_quaternion=delta@bone.rotation_quaternion
