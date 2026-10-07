import unittest
from combat_skills import select_target, begin_cast, advance_cast


def fighter(key, x, y, side=1, uid='agumon'):
    return dict(key=key, x=x, y=y, side=side, id=uid, star=1, hp=100, mana=0)


class CombatTargetingTests(unittest.TestCase):
    def setUp(self):
        self.source = fighter(0, 3, 4, 0)
        self.near = fighter(1, 3, 3)
        self.far = fighter(2, 3, 0)
        self.source['target'] = 2

    def test_reachable_enemy_replaces_out_of_range_target(self):
        self.far['hp'] = 1
        self.assertIs(select_target(self.source, [self.source, self.far, self.near]), self.near)

    def test_reachable_target_stays_locked_despite_closer_enemy(self):
        self.source.update(id='metalgarurumon')
        self.assertIs(select_target(self.source, [self.near, self.far]), self.far)

    def test_chase_stays_stable_when_nobody_is_reachable(self):
        closer = fighter(3, 3, 2)
        self.assertIs(select_target(self.source, [closer, self.far]), self.far)

    def test_initial_target_is_nearest_by_hex_distance(self):
        # Euclidean distance would favor key 2; the hex metric favors key 1.
        self.source.update(x=3, y=2, target=-1)
        diagonal = fighter(1, 2, 1)
        adjacent = fighter(2, 4.1, 2)
        self.assertIs(select_target(self.source, [adjacent, diagonal]), diagonal)
        self.source['target'] = -1
        tied = fighter(3, 4, 2)
        self.assertIs(select_target(self.source, [tied, diagonal]), diagonal)

    def test_dead_and_allied_targets_are_excluded(self):
        self.far['hp'] = 0
        ally = fighter(3, 3, 4, 0)
        self.assertIs(select_target(self.source, [ally, self.far, self.near]), self.near)
        self.source['target'] = 3
        self.assertIs(select_target(self.source, [ally, self.near]), self.near)
        self.assertIsNone(select_target(self.source, [ally, self.far]))

    def test_dead_windup_target_reacquires_but_released_cast_stays_locked(self):
        cast = begin_cast(self.source, self.far, 0, 1, damage=20)
        self.far['hp'] = 0
        advance_cast(cast, [self.source, self.far, self.near], .01)
        self.assertEqual(cast['target'], self.near['key'])
        advance_cast(cast, [self.source, self.far, self.near], .4)
        self.assertTrue(cast['released'])
        self.near['hp'] = 0
        self.far['hp'] = 100
        advance_cast(cast, [self.source, self.far, self.near], 2)
        self.assertEqual(self.far['hp'], 100)


if __name__ == '__main__':
    unittest.main()
