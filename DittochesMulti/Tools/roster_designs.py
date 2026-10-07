"""Per-character silhouette, palette and anatomy for the local Blender roster.

These are authored review models, not extracted game assets.
"""
DESIGNS = [
 # id, Korean name, anatomy, primary, secondary, distinguishing features
 ('koromon','코로몬','baby','#f29ab9','#c54c7b','long_ears'),
 ('tsunomon','뿔몬','baby','#df963f','#fff0d0','single_horn'),
 ('mochimon','모티몬','baby','#db88c0','#b34283','mitten_arms'),
 ('tanemon','시드몬','baby','#edf1d0','#64a940','sprout'),
 ('pyocomon','어니몬','baby','#ea8ac1','#749cdf','flower'),
 ('tokomon','토코몬','baby','#faf0dc','#df7996','tiny_legs'),
 ('agumon','아구몬','authored','#f7b424','#efe8ce','dinosaur'),
 ('gabumon','파피몬','biped','#e7dca9','#436b9d','fur_horn'),
 ('tentomon','텐타몬','insect','#d73632','#354063','ladybird'),
 ('palmon','팔몬','plant','#8ebd59','#d982ab','palm_flower'),
 ('piyomon','피요몬','bird','#df82b7','#4b83ae','pink_bird'),
 ('patamon','파닥몬','winged_blob','#dc9943','#f5ead6','ear_wings'),
 ('togemon','니드몬','plant','#569347','#b83831','cactus_boxer'),
 ('garurumon','가루몬','quadruped','#e9e7dc','#4c659b','striped_wolf'),
 ('greymon','그레이몬','dinosaur','#e59030','#704834','horned_helmet'),
 ('kabuterimon','캅테리몬','insect','#4a5b9c','#d8d2b4','rhino_beetle'),
 ('angemon','엔젤몬','humanoid','#f3e9d6','#d8b653','six_wing_staff'),
 ('birdramon','버드라몬','bird','#ea742e','#f7c044','flame_bird'),
 ('metalgreymon','메탈그레이몬','dinosaur','#db8b3f','#aab7bd','cyborg_dinosaur'),
 ('weregarurumon','워가루몬','humanoid','#d7dce2','#536598','wolf_fighter'),
 ('lilimon','릴리몬','humanoid','#e69ac0','#5b9a55','flower_fairy'),
 ('holyangemon','홀리엔젤몬','humanoid','#e6dce8','#8e70ab','eight_wing_sword'),
 ('atlur','아트라캅테리몬','insect','#a33135','#ebc387','red_beetle'),
 ('garudamon','가루다몬','humanoid','#c87b43','#eac19a','eagle_warrior'),
 ('herakle','헤라클레스캅테리몬','insect','#d0a33d','#46413c','gold_beetle'),
 ('hououmon','피닉스몬','bird','#eac342','#b94235','four_wing_phoenix'),
 ('wargreymon','워그레이몬','humanoid','#dc9a35','#b8c1c7','dragon_armor'),
 ('metalgarurumon','메탈가루몬','quadruped','#8dabce','#d9e0e4','metal_wolf'),
 ('rosemon','로제몬','humanoid','#bd384f','#488850','rose_whip'),
 ('seraphimon','세라피몬','humanoid','#5678b4','#e1bd55','ten_wing_armor'),
 ('kuwagamon','쿠가몬','insect','#b83d35','#dfc6a4','stag_beetle'),
 ('shellmon','쉘몬','shell','#d994a4','#c5b491','spiral_shell'),
 ('devimon','데블몬','humanoid','#353442','#ad393f','devil_wings'),
 ('etemon','에테몬','humanoid','#d59a48','#eddbba','monkey_sunglasses'),
]
OFFICIAL = {'tsunomon':'tunomon','pyocomon':'pyocomon','lilimon':'lilimon','weregarurumon':'weregarrumon',
 'greymon':'greymon-first','metalgreymon':'metalgreymon-v',
 'atlur':'atlurkabuterimon','herakle':'heraklekabuterimon'}
CLIPS=[('Idle',2.4,True),('Walk',.9,True),('Run',.6,True),('Attack',.8,False),
       ('PepperBreath',.95,False),('Hit',.55,False),('Defeat',1.2,False),('Turn',1.8,True)]
