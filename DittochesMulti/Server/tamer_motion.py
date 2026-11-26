"""Small public movement stream, independent of economy and combat snapshots."""
import math

SPEED = 4.0  # arena world units / second; identical for every participant


def cell_world(x, y):
    row = min(7, max(0, math.floor(y)))
    fraction = min(1, max(0, y-row))
    offset = ((row % 2)*(1-fraction)+(min(7, row+1) % 2)*fraction)*.65
    return (x-3)*1.3+offset-.325, (3.5-y)*1.125


def position(player, now):
    target = cell_world(player.get('tamerX', 3), player.get('tamerY', 7))
    motion = player.get('_tamerMotion')
    if motion is None:
        return target
    x, z, started = motion
    distance = math.hypot(target[0]-x, target[1]-z)
    fraction = min(1, max(0, now-started)*SPEED/distance) if distance else 1
    return x+(target[0]-x)*fraction, z+(target[1]-z)*fraction


def begin_move(player, x, y, now, sequence):
    px, pz = position(player, now)
    player.update(tamerX=float(x), tamerY=float(y), tamerSequence=sequence,
                  _tamerMotion=(px, pz, now))


def snapshot(room, side, now):
    return dict(room=room['id'], side=side, sampledAt=now, players=[
        dict(side=i, sequence=p.get('tamerSequence', 0), x=p.get('tamerX', 3), y=p.get('tamerY', 7),
             worldX=position(p, now)[0], worldZ=position(p, now)[1])
        for i, p in enumerate(room['players'])])
