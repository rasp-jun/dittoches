# 다른 컴퓨터에서 이어서 작업하기

## 2026-10-01 Git 저장 시점 — 먼저 확인

재개 절차·첫 수정 파일·검증 명령은 [NEXT_SESSION.md](NEXT_SESSION.md)의 최상단 **Git 저장 시점 인수인계**를 따릅니다. 브랜치는 `develop`, 원격은 `dittoches`(`rasp-jun/dittoches`)이며 정확한 저장 커밋은 `git log -1`로 확인합니다. 아래 미커밋/과거 HEAD 안내는 당시 기록입니다.

Git에는 코드·문서·실행 도구만 보존합니다. 새 PC에는 USB의 `ArtSource` 전체(특히 `FaithfulGallery`, `AnimatedReview`, `ThirdPartyCandidates`, `Roster/ReferencesV2`, `AnimationBackup-20261001`, `NaturalPassBackup-20261001`)와 필요한 `Assets/Resources/Models`, `Builds`를 함께 복사하세요. 현재 원본·비교 모델·출처·검사 결과는 Git 복제만으로 복원되지 않습니다. 프로젝트 안의 `Models_Preview.bat`로 현재 브라우저 미리보기를 열 수 있습니다. 실제 메시 15종/90클립이며, 19종과 Unity 게임 적용은 아직 남아 있습니다.

## 2026-10-01 2차 수정 — 가장 최신

아구몬 얼굴 곡면·손·입 안 재질, 워그레이몬 준비 자세와 보행을 보완하고 13종의 반복 시작 끊김을 없앴습니다. 최신 갤러리에 수정 전후 비교 버튼이 있습니다. PC 이동 시 `ArtSource/NaturalPassBackup-20261001`과 `FaithfulGallery/previous`도 함께 보존하세요. 13종/78클립 재검사와 3종의 같은 시점 전후 비교를 실제 Edge에서 확인했습니다. 보고서·전후 화면은 `Builds/FaithfulNaturalValidation`, 상세 작업은 `AGUMON_MODEL.md` 최상단입니다. 총 15종/90클립이며 19종과 Unity 적용은 남아 있습니다.

## 2026-10-01 실제 모델 애니메이션 — 가장 최신

새 갤러리는 15종 모두 움직이며 총 90클립입니다. 13종에 새로 만든 78개와 기존 버드라몬 11개·쿠가몬 1개를 합한 수치입니다. 19종 모델과 Unity 게임 적용은 남았습니다. 머리 비율·팔 자세·날개·연결 메시를 보완했고 원작 완성도 검토는 계속 필요합니다. 원본 백업은 `ArtSource/AnimationBackup-20261001`, 편집 가능한 13종 Blender 파일은 `ArtSource/AnimatedReview`, 실제 배포 파일은 `ArtSource/FaithfulGallery`에 있으므로 PC 이동 시 함께 복사하세요. Python 3과 Edge/Chrome으로 `Preview_Faithful_Digimon_3D.bat`를 실행합니다. 모션 선택·느린 재생·타임라인·프레임 이동을 지원합니다. 검증·재생성·출처는 `AGUMON_MODEL.md` 최상단과 `Builds/FaithfulMotionValidation/published-report.json`을 우선합니다.

## 2026-10-01 원작 외형 비교 — 최신

루트 `Preview_Faithful_Digimon_3D.bat`는 Python 3로 로컬 서버를 숨김 실행하고 실제 브라우저 창을 엽니다. 주소는 `http://127.0.0.1:8766/`입니다. 실제 공개 외부 모델 15종과 원작 이미지 34종을 비교하며, 미완료 19종을 구분합니다. 이전 도형 조합 모델의 외형은 사용자에게 거절됐습니다. 일반 게임 2D·정식 UnityWindows는 보존합니다. 최신 출처·원본·검증·미완료 작업은 `AGUMON_MODEL.md` 최상단을 우선합니다.

## 2026-10-01 전체 디지몬 뷰어 — 최신

루트 `Preview_All_Digimon_3D.bat`를 실행하면 34종의 제작 검토 모델을 고를 수 있습니다. 우측 상단 선택 버튼 또는 PgUp/PgDn을 사용합니다. 추가 33종의 원본은 `ArtSource/Roster`, 게임 자산은 `Assets/Resources/Models/Roster`, 보관소는 `02_ArtVault/Models/Roster`입니다. `--roster-smoke`로 전체 실행 검사를 진행합니다. 최신 검증·출처·미완료 사항은 `AGUMON_MODEL.md` 최상단을 우선합니다. 제작 검토본이며 최종 외형/모션 완성이 아닙니다. 정식 UnityWindows는 이전 실행본이고 일반 플레이는 2D입니다.

## 2026-10-01 후속 최신 실행 안내

아구몬 눈 수정과 마스터즈 공개 영상 기반 모션 개선을 자산과 portable에 반영했습니다. 루트 `Review_Agumon_Motion.bat`는 새 20초 검토 영상, `Preview_Agumon_3D.bat`는 최신 모션 뷰어입니다. 원본 백업은 `ArtSource/Agumon/Backups/20261001-before-motion`입니다. 눈 2,880면·Blender 접지 1,173개·portable 재생 1,030개/18캡처·C# 894개/계산 비교 1,458개 통과. 최신 출처·검증·남은 작업은 [AGUMON_MODEL.md](AGUMON_MODEL.md)의 최상단을 읽습니다. 정식 UnityWindows 실행본은 이전 상태이고 새 정식 빌드는 아직 하지 못했습니다. 일반 플레이는 2D입니다.

## 2026-10-01 최신 안내

현재 경로는 `E:\Dittoches_KEEP_20260917\01_CurrentProject\DittochesMulti`입니다. Git `develop` HEAD `6fa2f36`과 GitHub 원격 일치를 확인했습니다. 눈 메쉬 고리의 뒤집힌 면 방향을 생성 코드에서 수정했고 Blender의 12개 레이어·2,880개 면 검사와 정점 위치 보존 검사가 통과했습니다. 기존 모델 자산과 실행본에는 아직 반영하지 않았습니다. 이전 Unity Editor 경로가 이 PC에 없으므로 자산 재생성과 정식 빌드·시각 검증이 남아 있습니다. 상세 절차는 [AGUMON_MODEL.md](AGUMON_MODEL.md)의 10월 1일 기록을 우선합니다. 변경은 미커밋 상태입니다.

## 2026-09-29 최신 실행·검증 안내

현재 프로젝트는 `G:\Dittoches_KEEP_20260917\01_CurrentProject\DittochesMulti`입니다. 기존 아구몬 모델에 모션 뷰어 배속(0.25/0.5/1배), 타임라인, 1/30초 이동, 정지 중 회전과 연결 동작 검토를 추가했습니다. `Preview_Agumon_3D.bat` 또는 프로젝트의 `Models_Preview.bat`는 눈이 정상인 portable 뷰어를 기본 실행합니다. 정식 모델은 아래 `--model-gallery` 명령으로 별도 확인합니다. 루트 `Play_Unity_Windows.bat`는 정식 일반 게임 실행입니다. 모델 외형·원본 아트는 이번에 새로 만들지 않았고 일반 플레이는 계속 2D입니다.

자산 구조 166,365개·기존 Blender 보고서 자세 범위 60개·GLB 8개 클립, C# 894개·계산 비교 1,458개, 실제 portable 관절 재생 954개 검사가 통과했습니다. `Builds/PortablePreview/ModelSmoke-20260929.log`와 `ModelCaptures`에서 이번 재생 검사와 8개 클립·2개 시점·8개 연결 동작 전장 화면 18장을 확인합니다. 타임라인 기능은 실행 검사로 확인했지만 실제 마우스·키보드 조작과 전체 조작 화면의 육안 검증은 남았습니다.

정식 Unity 임포트·관절 재생 954개·두 셰이더·Windows Mono 빌드와 정식 플레이어의 954개 검사·18장 전장 캡처는 **통과**했습니다. 로그는 `Builds/UnityBuild-20260929.log`와 `Builds/UnityWindows/ModelSmoke-20260929.log`입니다. 외곽선 깊이 기록과 Gamma 색 변환은 보정했지만 최종 정식 렌더에서 눈 표면이 보이지 않아 시각 검증은 미완료입니다. 원인은 미확정이며 다음 재개 시 최우선 확인 대상입니다. 이번 2D UI 재검사도 숨김 실행에서 OnGUI 갱신이 없어 완료하지 못했습니다(`Builds/PortablePreview/ArenaSmoke-20260929.log`).

사용자의 요청으로 현재까지 빌드·검사·기록을 마치고 중단했습니다. 남은 실행 프로세스는 없습니다. 아래는 재개 시 사용할 절차입니다. 이 PC의 Editor는 `E:\6000.6.0f1\Editor\Unity.exe`이며 다른 PC에서는 실제 설치 경로로 바꿉니다. 프로젝트 폴더에서 검사와 별도 Windows Mono 빌드를 실행합니다.

```powershell
& 'E:/6000.6.0f1/Editor/Unity.exe' -batchmode -quit -projectPath (Get-Location).Path -buildTarget Win64 -executeMethod AuthoredModelBuild.Build -logFile 'Builds/UnityWindowsBuild.log'
```

출력은 `Builds/UnityWindows/DittochesMulti.exe`입니다. `AuthoredModelBuild.Build`는 아구몬 TextAsset·관절 재생과 `ArenaSurface`/`CharacterToon`의 실제 컴파일 오류를 확인합니다. 셰이더 검사에 그래픽 장치가 필요하므로 `-nographics`를 붙이지 않습니다. 기존 `BuildDesktop.Build`는 `Builds/Windows`를 덮어쓰므로 보존 미리보기 원본을 유지할 때는 새 명령을 사용합니다. 새 빌드 작업은 제품 설정을 바꾸지 않으며 변경이 필요했던 스크립팅 백엔드는 종료 전에 복원합니다.

빌드 성공 후 정식 셰이더 모델 뷰어와 자동 검사는 다음과 같습니다.

```powershell
& './Builds/UnityWindows/DittochesMulti.exe' --model-gallery -screen-fullscreen 0 -screen-width 1280 -screen-height 900
& './Builds/UnityWindows/DittochesMulti.exe' --model-smoke -screen-fullscreen 0 -screen-width 1280 -screen-height 900 -logFile 'Builds/UnityWindows/ModelSmoke.log'
```

Space는 재생/정지, 방향키는 1/30초 이동, Home은 처음으로 이동입니다. 걷기 주기/이동 속도·발 미끄러짐과 공격 명중 시점은 후속 조율 대상입니다. 기존 게임의 0.35초 기본 공격 타이머에 0.8초 Attack 클립을 압축하며 Hit/Run/Turn은 아직 게임 이벤트에 연결하지 않았습니다. 자세한 한계와 원본 위치는 [AGUMON_MODEL.md](AGUMON_MODEL.md)를 따릅니다.

9월 29일 변경은 로컬 상태이며 커밋·푸시하지 않았습니다. 과거 `CURRENT_HANDOFF.json`을 이번 결과의 파일 해시로 사용하지 않습니다. 아래 9월 19일/18일 안내는 이전 보존 기록이며 최신 상태는 이 섹션을 우선합니다.

**2026-09-19 추가:** 무료 Blender로 재제작한 아구몬과 8개 모션이 있습니다. 보존 루트 `Preview_Agumon_3D.bat`로 실제 모션 뷰어, `Open_Agumon_Blender.bat`로 편집 원본을 엽니다. 파일 위치·검증·남은 작업은 [AGUMON_MODEL.md](AGUMON_MODEL.md)를 먼저 확인하세요. `ArtSource/Agumon`, `Assets/Resources/Models/Agumon`, `../tmp/blender`도 함께 보존합니다. 아래 9월 18일 기록은 이전 실행본·복원 절차입니다.

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
