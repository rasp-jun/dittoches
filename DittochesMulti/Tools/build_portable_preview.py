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
    args=parser.parse_args()
    managed=args.player/'DittochesMulti_Data/Managed'
    if not args.compiler.is_file():
        parser.error('Pass --compiler with a Roslyn csc.exe path.')
    if not (args.player/'MonoBleedingEdge').is_dir() or not (managed/'Assembly-CSharp.dll').is_file():
        parser.error('A matching Windows Mono player is required.')
    destination=root/'Builds/PortablePreview'
    if destination.resolve()==args.player.resolve():
        parser.error('Preview destination must differ from the source player.')
    shutil.copytree(args.player,destination,dirs_exist_ok=True)
    output=destination/'DittochesMulti_Data/Managed/Assembly-CSharp.dll'
    references=list(managed.glob('UnityEngine*.dll'))+[managed/n for n in ('mscorlib.dll','netstandard.dll','System.dll','System.Core.dll')]
    arguments=['/nologo','/target:library','/nostdlib+','/langversion:latest','/define:DITTOCHES_PORTABLE_PREVIEW','/out:'+str(output)]
    arguments+=['/reference:'+str(p) for p in references]+[str(p) for p in (root/'Assets/Scripts').glob('*.cs')]
    response=destination/'compile.rsp'
    response.write_text('\n'.join('"'+a+'"' for a in arguments),encoding='utf-8-sig')
    subprocess.run([str(args.compiler),'@'+str(response)],check=True)
    streaming=destination/'DittochesMulti_Data/StreamingAssets'
    streaming.mkdir(exist_ok=True)
    for catalog in ('DigimonSkills.json','DigimonBuilds.json'):
        shutil.copy2(root/'Assets/Resources'/catalog,streaming/catalog)
    art=root/'Assets/Resources/ArtVariants/LicensedFanArt'
    for source in art.glob('*.png'):
        target=streaming/'PreviewArt/ArtVariants/LicensedFanArt'/source.name
        target.parent.mkdir(parents=True,exist_ok=True)
        shutil.copy2(source,target)
    print('Portable preview:',destination)
    print('Existing player assets; substitute shader; separate preview saves.')


if __name__=='__main__':
    main()
