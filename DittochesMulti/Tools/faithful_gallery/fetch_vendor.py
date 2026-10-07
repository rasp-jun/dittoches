"""Reproduce the pinned, offline Three.js vendor files from the official package."""
from concurrent.futures import ThreadPoolExecutor
import hashlib
import json
from pathlib import Path
import urllib.request

ROOT = Path(__file__).resolve().parent / 'vendor'
VERSION = '0.180.0'
FILES = ['build/three.module.js', 'build/three.core.js',
         'examples/jsm/loaders/GLTFLoader.js', 'examples/jsm/controls/OrbitControls.js',
         'examples/jsm/utils/BufferGeometryUtils.js', 'examples/jsm/environments/RoomEnvironment.js', 'LICENSE']

def fetch(path):
    url = 'https://cdn.jsdelivr.net/npm/three@' + VERSION + '/' + path
    dest = ROOT / path
    dest.parent.mkdir(parents=True, exist_ok=True)
    content = urllib.request.urlopen(url, timeout=60).read()
    dest.write_bytes(content)
    return {'file': path, 'url': url, 'bytes': len(content), 'sha256': hashlib.sha256(content).hexdigest()}

if __name__ == '__main__':
    with ThreadPoolExecutor(max_workers=4) as pool:
        records = list(pool.map(fetch, FILES))
    (ROOT / 'sources.json').write_text(json.dumps({'package': 'three', 'version': VERSION,
       'license': 'MIT', 'runtime_network_required': False, 'files': records}, indent=2), encoding='utf-8')
    print('VENDORED', len(records), 'THREE.JS FILES', sum(x['bytes'] for x in records), 'BYTES')
