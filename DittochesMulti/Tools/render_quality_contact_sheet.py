"""Assemble actual WebGL review captures without altering the renders."""
import json
import sys
from pathlib import Path
ROOT=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(ROOT.parent/f'tmp/gallery-test-tools-py{sys.version_info.major}{sys.version_info.minor}'))
from PIL import Image, ImageDraw, ImageFont

folder=ROOT/'Builds/QualityPass-20261006'
entries=json.loads((ROOT/'ArtSource/FaithfulGallery/manifest.json').read_text(encoding='utf-8'))['entries']
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',16)
poses=[('Idle-00','대기'),('Attack-51','일반 공격'),('Skill-35','기술 준비'),('Skill-67','기술 발사'),('Skill-81','기술 후반')]
for batch in range(0,len(entries),5):
    rows=entries[batch:batch+5]
    sheet=Image.new('RGB',(1700,270*len(rows)), '#f6f8fa');draw=ImageDraw.Draw(sheet)
    for row,e in enumerate(rows):
        for column,(pose,label) in enumerate(poses):
            path=folder/'current'/f"{e['id']}-{pose}.png"
            if not path.exists():continue
            tile=Image.open(path).convert('RGB').crop((218,82,1159,757)).resize((330,237),Image.Resampling.LANCZOS)
            x,y=column*340+5,row*270
            draw.text((x,y+3),e['name']+' · '+label,font=font,fill='#264854')
            sheet.paste(tile,(x,y+28))
    sheet.save(folder/f'quality-sheet-{batch//5+1:02}.jpg',quality=91)

examples=[('greymon','Skill-67'),('wargreymon','Skill-35'),('metalgreymon','Skill-81'),
          ('metalgarurumon','Skill-67'),('holyangemon','Skill-67'),('seraphimon','Skill-35')]
sheet=Image.new('RGB',(1200,len(examples)*452),'#f6f8fa');draw=ImageDraw.Draw(sheet)
for row,(ident,pose) in enumerate(examples):
    for column,variant in enumerate(('baseline','current')):
        tile=Image.open(folder/variant/f'{ident}-{pose}.png').convert('RGB').crop((218,82,1159,757)).resize((586,420),Image.Resampling.LANCZOS)
        x,y=column*600+7,row*452
        draw.text((x,y+3),ident+' · '+('수정 전' if column==0 else '수정 후'),font=font,fill='#264854')
        sheet.paste(tile,(x,y+27))
sheet.save(folder/'before-after.jpg',quality=93)
print('Contact sheets saved:',folder)
