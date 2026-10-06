"""Reviewed technique choices and local choreography, separate from game balance.

Names/descriptions follow the official encyclopedia. Attack labels without an
official named move describe a locally choreographed use of the character's
body or weapon; they are not asserted to be canonical named techniques.
"""
from roster_designs import OFFICIAL
REVISION='Technique-specific Attack and Skill / 2026-10-02'
# id, basic label/pattern/seconds, skill label/English alias/pattern/seconds,
# effect/colour/emitter, concise interpretation of the reference description.
ROWS=[
 ('koromon','몸통 박치기','bump',1.3,'거품 공격','Foam','bubbles',1.9,'bubbles','#ffb8d6','mouth','몸을 움츠린 뒤 입에서 거품을 연속으로 내뿜습니다.'),
 ('tsunomon','뿔 들이받기','horn_bump',1.35,'산성 거품','Acid Bubbles','bubbles',2.0,'bubbles','#bde8ff','mouth','뿔을 세우고 몸을 뒤로 젖혔다가 산성 거품을 뿜습니다.'),
 ('mochimon','탄성 몸통치기','squash_bump',1.5,'탄성 거품','Elastic Bubbles','elastic_bubbles',2.2,'bubbles','#e6b7ff','mouth','몸을 부풀렸다 수축하며 점착성 거품을 내보냅니다.'),
 ('tanemon','뿌리 박치기','root_bump',1.4,'접착 거품','Adhesive Bubble Blow','root_bubbles',2.1,'bubbles','#b8ef9d','mouth','뿌리를 디디고 잎을 흔든 뒤 거품을 내뿜습니다.'),
 ('pyocomon','꽃 흔들기','flower_bump',1.6,'비누꽃','Soap Flower','flower_bubbles',2.3,'bubbles','#ffa8ca','mouth','큰 꽃이 뒤따라 흔들리며 비눗방울 같은 거품을 뿜습니다.'),
 ('tokomon','짧은 몸통치기','bump',1.3,'물어뜯기','Bite','bite_lunge',1.7,'bite','#fff1bf','mouth','몸을 낮췄다가 앞으로 뛰어들어 깨물고 되돌아옵니다.'),
 ('agumon','발톱 베기','claw',1.4,'베이비 플레임','Pepper Breath / Baby Flame','fire_spit',2.0,'fireball','#ff8732','mouth','입에 불꽃을 모아 한 발을 뱉고 턱과 몸통이 뒤로 반동합니다.'),
 ('gabumon','발톱 휘두르기','claw',1.45,'파란 불꽃','Blue Blaster / Petit Fire','blue_spit',2.1,'fireball','#4c9cff','mouth','고개를 고정하고 입에서 푸른 불꽃을 뱉습니다.'),
 ('tentomon','앞발 연타','insect_rake',1.55,'쁘띠 썬더','Super Shocker / Petit Thunder','wing_electric',2.2,'lightning','#88e7ff','wings','등의 날개에 정전기를 모아 전방으로 방전합니다.'),
 ('palmon','덩굴 후려치기','vine_slap',1.6,'포이즌 아이비','Poison Ivy','vine_bind',2.3,'vines','#80db6e','hands','양손을 앞으로 뻗어 독성 덩굴로 상대를 휘감습니다.'),
 ('piyomon','부리 쪼기','peck',1.35,'매지컬 파이어','Spiral Twister / Magical Fire','spiral_flame',2.15,'spiral','#73e7a0','mouth','날개로 균형을 잡으며 나선으로 흐르는 불꽃을 내보냅니다.'),
 ('patamon','윙 슬랩','ear_slap',1.5,'에어 샷','Air Shot','air_shot',2.2,'air','#dbf5ff','mouth','깊이 들이마셔 몸을 부풀린 뒤 압축 공기를 내뿜습니다.'),
 ('togemon','좌우 펀치','boxing',1.65,'치쿠치쿠 뱅뱅','Needle Spray / Chikuchiku Bang Bang','needle_combo',2.5,'needles','#cff38a','hands','팔끝의 가시를 강화하고 좌우 주먹으로 연속 타격합니다.'),
 ('garurumon','송곳니 덮치기','wolf_pounce',1.7,'폭스 파이어','Fox Fire','blue_breath',2.4,'breath','#519aff','mouth','앞발을 넓게 딛고 고개를 낮춰 푸른 화염을 길게 뿜습니다.'),
 ('greymon','뿔과 발톱 밀어치기','horn_claw',1.7,'메가 플레임','Mega Flame','mega_flame',2.6,'breath','#ff742e','mouth','가슴과 목을 뒤로 당겨 숨을 모은 뒤 정면으로 강한 화염을 뿜습니다.'),
 ('kabuterimon','네 팔 내려치기','four_arm',1.7,'메가 블래스터','Electro Shocker / Mega Blaster','electric_orb',2.5,'electricorb','#73beff','head','네 팔을 벌려 몸을 고정하고 전방으로 전기 구체를 발사합니다.'),
 ('angemon','홀리 로드 타격','staff',1.65,'헤븐즈 너클',"Heaven's Knuckle",'holy_punch',2.4,'holybeam','#ffe6a0','rightHand','지팡이를 지지한 채 반대 주먹에 빛을 모아 앞으로 내지릅니다.'),
 ('birdramon','발톱 급강하','bird_dive',1.8,'메테오 윙','Meteor Wing','meteor_wings',2.8,'meteors','#ff9438','wings','양 날개를 크게 들어 올린 뒤 내려쳐 불타는 깃털을 유성처럼 날립니다.'),
 ('metalgreymon','트라이던트 암','metal_claw',1.85,'기가 디스트로이어','Giga Destroyer','chest_missiles',2.7,'missiles','#ffb55b','chest','양팔을 벌려 가슴의 발사구를 드러내고 미사일 두 발을 쏩니다.'),
 ('weregarurumon','권투 연타','boxing',1.6,'카이저 네일','Wolf Claw / Kaiser Nail','cross_claw',2.2,'claws','#f2bf83','hands','팔을 뒤로 당겼다가 날카로운 발톱으로 좌우 교차 베기를 합니다.'),
 ('lilimon','꽃잎 손날','petal_slap',1.5,'플라워 캐논','Flower Cannon','flower_cannon',2.6,'flower','#ff94d0','hands','두 손목을 앞에서 모아 꽃 모양 포구를 만들고 에너지탄을 발사합니다.'),
 ('holyangemon','엑스칼리버 베기','sword',1.8,'헤븐즈 게이트',"Heaven's Gate",'heavens_gate',3.0,'gate','#d6baff','sword','검을 들어 앞에 원을 그린 뒤 아공간의 문을 엽니다.'),
 ('atlur','앞다리와 뿔 타격','horn_claw',1.8,'혼 버스터','Horn Buster','horn_charge',2.4,'horn','#ffe277','head','몸을 낮추고 큰 뿔을 상대에게 겨눈 뒤 강하게 돌진합니다.'),
 ('garudamon','갈고리 손톱 베기','talon',1.7,'섀도 윙','Shadow Wing','shadow_wing',2.6,'shadowbird','#697da6','wings','날개를 펼쳤다가 쓸어내리며 새 그림자를 남기는 바람의 칼날을 날립니다.'),
 ('herakle','집게와 네 팔 타격','four_arm',1.9,'기가 블래스터','Giga Blaster','giga_blaster',2.9,'thunderbeam','#a7ddff','head','네 팔로 자세를 잡고 강한 전기 에너지를 집중해 발사합니다.'),
 ('hououmon','황금 날개 쓸기','four_wing',1.9,'스타라이트 익스플로전','Starlight Explosion','starlight',3.1,'starlight','#ffe195','wings','네 날개를 차례로 펼쳐 올리고 황금빛 입자를 아래로 흩뿌립니다.'),
 ('wargreymon','드라몬 킬러 베기','dragon_claw',1.8,'가이아 포스','Terra Force / Gaia Force','gaia_force',3.2,'sun','#ffb547','overhead','양팔을 머리 위로 들어 에너지를 모은 뒤 앞으로 던지고 버팁니다.'),
 ('metalgarurumon','기계 발톱 타격','wolf_pounce',1.65,'코큐토스 브레스','Freezing Breath / Cocytus Breath','ice_breath',2.7,'ice','#a7e9ff','mouth','다리를 고정하고 머리를 낮춰 절대영도의 냉기를 길게 방출합니다.'),
 ('rosemon','가시 채찍 후려치기','whip',1.9,'쏜 위프','Thorn Whip','electric_whip',2.6,'whip','#ff80bc','rightHand','채찍을 뒤로 당겨 전기를 두르고 휘둘러 끝마디까지 힘을 전달합니다.'),
 ('seraphimon','빛의 구체','light_orb',1.9,'세븐 헤븐즈','Strike of the Seven Stars / Seven Heavens','seven_heavens',3.1,'seven','#fff0ac','chest','날개와 양팔을 펼쳐 일곱 개의 빛을 모으고 차례로 발사합니다.'),
 ('kuwagamon','네 팔 할퀴기','insect_rake',1.8,'시저 암즈','Scissor Claw / Scissor Arms','scissor',2.4,'pincers','#ff9778','head','큰 집게를 앞으로 겨누고 목과 상체에 힘을 실어 내려칩니다.'),
 ('shellmon','앞발 밀어치기','shell_slam',1.8,'하이드로 프레셔','Hydro Blaster / Hydro Pressure','hydro_pressure',2.7,'water','#7bdcff','head','등껍질과 팔로 몸을 지지하고 머리 쪽에서 고압 물줄기를 쏩니다.'),
 ('devimon','어둠의 손날','claw',1.8,'데스 클로','Death Claw','death_claw',2.6,'darkclaw','#b18aea','hands','상체를 뒤로 젖혔다가 두 팔을 길게 뻗어 어둠의 손톱으로 찌릅니다.'),
 ('etemon','러브 세레나데','serenade',1.9,'다크 스피리츠','Dark Spirits','dark_spirits',2.5,'darkorb','#a476d0','rightHand','마이크를 든 팔과 반대 손을 구분해 검은 구체를 모아 던집니다.'),
]

def row_to_spec(row):
    ident,an,ap,ad,sn,en,sp,sd,effect,color,origin,description=row
    attack_effect={'ear_slap':'air','light_orb':'holyball','serenade':'sound','staff':'arc','sword':'arc','whip':'arc','vine_slap':'vines'}.get(ap,'arc' if ap in ('claw','dragon_claw','metal_claw','talon','petal_slap','four_arm','insect_rake') else 'impact')
    return dict(id=ident,source='https://digimon.net/reference_en/detail.php?directory_name='+OFFICIAL.get(ident,ident),checked='2026-10-02',
        motionBasis='공식 기술 설명을 참고해 구성한 모션',
        Attack=dict(name=an,pattern=ap,duration=ad,effect=attack_effect,color=color,origin='rightHand' if ap in ('metal_claw','whip') else 'leftHand' if ap in ('staff','sword','serenade') else 'hands' if attack_effect=='arc' else 'chest',release=.43,end=.76,description='체형과 무장에 맞춘 일반 공격입니다.'),
        Skill=dict(name=sn,aliases=en,pattern=sp,duration=sd,effect=effect,color=color,origin=origin,release=.57,end=.83,description=description))
TECHNIQUES={row[0]:row_to_spec(row) for row in ROWS}
assert len(TECHNIQUES)==34
for ident in ('koromon','tsunomon','mochimon','tanemon','pyocomon'):TECHNIQUES[ident]['Skill']['shots']=3
TECHNIQUES['seraphimon']['Skill']['shots']=7
TECHNIQUES['birdramon']['Skill']['shots']=6
TECHNIQUES['metalgreymon']['Skill']['shots']=2
TECHNIQUES['togemon']['Skill']['release']=.40
TECHNIQUES['togemon']['Skill']['shots']=4
TECHNIQUES['rosemon']['Attack']['release']=.48
TECHNIQUES['etemon']['Attack']['description']='마이크를 들어 소리로 상대를 압도합니다.'
TECHNIQUES['patamon']['Attack']['description']='큰 두 귀로 상대를 후려칩니다.'
TECHNIQUES['patamon']['Attack'].update(effect='impact',origin='head')
TECHNIQUES['seraphimon']['Attack']['origin']='leftHand'
TECHNIQUES['tokomon']['Skill']['rigNote']='입을 벌리는 별도 관절이 없는 원본 모델로, 머리와 몸의 물기 동작을 표현합니다.'
TECHNIQUES['kuwagamon']['Skill']['rigNote']='좌우 집게의 독립 관절이 없는 원본 모델로, 목·상체·아래턱과 집게 타격 효과를 조합합니다.'
