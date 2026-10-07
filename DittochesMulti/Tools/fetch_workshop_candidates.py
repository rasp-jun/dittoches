"""Save explicitly listed public workshop meshes as immutable local candidates.

Requires the downloaded public inventory; never executes workshop scripts.
The uploader identifies game-derived assets, not original CC character art.
"""
import concurrent.futures,hashlib,json,urllib.request,urllib.parse
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
PACKAGE=ROOT/'ArtSource/ThirdPartyCandidates/SteamDigimonModels-2101623444'
NAMES={'tanemon':'Tanemon','tokomon':'Tokomon','gabumon':'Gabumon','tentomon':'Tentomon',
       'palmon':'Palmon','piyomon':'Biyomon','patamon':'Patamon','togemon':'Togemon',
       'angemon':'Angemon','metalgreymon':'MetalGreymon','weregarurumon':'WereGarurumon',
       'lilimon':'Lilymon','garudamon':'Garudamon','herakle':'HerculesKabuterimon',
       'hououmon':'Phoenixmon','shellmon':'Shellmon','devimon':'Devimon','etemon':'Etemon'}

def fetch(item):
    ident,name=item
    inventory=json.loads((PACKAGE/'mesh-inventory.json').read_text())
    selected=[row for row in inventory if row['Nickname']==name]
    assert len(selected)==1,(ident,'ambiguous source',len(selected))
    record=selected[0];folder=PACKAGE/ident;folder.mkdir(exist_ok=True)
    files={}
    for key,filename in [('MeshURL','source.obj'),('DiffuseURL','diffuse.png')]:
        original_url=record['CustomMesh'][key]
        parsed=urllib.parse.urlsplit(original_url)
        assert parsed.path.startswith('/ugc/'),original_url
        # Steam migrated public Cloud assets to its current CDN. Keep the exact
        # public object path and normal TLS verification, and retain both URLs.
        url=urllib.parse.urlunsplit(('https','cdn.steamusercontent.com',parsed.path,'',''))
        target=folder/filename
        if not target.exists():
            with urllib.request.urlopen(url,timeout=45) as response:body=response.read()
            assert body and not body.lstrip().startswith(b'<!DOCTYPE'),url
            target.write_bytes(body)
        files[filename]={'url':url,'original_url':original_url,'sha256':hashlib.sha256(target.read_bytes()).hexdigest(),'bytes':target.stat().st_size}
    source={'id':ident,'title':name,'source_url':'https://steamcommunity.com/sharedfiles/filedetails/?id=2101623444',
            'author':'Digital Dray (workshop preparation); original game model creator not individually identified',
            'license':'Game-derived reference asset; redistribution permission not established',
            'provenance':'Uploader states these models were taken from games and public internet sources; not an original model authored by this project.',
            'usage':'Local appearance and animation review only; no external publication.',
            'files':files,'workshop_object':record,'checked_on':'2026-10-02'}
    (folder/'source.json').write_text(json.dumps(source,indent=2),encoding='utf-8')
    print('CANDIDATE SAVED',ident,files['source.obj']['bytes'],flush=True)
    return {'id':ident,'files':files}

if __name__=='__main__':
    rows=[]
    with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
        for item in pool.map(fetch,NAMES.items()):rows.append(item)
    (PACKAGE/'download-report.json').write_text(json.dumps(rows,indent=2),encoding='utf-8')
