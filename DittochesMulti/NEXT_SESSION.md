# 다음 PC·다음 세션 작업 메모

저장일: 2026-09-17. 사용자는 현재까지의 진행 상황을 저장하고 다른 컴퓨터에서 바로 이어서 작업하기를 요청했습니다. 이 문서는 이전 대화 없이 작업 맥락을 복구하기 위한 기록입니다.

## 시작할 위치

- 현재 프로젝트: 이 파일이 있는 `DittochesMulti`. Git 루트는 한 단계 위입니다.
- 저장소: [rasp-jun/dittoches](https://github.com/rasp-jun/dittoches/tree/develop), 브랜치 `develop`.
- 최신 기능 커밋: `6a72aa5`. GitHub 인증·업로드 안내 갱신: `eb5048a`. 이 인수인계 문서는 그 뒤의 문서 커밋입니다. 정확한 최신 HEAD는 `git log -1`로 확인합니다.
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

## 주요 파일 안내

| 작업 | 파일 |
|---|---|
| 스킬·계수·사거리 정의 | `Assets/Resources/DigimonSkills.json`, `Assets/Scripts/DigimonSkillCatalog.cs`, `DigimonCombatMath.cs`, `COMBAT_STATS_AND_SKILLS.md` |
| 시너지·장비 정의 | `Assets/Resources/DigimonBuilds.json`, `Assets/Scripts/DigimonBuildCatalog.cs`, `SYNERGIES_AND_EQUIPMENT.md` |
| 모집 카드·배치 미리보기 | `Assets/Scripts/NativeGame.Recruitment.cs`, `MultiLauncher.Recruitment.cs`, `FormationForecast.cs`, `FormationForecastUI.cs` |
| 전투 정보 | `Assets/Scripts/CombatReportUI.cs`, `NativeGame.CombatReport.cs`, `MultiLauncher.CombatReport.cs`, `*.SkillInspection.cs` |
| 전장·모션·3D 시험본 | `Assets/Scripts/TacticalArena.cs`, `DigimonModelLibrary.cs`, `DigimonMeshBuilder.cs`, `DigimonRig.cs` |
| 서버 | `Server/server.py`, `combat_skills.py`, `combat_builds.py`, `combat_stats.py` |
| 검증 | `Tools/validate_code.py`, `validate_online.py`, `Assets/Scripts/NativeGame.*Validation.cs`, `MultiLauncher.RuntimeValidation.cs`, `Server/test*.py` |

## 마지막으로 통과한 검증

| 검증 | 결과 |
|---|---|
| 서버 단위/HTTP 검사 | 65개 통과 (직전 전투 기록 패치, 이후 서버 코드 변경 없음) |
| C# 이동·기술 타이밍 검사 | 894개 통과 |
| C# ↔ 서버 능력치·피해·거리 비교 | 1,458개 통과 |
| 실제 솔로 Mono 플레이어 | 822개 통과 |
| 실제 온라인 플레이어 + HTTP 시험 상대 | 68개 통과 |

온라인 검사 수는 상점 추첨과 폴링 등에 따라 조금 달라질 수 있습니다. 두 PC의 실제 UI 조작을 검사한 것은 아닙니다. 최근 화면은 `Builds/PortablePreview/ArenaCaptures/19-synergy-swap.png`, `20-shop-skill.png`, `21-placement-blocked.png`, `OnlineCaptures/12-shop-skill.png`, `13-formation-preview.png`입니다. 마지막 솔로·온라인 로그에 런타임 예외가 없었습니다.

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
