import json, pathlib, hashlib, struct
root=pathlib.Path(__file__).resolve().parents[1]
old=json.loads((root/'Verification/BeforeParade/Analysis/cues.json').read_text(encoding='utf-8'))['cues']
new=json.loads((root/'Analysis/cues.json').read_text(encoding='utf-8'))['cues']
features=json.loads((root/'Analysis/features.json').read_text(encoding='utf-8'))['onsets']
def key(c): return c['label'],c['x'],c['z']
index={key(c):c for c in new}
preserved=all(key(c) in index and index[key(c)]['time']==c['time'] and index[key(c)]['anchor']==c['anchor'] and all(struct.pack('<f',index[key(c)][f])==struct.pack('<f',c[f]) for f in ['rise','x','y','z']) for c in old)
extra=[c for c in new if c['label'].startswith('PARADE')]
blue_old=[c['time'] for c in old if c['kind']=='Comet']
blue_new=[c['time'] for c in new if c['kind']=='Comet']
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
checks=dict(original_cues=len(old),current_cues=len(new),extra_cues=len(extra),original_timing_and_launch_positions_preserved=preserved,
    every_extra_anchor_is_a_measured_onset=all(min(abs(c['time']-o['t']) for o in features)<.00001 for c in extra),
    blue_cues_unchanged=blue_old==blue_new,
    playback_audio_unchanged=sha(root/'Assets/Festival/Audio/Show.wav')==sha(root/'Verification/BeforeParade/Assets/Festival/Audio/Show.wav'))
(root/'Verification/Parade/data-regression.json').write_text(json.dumps(checks,indent=2))
print(json.dumps(checks,indent=2))
assert all(v is not False for v in checks.values())
