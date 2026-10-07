"""Read-only HTTP boundary checks for the gallery server."""
import urllib.error
import urllib.request

BASE = 'http://127.0.0.1:8766'
ALLOWED = ['/health', '/Tools/faithful_gallery/index.html',
           '/ArtSource/FaithfulGallery/manifest.json', '/ArtSource/Roster/ReferencesV2/agumon.jpg']
DENIED = ['/README.md', '/.git/config', '/Tools/author_roster.py',
          '/Tools/faithful_gallery/%2e%2e/author_roster.py', '/ArtSource/FaithfulGallery/']
for path in ALLOWED + DENIED:
    try:
        status = urllib.request.urlopen(BASE + path, timeout=5).status
    except urllib.error.HTTPError as error:
        status = error.code
    expected = 200 if path in ALLOWED else 403
    assert status == expected, (path, status, expected)
    print('SERVER BOUNDARY PASS', status, path)
