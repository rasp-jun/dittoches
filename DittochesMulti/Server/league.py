"""Eight-player league orchestration; each battle uses the existing duel simulator.

Player indices are stable throughout a league. Combat side 0/1 is private to a
duel and is translated to the league index only when publishing a snapshot.
"""
import copy
import math


class LeagueMixin:
    @staticmethod
    def is_league(room):
        return bool(room and room.get('league'))

    def schedule_league(self, room):
        alive = [i for i, p in enumerate(room['players']) if not p.get('placement')]
        # Circle rotation gives every pair a meeting before a repeat while the
        # roster is unchanged. A bye fights a ghost copy instead of skipping.
        ring = alive + ([-1] if len(alive) % 2 else [])
        for _ in range((room['round'] - 1) % max(1, len(ring) - 1)):
            ring = [ring[0], ring[-1]] + ring[1:-1]
        pairs, opponents, ghosts = [], {}, {}
        for a, b in zip(ring[:len(ring)//2], reversed(ring[len(ring)//2:])):
            if a == -1 or b == -1:
                real = b if a == -1 else a
                donors = [i for i in alive if i != real]
                if not donors:
                    continue
                donor = donors[(room['round'] - 1) % len(donors)]
                pairs.append((real, donor, True))
                opponents[real], ghosts[real] = donor, True
            else:
                pairs.append((a, b, False))
                opponents[a], opponents[b] = b, a
        room.update(pairings=pairs, opponents=opponents, ghostOpponents=ghosts)

    def league_fight(self, room):
        if room['phase'] != 'prepare':
            return
        self.schedule_league(room)
        battles = []
        started = self.clock()
        for a, b, ghost in room['pairings']:
            duel = dict(players=[room['players'][a], copy.deepcopy(room['players'][b]) if ghost else room['players'][b]],
                        phase='prepare', round=room['round'], leagueSides=[a, b], ghost=ghost)
            self.fight(duel)
            # All four battle timelines begin together, regardless of CPU time.
            duel['battleStartedAt'] = started
            duel['deadline'] = started + duel['battleDuration']
            battles.append(duel)
        room.update(phase='battle', battles=battles, battleStartedAt=started,
                    deadline=max((d['deadline'] for d in battles), default=started+1))

    @staticmethod
    def league_battle(room, side):
        return next((d for d in room.get('battles', [])
                     if side == d['leagueSides'][0] or (not d['ghost'] and side == d['leagueSides'][1])), None)

    def league_equipment(self, room, player, slot=None, items=None, when=None, recompute=True):
        side = next(i for i, p in enumerate(room['players']) if p is player)
        duel = self.league_battle(room, side)
        if duel is None:
            return
        if slot is None:
            self.sync_combat_equipment(duel, player)
        else:
            self.update_combat_equipment(duel, player, slot, items, when, recompute)
        room['deadline'] = max(d['deadline'] for d in room['battles'])

    def league_eliminate(self, room, sides):
        alive_count = sum(not p.get('placement') for p in room['players'])
        candidates = [i for i in set(sides) if not room['players'][i].get('placement')]
        candidates.sort(key=lambda i: (room['players'][i].get('rawHp', room['players'][i]['hp']),
                                       room['players'][i].get('hpBefore', room['players'][i]['hp']), -i))
        for side in candidates:
            p = room['players'][side]
            p.update(hp=0, placement=alive_count, ready=False)
            alive_count -= 1
            if not p.get('poolReleased'):
                for unit in p['board'] + p['bench']:
                    if unit:
                        room['pool'][unit['id']] += 3 ** (unit['star'] - 1)
                for unit_id in p['shop']:
                    if unit_id:
                        room['pool'][unit_id] += 1
                p['shop'] = [None] * 5
                p['poolReleased'] = True
        alive = [p for p in room['players'] if not p.get('placement')]
        if len(alive) <= 1:
            winner = alive[0] if alive else min(room['players'], key=lambda p: p['placement'])
            winner['placement'] = 1
            self.finish(room, winner['id'])

    def league_settle(self, room):
        if room['phase'] != 'battle':
            return
        results, reports = {}, {}
        for duel in room['battles']:
            sides, winner = duel['leagueSides'], duel['roundWinner']
            before_hps = [p['hp'] for p in duel['players']]
            losses = [15 if winner < 0 else (0 if local == winner else 20 + room['round'] * 2) for local in (0, 1)]
            report = self.league_frames(duel, last=True)
            for local, side in enumerate(sides):
                if duel['ghost'] and local == 1:
                    continue
                p = room['players'][side]
                if p.get('placement'):
                    continue
                before = p['hp']
                p['hpBefore'], p['rawHp'] = before, before-losses[local]
                p['hp'] = max(0, p['rawHp'])
                interest, win_bonus = min(5, p['gold']//10), int(local == winner)
                income = 5 + interest + win_bonus
                p['gold'] += income
                self.add_xp(p, 2)
                p['ready'] = False
                results[side] = dict(version=1, round=room['round'], winner=sides[winner] if winner >= 0 else -1,
                                     hpBefore=before, hpAfter=p['hp'], healthLost=before-p['hp'],
                                     opponentHealthLost=0 if duel['ghost'] else min(before_hps[1-local],losses[1-local]),
                                     baseIncome=5, interest=interest, winBonus=win_bonus, income=income)
                reports[side] = dict(units=report, winner=sides[winner] if winner >= 0 else -1, opponent=sides[1-local], round=room['round'])
        room.setdefault('leagueReports', {}).update(reports)
        room.setdefault('leagueResults', {}).update(results)
        self.league_eliminate(room, [i for i,p in enumerate(room['players']) if p['hp'] <= 0])
        if room['phase'] == 'finished':
            return
        # Hard cap guarantees a completed game even under repeated zero-damage fixtures.
        if room['round'] >= 30:
            alive = sorted((i for i,p in enumerate(room['players']) if not p.get('placement')),
                           key=lambda i: (room['players'][i]['hp'], -i))
            for side in alive[:-1]:
                self.league_eliminate(room, [side])
            return
        room.update(phase='prepare', round=room['round']+1, deadline=self.clock()+40,
                    message='라운드 종료 · 다음 상대와 편성을 확인하세요')
        for p in room['players']:
            if p.get('placement'):
                continue
            p['inventory'].append((room['round']-2)%4)
            if room['round'] in (3,5,7,9):
                p['inventory'].append(15 if room['round'] in (3,7) else 16)
            if room['round']%3 == 0:
                p['inventory'].append(14)
            p['inventoryRevision'] += 1
            if not p.get('shopLocked', False):
                self.roll(room, p)
        self.schedule_league(room)

    def league_finish(self, room, winner):
        if room['phase'] == 'finished':
            return
        room.update(phase='finished', winner=winner, deadline=self.clock(), finishedAt=self.clock())
        changes = [0]*len(room['players'])
        if room['mode'] == 'ranked8':
            for i, a in enumerate(room['players']):
                for j in range(i+1, len(room['players'])):
                    b = room['players'][j]
                    expected = 1/(1+10**((b['rating']-a['rating'])/400))
                    change = round(32/(len(changes)-1)*(int(a['placement'] < b['placement'])-expected))
                    changes[i] += change
                    changes[j] -= change
            with self.db:
                for p, change in zip(room['players'], changes):
                    self.db.execute('UPDATE accounts SET rating=rating+? WHERE id=?', (change, p['id']))
                    p['rating'] = self.db.execute('SELECT rating FROM accounts WHERE id=?',(p['id'],)).fetchone()[0]
                    token = self.accounts.get(p['id'])
                    if token in self.sessions:
                        self.sessions[token]['rating'] = p['rating']
        room['ratingDeltas'] = changes
        room['message'] = next(p['name'] for p in room['players'] if p['id']==winner)+' · 최종 1위'

    def league_tick(self, room, now):
        absent = [i for i,t in enumerate(room['tokens']) if not room['players'][i].get('placement') and now-self.sessions[t]['seen']>60]
        if absent:
            self.league_eliminate(room, absent)
            if room['phase'] == 'finished':
                return
            if room['phase'] == 'prepare':
                self.schedule_league(room)
        if now >= room['deadline']:
            self.fight(room) if room['phase']=='prepare' else self.settle(room)

    @staticmethod
    def league_frames(duel, last=False):
        frames = copy.deepcopy(duel.get('frames', []))
        for frame in frames:
            for unit in frame['units']:
                unit['side'] = duel['leagueSides'][unit['side']]
        return frames[-1]['units'] if last and frames else ([] if last else frames)

    def league_snapshot(self, room, side, response):
        view = response['room']
        player = room['players'][side]
        duel = self.league_battle(room, side)
        report = room.get('leagueReports', {}).get(side, {})
        opponent = room['opponents'].get(side, next((i for i,p in enumerate(room['players']) if not p.get('placement') and i!=side), (side+1)%len(room['players'])))
        view.update(opponentSide=opponent, battleSide=duel['leagueSides'].index(side) if duel else 0,
                    ghostOpponent=room.get('ghostOpponents', {}).get(side,False),
                    aliveCount=sum(not p.get('placement') or p.get('placement')==1 and room['phase']=='finished' for p in room['players']),
                    placement=player.get('placement',0), ratingDelta=room.get('ratingDeltas',[0]*8)[side],
                    frames=self.league_frames(duel) if duel and room['phase']=='battle' and not player.get('placement') else [],
                    skillEvents=duel.get('skillEvents',[]) if duel and room['phase']=='battle' and not player.get('placement') else [],
                    battleDuration=duel.get('battleDuration',8) if duel else 8,
                    battleElapsed=max(0,self.clock()-room.get('battleStartedAt',self.clock())),
                    lastCombat=report.get('units',[]), reportRound=report.get('round',0), roundWinner=report.get('winner',-1),
                    reportOpponentSide=report.get('opponent',opponent),
                    roundResult=room.get('leagueResults',{}).get(side),
                    result=(str(player['placement'])+'위') if player.get('placement') else '')
        for original, published in zip(room['players'],view['players']):
            published['placement'] = original.get('placement',0)
            published['eliminated'] = original.get('placement',0)>0 and room['phase']!='finished' or original.get('placement',0)>1
        return response
