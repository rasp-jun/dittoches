"""Edit deterministic Unity footage into a 24-second 1080p promotional test.

Uses local Pillow/numpy, Windows Korean fonts, and the preserved FFmpeg binary.
The soundtrack is synthesized here; no downloaded music or generated game footage.
"""
import argparse
import hashlib
import json
import re
import subprocess
import wave
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parent.parent
W,H,FPS,SECONDS=1920,1080,30,24
STARTS=[0,4,8,12,16,21]
ENDS=[4,8,12,16,21,24]
GOLD=(235,195,115,255)
WHITE=(241,246,247,255)
MUTED=(167,190,201,255)


def font(size,bold=False):
    return ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf' if bold else 'C:/Windows/Fonts/malgun.ttf',size)


FONTS={n:font(n,n>=38) for n in (22,25,30,38,46,58,66,96)}


def text(draw,xy,value,size,color=WHITE,anchor=None):
    draw.text(xy,value,font=FONTS[size],fill=color,anchor=anchor,stroke_width=1,stroke_fill=(0,0,0,90))


def gradient():
    overlay=Image.new('RGBA',(W,H))
    d=ImageDraw.Draw(overlay)
    for y in range(H):
        top=max(0,1-y/330);bottom=max(0,(y-765)/(H-765))
        d.line((0,y,W,y),fill=(3,9,16,int(max(top*.78,bottom*.9)*255)))
    return overlay


SHADE=gradient()


def edit(path,t):
    shot=next(i for i,end in enumerate(ENDS) if t<end)
    local=t-STARTS[shot]
    frame=Image.open(path).convert('RGBA');frame=Image.alpha_composite(frame,SHADE)
    layer=Image.new('RGBA',(W,H));d=ImageDraw.Draw(layer)
    text(d,(76,44),'DITTOCHES',25,GOLD)
    text(d,(W-76,44),'MOTION PREVIEW  /  2026',22,MUTED,anchor='ra')
    y=112+int(16*(1-min(1,local/.35))**2)
    if shot==0:
        text(d,(76,y),'작은 시작, 거대한 진화',66)
        text(d,(80,y+101),'유년기부터 궁극체까지',30,GOLD)
        text(d,(76,893),'다섯 단계로 달라지는 전장의 존재감',38)
        text(d,(78,958),'유년기   →   성장기   →   성숙기   →   완전체   →   궁극체',25,MUTED)
    elif shot in (1,2,3):
        names=['그레이몬','메탈그레이몬','워그레이몬']
        skills=['메가 플레임','기가 디스트로이어','가이아 포스']
        captions=['화염으로 밀어붙이다','중화기로 전선을 돌파하다','궁극체의 힘을 해방하다']
        text(d,(76,y),names[shot-1],58)
        text(d,(80,y+90),skills[shot-1],38,GOLD)
        text(d,(76,901),captions[shot-1],46)
        text(d,(79,971),'SIGNATURE SKILL  /  '+str(shot).zfill(2),22,MUTED)
    elif shot==4:
        text(d,(76,y),'배치하고. 진화하고. 승리하라.',58)
        text(d,(80,y+92),'14유닛 전투 리플레이',30,GOLD)
        text(d,(76,915),'나만의 조합이 만드는 한 판',38)
    else:
        text(d,(W/2,126),'DITTOCHES',96,anchor='mt')
        text(d,(W/2,276),'나만의 진화로 완성하는 전장',38,GOLD,anchor='mt')
        text(d,(W/2,909),'3D 모션 · 진화 · 전투',38,anchor='mt')
    d.line((76,1031,W-76,1031),fill=(89,116,126,160),width=2)
    d.line((76,1031,76+(W-152)*t/SECONDS,1031),fill=GOLD,width=3)
    text(d,(W-76,974),'개발 중 화면',22,MUTED,anchor='ra')
    frame=Image.alpha_composite(frame,layer).convert('RGB')
    # Short film-style fades avoid abrupt pose/camera discontinuities between shots.
    fade_in=min(1,local/(.5 if shot==0 else .14))
    fade_out=min(1,(ENDS[shot]-t)/(.75 if shot==5 else .14))
    fade=min(fade_in,fade_out)
    if fade<1:frame=Image.blend(Image.new('RGB',(W,H),(3,8,14)),frame,fade)
    return frame


def soundtrack(path):
    rate=48000;n=rate*SECONDS;t=np.arange(n,dtype=np.float64)/rate
    audio=np.zeros(n);rng=np.random.default_rng(61006)
    # Original minor-key synth bed; six four-second harmonic blocks at 120 BPM.
    chords=[[146.83,174.61,220],[116.54,146.83,174.61],[130.81,174.61,220],[130.81,164.81,196],[146.83,174.61,220],[146.83,174.61,220]]
    for i,chord in enumerate(chords):
        local=t-i*4;env=np.clip(local/.5,0,1)*np.clip((4-local)/.7,0,1)
        for f in chord:
            audio+=.027*env*(np.sin(2*np.pi*f*t)+.3*np.sin(2*np.pi*f*1.003*t))
        audio+=.045*env*np.sin(2*np.pi*(chord[0]/2)*t)*(0.65+.35*np.sin(2*np.pi*2*t)**2)
    for beat in np.arange(0,23.5,.5):
        u=t-beat;mask=(u>=0)&(u<.22);x=u[mask]
        audio[mask]+=.15*np.sin(2*np.pi*(48*x+2.5*(1-np.exp(-25*x))))*np.exp(-20*x)
        if int(round(beat*2))%2:
            mask=(u>=0)&(u<.1);x=u[mask]
            audio[mask]+=.035*rng.normal(size=len(x))*np.exp(-45*x)
    for cut in (4,8,12,16,21):
        u=t-(cut-.2);mask=(u>=0)&(u<.4);x=u[mask]
        noise=rng.normal(size=len(x));noise=np.convolve(noise,np.ones(12)/12,mode='same')
        audio[mask]+=.09*noise*np.sin(np.pi*x/.4)**2
    audio*=np.minimum(1,t/.8)*np.clip((SECONDS-t)/1.1,0,1)
    audio=np.tanh(audio*1.6)*.8
    stereo=np.column_stack((audio,audio*.99)).astype(np.float64)
    with wave.open(str(path),'wb') as output:
        output.setnchannels(2);output.setsampwidth(2);output.setframerate(rate)
        output.writeframes((stereo*32767).astype('<i2').tobytes())


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--storyboard',action='store_true')
    args=parser.parse_args()
    capture=ROOT/'Builds/PromoPreview/PromoCapture'
    output=ROOT/'Builds/PromoVideo-20261006';output.mkdir(exist_ok=True)
    samples=[2,5,6,9,10,13,14,17,19,22]
    sheet=Image.new('RGB',(1280,5*390),(6,13,22));draw=ImageDraw.Draw(sheet)
    for i,t in enumerate(samples):
        frame=edit(capture/'frames'/f'{t*FPS:05}.jpg',t)
        frame.resize((640,360),Image.Resampling.LANCZOS).save(output/f'preview-{t:02}.jpg',quality=92)
        sheet.paste(frame.resize((640,360),Image.Resampling.LANCZOS),(i%2*640,i//2*390))
        draw.text((i%2*640+12,i//2*390+362),f'{t:02}s',font=FONTS[22],fill='white')
    sheet.save(output/'storyboard.jpg',quality=92)
    if args.storyboard:
        print(output/'storyboard.jpg');return
    report=json.loads((capture/'capture-report.json').read_text())
    assert report['passed'] and report['frames']==720
    for i in range(720):assert (capture/'frames'/f'{i:05}.jpg').is_file(),i
    music=output/'original-synth-bed.wav';soundtrack(music)
    ffmpeg=ROOT.parent/'tmp/media_tools/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
    movie=output/'Dittoches_Promo_Test_1080p.mp4'
    command=[str(ffmpeg),'-y','-hide_banner','-loglevel','warning','-f','rawvideo','-pix_fmt','rgb24','-s','1920x1080','-r','30','-i','pipe:0','-i',str(music),'-c:v','libx264','-preset','medium','-crf','18','-pix_fmt','yuv420p','-c:a','aac','-b:a','192k','-movflags','+faststart','-t','24',str(movie)]
    with (output/'encode.log').open('w') as log:
        process=subprocess.Popen(command,stdin=subprocess.PIPE,stderr=log)
        try:
            for i in range(720):
                process.stdin.write(edit(capture/'frames'/f'{i:05}.jpg',i/FPS).tobytes())
                if i%120==0:print('ENCODE',i,'/ 720',flush=True)
            process.stdin.close()
            if process.wait()!=0:raise RuntimeError('FFmpeg failed; see encode.log')
        except BaseException:
            process.kill();process.wait();raise
    # Decode the entire delivered file, checking real frame count and both streams.
    result=subprocess.run([str(ffmpeg),'-hide_banner','-i',str(movie),'-f','null','-'],capture_output=True,text=True)
    (output/'decode-check.log').write_text(result.stderr,encoding='utf-8')
    assert result.returncode==0 and '1920x1080' in result.stderr and 'Audio: aac' in result.stderr
    assert 'Duration: 00:00:24.00' in result.stderr and re.search(r'frame=\s*720\b',result.stderr), 'Unexpected delivered duration/frame count'
    summary={'passed':True,'file':movie.name,'width':W,'height':H,'fps':FPS,'seconds':SECONDS,'frames':720,'bytes':movie.stat().st_size,'sha256':hashlib.sha256(movie.read_bytes()).hexdigest(),'footage':'Actual current Unity models, production motion/effect renderer and server combat replay. Staged showcase camera shots.','audio':'Original procedural synth bed; no external music.','format':'H.264 / yuv420p + AAC stereo / faststart'}
    (output/'video-report.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
    print('VIDEO READY',movie)


if __name__=='__main__':main()
