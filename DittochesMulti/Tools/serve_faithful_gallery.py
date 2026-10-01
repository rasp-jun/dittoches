"""Serve the offline 3D comparison gallery on this computer only.

python Tools/serve_faithful_gallery.py --open
The manifest is ArtSource/FaithfulGallery/manifest.json. The server never fetches
model files or JavaScript from the internet at runtime.
"""
import argparse
import functools
import http.server
import json
from pathlib import Path
import sys
import threading
import urllib.request
import webbrowser

ROOT = Path(__file__).resolve().parent.parent
ENTRY = '/Tools/faithful_gallery/index.html'
ALLOWED_ROOTS = tuple((ROOT / path).resolve() for path in (
    'Tools/faithful_gallery', 'ArtSource/FaithfulGallery', 'ArtSource/Roster/ReferencesV2'))

class Handler(http.server.SimpleHTTPRequestHandler):
    extensions_map = {**http.server.SimpleHTTPRequestHandler.extensions_map,
                      '.js': 'text/javascript', '.mjs': 'text/javascript',
                      '.glb': 'model/gltf-binary', '.gltf': 'model/gltf+json',
                      '.json': 'application/json', '.wasm': 'application/wasm'}

    def send_head(self):
        # Resolve after URL decoding and normalisation; symlinks and traversal
        # must not expose neighbouring source, secrets or repository metadata.
        target = Path(self.translate_path(self.path)).resolve()
        if not any(target.is_relative_to(folder) for folder in ALLOWED_ROOTS):
            self.send_error(403, 'This path is not part of the gallery.')
            return None
        return super().send_head()

    def list_directory(self, path):
        self.send_error(403, 'Directory listing is disabled.')
        return None

    def do_GET(self):
        if self.path.split('?', 1)[0] == '/health':
            body = json.dumps({'app': 'faithful-digimon-gallery', 'ready': True}).encode()
            self.send_response(200)
            self.send_header('Content-Type', 'application/json')
            self.send_header('Content-Length', str(len(body)))
            self.end_headers()
            self.wfile.write(body)
            return
        if self.path.split('?', 1)[0] == '/':
            self.send_response(302)
            self.send_header('Location', ENTRY + ('?' + self.path.split('?', 1)[1] if '?' in self.path else ''))
            self.end_headers()
            return
        super().do_GET()

    def end_headers(self):
        self.send_header('Cache-Control', 'no-cache')
        self.send_header('X-Content-Type-Options', 'nosniff')
        super().end_headers()

    def log_message(self, pattern, *args):
        # Keep the helper log useful: normal model/module requests are quiet.
        if args and len(args) > 1 and str(args[1]) not in ('200', '302', '304'):
            super().log_message(pattern, *args)

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--port', type=int, default=8766)
    parser.add_argument('--open', action='store_true', help='Open the gallery in the default browser')
    args = parser.parse_args()
    url = f'http://127.0.0.1:{args.port}/'
    handler = functools.partial(Handler, directory=str(ROOT))
    try:
        server = http.server.ThreadingHTTPServer(('127.0.0.1', args.port), handler)
    except OSError as error:
        try:
            health = json.load(urllib.request.urlopen(url + 'health', timeout=2))
        except Exception:
            print(f'Cannot start gallery on {url}: {error}', file=sys.stderr)
            return 1
        if health.get('app') != 'faithful-digimon-gallery':
            print(f'Port {args.port} is already used by another application.', file=sys.stderr)
            return 1
        print('Gallery is already running:', url, flush=True)
        if args.open:
            webbrowser.open(url, new=2)
        return 0
    print('Faithful Digimon 3D gallery:', url, flush=True)
    if args.open:
        threading.Timer(.3, lambda: webbrowser.open(url, new=2)).start()
    try:
        server.serve_forever(poll_interval=.4)
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()
    return 0

if __name__ == '__main__':
    raise SystemExit(main())
