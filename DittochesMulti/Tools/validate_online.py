"""Run the real preview client against an isolated, temporary local HTTP server.

The second client is an HTTP test participant, not a second rendered window.
Uses neither the user's accounts nor the normal server database/save keys.
"""
import argparse
import subprocess
import sys
import tempfile
import threading
from http.server import ThreadingHTTPServer
from pathlib import Path

ROOT=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(ROOT/'Server'))
from server import Game, Handler


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--timeout',type=int,default=90)
    args=parser.parse_args()
    preview=ROOT/'Builds/PortablePreview'
    log=preview/'OnlineEquipmentSmoke.log'
    with tempfile.TemporaryDirectory(prefix='dittoches-online-') as temporary:
        game=Game(Path(temporary)/'smoke.sqlite3')
        http=ThreadingHTTPServer(('127.0.0.1',0),Handler);http.game=game
        worker=threading.Thread(target=http.serve_forever,daemon=True);worker.start()
        process=None
        try:
            process=subprocess.Popen([str(preview/'DittochesMulti.exe'),'--online-smoke','--test-server',
                f'http://127.0.0.1:{http.server_port}','-screen-fullscreen','0','-screen-width','1600','-screen-height','1000','-logFile',str(log)],cwd=preview)
            result=process.wait(timeout=args.timeout)
            output=log.read_text(encoding='utf-8',errors='replace')
            if result or 'ONLINE SMOKE COMPLETE:' not in output or 'Exception:' in output or 'ONLINE SMOKE FAILED:' in output:
                raise RuntimeError(f'Online client validation failed ({result}). Read {log}')
            print(next(line for line in output.splitlines() if 'ONLINE SMOKE COMPLETE:' in line))
            print('Screenshots:',preview/'OnlineCaptures')
        finally:
            if process is not None and process.poll() is None:
                process.terminate();process.wait(timeout=10)
            http.shutdown();http.server_close();worker.join(timeout=3);game.db.close()


if __name__=='__main__':main()
