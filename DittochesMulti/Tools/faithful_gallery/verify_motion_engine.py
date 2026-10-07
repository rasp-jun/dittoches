"""Frame-rate independence, outgoing velocity, buffer reuse and gait phase."""
import sys,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT.parent/f'tmp/gallery-test-tools-py{sys.version_info.major}{sys.version_info.minor}'))
import greenlet
sys.path.insert(0,str(ROOT.parent/'tmp/gallery-test-tools'))
from playwright.sync_api import sync_playwright
with sync_playwright() as pw:
    browser=pw.chromium.launch(executable_path='C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless=True,args=['--use-angle=swiftshader','--enable-unsafe-swiftshader'])
    page=browser.new_page();errors=[];page.on('pageerror',lambda e:errors.append(str(e)))
    page.goto('http://127.0.0.1:8766/#agumon',wait_until='networkidle')
    page.wait_for_function('window.faithfulGallery?.state.ready')
    report=page.evaluate('''async()=>{
      const THREE=await import('three');const {PoseTransition}=await import('/Tools/faithful_gallery/pose-transition.js');
      function trial(fps){
        const node=new THREE.Object3D(),blend=new PoseTransition(node),buffer=blend.buffer;
        node.position.x=0;blend.record(1/fps);node.position.x=1/fps;blend.record(1/fps);
        const source=blend.capture(),velocity=blend.captureVelocity();
        node.position.x=1;blend.begin(source,.24,velocity);
        const entry=node.position.x;
        for(let i=1;i<=fps/10;i++){blend.beforeUpdate();node.position.x=1+2*i/fps;blend.apply(1/fps);}
        if(blend.buffer!==buffer)throw Error('allocated a new target buffer');
        return {fps,value:node.position.x,source:entry};
      }
      const values=[30,60,120].map(trial);
      // Subtract the source contribution because each fps starts one frame apart.
      const u=.1/.24,w=u*u*u*(u*(u*6-15)+10);
      const adjusted=values.map(v=>v.value-v.source*(1-w));
      if(Math.max(...adjusted)-Math.min(...adjusted)>1e-8)throw Error('frame-rate dependent blend');
      const node=new THREE.Object3D(),blend=new PoseTransition(node);
      blend.record(.016);node.position.x=.016;blend.record(.016);
      const from=blend.capture(),velocity=blend.captureVelocity();node.position.x=1;blend.begin(from,.24,velocity);
      const initial=node.position.x;blend.beforeUpdate();node.position.x=1;blend.apply(.0001);
      const initialVelocity=(node.position.x-initial)/.0001;
      if(Math.abs(initialVelocity-1)>.01)throw Error('outgoing velocity stopped');
      const g=faithfulGallery;g.playClip(g.state.clips.indexOf('Walk'),false);g.seekMotion(g.state.animationDuration*.37);
      g.playClip(g.state.clips.indexOf('Run'));const phase=g.state.animationTime/g.state.animationDuration;
      if(Math.abs(phase-.37)>1e-6)throw Error('walk-run foot phase reset');
      return {passed:true,values,initialVelocity,gaitPhase:phase,bufferReused:true};
    }''')
    browser.close()
assert not errors,errors
out=ROOT/'Builds/MotionEngine-20261006';out.mkdir(exist_ok=True)
(out/'engine-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
