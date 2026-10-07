"""Cross-check production C# stat math against server stats for the real catalog."""
import subprocess
import sys
from pathlib import Path


def validate(root,compiler):
    sys.path.insert(0,str(root/'Server'))
    import combat_stats as s
    import combat_builds as b
    lines=[]
    def number(value):return format(value,'.9g')+'f'
    def check(expression,expected):lines.append(f'Check({expression},{number(expected)});')
    for uid,skill in s.SKILLS.items():
        for star in (1,2,3):
            for equipment in ([],[0],[3],[7,12]):
                f=dict(id=uid,star=star,build=b.resolve(uid,[uid],equipment))
                ad=s.attack(f);ap=s.ability_power(f);i=star-1
                check(f'DigimonCombatMath.Attack({number(skill["baseAttack"])},{star},{number(f["build"].get("attack",0))})',ad)
                check(f'DigimonCombatMath.AbilityPower({number(f["build"].get("abilityPower",0))})',ap)
                check(f'DigimonCombatMath.Skill({number(ad)},{number(ap)},{number(skill["adRatio"][i])},{number(skill["apRatio"][i])})',s.skill_damage(f))
    for increment in (0,.04,.07,.11):
        for age in (-1,0,2.99,3,5.99,6,9,12,14.99,15,18,30):
            check(f'DigimonCombatMath.RampSpeed({number(increment)},{number(age)})',s.ramp_speed(increment,age))
            for speed in (0,.08,.4):
                f=dict(id='gabumon',build=dict(speed=speed,rampSpeed=increment),combatAge=age)
                check(f'DigimonCombatMath.AttackSpeed({number(s.SKILLS["gabumon"]["attackSpeed"])},{number(speed)},{number(increment)},{number(age)})',s.attack_speed(f))
    for sx,sy in [(0,0),(3,2),(2.3,4.4),(6,7)]:
        for x in range(7):
            for y in range(8):check(f'DigimonCombatMath.HexDistance({number(sx)},{number(sy)},{x}f,{y}f)',s.hex_distance(sx,sy,x,y))
    for kind in ('physical','magic'):
        for armor,mr in [(0,0),(100,0),(0,100),(15,30),(200,300)]:
            check(f'DigimonCombatMath.Mitigate(360f,{armor}f,{mr}f,"{kind}")',s.mitigate(360,armor,mr,kind))
    out=root/'Builds/Validation';out.mkdir(parents=True,exist_ok=True)
    source=out/'ScalingChecks.cs'
    source.write_text('using System; class ScalingChecks { static int n; static void Check(float a,float b) { n++; if(Math.Abs(a-b)>.002f)throw new Exception("Stat parity "+n+": "+a+" != "+b); } static void Main() {\n'+'\n'.join(lines)+'\nConsole.WriteLine("COMBAT STAT PARITY: "+n+" checks passed."); } }',encoding='utf-8')
    exe=out/'ScalingChecks.exe'
    subprocess.run([str(compiler),'/nologo','/out:'+str(exe),str(root/'Assets/Scripts/DigimonCombatMath.cs'),str(source)],check=True)
    subprocess.run([str(exe)],check=True)


if __name__=='__main__':
    root=Path(__file__).resolve().parent.parent
    validate(root,root.parent/'tmp/roslyn/tasks/net472/csc.exe')
