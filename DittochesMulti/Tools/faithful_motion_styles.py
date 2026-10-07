"""Species-specific pacing and weight for the common animation vocabulary."""
GROUPS={
    'heavy':{'greymon','metalgreymon','wargreymon','garudamon','herakle','atlur','togemon'},
    'light':{'agumon','gabumon','palmon','piyomon','tentomon'},
    'human':{'weregarurumon','angemon','holyangemon','seraphimon','rosemon','lilimon','devimon','etemon'},
    'baby':{'koromon','tsunomon','pyocomon','mochimon','tanemon','tokomon','patamon'},
    'quadruped':{'garurumon','metalgarurumon'},'flier':{'birdramon','hououmon'},
    'insect':{'kabuterimon','kuwagamon'},'shell':{'shellmon'},
}
SETTINGS={
    'heavy':dict(walk=1.50,run=1.0,attack=1.50,skill=2.1,stride=.13,lift=.075,bob=.012,reach=.16,sway=.018),
    'light':dict(walk=1.14,run=.8,attack=1.25,skill=1.9,stride=.10,lift=.08,bob=.016,reach=.14,sway=.015),
    'human':dict(walk=1.30,run=.9,attack=1.35,skill=2.0,stride=.13,lift=.085,bob=.010,reach=.23,sway=.018),
    'baby':dict(walk=1.10,run=.8,attack=1.2,skill=1.8,stride=.06,lift=.11,bob=.018,reach=.06,sway=.009),
    'quadruped':dict(walk=1.40,run=.86,attack=1.3,skill=1.9,stride=.18,lift=.085,bob=.012,reach=.11,sway=.012),
    'flier':dict(walk=1.70,run=1.16,attack=1.5,skill=2.1,stride=.08,lift=.09,bob=.025,reach=.10,sway=.008),
    'insect':dict(walk=1.40,run=.94,attack=1.4,skill=2.0,stride=.11,lift=.075,bob=.012,reach=.14,sway=.012),
    'shell':dict(walk=1.80,run=1.24,attack=1.6,skill=2.2,stride=.045,lift=.035,bob=.006,reach=.085,sway=.006),
}

def style_for(ident):
    group=next(name for name,ids in GROUPS.items() if ident in ids)
    result=dict(group=group,**SETTINGS[group])
    if ident!='birdramon':
        for key,multiplier in [('stride',1.45),('lift',1.25),('bob',1.8),('reach',1.65),('sway',1.9)]:result[key]*=multiplier
    return result

def smooth01(value):
    t=min(1,max(0,float(value)));return t*t*t*(10+t*(-15+6*t))

def curve(u,keys):
    if u<=keys[0][0]:return keys[0][1]
    for (a,va),(b,vb) in zip(keys,keys[1:]):
        if u<=b:return va+(vb-va)*smooth01((u-a)/(b-a))
    return keys[-1][1]
