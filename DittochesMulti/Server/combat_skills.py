"""Canonical skill simulation. Shared catalog is also a Unity Resources asset.

Damage, target selection and interruption are resolved here, never by a client.
Amounts and control durations are autochess balancing choices, not anime claims.
"""
import math
import combat_builds as builds
import combat_stats as stats
from combat_stats import SKILLS

def contains(shape, sx, sy, tx, ty, x, y, radius, reach):
    dx, dy = x-sx, y-sy
    if shape == 'radial':
        return dx*dx+dy*dy <= radius*radius
    if shape == 'splash':
        return (x-tx)**2+(y-ty)**2 <= radius*radius
    ax, ay = tx-sx, ty-sy
    length = math.hypot(ax, ay)
    if length < .0001:
        ax, ay, length = 0, 1, 1
    ax, ay = ax/length, ay/length
    forward, side = dx*ax+dy*ay, abs(dx*ay-dy*ax)
    return -.15 <= forward <= reach and side <= radius*(max(.2, forward/reach) if shape == 'cone' else 1)


def duration(skill):
    return skill['windup']+skill['travel']+(skill['shots']-1)*skill['interval']+skill['recovery']


def begin_cast(fighter, target, now, serial, damage=None):
    skill = SKILLS[fighter['id']]
    fighter['mana'] = 0
    fighter['cooldown'] = duration(skill)
    fighter['castUntil'] = now+duration(skill)
    return dict(serial=serial, caster=fighter['key'], target=target['key'], id=fighter['id'],
                started=now, sx=fighter['x'], sy=fighter['y'], tx=target['x'], ty=target['y'],
                released=False, cancelled=False, hits=0, damage=stats.skill_damage(fighter) if damage is None else damage)


def advance_cast(cast, fighters, now):
    """Advance every due hit, including when a fixed step crosses several impacts."""
    skill = SKILLS[cast['id']]
    by_key = {f['key']: f for f in fighters}
    source = by_key[cast['caster']]
    age = now-cast['started']
    target = by_key.get(cast['target'])
    if cast['cancelled']:
        return
    if not cast['released']:
        if source['hp'] <= 0 or source.get('stun', 0) > 0:
            cast['cancelled'] = True
            source['castUntil'] = now
            return
        if target is None or target['hp'] <= 0:
            enemies = [f for f in fighters if f['hp'] > 0 and f['side'] != source['side']]
            target = min(enemies, key=lambda f: ((f['x']-source['x'])**2+(f['y']-source['y'])**2, f['key']), default=None)
            if target is None:
                cast['cancelled'] = True
                source['castUntil'] = now
                return
            cast['target'] = target['key']
        cast.update(sx=source['x'], sy=source['y'], tx=target['x'], ty=target['y'])
        if age+1e-6 >= skill['windup']:
            cast['released'] = True
    if cast['hits'] == 0 and target and target['hp'] > 0 and skill['shape'] in ('single', 'splash'):
        cast.update(tx=target['x'], ty=target['y'])
    impact = skill['windup']+skill['travel']
    due = 0 if age+1e-5 < impact else (skill['shots'] if not skill['interval'] else min(skill['shots'], 1+math.floor((age-impact+1e-5)/skill['interval'])))
    while cast['hits'] < due:
        victims = []
        for f in fighters:
            if f['hp'] <= 0 or f['side'] == source['side']:
                continue
            inside = f['key'] == cast['target'] if skill['shape'] == 'single' else contains(
                skill['shape'], cast['sx'], cast['sy'], cast['tx'], cast['ty'], f['x'], f['y'], skill['radius'], skill['reach'])
            if inside:
                victims.append(f)
        victims.sort(key=lambda f: ((f['x']-cast['tx'])**2+(f['y']-cast['ty'])**2, f['key']))
        for f in victims[:skill['targets']]:
            builds.damage(source, f, cast['damage']/skill['shots'],damage_type=skill['damageType'])
            f['stun'] = max(f.get('stun', 0), skill['stun'])
            f['hitAt'] = now
            if f['hp'] > 0 and skill['visual'] == 'gate':
                dx,dy=cast['tx']-f['x'],cast['ty']-f['y']
                distance=math.hypot(dx,dy)
                if distance:
                    shift=min(.45,distance)/distance
                    f['x']+=dx*shift
                    f['y']+=dy*shift
        cast['hits'] += 1


def simulate(fighters, definitions):
    """50 ms simulation, 200 ms snapshots, explicit events for reconnect-safe playback."""
    frames, casts = [], []
    step_time = .05
    serial = 0
    for f in fighters:
        skill=SKILLS[f['id']]
        f.update(mana=skill['startMana'], maxMana=skill['maxMana'], stun=0, castUntil=0, attackAt=-10, hitAt=-10, target=-1)
    builds.initialize(fighters)
    for f in fighters:
        f.update(attackDamage=stats.attack(f),abilityPower=stats.ability_power(f),armor=builds.value(f,'armor'),magicResist=builds.value(f,'magicResist'),attackRange=SKILLS[f['id']]['attackRange'])
    for step in range(481):
        now = step*step_time
        if step:
            for f in fighters:
                f['stun'] = max(0, f['stun']-step_time)
            for cast in casts:
                advance_cast(cast, fighters, now)
            hits = []
            for f in fighters:
                if f['hp'] <= 0:
                    continue
                builds.tick(f, step_time)
                f['cooldown'] -= step_time
                role=definitions[f['id']]['role']
                f['mana'] = min(f['maxMana'], f['mana']+step_time*(2 if role=='마법사' else 1.5 if role=='지원' else 0))
                if f['stun'] > 0 or f['castUntil'] > now:
                    continue
                enemies = [e for e in fighters if e['side'] != f['side'] and e['hp'] > 0]
                if not enemies:
                    continue
                target = next((e for e in enemies if e['key'] == f['target']), None)
                if target is None:
                    target = min(enemies, key=lambda e: ((e['x']-f['x'])**2+(e['y']-f['y'])**2, e['key']))
                f['target'] = target['key']
                dx, dy = target['x']-f['x'], target['y']-f['y']
                distance = math.hypot(dx, dy)
                definition = definitions[f['id']]
                skill=SKILLS[f['id']]
                if not stats.in_attack_range(f,target):
                    f['x'] += dx/distance*step_time*1.5
                    f['y'] += dy/distance*step_time*1.5
                    continue
                if f['cooldown'] > 0:
                    continue
                damage = stats.attack(f)
                if f['mana'] >= f['maxMana']:
                    serial += 1
                    casts.append(begin_cast(f, target, now, serial))
                    builds.on_cast(f, fighters)
                else:
                    f['attacks'] += 1
                    damage *= 1+(builds.value(f, 'thirdHit') if f['attacks']%3 == 0 else 0)
                    hits.append((f, target, damage))
                    f['attackAt'] = now
                    mana_gain=5 if role=='탱커' else 7 if role=='마법사' else 8 if role=='지원' else 10
                    f['mana'] = min(f['maxMana'], f['mana']+mana_gain+builds.value(f, 'manaOnAttack'))
                    f['cooldown'] = 1/(skill['attackSpeed']*(1+builds.value(f, 'speed')))
            for source, target, damage in hits:
                builds.damage(source, target, damage, basic=True)
                target['hitAt'] = now
                if target['hp'] > 0:
                    target['mana'] = min(target['maxMana'], target['mana']+5)
        living_sides = {f['side'] for f in fighters if f['hp'] > 0}
        projectiles = any(c['released'] and not c['cancelled'] and c['hits'] < SKILLS[c['id']]['shots'] for c in casts)
        finished = len(living_sides) < 2 and not projectiles
        if step % 4 == 0 or finished or step == 480:
            frames.append(dict(time=now, units=[{k: v for k, v in f.items() if k not in ('cooldown', 'castUntil', 'build', 'regenClock', 'lowShieldUsed', 'items')} for f in fighters]))
        if finished:
            break
    events = [{k: v for k, v in c.items() if k not in ('damage', 'hits')} for c in casts if not c['cancelled']]
    return frames, events, max(2, now+.65)
