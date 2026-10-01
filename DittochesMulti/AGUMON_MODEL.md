# 아구몬 Blender 모델과 모션 — 2026-09-29

> 2026-10-01 Git 저장 시점: 현재 코드와 다음 작업 메모를 저장합니다. 다음 세션은 [NEXT_SESSION.md](NEXT_SESSION.md) 최상단의 실행·수정·검증 절차에서 시작하세요. 현재 기준은 실제 메시 15종/90클립·미준비 19종·Unity 미적용입니다. 아래 ‘커밋·푸시하지 않았습니다’는 각 작업 당시 기록입니다. 모델·텍스처·원본 백업·검증 보고서는 Git 밖의 USB 로컬 자료이므로 함께 보존해야 합니다.

## 2026-10-01 2차 자연스러움 보완 — 가장 최신

‘더 수정해’ 요청에 따라 기존 15종 갤러리 중 직접 리깅한 13종을 추가 보완했습니다. 아구몬은 원본 메시의 주둥이 앞면·모서리를 곡면으로 다듬고 치아·잇몸도 함께 변형해 연결을 유지했습니다. 팔꿈치와 손목을 더 굽혀 손을 앞으로 들었고, 입 안과 혀의 과한 붉은 재질을 낮췄습니다. 워그레이몬은 무릎을 조금 낮춘 준비 자세와 앞으로 굽힌 팔로 바꿨습니다.

지상형 보행의 발 이동 속도를 착지·이륙 경계에서 이어 주고 골반의 체중 이동과 상체 반대 회전을 넣었습니다. 아구몬·워그레이몬·홀리엔젤몬·세라피몬에는 실제 발 표면을 기준으로 발목 높이를 보정하는 뒤꿈치 접촉·앞꿈치 밀기를 추가했습니다. 13종의 78클립 모두 GLB 내보내기 때 생기던 시작 1프레임 공백을 제거했습니다. 실제 Walk 길이는 이제 1.00초이며 대기·걷기·달리기 루프의 처음/끝 자세가 이어집니다. 종 수와 모션 수는 15종/90개로 그대로입니다. 버드라몬·쿠가몬 원본 클립은 이번에 바꾸지 않았습니다.

이번 수정 직전의 갤러리·Blender 원본·검증·도구는 `ArtSource/NaturalPassBackup-20261001`에 보존했습니다. `animate_faithful_models.py --reuse-bind`는 이 백업의 검증된 관절 가중치를 읽어 수정하므로 스컬핑을 반복해서 누적하지 않습니다. 주둥이·재질 수정 코드는 `faithful_sculpt.py`입니다. 수정 전 GLB 13종은 `FaithfulGallery/previous`에 있으며 미리보기의 **수정 전 보기 / 수정 후 보기** 버튼으로 같은 시점·각도에서 전환합니다.

검증: 13종/78클립 변형·접지 표본 검사와 실제 Edge 재생 검사, 내보낸 GLB의 시작 시간 0·정확한 길이·루프 연결·가중치 정규화·삼각형 및 텍스처 개수 보존을 확인했습니다. 비교 버튼은 아구몬·워그레이몬·홀리엔젤몬에서 이전/현재 모델이 다르고, 일시 정지한 동작의 시점과 현재 자세가 정확히 복원되는지 검사했습니다. 결과와 전후 화면은 `Builds/FaithfulNaturalValidation`, 새 브라우저 검사는 `FaithfulMotionValidation/review-naturalA`와 `review-naturalB`, 통합 배포 해시는 `FaithfulMotionValidation/published-report.json`입니다.

갤러리를 새로 열어야 최신 JavaScript와 비교 버튼이 보입니다. 실행 주소는 `http://127.0.0.1:8766/?motion=Walk#agumon`입니다. 원작 세부 외형의 완성 판정은 아니며, 19종 추가 모델과 Unity 게임 적용은 계속 미완료입니다. 일반 2D와 기존 빌드는 보존했습니다.

## 2026-10-01 실제 메시 15종 모션 추가 — 최신 기록

공개 외부 모델 15종 모두 갤러리에서 움직입니다. 기존 정적 모델 13종에 관절과 대기·걷기·달리기·공격·피격·승리 동작 78개를 새로 제작했고, 버드라몬의 원본 11개와 쿠가몬의 원본 1개를 보존해 총 90개입니다. 쿠가몬에는 아직 여섯 가지 기본 동작이 모두 있는 것이 아닙니다. 34종 중 나머지 19종은 원작 외형 모델 준비 중입니다. 새 동작을 마스터즈에서 추출한 원작 모션이라고 설명하지 마세요.

아구몬은 머리 윗부분을 12% 낮추고 폭을 5.5% 넓히며 팔을 앞으로 굽혔습니다. 그레이몬은 원본의 분할 메시를 연결·용접한 뒤 다시 줄여 움직일 때 벌어지는 틈을 수정했고, 겹쳐 보이는 손과 허벅지는 표면 연결 구조를 따르는 heat 가중치로 분리했습니다. 홀리엔젤몬·세라피몬의 T 자세를 내렸으며 홀리엔젤몬은 앞뒤 날개를 나눠 펼쳤습니다. 손톱·발톱·장갑·깃털 등 분리된 단단한 부위는 형태를 유지하고, 연결된 피부는 표면을 따라 가중치를 부드럽게 섞습니다. 사족 보행, 귀·꽃·꼬리의 후속 움직임, 유년기 도약과 접지도 넣었습니다. 원작 재현도와 모든 세부 조형이 완성됐다는 판정은 아닙니다.

원본 정적 갤러리는 `ArtSource/AnimationBackup-20261001`, 편집 가능한 13종 Blender 파일과 동작별 렌더는 `ArtSource/AnimatedReview/<id>`, 실제 갤러리 배포본은 `ArtSource/FaithfulGallery/<id>`에 있습니다. 원본 제작자·라이선스·다운로드 자료는 유지했습니다. 생성기는 `Tools/animate_faithful_models.py`, 관절 위치는 `faithful_rig_profiles.py`, 가중치 연결은 `faithful_skin_topology.py`입니다. `prepare_faithful_*`를 그대로 다시 실행해 새 모션 모델을 정적 파일로 덮어쓰지 마세요.

미리보기는 `http://127.0.0.1:8766/?motion=Walk#agumon` 또는 루트 `Preview_Faithful_Digimon_3D.bat`입니다. 모션 전환 0.22초, 단발 공격 후 대기 복귀, 공격·피격 반복 선택, 0.25배속, 타임라인과 ±1프레임 이동을 지원합니다. `Tools/Open_Faithful_Gallery.ps1 -Model agumon -Motion Walk`로 실제 브라우저 창을 엽니다.

검증: 13종의 78개 클립에서 반복 시작/끝 자세, 유한한 변형 좌표, 과도한 표면 늘어짐과 바닥 관통을 표본 검사했습니다. 실제 Edge WebGL에서 15종의 90개 클립 모두 정점이 움직이는지 확인했고, 일시 정지·재생·프레임 이동·공격 후 복귀·반복도 검사했습니다. 보고서는 `Builds/FaithfulMotionValidation/published-report.json`과 `published-smoke.json`, 실제 화면은 `published-agumon-walk.png`입니다. 검증한 GLB와 갤러리 GLB의 SHA-256이 일치합니다. 수치 검사는 원작 외형 완성도 판정과 구분합니다.

일반 2D 플레이와 기존 UnityWindows 빌드는 변경하지 않았습니다. 새 모델·모션의 Unity 게임 연결과 명중 타이밍, 나머지 19종, 종별 원작 모션 대조가 남았습니다. Git develop의 기존 변경을 보존했으며 커밋·푸시는 하지 않았습니다.

## 2026-10-01 원작 외형 재작업 — 아래 과거 완료 기록보다 우선

사용자가 34종 절차형 모델의 외형을 명확히 거절했습니다. 도형 조합 모델에 특징을 덧붙이는 대량 생성은 중단했습니다. 이전 구조·모션 검사 통과는 원작 재현도 통과를 의미하지 않습니다. `author_roster.py`의 체형 모듈과 표면 개선은 중단된 실험 코드이며, 그대로 다시 대량 생성하지 마세요.

새 방향은 실제 조형·텍스처가 있는 공개 외부 GLB를 원작과 비교하고, 필요한 메쉬·재질·리깅을 개별 수정하는 것입니다. `ArtSource/ThirdPartyCandidates`에 공개 Objaverse 배포 원본과 업로더 메타데이터를 보존했고, `ArtSource/FaithfulGallery/<id>`에 검토용 모델·출처·최적화 보고서·실제 렌더를 분리했습니다. 자체 제작 모델이나 마스터즈의 정식 제공 자산으로 설명하지 마세요. 원작 게임 추출이라고 명시된 후보와 팬 제작 후보, 출처 미확인 후보는 각 `source.json`에 구분했습니다. 외부 유료 생성·유료 모델 구매·게시·게임 배포는 하지 않았습니다.

현재 34종 중 **15종 외형 검토 모델**이 있습니다. 아구몬, 그레이몬, 가루몬, 캅테리몬, 아트라캅테리몬, 메탈가루몬, 쿠가몬, 코로몬, 뿔몬, 모티몬, 어니몬, 버드라몬, 홀리엔젤몬, 세라피몬, 워그레이몬입니다. 나머지 19종은 미완료이며 새 갤러리에서 대기 목록으로 표시합니다. 버드라몬의 11개 클립과 쿠가몬의 1개 클립은 원본을 보존합니다. 다른 후보에는 모션이 없으므로 이전 272클립을 새 모델에 적용했다고 설명하지 마세요. 게임용 리깅·동작 이식·최종 원작 외형 검수·Unity 적용은 남아 있습니다.

새 미리보기는 루트 `Preview_Faithful_Digimon_3D.bat`와 `Tools/Open_Faithful_Gallery.ps1`입니다. `127.0.0.1:8766`에서 로컬 Three.js 갤러리를 열며, 라이브러리·GLB·참고 이미지를 모두 로컬에 보관합니다. 드래그 회전·확대·정측후면·모션 선택·원작 비교·출처 확인·이미지 저장을 제공합니다. 서버는 숨김 실행하고 브라우저는 실제 보이는 창으로 엽니다. 일반 게임은 기존 2D를 유지하며, 정식 UnityWindows 빌드는 변경하지 않았습니다.

공식 참고 이미지도 재검증했습니다. `ArtSource/Roster/ReferencesV2`에 **34종 모두**를 페이지의 실제 이미지와 연결해 저장했습니다. 그레이몬은 `greymon-first`, 메탈그레이몬은 `metalgreymon-v`, 워가루몬은 `weregarrumon`으로 수정했습니다. 이전 References의 다른 세대 그레이몬·메탈그레이몬 이미지를 계속 사용하지 마세요. 게임 참고와 정확한 원작 색·비율을 우선합니다.

원본 보존: `ArtSource/QualityBackup-20261001`은 이번 외형 실험 전 자산입니다. 사용자 거절 시점까지 일부만 재생성된 89개 파일은 `ArtSource/RejectedProcedural-20261001`에 보존한 뒤 이전 자산으로 되돌렸습니다. portable의 중간 모델 7개도 같은 폴더에 보존 후 이전 자산으로 맞췄습니다. 이전 3D 검토본과 새 GLB 검토본은 다른 상태입니다. 과거 `02_ArtVault`와 해시 기록은 새 후보로 덮어쓰지 않았습니다.

재현 도구: `prepare_faithful_main.py`, `prepare_faithful_creatures.py`, `prepare_faithful_small.py`, `build_faithful_manifest.py`, `serve_faithful_gallery.py`. GLB 구조·임베디드 자산·원본 보존·텍스처 보존 검사는 각 `optimize-report.json` 및 `FaithfulGallery/validation.json`을 확인합니다. 실제 브라우저 렌더·15종 전환·모션·UI 검사와 캡처는 `Builds/FaithfulGalleryValidation`입니다. 구조 검사와 외형 완성 판정을 구분합니다. 커밋·푸시는 하지 않았습니다.

현재 루트 `Preview_All_Digimon_3D.bat`도 새 브라우저 갤러리로 연결합니다. `Preview_Agumon_3D.bat`/`Models_Preview.bat`는 새 갤러리의 아구몬을 선택합니다. 이전 실행 방식은 `Preview_All_Digimon_Procedural_Draft.bat`와 `Models_Preview_Procedural_Draft.bat`에 보존했습니다. 아구몬은 외부 후보도 아직 원작과 주둥이·치아·팔 자세 차이가 있으며, `AgumonRefinement`의 머리 비례 수정 시안은 별도 보존했습니다. 완성 판정이나 사용자 승인으로 처리하지 마세요.

최종 확인: Edge 실제 창에서 워그레이몬 로드 후 보이는 창 핸들을 확인했고 로컬 서버는 실행 중입니다. `browser-full15.json`은 15종 전체 검사, `browser-seam-fixes.json`은 경계 수정 후 가루몬·캅테리몬·아트라 재검사이며 `browser-report.json`이 두 단계 통합 결과와 최종 15종 해시입니다. 런타임 오류와 외부 네트워크 요청은 0건입니다. 원본이 16비트 정점 한도로 나뉜 조각은 먼저 다시 연결하고 동일 위치의 경계 정점을 합쳐 감량하여 표면 틈을 해결했습니다. 이전 자산과 해시가 일치하는 보관소의 모델 보고서 22개도 중간 보고서를 보존한 뒤 복구했습니다.

남은 원작 파일 조사: 한국 마스터즈 공식 다운로드는 로그인으로 연결되고, 글로벌 페이지는 직접 요청 403, 공식 FAQ의 오래된 설치 파일은 HEAD 404였습니다. 클라이언트 다운로드·설치·실행이나 접근 우회는 하지 않았습니다. `ThirdPartyCandidates/official-dmo-client-download-review.json`을 확인합니다. 사용자에게 이미 보유한 게임 또는 모델 폴더 경로를 질문한 상태입니다. 파일을 받으면 아구몬 NIF/KF 한 종부터 텍스처·스켈레톤·모션 변환을 검증해야 하며 34종 일괄 성공을 가정하지 마세요. 무료 변환 도구 조사 결과는 `ArtSource/DmoConversionResearch/research.json`이며 Blender 3.6 보조 환경이 필요할 수 있습니다.

## 2026-10-01 전체 디지몬 제작 확대 — 과거 기록

사용자가 아구몬을 더 자연스럽게 다듬고 모든 캐릭터를 제작하도록 요청했습니다. 범위는 현재 게임 카탈로그의 플레이어 30종·적 4종, 총 **34종**입니다. 아구몬은 머리·주둥이의 각진 윤곽을 부드럽게 다시 만들었고, 추가 33종에 각각 Blender 원본·GLB·스킨 메쉬·8개 관절 모션을 생성했습니다. 전체 272개 클립이며 새 모델은 **제작 검토본**입니다. 원작과 동일한 최종 외형이나 모든 원작 기술 모션을 완성했다는 뜻이 아닙니다.

### 실행과 파일

- 루트 `Preview_All_Digimon_3D.bat`: 전체 선택 메뉴를 연 상태로 실행합니다. 오른쪽 위 캐릭터 선택 또는 PgUp/PgDn으로 34종을 바꿉니다. 모션·타임라인·회전·배속은 유지합니다. 체형별로 카메라 거리가 맞춰집니다.
- `ArtSource/Roster/<id>/<id>.blend`, `<id>.glb`: 새 33종의 편집 원본과 교환 파일. `preview.png`는 Blender 실제 렌더입니다.
- `Assets/Resources/Models/Roster/<id>.bytes`: 게임 스킨·관절·모션 데이터. 아구몬은 기존 `Models/Agumon/Agumon.bytes`입니다.
- `Builds/PortablePreview/RosterCaptures`: 캐릭터마다 대기·이동·공격·기술, 총 136장 실제 플레이어 캡처.
- `ArtSource/Roster/all-digimon-player-preview.jpg`: 전체 플레이어 캡처 모음. 보관소는 `02_ArtVault/Models/Roster`입니다.

### 제작과 참고 범위

`Tools/roster_designs.py`에 34종의 색·체형·특징을 정의하고 `Tools/author_roster.py`로 생성합니다. 유년기, 이족·공룡, 사족, 곤충, 조류, 식물, 인간형, 날개 몸통형, 쉘몬을 구분합니다. 뿔·꽃·날개 수·갑옷·꼬리·복부 무늬·발톱 등의 특징을 개별 지정했습니다. 연속 몸통 메쉬와 최대 4개 관절 가중치, 지상형 다리 IK, 사족의 앞뒤 발 교대, 곤충 다리와 날개, 조류의 날갯짓을 사용합니다. 피격·쓰러짐에는 부속물까지 포함한 지면 보정이 들어갑니다.

공식 도감 이미지 33개를 `ArtSource/Roster/References`에 모아 비교했고 URL은 `sources.json`에 있습니다. 워가루몬 공식 이미지 다운로드는 실패했으며 해당 참조의 세부 대조가 남습니다. 아구몬 모션은 아래 기록의 마스터즈 영상 프레임을 직접 참고했습니다. 다른 33종은 체형별 직접 제작 모션으로, 마스터즈의 각 종 영상을 모두 대조한 상태는 아닙니다. 외부 유료 생성이나 게임 자산 추출은 하지 않았습니다.

### 검사와 한계

최종 외형 보강에서는 12종을 재생성했습니다. 워그레이몬의 각진 얼굴 가면·붉은 갈기·금색 장갑·흉부와 다리 장갑, 그레이몬 계열의 이마·볼 투구, 천사형의 불투명 얼굴 가리개, 사람형의 손가락·신발, 꽃 요정 계열의 얼굴색을 구분했습니다. 이전 12종 원본과 생성 코드는 `ArtSource/Roster/Backups/20261001-before-armor-polish`에 보존합니다. 여전히 단순화한 제작 검토 모델이며 원작 수준의 세부 조형 완성은 아닙니다.

최종 구조 검사는 1,507,402개, Blender 메쉬 자세 검사는 62,200개가 통과했습니다. 검사한 1,360개 자세에서 지면 관통은 0건, 가장 낮은 경계는 모델 단위 +0.006704입니다. 실제 플레이어 최종 로그는 `Builds/PortablePreview/Roster-Final.log`이며 34종·27,396개 검사·136장 캡처를 통과했습니다. 아구몬 별도 회귀도 `Agumon-Final.log`의 1,030개 검사를 통과했습니다. 보관소의 추가 33종 165파일 및 아구몬 15파일을 동기화하고 엔진 자산과 portable 자산 해시 일치를 확인했습니다.

`validate_roster.py`는 게임 카탈로그와 제작 목록의 일치, 34개 바이너리·272개 클립·별도 GLB를 검사합니다. `validate_roster_motion.py`는 각 모션의 5개 시점(전체 1,360개 자세)에서 변형된 메쉬 경계를 검사합니다. 지면 허용 오차는 모델 단위 -0.005이며 전 구간의 연속 충돌 검사나 발 미끄러짐 전체 검증을 대신하지 않습니다. 실제 결과는 `ArtSource/Roster/validation-report.json`, `motion-bounds-report.json`, `Builds/RosterPoseValidation-20261001.log`를 따릅니다.

플레이어 `--roster-smoke`는 34종의 27,396개 관절 검사와 136개 캡처를 수행합니다. 최신 로그는 `Builds/PortablePreview/Roster-Final.log`입니다. 아구몬의 별도 눈·접지 검사는 유지합니다. C# 이동·타이밍 894개, 계산 비교 1,458개가 통과했습니다.

일반 플레이는 기존 2D 기본값이며 3D는 제작 뷰어에서 검토합니다. 새 모델은 단순화한 외형으로, 얼굴·털·깃털·갑옷의 디테일, 종별 원작 모션과 명중 타이밍, 손가락·표정, 체형별 전환 품질을 더 다듬어야 합니다. 정식 UnityWindows 실행본은 이전 상태이며 정식 임포트·셰이더·최적화 검증은 남습니다. `AuthoredModelBuild.Validate`에는 전체 로스터 검사도 연결했지만 현재 PC에서 Editor 실행은 하지 못했습니다. 기존 아트·이전 빌드·원본 백업은 보존하며 커밋·푸시는 하지 않았습니다.

```powershell
& '../tmp/blender/blender-4.5.9-windows-x64/blender.exe' --background --threads 6 --python-exit-code 1 --python Tools/author_roster.py -- --render
python Tools/validate_roster.py
& '../tmp/blender/blender-4.5.9-windows-x64/blender.exe' --background --python-exit-code 1 --python Tools/validate_roster_motion.py
python Tools/build_portable_preview.py
```

생성기는 해당 제작 자산을 덮어씁니다. Blender에서 수동 편집한 원본은 실행 전에 별도로 보존합니다. `--ids`로 특정 종만 재생성할 수 있습니다.

## 2026-10-01 후속 — 참고 영상 기반 모션 개선과 자산 반영

사용자가 아구몬 이미지와 디지몬 마스터즈의 실제 동작을 참고해 자연스럽게 만들도록 요청했습니다. 다음 공개 자료를 직접 비교했습니다.

- [공식 아구몬 이미지](https://digimon.net/reference/detail.php?directory_name=agumon): 머리·주둥이·녹색 눈·세 발톱 비율.
- [마스터즈 게임 화면](https://dmo.gameking.com/Community/ScreenshotView.aspx?idx=15010), [입 벌림 화면](https://www.digimonmasters.com/comm/sub_attach.aspx?Btype=Screen&o=777623).
- [Dark Games의 아구몬 시연 영상](https://www.youtube.com/watch?v=pNWiiSKslAI): 39–42초 이동, 42–45초 발톱 공격, 58–62초 불 뿜기를 연속 프레임으로 확인했습니다. 화면을 참고해 직접 동작을 조정했으며 게임 모델·애니메이션 데이터는 추출하지 않았습니다. 프레임 속도·모션을 그대로 복제한 작업은 아닙니다.

눈 고리 방향 수정을 실제 `.blend`/GLB/FBX/`.bytes`에 반영했습니다. 대기 호흡과 팔의 작은 움직임, 접지에서 발을 들어 올릴 때의 속도 연결, 부드러운 착지, 체중 이동, 머리의 반대 방향 움직임과 꼬리의 지연 반동을 조정했습니다. 다리가 닿지 못하는 보폭을 줄이고 골반을 내려 발 접지 오차를 개선했습니다. 공격은 준비 후 앞/아래로 내려긋고 회복하는 동작이며, 불 뿜기에는 꼬리 반동을 추가했습니다. 기존 모델 실루엣을 유지한 모션 개선입니다.

게임의 걷기 재생 주기는 실제 이동 거리와 성급 크기로 계산합니다. 새 공격 이벤트에서는 0.8초 클립을 0.35초 플래시 길이에 압축하지 않고 회복까지 이어 재생합니다. 실제 피해 발생 타이밍·게임 규칙은 변경하지 않았으므로 시각적 접촉과 피해 시점의 완전한 일치는 후속 작업입니다. Hit/Run/Turn의 게임 이벤트 연결도 아직 남아 있습니다.

검토 파일은 `ArtSource/Agumon/Agumon-motion-review-20261001.mp4`(20초, 8개 모션)와 `review-*.png`입니다. 루트 `Review_Agumon_Motion.bat`로 영상을 열고 `Preview_Agumon_3D.bat`로 최신 portable 뷰어를 실행합니다. 영상은 실제 Blender 관절 모션을 Workbench로 렌더링한 것으로 정식 Unity 셰이더 검증을 대신하지 않습니다. 기존 이름의 영상과 `01-...08-...png`는 이전 기록입니다.

검증: 눈 12개 레이어/2,880개 면 정상, 자산 구조 166,347개/60개 변형 자세/GLB 8클립 통과. 저장된 Blender 모션의 접지·반복 검사 1,173개 통과(모델 단위 최대 오차 Walk 0.002974, Run 0.006829). portable 관절 재생 1,030개, 18장 캡처 통과. C# 이동·타이밍 894개, 계산 비교 1,458개 통과. 로그는 `Builds/*20261001.log`와 `Builds/PortablePreview/ModelSmoke-20261001.log`입니다.

변경 전 원본은 `ArtSource/Agumon/Backups/20261001-before-motion`에 보존했습니다. 최신 자산은 portable과 `02_ArtVault/Models/Agumon`에 반영합니다. 정식 Unity Editor가 없는 현재 PC에서 `Builds/UnityWindows`는 재빌드하지 않았으며 이전 실행본입니다. 일반 플레이는 2D 기본값입니다. 정식 셰이더 시각 검증, 실제 입력 검증, 추가 외형·동작 품질 조율은 남습니다. 커밋·푸시는 하지 않았습니다.

추가 검사 명령: Blender `--background --python-exit-code 1 --python Tools/validate_agumon_motion.py`. 재생성 시 `Tools/author_agumon.py -- --no-render`로 자산·구조 검사까지만 수행한 뒤, 저장된 `.blend`에 `Tools/render_agumon_motion.py -- --quick`을 실행하면 검토 이미지를 갱신할 수 있습니다.

## 2026-10-01 눈 메쉬 원인 확인과 생성 코드 수정

`author_agumon.py`의 눈 중심 부채꼴과 바깥 고리 면 방향이 서로 반대였습니다. 기존 Blender 원본에서 눈 12개 레이어의 2,880개 면 중 2,304개 고리 면이 안쪽을 향하는 것을 확인했습니다. 정식 셰이더의 `Cull Back`에서 고리가 사라지고 중심만 남는 캡처와 일치합니다. 고리 정점 순서를 수정했으며 셰이더는 변경하지 않았습니다.

`Tools/validate_eye_winding.py`는 보존된 두개골 표면에서 실제 생성 함수를 실행해 12개 레이어·2,880개 면의 바깥 방향과 기존 정점 위치 보존을 검사합니다. Blender 4.5.9에서 통과했습니다. 자산을 저장하지 않는 검사입니다.

```powershell
& '../tmp/blender/blender-4.5.9-windows-x64/blender.exe' --background --threads 4 --python-exit-code 1 --python Tools/validate_eye_winding.py
```

이번 변경은 **생성 코드 수정까지**입니다. `.blend`/GLB/FBX/`.bytes`, 보관소 자산과 기존 실행본은 아직 이전 상태입니다. 현재 PC에서 이전 Unity Editor 경로가 없으므로 정식 셰이더 재빌드·시각 검증은 하지 못했습니다. 다음 작업은 원본을 별도 보존한 뒤 아래 재생성 절차로 자산을 갱신하고, 구조 검사와 정식 Unity 빌드·모델 캡처를 확인하는 것입니다. 그 뒤 portable과 보관소도 동기화합니다. 눈 표시 해결 완료로 판정하지 않습니다.

Git은 `develop`의 `6fa2f369e9c3f1ced97c76cb39def1f5d5f39644`이며 10월 1일 `ls-remote`로 GitHub `dittoches/develop`과 일치를 확인했습니다. 기존 9월 작업과 이번 변경은 미커밋 상태입니다. 현재 작업 경로는 `E:\Dittoches_KEEP_20260917\01_CurrentProject\DittochesMulti`입니다.

## 2026-09-29 최신 상태

기존 아구몬 자산과 실행 상태를 다시 확인하고 모션 뷰어·관절 재생을 보완했습니다. 이번에는 모델 외형이나 Blender/GLB/FBX 원본을 새로 생성하지 않았고 외부 유료 생성도 사용하지 않았습니다. 현재 프로젝트는 `G:\Dittoches_KEEP_20260917\01_CurrentProject\DittochesMulti`이며, 일반 플레이의 기본값은 계속 2D입니다.

- 8개 모션에 0.25/0.5/1배속, 재생 위치 슬라이더, 1/30초 앞뒤 이동, 반복/마지막 자세 유지, 일시정지 중 회전을 제공합니다. Space는 재생/정지, 방향키는 한 프레임 이동, Home은 처음으로 이동입니다.
- ‘연결 동작’에서는 실제 전투 재생 경로의 대기→걷기→정지→공격→불 뿜기→쓰러짐을 확인합니다. 재생 위치를 건너뛰면 앞부분을 다시 계산해 걷기 위상과 전환 자세가 일정하게 재현됩니다.
- 뷰어가 일반 Unity 빌드에서도 `--model-gallery`로 실행됩니다. `--model-smoke`는 모션 재생 검사와 캡처 후 종료합니다.
- 자산 구조 검사 **166,365개**, 기존 Blender 보고서의 변형 자세 범위 **60개**, GLB 애니메이션 **8개** 확인 통과. C# 이동/타이밍 **894개**, C#/서버 계산 비교 **1,458개** 통과.
- 실제 portable 플레이어에서 관절 재생 회귀 검사 **954개** 통과. 8개 클립·추가 시점 2개·전투 연결 동작 8개의 전장 화면 18장을 캡처했습니다. 로그는 `Builds/PortablePreview/ModelSmoke-20260929.log`, 화면은 같은 폴더의 `ModelCaptures`입니다. 타임라인 기능은 코드와 실행 검사로 확인했으며 실제 마우스·키보드 조작과 전체 조작 화면의 육안 검증은 남았습니다.
- **정식 Unity 임포트·두 셰이더·Windows Mono 빌드가 통과했습니다.** `E:\6000.6.0f1\Editor\Unity.exe`에서 관절 재생 954개와 자산/셰이더 검사를 거쳐 `Builds/UnityWindows`에 빌드했습니다. 최종 빌드 크기는 119,225,634바이트이며 로그는 `Builds/UnityBuild-20260929.log`입니다. 정식 플레이어도 관절 재생 954개·전장 캡처 18장을 완료하고 정상 종료했습니다. 로그는 `Builds/UnityWindows/ModelSmoke-20260929.log`입니다. 기존 `Builds/Windows`와 portable 실행본은 보존합니다.
- **정식 모델의 시각 검증은 미완료입니다.** `CharacterToon.shader`의 외곽선 깊이 기록과 Gamma 색 변환을 보정해 검은 외곽선에 가려지는 문제와 어두운 색을 개선했으나, 최종 렌더에서도 눈 표면이 보이지 않습니다. 원인은 아직 확정하지 않았으며 다음 재개 시 최우선 확인 항목입니다. 눈이 정상인 portable 뷰어를 `Preview_Agumon_3D.bat`의 기본값으로 유지합니다.
- 이번 일반 2D UI 재검사는 숨김 실행에서 OnGUI 갱신이 발생하지 않아 완료하지 못했습니다(`Builds/PortablePreview/ArenaSmoke-20260929.log`: repaints 0, actors 0). 과거 838개 통과는 9월 18일 결과이며 이번 변경의 UI 재검증으로 주장하지 않습니다.

사용자가 현재까지 마무리하고 중단하도록 요청했습니다. 진행 중이던 최종 빌드·모델 검사와 기록만 마쳤으며 남은 실행 프로세스는 없습니다. 후속 수정은 재개 요청을 받은 뒤 진행합니다.

남은 핵심은 실제 이동 거리와 걷기 주기·발 미끄러짐을 맞추는 일입니다. 기본 공격은 기존 게임 타이머 0.35초에 0.8초 Attack 클립을 압축해 재생하므로 명중 시점과 동작 속도를 다시 조율해야 합니다. Hit/Run/Turn은 뷰어에서 확인 가능한 클립이며 게임의 피격·달리기·회전 이벤트에 연결한 상태는 아닙니다. 이번 검사 통과가 모델 외형의 최종 승인이나 전체 3D 완성을 의미하지 않습니다.

정식 빌드 명령과 실행 절차는 [HOME_HANDOFF.md](HOME_HANDOFF.md)를 따릅니다. `AuthoredModelBuild.Build`는 자산·실제 관절 재생·두 셰이더의 컴파일 오류를 확인한 뒤 별도 경로에 빌드하며 제품 설정은 변경하지 않습니다. 현재 소스와 문서는 로컬 변경이며 이번 세션에서 커밋·푸시하지 않았습니다. `CURRENT_HANDOFF.json`과 `FILE_HASHES.json`은 과거 기록 그대로 유지합니다.

## 2026-09-19 모델 제작 기록

사용자가 3D 제작 재개를 요청했고, Fal 유료 생성 대신 무료 Blender 직접 제작을 선택했습니다. 디지몬 마스터즈와 디지몬 RPG의 모습을 참고하라는 요청에 따라 공개 화면을 조사했습니다. 외형의 직접 비교에는 아래 마스터즈 게임 화면과 공식 아구몬 그림을 사용했습니다. 다른 게임의 모델·애니메이션 파일을 추출한 것은 아닙니다.

- [공식 아구몬 도감](https://digimon.net/reference/detail.php?directory_name=agumon)
- [마스터즈 아구몬 플레이 화면](https://dmo.gameking.com/Community/ScreenshotView.aspx?idx=15010)
- [마스터즈 아구몬 얼굴·입 벌림 비교](https://www.digimonmasters.com/comm/sub_attach.aspx?Btype=Screen&o=777623)
- [디지몬 RPG 공식 게임 안내](https://dro.gameking.com/Aboutgame/gameguide/index.html)

이전 도형 조립 시험본에서 연결된 몸통·팔다리·꼬리, 별도 스컬프한 머리·주둥이·볼, 머리 표면에 밀착한 눈, 회전하는 아래턱으로 교체했습니다. 중간 렌더에 대한 사용자의 피드백을 반영해 긴 목, 돌출된 눈, 과한 눈썹 볼륨과 얼룩진 명암을 수정했습니다. 원작과 완전히 동일한 모델이라는 판정은 하지 않습니다. 현재 제작 범위는 아구몬 한 종입니다.

## 확인과 편집

- 보존 폴더 루트 `Preview_Agumon_3D.bat`: `Models_Preview.bat`를 통해 눈이 정상인 portable 회전·8개 모션·연결 동작 뷰어를 실행합니다. 정식 모델은 `Builds/UnityWindows/DittochesMulti.exe --model-gallery`로 별도 확인합니다.
- 루트 `Play_Unity_Windows.bat`: 정식 Windows 일반 게임 실행. 일반 플레이는 2D 기본값입니다.
- 루트 `Open_Agumon_Blender.bat`: 휴대용 Blender로 편집 원본 열기.
- `ArtSource/Agumon/Agumon.blend`: 셀 명암 재질, 스킨 가중치, 19개 관절, 8개 Action이 있는 원본.
- 같은 폴더 `Agumon.fbx`, `Agumon.glb`: 일반 3D 도구용 내보내기. GLB/FBX의 무광 재질은 Blender 셀 재질 노드와 다릅니다.
- 같은 폴더 `01-three-quarter.png`부터 `08-defeat.png`: 실제 Blender 렌더.
- 같은 폴더 `Agumon-motion-review.mp4`: 원본 관절 Action을 재생한 검토 영상.
- `Assets/Resources/Models/Agumon/Agumon.bytes`: 엔진용 메쉬·가중치·관절·모션 데이터.
- `Builds/PortablePreview/ModelCaptures`: 실제 게임 플레이어의 클립·시점·연결 동작 전장 캡처.

19개 관절, 26,892개 정점, 53,072개 삼각형입니다. 양손·양발 각각 세 발톱이며 머리와 눈은 Head, 아래턱·혀·아래쪽 치아는 Jaw 관절에 연결합니다. 걷기에는 발 위치를 지정하는 두 관절 IK를 사용하고 엔진에는 계산된 관절 모션을 저장합니다.

| 모션 | 길이 | 재생 |
|---|---:|---|
| Idle | 2.4초 | 반복 |
| Walk | 0.9초 | 반복 |
| Run | 0.6초 | 반복 |
| Attack | 0.8초 | 1회 |
| PepperBreath | 0.95초 | 1회 |
| Hit | 0.55초 | 1회 |
| Defeat | 1.2초 | 마지막 자세 유지 |
| Turn | 1.8초 | 방향 살피기 반복 |

모션 뷰어는 위 모션을 모두 확인할 수 있습니다. 전투용 포즈 연결에는 대기·걷기·공격·기술·사망 경로와 0.12초 전환이 들어 있습니다. 달리기·피격·방향 살피기는 검토용 클립이며 일반 게임 이벤트에 모두 연결했다는 뜻은 아닙니다. 일반 플레이는 기존 2D 기본값을 유지하고 모델 뷰어에서 3D를 사용합니다.

## 재생성

프로젝트 폴더에서 실행합니다. 재생성은 현재 산출물을 덮어쓰므로 Blender에서 수동 편집한 버전은 별도 이름으로 먼저 보관합니다.

```powershell
& '../tmp/blender/blender-4.5.9-windows-x64/blender.exe' --background --threads 8 --python Tools/author_agumon.py
& '../tmp/blender/blender-4.5.9-windows-x64/4.5/python/bin/python.exe' Tools/validate_authored_model.py
& '../tmp/blender/blender-4.5.9-windows-x64/4.5/python/bin/python.exe' Tools/build_portable_preview.py
& '../tmp/blender/blender-4.5.9-windows-x64/blender.exe' --background ArtSource/Agumon/Agumon.blend --threads 8 --python Tools/render_agumon_motion.py
```

Blender 4.5.9 LTS는 공식 배포 ZIP의 SHA-256을 확인한 휴대용 버전입니다. 외부 모델 생성은 실행하지 않았습니다. 이 PC의 실제 Unity 플레이어는 RTX 5060 Laptop GPU와 약 7.9GB VRAM을 보고했습니다.

## 2026-09-19 검증과 남은 일

- 정점·인덱스·스킨 가중치·관절 회전·반복 모션 경계 검사 통과.
- 독립 GLB 파일에 19개 관절과 8개 별도 애니메이션이 있는지 검사 통과.
- Blender에서 실제 변형된 메쉬의 60개 자세 범위를 검사. 쓰러짐 중간의 바닥 관통을 수정했습니다.
- 일반 C#과 휴대용 C# 컴파일 통과, 기존 이동/타이밍 894개와 계산 비교 1,458개 검사 통과. 이번 변경은 서버 규칙을 바꾸지 않습니다.
- 실제 모델 뷰어에서 8개 모션 및 추가 시점 2개 캡처 완료, 실행 중 예외 없음.
- 휴대용 플레이어는 새 셰이더를 임포트하지 못하므로 기본 자세의 명암을 정점 색으로 표시합니다. 새 `CharacterToon.shader`의 정식 Unity 임포트·컴파일과 정식 Windows 빌드는 별도 검증이 필요합니다.
- 눈꺼풀/눈 감기, 손가락 개별 관절, 공격 명중 순간의 추가 연출, 많은 유닛을 동시에 표시하는 최적화와 다른 디지몬 제작은 남아 있습니다.

새 모델·Blender 원본·영상은 Git 제외 대상입니다. `02_ArtVault/Models/Agumon`에 별도 보관하며, 소스 생성 스크립트·런타임 로더·문서는 코드 변경으로 남습니다. 루트 `CURRENT_HANDOFF.json`은 9월 18일 저장 기록으로 이번 모델을 포함하지 않습니다.
