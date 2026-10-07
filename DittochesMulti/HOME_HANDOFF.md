# 다른 컴퓨터에서 이어서 작업하기

## 현재 체크포인트 — 2026-10-07

전투 중 상점/대기석 관리, 상점 드래그 판매, 장비 드래그/즉시 적용, 디지바이스·D-3와 인장8종·배치 확장3종까지 코드와 인수인계를 함께 Git에 저장하는 시점입니다. 프로젝트 `DittochesMulti`, 브랜치 `develop`, 원격 `dittoches`를 사용합니다. 아래 ‘미커밋/최신’ 표시는 과거 기록입니다.

**다음 작업은 [NEXT_SESSION.md](NEXT_SESSION.md) 맨 위에서 시작하세요.** 완료/유지 규칙, 다음 작업 우선순위, 실행·검증 명령, 변경 파일 위치와 백업을 모았습니다. 최신 검증은 솔로12212·온라인255·서버125 통과, 실행본은 `Play_Preview.bat` 또는 USB 선별본 `01_게임실행.bat`입니다.

남은 작업: 원격 서버 배포/재시작과 외부 두 PC 검증, 실제 플레이 밸런스 점검, 정식 Unity Editor 빌드/새 셰이더 검증. 증강은 추후입니다. Git에는 코드·설정·문서를 저장하며 모델/실행본/개인 DB는 USB로 보존합니다. 정확한 커밋·업로드 상태는 `git log -1`과 원격 비교로 확인합니다.

## 2026-10-07 디지바이스·D-3 / 시너지 인장·배치 확장 — 최신

- 사용자 승인: 디지바이스+D-3는 **디지털 게이트 / 배치 +1**입니다. 디지바이스 2개는 **테이머의 유대**, D-3 2개는 **D-터미널**로 구현했습니다. 세 종류 모두 보유 장비당 배치 한도 +1이 중첩되며, 보관함/전장/대기석 어디에 있어도 한 번만 집계합니다. 장착하면 체력 +100, 회수/착용 유닛 판매로 보관함에 돌아와도 인원 보너스는 유지합니다. 최대 한도는 전장 28칸이며 경험치/상점 확률의 레벨은 올리지 않습니다. 전투 중 합성해도 추가 유닛 배치는 다음 준비 단계입니다.
- 카탈로그 v5, 장비 28종: 기존 ID0~14 유지, 디지바이스15·D-3 16, 인장17~24, 확장 장비25~27. 일반 재료 4종과 합성해 인장 8종을 만듭니다. 디지바이스는 용기/우정/사랑/지식, D-3는 투사/수호자/포격수/술사를 부여합니다. 희망/순수/조율자의 기존 자연 소속은 유지합니다.
- 시너지는 자연 소속+장착 인장을 합쳐 서로 다른 디지몬 종류를 셉니다. 원래 가진 시너지 인장/동일 인장 중복 착용은 재료를 소모하지 않고 거절합니다. 승급 때 남거나 중복되는 인장은 반환합니다. 전투 중 인장 장착/회수는 해당 팀의 현재 시너지를 즉시 다시 계산하며 현재 체력 비율·마나·발동 이력을 유지합니다. 사망은 되돌리지 않고 시작 효과도 재발동하지 않습니다. 전투 편성/성급은 시작값을 유지합니다.
- 디지몬 새 솔로/온라인 경기는 디지바이스와 D-3를 각 1개 지급합니다. 기존 솔로 저장에는 소급 지급하지 않습니다. 솔로 장비 보상과 공동 선택에도 두 재료를 연결했고 온라인은 3/7라운드에 디지바이스, 5/9라운드에 D-3를 추가 지급합니다. 저장/불러오기는 새 ID와 배치 한도를 보존합니다.
- 전체 조합 도감은 6×6이며 새 아이콘/역조합/부족 재료/효과 설명을 표시합니다. 인장 착용에 따른 시너지 단계와 전투 능력치 미리보기, 배치 HUD/배치 가능 검사/편성 예측/팀 계획의 현재 편성 비교도 연결했습니다. 팀 계획의 계획 목록은 기존처럼 자연 소속 기준 최대 9종입니다.
- 검증: 실제 솔로12212·온라인255·서버125 통과. 36개 재료 쌍, 인장8종, 확장3종의 합성/중첩/장착/회수/판매/저장, 전투 팀 효과 적용을 검증했습니다. 온라인 실제 UI 합성·체력바 드래그 장착 및 통신 단절 재시도 통과. 일반/portable 컴파일, 모션158·시점736·수치 비교1650 통과. 도감/전투 캡처 확인 완료.
- 기본 FaithfulPreview와 F:/Dittoches_LATEST_20261007 선별본에 최신 소스·DLL·StreamingAssets/DigimonBuilds.json을 함께 반영했습니다. **온라인은 최신 클라이언트와 Server 코드를 함께 사용하고 서버를 재시작해야 합니다. 원격 서버 배포/재시작은 미수행입니다.** 커밋/푸시 미수행, 정식 Unity Editor 빌드/새 셰이더 검증과 증강은 별도입니다. 수정 전 코드/서버/카탈로그/DLL은 Builds/Emblems-20261007/before, 검증 로그·화면·반영 해시는 같은 폴더입니다.

## 2026-10-07 장착 대상 판정·전투 미리보기 가독성 — 최신

- 솔로 전투의 체력바에도 장비를 놓거나 클릭해 장착/회수할 수 있습니다. 체력바를 다른 모델의 몸통보다 우선 선택하고, 사망했거나 합성으로 편성에서 사라진 유닛은 장착·미리보기 대상에서 제외합니다.
- 온라인 준비 단계 전장/대기석은 바닥 칸뿐 아니라 모델의 몸통으로 장비 대상을 선택합니다. 전투 몸통 판정은 모델 머리 높이와 현재 보간 위치를 기준으로 계산하며, 양쪽 진영에서 아군/적 구분을 검증했습니다. 클릭 장착과 드래그가 같은 대상 선택 함수를 사용합니다.
- 장비를 들고 대상 위에 올리면 초록색(장착 가능)/빨간색(장착 불가) 모서리 표시가 나타납니다. 보관함과 상점의 기존 설명이 가리지 않도록 기존 드래그 툴팁 억제를 유지합니다. 기존 오리지널 모드에는 새 디지몬 대상 UI를 표시하지 않습니다.
- 전투 장착 미리보기에서 최대 체력/현재 체력/현재 마나를 구분합니다. 최대 체력 변화는 현재 체력 비율을 유지하고 현재 마나는 그대로이며, 시작 마나·시작 보호막은 다음 전투 적용임을 명시합니다. 실제 장착/추출기 회수 결과와 예상 체력을 대조했습니다.
- 실제 솔로12063·온라인222·서버116 검사 통과. 온라인은 구매 응답 단절 후 중복 방지 포함. 일반/portable 컴파일과 모션158·시점736·수치 비교1650 통과. 전투/대기석 미리보기와 대상 강조 화면을 확인했습니다.
- 기본 FaithfulPreview 및 F:/Dittoches_LATEST_20261007의 소스/DLL을 함께 반영했습니다. 수정 전 코드는 Builds/TargetClarity-20261007/before, 로그/화면/배포 해시는 같은 검증 폴더에 있습니다. 커밋/푸시·운영 서버 재시작은 미수행, 온라인은 앞선 최신 서버 코드가 필요합니다. Unity Editor 정식 빌드/새 셰이더는 별도이며 증강은 추후입니다.

## 2026-10-07 장비 드래그 장착·조작 보완 — 최신

- 보관함 장비를 아군 모델에 끌어 놓아 바로 장착합니다. 준비 단계 전장/대기석과 전투 중 살아 있는 아군/대기석을 지원하며 진행 중 전투 능력치도 즉시 반영됩니다. 온라인은 체력바에도 놓을 수 있습니다. 기존 클릭 선택·클릭 장착·재료 두 개 클릭 합성을 유지합니다.
- 드래그 아이콘/장착 안내와 장착 전후 능력치를 표시합니다. 온라인 전투 대상의 미리보기는 현재 전투 성급과 시너지를 사용합니다. 장비/판매 드래그 중 뒤쪽 상점 툴팁이 겹치던 현상도 정리했습니다.
- ESC/우클릭, 빈 곳에 놓기, 가득 찬 장비 칸은 장비를 소모하지 않습니다. 보관함 내용/온라인 revision, 경기/라운드/단계 변경, 모달, 연결 상태와 창 포커스를 확인해 지난 드래그가 다른 장비를 장착하지 않게 했습니다. 장비를 상점에 놓아도 구매/유닛 판매로 처리하지 않습니다.
- 실제 솔로12055개(새 장비 입력14개), 온라인218개(실제 드래그와 구매 응답 단절 재시도 포함), 서버116개 통과. 일반/portable C# 컴파일, 모션158·시점736·수치비교1650개 통과. 온라인 전투/대기석 드래그 화면을 확인했습니다.
- 변경 파일: EquipmentDrag.cs(입력 수명/보관함 스냅샷), NativeGame.EquipmentDrag.cs, MultiLauncher.EquipmentDrag.cs, HUD/전투 미리보기 연결 및 실제 플레이어 검증. 신규 .meta 포함. 증강은 추후입니다.
- 기본 FaithfulPreview와 F:/Dittoches_LATEST_20261007 선별본의 코드/DLL을 함께 갱신합니다. 수정 전 코드/DLL, 로그, 화면과 배포 해시는 Builds/EquipmentDrag-20261007에 보존합니다. Git 커밋/푸시와 원격 서버 배포는 하지 않았습니다. 온라인 전투 조작에는 앞선 최신 Server 코드 재시작이 필요하며 정식 Unity Editor 빌드는 별도입니다.

## 2026-10-07 전투 중 관리·상점 드래그 판매 — 최신

- 사용자 요청: 롤체처럼 전투 중에도 전략 행동 허용, 전장 장비는 현재 전투에 즉시 반영, 작은 우측 판매 버튼 대신 상점으로 드래그 판매. 앞으로도 구매·배치·전투 중 관리 흐름을 우선 보완합니다. 증강은 여전히 추후입니다.
- 솔로/온라인: 전투 중 모집, 2G 리롤(D), 4G 경험치 구매(F), 상점 잠금, 대기석 이동·판매(E), 장비 장착/재료 합성/추출기 회수를 허용합니다. 전장 이동·판매는 전투 중 금지합니다. 준비 완료 대기 중 잠금은 기존 규칙이며 준비 취소 후 변경할 수 있습니다.
- 디지몬 모드에서 유닛 드래그 시 구매 카드 전체가 판매 영역으로 전환됩니다. 이름·가격·반환 장비 수·판매 후 이자를 표시합니다. 솔로 우측/온라인 경제 패널의 작은 판매 버튼을 안내 문구로 대체했습니다. ESC·우클릭·모달·단계 전환·끊긴 연결은 잘못된 드롭을 방지합니다. 온라인은 대기석/전장 드래그 이동도 지원합니다.
- 솔로는 전투 시작 시 유닛 성급/장비를 별도 스냅샷으로 분리합니다. 구매 합성으로 진행 중 성급/시너지가 갑자기 바뀌지 않으며 별 승급은 다음 전투에 적용합니다. 장비 변화는 살아 있는 현재 전투 유닛의 공격력/AP/공속/방어/체력/지속 효과에 즉시 반영합니다. 최대 체력 변경은 현재 체력 비율을 유지해 반복 탈착으로 회복하지 못하게 했습니다. 사망·피해 기록·위기 효과 사용 여부·공격 횟수·회복 타이머를 초기화하지 않습니다. 전투 시작 마나/시작 보호막은 중간 장착으로 다시 지급하지 않고, 이미 시작한 스킬의 확정 피해는 유지합니다.
- 온라인은 기존 결정론적 전투 계산에 장비 변경 시각을 추가합니다. 서버 수신 후 다음 0.2초 스냅샷 경계부터 적용하며, 시작 시점/이미 지나간 프레임은 유지하고 이후 프레임·결과·정산 시각을 갱신합니다. 전체 전투를 과거로 되돌려 재생하지 않습니다. `combatActions=1` 서버에서만 새 전투 조작을 허용합니다. 구매로 합성된 장비는 같은 시각에 이동/제거해 여러 전투 유닛에 중복 적용되지 않습니다. 네트워크 재시도는 기존 요청 번호 중복 방지를 사용합니다.
- 검증: 서버 116개(신규9개 포함), 실제 솔로 12041개, 실제 렌더 클라이언트1개+HTTP 상대1개 온라인205개(구매 응답 단절/재시도 포함). 일반/portable C# 컴파일, 모션158·시점736·수치비교1650개 통과. 솔로 수치는 이번 프리뷰에 있는 검사/fixture 기준이며 과거 다른 검증 폴더의 수치와 합산하지 않습니다. 숨김 Unity 실행은 OnGUI repaint가 없어 화면 검사를 완료하지 못했으며 정상 렌더 실행으로 재검증했습니다.
- 검증본 `Builds/CombatActionsPreview`, 백업/로그/화면/해시 `Builds/CombatActions-20261007`. 최종 DLL SHA256: `8efddd548a9ed8cad13a2c96724633ef8d92184c2cf468727f7c1653e82a4716`.
- 현재 기준 Git HEAD는 `9e9f47f`이며 이번 변경은 미커밋입니다. 원격 서버 배포/운영 서버 재시작/정식 Unity Editor 빌드/외부 두 PC 검증은 수행하지 않았습니다. 서버는 최신 코드로 재시작해야 전투 중 관리가 활성화됩니다. 기존 사용자 DB/저장 파일은 검증에 쓰지 않았습니다.
- 참고한 공식 TFT 설계 방향: https://nexus.leagueoflegends.com/en-gb/2019/06/dev-design-pillars-of-teamfight-tactics/ (팀 구성·장비·경제 전략). 실제 세부 수치/무료 추출 규칙 등을 TFT와 동일하게 복제했다고 주장하지 않습니다.

2026-10-06 코드 Git 업로드 완료: `dittoches/develop` 커밋 `9e9f47f9f53db24fdad6573c0232e73deead124f`를 원격 해시와 대조했습니다. 코드/필수 설정213개 파일만 커밋했고 진행 문서6개·모델/영상/실행본은 USB에 보존합니다. UTF-8 메모장 파일은 USB 루트 작업 폴더의 `디토체스_진행상황_2026-10-06.txt`입니다. 아래 단계별 미커밋 표기는 과거 기록입니다.

2026-10-06 멀티 안정성 — 12차 최신: 요청 번호로 중복 구매/준비/갱신 방지, 지난 경기·라운드 입력 차단, 일시 오류 재시도/같은 계정 재로그인, 상대 복귀 유예·통신 상태 표시를 추가했습니다. 서버107개·실제 클라이언트/HTTP 상대273개 검증(구매 직후 TCP 단절 포함) 통과. FaithfulPreview DLL `23e3dffea4e8a72372916d49e8516c716fdeedbd8de6aff25200c12ff2f77378` 반영. 운영 서버는 최신 코드 재시작 필요; 원격 배포/외부 두 PC 검증/서버 재시작 경기 복원은 미수행. 백업 `Builds/MultiplayerReliability-20261006`, 상세 NEXT_SESSION 최상단. 증강 추후.

2026-10-06 기술 효과 품질 — 11차 최신: 화염 잔광·미사일 몸체/날개/분사·얼음 파편과 부드러운 충격 효과를 실제 프리뷰에 적용했습니다. 34종 기술702개·최종 솔로87925개·온라인261개 검증 통과. FaithfulPreview DLL SHA256 `c2d24bc15179689fa9d274c9de5717b272168ce75d233505113d3de59f1eb040`, 백업/비교/로그는 `Builds/SkillFinish-20261006`. 기존 도감 영상은 10차 시점입니다. 증강 추후, 정식 Editor 빌드/새 PBR 셰이더 적용/원격 배포 미수행. 상세 NEXT_SESSION 최상단.

2026-10-06 실제 구동·도감 영상: 요청한 30초/1080p MP4를 `Builds/GameplayVideo-20261006/Dittoches_Gameplay_Guides_1080p.mp4`에 저장했습니다. 준비→용기·우정 시너지 도감→브레이브 실드·베렌헤나 장비 도감→실제 솔로 전투 순서, 음성/음악 없음. 전체900프레임 디코딩·도감 전환/전투 변화 검사와 화면 확인 완료. 기본 FaithfulPreview DLL은 모션·타격감 10차 버전을 유지합니다. 재생성은 NEXT_SESSION 최상단.

2026-10-06 모션·타격감 보강 — 최신: 공격·스킬을 유지하는 0.22초 피격 반동, 단계 크기별 제한·접지 기준 회전, 온라인 피해 시각 연결과 되감기 직후 전환 속도 초기화를 반영했습니다. 34종 피격5624개·전체352클립/스키닝332612개·실제 솔로87223개·온라인261개 통과. FaithfulPreview DLL 반영, 증강 추후. 상세 NEXT_SESSION 최상단. 정식 Editor 빌드/원격 배포 미수행.
2026-10-06 전체 품질 강화 — 최신: 사용자 “전체적으로 퀄리티 극한으로 상승” 지시에 따라 시각/모션/타격감/UI 전반을 지속 개선합니다. 이번에는 부드러운 공유 접지 그림자, 모서리를 다듬은 육각 타일, 지원 GPU의 4x MSAA를 적용했습니다. 라운드 정산의 체력 변화/수입 내역·기록 버튼도 추가했습니다. 실제 솔로81599개·온라인261개·서버94개 검증, 기본 FaithfulPreview 반영. 증강 추후. 상세 NEXT_SESSION. 온라인 정산은 최신 서버 필요, 원격 배포/정식 Editor 빌드는 미수행.

2026-10-06 모집·승급 결과 — 최신: 솔로/온라인에 실제 구매 결과의 성급·지출·초과 장비 반환을 표시합니다. 온라인 승급 유닛에는 짧은 금색 강조를 추가하고 재접속/거절 응답은 새 성공 연출을 만들지 않습니다. 실제 솔로 81593개·온라인 251개·서버 모집4개 검증 통과, FaithfulPreview DLL 반영. 증강은 추후, 지속 업데이트 중. 상세 NEXT_SESSION 최상단.

2026-10-06 온라인 단계 전환 — 최신: 전투 시작/정산/방·진영 변경 때 이전 배치 선택과 누른 상태를 정리합니다. 다음 준비 단계 첫 클릭의 의도치 않은 이동을 방지하고, 전투 중 예전 배치 칸/사망 유닛 사거리 표시를 제거했습니다. 현재 프레임 기준 생존 인원을 표시합니다. 실제 온라인 208개·일반/portable 컴파일 통과, 기본 FaithfulPreview DLL 반영. 증강은 추후, 지속 업데이트 중. 상세 NEXT_SESSION 최상단.

2026-10-06 전투 HUD 성능 — 최신: 체력바 후보 검사를 줄이고 솔로/온라인 체력바·전투 기록 행·글꼴 설정을 재사용합니다. 512배치/4599개 위치가 기존과 일치, 겹침 검사 약85% 감소, 배치 계산 묶음 중앙값 244.4→79.7ms(전체 FPS 아님). 실제 솔로 81576개·온라인 123개 통과, 기본 FaithfulPreview DLL 반영. 증강은 추후, 지속 업데이트 중. 상세 NEXT_SESSION 최상단. 정식 Editor 빌드/원격 배포 미수행.

2026-10-06 전투 발동 연출 — 최신: 위기 효과 발동은 금색 표시/문구, 우정 최대 중첩은 푸른 파동/문구를 짧게 표시합니다. 실제 전투 시간으로 재생하고 사망/지난 발동/옛 서버에는 새 효과를 만들지 않습니다. 서버 89개·실제 솔로 76927개·온라인 123개 및 양쪽 진영 실전 자료 렌더 검증 통과, FaithfulPreview DLL 반영. 증강은 추후, 지속 업데이트 중. 상세 NEXT_SESSION 최상단. 최신 서버 필요, 원격 배포/정식 Editor 빌드 미수행.

2026-10-06 지난 전투 상세 — 최신: 온라인 전투 기록에서 유닛을 클릭하면 당시 체력/능력치/피해/회복/보호막/공격·시전 횟수를 조회합니다. 현재 유닛을 판매하거나 장비를 바꿔도 기록은 유지하며 현재 배치 선택과 분리됩니다. 서버 89개·실제 온라인 115개 및 일반/portable 컴파일 통과, FaithfulPreview DLL 반영. 최신 서버가 필요하며 원격 배포는 미수행입니다. 증강은 추후, 사용자 요청으로 지속 업데이트 중. 상세 NEXT_SESSION 최상단.

2026-10-06 온라인 상점 잠금 — 최신: 모집 목록을 다음 라운드까지 유지하는 상점 잠금을 실제 UI/서버에 추가했습니다. 전투 중·준비 완료 후에도 설정 가능, 구매한 빈 칸 유지, 수동 갱신 2G는 허용, 해제 전까지 지속하며 재접속도 유지합니다. 상대 목록/잠금 상태는 숨깁니다. 서버 88개·실제 온라인 108개 통과, FaithfulPreview DLL 반영. 증강은 추후이며 지속 업데이트 중입니다. 상세 NEXT_SESSION 최상단. 원격 서버 배포는 미수행입니다.

2026-10-06 전투 상태 가독성 — 최신: 솔로/온라인 유닛 정보에 우정 중첩·강화 공격 주기·위기 효과 사용 여부를 추가했습니다. 준비/현재 프레임/사망·종료 기록을 구분하고 온라인의 미래 결과 선표시를 방지합니다. 실제 솔로 76916개(시너지 계산 비교 75900개 포함), 로컬 온라인 94개, 서버 81개 통과. FaithfulPreview DLL 반영 완료. 상세 NEXT_SESSION 최상단. 사용자 요청으로 지속 업데이트 중이며 증강은 추후, 원격 서버 배포·정식 Editor 빌드는 미수행입니다.

2026-10-06 시너지 전면 개편 — 최신: 문장 6종·전투 역할 5종의 효과/수치/설명을 전면 재설계했습니다. 기존 30종 소속·단계/장비 ID·조합은 유지하며, 우정 공속 중첩·희망 1회 회복·조율자 팀 저항을 솔로/서버에 공통 적용합니다. 전용 아이콘 11종과 전체 도감(단계/부족 인원/3D 초상화/보유 상태), 뒤쪽 입력 차단을 추가했습니다. 증강은 추후입니다. 서버 80개·수치 비교 1650개·시너지 계산 3036구성·실제 솔로 76910개(계산 필드 검사 포함)/온라인 90개·조합 전투 110개 통과. FaithfulPreview DLL+카탈로그 v4 반영 완료. 원격 서버 배포·정식 Editor 빌드 미수행, 최종 경쟁 밸런스는 후속 플레이테스트 필요. 상세는 NEXT_SESSION 최상단, 변경은 미커밋입니다.

## 2026-10-06 팀 계획·상점 목표 표시 — 최신

로비와 솔로/온라인 경기에서 공통 **팀 계획**을 사용할 수 있습니다. 플랜 3개에 최대 9종씩 저장하고 이름·시너지·가격으로 검색합니다. 목표의 전장/대기석/미보유 상태, 현재→계획 시너지와 활성 효과를 비교하며 현재 전장을 계획으로 담기·비우기·이전 변경 취소를 지원합니다. 활성 목표가 상점에 나오면 표시합니다. 창 뒤 게임 조작/단축키를 차단하지만 라운드 시간은 계속 흐릅니다. 계획은 PC에 저장하며 실제 배치·구매·전투 수치를 바꾸지 않습니다. 증강은 추후 작업입니다.

계획 UI **134개**, 실제 솔로 **957개**, 온라인 **86개**, 서버 **74개**, 일반/portable 컴파일과 기존 모션/타이밍/수치 검사를 통과했습니다. FaithfulPreview DLL 반영 완료이며 `Play_Preview.bat`로 실행합니다. 검증 화면은 `Builds/TeamPlanPreview`, 이전 DLL/로그/반영 SHA는 `Builds/TeamPlanner-20261006`에 있습니다. 정식 Editor 빌드·원격 배포·계획 클라우드 동기화는 수행하지 않았습니다. 상세는 NEXT_SESSION 최상단을 따릅니다.

## 2026-10-06 테이머·필드·처형 효과 대기실 — 최신

조작 캐릭터를 **테이머**로 통일하고 공통 대기실 설정에 테이머 4종·필드 3종·처형 효과 3종을 추가했습니다. 실제 3D 전장 미리보기, 이동 시연/효과 재생, 적용·되돌리기, PC 저장/복원과 매칭 중 변경 제한을 제공합니다. 비트몬은 기존 2D, 나머지 3종은 기존 faithful 3D 모델입니다. 선택한 필드는 자기 화면에, 처형 효과는 라운드 승리 마무리에 적용됩니다. 온라인 상대 테이머 표시·이동 동기화·재접속 유지와 효과 중복 재생 방지도 연결했습니다. 증강은 추후 작업입니다.

서버 **74개**, 실제 설정 창 **200개**, 솔로 **952개**, 온라인 **84개**, 일반/portable C# 컴파일 및 모션/타이밍/수치 검사를 통과했습니다. `Play_Preview.bat` 실행본인 FaithfulPreview에 DLL을 반영했고, 실제 화면은 `Builds/TamerPreview`, 이전 DLL과 반영 기록은 `Builds/TamerLoadout-20261006`입니다. 정식 Editor 빌드/원격 배포는 하지 않았습니다. 온라인 적용은 최신 서버 재시작이 필요합니다. 상세·제한·재검증은 NEXT_SESSION 최상단을 따릅니다.

## 2026-10-06 디지털 무장 15종·장비 도감 개편 — 최신

재료 4종·완성 무장 10종·추출기 1종의 한 글자 표시를 전용 그림 아이콘으로 교체했습니다. 솔로/온라인 공통 도감에 전체 조합표, 재료 보유/부족 수량, 기본 능력치·전용 효과·활용 방향을 넣었습니다. 보관함·장착 표시·전장 배지·선택 중 아이콘·장착 미리보기·보상에 연결했고, 합성 가능한 상대 재료와 결과도 안내합니다. 기존 장비 이름/ID/수치/효과/조합식은 유지합니다. 증강은 추후 추가합니다.

서버 69개, 실제 솔로 948개/온라인 73개, C# 컴파일·모션/스킬/수치 검사를 통과했습니다. 검증 DLL과 최신 카탈로그를 FaithfulPreview에 반영했으며 `Play_Preview.bat`로 실행합니다. 화면은 `Builds/EquipmentPreview`, 이전 DLL/카탈로그와 SHA는 `Builds/EquipmentRedesign-20261006`에 있습니다. 정식 Editor 빌드/원격 배포는 수행하지 않았고 코드·문서는 미커밋입니다. 상세는 NEXT_SESSION 최상단을 따릅니다.

## 2026-10-06 상점·경제·배치 안내 보완 — 최신

증강은 추후 추가하도록 남겨 두었습니다. 솔로/온라인 상점에 구매 후 골드·이자 변화, 레벨업 구매 비용, 다음 레벨 상점 확률, 보유 수량·연속 합성 안내와 전체 카드 우클릭 상세 보기를 추가했습니다. 빈 전장 슬롯 안내도 보완했습니다. 온라인의 대기석 만석 자동 합성 구매를 서버/클라이언트 양쪽에서 수정했고 장비·재고·실패 원자성을 검사했습니다.

서버 테스트 69개, 실제 솔로 848개·온라인 73개, C# 컴파일과 모션/스킬/수치 검사를 통과했습니다. `Play_Preview.bat`의 FaithfulPreview에 검증 DLL을 반영했습니다. 화면/로그는 `Builds/TacticalUpdatePreview`, 이전 DLL·SHA 기록은 `Builds/TacticalUpdate-20261006`입니다. 이미 실행 중인 별도 서버는 수정 반영에 재시작이 필요합니다. 정식 Unity Editor 빌드·원격 서버 배포는 수행하지 않았습니다. 상세는 NEXT_SESSION 최상단을 따릅니다.

## 2026-10-06 홍보용 테스트 영상

가로 16:9, 1080p/30fps, 24초 영상 `Builds/PromoVideo-20261006/Dittoches_Promo_Test_1080p.mp4`를 제작했습니다. 진화 단계 비교·대표 기술 3종·실제 서버 전투 리플레이에 한글 자막과 직접 합성한 배경음을 더했습니다. 720프레임과 최종 MP4 전체 디코딩 검사를 통과했습니다. 재생성 방법과 검증 자료는 NEXT_SESSION 최상단을 따릅니다. 영상은 로컬 USB 보존 자료이며 게시하지 않았습니다.

## 2026-10-06 3D 초상화·진화 단계 크기 — 최신

현재 34종 메시로 렌더한 68장 초상화를 상점·도감·상세·전투 기록에 연결했습니다. 유년기→성장기→성숙기→완전체→궁극체 기본 높이는 0.52→0.88→1.28→1.64→2.02이며 그림자/이름표도 맞췄습니다. 별 합성이 단계 크기 순서를 뒤집지 않습니다. 단계 크기 629개, 실제 솔로 UI/전투 838개 검사를 통과했습니다. `Play_Preview.bat`는 갱신한 FaithfulPreview를 실행합니다. `Assets/StreamingAssets/FaithfulPortraits`의 68장/manifest(약 19 MB)를 모델 패키지와 함께 USB 보존하세요. 기존 모델/모션과 전투 수치는 그대로이며, 정식 Unity Editor 빌드는 여전히 별도입니다. 상세 명령·도감 검사·화면·배포 검증은 NEXT_SESSION 최상단과 `Builds/PortraitReview-20261006`을 따릅니다.

## 2026-10-06 전체 모션·게임 통합 — 최신

검토된 실제 모델 34종/352클립을 솔로·온라인 전장에 연결했습니다. `Play_Preview.bat`로 `Builds/FaithfulPreview`를 실행합니다. `Assets/StreamingAssets/FaithfulModels`의 약 72 MB 패키지·기술 메타데이터·출처를 USB로 함께 옮기세요. 포즈/속도 전환과 발 디딤 위상, 전투 발사 시점·관절 위치, 투사체 출발점 고정을 보완했습니다. 재생성 명령과 전체 검증은 NEXT_SESSION 최상단 및 `Builds/MotionEngine-20261006`을 따릅니다. 기존 플레이어의 셰이더로 실제 게임 렌더를 검사했으며 새 셰이더의 정식 Unity Editor 빌드와 전체 UI 재검사는 남아 있습니다. 아래 게임 미통합 기록은 과거 상태입니다.

## 2026-10-06 메탈그레이몬 어깨 수정 — 최신

메탈그레이몬 실제 어깨 간격을 약 44% 넓히고 흉곽/상완 두께와 Down 바닥 접촉을 보완해 갤러리에 반영했습니다. 10동작 변형·GLB·WebGL·기술 표시 검증 통과. 이 종의 기술 저작 기준은 이제 `ArtSource/MetalGreymonShoulderApproved-20261006`이며 다른 종은 기존 기준입니다. 이 폴더와 수정 전 `MetalGreymonShoulderBackup-20261006`도 USB로 보존하세요. 전후 이미지는 `Builds/MetalGreymonShoulder-20261006/shoulder-before-after.jpg`, 상세는 NEXT_SESSION 최상단. Unity 미통합입니다.

## 2026-10-06 표면 재질·기술 표현 개선 — 최신

현재 갤러리는 종별 표면 재질과 기술별 전용 형상을 사용합니다. 최신 코드는 `character-finish.js`, `technique-signatures.js` 및 수정한 `technique-effects.js`/`gallery.js`입니다. 모든 파일을 함께 보존하세요. 검증/전후 캡처는 `Builds/QualityPass-20261006`, 작업 범위/실행 명령은 `NEXT_SESSION.md` 최상단입니다. 모델 체형/리그/GLB는 기존 그대로이며 게임에는 미통합입니다.

## 2026-10-06 재개

현재 프로젝트는 `F:\Dittoches_KEEP_20260917\01_CurrentProject\DittochesMulti`입니다. `develop` ce22d6e와 원격 일치를 확인했고 기존 미커밋 작업을 유지했습니다. 34종 모델의 갤러리 모션 연결과 전환 중 일시정지를 보완했습니다. 모델 파일은 기존 그대로이며 Unity 게임에는 미통합입니다. 상세·검증 명령·Python 3.11 환경은 `NEXT_SESSION.md` 최상단을 따릅니다.

## 2026-10-02 종별 기술·공격 모션 — 현재 기준

사용자가 직전 외형·모션을 받아들인 뒤 **디지몬별 기술 파악과 일반/특수 공격, 부드러운 모션 연결**을 요청했습니다. 공식 도감 34종의 기술 설명을 확인해 **Attack/Skill 68클립을 교체**하고 갤러리에 기술 효과·기술명·공식 설명 링크·기술만 순회하는 시연을 추가했습니다. 전체는 기존과 같은 **34종/352클립**입니다. 각 모델의 다른 8개 공통 클립 272개, 네이티브 원본 12클립, 메시·스킨·재질·텍스처·노드 정의와 원본 BIN 바이트는 그대로 보존했습니다.

- 그레이몬 메가 플레임은 입에서 나가며 목·가슴의 준비와 반동이 연결됩니다. 워그레이몬은 양팔을 머리 위로 들어 가이아 포스를 모아 던집니다. 메탈그레이몬은 가슴 미사일, 릴리몬은 두 손목 캐논, 엔젤몬은 지팡이 반대쪽 빛 주먹, 홀리엔젤몬은 실제 검 손과 게이트, 로제몬은 실제 채찍 손과 마디 지연, 세라피몬은 일곱 빛, 버드라몬은 날개 깃털 유성으로 구분했습니다.
- 텐타몬은 날개에서 정전기, 가루다몬은 새 그림자를 남기는 바람 칼날, 봉황몬은 네 날개의 황금 입자입니다. 공식 설명에 맞춰 토게몬은 단단해진 바늘 주먹 연타, 토코몬은 물기로 정리했습니다. 토코몬 턱·쿠가몬 좌우 집게에는 독립 관절이 없어 해당 원본 리그 범위의 몸·목·아래턱과 타격 효과로 표현합니다. 원작의 모든 기술을 제작하거나 공식 게임 모션을 추출한 결과가 아닙니다.
- **관절 모션은 GLB에 포함**, 불꽃·전기·물·미사일·빛·문·채찍 등의 효과는 `Tools/faithful_gallery/technique-effects.js`에서 클립 시간과 관절 위치로 계산합니다. 효과는 정지·배속·프레임 이동·반복과 함께 움직이고 비공격/수정 전 보기에는 남지 않습니다. Unity 일반 플레이는 기존 2D이며 이 갤러리 작업을 게임 전투에 통합하지 않았습니다.
- 가감속은 양 끝 속도/가속도가 0인 곡선으로 구성하고 공격 후 중립 자세로 복귀합니다. 팔·턱·긴 귀·날개에서 발견된 늘어짐/급격한 이동은 동작 범위와 준비 시간을 조정했습니다. 34종 변형 표본, 프레임별 관절 이동, 그레이몬 눈 축/목 연결, GLB 구조·타이밍·보존 바이트 검사가 통과했습니다. 새 68클립은 실제 WebGL 변형 및 효과·기술명·준비/발사/복귀·켜기/끄기·재생 조작 검사를 했고, 변경 없는 클립은 동일 바이트와 직전 실제 브라우저 검사 증거를 확인했습니다. 게시 파일 SHA와 전후 비교·기술 시연 검사도 통과했습니다.
- 현재 입력/복구 기준은 **`ArtSource/TechniqueMotionBackup-20261002`**입니다. `--reuse-current`는 `author_technique_motion.py`로 분기해 이 백업의 Blender/GLB를 읽고 두 공격만 다시 굽습니다. `merge_technique_clips.py`가 두 클립의 애니메이션 데이터만 원본 GLB에 합칩니다. **그레이몬 목 재구성·재스키닝을 다시 적용하면 안 됩니다.** 직전 ExpressiveMotionBackup 및 이전 백업은 과거 이력으로 보존합니다.
- 기술 정의/출처: `Tools/faithful_techniques.py`, 관절 연출: `faithful_technique_poses.py`·`faithful_native_techniques.py`, 전체 기술 자료: `ArtSource/FaithfulGallery/techniques.json`, 조사 원문: `Builds/TechniqueResearch/official-profiles.json`. 그레이몬 공식 페이지는 `greymon-first`이며 `greymon`(다른 버전)을 사용하지 않습니다.
- 확인: `http://127.0.0.1:8766/?motion=Skill#greymon`, 전체 기술 시연: `http://127.0.0.1:8766/?demo=skills#greymon`. ‘기술 효과’로 몸동작만 볼 수 있습니다. 검토 이미지: `Builds/TechniqueReview/techniques-1.jpg`~`techniques-6.jpg`, `techniques-overview.jpg`. 실제 영상 대신 갤러리 재생과 준비/발사 캡처를 제공했습니다.
- 검증: `Builds/FaithfulMotionValidation/review-techniqueallfirst/browser-report.json`, `review-techniqueallrest/browser-report.json`, `published-report.json`, `Builds/FaithfulNaturalValidation/{glb-report,native-transition-report,comparison-report}.json`, 각 모델의 `deformation-report.json`. 반영은 `Tools/promote_faithful_motion.py`의 SHA/기술 브라우저 검사 게이트를 통과해야 합니다.
- Windows 검사 명령은 C드라이브 작업 디렉터리에서 G드라이브 절대 경로를 사용합니다. Python 3.12용 `tmp/gallery-test-tools-py312`의 greenlet을 먼저 로드하는 기존 래퍼로 실행합니다. 두 개의 소프트웨어 WebGL 브라우저를 동시에 돌리면 매우 느려질 수 있어 순차 검사를 권장합니다. 인앱 브라우저는 이번에도 sandboxPolicy 오류로 시작하지 못해 설치된 Edge를 사용했습니다.
- 코드·문서는 미커밋입니다. 모델·백업·Blender 편집본·검증 자료는 Git 제외 USB 자료로 함께 보존합니다. 이전 기록의 ‘현재 기준’은 아래 이력입니다.

## 2026-10-02 그레이몬 목 재구성·동작 개성 보완 — 현재 기준

사용자가 직전 수정본도 “그레이몬 목이 돌아가 있고 모션이 단조롭다”고 지적해 다시 수정했습니다. **그레이몬의 실제 눈 표면 축을 기준으로 머리를 수평 정렬하고, 별도 Neck 관절·목 두께·목 가중치를 다시 만들었습니다.** 골반과 어깨 축, 팔꿈치·손·발 방향도 재정렬해 중립 자세를 새로 구웠습니다. 공개 원본의 메시 면수·UV·재질·출처는 유지했습니다. 현재 작업은 34종 갤러리의 모델·모션 수정이며 Unity 게임 적용은 아직입니다.

- 그레이몬은 목과 머리가 나눠 움직입니다. 물기는 앞으로 몸을 싣고 턱을 열며, 특수 공격과 승리는 목·가슴을 들어 올리는 포효 자세로 구분했습니다. `alignment-report.json`은 실제 양 눈의 높이/앞뒤 축과 Spine→Neck→Head 연결을 검사하고 반영 GLB의 SHA를 기록합니다.
- 전체 모션은 대기 중 시선 이동·호흡·체중 이동, 보행 시 몸통 반동·팔 스윙·꼬리 지연을 늘렸습니다. 물기/베기/양팔 연속 공격/펀치/돌진/지팡이/검/채찍/마법 등 종별 패턴을 나눴고 워그레이몬 특수 공격은 양팔을 머리 위로 올립니다. 유년기 눌림·도약, 사족형 몸통 돌진, 봉황몬 네 날개 위상, 승리 제스처와 넘어짐을 보완했습니다.
- 큰 동작에서 드러난 파피몬 등 털·팔 안쪽과 헤라클레스캅테리몬 복부의 잘못된 관절 연결을 국소 수정했습니다. 로제몬은 실제 채찍 손의 관절 방향을 확인해 손목 방향을 고정하고 채찍 마디가 순서대로 따라오게 했습니다. 어니몬 공격은 1.6초로 늘려 큰 꽃의 반동이 급하지 않게 했습니다. 연속 공격의 두 번째 팔은 첫 타격과 독립된 연속 곡선을 사용합니다.
- 버드라몬은 사용자가 기준으로 든 기존 비행 동작을 유지했습니다. 쿠가몬은 원래 관절에 네 팔·가슴·머리 반동을 보완했습니다. 두 종의 원본 12클립과 원본 메시·스킨·재질 BIN 바이트도 보존했습니다. 총량은 공통 340개와 원본 12개, **34종/352클립**으로 같으며 모든 클립을 새로 제작하거나 게임에서 추출했다고 설명하지 않습니다.

검증: 34종 GLB 구조·타이밍·루프·정규화 가중치·기존 면수와 텍스처 보존, 공통 340클립의 표본 표면 변형/접지 및 모든 프레임 관절 전환, 전체 352클립의 실제 Edge WebGL 정점 이동·재생·타임라인·단발 복귀/반복·Down 유지가 통과했습니다. 34종의 대기/걷기/공격/특수 공격/승리/쓰러짐 6포즈를 7장으로 시각 검토했고 그레이몬의 정면 전후와 공격 애니메이션도 캡처했습니다. 6개 동작의 수정 전후 비교·정규화 시간·정지 상태 복원과 반영 SHA 일치를 확인했습니다. 표본 검증이며 완벽한 원작 재현이나 모든 자기 관통 인증은 아닙니다.

재생: `http://127.0.0.1:8766/?motion=Walk#greymon`, `Tools/Open_Faithful_Gallery.ps1 -Model greymon -Motion Walk`. `전체 시연`으로 34종을 순회하며 `수정 전 보기`는 이번 직전 모델과 같은 동작·정규화 시점을 비교합니다. 실제 캡처는 `Builds/FaithfulNaturalValidation/greymon-stance-before-after.png`, `greymon-attack-before-after.gif`, `motion-sheet-01.png`~`07.png`이며 전체 미리보기는 `Builds/FaithfulFullRosterValidation/all-34-species.png`입니다.

**현재 재생성 입력은 `ArtSource/ExpressiveMotionBackup-20261002`**입니다. 갤러리 34종·Blender 편집본·도구·검사와 `model-hashes.json`을 보존했습니다. `faithful_motion_catalog.BASELINE`과 `--reuse-current`가 이 백업을 읽습니다. 이전 NaturalMotionBackup 및 다른 백업도 그대로 둡니다. 새 목 교정은 이 입력에 한 번 적용하며 이미 교정된 후보에 반복 적용하지 마세요. 편집본 `AnimatedReview`, 반영본 `FaithfulGallery`, 비교본 `FaithfulGallery/previous`는 구분합니다. 정적 prepare/재질 전용 도구로 현재 애니메이션을 덮어쓰지 마세요.

핵심 코드: `faithful_greymon_rebuild.py`, `verify_greymon_alignment.py`, `faithful_motion_personality.py`, `faithful_motion_binding.py`, `faithful_natural_motion.py`, `faithful_native_motion.py`, `faithful_motion_styles.py`, `faithful_motion_catalog.py`. 검사 기록: `FaithfulMotionValidation/published-report.json`, `review-expressivefirst/rest/native/fix/last`, `FaithfulNaturalValidation/natural-motion-report.json`, `glb-report.json`, `native-transition-report.json`, `comparison-report.json`, 각 모델의 `deformation-report.json`. 변경은 ce22d6e 위 미커밋이며 모델·백업·검사 파일은 Git 제외 USB 자료입니다. 아래 기록은 이전 수정 이력입니다.

2026-10-02 자세·모션 보완 — 가장 최신: 그레이몬의 비대칭 기본 자세(팔·다리·골반·턱)를 교정하고 **34종의 공통 10동작, 총 340개를 다시 다듬어 갤러리에 반영**했습니다. 원본 12개를 포함한 전체 352클립의 WebGL 재생·GLB 구조·반영 SHA 검사가 통과했습니다. 체형별 보행·발 디딤·관절 방향·준비/타격/복귀·쓰러짐 접촉 해제를 보완했습니다. 입력 기준은 새 `ArtSource/NaturalMotionBackup-20261002`이며 이전 백업은 보존합니다. `--reuse-current`는 이 백업을 읽고 그레이몬 자세 교정은 여기서 한 번만 적용합니다. `http://127.0.0.1:8766/?motion=Walk#greymon`에서 확인합니다. Unity 게임은 여전히 기존 2D이며 이번 갤러리 모델의 게임 적용은 하지 않았습니다. 아래 모션 확장 기록은 수정 전 이력입니다. 상세는 `NEXT_SESSION.md`와 `AGUMON_MODEL.md` 최상단을 보세요. 변경은 미커밋, 모델·백업·검사 자료는 Git 제외 USB 보존 자료입니다.

2026-10-02 모션 확장 — 가장 최신: **실제 메시 34종·공통 10동작·총 352클립, 미준비 0종**을 갤러리에 반영했습니다. 직전 204개에서 148개가 추가됐고 버드라몬/쿠가몬의 원본 12클립도 보존합니다. 34종 GLB·변형·352클립 실제 WebGL 재생 및 반영 SHA 검사가 통과했습니다. 특수 공격·방어·회피·쓰러짐과 전체 자동 시연이 추가됐습니다. `Models_Demo.bat` 또는 `http://127.0.0.1:8766/?demo=1#agumon`으로 봅니다. 이번 직전 백업은 `ArtSource/MotionExpansionBackup-20261002`이며 `--reuse-current`의 입력 기준입니다. 기존 백업·출처와 2D 게임은 보존하며 Unity 게임 적용은 아직입니다. `NEXT_SESSION.md`/`AGUMON_MODEL.md` 최상단을 우선하고 아래 204클립·6동작은 과거 이력으로 읽으세요. 변경은 미커밋이며 모델·백업·검사는 Git 제외 USB 자료입니다.

2026-10-02 전체 갱신 — 가장 최신: 실제 메시 **34종/204클립, 미준비 0종**을 갤러리에 반영했습니다. 기존 13종 모션 보완, 기존 2종 재질 보완(원본 12클립 보존), 미제작 19종 추가와 각 6동작입니다. 32종 변형/GLB 검사 및 34종/204클립 WebGL 재생 검사와 반영 SHA 일치, 3종 전후 비교가 통과했습니다. 전체 갱신 전 백업은 `ArtSource/FullRosterBackup-20261002`, 새 원본은 `ExpandedSources-20261002`, 출처는 `ThirdPartyCandidates`, 반영본은 `FaithfulGallery`입니다. 구형 도형 조립식 모델을 재사용하지 않았으며 Unity 게임 적용은 아직입니다. 코드·문서는 미커밋, 모델·백업은 Git 제외 USB 로컬 자료입니다. `NEXT_SESSION.md`와 `AGUMON_MODEL.md` 최상단을 우선하고 아래 15종/90클립·미준비 19종은 과거 이력으로 읽으세요.

## 2026-10-02 현재 상태

아구몬 얼굴·손 자세와 얼굴 관절 연결을 보완하고 검증 후 갤러리에 반영했습니다. 15종/90클립·미준비 19종·Unity 미적용 상태입니다. 수정 전 전체 `ArtSource/FacePassBackup-20261002`도 다른 백업과 함께 보존하세요. 비교 버튼은 아구몬의 10월 1일 최종본과 새 수정본을 보여 줍니다. 시작 HEAD는 `ce22d6e`, 이번 코드·문서는 미커밋입니다. 현재 G 드라이브에서 실행하며 Python 3.12 브라우저 검사 환경과 재개 순서는 [NEXT_SESSION.md](NEXT_SESSION.md) 최상단을 따릅니다.

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
