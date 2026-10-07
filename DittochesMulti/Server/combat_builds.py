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


def damage(source, target, amount, basic=False, damage_type='physical'):
    if target['hp'] <= 0:
        return 0
    amount = mitigate(amount,value(target,'armor'),value(target,'magicResist'),damage_type)
    amount *= 1-min(.6, max(0, value(target, 'reduction')))
    if target['hp'] >= target['maxHp']*.7:
        amount *= 1+value(source, 'highHealthDamage')
    absorbed = min(target.get('shield', 0), amount)
    target['shield'] = target.get('shield', 0)-absorbed
    actual = min(target['hp'], max(0, amount-absorbed))
    target['hp'] -= actual
    source['damageDone'] = source.get('damageDone', 0)+actual
    kind = 'basicDamageDone' if basic else 'skillDamageDone'
    source[kind] = source.get(kind, 0)+actual
    target['damageTaken'] = target.get('damageTaken', 0)+actual
    target['shieldAbsorbed'] = target.get('shieldAbsorbed', 0)+absorbed
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
