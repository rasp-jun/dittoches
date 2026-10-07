"""Validate portrait provenance and PNGs, then copy the reviewed renders into StreamingAssets."""
import argparse
import hashlib
import json
import shutil
import struct
from pathlib import Path


def main():
    root=Path(__file__).resolve().parent.parent
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--player',type=Path,default=root/'Builds/PortraitPreview')
    args=parser.parse_args()
    source=args.player/'DittochesMulti_Data/StreamingAssets/FaithfulPortraits'
    manifest=json.loads((source/'manifest.json').read_text(encoding='utf-8'))
    models=json.loads((root/'Assets/StreamingAssets/FaithfulModels/manifest.json').read_text(encoding='utf-8'))
    expected={row['id']:row['source_sha256'] for row in models['models']}
    assert manifest['passed'] and len(manifest['models'])==len(expected)==34
    assert {row['id'] for row in manifest['models']}==set(expected)
    files={}
    for row in manifest['models']:
        assert row['sourceHash']==expected[row['id']], f"Stale model: {row['id']}"
        for suffix in ('','-bust'):
            name=row['id']+suffix+'.png';data=(source/name).read_bytes()
            assert data[:8]==b'\x89PNG\r\n\x1a\n' and struct.unpack('>II',data[16:24])==(768,768),name
            files[name]=hashlib.sha256(data).hexdigest()
    manifest['files']=files
    target=root/'Assets/StreamingAssets/FaithfulPortraits';target.mkdir(parents=True,exist_ok=True)
    for name in files:shutil.copy2(source/name,target/name)
    (target/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
    print(f'Promoted {len(files)} portraits from {len(expected)} matching reviewed models.')


if __name__=='__main__':main()
