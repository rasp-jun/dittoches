"""Create review sheets from real browser screenshots, without image generation."""
from pathlib import Path
import json,math
from PIL import Image,ImageDraw,ImageFont

ROOT=Path(__file__).resolve().parents[1]
FOLDER=ROOT/'Builds/FaithfulMotionValidation'
OUT=ROOT/'Builds/TechniqueReview'


def main():
    manifest=json.loads((ROOT/'ArtSource/FaithfulGallery/manifest.json').read_text(encoding='utf-8-sig'))
    ids=[e['id'] for e in manifest['entries']]
    files={}
    for folder in sorted(FOLDER.glob('review-technique*')):
        for p in folder.glob('*.png'):files[p.stem]=p
    for p in (OUT/'frames').glob('*.png'):files[p.stem]=p
    OUT.mkdir(exist_ok=True)
    font=ImageFont.load_default()
    small=ImageFont.load_default()
    modes=['Attack-charge','Attack-release','Skill-charge','Skill-release']
    for page,start in enumerate(range(0,len(ids),6),1):
        subset=ids[start:start+6];canvas=Image.new('RGB',(1600,60+len(subset)*260),'#f8fafb');draw=ImageDraw.Draw(canvas)
        draw.text((15,12),'Technique motion review / 2026-10-02 / '+str(page),font=font,fill='#244955')
        for row,ident in enumerate(subset):
            for column,mode in enumerate(modes):
                path=files[ident+'-'+mode]
                shot=Image.open(path).convert('RGB').crop((218,82,1160,690));shot.thumbnail((390,215),Image.Resampling.LANCZOS)
                x=column*400;y=60+row*260;canvas.paste(shot,(x+(400-shot.width)//2,y+25))
                draw.text((x+12,y+3),ident+' / '+mode,font=small,fill='#244955')
        canvas.save(OUT/('techniques-'+str(page)+'.jpg'),quality=93)
    selected=['greymon','wargreymon','metalgreymon','lilimon','birdramon','seraphimon']
    canvas=Image.new('RGB',(1500,1530),'#f8fafb');draw=ImageDraw.Draw(canvas)
    for index,ident in enumerate(selected):
        path=files[ident+'-Skill-release'];shot=Image.open(path).convert('RGB').crop((218,82,1160,690));shot.thumbnail((720,465),Image.Resampling.LANCZOS)
        x=(index%2)*750;y=(index//2)*510
        canvas.paste(shot,(x+15,y+32));draw.text((x+15,y+12),ident,font=font,fill='#244955')
    canvas.save(OUT/'techniques-overview.jpg',quality=94)
    print('TECHNIQUE REVIEW SHEETS',len(ids),len(modes),'poses')


if __name__=='__main__':main()
