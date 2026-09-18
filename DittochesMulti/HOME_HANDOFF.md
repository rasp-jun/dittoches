# 다른 컴퓨터에서 이어서 작업하기

기준일: 2026-09-18. 작업 맥락은 [NEXT_SESSION.md](NEXT_SESSION.md), 전체 진행표는 [PROJECT_STATUS.md](PROJECT_STATUS.md)에 있습니다. 최신 기능은 연속 드래그·모션 전환·전투 체력바 배치와 온라인 경제 UI 개선이며, 최신 작업은 [GitHub develop](https://github.com/rasp-jun/dittoches/tree/develop)에 업로드했습니다. main은 이전 상태입니다.

## 가장 빠른 방법: USB 보존 폴더 전체 사용

1. `Dittoches_KEEP_20260917` 폴더 전체를 다른 PC에 복사하거나 USB에서 그대로 사용합니다. 드라이브 문자는 달라도 됩니다.
2. IDE/Codex에서는 `01_CurrentProject/DittochesMulti`를 엽니다. Git 저장소 루트는 한 단계 위 `01_CurrentProject`입니다. 숨김 폴더 `.git`도 함께 옮겨야 합니다.
3. 이 프로젝트의 `Play_Preview.bat` 또는 보존 폴더 루트의 `Play_Current_Preview.bat`를 더블클릭하면 마지막으로 검증한 미리보기를 실행합니다. 실행 파일만 따로 복사하지 마세요.
4. 새 대화에는 “NEXT_SESSION.md와 HOME_HANDOFF.md를 읽고 develop에서 이어서 작업해”라고 입력합니다.
5. 소스를 수정한 뒤에는 아래 재컴파일 또는 정식 Unity 빌드를 수행합니다. `Play_Preview.bat` 자체는 코드를 다시 컴파일하지 않습니다.

보존 폴더 루트의 `Play_Windows.bat`는 9월 15일 이전 빌드입니다. 현재 작업 실행 경로와 다릅니다.

## 반드시 함께 옮길 것

| 위치 (보존 폴더 기준) | 용도 |
|---|---|
| `01_CurrentProject/.git` | 커밋과 develop 브랜치 |
| `01_CurrentProject/DittochesMulti` 전체 | 현재 소스·설정·에셋·실행본·문서 |
| `01_CurrentProject/DittochesMulti/Builds/PortablePreview` 전체 | 현재 코드가 적용된 실행본, 데이터·DLL·Mono 런타임 포함 |
| `01_CurrentProject/DittochesMulti/Builds/Windows` 전체 | 미리보기 재컴파일의 원본 플레이어와 Unity 참조 DLL |
| `01_CurrentProject/tmp/roslyn` 전체 | Unity Editor 없는 PC의 C# 컴파일러 |
| `01_CurrentProject/tmp/mingit`, `tmp/github-cli` | 선택: 준비한 Git·GitHub CLI 실행본 |
| `02_ArtVault` | 현재 프로젝트 이미지의 별도 보관본 |
| `03_PreviousWork.zip`, `04_Android` | 이전 작업과 이전 Android 빌드 보관용 |

저장 시점에 필수 경로가 모두 존재했고, 현재 Resources의 이미지·아트 53개는 `02_ArtVault/Resources`와 SHA-256이 모두 일치했습니다. 보존 폴더 루트의 `CURRENT_HANDOFF.json`에는 이번 저장 시점의 Git HEAD, 소스·에셋·실행본·도구 파일 해시를 기록합니다. `FILE_HASHES.json`과 `PRESERVATION_COMPLETE.json`은 최초 보존 시점 기록이므로 덮어쓰지 않았습니다.

## Unity 없이 수정한 코드 실행

Windows에서 Python 3.12로 검증했습니다. Python과 .NET Framework 기반 Roslyn 실행 환경이 필요합니다. 아래 명령은 `DittochesMulti` 폴더의 터미널에서 실행합니다.

```powershell
python Tools/build_portable_preview.py
.\Play_Preview.bat
```

기본 컴파일러 위치는 `../tmp/roslyn/tasks/net472/csc.exe`입니다. 다른 위치라면 `--compiler "C:/.../csc.exe"`를 지정합니다. 실행 중인 미리보기는 닫고 재컴파일합니다.

미리보기는 기존 Mono 플레이어에 현재 C#과 두 JSON을 넣습니다. 대체 셰이더를 사용하고 Unity 에셋을 새로 임포트하지 않습니다. 일반 플레이는 2D 캐릭터 이미지이며 전체 3D 게임이 완성된 상태는 아닙니다. `Skills_Preview.bat`는 기술 연출, `Models_Preview.bat`는 아구몬 한 종의 미완성 3D 시험본 확인용입니다.

## Unity가 있는 PC

Unity Hub에서 `01_CurrentProject/DittochesMulti`를 추가하고 `ProjectSettings/ProjectVersion.txt`의 **6000.6.0f1**로 엽니다. 임포트 완료 후 `Assets/Scenes/Bootstrap.unity`를 열어 Play하고, 새 Windows 빌드에서 셰이더·아트·입력을 확인합니다. 아구몬 시험본의 일반 플레이 적용은 별도 작업이며 현재 기본값은 비활성입니다.

## GitHub에서 새로 받을 때

코드만 clone해서는 현재 에셋과 미리보기 실행본이 모두 준비되지 않습니다. USB 보존 폴더도 필요합니다. 아래는 **아직 수정하지 않은 새 clone**에서만 실행하는 절차입니다.

```powershell
git clone --branch develop --origin dittoches https://github.com/rasp-jun/dittoches.git 01_CurrentProject
cd 01_CurrentProject
powershell -NoProfile -ExecutionPolicy Bypass -File ./Tools/Sync-ExternalAssets.ps1 -Mode Restore -AssetVault "E:/Dittoches_KEEP_20260917/02_ArtVault"
git restore --source=HEAD -- DittochesMulti/Assets/Resources
```

예시의 E:는 실제 USB 위치로 바꿉니다. 마지막 명령은 보관소에서 함께 복사된 예전 JSON·메타데이터 대신 Git의 최신 정의와 GUID를 유지합니다. 이미 작업한 폴더에는 이 복원 절차를 적용하지 않습니다.

Unity 없이 실행/재컴파일하려면 USB에서 `DittochesMulti/Builds`와 `01_CurrentProject/tmp/roslyn`도 같은 상대 위치로 복사합니다. 정식 Unity 빌드를 만들 수 있으면 기존 플레이어에 의존하지 않아도 됩니다.

## 새 PC의 GitHub 로그인

이 PC에서는 GitHub CLI로 `rasp-jun` 인증 후 `dittoches/develop` 업로드와 원격 SHA 일치를 확인했습니다. 자격증명은 Windows 저장소에 있으므로 USB 복사만으로 새 PC에 로그인되지는 않습니다.

Git과 gh를 설치했다면 `gh auth login --hostname github.com --git-protocol https --web`로 브라우저 승인을 진행합니다. 사용자 계정과 대상 저장소를 확인한 뒤 업로드합니다. GitHub 연결 앱의 쓰기 403과 로컬 gh 인증은 별개입니다.

USB에 준비한 도구를 그대로 쓸 때는 `DittochesMulti` 폴더의 PowerShell에서:

```powershell
$env:Path = (Resolve-Path ../tmp/mingit/cmd).Path + ";" + (Resolve-Path ../tmp/github-cli/bin).Path + ";" + $env:Path
gh auth login --hostname github.com --git-protocol https --web
```

USB의 드라이브 문자가 달라졌다면 이 저장소의 기존 GitHub 자격증명 도우미 경로를 갱신합니다. 현재 PC는 절대 경로의 gh 실행본을 사용하도록 연결되어 있습니다.

```powershell
git config --local --replace-all credential.https://github.com.helper "!gh auth git-credential"
git remote -v
git status -sb
```

이는 Git과 gh가 PATH에 있는 터미널에서 실행합니다. USB 파일 시스템의 소유권 경고가 나오면 메시지에 표시된 **현재 Git 루트 하나만** safe.directory로 등록합니다. 이 USB 저장소의 `origin`은 이전 digimon-game일 수 있으므로 업로드는 `git push dittoches develop`을 사용합니다.

## 온라인 실행과 검증

솔로는 서버가 필요 없습니다. 현재 온라인은 두 사용자 1대1 시험 구현입니다. 아래는 로컬 접속용 서버 실행 명령입니다.

```powershell
python Server/server.py --host 127.0.0.1 --port 7777
```

클라이언트 주소는 `http://127.0.0.1:7777`입니다. 두 PC 연결은 서버의 LAN 주소·방화벽·클라이언트 주소를 별도로 설정하고 확인해야 합니다.

코드 변경 범위에 맞춰 다음 검사를 사용합니다.

```powershell
python Tools/validate_code.py
python -m unittest discover -s Server -v
python Tools/build_portable_preview.py
python Tools/validate_online.py
```

솔로 실제 실행 검사는 미리보기 폴더에서 `DittochesMulti.exe --arena-smoke`로 실행합니다. 이 검사는 별도 시험 저장 키를 사용합니다. 마지막 결과는 서버 65개(서버 미변경), 이동/타이밍 894개, C#/서버 계산 비교 1,458개, 솔로 838개, 온라인 72개 통과입니다. 온라인 검사 수는 상점 추첨·폴링 등에 따라 조금 달라질 수 있습니다.

화면과 로그는 `Builds/PortablePreview/ArenaCaptures`, `OnlineCaptures`, `BuildsSmoke.log`, `OnlineEquipmentSmoke.log`에 있습니다. 정식 Unity 셰이더, Android 실기기, 실제 두 PC의 입력·연결, 장시간 밸런스는 추가 검증이 필요합니다.

## 작업 기록과 개인 세이브의 차이

이번에 저장한 것은 개발 코드·문서·에셋·미리보기와 검증 기록입니다. 플레이어의 개인 세이브/설정은 Unity PlayerPrefs, 온라인 RP는 서버의 `ratings.sqlite3`에 별도로 저장됩니다. GitHub 로그인 정보와 이 개인 데이터는 Git에 올리지 않습니다. 실행 중인 서버 방과 전투 기록은 서버 재시작 시 초기화됩니다.
