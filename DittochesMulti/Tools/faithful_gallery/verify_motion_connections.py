"""Check displayed mesh continuity, paused transitions and exact frame seeking."""
import argparse
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT.parent / f'tmp/gallery-test-tools-py{sys.version_info.major}{sys.version_info.minor}'))
import greenlet  # Load the matching native extension before the shared tools.
sys.path.insert(0, str(ROOT.parent / 'tmp/gallery-test-tools'))
from playwright.sync_api import sync_playwright


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--ids', default='')
    parser.add_argument('--baseline', action='store_true')
    parser.add_argument('--output', default='Builds/MotionConnectionValidation-20261006')
    args = parser.parse_args()
    output = ROOT / args.output
    output.mkdir(parents=True, exist_ok=True)
    errors, results = [], []
    with sync_playwright() as pw:
        browser = pw.chromium.launch(
            executable_path='C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',
            headless=True, args=['--use-angle=swiftshader', '--enable-unsafe-swiftshader'])
        page = browser.new_page(viewport={'width':1280, 'height':900})
        page.on('pageerror', lambda error: errors.append(str(error)))
        if args.baseline:
            page.route('**/gallery.js', lambda route: route.fulfill(
                path=output / 'before/gallery.js', content_type='text/javascript'))
        page.goto('http://127.0.0.1:8766/#greymon', wait_until='networkidle', timeout=90000)
        page.wait_for_function('window.faithfulGallery?.state.ready', timeout=90000)
        ids = args.ids.split(',') if args.ids else page.evaluate('faithfulGallery.state.entries.map(e=>e.id)')
        for ident in ids:
            page.evaluate('(id)=>faithfulGallery.selectModel(id)', ident)
            page.wait_for_function('(id)=>faithfulGallery.state.ready&&faithfulGallery.state.selected===id', arg=ident)
            # Expose only test helpers; snapshots are actual skinned world vertices.
            page.evaluate('''() => {
              window.connectionTest = {
                play: name => faithfulGallery.playClip(faithfulGallery.state.clips.indexOf(name)),
                snapshot: () => faithfulGallery.poseSnapshot().positions,
                distance: (a,b) => Math.max(...a.map((v,i)=>Math.abs(v-b[i]))),
              };
            }''')
            page.evaluate('''() => {
              connectionTest.play('Attack');
              faithfulGallery.seekMotion(faithfulGallery.state.animationDuration*.48);
              connectionTest.play('Walk');
            }''')
            page.wait_for_function('faithfulGallery.state.animationTime>.035')
            before = page.evaluate('''() => {
              document.getElementById('animation-play').click();
              return {pose:connectionTest.snapshot(), frame:faithfulGallery.state.frames};
            }''')
            page.wait_for_function('(f)=>faithfulGallery.state.frames>f+8', arg=before['frame'])
            drift = page.evaluate('(before)=>connectionTest.distance(before,connectionTest.snapshot())', before['pose'])
            if args.baseline:
                results.append({'id':ident, 'paused_vertex_drift':drift})
                continue
            assert drift < 1e-8, (ident, 'paused blend moved vertices', drift)
            # An interruption and repeated selection both start at the displayed pose.
            jumps = page.evaluate('''() => {
              const jumps=[];
              for (const name of ['Skill','Run','Hit','Skill','Skill','Guard']) {
                const before=connectionTest.snapshot(); connectionTest.play(name);
                jumps.push(connectionTest.distance(before,connectionTest.snapshot()));
              }
              return jumps;
            }''')
            assert max(jumps) < 1e-8, (ident, 'switch snapped', jumps)
            # Seeking cancels blending and reproduces the exact pose independently
            # of the prior action or the order of frame inspection.
            seek_error = page.evaluate('''() => {
              connectionTest.play('Skill'); faithfulGallery.seekMotion(.4);
              const expected=connectionTest.snapshot();
              connectionTest.play('Walk'); faithfulGallery.seekMotion(.25);
              connectionTest.play('Skill'); faithfulGallery.seekMotion(.4);
              return connectionTest.distance(expected,connectionTest.snapshot());
            }''')
            assert seek_error < 1e-8, (ident, 'seek depends on old blend', seek_error)
            # Confirm a real interruption after the first blend has progressed.
            page.evaluate("connectionTest.play('Run')")
            page.wait_for_function('faithfulGallery.state.animationTime>.08')
            interrupt = page.evaluate('''() => {
              const before=connectionTest.snapshot(); connectionTest.play('Attack');
              return connectionTest.distance(before,connectionTest.snapshot());
            }''')
            assert interrupt < 1e-8, (ident, 'interrupted blend snapped', interrupt)
            page.wait_for_function('faithfulGallery.state.activeClip==="Idle"', timeout=30000)
            assert not page.evaluate('faithfulGallery.state.techniqueEffect.active')
            # The demonstration must preserve the displayed pose when changing
            # from the middle of one motion to the next.
            demo_jump = page.evaluate('''async () => {
              await faithfulGallery.startDemo(true);
              faithfulGallery.seekMotion(faithfulGallery.state.animationDuration*.5);
              const before=connectionTest.snapshot(); await faithfulGallery.advanceDemo();
              const jump=connectionTest.distance(before,connectionTest.snapshot());
              faithfulGallery.stopDemo(); return jump;
            }''')
            assert demo_jump < 1e-8, (ident, 'demo snapped', demo_jump)
            results.append(dict(id=ident, paused_vertex_drift=drift, switch_jumps=jumps,
                                seek_error=seek_error, interrupt_jump=interrupt, demo_jump=demo_jump))
            if ident in ('greymon', 'wargreymon', 'birdramon'):
                page.evaluate('faithfulGallery.seekMotion(faithfulGallery.state.animationDuration*.60)')
                page.screenshot(path=str(output / (ident + '-skill.png')))
            print('MOTION CONNECTION PASS', ident, flush=True)
        browser.close()
    report = dict(passed=not errors, baseline=args.baseline, models=results, errors=errors)
    (output / ('baseline-report.json' if args.baseline else 'connection-report.json')).write_text(
        json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    assert not errors, errors
    print(json.dumps({'passed':not errors, 'models':len(results), 'baseline':args.baseline}), flush=True)


if __name__ == '__main__':
    main()
