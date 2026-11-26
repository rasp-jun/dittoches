"""Stage attributed mesh sources for the 34-model rebuild, keeping originals intact."""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
import hashlib,json,urllib.request

GAME=Path(__file__).resolve().parents[1]
OUT=GAME/'ArtSource/RosterRebuild-20261013'
INVENTORY=GAME/'ArtSource/ThirdPartyCandidates/SteamDigimonModels-2101623444/mesh-inventory.json'
NAMES=dict(agumon='Agumon',wargreymon='WarGreymon',greymon='Greymon',garurumon='Garurumon',
 kabuterimon='Kabuterimon',atlur='MegaKabuterimon',koromon='Koromon',tsunomon='Tsunomon',
 mochimon='Mochimon',pyocomon='Yokomon',holyangemon='MagnaAngemon',metalgarurumon='MetalGarurumon',
 birdramon='Birdramon',kuwagamon='Kuwagamon')
ENEMIES=dict(monochromon='Monochromon',ogremon='Ogremon',meramon='Meramon',leomon='Leomon',
 andromon='Andromon',skullgreymon='SkullGreymon',piedmon='Piedmon',machinedramon='Machinedramon')
NAMES.update(ENEMIES)

def main():
    inventory=json.loads(INVENTORY.read_text(encoding='utf-8-sig'))
    def fetch(pair):
        ident,title=pair
        record=next(x for x in inventory if x['Nickname']==title)
        folder=OUT/ident/'source';folder.mkdir(parents=True,exist_ok=True);files={}
        for key,name in [('MeshURL','source.obj'),('DiffuseURL','diffuse.png')]:
            original=record['CustomMesh'][key]
            url=original.replace('http://cloud-3.steamusercontent.com','https://cdn.steamusercontent.com')
            target=folder/name
            if not target.exists():
                with urllib.request.urlopen(url,timeout=60) as response:data=response.read()
                assert len(data)>1000 and not data.startswith(b'<!'),(ident,name)
                target.write_bytes(data)
            files[name]=dict(url=url,sha256=hashlib.sha256(target.read_bytes()).hexdigest(),bytes=target.stat().st_size)
        source=dict(id=ident,title=title,source_url='https://steamcommunity.com/sharedfiles/filedetails/?id=2101623444',
          author='Digital Dray (workshop preparation); original game artists not individually identified',
          provenance='Public game-derived reference mesh. Not original geometry authored by this project.',
          license='Redistribution permission not established; local project use only. No external publication.',
          files=files,date='2026-10-13',workshop_object=record)
        (folder/'source.json').write_text(json.dumps(source,ensure_ascii=False,indent=2),encoding='utf-8')
        print(ident,[(name,row['bytes']) for name,row in files.items()],flush=True)
        return source
    with ThreadPoolExecutor(max_workers=3) as pool:records=list(pool.map(fetch,NAMES.items()))
    (OUT/'source-inventory.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')

if __name__=='__main__':main()
