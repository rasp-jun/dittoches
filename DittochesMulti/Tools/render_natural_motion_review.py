"""Compose local browser captures into motion sheets for visual inspection."""
import json,math,argparse
from pathlib import Path
from PIL import Image,ImageDraw

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Builds/FaithfulNaturalValidation'
MODES=['Idle','Walk','Attack','Skill','Victory','Down']

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--revision',default='naturalfinal');args=parser.parse_args()
    manifest=json.loads((ROOT/'ArtSource/FaithfulGallery/manifest.json').read_text(encoding='utf-8-sig'))
    ids=[e['id'] for e in manifest['entries']]
    suffixes=['first','rest','native','detail','fix','last']
    folders=[ROOT/'Builds/FaithfulMotionValidation'/('review-'+args.revision+s) for s in suffixes]
    rows=[]
    for ident in ids:
        captures=[]
        for mode in MODES:
            matches=[p/(ident+'-'+mode+'.png') for p in folders if (p/(ident+'-'+mode+'.png')).exists()]
            if not matches:break
            captures.append(matches[-1])
        if len(captures)==len(MODES):rows.append((ident,captures))
    for start in range(0,len(rows),5):
        batch=rows[start:start+5]
        sheet=Image.new('RGB',(360*len(MODES),52+len(batch)*263),'#f7f9fa');draw=ImageDraw.Draw(sheet)
        draw.text((18,12),'NATURAL MOTION REVIEW / %02d-%02d'%(start+1,start+len(batch)),fill='#264b56',font_size=22)
        for row,(ident,paths) in enumerate(batch):
            y=52+row*263
            for col,(mode,path) in enumerate(zip(MODES,paths)):
                draw.text((col*360+12,y+6),ident+' / '+mode,fill='#25313d',font_size=17)
                im=Image.open(path).convert('RGB').crop((218,82,1160,688))
                im.thumbnail((358,230),Image.Resampling.LANCZOS)
                sheet.paste(im,(col*360+(360-im.width)//2,y+32))
        sheet.save(OUT/('motion-sheet-%02d.png'%(start//5+1)))
    print('VISUAL SHEETS',len(rows),'species',math.ceil(len(rows)/5),'pages')

if __name__=='__main__':main()
