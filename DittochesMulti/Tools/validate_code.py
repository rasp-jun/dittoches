"""Compile runtime C# and run deterministic motion/skill checks without Unity Editor."""
import argparse
import subprocess
from pathlib import Path


def main():
    root=Path(__file__).resolve().parent.parent
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--compiler',type=Path,default=root.parent/'tmp/roslyn/tasks/net472/csc.exe')
    args=parser.parse_args()
    out=root/'Builds/Validation'
    out.mkdir(parents=True,exist_ok=True)
    managed=root/'Builds/Windows/DittochesMulti_Data/Managed'
    references=list(managed.glob('UnityEngine*.dll'))+[managed/n for n in ('mscorlib.dll','netstandard.dll','System.dll','System.Core.dll')]
    if not args.compiler.is_file() or not references[0].is_file():
        parser.error('A Roslyn compiler and the existing Mono player references are required.')
    runtime=['/nologo','/target:library','/nostdlib+','/langversion:latest','/out:'+str(out/'Arena.Runtime.dll')]
    runtime+=['/reference:'+str(p) for p in references]+[str(p) for p in (root/'Assets/Scripts').glob('*.cs')]
    response=out/'runtime-check.rsp'
    response.write_text('\n'.join('"'+a+'"' for a in runtime),encoding='utf-8-sig')
    subprocess.run([str(args.compiler),'@'+str(response)],check=True)
    entry=out/'CheckMain.cs'
    entry.write_text('public static class CheckMain { public static void Main() { CombatMotionValidation.Validate(); SkillTimingValidation.Validate(); } }',encoding='utf-8')
    checks=out/'GameplayChecks.exe'
    sources=[root/'Assets/Scripts/CombatSeparation.cs',root/'Assets/Scripts/SkillGeometry.cs',root/'Assets/Editor/CombatMotionValidation.cs',root/'Assets/Editor/SkillTimingValidation.cs',entry]
    subprocess.run([str(args.compiler),'/nologo','/out:'+str(checks)]+[str(p) for p in sources],check=True)
    subprocess.run([str(checks)],check=True)
    print('Runtime compile and gameplay checks passed. UI/shader validation still requires a running player.')


if __name__=='__main__':
    main()
