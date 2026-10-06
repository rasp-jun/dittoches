"""Reuse this project's existing Windows Mono player for a local code preview.

Does not build or import Unity assets. Requires a matching existing player and
Roslyn csc.exe; no Editor, account, license activation or extra Python packages.
"""
import argparse
import shutil
import subprocess
from pathlib import Path


def main():
    root=Path(__file__).resolve().parent.parent
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--compiler',type=Path,default=root.parent/'tmp/roslyn/tasks/net472/csc.exe')
    parser.add_argument('--player',type=Path,default=root/'Builds/Windows')
    parser.add_argument('--destination',type=Path,default=root/'Builds/PortablePreview')
    args=parser.parse_args()
    managed=args.player/'DittochesMulti_Data/Managed'
    if not args.compiler.is_file():
        parser.error('Pass --compiler with a Roslyn csc.exe path.')
    if not (args.player/'MonoBleedingEdge').is_dir() or not (managed/'Assembly-CSharp.dll').is_file():
        parser.error('A matching Windows Mono player is required.')
    destination=args.destination
    if destination.resolve()==args.player.resolve() or destination.resolve().is_relative_to(args.player.resolve()):
        parser.error('Preview destination must differ from the source player.')
    destination.mkdir(parents=True,exist_ok=True)
    stage=destination/'compile-stage';stage.mkdir(exist_ok=True)
    output=stage/'Assembly-CSharp.dll'
    references=list(managed.glob('UnityEngine*.dll'))+[managed/n for n in ('mscorlib.dll','netstandard.dll','System.dll','System.Core.dll')]
    arguments=['/nologo','/target:library','/nostdlib+','/langversion:latest','/define:DITTOCHES_PORTABLE_PREVIEW','/out:'+str(output)]
    arguments+=['/reference:'+str(p) for p in references]+[str(p) for p in (root/'Assets/Scripts').glob('*.cs')]
    response=destination/'compile.rsp'
    response.write_text('\n'.join('"'+a+'"' for a in arguments),encoding='utf-8-sig')
    subprocess.run([str(args.compiler),'@'+str(response)],check=True)
    shutil.copytree(args.player,destination,dirs_exist_ok=True)
    shutil.copy2(output,destination/'DittochesMulti_Data/Managed/Assembly-CSharp.dll')
    output.unlink()
    streaming=destination/'DittochesMulti_Data/StreamingAssets'
    streaming.mkdir(exist_ok=True)
    faithful=root/'Assets/StreamingAssets/FaithfulModels'
    if faithful.is_dir():shutil.copytree(faithful,streaming/'FaithfulModels',dirs_exist_ok=True)
    portraits=root/'Assets/StreamingAssets/FaithfulPortraits'
    if portraits.is_dir():shutil.copytree(portraits,streaming/'FaithfulPortraits',dirs_exist_ok=True)
    for catalog in ('DigimonSkills.json','DigimonBuilds.json'):
        shutil.copy2(root/'Assets/Resources'/catalog,streaming/catalog)
    model=root/'Assets/Resources/Models/Agumon/Agumon.bytes'
    if model.is_file():
        (streaming/'Models').mkdir(exist_ok=True)
        shutil.copy2(model,streaming/'Models/Agumon.bytes')
    for model in (root/'Assets/Resources/Models/Roster').glob('*.bytes'):
        (streaming/'Models').mkdir(exist_ok=True)
        shutil.copy2(model,streaming/'Models'/model.name)
    art=root/'Assets/Resources/ArtVariants/LicensedFanArt'
    for source in art.glob('*.png'):
        target=streaming/'PreviewArt/ArtVariants/LicensedFanArt'/source.name
        target.parent.mkdir(parents=True,exist_ok=True)
        shutil.copy2(source,target)
    print('Portable preview:',destination)
    print('Reused imported player assets; StreamingAssets models included; separate preview saves.')


if __name__=='__main__':
    main()
