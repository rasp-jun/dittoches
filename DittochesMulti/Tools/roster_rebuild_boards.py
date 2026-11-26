from pathlib import Path
import sys,argparse
sys.path.insert(0,r'D:\Dittoches_KEEP_20260917\01_CurrentProject\tmp\gallery-test-tools-py311')
from PIL import Image,ImageDraw
from roster_rebuild_sources import OUT
p=argparse.ArgumentParser();p.add_argument('--ids',required=True);p.add_argument('--views',default='preview,attack,skill');p.add_argument('--name',default='qa');a=p.parse_args()
ids=a.ids.split(',');views=a.views.split(',')
for start in range(0,len(ids),6):
    group=ids[start:start+6];board=Image.new('RGB',(300*len(group),330*len(views)),(24,30,40));draw=ImageDraw.Draw(board)
    for x,ident in enumerate(group):
        for y,view in enumerate(views):
            path=OUT/ident/(view+'.png')
            if path.exists():board.paste(Image.open(path).convert('RGB').resize((300,300)),(300*x,330*y+30))
            draw.text((300*x+8,330*y+8),ident+' / '+view,fill='white')
    board.save(OUT/(a.name+'-'+str(start//6)+'.jpg'),quality=90)
