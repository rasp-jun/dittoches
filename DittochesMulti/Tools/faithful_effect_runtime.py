"""Fingerprint every local module that affects technique presentation."""
import hashlib

FILES=('gallery.js','technique-effects.js','technique-signatures.js',
       'character-finish.js','pose-transition.js')


def effect_runtime_hashes(root):
    folder=root/'Tools/faithful_gallery'
    return {name:hashlib.sha256((folder/name).read_bytes()).hexdigest() for name in FILES}
