# 다음 PC·다음 세션 작업 메모

## 2026-10-01 Git 저장 시점 인수인계 — 다음 작업은 여기서 시작

이 섹션이 아래의 과거 ‘최신’, ‘미커밋’, ‘34종 완료’ 기록보다 우선합니다. 현재 작업은 **실제 메시 갤러리 15종·90클립**, 미준비 모델 19종입니다. 원작 외형 재현도는 계속 수정 중이며, 새 갤러리 모델의 Unity 게임 적용은 아직 하지 않았습니다. 일반 플레이는 기존 2D입니다. 이번 저장 범위는 지금까지의 코드·검증 도구·실행 도구·문서이며, 실제 모델·텍스처·렌더·빌드는 Git에 포함하지 않습니다.

### 처음 열 파일과 실행

1. Git 루트 `01_CurrentProject`에서 `develop` 상태와 `git log -1`을 확인합니다. 업로드 원격은 `dittoches` = `https://github.com/rasp-jun/dittoches.git`입니다. `origin`은 이전 저장소이므로 혼동하지 않습니다.
2. [AGUMON_MODEL.md](AGUMON_MODEL.md) 최상단과 이 문서를 읽고, 아래 로컬 자산이 있는지 먼저 확인합니다. Git만 새로 받아서는 현재 모델을 열 수 없습니다.
3. `DittochesMulti/Models_Preview.bat`를 실행합니다. 걷기부터 보려면 프로젝트 폴더에서 아래 명령을 사용합니다. Python 3와 Edge 또는 기본 브라우저가 필요합니다.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Open_Faithful_Gallery.ps1 -Model agumon -Motion Walk
```

주소는 `http://127.0.0.1:8766/?motion=Walk#agumon`입니다. 아구몬을 0.25배속·정면/측면으로 보고 **수정 전 보기 / 수정 후 보기**를 비교한 다음 워그레이몬·홀리엔젤몬을 확인합니다. 이 브라우저 갤러리가 현재 작업 대상입니다. 이전 portable 절차형 뷰어의 34종/272클립과 구분합니다.

### 다음에 수정할 순서

1. **아구몬의 원작 비율과 얼굴부터 이어갑니다.** 공식 참고 이미지 `ArtSource/Roster/ReferencesV2`와 비교해 주둥이·눈·치아·손/발톱을 개별 수정합니다. 시작 코드는 `Tools/faithful_sculpt.py`, 편집 원본은 `ArtSource/AnimatedReview/agumon/agumon.blend`입니다. 현재 후보도 팬 제작 특성이 남아 있으며 사용자에게 외형 완성 승인을 받은 상태가 아닙니다.
2. `Tools/animate_faithful_models.py`에서 걷기/달리기의 발 접지와 공격 준비·명중·회복을 더 다듬습니다. 관절 위치는 `faithful_rig_profiles.py`, 가중치는 `faithful_skin_topology.py`입니다. 아구몬 이후 워그레이몬·천사형부터 종별 모습을 대조합니다. 종별 마스터즈/RPG 동작을 모두 확인하거나 원본 모션을 추출한 상태가 아닙니다.
3. 쿠가몬은 원본 1클립만 있으므로 기본 동작 확장이 남았습니다. 미준비 19종은 원작에 가까운 실제 메시·텍스처를 확보/제작한 뒤 개별 검수합니다. 사용자에게 거절된 `author_roster.py` 도형 조합 모델의 일괄 생성으로 대체하지 않습니다.
4. 외형·모션 검토 후 Unity Editor가 있는 PC에서 게임 연결, 셰이더·명중 시점·성능을 검증합니다. 현재 PC에는 사용할 Unity Editor가 없고, 최신 GLB를 게임에 연결하지 않았습니다.

### 한 종 수정 → 확인 → 갤러리 반영

수정 전 현재 `AnimatedReview`, `FaithfulGallery`, 도구와 검사 결과를 별도 새 폴더에 보존합니다. 기존 `NaturalPassBackup-20261001`을 덮어쓰지 않습니다. 아래는 아구몬 한 종의 예이며, Blender 경로는 설치 위치에 맞춥니다. 브라우저 검사는 로컬 서버가 실행 중이어야 하고 Python의 Playwright가 필요합니다(현재 보조 설치: `01_CurrentProject/tmp/gallery-test-tools`).

```powershell
& '../tmp/blender/blender-4.5.9-windows-x64/blender.exe' --background --python-exit-code 1 --python Tools/animate_faithful_models.py -- --ids agumon --reuse-bind
& '../tmp/blender/blender-4.5.9-windows-x64/blender.exe' --background --python-exit-code 1 --python Tools/verify_faithful_deformation.py -- --ids agumon
python Tools/verify_faithful_glb_motion.py
python Tools/faithful_gallery/verify_motion_browser.py --review --ids agumon --group nextAgumon
```

`--reuse-bind`는 현재 편집본이 아니라 **NaturalPassBackup의 이전 바인딩**을 읽습니다. 수동 Blender 수정본을 먼저 보존하고, 새 조형이 매번 같은 기준에서 재현되도록 코드를 수정합니다. GLB 검사는 13종 전체의 기존 삼각형·텍스처 수 보존을 가정하므로 토폴로지를 바꾸는 경우 해당 검증 기준도 의도에 맞게 재검토해야 합니다.

변형 검사·브라우저 검사와 실제 외형 확인 후 `python Tools/promote_faithful_motion.py`로 로컬 갤러리에 반영하고 `python Tools/faithful_gallery/verify_comparison.py`로 전후 비교 UI를 검사합니다. 반영 도구는 **13종 전부**의 현재 파일 해시와 통과 보고서를 요구합니다. 다른 종의 보고서는 파일이 바뀌지 않았을 때만 재사용할 수 있습니다. `prepare_faithful_*`는 초기 정적 모델 준비용이므로 현재 모션 갤러리를 덮어쓰는 용도로 실행하지 않습니다.

### Git 외에 함께 보존할 파일

현재 USB 프로젝트 `E:\Dittoches_KEEP_20260917\01_CurrentProject\DittochesMulti`의 `ArtSource` 전체를 함께 옮기는 것이 안전합니다. 최소한 `FaithfulGallery`(manifest·출처·previous 포함), `AnimatedReview`, `ThirdPartyCandidates`, `Roster/ReferencesV2`, `AnimationBackup-20261001`, `NaturalPassBackup-20261001`이 필요합니다. 기존 Unity 모델은 `Assets/Resources/Models`, 빌드는 `Builds`에 별도로 남아 있습니다. 각 모델의 `source.json`과 라이선스를 함께 보존합니다.

최신 검증 증거는 `Builds/FaithfulMotionValidation/published-report.json`(15종/90클립 및 해시), `review-naturalA`/`review-naturalB`, `Builds/FaithfulNaturalValidation/glb-report.json`(13종/78클립), `comparison-report.json`(3종)입니다. 변형·접지·루프 표본 검사와 실제 Edge 재생 검사를 통과했지만 원작 외형의 완성 판정은 아닙니다. 이 보고서·캡처는 Git에 없으므로 PC 이동 시 함께 복사합니다. 기존 `FILE_HASHES.json`/`CURRENT_HANDOFF.json`은 과거 기록이며 현재 자산 해시로 간주하지 않습니다.

## 2026-10-01 2차 자연스러움 보완 — 가장 최신

아구몬 주둥이 곡면·앞으로 굽힌 손·입 안 재질, 워그레이몬 준비 자세를 보완하고 지상형의 발 궤적·체중 이동과 4종의 발목 굽힘을 개선했습니다. 13종/78클립의 시작 1프레임 공백을 제거했습니다. 총 15종/90클립, 미완료 19종 및 Unity 게임 미적용 상태는 그대로입니다. 수정 전후 비교 버튼이 생겼고 이전 상태는 `ArtSource/NaturalPassBackup-20261001`, 검사·전후 화면은 `Builds/FaithfulNaturalValidation`에 있습니다. 생성기 `--reuse-bind`는 이 백업을 읽습니다. 최신 세부 기록은 `AGUMON_MODEL.md` 최상단을 우선합니다.

## 2026-10-01 실제 모델 15종 모션 — 가장 최신

갤러리의 정적 13종에 관절과 여섯 가지 동작 78개를 추가했습니다. 기존 버드라몬 11개·쿠가몬 1개를 합쳐 15종/90개 모션이며, 원작 외형 모델이 없는 19종은 미완료입니다. 아구몬 머리·팔 자세, 그레이몬 연결 메시·가중치, 천사형 기본 자세와 날개를 보완했습니다. `ArtSource/AnimationBackup-20261001`은 변경 전 원본, `AnimatedReview`는 Blender 편집본, `FaithfulGallery`는 실제 미리보기 자산입니다. 생성기와 검증·출처·한계는 `AGUMON_MODEL.md` 최상단을 먼저 읽으세요. 과거의 ‘13종 모션 미준비’ 기록은 이전 상태입니다.

15종/90클립의 실제 WebGL 정점 이동과 컨트롤을 검증했고, 13종의 반복 연결·표면 늘어짐·접지를 표본 검사했습니다. 결과는 `Builds/FaithfulMotionValidation/published-report.json`입니다. 실행은 `http://127.0.0.1:8766/?motion=Walk#agumon`, 또는 기존 미리보기 배치 파일입니다. Unity 게임 연결과 19종 모델 확보·제작, 원작 세부 조형·종별 동작 대조는 남았습니다. 일반 2D 게임과 기존 UnityWindows 빌드는 보존했습니다.

## 2026-10-01 원작 외형 재작업 — 최우선

사용자가 기존 34종의 도형 조합 외형을 거절했습니다. 대량 생성은 중단했고 실제 공개 GLB·텍스처 기반 15종을 별도 비교 갤러리에 준비했습니다. 19종과 대부분의 리깅·모션은 남아 있습니다. 루트 `Preview_Faithful_Digimon_3D.bat`를 열고 `AGUMON_MODEL.md` 최상단을 읽으세요. 원본은 `ThirdPartyCandidates`, 검토본은 `FaithfulGallery`, 정확한 공식 이미지 34종은 `Roster/ReferencesV2`입니다. 이전 검증 수치를 새 외형 완성으로 설명하지 마세요. 새 모델의 Unity 게임 적용은 아직 하지 않았습니다.

## 2026-10-01 전체 34종 3D 제작 — 최우선 기록

사용자가 전체 캐릭터 제작을 요청해 아구몬 얼굴 윤곽을 다듬고 추가 33종의 Blender·GLB·엔진 메쉬와 체형별 모션을 만들었습니다. 전체 34종/272클립의 제작 검토본이며 최종 원작 재현도는 미완료입니다. `Preview_All_Digimon_3D.bat`로 선택 메뉴를 열고 PgUp/PgDn으로 교체합니다. 보관소는 `02_ArtVault/Models/Roster`입니다. 아구몬의 기존 검사·원본도 유지합니다.

상세 검증·참고 URL·경로·재생성은 `AGUMON_MODEL.md` 최상단을 읽습니다. 다음 단계는 종별 얼굴·체형·갑옷·날개 세부 품질과 원작 모션 대조, 부드러운 전환·공격 명중 시점, Unity Editor 정식 빌드입니다. 일반 플레이는 2D 기본값입니다. 사용자 요청은 아구몬 한 종에 국한되지 않으며 34종 전체를 개선하는 방향입니다. 아래 과거 기록의 ‘아구몬만 제작’은 이전 상태입니다.

## 2026-10-01 후속 — 3D 모션 재개 완료 범위

사용자가 공식 아구몬 이미지·마스터즈 모션을 참고한 개선을 요청했습니다. 공개 영상의 이동/발톱/불 뿜기 프레임을 확인하고 접지·체중 이동·팔과 꼬리·공격 회복을 개선했습니다. 눈 수정과 새 모션을 Blender/GLB/FBX/엔진 자산 및 portable에 반영했습니다. 20초 검토 영상은 루트 `Review_Agumon_Motion.bat`, 최신 뷰어는 `Preview_Agumon_3D.bat`입니다. 이전 원본은 `ArtSource/Agumon/Backups/20261001-before-motion`에 있습니다.

눈 2,880면, Blender 접지·반복 1,173개, portable 재생 1,030개·18캡처, C# 이동/타이밍 894개·계산 비교 1,458개 통과. 자세한 출처·산출물·한계는 [AGUMON_MODEL.md](AGUMON_MODEL.md)의 최상단 기록을 우선합니다. 정식 UnityWindows는 이전 실행본입니다. 다음 단계는 Unity Editor에서 정식 빌드/눈·접지 확인, 피해 시점과 공격 접촉 조율, Hit/Run/Turn 이벤트 연결입니다. 일반 플레이는 2D이며 변경은 미커밋입니다.

## 2026-10-01 재개 — 눈 면 방향 수정

현재 경로는 `E:\Dittoches_KEEP_20260917\01_CurrentProject\DittochesMulti`입니다. Git `develop` HEAD `6fa2f36`은 GitHub 원격과 일치하며 기존 변경을 보존했습니다. 눈 고리 2,304개 면이 안쪽을 향하는 것을 확인하고 생성 코드의 정점 순서를 수정했습니다. Blender 회귀 검사에서 12개 레이어·2,880개 면의 바깥 방향과 정점 위치 보존이 통과했습니다. 상세 내용과 명령은 [AGUMON_MODEL.md](AGUMON_MODEL.md)의 10월 1일 기록을 따릅니다.

자산·실행본은 아직 갱신하지 않았습니다. 다음 단계는 기존 원본 보존 → 수정된 생성기로 자산 재생성 → 구조 검사 → Unity 정식 빌드와 눈 표시 캡처 확인입니다. 이 PC에는 이전 Unity Editor 경로가 없습니다. 정식 눈 표시 해결은 아직 검증하지 않았고, 커밋·푸시는 하지 않았습니다.

## 2026-09-29 최신 작업 — 먼저 확인

현재 작업 경로는 `G:\Dittoches_KEEP_20260917\01_CurrentProject\DittochesMulti`입니다. 기존 아구몬 자산을 유지하면서 모션 뷰어의 배속·타임라인·1/30초 이동·반복/마지막 자세 유지와 일시정지 중 회전을 보완했습니다. 실제 전투 경로의 연결 동작도 조회하며 타임라인 이동 시 상태를 다시 계산합니다. 일반 Unity 빌드에서도 `--model-gallery`와 `--model-smoke`를 사용할 수 있습니다. 자세한 사용법·남은 작업은 [AGUMON_MODEL.md](AGUMON_MODEL.md)에 있습니다.

자산 구조 166,365개, 기존 Blender 보고서의 60개 자세 범위와 GLB 8개 클립, C# 이동/타이밍 894개 및 계산 비교 1,458개가 통과했습니다. 실제 portable 플레이어 관절 재생 검사 954개와 8개 클립·시점 2개·연결 동작 8개, 총 18장 전장 캡처도 완료했습니다. 로그는 `Builds/PortablePreview/ModelSmoke-20260929.log`입니다. 정식 Unity 임포트·두 셰이더·Windows Mono 빌드와 정식 플레이어 954개 검사·18장 전장 캡처도 통과하고 정상 종료했습니다. 출력은 `Builds/UnityWindows`, 로그는 `Builds/UnityBuild-20260929.log`와 `Builds/UnityWindows/ModelSmoke-20260929.log`입니다.

정식 모델은 외곽선 깊이 기록과 Gamma 색 변환을 보정했지만 최종 렌더에서 눈 표면이 보이지 않아 시각 검증이 미완료입니다. 원인은 미확정입니다. 루트 `Preview_Agumon_3D.bat`는 눈이 정상인 portable 뷰어를 기본 실행합니다. 정식 모델은 `Builds/UnityWindows/DittochesMulti.exe --model-gallery`로 별도 확인하며 `Play_Unity_Windows.bat`는 정식 일반 게임 실행입니다. 이번 2D UI 재검사는 숨김 실행의 OnGUI 갱신이 없어 완료하지 못했습니다(`ArenaSmoke-20260929.log`). 과거 838개 통과와 구분합니다. 실제 마우스·키보드 조작과 전체 조작 화면의 육안 검증도 남았습니다.

사용자가 현재까지 마무리하고 중단하도록 요청해 최종 빌드·검사·기록을 마친 뒤 멈췄습니다. 실행 중인 작업은 없습니다. 재개 요청을 받으면 정식 셰이더에서 눈이 보이지 않는 문제를 먼저 확인하고, 이후 걷기 주기/이동 속도/발 미끄러짐과 0.8초 Attack 클립이 기존 0.35초 기본 공격 타이머에 압축되는 문제를 조율합니다. Hit/Run/Turn은 아직 뷰어용이며 실제 게임 이벤트 연결은 남았습니다. 일반 플레이는 2D 기본값이고 이번에 모델 아트를 새로 만들거나 외부 유료 생성을 사용하지 않았습니다.

이번 변경은 로컬 작업이며 커밋·푸시하지 않았습니다. `CURRENT_HANDOFF.json`은 9월 18일 기록으로 이번 변경의 해시가 아닙니다. 아래 9월 19일/18일 기록은 이전 맥락이며 완료 여부는 이 최신 섹션과 실제 Git·검증 로그를 우선합니다.

## 2026-09-19 최신 작업

사용자가 3D 모델·모션 제작을 재개했습니다. Fal의 제작 방식은 설명만 했고 사용자는 **Blender 직접 제작, 외부 생성 비용 없음**을 선택했습니다. 마스터즈/RPG의 모습을 참고하고 어색한 얼굴을 개선하라는 피드백을 받아 아구몬 한 종을 반복 수정했습니다. 연결된 몸체와 스컬프 머리, 밀착된 눈, 별도 아래턱, 19개 관절과 8개 모션, Blender/GLB/FBX/게임용 데이터가 있습니다. 상세 상태와 실행은 [AGUMON_MODEL.md](AGUMON_MODEL.md)를 먼저 읽습니다.

보존 루트 `Preview_Agumon_3D.bat`와 `Open_Agumon_Blender.bat`를 사용합니다. 일반 플레이는 2D 기본값이며 전체 유닛 3D가 완성된 것은 아닙니다. 새 자산은 `ArtSource/Agumon`, 엔진 파일은 `Assets/Resources/Models/Agumon`, 별도 보관은 `02_ArtVault/Models/Agumon`입니다. 아래 9월 18일 기록은 이전 UI 작업 맥락으로 유지합니다. Git HEAD와 `CURRENT_HANDOFF.json`은 9월 18일 기준이며 이번 작업은 로컬 변경입니다.

저장일: 2026-09-18. 사용자가 진행 중인 UI 패치를 마무리하고 다른 작업으로 넘어가기를 요청했습니다. 현재 범위의 검증·저장까지 마쳤으며 새 기능이나 3D 작업을 자동으로 시작하지 않습니다. 이 문서는 이전 대화 없이 작업 맥락을 복구하기 위한 기록입니다.

## 시작할 위치

- 현재 프로젝트: 이 파일이 있는 `DittochesMulti`. Git 루트는 한 단계 위입니다.
- 저장소: [rasp-jun/dittoches](https://github.com/rasp-jun/dittoches/tree/develop), 브랜치 `develop`.
- 최신 기능: 9월 18일 배치·전투 UI 마무리. 이전 모집·배치 패치는 `6a72aa5`, 인수인계는 `dc080cb`입니다. 정확한 최신 HEAD는 `git log -1` 또는 보존 폴더의 `CURRENT_HANDOFF.json`으로 확인합니다.
- 로컬 Git과 GitHub CLI 인증으로 업로드 성공, `develop` 원격 SHA 일치를 확인했습니다. 연결 앱의 코드 쓰기 403은 이 경로의 업로드에 영향을 주지 않습니다.
- `main`은 이전 상태입니다. 새 PC의 복원·실행·로그인은 [HOME_HANDOFF.md](HOME_HANDOFF.md)를 따릅니다.

## 사용자가 정한 방향

디지몬 버전을 먼저 완성하고, 오리지널 버전은 이후 캐릭터 교체 단계에서 진행합니다. 롤토체스처럼 자연스러운 배치·전투·UI가 목표입니다. 전체 3D 제작은 집 PC로 보류했고, 추가 유료 생성 서비스를 쓰지 않기로 했습니다. 기존 아구몬 시험본만 별도 보관 중입니다.

스킬은 디지몬의 기술을 소재로 하되 공격력/주문력 계수와 게임 내 피해 유형을 구분합니다. 동일 성급도 장비와 시너지에 따라 피해가 달라져야 합니다. 유닛별 기본 공격 사거리도 다릅니다. 스킬 정보는 아이콘 우클릭으로 조회합니다. 장비에 오너 표시는 필요 없으며, 캡슐은 나중의 증강 시스템용입니다.

## 구현 완료한 최신 상태

1. 플레이어 30종 + 크립 4종의 스킬, 성급별 AD/AP 계수, 물리/마법 피해와 저항, 1~5칸 기본 사거리를 공유 JSON과 계산 코드에 연결했습니다.
2. 문장 6종(2/3/5종)과 전투 특성 5종(2/4/6종), 재료 4종·완성 장비 10종·추출기를 솔로와 온라인에 적용했습니다. 시너지는 전장에 있는 서로 다른 종류만 집계합니다.
3. 육각 거리 기반 범위 표시·대상 선택, 장비 장착/합성/회수 전후 능력치 미리보기를 구현했습니다.
4. 기본 공격/스킬 피해, 실제 받은 피해/보호막 흡수, 실제 회복/보호막 생성 기록과 현재 HP/마나/상태를 표시합니다. 온라인 완료 기록은 같은 방 재접속에도 남고, 솔로 과거 기록은 다음 준비 단계의 장비·성급 변경과 분리합니다.
5. 상점 카드에 공격/마법/혼합 유형·사거리·스킬 아이콘을 추가했습니다. 우클릭 상세는 장비·시너지 없는 1성 기준이며 골드가 없어도 조회만 가능합니다.
6. 배치할 칸을 가리키면 이동/교환 전후 시너지 인원과 활성·강화·약화·해제를 미리 봅니다. 교환으로 빠지는 유닛, 중복 종류와 인원 제한도 반영합니다. 실제 입력 전에는 배치를 바꾸지 않습니다.
7. 솔로 연속 드래그와 별도 목적지 강조, 기존 이미지의 모션 전환 보간, 솔로·온라인 전투 체력바 겹침 회피를 적용했습니다. 온라인 모집 카드·경제 UI를 정리하고 F/D/Space 단축키, 공통 버튼 호버와 지연 툴팁을 추가했습니다.

## 주요 파일 안내

| 작업 | 파일 |
|---|---|
| 스킬·계수·사거리 정의 | `Assets/Resources/DigimonSkills.json`, `Assets/Scripts/DigimonSkillCatalog.cs`, `DigimonCombatMath.cs`, `COMBAT_STATS_AND_SKILLS.md` |
| 시너지·장비 정의 | `Assets/Resources/DigimonBuilds.json`, `Assets/Scripts/DigimonBuildCatalog.cs`, `SYNERGIES_AND_EQUIPMENT.md` |
| 모집 카드·배치 미리보기 | `Assets/Scripts/NativeGame.Recruitment.cs`, `MultiLauncher.Recruitment.cs`, `FormationForecast.cs`, `FormationForecastUI.cs` |
| 전투 정보 | `Assets/Scripts/CombatReportUI.cs`, `NativeGame.CombatReport.cs`, `MultiLauncher.CombatReport.cs`, `*.SkillInspection.cs` |
| UI 마무리 | `Assets/Scripts/ArenaInterface.cs`, `CombatLabelLayout.cs`, `NativeGame.InterfacePolish.cs`, `MultiLauncher.InterfacePolish.cs`, `NativeGame.Presentation.cs`, `TacticalArena.Skills.cs` |
| 전장·모션·3D 시험본 | `Assets/Scripts/TacticalArena.cs`, `DigimonModelLibrary.cs`, `DigimonMeshBuilder.cs`, `DigimonRig.cs` |
| 서버 | `Server/server.py`, `combat_skills.py`, `combat_builds.py`, `combat_stats.py` |
| 검증 | `Tools/validate_code.py`, `validate_online.py`, `Assets/Scripts/NativeGame.*Validation.cs`, `MultiLauncher.RuntimeValidation.cs`, `Server/test*.py` |

## 마지막으로 통과한 검증

| 검증 | 결과 |
|---|---|
| 서버 단위/HTTP 검사 | 65개 통과 (직전 전투 기록 패치, 이후 서버 코드 변경 없음) |
| C# 이동·기술 타이밍 검사 | 894개 통과 |
| C# ↔ 서버 능력치·피해·거리 비교 | 1,458개 통과 |
| 실제 솔로 Mono 플레이어 | 838개 통과 |
| 실제 온라인 플레이어 + HTTP 시험 상대 | 72개 통과 |

온라인 검사 수는 상점 추첨과 폴링 등에 따라 조금 달라질 수 있습니다. 두 PC의 실제 UI 조작을 검사한 것은 아닙니다. 최근 화면은 `Builds/PortablePreview/ArenaCaptures/04-combat.png`, `22-drag-follow.png`, `23-drop-settle.png`, `OnlineCaptures/01-inventory.png`, `12-shop-skill.png`입니다. 밀집/경계 체력바 배치, 같은 칸 안의 연속 드래그와 실제 교환, 온라인 정보창/골드 부족 시 단축키 차단도 검사합니다.

## 다음에 할 일

1. 새 PC에서 전체 USB 보존 폴더를 복사하거나 연결하고 `Play_Preview.bat` 실행을 확인합니다. 개발 재개 전에 실제 Git 상태와 `develop`을 확인합니다.
2. Unity `6000.6.0f1`로 정식 임포트와 Windows 빌드를 확인합니다. 현재 미리보기의 대체 셰이더는 정식 빌드 검증을 대신하지 않습니다.
3. 사용자가 집 PC에서 3D 작업 재개를 원하면 아구몬 한 종의 외형·리깅·걷기·공격·스킬·사망을 먼저 완성합니다. 다른 PC라는 이유만으로 자동으로 3D 제작을 시작하지는 않습니다.
4. UI 작업을 계속 요청하면 다양한 해상도에서 배치/드래그/정보창·꽉 찬 대기석·자동 합성을 점검하고 근접 난전 가독성을 다듬습니다. 두 PC에서 구매→배치→전투→결과도 확인합니다.

미완료: 전체 3D 캐릭터/자연스러운 전환 모션, 모든 원작 장면·한국 더빙 기술명 대조, 캡슐 증강, 8인 매칭, 온라인 초밥집과 솔로 전체 콘텐츠 이식, 장시간 밸런스, 정식 셰이더·Android 실기기 검증. 전체 진행표는 [PROJECT_STATUS.md](PROJECT_STATUS.md)에 있습니다.

## 주의할 복원 차이

GitHub는 새 코드·문서·설정 중심입니다. 현재 프로젝트 이미지 53개, 플레이어 빌드와 컴파일러는 USB에 별도로 있습니다. USB 보존 폴더 루트의 `CURRENT_HANDOFF.json`에 이번 저장의 정확한 HEAD와 파일별 SHA-256을 기록하며, `FILE_HASHES.json`은 최초 보존 작업 시점의 기록으로 유지합니다. 루트의 `Play_Current_Preview.bat`는 현재 프로젝트 실행 바로가기입니다. 게임의 개인 세이브·GitHub 로그인·실행 중인 서버 방은 코드 저장과 별개이며 자동으로 다른 PC에 이전되지 않습니다.

새 세션에 전달할 문장:

> DittochesMulti/NEXT_SESSION.md와 HOME_HANDOFF.md, PROJECT_STATUS.md를 읽고 develop의 최신 상태부터 이어서 작업해. 디지몬 버전이 우선이고, 오리지널은 보존해. 먼저 실행 상태를 확인하고 남은 UI·전투 작업을 진행해.
