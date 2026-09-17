# 집에서 이어서 작업하기

전체 진행 현황과 작업 우선순위는 [PROJECT_STATUS.md](PROJECT_STATUS.md)에 정리했습니다.

최신 패치는 [COMBAT_STATS_AND_SKILLS.md](COMBAT_STATS_AND_SKILLS.md)의 전투 기록과 현재 체력·마나·보호막 표시입니다. 피해는 평타/스킬, 받은 피해는 체력/보호막 흡수로 나눕니다. 온라인 오른쪽 기록 전환 버튼으로 보고, 완료 기록은 같은 서버의 같은 방에 재접속하면 유지됩니다. 서버 재시작 후에는 방과 기록이 초기화됩니다. 솔로 지난 전투 정보는 장비·성급을 다음 라운드에 바꿔도 유지합니다. 최신 검증은 서버 65개, 계산 비교 1,458개, 솔로 실행 645개, 온라인 실행 57개입니다. 아래 44/440개 기록은 이전 장비 패치 때의 검증입니다.

이전 패치의 육각 사거리 표시, 사거리 내 적 우선 공격, 장착 전후 능력치·스킬 피해 미리보기와 스킬 아이콘 우클릭도 적용되어 있습니다. 온라인 서버와 `Play_Preview.bat`를 함께 최신 버전으로 실행하세요.

장비 명칭은 기본 재료 크롬/레드/블루디지조이드·디지코어와 고유 무장 10종입니다. 베렌헤나는 완성품, 회수 소모품은 데이터 추출기입니다. `DigimonBuilds.json`의 이름·용도·소재 설명을 솔로/온라인에서 공유하며 아이템 ID와 합성식은 유지했습니다.

시너지·장비 개편은 [SYNERGIES_AND_EQUIPMENT.md](SYNERGIES_AND_EQUIPMENT.md)를 참고하세요. 문장 6종/전투 특성 5종 및 기본 4종/완성 10종 장비를 적용했고, 온라인 장비 획득·합성·장착·회수까지 이어서 구현했습니다. 서버도 최신 코드로 재시작하세요. 캡슐 증강은 다음 단계입니다. 서버 테스트는 44개이며 솔로 검사 기록은 `BuildsSmoke.log` (440개), 온라인은 `OnlineEquipmentSmoke.log`에 있습니다.

현재 프로젝트는 이 문서가 있는 `DittochesMulti` 폴더입니다. 상위 `01_CurrentProject`가 Git 저장소이며 작업 브랜치는 `develop`, 업로드 대상은 `dittoches` 원격(`rasp-jun/dittoches`)입니다.

## 실행

- `Play_Preview.bat`: Unity Editor 없이 현재 코드로 게임 실행. 솔로 입장 → 디지몬 버전 선택.
- `Skills_Preview.bat`: 34종 기술 타이밍과 이펙트 확인. 현재 캐릭터 표시는 기존 이미지입니다.
- `Models_Preview.bat`: 아구몬 한 종의 미완성 직접 제작 3D 시험본. 일반 게임에는 적용되지 않습니다.

미리보기는 `Builds/Windows`의 기존 Mono 플레이어를 `Builds/PortablePreview`에 복사하고 현재 C#을 컴파일한 것입니다. 일반 Unity 빌드와 셰이더가 다르고 새 Unity 에셋을 임포트하지 않습니다. 최신 팬 아트 PNG와 기술·시너지·장비 JSON을 외부 파일로 읽습니다. 저장 데이터는 기존 실행본과 다른 키를 사용합니다.

집에서는 Unity Hub에 이 폴더를 추가하고 `ProjectSettings/ProjectVersion.txt`의 버전으로 열어 정식 빌드를 확인합니다. GitHub에는 이미지·3D 바이너리를 올리지 않으므로 USB의 프로젝트 에셋과 `02_ArtVault`도 보관해야 합니다.

## 이번 코드 변경

- 추가 UI 패치: 상점 옆 레벨/XP, 색상별 등급 확률, 이자 5칸, 오른쪽 잠금 버튼, 합성 카드 금색 테두리, 평면 버튼, 중앙 라운드 진행 표시. 전투 시작/결과 안내를 전장 위쪽으로 옮겨 유닛 가림을 줄였습니다. 3D 제작 상태는 바뀌지 않았습니다.

- 디지몬 솔로 전장 폭을 넓히고 왼쪽 시너지/장비, 오른쪽 테이머/상세/전투 기록, 아래 5개 모집 카드를 재배치했습니다.
- 카드 전체 구매, 비용/대기석 제한 표시, 합성 안내, 드래그 판매, 56칸 및 대기석 9칸의 카메라 기준 판정을 연결했습니다.
- 테라스·수로·이끼·낮은 등불로 전장을 재구성했습니다. 오리지널 버전은 이전 UI와 전장을 사용합니다.
- `Assets/Resources/DigimonSkills.json`의 30종 유닛+4종 크립 정의를 솔로/온라인 서버/표시에 공유합니다. 준비·발사·명중·회복, 다중 발사, 범위, 기절을 분리했습니다.
- 온라인 재생은 서버 프레임 시간과 스킬 이벤트를 사용합니다. 이전처럼 모든 전투를 8초로 압축하지 않습니다.
- 결과가 결정된 뒤에는 추가 피해 없이 이펙트를 마무리합니다.

## 3D 작업은 보류

사용자 요청에 따라 집 PC에서 계속합니다. `DigimonModelLibrary.PreviewEnabled`는 기본값 false이며 모델 미리보기에서만 true입니다. 아구몬 시험본은 `DigimonMeshBuilder.cs`, `DigimonModelLibrary.cs`, `DigimonRig.cs`에 보관했습니다. 다른 유닛의 3D 모델은 아직 제작하지 않았습니다.

현재 시험본은 코드로 만든 메쉬/본/스킨 가중치와 절차적 관절 동작입니다. 완성된 롤토체스 수준 모델이나 전체 애니메이션 세트가 아닙니다. 집에서 형상·관절·걷기·공격·스킬·사망 동작을 개선한 다음 일반 플레이 연결 여부를 정해야 합니다.

기술 이름/설명은 일본어 원명 기반이며 한국 더빙 명칭 및 애니메이션 장면별 포즈를 모두 대조한 상태는 아닙니다. 자료 링크는 JSON의 `source`에 있습니다.

## 확인 방법과 남은 검증

```powershell
python Tools/validate_code.py
python -m unittest discover -s Server -v
python Tools/build_portable_preview.py
```

컴파일러 기본 경로는 상위 `tmp/roslyn/tasks/net472/csc.exe`입니다. 없으면 `--compiler`로 Roslyn 경로를 전달합니다. Unity가 설치되어 있으면 Editor의 정식 빌드 검사를 우선합니다.

현재 PC에서 서버 테스트 44개, C# 이동/기술 검사 894개, 솔로 실행 검사 440개를 수행했습니다. `Tools/validate_online.py`로 실제 플레이어의 HTTP 접속·장비 조작·재접속·전투·보급도 검사했습니다. 솔로 화면은 `ArenaCaptures`, 온라인 화면은 `OnlineCaptures`에 있습니다.

정식 Unity의 새 셰이더 임포트, Android 실기기, 두 PC의 실시간 네트워크 UI는 별도 확인이 필요합니다. 온라인 초밥집·8인 매칭은 아직 미구현입니다. 온라인 장비는 이번 업데이트에서 연결했습니다.

GitHub 연결 앱 쓰기는 `403 Resource not accessible by integration`, 로컬 HTTP 푸시는 `HTTPUnauthorized: No valid credentials provided`로 실패했습니다. 로컬 커밋은 남겨 두며 집의 인증된 Git에서 `git push dittoches develop`으로 올릴 수 있습니다. 현재 로컬 변경이 GitHub에 업로드되었다고 간주하지 마세요.
