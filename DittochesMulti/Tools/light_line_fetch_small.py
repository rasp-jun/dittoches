"""Fetch attributed public original model references for the light line.

Separate from the existing 34 species; this does not claim original authorship.
"""
import hashlib, json, urllib.request
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'ArtSource/LightLine-20261013'
INVENTORY=ROOT/'ArtSource/ThirdPartyCandidates/SteamDigimonModels-2101623444/mesh-inventory.json'
NAMES={'nyaromon':'Nyaromon','plotmon':'Salamon','tailmon':'Gatomon'}

def download(url,path):
    if not path.exists():
        req=urllib.request.Request(url,headers={'User-Agent':'Mozilla/5.0'})
        with urllib.request.urlopen(req,timeout=90) as response:path.write_bytes(response.read())
    return {'url':url,'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'bytes':path.stat().st_size}

for ident,title in NAMES.items():
    record=next(x for x in json.loads(INVENTORY.read_text(encoding='utf-8-sig')) if x['Nickname']==title)
    folder=OUT/ident;folder.mkdir(parents=True,exist_ok=True);files={}
    for key,name in [('MeshURL','source.obj'),('DiffuseURL','diffuse.png')]:
        original=record['CustomMesh'][key]
        url=original.replace('http://cloud-3.steamusercontent.com','https://cdn.steamusercontent.com')
        files[name]=download(url,folder/name);files[name]['original_url']=original
    files['official-reference.jpg']=download('https://digimon.net/cimages/digimon/'+ident+'.jpg',folder/'official-reference.jpg')
    source={'id':ident,'title':title,'source_url':'https://steamcommunity.com/sharedfiles/filedetails/?id=2101623444',
      'author':'Digital Dray (workshop preparation); original game model creator not individually identified',
      'license':'Game-derived reference asset; redistribution permission not established',
      'provenance':'Uploader states these models were taken from games and public internet sources; not an original model authored by this project.',
      'usage':'Local appearance, animation review and user-requested local game integration; no external publication.',
      'official_reference':'https://digimon.net/reference_en/detail.php?directory_name='+ident,
      'files':files,'workshop_object':record,'checked_on':'2026-10-13'}
    (folder/'source.json').write_text(json.dumps(source,ensure_ascii=False,indent=2),encoding='utf-8')
    print(ident,{k:v['bytes'] for k,v in files.items()},flush=True)
