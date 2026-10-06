"""Remove anatomical cross-binding exposed by larger, articulated motions."""
import numpy as np
def smooth(x):
    x=np.clip(x,0,1);return x*x*(3-2*x)

def repair(ident,meshes):
    if ident not in ('gabumon','herakle'):return 0
    changed=0
    for obj in meshes:
        names=[g.name for g in obj.vertex_groups];n=len(obj.data.vertices)
        xyz=np.array([v.co[:] for v in obj.data.vertices]);x,y,z=xyz.T;w=np.zeros((n,len(names)))
        for v in obj.data.vertices:
            for g in v.groups:w[v.index,g.group]=g.weight
        original=w.copy();arms=[i for i,k in enumerate(names) if 'Arm' in k or 'Hand' in k]
        if ident=='gabumon':
            # The back of the fur coat follows the trunk, not either wrist.
            fur_arms=[i for i,k in enumerate(names) if 'arm' in k.lower() or 'Hand' in k]
            mask=smooth((y-.09)/.17)*smooth((z-.64)/.22)*(1-smooth((z-1.23)/.17))*(1-smooth((np.abs(x)-.45)/.25))
            amount=w[:,fur_arms].sum(1)*mask;w[:,fur_arms]*=(1-mask[:,None]);w[:,names.index('Spine')]+=amount
            # Separate the inside forearm from the neighbouring short leg.
            for side,s in [(-1,'L'),(1,'R')]:
                leg=[i for i,k in enumerate(names) if k in ('Thigh'+s,'Shin'+s,'Foot'+s)]
                mask=smooth((side*x-.39)/.13)*smooth((z-.40)/.15)*(1-smooth((z-.81)/.16))*(1-smooth((y+.04)/.18))
                amount=w[:,leg].sum(1)*mask;w[:,leg]*=(1-mask[:,None]);w[:,names.index('Forearm'+s)]+=amount
        else:
            # Central abdominal armour was partially assigned to both pairs
            # of arms. Keep the armour with the body as the arms counter-swing.
            mask=(1-smooth((np.abs(x)-.34)/.18))*(1-smooth((y+.28)/.18))*smooth((z-.79)/.12)*(1-smooth((z-1.13)/.16))
            amount=w[:,arms].sum(1)*mask;w[:,arms]*=(1-mask[:,None]);w[:,names.index('Spine')]+=amount
        affected=np.flatnonzero(np.abs(w-original).max(1)>1e-6);changed+=len(affected)
        for group in obj.vertex_groups:group.remove(affected.tolist())
        for index in affected:
            keep=np.argsort(w[index])[-4:];values=w[index,keep];values/=values.sum()
            for group,value in zip(keep,values):
                if value>1e-7:obj.vertex_groups[int(group)].add([int(index)],float(value),'REPLACE')
    return changed
