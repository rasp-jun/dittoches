# 집에서 이어서 작업하기

전체 진행 현황과 작업 우선순위는 [PROJECT_STATUS.md](PROJECT_STATUS.md)에 정리했습니다.

현재 프로젝트는 이 문서가 있는 `DittochesMulti` 폴더입니다. 상위 `01_CurrentProject`가 Git 저장소이며 작업 브랜치는 `develop`, 업로드 대상은 `dittoches` 원격(`rasp-jun/dittoches`)입니다.

## 실행

- `Play_Preview.bat`: Unity Editor 없이 현재 코드로 게임 실행. 솔로 입장 → 디지몬 버전 선택.
- `Skills_Preview.bat`: 34종 기술 타이밍과 이펙트 확인. 현재 캐릭터 표시는 기존 이미지입니다.
- `Models_Preview.bat`: 아구몬 한 종의 미완성 직접 제작 3D 시험본. 일반 게임에는 적용되지 않습니다.

미리보기는 `Builds/Windows`의 기존 Mono 플레이어를 `Builds/PortablePreview`에 복사하고 현재 C#을 컴파일한 것입니다. 일반 Unity 빌드와 셰이더가 다르고 새 Unity 에셋을 임포트하지 않습니다. 최신 팬 아트 PNG와 기술 JSON만 외부 파일로 읽습니다. 저장 데이터는 기존 실행본과 다른 키를 사용합니다.

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

현재 PC에서 서버 테스트 21개, C# 이동 간격 및 기술 타이밍 검사, 실제 플레이어의 전장/대기석 판정·구매·스킬 사용·전투 종료 검사를 수행했습니다. 화면 기록은 `Builds/PortablePreview/ArenaCaptures`, 실행 로그는 `ArenaSmoke.log`에 있습니다.

정식 Unity의 새 셰이더 임포트, Android 실기기, 두 PC의 실시간 네트워크 UI는 별도 확인이 필요합니다. 온라인 장비·초밥집·8인 매칭은 기존 미구현 범위이며 이번 UI 변경으로 추가되지 않았습니다.

GitHub 연결 앱의 쓰기 호출은 현재 `403 Resource not accessible by integration`으로 거절됩니다. 로컬 커밋은 남겨 두며 집의 인증된 Git에서 `git push dittoches develop`으로 올릴 수 있습니다. 현재 로컬 변경이 GitHub에 업로드되었다고 간주하지 마세요.
