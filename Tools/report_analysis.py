import json, pathlib, wave, sys, csv
import numpy as np
from PIL import Image, ImageDraw, ImageFont
sys.stdout.reconfigure(encoding='utf-8')
root=pathlib.Path(__file__).resolve().parents[1]
features=np.load(root/'Analysis/features.npz')
meta=json.loads((root/'Analysis/audio-metadata.json').read_text())
show=json.loads((root/'Analysis/cues.json').read_text(encoding='utf-8'))
cues=show['cues']
font_path='C:/Windows/Fonts/segoeui.ttf'
font=ImageFont.truetype(font_path,19)
small=ImageFont.truetype(font_path,15)
title=ImageFont.truetype(font_path,30)
img=Image.new('RGB',(1800,1060),'#0c121e'); d=ImageDraw.Draw(img)
d.text((52,28),'SHOW / SOURCE-AUDIO TIMING STUDY',font=title,fill='#FFD36A')
d.text((52,73),'189.639833 s  |  48 kHz stereo  |  file origin retained  |  10 ms onset analysis  |  no title-based timing',font=font,fill='#c2cbd8')
t=features['times']; x0,x1=70,1740
for row,(key,label,color) in enumerate([('rms','RMS energy (peak-normalized for analysis)','#FFD36A'),('low','Low band / 35-200 Hz','#E63227'),('high','High band / 2-10 kHz','#548CFF')]):
    y0=150+row*200; y1=y0+130
    d.text((x0,y0-30),label,font=font,fill=color)
    a=features[key]; high=float(np.percentile(a,99.5))
    points=[]
    for px in range(x1-x0):
        mask=(t>=px/(x1-x0)*meta['duration'])&(t<(px+1)/(x1-x0)*meta['duration'])
        val=float(a[mask].mean()) if mask.any() else 0
        points.append((px+x0,y1-min(1,val/high)*(y1-y0)))
    d.line(points,fill=color,width=2)
    for sec in range(0,190,10):
        x=x0+sec/meta['duration']*(x1-x0)
        d.line((x,y0,x,y1),fill='#293142'); d.text((x-6,y1+5),str(sec),font=small,fill='#9da9bb')
    for sec,mark in [(66.1813,'FIRST GRAND'),(155.2713,'FINAL REPEAT'),(173.9113,'BLUE'),(188.0213,'LAST')]:
        x=x0+sec/meta['duration']*(x1-x0);d.line((x,y0,x,y1),fill='#616d80',width=1)
        if row==0:d.text((x-12,y0-50),mark,font=small,fill='#d7dde8')
y=785
d.text((70,y-35),'SELECTED CUES / every mark is a measured audio onset, not a synthetic beat grid',font=font,fill='#d7dde8')
for kind,col,yy in [('Chrysanthemum','#FFD36A',y),('Willow','#B7954B',y+40),('RedBurst','#E63227',y+80),('Fan','#EF6955',y+120),('Comet','#548CFF',y+160)]:
    d.text((70,yy-17),kind,font=small,fill=col)
    for c in cues:
        if c['kind']==kind:
            x=x0+c['time']/meta['duration']*(x1-x0)
            d.line((x,yy,x,yy+16),fill=col,width=3)
d.text((70,1008),'66.18 s: bass return after short rests   /   188.02 s: final clear attack   /   188.24-189.64 s: near-silent file tail',font=font,fill='#aeb9ca')
img.save(root/'Analysis/audio-timing-overview.png')

img=Image.new('RGB',(1800,1000),'#0c121e'); d=ImageDraw.Draw(img)
d.text((55,25),'ATTACK / REST / FINAL HIT DETAILS',font=title,fill='#FFD36A')
for i,(lo,hi,label,marks) in enumerate([(0,1.1,'Opening: first prominent attack',[.5713]),(18.7,19.6,'Three successive short attacks',[18.9113,19.1413,19.3413]),(64.9,66.65,'Short rests, then full bass return',[65.0313,65.2513,65.4813,66.1813]),(186.3,189.64,'Final clear attack and near-silent file tail',[186.6413,188.0213])]):
    x0=70+(i%2)*880; x1=x0+770; y0=145+(i//2)*440; y1=y0+300
    d.text((x0,y0-45),label,font=font,fill='#d7dde8')
    mask=(t>=lo)&(t<=hi); a=features['rms'][mask]; tt=t[mask]
    pts=[(x0+(v-lo)/(hi-lo)*(x1-x0),y1-float(r)/.32*(y1-y0)) for v,r in zip(tt,a)]
    d.line(pts,fill='#FFD36A',width=3)
    for mark in marks:
        x=x0+(mark-lo)/(hi-lo)*(x1-x0);d.line((x,y0,x,y1),fill='#E63227',width=2);d.text((x-25,y1+12),f'{mark:.3f}',font=small,fill='#E6B5A1')
    d.text((x0,y1+45),f'{lo:.2f} s',font=small,fill='#aeb9ca');d.text((x1-85,y1+45),f'{hi:.2f} s',font=small,fill='#aeb9ca')
img.save(root/'Analysis/audio-timing-details.png')

with (root/'Analysis/all-cues.csv').open('w',encoding='utf-8-sig',newline='') as out:
    writer=csv.writer(out)
    writer.writerow(['Label','File time seconds','Anchor','Kind','Launch time','Burst time','X','Y','Z','Height','Scale','Evidence'])
    for c in cues: writer.writerow([c['label'],c['time'],c['anchor'],c['kind'],round(c['time']-c['rise'],4) if c['anchor']=='Burst' else c['time'],c['time'] if c['anchor']=='Burst' else 'N/A',c['x'],c['y'],c['z'],c['height'],c['scale'],c['evidence']])

with wave.open(str(root/'Analysis/ado_show_decoded.wav')) as w:
    params=w.getparams(); original=np.frombuffer(w.readframes(w.getnframes()),dtype='<i2')
with wave.open(str(root/'Assets/Festival/Audio/Show.wav')) as w:
    params_out=w.getparams(); playback=np.frombuffer(w.readframes(w.getnframes()),dtype='<i2')
checks={'frames_unchanged':params.nframes==params_out.nframes,'channels_unchanged':params.nchannels==params_out.nchannels,'rate_unchanged':params.framerate==params_out.framerate,'gain_exact_18_no_shift':bool(np.array_equal(playback,original.astype(np.int32)*18)),'no_clipping':bool(np.max(np.abs(playback.astype(np.int32)))<32767),'cue_count':len(cues),'source_sha256':meta['sourceSha256'],'playback_peak':float(np.max(np.abs(playback.astype(np.int32)))/32768)}
(root/'Verification').mkdir(exist_ok=True)
(root/'Verification/audio-validation.json').write_text(json.dumps(checks,indent=2))
print(json.dumps(checks,indent=2))
