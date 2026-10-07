"""Readable action motifs, asymmetric phrasing and secondary reaction per species."""
import math
from faithful_motion_styles import curve
MOTIFS={
 'greymon':'bite','metalgreymon':'cannon','agumon':'claw','wargreymon':'cross',
 'gabumon':'claw','weregarurumon':'combo','togemon':'box','garudamon':'rake',
 'garurumon':'pounce','metalgarurumon':'pounce','angemon':'staff',
 'holyangemon':'blade','seraphimon':'magic','rosemon':'whip','lilimon':'flower',
 'palmon':'vine','piyomon':'peck','tentomon':'rake','kabuterimon':'ram',
 'atlur':'ram','herakle':'ram','kuwagamon':'rake','birdramon':'flight',
 'hououmon':'flight','koromon':'bounce','tsunomon':'bounce','mochimon':'bounce',
 'tanemon':'spore','tokomon':'bounce','pyocomon':'spore','patamon':'flutter',
 'devimon':'claw','etemon':'perform','shellmon':'brace'}

def pulse(u,a,b,c,d):return curve(u,[(0,0),(a,0),(b,1),(c,1),(d,0),(1,0)])

def phrase(ident,mode,u):
    motif=MOTIFS[ident];skill=mode=='Skill';action=mode in ('Attack','Skill')
    wind=pulse(u,.02,.25 if not skill else .35,.29 if not skill else .42,.45 if not skill else .60) if action else 0
    strike=pulse(u,.29 if not skill else .43,.43 if not skill else .60,.49 if not skill else .69,.89 if not skill else .96) if action else 0
    second=pulse(u,.49,.62,.66,.93) if mode=='Attack' and motif in ('combo','box','cross','rake') else 0
    if mode=='Attack' and motif in ('combo','box','cross','rake'):strike=pulse(u,.24,.37,.40,.57)
    body_twist=0
    if motif in ('claw','combo','box','cross','whip','vine','blade','staff','perform','rake'):
        body_twist=-9*wind+13*strike-10*second
    if skill and motif in ('magic','flower','spore','cannon'):body_twist=2*wind-3*strike
    if motif=='whip':body_twist*=-1
    return dict(motif=motif,wind=wind,strike=strike,second=second,twist=body_twist,
        step=pulse(u,.12,.32,.65,.97) if action and motif not in ('bounce','spore','flutter','flight','brace') else 0,
        glance=curve(u,[(0,0),(.14,0),(.28,1),(.39,.85),(.56,0),(.67,0),(.8,-.65),(.92,0),(1,0)]) if mode=='Idle' else 0,
        cheer=pulse(u,.04,.29,.65,.96) if mode=='Victory' else 0)

def arm_delta(s,mode,side,secondary,reach):
    from mathutils import Vector
    w,a,b=s['wind'],s['strike'],s['second'];motif=s['motif'];skill=mode=='Skill'
    combo=mode=='Attack' and motif in ('combo','box','cross','rake')
    lead=a if side<0 else b if combo else a*.3
    if motif=='whip':lead=a if side>0 else a*.3
    if secondary:lead=b if combo else a*.55
    if skill:lead=a
    y=w*.11-lead*reach;z=-w*.06+lead*.15;x=-side*w*.06
    if motif in ('claw','whip','vine','blade','cross'):
        x+=side*w*.14-side*lead*.20;z+=w*.13-lead*.05
    if motif in ('box','combo'):z+=w*.15+lead*.07;y-=lead*.08
    if motif in ('bite','peck','brace'):y=w*.06-a*.15;z=w*.01+a*.05;x=side*a*.025
    if motif in ('ram','rake'):x+=side*a*.09;z+=w*.08-a*.04
    if skill and motif in ('magic','flower','spore'):
        x=-side*w*.13+side*a*.15;y=w*.025-a*.18;z=w*.24+a*.30
    if skill and motif=='cannon':x=-side*w*.03;y=-a*(.34 if side>0 else .12);z=w*.12+a*.10
    if skill and motif=='bite':x=side*a*.1;y=w*.10-a*.12;z=w*.10+a*.08
    if skill and motif=='cross':x=-side*w*.13+side*a*.10;y=-a*.22;z=w*.83+a*.95
    return Vector((x,y,z))*(.65 if secondary else 1)
