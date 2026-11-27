"""Check timing preservation, measured accents, spatial spread and rendered contrast."""
import collections
import hashlib
import json
import math
import pathlib
import struct
import sys

root = pathlib.Path(__file__).resolve().parents[1]
project = pathlib.Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else root
old = json.loads((root / 'Verification/BeforeWide/Analysis/cues.json').read_text(encoding='utf-8-sig'))['cues']
new = json.loads((project / 'Analysis/cues.json').read_text(encoding='utf-8-sig'))['cues']
onsets = json.loads((root / 'Analysis/features.json').read_text(encoding='utf-8-sig'))['onsets']
identity = lambda c: (c['label'], c['time'], c['seed'], c['kind'])
index = {identity(c): c for c in new}
preserved = all(identity(c) in index and index[identity(c)]['anchor'] == c['anchor']
                and struct.pack('<f', index[identity(c)]['rise']) == struct.pack('<f', c['rise']) for c in old)
extra = [c for c in new if c['label'].startswith(('CALM', 'WIDE'))]
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()

def coverage(cues):
    counts = collections.Counter()
    angle = math.radians(18)
    for c in cues:
        if c['kind'] in ('Fan', 'Comet'):
            continue
        depth = math.sin(angle) * (c['y'] + c['height'] - 1.7) + math.cos(angle) * (c['z'] + 28)
        u = .5 + c['x'] / (2 * depth * math.tan(math.radians(29)) * 16 / 9)
        counts['left' if u < .18 else 'right' if u > .80 else 'inner'] += 1
    return dict(counts)

before, after = coverage(old), coverage(new)
checks = {
    'previous_cues': len(old), 'current_cues': len(new),
    'calm_additions': sum(c['label'].startswith('CALM') for c in extra),
    'wide_additions': sum(c['label'].startswith('WIDE') for c in extra),
    'types': sorted({c['kind'] for c in new}),
    'original_anchor_times_and_rise_times_preserved': preserved,
    'every_added_anchor_is_a_measured_source_onset': all(min(abs(c['time'] - o['t']) for o in onsets) < .00001 for c in extra),
    'blue_cue_times_unchanged': [c['time'] for c in old if c['kind'] == 'Comet'] == [c['time'] for c in new if c['kind'] == 'Comet'],
    'playback_audio_unchanged': sha(project / 'Assets/Festival/Audio/Show.wav') == sha(root / 'Verification/BeforeWide/Assets/Festival/Audio/Show.wav'),
    'burst_centres_by_screen_region_before': before,
    'burst_centres_by_screen_region_after': after,
    'both_outer_regions_used_more': after.get('left', 0) > before.get('left', 0) and after.get('right', 0) > before.get('right', 0),
}
images = project / 'Verification/Wide'
if (images / '03_highlight.png').exists():
    import numpy as np
    from PIL import Image
    background = np.asarray(Image.open(images / '00_background.png'), dtype=np.float32)
    def light(name):
        pixels = np.asarray(Image.open(images / name), dtype=np.float32)
        return float(np.maximum(pixels - background, 0).sum())
    quiet, highlight = light('02_quiet.png'), light('03_highlight.png')
    checks['snapshot_quiet_to_highlight_added_light_ratio'] = round(quiet / highlight, 4)
    checks['quiet_visible_and_highlight_brighter_in_snapshots'] = 0 < quiet < highlight
    checks['contrast_measure_scope'] = 'Two fixed Camera renders at 130.10s and 160.60s; pixel sum above identical background, not whole-song loudness.'
images.mkdir(parents=True, exist_ok=True)
(images / 'data-regression.json').write_text(json.dumps(checks, indent=2), encoding='utf-8')
print(json.dumps(checks, indent=2))
assert len(old) == 130 and len(new) == 163 and len(extra) == 33 and len(checks['types']) == 13
assert all(value is not False for value in checks.values())
