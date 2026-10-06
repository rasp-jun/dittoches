"""Shared authored motion names and timing; source clips remain separately labeled."""
CLIPS=[('Idle',3.6,True),('Walk',1.3,True),('Run',.9,True),
       ('Attack',1.4,False),('Skill',2.0,False),('Guard',1.8,False),
       ('Dodge',1.0,False),('Hit',.8,False),('Victory',3.2,False),('Down',2.2,False)]
DURATIONS={name:duration for name,duration,_ in CLIPS}
BASELINE='ArtSource/TechniqueMotionBackup-20261002'

def clips_for(ident):
    from faithful_motion_styles import style_for
    style=style_for(ident)
    durations={'Walk':style['walk'],'Run':style['run'],'Attack':style['attack'],'Skill':style['skill']}
    if ident!='birdramon':durations['Idle']=6.4 if style['group']=='baby' else 7.2
    if ident=='pyocomon':durations['Attack']=1.6
    from faithful_techniques import TECHNIQUES
    for name in ('Attack','Skill'):durations[name]=TECHNIQUES[ident][name]['duration']
    # Bake whole frames so the stored duration agrees exactly with 30 fps keys.
    return [(name,round(durations.get(name,duration)*30)/30,loop) for name,duration,loop in CLIPS]
