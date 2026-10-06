"""Assemble the actual --gameplay-capture frames with their recorded wall-clock timing.
Preserves the full game UI; produces a 30-second 1080p MP4 with guide chapters.
"""
import argparse,json,subprocess,hashlib,re,statistics
from pathlib import Path
from PIL import Image,ImageChops,ImageStat

ROOT=Path(__file__).resolve().parent.parent
FFMPEG=ROOT.parent/'tmp/media_tools/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
OUTPUT=ROOT/'Builds/GameplayVideo-20261006'

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--capture',type=Path,default=ROOT/'Builds/CombatReadabilityPreview/GameplayCaptureSmooth-20261006')
    parser.add_argument('--verify-only',action='store_true')
    args=parser.parse_args();capture=args.capture.resolve()
    report=json.loads((capture/'capture-report.json').read_text());frames=report['frames']
    assert report['passed'] and report['width']==1920 and report['height']==1080 and len(frames)>=200
    assert len(set(f['section'] for f in frames))==6
    offset=frames[0]['time'];lines=['ffconcat version 1.0'];chapters=[]
    names={'prepare':'준비 화면','synergy-courage':'시너지 도감 · 용기','synergy-friendship':'시너지 도감 · 우정',
           'equipment-shield':'장비 도감 · 브레이브 실드','equipment-weapon':'장비 도감 · 베렌헤나','combat':'실제 자동 전투'}
    for i,frame in enumerate(frames):
        path=capture/frame['file'];assert path.is_file(),path
        stamp=frame['time']-offset
        if not chapters or chapters[-1][1]!=frame['section']:chapters.append((stamp,frame['section']))
        duration=(frames[i+1]['time']-frame['time']) if i+1<len(frames) else max(.001,30-stamp)
        assert duration>0
        lines += ["file '"+path.as_posix()+"'",f'duration {duration:.9f}']
    lines += ["file '"+(capture/frames[-1]['file']).as_posix()+"'"]
    OUTPUT.mkdir(exist_ok=True);concat=OUTPUT/'frames.ffconcat';concat.write_text('\n'.join(lines)+'\n',encoding='utf-8')
    meta=[';FFMETADATA1','title=Dittoches · 시너지와 장비 도감','comment=실제 게임 화면 · 시연용 배치 · 자동 전투']
    for i,(stamp,section) in enumerate(chapters):
        end=chapters[i+1][0] if i+1<len(chapters) else 30
        meta += ['[CHAPTER]','TIMEBASE=1/1000','START='+str(round(stamp*1000)),'END='+str(round(end*1000)),'title='+names[section]]
    metadata=OUTPUT/'chapters.ffmetadata';metadata.write_text('\n'.join(meta)+'\n',encoding='utf-8')
    movie=OUTPUT/'Dittoches_Gameplay_Guides_1080p.mp4'
    if not args.verify_only:subprocess.run([str(FFMPEG),'-y','-hide_banner','-loglevel','warning','-safe','0','-f','concat','-i',str(concat),'-i',str(metadata),
                    '-map','0:v','-map_metadata','1','-t','30','-r','30','-c:v','libx264','-preset','medium','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(movie)],check=True)
    result=subprocess.run([str(FFMPEG),'-hide_banner','-i',str(movie),'-f','null','-'],capture_output=True,text=True,encoding="utf-8",errors="replace")
    (OUTPUT/'decode-check.log').write_text(result.stderr,encoding='utf-8')
    assert result.returncode==0 and '1920x1080' in result.stderr and re.search(r'frame=\s*900\b',result.stderr)
    previews={}
    for second in [1,5,9,13,17,21,24,27,28]:
        image=OUTPUT/f'preview-{second:02}.png'
        subprocess.run([str(FFMPEG),'-y','-hide_banner','-loglevel','error','-ss',str(second),'-i',str(movie),'-frames:v','1',str(image)],check=True)
        previews[second]=Image.open(image).convert('RGB')
    # Reject stale-window recordings: both guides and the battle must actually change.
    differences={}
    for a,b in [(1,5),(5,9),(9,13),(13,17),(17,24),(21,24),(24,27)]:
        differences[f'{a}-{b}']=sum(ImageStat.Stat(ImageChops.difference(previews[a],previews[b])).mean)/3
        assert differences[f'{a}-{b}']>.3,(a,b,'stale or missing section')
    summary={'passed':True,'file':movie.name,'seconds':30,'width':1920,'height':1080,'fps':30,'frames':900,'sourceFrames':len(frames),
             'sourceMedianInterval':statistics.median(b['time']-a['time'] for a,b in zip(frames,frames[1:])),
             'bytes':movie.stat().st_size,'sha256':hashlib.sha256(movie.read_bytes()).hexdigest(),
             'recording':'Actual Unity screen including production UI and solo combat. Staged formation; guide navigation scripted. Captured timestamps preserved, resampled to 30fps.',
             'audio':'none','chapters':[{'start':round(t,2),'title':names[s]} for t,s in chapters],'visualDifferences':differences}
    (OUTPUT/'video-report.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
    print('VIDEO READY',movie,flush=True)

if __name__=='__main__':main()
