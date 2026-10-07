import sys,json
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from faithful_rig_common import *
ids=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else ['agumon','wargreymon','greymon','garurumon','metalgarurumon','kabuterimon','atlur','holyangemon','seraphimon','koromon','tsunomon','pyocomon','mochimon']
for ident in ids:
    meshes=import_static(ident);folder=OUT/ident;folder.mkdir(parents=True,exist_ok=True)
    rows=[]
    for obj in meshes:
        lo,hi=bounds([obj]);rows.append({'object':obj.name,'vertices':len(obj.data.vertices),'min':list(lo),'max':list(hi),'materials':[m.name for m in obj.data.materials]})
    (folder/'source-anatomy.json').write_text(json.dumps(rows,indent=2),encoding='utf-8')
    camera=studio(meshes)
    render(folder/'source-front.png',camera,(0,-8,1.2))
    render(folder/'source-side.png',camera,(8,0,1.2))
    print('ANATOMY INSPECTED',ident,flush=True)
