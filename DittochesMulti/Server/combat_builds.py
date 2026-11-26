"""Trait and equipment rules from the same catalog used by the Unity client.

Teams are frozen at combat start. Bench units never enter this module. All
percentage bonuses add; reduction caps at 60%, shields at 50% of max health.
"""
import json
from pathlib import Path
from combat_stats import mitigate

DATA = json.loads((Path(__file__).resolve().parent.parent/'Assets/Resources/DigimonBuilds.json').read_text(encoding='utf-8'))
TRAITS, ITEMS = DATA['traits'], DATA['items']


def has_trait(trait, uid, items=()):
    return uid in trait['members'] or any(0 <= i < len(ITEMS) and ITEMS[i].get('grantsTrait') == trait['id'] for i in items)


def level(trait, ids):
    members = set()
    for u in ids:
        uid, items = (u['id'], u.get('items', ())) if isinstance(u, dict) else (u, ())
        if has_trait(trait, uid, items):
            members.add(uid)
    count = len(members)
    return sum(count >= t['count'] for t in trait['tiers'])


def resolve(unit_id, team_ids, equipment=()):
    equipment = list(equipment)
    bonus = {}
    def add(values):
        for key, value in values.items():
            bonus[key] = bonus.get(key, 0)+value
    ids = list(team_ids)
    for trait in TRAITS:
        tier = level(trait, ids)
        if not tier:
            continue
        active = trait['tiers'][tier-1]['bonus']
        if has_trait(trait, unit_id, equipment):
            add(active)
        add({'startShield': active.get('teamShield', 0), 'armor': active.get('teamResist', 0), 'magicResist': active.get('teamResist', 0)})
    for item in equipment:
        if type(item) is int and 0 <= item < len(ITEMS) and ITEMS[item]['kind'] != 'utility':
            add(ITEMS[item]['bonus'])
    equipped = min(3, sum(type(i) is int and 0 <= i < len(ITEMS) and ITEMS[i]['kind'] != 'utility' for i in equipment))
    for stat, per_item in (('hp', 'equippedHp'), ('attack', 'equippedAttack')):
        if bonus.get(per_item, 0):
            bonus[stat] = bonus.get(stat, 0) + bonus[per_item]*equipped
    if equipped == 3:
        bonus['castShield'] = bonus.get('castShield', 0)+bonus.get('fullGearShield', 0)
    return bonus


def combine(a, b):
    if not is_component(a) or not is_component(b):
        return None
    return next((i['id'] for i in ITEMS if sorted(i['recipe']) == sorted([a, b])), None)


def is_component(item):
    return type(item) is int and 0 <= item < len(ITEMS) and ITEMS[item]['kind'] == 'component'


def value(fighter, key):
    return fighter.get('build', {}).get(key, 0)


def shield(source, target, amount):
    if target['hp'] <= 0:
        return 0
    actual = max(0, min(target['maxHp']*.5-target.get('shield', 0), amount))
    target['shield'] = target.get('shield', 0)+actual
    source['shieldingDone'] = source.get('shieldingDone', 0)+actual
    return actual


def heal(source, target, amount):
    if target['hp'] <= 0:
        return 0
    actual = max(0, min(target['maxHp']-target['hp'], amount*(1+value(source, 'healPower'))))
    target['hp'] += actual
    source['healingDone'] = source.get('healingDone', 0)+actual
    return actual


def initialize(fighters):
    for f in fighters:
        f['build'] = resolve(f['id'], [u for u in fighters if u['side'] == f['side']], f.get('items', []))
        f['maxHp'] = (f['maxHp']+value(f, 'health')*1.8**(f['star']-1))*(1+value(f, 'hp'))
        f['hp'] = f['maxHp']
        f.update(attacks=0, casts=0, lowShieldUsed=False, crisisAt=-1, friendshipActive=value(f,'rampSpeed')>0, regenClock=0, combatAge=0, shield=0, damageDone=0,
                 basicDamageDone=0, skillDamageDone=0, damageTaken=0, shieldAbsorbed=0, healingDone=0, shieldingDone=0)
        f.update(burnRemaining=0, burnClock=0, burnRate=0, burnOwner=-1, shredRemaining=0, armorShred=0,
                 synergyUsed=0, synergyAt=-1, synergyKind=0, battleAge=0, revived=False, revivedAt=-1)
        f['mana'] = min(f['maxMana'], f['mana']+value(f, 'startMana'))
        shield(f, f, f['maxHp']*value(f, 'startShield'))


def tick(f, dt):
    if f['hp'] <= 0:
        return
    f['combatAge'] = f.get('combatAge', 0)+dt
    f['mana'] = min(f['maxMana'], f['mana']+value(f, 'manaRegen')*dt)
    f['regenClock'] = f.get('regenClock', 0)+dt
    while f['regenClock']+1e-8 >= 1:
        f['regenClock'] -= 1
        if value(f, 'regen'):
            heal(f, f, f['maxHp']*value(f, 'regen'))


def change_equipment(f, items, team_ids):
    """Change ongoing bonuses without restarting combat or granting opening effects.

    Preserve health percentage, so removing/re-equipping health cannot heal.
    A dead fighter stays dead; its next-round inventory is managed by the server.
    """
    if f['hp'] <= 0:
        return
    from combat_stats import SKILLS, attack_speed
    old_speed = attack_speed(f)
    fraction = f['hp']/f['maxHp']
    f['items'] = list(items)
    f['build'] = resolve(f['id'], team_ids, items)
    f['maxHp'] = (SKILLS[f['id']]['baseHealth']+value(f, 'health'))*1.8**(f['star']-1)*(1+value(f, 'hp'))
    f['hp'] = f['maxHp']*fraction
    f['shield'] = min(f.get('shield', 0), f['maxHp']*.5)
    if f.get('castUntil', 0) <= f.get('combatAge', 0):
        f['cooldown'] *= old_speed/attack_speed(f)


def on_cast(source, fighters):
    allies = [f for f in fighters if f['side'] == source['side'] and f['hp'] > 0]
    if source['hp'] <= 0:
        return
    if allies and value(source, 'castHeal'):
        target = min(allies, key=lambda f: (f['hp']/f['maxHp'], f['key']))
        heal(source, target, target['maxHp']*value(source, 'castHeal'))
    if value(source, 'castShield'):
        shield(source, source, source['maxHp']*value(source, 'castShield'))
    if allies and value(source, 'castAllyShield'):
        target = min(allies, key=lambda f: (f['hp']/f['maxHp'], f['key']))
        shield(source, target, target['maxHp']*value(source, 'castAllyShield'))
    source['mana'] = min(source['maxMana'], source['mana']+value(source, 'castMana'))
    recipients = sorted((f for f in allies if f is not source and f['mana'] < f['maxMana']),
                        key=lambda f: (f['mana']/f['maxMana'], f['key']))[:2]
    for target in recipients:
        target['mana'] = min(target['maxMana'], target['mana']+value(source, 'castAllyMana'))


def damage(source, target, amount, basic=False, damage_type='physical'):
    if target['hp'] <= 0:
        return 0
    amount = mitigate(amount,effective_armor(target),value(target,'magicResist'),damage_type)
    opening = value(target, 'openingReduction') if target.get('battleAge', 0) < 8-1e-6 else 0
    amount *= 1-min(.6, max(0, value(target, 'reduction')+opening))
    if target['hp'] >= target['maxHp']*.7:
        amount *= 1+value(source, 'highHealthDamage')
    if target['hp'] <= target['maxHp']*(.35+1e-7):
        amount *= 1+value(source, 'lowHealthDamage')
    absorbed = min(target.get('shield', 0), amount)
    target['shield'] = target.get('shield', 0)-absorbed
    actual = min(target['hp'], max(0, amount-absorbed))
    target['hp'] -= actual
    source['damageDone'] = source.get('damageDone', 0)+actual
    kind = 'basicDamageDone' if basic else 'skillDamageDone'
    source[kind] = source.get(kind, 0)+actual
    target['damageTaken'] = target.get('damageTaken', 0)+actual
    target['shieldAbsorbed'] = target.get('shieldAbsorbed', 0)+absorbed
    if target['hp'] <= 0 and source['hp'] > 0:
        source['mana'] = min(source['maxMana'], source['mana']+value(source, 'killMana'))
    if 0 < target['hp'] <= target['maxHp']*(.35+1e-7) and not target.get('lowShieldUsed', False) and (value(target, 'lowShield') or value(target, 'lowHeal')):
        target['lowShieldUsed'] = True
        target['crisisAt'] = target.get('combatAge',0)
        shield(target, target, target['maxHp']*value(target, 'lowShield'))
        heal(target, target, target['maxHp']*value(target, 'lowHeal'))
    if basic:
        if source['hp'] > 0 and value(source, 'lifesteal'):
            heal(source, source, actual*value(source, 'lifesteal'))
        if target['hp'] > 0 and source.get('attacks', 0)%3 == 0:
            target['stun'] = max(target.get('stun', 0), value(source, 'thirdStun'))
    return actual


def effective_armor(f):
    return value(f, 'armor')*(1-f.get('armorShred', 0))


def on_skill_hit(source, target):
    if target['hp'] <= 0:
        return
    shred = value(source, 'armorShred')
    if shred > 0 and shred >= target.get('armorShred', 0):
        target.update(armorShred=shred, shredRemaining=4)
    apply_burn(source, target)


def apply_burn(source, target):
    burn = value(source, 'burn')
    if target['hp'] > 0 and burn > 0 and burn >= target.get('burnRate', 0):
        if target.get('burnRemaining', 0) <= 0:
            target['burnClock'] = 0
        target.update(burnRate=burn, burnRemaining=3, burnOwner=source['key'])


def tick_effects(fighters, dt, now):
    """Advance existing debuffs before new hits. Refreshes never postpone a due tick."""
    by_key = {f['key']: f for f in fighters}
    for f in fighters:
        f['battleAge'] = now
        if f['hp'] <= 0:
            f.update(burnRemaining=0, burnClock=0, burnRate=0, shredRemaining=0, armorShred=0)
            continue
        f['shredRemaining'] = max(0, f.get('shredRemaining', 0)-dt)
        if f['shredRemaining'] < 1e-6:
            f['armorShred'] = 0
        active = min(dt, f.get('burnRemaining', 0))
        f['burnClock'] = f.get('burnClock', 0)+active
        f['burnRemaining'] = max(0, f.get('burnRemaining', 0)-dt)
        while f['burnClock']+1e-6 >= 1:
            f['burnClock'] -= 1
            owner = by_key.get(f.get('burnOwner'))
            if owner is not None:
                damage(owner, f, f['maxHp']*f.get('burnRate', 0), damage_type='magic')
                f['hitAt'] = now
        if f['burnRemaining'] < 1e-6:
            f.update(burnRate=0, burnClock=0, burnRemaining=0)


def third_splash(source, target, fighters, now):
    from combat_stats import attack, hex_distance
    if source.get('attacks', 0) <= 0 or source['attacks']%3 or not value(source, 'thirdSplash'):
        return
    victims = sorted((f for f in fighters if f is not target and f['side'] != source['side'] and f['hp'] > 0
                      and hex_distance(f['x'], f['y'], target['x'], target['y']) <= 1+1e-6),
                     key=lambda f: (hex_distance(f['x'], f['y'], target['x'], target['y']), f['key']))[:2]
    for f in victims:
        damage(source, f, attack(source)*value(source, 'thirdSplash'))
        f['hitAt'] = now


def team_crisis(fighters, definitions, now):
    """Latch all sides first, rescue before blasts; no team resurrects after a full wipe."""
    pending = []
    for side in sorted({f['side'] for f in fighters}):
        team = [f for f in fighters if f['side'] == side]
        if not any(f['hp'] > 0 for f in team) or sum(f['hp'] for f in team) > sum(f['maxHp'] for f in team)*(.4+1e-7):
            continue
        for field, bit, kind in (('teamRally', 1, 3), ('teamSalvation', 4, 5), ('teamInferno', 2, 4)):
            if any(f.get('synergyUsed', 0)&bit for f in team):
                continue
            carriers = [f for f in team if value(f, field) > 0]
            if not carriers:
                continue
            source = min(carriers, key=lambda f: (f['hp'] <= 0, -value(f, field), f['key']))
            for f in team:
                f['synergyUsed'] = f.get('synergyUsed', 0)|bit
                f.update(synergyAt=now, synergyKind=kind)
            pending.append((field, source, team))
    for field, source, team in sorted(pending, key=lambda p: p[0] == 'teamInferno'):
        if field == 'teamRally':
            for f in team:
                if f['hp'] > 0 and value(f, field) > 0:
                    f['stun'] = 0
                    heal(source, f, f['maxHp']*value(source, field))
                    shield(source, f, f['maxHp']*value(source, field))
                    f['mana'] = min(f['maxMana'], f['mana']+30)
        elif field == 'teamSalvation':
            dead = sorted((f for f in team if f['hp'] <= 0),
                          key=lambda f: (-definitions[f['id']]['cost'], -f['star'], f['key']))
            if dead:
                f = dead[0]
                f.update(hp=f['maxHp']*value(source, field), shield=0, mana=0, stun=0, cooldown=.25,
                         castUntil=now, target=-1, revived=True, revivedAt=now, burnRemaining=0, burnRate=0,
                         burnClock=0, armorShred=0, shredRemaining=0)
                source['healingDone'] = source.get('healingDone', 0)+f['hp']
            for f in team:
                shield(source, f, f['maxHp']*.15)
        else:
            for f in fighters:
                if f['side'] != source['side'] and f['hp'] > 0:
                    damage(source, f, f['maxHp']*value(source, field), damage_type='magic')
                    apply_burn(source, f)
                    f['hitAt'] = now
    # One visible announcement per side. The full team retains its spent flags.
    for side in {source['side'] for _,source,_ in pending}:
        team = [f for f in fighters if f['side']==side]
        kind = team[0]['synergyKind']
        for f in team:f['synergyKind']=0
        living = sorted((f for f in team if f['hp']>0),
                        key=lambda f: (not f.get('revived',False),-definitions[f['id']]['cost'],-f['star'],f['key']))
        if living:living[0]['synergyKind']=kind
