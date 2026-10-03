#!/usr/bin/env python3
"""Source-integrity checks only. This is not a Unity compiler or runtime test."""
from pathlib import Path
import hashlib
import json
import re
import struct
import sys

root = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(__file__).resolve().parents[1]
def require(condition, message):
    if not condition: raise SystemExit(message)
json.loads((root / 'Packages/manifest.json').read_text())
metas = list((root / 'Assets').rglob('*.meta'))
guids = {}
for path in metas:
    match = re.search(r'^guid: ([a-f0-9]{32})$', path.read_text(), re.M)
    require(match is not None, 'Missing GUID: ' + str(path))
    require(match[1] not in guids, 'Duplicate GUID: ' + str(path))
    guids[match[1]] = path
for path in (root / 'Assets').rglob('*'):
    if path.suffix == '.meta': continue
    require(Path(str(path) + '.meta').exists(), 'Missing meta: ' + str(path))
    if path.is_file():
        for reference in re.findall(r'guid: ([a-f0-9]{32})', path.read_text()):
            require(reference in guids, 'Unresolved asset reference: ' + str(path))
version = (root / 'ProjectSettings/ProjectVersion.txt').read_text()
require('m_EditorVersion: 6000.0.60f1\n' in version, 'Unexpected Unity version')
scene = (root / 'Assets/AetherWild/Scenes/Foundation.unity').read_text()
require('  mae: {fileID: 11400000, guid:' in scene, 'Missing scene Mae reference')
map_text = (root / 'Assets/AetherWild/Data/Battlefield.asset').read_text()
heights = struct.unpack('<64i', bytes.fromhex(re.search(r'  heights: (\w+)', map_text)[1]))
require(min(heights) > 0, 'Invalid authored terrain')
for x in (-12, 12): require(-5 + heights[int((x + 16) / .5)] * .5 <= -.6, 'Spawn overlaps terrain')
manifest_path = root / 'Docs/M0-PRESERVED-SHA256.json'
for path, digest in json.loads(manifest_path.read_text()).items():
    require(hashlib.sha256((root/path).read_bytes()).hexdigest() == digest, 'Validated M0 file changed: ' + path)
for path in root.rglob('*'):
    require(not any(part.lower() in ('library', 'temp', 'logs', 'obj', 'build', 'builds', 'buildcache', '.git')
                    for part in path.relative_to(root).parts), 'Excluded cache in source: ' + str(path))
print('PASS: manifest, Unity version, GUIDs/references, scene wiring, spawn clearance, M0 preservation, cache exclusion.')
print('Unity C# compilation, physical collision, rendering, and iPhone acceptance require Unity/device execution.')
