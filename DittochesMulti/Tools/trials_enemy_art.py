"""Anatomy and choreography for the eight enemy-only encounter additions."""
from faithful_techniques import TECHNIQUES,row_to_spec
from faithful_motion_styles import GROUPS
from faithful_motion_personality import MOTIFS
ROWS=[
 ('monochromon','뿔 들이받기','horn_claw',1.8,'볼케이노 스트라이크','Volcanic Strike','fire_spit',2.5,'fireball','#ff9a55','mouth','몸을 지탱하고 입에서 화염탄을 쏩니다.'),
 ('ogremon','뼈곤봉 내려치기','staff',1.8,'패왕권','Pummel Whack','holy_punch',2.4,'impact','#b2f77c','rightHand','곤봉 반대쪽 주먹을 크게 내지릅니다.'),
 ('meramon','불꽃 주먹','boxing',1.5,'버닝 피스트','Burning Fist','holy_punch',2.2,'fireball','#ff722a','rightHand','팔의 불길을 모아 불꽃 주먹을 내지릅니다.'),
 ('leomon','야수의 연타','boxing',1.7,'수왕권','Fist of the Beast King','holy_punch',2.5,'holybeam','#ffd176','rightHand','몸통을 돌려 전방으로 주먹의 기운을 날립니다.'),
 ('andromon','기계 손날','metal_claw',1.9,'스파이럴 소드','Spiral Sword','cross_claw',2.6,'claws','#86d5ed','hands','기계 팔을 교차해 에너지 칼날을 쏩니다.'),
 ('skullgreymon','뼈 발톱','horn_claw',1.9,'그라운드 제로','Oblivion Bird / Ground Zero','chest_missiles',2.8,'missiles','#ffc67e','back','척추의 미사일을 발사하며 몸을 지탱합니다.'),
 ('piedmon','광대의 손날','claw',1.7,'트럼프 소드','Trump Sword','dark_spirits',2.8,'swords','#d5a4ff','back','한 손으로 표적을 가리키며 네 검을 차례로 소환합니다.'),
 ('machinedramon','강철 손톱','metal_claw',2.0,'무한 캐논','Infinity Cannon','chest_missiles',3.0,'thunderbeam','#ffa957','back','두 등 포신을 고정하고 강한 포격의 반동을 버팁니다.'),
]
for row in ROWS:
    ident=row[0];spec=row_to_spec(row);spec['checked']='2026-10-13'
    directory={'ogremon':'orgemon','piedmon':'piemon','machinedramon':'mugendramon'}.get(ident,ident)
    spec['source']='https://digimon.net/reference_en/detail.php?directory_name='+directory
    TECHNIQUES[ident]=spec
TECHNIQUES['piedmon']['Skill']['shots']=4
TECHNIQUES['machinedramon']['Skill']['shots']=2
for group,ids in dict(quadruped=['monochromon'],heavy=['ogremon','skullgreymon','machinedramon'],human=['meramon','leomon','andromon','piedmon']).items():GROUPS[group].update(ids)
MOTIFS.update(monochromon='ram',ogremon='staff',meramon='box',leomon='combo',andromon='cross',skullgreymon='cannon',piedmon='magic',machinedramon='cannon')

PROFILES={
 'monochromon':dict(kind='quadruped',hip=(0,.72,.91),chest=(0,-.68,1.06),head=(0,-1.23,1.10),head_top=(0,-1.7,1.2),head_cut_y=-1.06,leg_top=.74,
  front=[(.43,-.74,.73),(.49,-.79,.40),(.52,-.89,.1)],rear=[(.40,.74,.68),(.42,.63,.34),(.47,.83,.09)],tail=[(0,1.13,.97),(0,1.70,1.0),(0,2.20,.95)]),
 'ogremon':dict(kind='human',hip=(.05,.22,.83),chest=(.05,.08,1.47),head=(.06,-.17,1.89),head_top=(.06,-.17,2.22),head_cut=1.84,arm_cut=.48,leg_cut=.86,
  arms_left=[(-.43,.02,1.78),(-.68,-.16,1.45),(-.84,-.64,1.26)],arms_right=[(.49,.02,1.77),(.79,-.12,1.43),(.94,-.51,1.10)],
  legs=[(.35,.18,.86),(.39,.02,.48),(.40,.13,.12)],step_scale=.7),
 'meramon':dict(kind='human',hip=(0,.04,1.16),chest=(0,.02,1.68),head=(0,0,2.01),head_top=(0,0,2.23),head_cut=1.97,arm_cut=.25,leg_cut=1.14,
  arms=[(.29,0,1.83),(.42,-.03,1.5),(.48,-.13,1.20)],legs=[(.18,.03,1.10),(.20,.0,.62),(.21,-.01,.13)]),
 'leomon':dict(kind='human',hip=(-.07,.28,1.06),chest=(-.08,.23,1.61),head=(-.07,.19,1.96),head_top=(-.07,.20,2.23),head_cut=1.96,arm_cut=.34,leg_cut=1.09,
  arms_left=[(-.39,.18,1.8),(-.52,.04,1.39),(-.68,-.09,1.01)],arms_right=[(.25,.19,1.80),(.39,-.10,1.55),(.06,-.35,1.72)],
  legs_left=[(-.28,.27,1.03),(-.34,.23,.6),(-.48,.20,.13)],legs_right=[(.16,.29,1.03),(.22,.27,.6),(.25,.21,.12)],tail=[(-.08,.45,1.15),(-.08,.82,.92),(-.22,1.08,.85)],tail_width=.45),
 'andromon':dict(kind='human',hip=(0,.08,1.19),chest=(0,.05,1.68),head=(0,.03,2.13),head_top=(0,.03,2.35),head_cut=2.04,arm_cut=.28,leg_cut=1.18,
  arms=[(.28,.17,1.87),(.35,.14,1.37),(.48,-.02,.84)],legs=[(.18,.08,1.18),(.20,.07,.65),(.24,.06,.12)],step_scale=.7),
 'skullgreymon':dict(kind='dinosaur',hip=(0,.25,.78),chest=(0,.13,1.32),head=(0,-.40,1.68),head_top=(0,-.48,2.02),head_cut=1.55,arm_cut=.40,arm_top=1.68,leg_cut=.75,
  arms=[(.38,.02,1.42),(.72,-.27,1.05),(.70,-.65,1.05)],legs=[(.36,.32,.76),(.50,.27,.43),(.57,.17,.12)],
  tail=[(0,.67,.71),(0,1.12,.58),(0,1.76,.52)],tail_width=.4),
 'piedmon':dict(kind='human',hip=(0,-.08,1.15),chest=(0,-.15,1.65),head=(0,-.10,2.07),head_top=(0,-.10,2.30),head_cut=1.99,arm_cut=.26,leg_cut=1.15,
  arms=[(.25,-.13,1.83),(.42,-.20,1.41),(.64,-.30,1.13)],legs=[(.16,-.09,1.13),(.18,-.06,.74),(.20,-.10,.15)],cape=True),
 'machinedramon':dict(kind='dinosaur',hip=(0,.18,.78),chest=(0,-.03,1.34),head=(0,-.39,1.48),head_top=(0,-.74,1.61),head_cut=1.39,arm_cut=.28,arm_top=1.76,leg_cut=.78,
  arms=[(.41,-.02,1.52),(.50,-.13,1.20),(.52,-.36,.91)],legs=[(.26,.19,.76),(.29,-.17,.58),(.35,.08,.16)],
  tail=[(0,.42,.96),(0,.98,1.12),(0,1.32,1.40)],tail_width=.25,step_scale=.65),
}
