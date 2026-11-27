import json, wave, pathlib
import numpy as np

root = pathlib.Path(__file__).resolve().parents[1]
with wave.open(str(root / 'Analysis/ado_show_decoded.wav'), 'rb') as w:
    sr = w.getframerate()
    channels = w.getnchannels()
    pcm = np.frombuffer(w.readframes(w.getnframes()), dtype='<i2').reshape(-1, channels).astype(np.float32) / 32768
analysis_gain = 1.0 / max(float(np.max(np.abs(pcm))), 1e-9)
mono = pcm.mean(axis=1)[::2] * analysis_gain
rate = sr // 2
hop, size = 240, 1024
frames = np.lib.stride_tricks.sliding_window_view(mono, size)[::hop]
rms = np.sqrt(np.mean(frames**2, axis=1))
spec = np.abs(np.fft.rfft(frames * np.hanning(size), axis=1)).astype(np.float32)
freq = np.fft.rfftfreq(size, 1 / rate)
times = (np.arange(len(frames)) * hop + size / 2) / rate
diff = np.maximum(0, np.diff(np.log1p(spec * 10), axis=0, prepend=np.zeros((1,spec.shape[1]))))
flux = diff.mean(axis=1)
low = np.sqrt(np.mean(spec[:, (freq >= 35) & (freq < 200)] ** 2, axis=1))
high = np.sqrt(np.mean(spec[:, (freq >= 2000) & (freq < 10000)] ** 2, axis=1))
lowflux = diff[:, (freq >= 35) & (freq < 200)].mean(axis=1)
def peaks(y, distance=12, threshold=0):
    p = np.flatnonzero((y[1:-1] > y[:-2]) & (y[1:-1] >= y[2:])) + 1
    p = p[y[p] > threshold]
    selected = []
    for i in p[np.argsort(y[p])[::-1]]:
        if all(abs(int(i)-int(j)) >= distance for j in selected): selected.append(int(i))
    return sorted(selected)
onsets = peaks(flux, 12, np.percentile(flux, 60))
def ranges(mask):
    edges = np.diff(np.r_[False,mask,False].astype(int))
    return [[round(float(times[a] - size/2/rate),3), round(float(times[b-1] + size/2/rate),3)] for a,b in zip(np.where(edges==1)[0],np.where(edges==-1)[0]) if b-a>=8]
seconds = []
for s in range(int(len(mono)/rate)+1):
    mask = (times >= s) & (times < s+1)
    if not mask.any(): continue
    seconds.append(dict(t=s, rms=round(float(rms[mask].mean()),4), peak=round(float(rms[mask].max()),4), low=round(float(low[mask].mean()),2), high=round(float(high[mask].mean()),2), flux=round(float(flux[mask].mean()),4)))
events=[dict(t=round(float(times[i]),4),flux=round(float(flux[i]),4),rms=round(float(rms[i]),4),lowflux=round(float(lowflux[i]),4)) for i in onsets]
ac=[]
f=flux-flux.mean()
for lag in range(25,100): ac.append((float(np.dot(f[:-lag], f[lag:])),lag,60/(lag*hop/rate)))
result=dict(duration=len(pcm)/sr, analysis_gain=analysis_gain, frame_seconds=size/rate, hop_seconds=hop/rate, timing_note='Window-centered spectral flux candidates; 10 ms resolution, about +/- 25 ms onset uncertainty. No title-based timing. Analysis only peak-normalized; source WAV retains original level.', silence=ranges(rms<0.001), quiet=ranges(rms<0.025), tempo_candidates=sorted(ac,reverse=True)[:12], seconds=seconds, onsets=events)
(root/'Analysis/features.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
np.savez_compressed(root/'Analysis/features.npz',times=times,rms=rms,flux=flux,low=low,high=high,lowflux=lowflux)
print(json.dumps({k:result[k] for k in ['duration','silence','quiet','tempo_candidates']},indent=2))
print('SECONDS: t rms peak low high flux')
for s in seconds: print(*s.values())
