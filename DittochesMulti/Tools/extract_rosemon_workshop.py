"""Preserve a publicly downloaded GMA and extract only model/material data."""
import io,json,struct,hashlib,shutil,zlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'ArtSource/ThirdPartyCandidates/RosemonWorkshop-2451134533'
INPUT=ROOT.parent/'tmp/steamcmd/steamapps/workshop/content/4000/2451134533/new_item_via_crowbar.gma'

def main():
    OUT.mkdir(parents=True,exist_ok=True)
    shutil.copy2(INPUT,OUT/'workshop-original.gma')
    raw=INPUT.read_bytes();stream=io.BytesIO(raw)
    def read(fmt):return struct.unpack('<'+fmt,stream.read(struct.calcsize('<'+fmt)))[0]
    def string():
        value=bytearray()
        while True:
            byte=stream.read(1)
            assert byte,'Unexpected end of GMA'
            if byte==b'\0':return value.decode('utf-8')
            value.extend(byte)
    assert stream.read(4)==b'GMAD'
    version=read('B');assert version==3
    steam_id=read('Q');timestamp=read('Q')
    while string():pass
    title,description,author=string(),string(),string();addon_version=read('I')
    files=[]
    while read('I'):
        files.append((string(),read('Q'),read('I')))
    records=[];base=(OUT/'data').resolve()
    for name,size,crc in files:
        body=stream.read(size);assert len(body)==size
        assert zlib.crc32(body)==crc,('CRC mismatch',name)
        path=(base/name).resolve();assert path.is_relative_to(base),name
        if path.suffix.lower() not in ('.mdl','.vvd','.vtx','.vmt','.vtf','.phy'):continue
        path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(body)
        records.append({'name':name,'size':size,'sha256':hashlib.sha256(body).hexdigest()})
    (OUT/'extraction-report.json').write_text(json.dumps({'title':title,'description':description,'author':author,'package_sha256':hashlib.sha256(raw).hexdigest(),'files':records},indent=2),encoding='utf-8')
    print('Extracted',len(records),'data files; no addon scripts executed')
    print('\n'.join(r['name'] for r in records))
if __name__=='__main__':main()
