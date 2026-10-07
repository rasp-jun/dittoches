"""Dittoches Multi prototype. Python 3.10+, standard library only."""
import argparse
import copy
import hashlib
import json
import math
import random
import secrets
import sys
import sqlite3
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from combat_skills import simulate
from combat_builds import combine, ITEMS
from combat_stats import SKILLS

ROOT = Path(__file__).resolve().parent
def formation_limit(player):
    items=list(player.get('inventory', []))
    for unit in player['board']+player['bench']:
        if unit:items.extend(unit.get('items', []))
    return min(28,player['level']+sum(ITEMS[i].get('teamSize',0) for i in items))
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
                    board=[None]*28, bench=[None]*9, shop=[None]*5, ready=False, shopLocked=False,
                    inventory=[0,1,2,3,14,15,16],inventoryRevision=0,tamerX=3.,tamerY=7.,
                    **session.get('cosmetics',dict(tamer=0,field=0,finisher=0)))

    @staticmethod
    def cosmetics(data):
        result={}
        for key,limit in (('tamer',4),('field',3),('finisher',3)):
            value=data.get(key,0)
            require(type(value) is int and 0<=value<limit,'잘못된 외형 선택입니다.')
            result[key]=value
        return result

    def move_tamer(self, session, data):
        room=self.rooms.get(session['room'])
        require(room is not None and room['phase'] in ('prepare','battle'),'지금은 테이머를 이동할 수 없습니다.')
        x,y=data.get('x'),data.get('y')
        require(type(x) in (int,float) and type(y) in (int,float) and math.isfinite(x) and math.isfinite(y)
                and 0<=x<=6 and 4<=y<=7,'자신의 전장 안에서만 이동할 수 있습니다.')
        require(self.clock()-session.get('last_tamer_move',0)>=.08,'잠시 후 다시 이동하세요.')
        player=next(p for p in room['players'] if p['id']==session['id'])
        session['last_tamer_move']=self.clock();player['tamerX']=float(x);player['tamerY']=float(y)

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
                    self.merge_slots(player,three,unit_id,star)
        locked={u['id'] for u in player['board']+player['bench'] if u and u['star']==3}
        for i,u in enumerate(player['shop']):
            if u in locked:
                room['pool'][u]+=1
                player['shop'][i]=None

    @staticmethod
    def merge_slots(player, slots, unit_id, star):
        equipment=[item for area,i in slots for item in area[i].get('items',[])]
        for area,i in slots:
            area[i]=None
        area,i=slots[0]
        kept=[];returned=[];grants=set()
        for item in equipment:
            grant=ITEMS[item].get('grantsTrait')
            if len(kept)>=2 or grant and grant in grants:returned.append(item)
            else:
                kept.append(item)
                if grant:grants.add(grant)
        area[i]={'id':unit_id,'star':star+1,'items':kept}
        player['inventory'].extend(returned)
        if equipment:
            player['inventoryRevision']+=1

    def action(self, session, data):
        room=self.rooms.get(session['room'])
        action=data.get('action')
        if action=='shop_lock':
            require(room is not None and room['phase'] in ('prepare','battle'),'지금은 상점을 잠글 수 없습니다.')
            require(type(data.get('shopLocked')) is bool,'상점 잠금 상태가 잘못되었습니다.')
            player=next(p for p in room['players'] if p['id']==session['id'])
            # Explicit state makes a repeated request idempotent; never reroll here.
            player['shopLocked']=data['shopLocked']
            return
        require(room is not None and room['phase'] in ('prepare','battle'), '지금은 변경할 수 없습니다.')
        combat=room['phase']=='battle'
        player=next(p for p in room['players'] if p['id']==session['id'])
        if action=='ready':
            require(not combat, '전투 중에는 준비 상태를 변경할 수 없습니다.')
            player['ready']=not player['ready']
        else:
            require(combat or not player['ready'], '준비 완료를 취소한 뒤 변경하세요.')
            if action=='buy':
                i=index(data.get('slot'),5)
                unit_id=player['shop'][i]
                require(unit_id is not None,'빈 상점 슬롯입니다.')
                cost=DEFS[unit_id]['cost']
                require(player['gold']>=cost,'골드가 부족합니다.')
                pair=[(area,j) for area in (player['board'],player['bench']) for j,u in enumerate(area)
                      if u and u['id']==unit_id and u['star']==1][:2]
                require(None in player['bench'] or len(pair)==2,'대기석이 가득 찼습니다.')
                purchased={'id':unit_id,'star':1,'items':[]}
                if None in player['bench']:
                    player['bench'][player['bench'].index(None)]=purchased
                else:
                    # A virtual third copy preserves board priority without extending the bench.
                    self.merge_slots(player,pair+[([purchased],0)],unit_id,1)
                player['gold']-=cost
                player['shop'][i]=None
                self.merge(room,player)
                if combat:
                    self.sync_combat_equipment(room,player)
            elif action=='reroll':
                require(player['gold']>=2,'골드가 부족합니다.')
                player['gold']-=2
                self.roll(room,player)
            elif action=='xp':
                require(player['gold']>=4 and player['level']<9,'경험치를 구매할 수 없습니다.')
                player['gold']-=4
                self.add_xp(player,4)
            elif action in ('equip','combine_items'):
                live_equip=combat and action=='equip' and data.get('area')=='board'
                if live_equip:
                    require(len(room.get('equipmentEvents',[]))<256,'이번 전투의 장비 변경 한도에 도달했습니다.')
                self.equipment_action(player,data)
                if live_equip:
                    self.update_combat_equipment(room,player,data['slot'])
            elif action in ('move','sell'):
                area=data.get('area')
                require(area in ('board','bench'),'잘못된 영역입니다.')
                require(not combat or area=='bench', '전투 중인 전장 유닛은 이동·판매할 수 없습니다.')
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
                    require(not combat or target_area=='bench','전투 중에는 대기석 안에서만 이동할 수 있습니다.')
                    target=player[target_area]
                    j=index(data.get('targetSlot'),len(target))
                    count=sum(u is not None for u in player['board'])
                    increase=area=='bench' and target_area=='board' and target[j] is None
                    require(not increase or count<formation_limit(player),'배치 한도를 초과합니다.')
                    source[i],target[j]=target[j],source[i]
            else:
                raise Rejected('알 수 없는 행동입니다.')
        if not combat and all(p['ready'] for p in room['players']):
            self.fight(room)

    def sync_combat_equipment(self,room,player):
        side=room['players'].index(player)
        changes=[]
        for fighter in room['initialFighters']:
            if fighter['side']!=side:continue
            unit=player['board'][fighter['slot']]
            desired=list(unit.get('items',[])) if unit and unit['id']==fighter['id'] else []
            previous=next((e['items'] for e in reversed(room['equipmentEvents']) if e['side']==side and e['slot']==fighter['slot']),fighter.get('items',[]))
            if desired!=previous:changes.append((fighter['slot'],desired))
        when=(math.floor(max(0,self.clock()-room['battleStartedAt'])/.2)+1)*.2
        for slot,items in changes:self.update_combat_equipment(room,player,slot,items,when,False)
        if changes:self.recompute_combat(room)

    def update_combat_equipment(self,room,player,slot,items=None,when=None,recompute=True):
        side=room['players'].index(player)
        fighter=next((f for f in room['initialFighters'] if f['side']==side and f['slot']==slot),None)
        if fighter is None:return
        if items is None:items=player['board'][slot].get('items',[])
        # Apply at the next 200 ms boundary. All already displayed snapshots stay
        # unchanged, and deterministic replay retains HP, deaths, casts and timers.
        elapsed=max(0,self.clock()-room['battleStartedAt'])
        if when is None:when=(math.floor(elapsed/.2)+1)*.2
        room['equipmentEvents'].append(dict(time=when,side=side,slot=slot,id=fighter['id'],items=list(items)))
        if recompute:self.recompute_combat(room)

    def recompute_combat(self,room):
        fighters=copy.deepcopy(room['initialFighters'])
        frames,events,duration=simulate(fighters,DEFS,room['equipmentEvents'])
        totals=[sum(f['hp'] for f in fighters if f['side']==s) for s in (0,1)]
        winner=-1 if abs(totals[0]-totals[1])<.001 else int(totals[1]>totals[0])
        room.update(frames=frames,skillEvents=events,battleDuration=duration,
                    deadline=room['battleStartedAt']+duration,roundWinner=winner)

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
            require(completed is not None,'합성 가능한 재료 2개를 선택하세요.')
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
                partner=next((j for j,value in enumerate(equipped) if combine(value,item) is not None),None)
                require(partner is not None or len(equipped)<2,'장비 슬롯이 가득 찼습니다.')
                if partner is not None:
                    equipped[partner]=combine(equipped[partner],item)
                else:
                    equipped.append(item)
                from combat_builds import ITEMS, TRAITS
                granted=[ITEMS[value].get('grantsTrait') for value in equipped if ITEMS[value].get('grantsTrait')]
                require(len(granted)==len(set(granted)) and not any(t['id'] in granted and unit['id'] in t['members'] for t in TRAITS),
                        '이미 보유한 시너지입니다. 다른 아군에게 장착하세요.')
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
        room.update(initialFighters=copy.deepcopy(fighters),equipmentEvents=[],battleStartedAt=self.clock())
        frames, skill_events, playback_duration = simulate(fighters, DEFS)
        totals=[sum(f['hp'] for f in fighters if f['side']==side) for side in (0,1)]
        winner=-1 if abs(totals[0]-totals[1])<.001 else int(totals[1]>totals[0])
        room.update(phase='battle',deadline=room['battleStartedAt']+playback_duration,frames=frames,skillEvents=skill_events,battleDuration=playback_duration,roundWinner=winner)

    def settle(self, room):
        if room['phase']!='battle':
            return
        # Only publish a completed round. The report survives preparation/reconnects
        # without exposing inventory, bench, internal bonuses or future rounds.
        report_fields = ('key','side','slot','id','star','hp','maxHp','shield','mana','maxMana',
                         'damageDone','basicDamageDone','skillDamageDone','damageTaken','shieldAbsorbed',
                         'healingDone','shieldingDone','attacks','casts','combatAge','lowShieldUsed',
                         'crisisAt','friendshipActive','combatStatsVersion','attackDamage','abilityPower','armor','magicResist','attackRange','attackSpeed')
        final = room['frames'][-1]['units'] if room['frames'] else []
        room['lastCombat'] = [{key:f[key] for key in report_fields if key in f} for f in final]
        room['reportRound'] = room['round']
        winner=room['roundWinner']
        room['reportWinner']=winner
        results=[]
        for side,p in enumerate(room['players']):
            hp_before=p['hp']
            if side!=winner:
                p['hp']=max(0,p['hp']-(15 if winner<0 else 20+room['round']*2))
            interest=min(5,p['gold']//10)
            win_bonus=1 if side==winner else 0
            income=5+interest+win_bonus
            p['gold']+=income
            results.append(dict(version=1,round=room['round'],winner=winner,hpBefore=hp_before,hpAfter=p['hp'],
                                healthLost=hp_before-p['hp'],baseIncome=5,interest=interest,winBonus=win_bonus,income=income))
            self.add_xp(p,2)
            p['ready']=False
        for side,result in enumerate(results):
            result['opponentHealthLost']=results[1-side]['healthLost']
        room['roundResults']=results
        room['message']='무승부 · 양쪽 체력 감소' if winner<0 else room['players'][winner]['name']+' 라운드 승리'
        if any(p['hp']==0 for p in room['players']) or room['round']>=10:
            a,b=room['players']
            self.finish(room,'' if a['hp']==b['hp'] else (a if a['hp']>b['hp'] else b)['id'])
        else:
            room.update(phase='prepare',round=room['round']+1,deadline=self.clock()+40)
            for p in room['players']:
                # Equal supplies for both sides; no client-controlled loot rolls.
                p['inventory'].append((room['round']-2)%4)
                if room['round'] in (3,5,7,9):p['inventory'].append(15 if room['round'] in (3,7) else 16)
                if room['round']%3==0:
                    p['inventory'].append(14)
                p['inventoryRevision']+=1
                if not p.get('shopLocked',False):
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
                      waiting=len(self.queues.get(session['queue'],[])),room=None,error='',reliableCommands=1,combatActions=1)
        room=self.rooms.get(session['room'])
        if room:
            side=next(i for i,p in enumerate(room['players']) if p['id']==session['id'])
            players=[]
            for i,p in enumerate(room['players']):
                age=max(0,self.clock()-self.sessions[room['tokens'][i]]['seen'])
                players.append(dict(connectionKnown=True,connected=age<8,reconnectRemaining=max(0,60-age),name=p['name'],rating=p['rating'],hp=p['hp'],gold=p['gold'] if i==side else 0,
                                    tamer=p.get('tamer',0),field=p.get('field',0),finisher=p.get('finisher',0),tamerX=p.get('tamerX',3.),tamerY=p.get('tamerY',7.),
                                    level=p['level'],xp=p['xp'] if i==side else 0,ready=p['ready'],board=self.units(p['board']),
                                    bench=self.units(p['bench']) if i==side else [],shop=[u or '' for u in p['shop']] if i==side else [],
                                    shopLocked=p.get('shopLocked',False) if i==side else False,
                                    inventory=list(p['inventory']) if i==side else [],inventoryRevision=p['inventoryRevision'] if i==side else 0))
            result='' if room['phase']!='finished' else ('무승부' if not room['winner'] else ('승리' if room['winner']==session['id'] else '패배'))
            response['room']=dict(id=room['id'],mode=room['mode'],phase=room['phase'],round=room['round'],side=side,
                                  remaining=max(0,room['deadline']-self.clock()),battleDuration=room.get('battleDuration',8),skillEvents=room.get('skillEvents',[]) if room['phase']=='battle' else [],players=players,result=result,message=room['message'],
                                  ratingDelta=room['ratingDelta']*(1 if side==0 else -1),frames=room['frames'] if room['phase']=='battle' else [],
                                  lastCombat=room.get('lastCombat',[]),reportRound=room.get('reportRound',0),roundWinner=room.get('reportWinner',-1),
                                  roundResult=dict(room['roundResults'][side]) if room.get('roundResults') else None)
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
            request_id=data.get('requestId','')
            if not request_id:
                self.dispatch(session,path,data)
                return self.snapshot(session)
            require(isinstance(request_id,str) and len(request_id)==32 and all(c in '0123456789abcdef' for c in request_id),'요청 번호가 잘못되었습니다.')
            fingerprint=json.dumps([path,data],sort_keys=True,separators=(',',':'),ensure_ascii=False)
            receipts=session.setdefault('receipts',{})
            previous=receipts.get(request_id)
            if previous:
                require(previous[0]==fingerprint,'같은 요청 번호에 다른 명령을 보낼 수 없습니다.')
                if previous[1]:
                    raise Rejected(previous[1])
                response=self.snapshot(session)
                response.update(acknowledgedRequestId=request_id,duplicateRequest=True)
                return response
            error=''
            try:
                if path in ('/action','/tamer-move','/leave'):
                    require(data.get('expectedRoom','')==session['room'],'경기가 변경되었습니다. 최신 상태를 확인하세요.')
                    room=self.rooms.get(session['room'])
                    if room:
                        require(type(data.get('expectedRound')) is int and data['expectedRound']==room['round'] and data.get('expectedPhase')==room['phase'],'라운드가 변경되었습니다. 최신 상태에서 다시 선택하세요.')
                self.dispatch(session,path,data)
            except Rejected as failure:
                error=str(failure)
                raise
            finally:
                # Only serialize expected rejections. Unexpected failures must not be acknowledged.
                if error or sys.exc_info()[0] is None:
                    receipts[request_id]=(fingerprint,error)
                    while len(receipts)>256:
                        del receipts[next(iter(receipts))]
            response=self.snapshot(session)
            response.update(acknowledgedRequestId=request_id,duplicateRequest=False)
            return response

    def dispatch(self, session, path, data):
        token=session['token']
        if path=='/queue':
            mode=data.get('mode')
            require(mode in self.queues,'일반 또는 랭크 모드를 선택하세요.')
            require(not session['room'],'진행 중인 경기를 먼저 종료하세요.')
            cosmetics=self.cosmetics(data)
            self.cancel(session)
            session['cosmetics']=cosmetics
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
        elif path=='/tamer-move':
            self.move_tamer(session,data)
        elif path=='/action':
            require(self.clock()-session['last_action']>=.08,'잠시 후 다시 시도하세요.')
            session['last_action']=self.clock()
            self.action(session,data)
        else:
            raise Rejected('존재하지 않는 API입니다.')

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
