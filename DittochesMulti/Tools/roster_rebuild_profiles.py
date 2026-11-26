"""Joint landmarks for the replacement source meshes, X/-Y/Z, height 2.4."""
from copy import deepcopy
from faithful_rig_profiles import PROFILES as EARLIER
PROFILES={k:deepcopy(v) for k,v in EARLIER.items()}
PROFILES.update(
 agumon=dict(kind='biped',hip=(0,.25,.64),chest=(0,.08,1.18),head=(0,-.12,1.65),head_top=(0,-.20,2.15),
  head_cut=1.41,arm_cut=.34,arm_top=1.42,leg_cut=.67,base_arm=0,base_fore=0,step_scale=.7,
  arms=[(.34,.02,1.27),(.52,-.10,.94),(.68,-.24,.67)],hand_tip=(.72,-.43,.48),
  legs=[(.26,.27,.60),(.30,.23,.35),(.34,.10,.13)],tail=[(0,.58,.71),(0,.79,.80),(0,.96,.86)]),
 greymon=dict(kind='dinosaur',hip=(0,.17,.70),chest=(0,-.14,1.22),head=(0,-.34,1.73),head_top=(0,-.38,2.13),
  head_cut=1.50,arm_cut=.34,arm_top=1.50,leg_cut=.80,base_arm=0,base_fore=0,step_scale=.7,
  arms=[(.31,-.18,1.26),(.53,-.34,1.09),(.65,-.48,.90)],hand_tip=(.69,-.65,.82),
  legs=[(.27,.12,.70),(.32,-.05,.42),(.36,-.12,.13)],
  tail=[(0,.43,.74),(.27,.94,.74),(.65,1.28,.97)],tail_width=.8),
 holyangemon=dict(kind='human',hip=(0,.02,.80),chest=(0,.01,1.24),head=(0,0,1.64),head_top=(0,0,1.84),
  head_cut=1.53,arm_cut=.20,arm_top=1.58,leg_cut=.80,base_arm=0,base_fore=0,step_scale=.75,
  arms=[(.18,-.01,1.41),(.30,-.18,1.18),(.35,-.39,1.23)],
  legs=[(.11,.02,.78),(.12,.01,.42),(.13,-.06,.12)],
  wing_root=(.15,.15,1.45),wing_tip=(.99,.26,2.02),wing_y=.12,wing_z=.88,wing_x=.26,
  extra_wings=[(.15,.15,1.33),(.97,.26,1.28)],wing_flap=.8),
 mochimon=dict(kind='baby',hip=(0,0,.22),chest=(0,0,.91),head=(0,-.03,1.52),head_top=(0,0,2.12),
  arms=[(.64,0,1.11),(.99,0,1.11),(1.32,0,1.11)],arm_cut=.64,base_arm=0,base_fore=0,arm_lower=40),
 birdramon=dict(kind='bird',hip=(0,.0,.53),chest=(0,-.08,1.02),head=(0,-.17,1.51),head_top=(0,-.20,1.94),
  head_cut=1.43,arm_cut=.3,arm_top=1.5,leg_cut=.55,
  legs=[(.20,.01,.51),(.22,-.04,.29),(.23,-.13,.10)],
  wing_root=(.28,.0,1.29),wing_tip=(1.27,.03,1.58),wing_y=-.8,wing_z=.65,wing_x=.34,
  tail=[(0,.26,.79),(.0,.83,.73),(.0,1.23,.94)],tail_width=.4,wing_flap=1.6),
 kuwagamon=dict(kind='insect',hip=(0,.20,.66),chest=(0,.0,1.14),head=(0,-.25,1.68),head_top=(0,-.26,2.06),
  head_cut=1.65,arm_cut=.35,arm_top=1.7,leg_cut=.7,
  arms=[(.34,-.07,1.40),(.61,-.23,1.20),(.79,-.44,1.12)],
  extra_arms=[(.32,.13,1.12),(.53,-.11,.92),(.66,-.33,.70)],
  legs=[(.27,.21,.63),(.38,.15,.36),(.40,-.03,.10)],
  wing_root=(.26,.40,1.51),wing_tip=(.63,.94,.72),wing_y=.34,wing_z=.61,wing_x=.18))
# These source meshes already have their neutral stance and open wings.
for ident in ('wargreymon','kabuterimon','atlur','metalgarurumon'):
 PROFILES[ident].update(base_arm=0,base_fore=0)
PROFILES['koromon'].update(hip=(0,0,.22),chest=(0,0,.80),head=(0,-.15,1.04),head_top=(0,-.10,1.7),
 ears=[(.48,.05,1.75),(.85,.08,2.15),(1.35,.09,1.95)],ear_cut=1.73)
PROFILES['tsunomon'].update(head=(0,-.04,.71),head_top=(0,0,1.0))
PROFILES['pyocomon'].update(head=(0,-.01,.79),head_top=(0,0,1.30),
 flower=[(0,0,1.30),(0,0,1.68),(0,0,2.18)],flower_cut=1.30)
