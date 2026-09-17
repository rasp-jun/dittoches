"""Shared per-unit stats and explicit AD/AP coefficients; no hidden skill multiplier."""
import json
from pathlib import Path

SKILLS={s['id']:s for s in json.loads((Path(__file__).resolve().parent.parent/'Assets/Resources/DigimonSkills.json').read_text(encoding='utf-8'))['skills']}


def star_index(star):
    return max(0,min(2,star-1))


def attack(f):
    return SKILLS[f['id']]['baseAttack']*1.5**star_index(f['star'])*max(0,1+f.get('build',{}).get('attack',0))


def ability_power(f):
    return max(0,100+f.get('build',{}).get('abilityPower',0))


def skill_damage(f):
    s=SKILLS[f['id']];index=star_index(f['star'])
    return max(0,attack(f)*s['adRatio'][index]+ability_power(f)*s['apRatio'][index])


def mitigate(raw,armor,magic_resist,damage_type):
    resistance=armor if damage_type=='physical' else magic_resist
    return max(0,raw)*100/(100+max(0,resistance))


def row_offset(row):
    row=max(0,min(7,row));first=int(row);nxt=min(7,first+1)
    return (first%2+(nxt%2-first%2)*(row-first))*.5


def hex_distance(sx,sy,tx,ty):
    r=ty-sy;q=(tx+row_offset(ty)-ty*.5)-(sx+row_offset(sy)-sy*.5)
    return (abs(q)+abs(r)+abs(q+r))*.5


def in_attack_range(source,target):
    return hex_distance(source['x'],source['y'],target['x'],target['y'])<=SKILLS[source['id']]['attackRange']+.025
