"""Select measured onsets, retaining measurement provenance in every cue."""
import json, pathlib, wave, shutil, sys
sys.stdout.reconfigure(encoding='utf-8')
import numpy as np
root=pathlib.Path(__file__).resolve().parents[1]
f=json.loads((root/'Analysis/features.json').read_text())
events=f['onsets']
cues=[]

def add(target,kind,x=8,height=35,scale=.7,rise=2,anchor='Burst',evidence='',label='',z=72):
    event=min(events,key=lambda e:abs(e['t']-target))
    if abs(event['t']-target)>.12: raise ValueError(f'No measured onset near {target}: {event}')
    cues.append(dict(label=label or f'{kind} {len(cues)+1:02d}',time=event['t'],kind=kind,anchor=anchor,rise=rise if anchor=='Burst' else 0,
                     x=x,y=0,z=z,height=height,scale=scale,seed=811+len(cues)*137,
                     evidence=evidence+f" | Measured spectral-flux peak {event['t']:.4f}s; flux={event['flux']:.4f}; normalized RMS={event['rms']:.4f}."))

add(.5713,'Chrysanthemum',13,27,.38,1.45,evidence='First prominent broadband attack with simultaneous low-frequency rise; no long leading silence.',label='Opening • one small gold above the roof')
for idx,t in enumerate([4.1113,4.3313,5.2613,5.3913,6.7613,7.1013]):
    add(t,'RedBurst',-24 if idx%2==0 else 29,24,.55 if idx<2 else .64,1.4,evidence='Separated short transient phrase; alternate left and right.',label=f'Opening red reply {idx+1}')
add(11.2,'Chrysanthemum',6,32,.5,1.8,evidence='Opening phrase thickens; still below the scale of the first grand explosion.',label='Opening gold punctuation')
# Three consecutive strong detected attacks, approximately 0.23 s apart.
for j,t in enumerate([18.9113,19.1413,19.3413]):
    add(t,'RedBurst',[-23,29,5][j],[23,23,27][j],[.60,.60,.76][j],1.35,evidence='Three consecutive strong attacks at 18.911 / 19.141 / 19.341 s (230 / 200 ms apart).',label=['Triple • left','Triple • right','Triple • larger centre'][j])
for j,t in enumerate([20.2713,21.0713,21.4113]):
    add(t,'RedBurst',[31,-26,8][j],25+j,[.66,.66,.8][j],1.4,evidence='Second short transient phrase; position and size vary from the first response.',label=f'Short phrase variation {j+1}')
for j,t in enumerate([22.5513,26.1913,29.8213,33.4613,37.1013,40.7313,44.3713,48.0013]):
    add(t,'Chrysanthemum' if j%3==0 else 'RedBurst',[-17,26,3,-22,24,1,-18,25][j],27+(j%3)*3,.57+(j%2)*.10,1.8,evidence='Persistent low-frequency pulse in the first dance passage; sparse selected accents, no grand-scale burst yet.',label=f'Early pulse {j+1}')
add(51.6413,'Chrysanthemum',8,33,.58,2,evidence='Phrase-ending accent before reduced bass at 52–58 s.',label='Early phrase gold release')
for j,t in enumerate([55.2713,58.9113,61.6413]):
    add(t,'RedBurst' if j<2 else 'Willow',[-19,26,9][j],26 if j<2 else 37,.6 if j<2 else .4,1.6 if j<2 else 2.1,
        evidence='Reduced-bass passage. Small gold willow supplies falling residual embers across the upcoming short rests.',label=f'Breathing passage {j+1}')
add(66.1813,'Chrysanthemum',8,46,1.08,2.6,evidence='After 65.13–65.26 and 65.36–65.48 s short rests, bass jumps from about 3.7 to 29.6 at 66.18–66.25 s; sustained full-band return.',label='FIRST GRAND • full-band return')

def strong_phrase(times,high=False,name=''):
    # The second statement uses a higher tier and greater scale.
    for i,t in enumerate(times):
        if i%3==0:
            for x in [-26,31]: add(t,'Fan',x,height=0,scale=.86 if not high else 1.02,anchor='Launch',evidence='Low red fan begins at the measured rhythmic attack; no airborne explosion.',label=f'{name} • red fan {i//3+1}',z=68)
        elif i%3==1:
            add(t,'Chrysanthemum',[-10,19,5][(i//3)%3],39 if high else 32,.94 if high else .78,2.2,evidence='Sustained high-energy repeated passage; mid gold ring above the low red fan.',label=f'{name} • gold middle {i//3+1}')
        else:
            add(t,'Willow',[-3,19,7][(i//3)%3],54 if high else 45,1.02 if high else .83,2.8,evidence='High gold willow layer; repeat raises height and scale, with extended falling tails.',label=f'{name} • willow high {i//3+1}',z=82)

strong_phrase([67.1013,68.0013,69.8213,71.6413,73.4613,75.2713,77.1013],False,'Strong passage A • wide')
for j,t in enumerate([80.7313,84.3713,88.0013,91.6413]):
    add(t,'RedBurst',-23 if j%2==0 else 28,27+j,.65+j*.04,1.6,evidence='Bass texture thins; return to alternating short red shells.',label=f'Middle red conversation {j+1}')
strong_phrase([95.2713,97.1013,98.9113,100.7313,102.5513,104.3713,106.1913,108.0013],True,'Repeated phrase • higher')
for j,t in enumerate([111.6413,113.4613,115.7213]):
    add(t,'RedBurst',[-24,30,6][j],26,.67,1.6,evidence='Short attacks during lower-energy transition; space between shells is preserved.',label=f'Transition reply {j+1}')
strong_phrase([117.0913,118.9113,120.7313,122.5513],True,'Second return')
add(124.3713,'Willow',9,47,.67,2.7,evidence='High-frequency density drops around 124–138 s; one gold willow fades into the breakdown.',label='Breakdown • long gold decay')
for j,t in enumerate([131.6413,136.1813,140.7313,144.3713,148.0013,151.6413]):
    add(t,'Chrysanthemum' if j in [1,4] else 'RedBurst',[-16,17,27,-22,6,28][j],31+(j%3)*2,.55+j*.035,1.9,evidence='Sparse accents in rebuilding texture; gold and red only.',label=f'Rebuild {j+1}')
strong_phrase([155.2713,157.1013,158.9113,160.7313,162.5513,164.3713,166.1913,168.0013,169.8213],True,'Final strong repeat')
add(171.1813,'Willow',9,54,.90,2.7,evidence='End of the sustained repeated section; gold decay precedes the last density lift.',label='Final repeat release')
for j,t in enumerate([173.9113,177.5513,181.1813]):
    add(t,'Comet',[-18,22,4][j],0,.92,0,'Launch',evidence='Final high-frequency/percussive density lift (174–185 s); only these three cues use thin blue comets.',label=f'Finale • blue punctuation {j+1}',z=75)
for j,t in enumerate([175.7313,179.3713,183.0013,185.5113]):
    add(t,'Chrysanthemum',[-8,22,7,9][j],42+(j%2)*4,.92+(j%2)*.08,2.4,evidence='Late dense phrase: gold layer between restrained blue punctuation.',label=f'Finale gold {j+1}',z=78)
add(186.6413,'Willow',9,52,1.0,2.9,evidence='Penultimate strong broadband accent; slow gold tails continue beneath the final hit.',label='Finale • curtain of gold',z=83)
for kind,x,height,scale,rise,label in [
    ('Chrysanthemum',8,47,1.32,2.8,'FINAL • central gold'),
    ('RedBurst',-23,32,1.02,1.7,'FINAL • left red'),
    ('RedBurst',34,32,1.02,1.7,'FINAL • right red')]:
    add(188.0213,kind,x,height,scale,rise,evidence='Last clear broadband attack at 188.021 s, followed by final near-silence from about 188.24 s; synchronized first flashes.',label=label,z=80)

cues.sort(key=lambda c:(c['time'],c['label']))
notes='Source ado_show_.m4a, decoded duration 189.639833 s, 48000 Hz stereo. Time zero is file start. Measured spectral flux / RMS / band energy, not title/version metadata. Onset resolution 10 ms; allow about ±25 ms perceptual adjustment. No long leading silence. Playback WAV uses fixed 18x gain (25.105 dB), no trimming/stretching/compression. First grand burst 66.1813 s; last accent 188.0213 s. Near-silent tail about 188.24 s to file end. Blue only at 173.9113, 177.5913, 181.1413 s. Three short attacks at 18.9113, 19.1413, 19.3413 s. All launch times are derived from the editable anchor and rise time.'
(root/'Analysis/cues.json').write_text(json.dumps(dict(notes=notes,cues=cues),ensure_ascii=False,indent=2),encoding='utf-8')
(root/'Assets/Festival/Audio').mkdir(parents=True,exist_ok=True)
with wave.open(str(root/'Analysis/ado_show_decoded.wav'),'rb') as src:
    params=src.getparams(); pcm=np.frombuffer(src.readframes(src.getnframes()),dtype='<i2').astype(np.float64)
with wave.open(str(root/'Assets/Festival/Audio/Show.wav'),'wb') as dst:
    dst.setparams(params); dst.writeframes(np.clip(pcm*18,-32768,32767).astype('<i2').tobytes())
(root/'References').mkdir(exist_ok=True)
shutil.copy2('C:/Users/user/Downloads/ado_show_.m4a',root/'References/ado_show_.m4a')
shutil.copy2('C:/Users/user/AppData/Local/Temp/codex-clipboard-18d2a46f-4145-48d4-8a3d-5a6a1c456c68.png',root/'References/festival-composition.png')
print('Cues:',len(cues))
for c in cues:
    if abs(c['time']-188.0213)<.01 or c['label'].startswith(('Opening •','Triple','FIRST','Finale • blue')): print(c['label'],c['time'],c['time']-c['rise'],c['kind'])
