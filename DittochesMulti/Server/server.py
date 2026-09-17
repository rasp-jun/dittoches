"""Dittoches Multi prototype. Python 3.10+, standard library only."""
import argparse
import hashlib
import json
import math
import random
import secrets
import sqlite3
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from combat_skills import simulate
from combat_builds import combine
from combat_stats import SKILLS

ROOT = Path(__file__).resolve().parent
ROSTER = json.loads((ROOT / 'roster.json').read_text(encoding='utf-8'))
DEFS = {u['id']: u for u in ROSTER}
POOL = [0, 39, 26, 21, 13, 10]
ODDS = [[100,0,0,0,0],[100,0,0,0,0],[75,25,0,0,0],[55,30,15,0,0],[40,35,23,2,0],[25,40,28,7,0],[18,30,35,16,1],[15,20,32,28,5],[10,15,25,35,15]]
XP = [0,2,2,6,10,20,36,60,68]

class Rejected(Exception):
    pass

def require(condition, message):
    if not condition:
        raise Rejected(message)

def index(value, size):
    require(type(value) is int and 0 <= value < size, '잘못된 슬롯입니다.')
    return value

class Game:
    def __init__(self, database=ROOT / 'ratings.sqlite3', clock=time.time):
        self.clock = clock
        self.db = sqlite3.connect(str(database), check_same_thread=False)
        self.db.execute('CREATE TABLE IF NOT EXISTS accounts (id TEXT PRIMARY KEY, name TEXT NOT NULL, rating INTEGER NOT NULL DEFAULT 1000)')
        self.db.commit()
        self.lock = threading.RLock()
        self.sessions, self.accounts, self.rooms = {}, {}, {}
        self.queues = {'normal': [], 'ranked': []}

    def login(self, data):
        name, key = data.get('name', ''), data.get('key', '')
        require(isinstance(name,str) and 1 <= len(name.strip()) <= 20 and all(ord(c)>=32 for c in name), '닉네임은 1~20자로 입력하세요.')
        require(isinstance(key,str) and len(key)==64 and all(c in '0123456789abcdef' for c in key), '계정 키 형식이 잘못되었습니다.')
        identity = hashlib.sha256(key.encode()).hexdigest()
        now = self.clock()
        # Reuse a live session so reconnecting cannot create two queue entries.
        existing = self.accounts.get(identity)
        if existing and existing in self.sessions:
            session = self.sessions[existing]
            session['seen'] = now
            return self.snapshot(session)
        self.db.execute('INSERT OR IGNORE INTO accounts(id,name) VALUES (?,?)',(identity,name.strip()))
        self.db.execute('UPDATE accounts SET name=? WHERE id=?',(name.strip(),identity))
        self.db.commit()
        rating = self.db.execute('SELECT rating FROM accounts WHERE id=?',(identity,)).fetchone()[0]
        token = secrets.token_hex(32)
        session = dict(token=token, id=identity, name=name.strip(), rating=rating, seen=now, queue='', room='', last_action=0)
        self.sessions[token] = session
        self.accounts[identity] = token
        return self.snapshot(session)

    def player(self, session):
        return dict(id=session['id'], name=session['name'], rating=session['rating'], hp=100, gold=10, level=3, xp=0,
                    board=[None]*28, bench=[None]*9, shop=[None]*5, ready=False,
                    inventory=[0,1,2,3,14],inventoryRevision=0)

    def cancel(self, session):
        for queue in self.queues.values():
            if session['token'] in queue:
                queue.remove(session['token'])
        session['queue'] = ''

    def match(self, mode):
        queue = self.queues[mode]
        while len(queue) >= 2:
            left, right = (self.sessions[queue.pop(0)] for _ in range(2))
            room_id = secrets.token_hex(8)
            players = [self.player(left), self.player(right)]
            room = dict(id=room_id, mode=mode, players=players, tokens=[left['token'],right['token']],
                        phase='prepare', round=1, deadline=self.clock()+40, pool={u['id']:POOL[u['cost']] for u in ROSTER},
                        frames=[], winner='', message='', ratingDelta=0, created=self.clock())
            self.rooms[room_id] = room
            for session, player in zip((left,right),players):
                session['queue']=''
                session['room']=room_id
                player['board'][3]={'id':'koromon','star':1,'items':[]}
                room['pool']['koromon']-=1
                self.roll(room,player)

    def roll(self, room, player):
        for unit_id in player['shop']:
            if unit_id:
                room['pool'][unit_id]+=1
        locked = {u['id'] for u in player['board']+player['bench'] if u and u['star']==3}
        player['shop']=[None]*5
        for i in range(5):
            cost=random.choices(range(1,6),weights=ODDS[player['level']-1])[0]
            eligible=[u['id'] for u in ROSTER if u['cost']==cost and room['pool'][u['id']]>0 and u['id'] not in locked]
            if eligible:
                choice=random.choices(eligible,weights=[room['pool'][u] for u in eligible])[0]
                player['shop'][i]=choice
                room['pool'][choice]-=1

    def merge(self, room, player):
        for star in (1,2):
            for unit_id in DEFS:
                slots=[(area,i) for area in (player['board'],player['bench']) for i,u in enumerate(area) if u and u['id']==unit_id and u['star']==star]
                while len(slots)>=3:
                    three,slots=slots[:3],slots[3:]
                    equipment=[item for area,i in three for item in area[i].get('items',[])]
                    for area,i in three:
                        area[i]=None
                    area,i=three[0]
                    area[i]={'id':unit_id,'star':star+1,'items':equipment[:2]}
                    if equipment[2:]:
                        player['inventory'].extend(equipment[2:])
                    if equipment:
                        player['inventoryRevision']+=1
        locked={u['id'] for u in player['board']+player['bench'] if u and u['star']==3}
        for i,u in enumerate(player['shop']):
            if u in locked:
                room['pool'][u]+=1
                player['shop'][i]=None

    def action(self, session, data):
        room=self.rooms.get(session['room'])
        require(room is not None and room['phase']=='prepare', '현재 준비 단계가 아닙니다.')
        player=next(p for p in room['players'] if p['id']==session['id'])
        action=data.get('action')
        if action=='ready':
            player['ready']=not player['ready']
        else:
            require(not player['ready'], '준비 완료를 취소한 뒤 변경하세요.')
            if action=='buy':
                i=index(data.get('slot'),5)
                unit_id=player['shop'][i]
                require(unit_id is not None,'빈 상점 슬롯입니다.')
                cost=DEFS[unit_id]['cost']
                require(player['gold']>=cost,'골드가 부족합니다.')
                require(None in player['bench'],'대기석이 가득 찼습니다.')
                player['bench'][player['bench'].index(None)]={'id':unit_id,'star':1,'items':[]}
                player['gold']-=cost
                player['shop'][i]=None
                self.merge(room,player)
            elif action=='reroll':
                require(player['gold']>=2,'골드가 부족합니다.')
                player['gold']-=2
                self.roll(room,player)
            elif action=='xp':
                require(player['gold']>=4 and player['level']<9,'경험치를 구매할 수 없습니다.')
                player['gold']-=4
                self.add_xp(player,4)
            elif action in ('equip','combine_items'):
                self.equipment_action(player,data)
            elif action in ('move','sell'):
                area=data.get('area')
                require(area in ('board','bench'),'잘못된 영역입니다.')
                source=player[area]
                i=index(data.get('slot'),len(source))
                require(source[i] is not None,'빈 슬롯입니다.')
                if action=='sell':
                    unit=source[i]
                    copies=3**(unit['star']-1)
                    player['gold']+=DEFS[unit['id']]['cost']*copies
                    room['pool'][unit['id']]+=copies
                    if unit.get('items'):
                        player['inventory'].extend(unit['items'])
                        player['inventoryRevision']+=1
                    source[i]=None
                else:
                    target_area=data.get('targetArea')
                    require(target_area in ('board','bench'),'잘못된 영역입니다.')
                    target=player[target_area]
                    j=index(data.get('targetSlot'),len(target))
                    count=sum(u is not None for u in player['board'])
                    increase=area=='bench' and target_area=='board' and target[j] is None
                    require(not increase or count<player['level'],'배치 한도를 초과합니다.')
                    source[i],target[j]=target[j],source[i]
            else:
                raise Rejected('알 수 없는 행동입니다.')
        if all(p['ready'] for p in room['players']):
            self.fight(room)

    @staticmethod
    def equipment_action(player, data):
        """Validate the entire request before consuming anything; ignore client item IDs."""
        revision=data.get('inventoryRevision')
        require(type(revision) is int and revision==player['inventoryRevision'],
                '장비 목록이 변경되었습니다. 현재 목록에서 다시 선택하세요.')
        inventory=player['inventory']
        i=index(data.get('itemSlot'),len(inventory))
        item=inventory[i]
        if data['action']=='combine_items':
            j=index(data.get('targetItemSlot'),len(inventory))
            require(i!=j,'서로 다른 두 재료 슬롯을 선택하세요.')
            completed=combine(item,inventory[j])
            require(completed is not None,'기본 재료 2개만 합성할 수 있습니다.')
            for slot in sorted((i,j),reverse=True):
                inventory.pop(slot)
            inventory.append(completed)
        else:
            area=data.get('area')
            require(area in ('board','bench'),'잘못된 영역입니다.')
            slot=index(data.get('slot'),len(player[area]))
            unit=player[area][slot]
            require(unit is not None,'장비를 장착할 유닛이 없습니다.')
            equipped=list(unit.get('items',[]))
            if item==14:
                require(equipped,'회수할 장비가 없습니다.')
                inventory.pop(i)
                inventory.extend(equipped)
                unit['items']=[]
            else:
                partner=next((j for j,value in enumerate(equipped) if value<4),None) if item<4 else None
                require(partner is not None or len(equipped)<2,'장비 슬롯이 가득 찼습니다.')
                if partner is not None:
                    equipped[partner]=combine(equipped[partner],item)
                else:
                    equipped.append(item)
                unit['items']=equipped
                inventory.pop(i)
        player['inventoryRevision']+=1

    @staticmethod
    def add_xp(player, amount):
        player['xp']+=amount
        while player['level']<9 and player['xp']>=XP[player['level']]:
            player['xp']-=XP[player['level']]
            player['level']+=1
        if player['level']==9:
            player['xp']=0

    def fight(self, room):
        if room['phase']!='prepare':
            return
        fighters=[]
        for side,player in enumerate(room['players']):
            for slot,unit in enumerate(player['board']):
                if not unit:
                    continue
                definition=DEFS[unit['id']]
                hp=SKILLS[unit['id']]['baseHealth']*1.8**(unit['star']-1)
                fighters.append(dict(key=len(fighters),side=side,slot=slot,id=unit['id'],star=unit['star'],x=float(slot%7 if side==0 else 6-slot%7),
                                     y=float(slot//7+4 if side==0 else 3-slot//7),hp=hp,maxHp=hp,cooldown=0,items=list(unit.get('items',[]))))
        frames, skill_events, playback_duration = simulate(fighters, DEFS)
        totals=[sum(f['hp'] for f in fighters if f['side']==side) for side in (0,1)]
        winner=-1 if abs(totals[0]-totals[1])<.001 else int(totals[1]>totals[0])
        room.update(phase='battle',deadline=self.clock()+playback_duration,frames=frames,skillEvents=skill_events,battleDuration=playback_duration,roundWinner=winner)

    def settle(self, room):
        if room['phase']!='battle':
            return
        winner=room['roundWinner']
        for side,p in enumerate(room['players']):
            if side!=winner:
                p['hp']=max(0,p['hp']-(15 if winner<0 else 20+room['round']*2))
            p['gold']+=5+min(5,p['gold']//10)+(1 if side==winner else 0)
            self.add_xp(p,2)
            p['ready']=False
        room['message']='무승부 · 양쪽 체력 감소' if winner<0 else room['players'][winner]['name']+' 라운드 승리'
        if any(p['hp']==0 for p in room['players']) or room['round']>=10:
            a,b=room['players']
            self.finish(room,'' if a['hp']==b['hp'] else (a if a['hp']>b['hp'] else b)['id'])
        else:
            room.update(phase='prepare',round=room['round']+1,deadline=self.clock()+40)
            for p in room['players']:
                # Equal supplies for both sides; no client-controlled loot rolls.
                p['inventory'].append((room['round']-2)%4)
                if room['round']%3==0:
                    p['inventory'].append(14)
                p['inventoryRevision']+=1
                self.roll(room,p)

    def finish(self, room, winner):
        if room['phase']=='finished':
            return
        room.update(phase='finished',winner=winner,deadline=self.clock(),finishedAt=self.clock())
        if room['mode']=='ranked':
            a,b=room['players']
            score=.5 if not winner else float(winner==a['id'])
            delta=round(32*(score-1/(1+10**((b['rating']-a['rating'])/400))))
            room['ratingDelta']=delta
            with self.db:
                for p,change in ((a,delta),(b,-delta)):
                    self.db.execute('UPDATE accounts SET rating=rating+? WHERE id=?',(change,p['id']))
                    p['rating']+=change
                    token=self.accounts.get(p['id'])
                    if token in self.sessions:
                        self.sessions[token]['rating']=p['rating']

    def tick(self):
        now=self.clock()
        for session in list(self.sessions.values()):
            if session['queue'] and now-session['seen']>20:
                self.cancel(session)
        for room in list(self.rooms.values()):
            if room['phase']=='finished':
                if now-room['finishedAt']>600:
                    for token in room['tokens']:
                        if token in self.sessions and self.sessions[token]['room']==room['id']:
                            self.sessions[token]['room']=''
                    del self.rooms[room['id']]
                continue
            absent=[i for i,t in enumerate(room['tokens']) if now-self.sessions[t]['seen']>60]
            if absent:
                self.finish(room,'' if len(absent)==2 else room['players'][1-absent[0]]['id'])
                room['message']='연결 종료로 경기가 종료되었습니다.'
            elif now>=room['deadline']:
                self.fight(room) if room['phase']=='prepare' else self.settle(room)
        for token,session in list(self.sessions.items()):
            if not session['room'] and not session['queue'] and now-session['seen']>86400:
                self.accounts.pop(session['id'],None)
                del self.sessions[token]

    @staticmethod
    def units(area):
        return [dict(id=u['id'],star=u['star'],slot=i,items=list(u.get('items',[]))) for i,u in enumerate(area) if u]

    def snapshot(self, session):
        response=dict(token=session['token'],name=session['name'],rating=session['rating'],queue=session['queue'],
                      waiting=len(self.queues.get(session['queue'],[])),room=None,error='')
        room=self.rooms.get(session['room'])
        if room:
            side=next(i for i,p in enumerate(room['players']) if p['id']==session['id'])
            players=[]
            for i,p in enumerate(room['players']):
                players.append(dict(name=p['name'],rating=p['rating'],hp=p['hp'],gold=p['gold'] if i==side else 0,
                                    level=p['level'],xp=p['xp'] if i==side else 0,ready=p['ready'],board=self.units(p['board']),
                                    bench=self.units(p['bench']) if i==side else [],shop=[u or '' for u in p['shop']] if i==side else [],
                                    inventory=list(p['inventory']) if i==side else [],inventoryRevision=p['inventoryRevision'] if i==side else 0))
            result='' if room['phase']!='finished' else ('무승부' if not room['winner'] else ('승리' if room['winner']==session['id'] else '패배'))
            response['room']=dict(id=room['id'],mode=room['mode'],phase=room['phase'],round=room['round'],side=side,
                                  remaining=max(0,room['deadline']-self.clock()),battleDuration=room.get('battleDuration',8),skillEvents=room.get('skillEvents',[]) if room['phase']=='battle' else [],players=players,result=result,message=room['message'],
                                  ratingDelta=room['ratingDelta']*(1 if side==0 else -1),frames=room['frames'] if room['phase']=='battle' else [])
        return response

    def request(self, path, data, token=''):
        with self.lock:
            self.tick()
            if path=='/login':
                return self.login(data)
            require(token in self.sessions,'인증이 만료되었습니다. 다시 접속하세요.')
            session=self.sessions[token]
            session['seen']=self.clock()
            if path=='/state':
                return self.snapshot(session)
            if path=='/queue':
                mode=data.get('mode')
                require(mode in self.queues,'일반 또는 랭크 모드를 선택하세요.')
                require(not session['room'],'진행 중인 경기를 먼저 종료하세요.')
                self.cancel(session)
                session['queue']=mode
                self.queues[mode].append(token)
                self.match(mode)
            elif path=='/cancel':
                self.cancel(session)
            elif path=='/leave':
                self.cancel(session)
                room=self.rooms.get(session['room'])
                if room and room['phase']!='finished':
                    self.finish(room,next(p['id'] for p in room['players'] if p['id']!=session['id']))
                    room['message']='상대가 경기를 포기했습니다.'
                session['room']=''
            elif path=='/action':
                require(self.clock()-session['last_action']>=.08,'잠시 후 다시 시도하세요.')
                session['last_action']=self.clock()
                self.action(session,data)
            else:
                raise Rejected('존재하지 않는 API입니다.')
            return self.snapshot(session)

class Handler(BaseHTTPRequestHandler):
    def log_message(self, *args):
        pass  # Do not write bearer credentials or guest keys to logs.

    def do_GET(self):
        self.reply(200,{'status':'ok','service':'Dittoches Multi'}) if self.path=='/health' else self.reply(404,{'error':'Not found'})

    def reply(self, status, data):
        raw=json.dumps(data,ensure_ascii=False).encode('utf-8')
        self.send_response(status)
        self.send_header('Content-Type','application/json; charset=utf-8')
        self.send_header('Content-Length',str(len(raw)))
        self.send_header('Cache-Control','no-store')
        self.end_headers()
        self.wfile.write(raw)

    def do_POST(self):
        try:
            self.connection.settimeout(5)
            length=int(self.headers.get('Content-Length','0'))
            require(0<length<=8192,'요청 크기가 잘못되었습니다.')
            data=json.loads(self.rfile.read(length))
            require(isinstance(data,dict),'JSON 객체가 필요합니다.')
            token=self.headers.get('Authorization','').removeprefix('Bearer ')
            self.reply(200,self.server.game.request(self.path,data,token))
        except (Rejected,ValueError,TypeError) as error:
            self.reply(400,{'error':str(error)})
        except Exception:
            self.reply(500,{'error':'서버 처리 오류입니다.'})

def serve(host, port, database):
    game=Game(database)
    server=ThreadingHTTPServer((host,port),Handler)
    server.game=game
    stop=threading.Event()
    def timer():
        while not stop.wait(.25):
            with game.lock:
                game.tick()
    threading.Thread(target=timer,daemon=True).start()
    print(f'Dittoches Multi listening on {host}:{port}',flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        stop.set()
        server.server_close()

if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('--host',default='127.0.0.1')
    parser.add_argument('--port',type=int,default=7777)
    parser.add_argument('--database',default=str(ROOT/'ratings.sqlite3'))
    args=parser.parse_args()
    serve(args.host,args.port,args.database)
