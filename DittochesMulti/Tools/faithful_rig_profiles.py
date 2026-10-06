"""Anatomical joint placements measured on canonical, 2.4-unit source meshes.

Coordinates are X right, -Y forward, Z up. These rigs bind the original mesh;
they do not construct replacement body geometry.
"""
PROFILES={
 'agumon':dict(kind='biped',hip=(0,.22,.72),chest=(0,.08,1.28),head=(0,-.03,1.65),head_top=(0,-.20,2.15),
   head_cut=1.49,arm_cut=.37,leg_cut=.76,
   arms=[(.32,.10,1.35),(.50,.25,1.03),(.67,.14,.64)],hand_tip=(.72,.07,.37),
   legs=[(.27,.23,.69),(.34,.13,.40),(.39,.07,.15)],
   tail=[(0,.48,.78),(0,.72,.87),(0,.95,.86)],jaw=(0,-.02,1.67),jaw_tip=(0,-.70,1.59)),
 'wargreymon':dict(kind='human',hip=(0,.02,1.04),chest=(0,.01,1.56),head=(0,-.015,1.95),head_top=(0,-.05,2.2),
   head_cut=1.93,arm_cut=.35,leg_cut=1.06,
   arms=[(.40,.00,1.78),(.59,-.005,1.48),(.78,-.02,1.10)],
   legs=[(.24,.04,.98),(.27,.03,.56),(.28,.03,.17)],shield=True),
 'holyangemon':dict(kind='angel',hip=(0,0,1.12),chest=(0,0,1.69),head=(0,0,2.12),head_top=(0,0,2.35),
   head_cut=2.08,arm_cut=.28,leg_cut=1.10,
   arms=[(.27,.03,1.95),(.65,.03,1.95),(1.00,.03,1.95)],
   legs=[(.12,0,1.09),(.12,.02,.59),(.12,-.03,.14)],wing_root=(.06,.15,1.67),wing_tip=(.06,1.5,1.8),front_wings=True),
 'seraphimon':dict(kind='angel',hip=(0,-.01,.92),chest=(0,-.01,1.39),head=(0,-.02,1.71),head_top=(0,-.03,1.92),
   head_cut=1.69,arm_cut=.29,leg_cut=.94,
   arms=[(.27,-.01,1.53),(.60,-.01,1.53),(.85,-.01,1.50)],
   legs=[(.11,0,.89),(.12,0,.46),(.12,-.04,.12)],wing_root=(.17,.12,1.45),wing_tip=(.7,.15,2.00)),
 'greymon':dict(kind='dinosaur',hip=(0,.20,.75),chest=(-.16,-.28,1.30),head=(-.27,-.52,1.87),head_top=(-.28,-.56,2.10),
   head_cut=1.78,arm_cut=.44,leg_cut=.92,
   arms_left=[(-.46,-.32,1.46),(-.72,-.17,1.17),(-.80,-.31,.91)],hand_tip_left=(-.87,-.30,.70),
   arms_right=[(.21,-.38,1.54),(.49,-.62,1.43),(.64,-.83,1.60)],hand_tip_right=(.74,-.97,1.60),
   legs_left=[(-.39,.18,.83),(-.59,.25,.53),(-.70,.07,.13)],
   legs_right=[(.39,-.15,.83),(.57,-.53,.54),(.72,-.72,.13)],
   tail=[(.18,.56,.91),(.53,1.0,1.15),(.83,1.42,1.31)],jaw=(-.28,-.39,1.83),jaw_tip=(-.28,-.73,1.50)),
 'garurumon':dict(kind='quadruped',hip=(0,.54,1.18),chest=(0,-.82,1.19),head=(0,-1.44,1.46),head_top=(0,-1.82,1.53),
   head_cut_y=-1.34,leg_top=1.03,
   front=[(.40,-.96,1.15),(.46,-.93,.60),(.52,-1.17,.12)],
   rear=[(.35,.73,1.21),(.42,.39,.76),(.45,.72,.11)],
   tail=[(0,1.03,1.30),(0,1.60,1.45),(0,2.10,1.40)]),
 'metalgarurumon':dict(kind='quadruped',hip=(0,.64,1.10),chest=(0,-.66,1.16),head=(0,-1.10,1.28),head_top=(0,-1.46,1.30),
   head_cut_y=-1.07,leg_top=1.08,
   front=[(.41,-.63,1.13),(.43,-.52,.65),(.44,-.69,.18)],
   rear=[(.38,.65,1.14),(.41,.52,.73),(.44,.76,.20)],
   tail=[(0,.98,1.36),(0,1.34,1.51),(0,1.66,1.66)],wing_root=(.25,-.61,1.47),wing_tip=(.95,-.48,2.13)),
 'kabuterimon':dict(kind='insect',hip=(0,.04,.75),chest=(0,-.02,1.22),head=(0,-.1,1.65),head_top=(0,-.14,2.1),
   head_cut=1.64,arm_cut=.39,leg_cut=.86,
   arms=[(.36,-.09,1.43),(.68,-.19,1.28),(.91,-.35,1.13)],
   extra_arms=[(.34,.02,1.17),(.56,-.08,.97),(.68,-.23,.75)],
   legs=[(.30,.10,.69),(.51,-.12,.39),(.48,.05,.11)],wing_root=(.22,.24,1.37),wing_tip=(.95,.45,2.16)),
 'atlur':dict(kind='insect',hip=(0,.10,.75),chest=(0,.03,1.24),head=(0,-.12,1.62),head_top=(0,-.09,2.02),
   head_cut=1.62,arm_cut=.41,leg_cut=.82,
   arms=[(.35,-.12,1.50),(.71,-.14,1.55),(.97,-.24,1.77)],
   extra_arms=[(.37,.10,1.29),(.68,-.13,.99),(.81,-.29,.70)],
   legs=[(.27,.1,.74),(.42,-.1,.42),(.42,.09,.14)],wing_root=(.28,.40,1.18),wing_tip=(.54,.47,1.42)),
 'koromon':dict(kind='baby',hip=(0,0,.20),chest=(0,0,.70),head=(0,-.1,.94),head_top=(0,-.1,1.23),
   ears=[(.44,.07,1.31),(.74,.07,1.93),(1.20,.07,2.09)]),
 'tsunomon':dict(kind='baby',hip=(0,0,.19),chest=(0,0,.70),head=(0,-.05,.91),head_top=(0,0,1.32)),
 'pyocomon':dict(kind='baby',hip=(0,0,.12),chest=(0,0,.62),head=(0,0,.81),head_top=(0,0,1.20),
   flower=[(0,0,1.14),(0,0,1.59),(0,0,2.1)]),
 'mochimon':dict(kind='baby',hip=(0,0,.23),chest=(0,0,1.03),head=(0,-.02,1.50),head_top=(0,0,2.1),
   arms=[(.67,0,1.21),(.81,-.01,.90),(.81,-.02,.55)]),
}
from faithful_expanded_profiles import EXPANDED
PROFILES.update(EXPANDED)


def skeleton(profile):
    p=profile;defs=[]
    def add(n,parent,h,t):defs.append((n,parent,tuple(h),tuple(t)))
    add('Root',None,(0,0,0),(0,0,.2))
    add('Hips','Root',p['hip'],p['chest'])
    add('Spine','Hips',p['chest'],p['head'])
    add('Head','Spine',p['head'],p['head_top'])
    if 'jaw' in p:add('Jaw','Head',p['jaw'],p['jaw_tip'])
    for side,suffix,label in [(-1,'L','left'),(1,'R','right')]:
        def mirrored(v):return (side*v[0],v[1],v[2])
        for key,names,parent in [('arms',['UpperArm','Forearm','Hand'],'Spine'),
                                 ('extra_arms',['LowerArm','LowerForearm','LowerHand'],'Spine'),
                                 ('legs',['Thigh','Shin','Foot'],'Hips'),
                                 ('front',['FrontUpper','FrontLower','FrontFoot'],'Spine'),
                                 ('rear',['RearUpper','RearLower','RearFoot'],'Hips')]:
            points=p.get(key+'_'+label)
            if points is None and key in p:points=[mirrored(v) for v in p[key]]
            if points is None:continue
            if key=='arms' and p['kind']=='baby':parent='Head'
            for i,n in enumerate(names):
                end=points[i+1] if i<2 else (points[i][0],points[i][1]-.23,points[i][2]-.035)
                if n=='Hand':
                    if 'hand_tip_'+label in p:end=p['hand_tip_'+label]
                    elif 'hand_tip' in p:end=mirrored(p['hand_tip'])
                add(n+suffix,parent if i==0 else names[i-1]+suffix,points[i],end)
        if 'wing_root' in p:add('Wing'+suffix,'Spine',mirrored(p['wing_root']),mirrored(p['wing_tip']))
        if 'extra_wings' in p:add('LowerWing'+suffix,'Spine',mirrored(p['extra_wings'][0]),mirrored(p['extra_wings'][1]))
        if p.get('front_wings'):
            add('FrontUpperWing'+suffix,'Spine',mirrored((.14,.10,1.83)),mirrored((.35,-.15,2.15)))
            add('FrontLowerWing'+suffix,'Spine',mirrored((.14,.08,1.55)),mirrored((.36,-.15,1.20)))
        if 'ears' in p:
            points=[mirrored(v) for v in p['ears']]
            add('Ear'+suffix,'Head',points[0],points[1]);add('EarTip'+suffix,'Ear'+suffix,points[1],points[2])
    if 'tail' in p:
        pts=p['tail'];add('Tail','Hips',pts[0],pts[1]);add('TailTip','Tail',pts[1],pts[2])
    if 'flower' in p:
        pts=p['flower'];add('Flower','Head',pts[0],pts[1]);add('Petals','Flower',pts[1],pts[2])
    return defs
