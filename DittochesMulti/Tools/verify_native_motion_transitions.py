"""Inspect every exported native-rig frame and common-action recovery."""
import json,hashlib
from pathlib import Path
import numpy as np
from extend_native_motion import Native
from faithful_motion_catalog import clips_for
ROOT=Path(__file__).resolve().parents[1]

def main():
    reports=[]
    for ident in ['birdramon','kuwagamon']:
        path=ROOT/'ArtSource/AnimatedReview'/ident/'model.glb';m=Native(path)
        neutral=np.concatenate(m.surfaces(m.sample('Idle',0)));height=np.ptp(neutral[:,1]);scale=2.4/height
        rows=[];faults=[]
        bones=sorted({j for skin in m.doc['skins'] for j in skin['joints']})
        for name,duration,loop in clips_for(ident):
            first=np.concatenate(m.surfaces(m.sample(name,0)));last=np.concatenate(m.surfaces(m.sample(name,1)))
            entry=float(np.abs(first-neutral).max()*scale) if not loop else None
            recovery=float(np.abs(last-neutral).max()*scale) if not loop and name!='Down' else None
            closure=float(np.abs(first-last).max()*scale) if loop else None
            max_step=0;previous=None;worst=None
            for frame,t in enumerate(np.linspace(0,1,round(duration*30)+1)):
                joints=m.world(m.sample(name,t))[bones,:3,3]
                if previous is not None:
                    distances=np.linalg.norm(joints-previous,axis=1)*scale;step=float(distances.max())
                    if step>max_step:
                        j=int(distances.argmax());max_step=step
                        worst=dict(frame=frame,bone=m.nodes[bones[j]].get('name'),before=previous[j].tolist(),after=joints[j].tolist())
                previous=joints
            for check,value in [('entry',entry),('recovery',recovery),('loop',closure)]:
                if value is not None and value>1e-4:faults.append(name+' '+check)
            if max_step>.32:faults.append(name+' joint jump')
            rows.append(dict(clip=name,entry_error=entry,recovery_error=recovery,loop_error=closure,max_frame_joint_step=max_step,worst=worst))
        reports.append(dict(id=ident,passed=not faults,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),clips=rows,faults=faults))
        print('NATIVE TRANSITION',ident,'PASS' if not faults else faults,flush=True)
    out=ROOT/'Builds/FaithfulNaturalValidation/native-transition-report.json'
    out.write_text(json.dumps(dict(passed=all(r['passed'] for r in reports),models=reports),indent=2),encoding='utf-8')
    assert all(r['passed'] for r in reports)

if __name__=='__main__':main()
