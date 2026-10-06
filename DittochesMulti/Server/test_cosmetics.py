import copy
import secrets
import tempfile
import unittest
from pathlib import Path
from server import Game, Rejected


class CosmeticTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.now = 1000.
        self.game = Game(Path(self.temp.name)/'test.sqlite3', clock=lambda: self.now)
        self.keys = [secrets.token_hex(32) for _ in range(2)]
        self.tokens = [self.game.request('/login', dict(name='tamer', key=k))['token'] for k in self.keys]

    def tearDown(self):
        self.game.db.close()
        self.temp.cleanup()

    def request(self, path, data=None, who=0):
        self.now += .1
        return self.game.request(path, data or {}, self.tokens[who])

    def match(self):
        self.request('/queue', dict(mode='normal', tamer=1, field=2, finisher=1))
        response = self.request('/queue', dict(mode='normal', tamer=3, field=1, finisher=2), 1)
        return self.game.rooms[response['room']['id']]

    def test_invalid_selection_preserves_queue_and_previous_loadout(self):
        self.request('/queue', dict(mode='normal', tamer=2, field=1, finisher=2))
        before = copy.deepcopy(self.game.queues)
        for key, maximum in [('tamer', 4), ('field', 3), ('finisher', 3)]:
            for value in [-1, maximum, True, 1.0, '1', None]:
                with self.subTest(key=key, value=value):
                    with self.assertRaises(Rejected):
                        self.request('/queue', dict(mode='ranked', **{key: value}))
                    self.assertEqual(before, self.game.queues)
                    self.assertEqual(self.game.sessions[self.tokens[0]]['cosmetics'], dict(tamer=2, field=1, finisher=2))

    def test_public_cosmetics_keep_private_inventory_hidden_and_reconnect(self):
        self.match()
        response = self.request('/state', who=1)
        other = response['room']['players'][0]
        self.assertEqual([other[k] for k in ('tamer', 'field', 'finisher')], [1, 2, 1])
        self.assertEqual(other['inventory'], [])
        self.assertEqual(other['shop'], [])
        self.request('/tamer-move', dict(x=5, y=6))
        reconnect = self.game.request('/login', dict(name='again', key=self.keys[0]))
        own = reconnect['room']['players'][0]
        self.assertEqual([own[k] for k in ('tamer', 'field', 'finisher', 'tamerX', 'tamerY')], [1, 2, 1, 5, 6])

    def test_move_changes_only_own_position_in_prepare_ready_and_battle(self):
        room = self.match()
        for phase, ready in [('prepare', False), ('prepare', True), ('battle', True)]:
            room['phase'] = phase
            room['players'][0]['ready'] = ready
            before = copy.deepcopy(room)
            self.request('/tamer-move', dict(x=2.5, y=4.5, tamer=3, hp=999))
            expected = before['players'][0]
            expected.update(tamerX=2.5, tamerY=4.5)
            self.assertEqual(room, before)

    def test_bounds_invalid_numbers_and_rate_do_not_mutate_game(self):
        room = self.match()
        for x, y in [(-1, 4), (7, 4), (3, 3), (3, 8), (True, 4), ('1', 4), (float('nan'), 4), (3, float('inf'))]:
            before = copy.deepcopy(room)
            with self.assertRaises(Rejected):
                self.request('/tamer-move', dict(x=x, y=y))
            self.assertEqual(before, room)
        self.request('/tamer-move', dict(x=0, y=4))
        before = copy.deepcopy(room)
        with self.assertRaises(Rejected):
            self.game.request('/tamer-move', dict(x=6, y=7), self.tokens[0])
        self.assertEqual(before, room)
        room['phase'] = 'finished'
        room['finishedAt'] = self.now
        with self.assertRaises(Rejected):
            self.request('/tamer-move', dict(x=6, y=7))

    def test_legacy_client_defaults(self):
        self.request('/queue', dict(mode='normal'))
        response = self.request('/queue', dict(mode='normal'), 1)
        for player in response['room']['players']:
            self.assertEqual([player[k] for k in ('tamer', 'field', 'finisher')], [0, 0, 0])
