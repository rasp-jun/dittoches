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
    parser.add_argument('--network-faults',action='store_true',help='Drop one committed purchase response and fail two state polls.')
    parser.add_argument('--timeout',type=int,default=90)
    parser.add_argument('--preview',type=Path,default=ROOT/'Builds/PortablePreview')
    args=parser.parse_args()
    preview=args.preview.resolve()
    log=preview/'OnlineEquipmentSmoke.log'
    with tempfile.TemporaryDirectory(prefix='dittoches-online-') as temporary:
        class FaultGame(Game):
            dropped_purchase=False
            state_failures=0
            duplicate_purchase=False
            def request(self,path,data,token=''):
                result=super().request(path,data,token)
                if args.network_faults:
                    if path=='/state' and self.state_failures<2:
                        self.state_failures+=1
                        raise RuntimeError('simulated transient outage')
                    if path=='/action' and data.get('action')=='buy' and data.get('requestId'):
                        if not self.dropped_purchase:
                            self.dropped_purchase=True
                            result['_testDropResponse']=True
                        if result.get('duplicateRequest'):self.duplicate_purchase=True
                return result
        game=FaultGame(Path(temporary)/'smoke.sqlite3')
        class FaultHandler(Handler):
            def reply(self,status,data):
                if data.pop('_testDropResponse',False):
                    # Apply on the real server, then close TCP before any HTTP response.
                    self.close_connection=True
                    return
                super().reply(status,data)
        http=ThreadingHTTPServer(('127.0.0.1',0),FaultHandler);http.game=game
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
            if args.network_faults:
                assert game.dropped_purchase and game.duplicate_purchase and game.state_failures==2, 'Fault injection did not exercise recovery'
                print('Network faults: transient outage recovered; committed purchase acknowledged once on retry.')
            print('Screenshots:',preview/'OnlineCaptures')
        finally:
            if process is not None and process.poll() is None:
                process.terminate();process.wait(timeout=10)
            http.shutdown();http.server_close();worker.join(timeout=3);game.db.close()


if __name__=='__main__':main()
