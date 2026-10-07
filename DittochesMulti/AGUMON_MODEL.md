# 아구몬 Blender 모델과 모션 — 2026-09-29

## 2026-10-06 실제 모델 전체 게임 연결 — 최신

현재 검토된 GLB 34종/352클립을 FDM1으로 변환해 Unity 전투에 연결했습니다. `Play_Preview.bat`는 FaithfulPreview를 실행하며 기존 절차형 모델 검토기는 별도로 남습니다. 포즈/속도 전환, 이동 위상, 종별 기술 발사 시점과 관절 위치, 투사체 출발점 고정을 적용했습니다. 실제 Unity 스키닝 전체·14유닛 전투·양쪽 리플레이 검사는 NEXT_SESSION 최상단을 따릅니다. 새 정식 셰이더의 Editor 빌드와 전체 UI 재검사는 남아 있습니다. 최신 모델 제작·메탈그레이몬 백업은 아래 기록을 그대로 유지합니다.

## 2026-10-06 메탈그레이몬 어깨 보완 — 최신

메탈그레이몬 메시의 흉곽/상완 두께와 어깨 관절 간격(0.86→1.24), 높이를 보강했습니다. Down 접지 보정 후 10동작 변형·GLB·실제 WebGL·기술 표시 검증을 통과하고 갤러리에 반영했습니다. 다음 기술 저작은 이 종에 한해 `MetalGreymonShoulderApproved-20261006`을 자동 선택합니다. 다른 종은 기존 기준입니다. 수정 전 백업과 전후 이미지, 재실행 주의사항은 NEXT_SESSION 최상단을 따르세요. Unity 미통합입니다.

## 2026-10-06 캐릭터 표면·기술 표현 개선 — 최신

34종의 표면 재질/조명과 공격/기술 표현을 개선했습니다. 피부·털·껍질·금속의 마감, 불기둥·얼음 결정·물보라·전기·구체·문·미사일·깃털·덩굴의 형태를 구분합니다. 발사체는 실제 발사 자세에서 출발점을 고정해 손/입 반동과 독립적으로 이동합니다. 모델 체형·리그·GLB 클립은 보존하며 이번 범위는 갤러리 렌더링입니다. 상세·검사·전후 캡처는 `NEXT_SESSION.md` 최상단과 `Builds/QualityPass-20261006`입니다.

## 2026-10-06 갤러리 모션 연결 보완

기존 34종/352클립과 모델 원본을 유지하며 동작 전환을 보완했습니다. 현재 표시 자세에서 0.22초 연결, 전환 중 정지, 빠른 연속 선택, 자동 시연 연결과 정확한 프레임 이동을 처리합니다. 변경 전 코드에서는 그레이몬 일시정지 후 메시 정점이 움직이는 문제가 재현됐습니다. 코드·검사·현재 PC 경로는 `NEXT_SESSION.md` 최상단, 검사 자료는 `Builds/MotionConnectionValidation-20261006`에 있습니다. 기존 기술 저작 입력은 계속 TechniqueMotionBackup-20261002이며 Unity 게임에는 미통합입니다.

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

| 디지몬 ID | 일반 공격 | 특수 공격 |
|---|---|---|
| koromon | 몸통 박치기 | 거품 공격 |
| tsunomon | 뿔 들이받기 | 산성 거품 |
| mochimon | 탄성 몸통치기 | 탄성 거품 |
| tanemon | 뿌리 박치기 | 접착 거품 |
| pyocomon | 꽃 흔들기 | 비누꽃 |
| tokomon | 짧은 몸통치기 | 물어뜯기 |
| agumon | 발톱 베기 | 베이비 플레임 |
| gabumon | 발톱 휘두르기 | 파란 불꽃 |
| tentomon | 앞발 연타 | 쁘띠 썬더 |
| palmon | 덩굴 후려치기 | 포이즌 아이비 |
| piyomon | 부리 쪼기 | 매지컬 파이어 |
| patamon | 윙 슬랩 | 에어 샷 |
| togemon | 좌우 펀치 | 치쿠치쿠 뱅뱅 |
| garurumon | 송곳니 덮치기 | 폭스 파이어 |
| greymon | 뿔과 발톱 밀어치기 | 메가 플레임 |
| kabuterimon | 네 팔 내려치기 | 메가 블래스터 |
| angemon | 홀리 로드 타격 | 헤븐즈 너클 |
| birdramon | 발톱 급강하 | 메테오 윙 |
| metalgreymon | 트라이던트 암 | 기가 디스트로이어 |
| weregarurumon | 권투 연타 | 카이저 네일 |
| lilimon | 꽃잎 손날 | 플라워 캐논 |
| holyangemon | 엑스칼리버 베기 | 헤븐즈 게이트 |
| atlur | 앞다리와 뿔 타격 | 혼 버스터 |
| garudamon | 갈고리 손톱 베기 | 섀도 윙 |
| herakle | 집게와 네 팔 타격 | 기가 블래스터 |
| hououmon | 황금 날개 쓸기 | 스타라이트 익스플로전 |
| wargreymon | 드라몬 킬러 베기 | 가이아 포스 |
| metalgarurumon | 기계 발톱 타격 | 코큐토스 브레스 |
| rosemon | 가시 채찍 후려치기 | 쏜 위프 |
| seraphimon | 빛의 구체 | 세븐 헤븐즈 |
| kuwagamon | 네 팔 할퀴기 | 시저 암즈 |
| shellmon | 앞발 밀어치기 | 하이드로 프레셔 |
| devimon | 어둠의 손날 | 데스 클로 |
| etemon | 러브 세레나데 | 다크 스피리츠 |


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

## 2026-10-02 그레이몬 자세·전체 자연스러운 모션 보완 — 가장 최신

사용자 요청 “그레이몬 자세 수정, 전체 모션 자연스럽게”를 반영했습니다. **34종의 공통 10동작 340개를 갱신**했으며 버드라몬 11개·쿠가몬 1개 원본 동작은 보존해 총 352클립입니다. 새로 종이나 클립 수를 늘린 작업이 아닙니다. 아래의 모션 확장 기록은 이번 수정 전 상태입니다.

- 그레이몬: 한 팔을 올리고 다리를 벌린 조각 원본 자세를 대기로 사용하던 문제를 수정했습니다. 골반과 몸통의 좌우 기울기, 양팔과 발 간격·높이, 손 방향, 턱과 꼬리를 다시 정렬한 중립 자세로 재바인딩했습니다. 잘못 Spine에 묶인 혀 944정점을 Jaw로 연결하고 발목 접힘을 국소 보정했습니다. 기존 메시 면수·UV·재질을 유지했습니다. `stance-report.json`에 전후 관절 좌표와 적용 내용을 기록했습니다.
- 전체 모션: 무거운 이족형·작은 이족형·인간형·유년기·사족형·비행형·곤충형·셸몬으로 속도와 보폭을 구분했습니다. 지지하는 발은 골반 움직임과 분리하고, 스윙 궤적과 발끝 회전을 연결했습니다. 사족형은 걷기의 네 박자와 달리기의 다리 위상을 구분했습니다. 유년기는 눌림과 도약, 날개·귀·꼬리는 지연 반응을 적용했습니다.
- 팔·다리 관절: 실제 부모 자세의 좌표계에서 회전하고 모델 고유의 무릎·팔꿈치 굽힘 면을 유지합니다. 일반적인 고정 방향을 사용하면서 관절이 갑자기 뒤집히던 문제를 줄였습니다. 공격은 준비·타격·회복의 시간을 구분하며, 한 번 재생하는 동작은 중립 자세로 진입·복귀합니다. Down은 다리 길이에 맞게 굽히고 균형을 잃을 때 접지를 풀며 발목 방향도 연속적으로 전환합니다.
- 버드라몬: 기존 공격/승리 동작의 급격한 날개 전환을 그대로 이어 쓰지 않고 비행 사이클·선택 자세에 몸통·머리·회복·선회·착지를 다시 구성했습니다. Run의 날갯짓을 과속시키던 중복 사이클을 제거했습니다. 쿠가몬은 원래 관절에 네 팔의 지연 동작과 발 접지를 적용했습니다. 두 종의 원본 12클립 정의·원본 BIN 바이트·메시·스킨·재질은 보존했습니다.

검증 완료: 34종/352클립 GLB 구조·길이·반복 끝점·가중치·기존 면수/텍스처 보존 검사, 공통 340클립의 13포즈 표면 늘어짐·바닥 침투 검사, 32종 Blender의 모든 프레임 관절 이동·중립 진입/복귀 검사, 원래 관절을 쓰는 2종의 내보낸 모든 프레임 전환 검사가 통과했습니다. 전체 352클립의 실제 Edge WebGL 정점 이동·타임라인·일회 재생/반복·Down 유지와 반영 SHA-256 일치를 확인했습니다. 34종의 대기/걷기/공격/특수 공격/쓰러짐 선택 프레임을 7장으로 검토했습니다. 이는 표본/선택 자세 검증이며 원작 모션의 완벽한 재현이나 모든 교차 관통 검증을 뜻하지 않습니다.

재생: `http://127.0.0.1:8766/?motion=Walk#greymon` 또는 `Tools/Open_Faithful_Gallery.ps1 -Model greymon -Motion Walk`. `수정 전 보기`는 이번 수정 직전 352클립과 같은 동작·정규화 시점으로 비교합니다. `전체 시연`과 `Models_Demo.bat`는 34종을 순회합니다. 이번 변경은 갤러리 자산이며 Unity 게임 적용은 아직 하지 않았습니다.

직전 상태·도구·검사 자료는 **`ArtSource/NaturalMotionBackup-20261002`**에 보존했습니다. `model-hashes.json`이 백업 기준입니다. `faithful_motion_catalog.BASELINE`과 `--reuse-current` 입력은 이 백업으로 갱신했습니다. 그레이몬의 자세 교정을 이미 수정된 Blender에 다시 적용하지 마세요. 편집본은 `ArtSource/AnimatedReview`, 반영본은 `ArtSource/FaithfulGallery`, 전후 비교는 `FaithfulGallery/previous`입니다. 더 이전의 MotionExpansionBackup/FullRosterBackup/FacePassBackup/NaturalPassBackup/AnimationBackup은 보존합니다.

주요 코드: `faithful_pose_math.py`, `faithful_greymon_stance.py`, `faithful_motion_styles.py`, `faithful_natural_motion.py`, `faithful_native_motion.py`, `animate_faithful_models.py`, `extend_native_motion.py`. 결과는 `Builds/FaithfulMotionValidation/published-report.json`, `review-naturalfinalfirst/rest/fix`, `Builds/FaithfulNaturalValidation/glb-report.json`, `native-transition-report.json`, `motion-sheet-01.png`~`07.png`, 각 종의 `deformation-report.json`에 기록했습니다. 아구몬의 이전 얼굴 보정·출처·원본 모델은 유지했습니다. 코드·문서는 ce22d6e 위의 미커밋이고 자산·백업·검사 파일은 Git 제외 USB 자료입니다.

추가 확인: 반영본에서 6개 동작의 수정 전후 비교와 정규화 시점·정지 상태 복원을 검사했습니다. Builds/FaithfulNaturalValidation/natural-motion-report.json에 34종/340개 공통/352개 전체와 전환 검사를 모았습니다. greymon-stance-before-after.png와 greymon-walk-before-after.gif는 실제 갤러리의 수정 전후 캡처이며, Builds/FaithfulFullRosterValidation/all-34-species.png도 갱신했습니다.

## 2026-10-02 모션 확장 — 가장 최신

**실제 메시 34종 모두 공통 10동작, 원본 모션 포함 총 352클립을 갤러리에 반영했습니다.** 직전 204클립에서 148개가 추가됐습니다. 아래 204클립과 6동작 기록은 이번 확장 전 이력입니다. Unity 게임 적용은 아직이며 일반 플레이는 기존 2D입니다.

공통 동작은 Idle(대기), Walk(걷기), Run(달리기), Attack(공격), Skill(특수 공격), Guard(방어), Dodge(회피), Hit(피격), Victory(승리), Down(쓰러짐)입니다. 기존 32종은 6동작에서 10동작으로 확장했습니다. 쿠가몬은 원래 관절·스킨에 공통 10동작을 새로 붙이고 원본 1클립을 보존했습니다. 버드라몬은 기존 동작을 조정하고 회피·넘어짐을 만들어 공통 10동작을 추가했으며 원본 11클립도 보존했습니다. 352개를 전부 게임에서 추출한 원본 모션이라고 설명하지 않습니다.

- 32종의 검토된 메시·가중치와 아구몬 얼굴 보정을 유지했습니다. 파피몬·가루다몬의 방어 팔 각도는 연결 메시가 늘어나지 않도록 줄였습니다.
- 로제몬의 원래 팔 관절에는 IK 목표를 사용해 방어 시 손을 올리고 특수 공격 시 앞으로 뻗도록 보완했습니다. 버드라몬의 새 Down은 바닥에 닿는 넘어짐입니다.
- 갤러리에 공통 10동작 바로 선택, 전체 자동 시연, 다음 캐릭터 이동을 추가했습니다. Down은 마지막 자세를 유지합니다. 수정 전 파일에 없는 새 동작은 대기 자세로 비교하고, 돌아오면 원래 동작·시간·정지를 복원합니다.
- 재생: `http://127.0.0.1:8766/?demo=1#agumon`, 프로젝트의 `Models_Demo.bat`, 또는 `Tools/Open_Faithful_Gallery.ps1 -Model agumon -Demo`. 기존 `Models_Preview.bat`도 유효합니다.

검증 결과: 34종/352클립 GLB 구조·타이밍·루프 검사와 모든 클립의 실제 WebGL 정점 이동·재생 검사가 통과했습니다. 공통 동작 340개의 클립당 13포즈에서 늘어짐·바닥 침투를 검사했고, 아구몬 얼굴의 Head 상대 변형도 검사했습니다. 반영 파일의 SHA-256은 검증한 후보와 모두 같습니다. 원본 12클립과 버드라몬·쿠가몬의 원본 메시·스킨·텍스처 BIN 바이트는 그대로 보존됐습니다. 표본 검사이며 원작 동작·세부 외형·모든 교차 관통의 완벽한 재현을 뜻하지 않습니다.

추가 UI 검사도 통과했습니다: 자동 시연 시작·걷기로 자동 전환·정지/재개·다음 캐릭터·수동 선택 시 시연 중지·좁은 화면 표시, 새 특수 공격의 수정 전 대기 비교와 원래 포즈 복원입니다. `Builds/FaithfulCombatValidation/demo-report.json`과 `FaithfulNaturalValidation/comparison-report.json`에 기록했습니다.

이번 수정 전 34종/204클립과 도구·검사 자료는 **`ArtSource/MotionExpansionBackup-20261002`**에 보존했습니다. `model-hashes.json`이 백업 기준입니다. 이전 FullRosterBackup/FacePassBackup/NaturalPassBackup/AnimationBackup도 덮어쓰지 않습니다. 현재 편집본은 `AnimatedReview`, 반영본은 `FaithfulGallery`이며 34종 모두 수정 전 비교 파일이 있습니다.

주요 코드: `faithful_motion_catalog.py`(10클립과 기준 백업), `faithful_combat_poses.py`(새 동작), `animate_faithful_models.py --reuse-current`(32종), `extend_native_motion.py`(버드라몬·쿠가몬), `faithful_gallery/gallery.js`(재생·시연). **`--reuse-current`는 실행 시점의 후보가 아니라 MotionExpansionBackup의 확장 전 Blender를 읽습니다.** 다음 수동 편집을 재생성으로 덮기 전에 새 백업·입력 기준을 검토하세요. 원래 파일을 정적 파일로 덮는 prepare 도구나 재질 전용 도구를 현재 갤러리에 다시 실행하지 않습니다.

검사 자료: `Builds/FaithfulMotionValidation/published-report.json`(34종/352클립과 해시), `Builds/FaithfulNaturalValidation/glb-report.json`, 각 후보의 `deformation-report.json`, `review-combatfirst`, `review-combatrest`, `review-collapse`, `review-rosegaze`. `Builds/FaithfulFullRosterValidation/all-34-species.png`은 갱신된 전체 미리보기입니다. 새 변경은 `ce22d6e` 위의 미커밋이며 자산·백업·보고서는 Git 제외 USB 자료입니다. 외부 공개·유료 생성·Git 업로드는 하지 않았습니다.

## 2026-10-02 전체 갱신 — 가장 최신

사용자 요청으로 **기존 15종을 모두 갱신하고 미제작 19종을 추가해 실제 메시 34종·204클립·미준비 0종**을 갤러리에 반영했습니다. 아래의 15종/90클립·19종 미준비 기록은 과거 이력입니다. 도형 조립식 구형 34종을 재사용하지 않았습니다. 일반 게임은 기존 2D이며, 이번 34종의 Unity 게임 적용은 아직 하지 않았습니다.

- 기존 13종: 아구몬 얼굴 보정을 유지하고 달리기 팔 동작, 공격 체중 이동·반동, 피격 회복, 승리 제스처와 접지를 보완했습니다.
- 신규 18종: 원작 게임에서 유래한 공개 Workshop OBJ와 원본 텍스처를 보존해 관절과 기본 6동작을 붙였습니다. 텐타몬 손 관절, 가루다몬 몸 중심, 엔젤몬 지팡이 양 끝·6개 날개·발목 연결을 검사 후 수정했습니다.
- 로제몬: 공개 Workshop 배포본을 공식 SteamCMD 익명 다운로드로 확보했습니다. SourceIO로 원본 관절·스킨·얼굴 변형을 보존해 정상 형태를 변환하고 6동작을 추가했습니다. 보유 게임 폴더는 사용하지 않았습니다.
- 버드라몬: 투명 깃털 경계를 유지하면서 몸과 겹친 깃털의 깊이 표시를 수정했습니다. 쿠가몬: 원본 텍스처에 곱해지던 회색과 과도한 광택을 보완했습니다. 두 종의 메시·텍스처·스킨·기존 모션 BIN 데이터는 원본과 바이트 단위로 같습니다.
- 204클립 구성: 32종 × 신규/수정 6클립 = 192개 + 버드라몬 기존 11개 + 쿠가몬 기존 1개입니다. 모두 게임 원본 모션이라고 설명하지 않습니다.

검증: 32종/192클립 GLB 구조·가중치·시간·루프 검사, 32종의 클립당 13포즈 실제 변형·늘어짐·접지 검사, 전체 34종/204클립 Edge WebGL 정점 변형·재생·타임라인 검사가 통과했습니다. 반영된 GLB 해시가 검사한 후보와 모두 같으며, 34개 선택 항목·미준비 0개·로제몬 한글 검색과 공식 참고 이미지 로딩, 아구몬/워그레이몬/홀리엔젤몬의 공격 자세 전후 비교도 확인했습니다. 표본 검사이므로 모든 자세의 원작 재현도나 모든 교차 관통까지 완벽하다고 주장하지 않습니다.

현재 갤러리: `http://127.0.0.1:8766/` 또는 `Models_Preview.bat` / USB 루트 `Preview_Faithful_Digimon_3D.bat`. 기존 15종에는 이번 전체 갱신 직전 모습과 비교하는 버튼이 있습니다. 전체 미리보기 이미지는 `Builds/FaithfulFullRosterValidation/all-34-species.png`입니다.

보존 위치:
- `ArtSource/FullRosterBackup-20261002`: 이번 전체 갱신 직전 15종과 도구·검증 자료. 이전 FacePassBackup/NaturalPassBackup/AnimationBackup도 보존합니다.
- `ArtSource/ThirdPartyCandidates/SteamDigimonModels-2101623444`: 원본 Workshop 목록, 18종 OBJ·텍스처, URL·SHA-256·출처.
- `ArtSource/ThirdPartyCandidates/RosemonWorkshop-2451134533`: 공개 GMA 원본, 데이터 파일, 원본 Blender 가져오기, 추출 검증. 애드온 게임 스크립트는 실행하지 않았습니다.
- `ArtSource/ExpandedSources-20261002`: 신규 19종 정규화 원본. 로제몬의 원본 관절 포함 Blender도 여기 있습니다.
- `ArtSource/AnimatedReview`: 32종의 편집 가능한 Blender와 후보 GLB, 원본 모션 2종의 재질 수정 후보. `ArtSource/FaithfulGallery`: 반영본 34종.
- `Builds/FaithfulMotionValidation/published-report.json`: 34종 최종 해시·204클립 검사 결과. `Builds/FaithfulNaturalValidation/glb-report.json`: 32종/192클립 구조 검사. 각 후보 폴더에 변형 검사 결과가 있습니다.

출처: 새 OBJ 묶음은 https://steamcommunity.com/sharedfiles/filedetails/?id=2101623444 (Digital Dray), 로제몬은 https://steamcommunity.com/sharedfiles/filedetails/?id=2451134533 (BANDAI NAMCO 원본 게임, Debiddo 변환, Impmon 배포)입니다. 직접 창작 모델이나 공식 제공 자산으로 표현하지 않습니다. 출처·이용 조건과 원본 파일을 보존하며 이번 작업에서 외부 공개·배포·Git 업로드는 하지 않았습니다.

다음 변경 시 새 백업을 만든 뒤 개별 외형·동작을 다듬습니다. 기존 13종의 `--reuse-bind`는 여전히 NaturalPassBackup 입력입니다. 신규 OBJ 18종은 `--reuse-bind` 없이 생성하고 로제몬은 native 경로로 원본 관절을 읽습니다. `faithful_rig_profiles.py`는 기존 13종 + `faithful_expanded_profiles.py` 신규 19종입니다. 재질만 갱신한 버드라몬/쿠가몬은 기존 애니메이션을 재샘플링하지 않습니다. 재생성한 종은 GLB·변형·브라우저 검사를 다시 통과시킨 뒤 `promote_faithful_motion.py --ids ...`로 반영합니다. 반영 도구는 후보 SHA와 일치하는 검사만 허용합니다.

현재 경로는 G 드라이브입니다. 이전 E 경로의 그레이몬 원본은 프로젝트 상대 위치로 찾고 원본 SHA를 확인합니다. 기본 Python 3.12의 브라우저 검사는 `tmp/gallery-test-tools-py312`의 greenlet을 먼저 불러옵니다. 의존성은 Blender 4.5.9, Pillow, 기존 Playwright/Edge이며 로제몬 원본 재가져오기에는 `01_CurrentProject/tmp/SourceIO`가 필요합니다. 실제 자산·백업·검증 결과는 Git 제외이므로 USB를 함께 보존해야 합니다. 현재 HEAD `ce22d6e` 위의 변경은 미커밋입니다.

## 2026-10-02 아구몬 얼굴·손 후속 수정 — 현재 상태

어제 저장한 `develop`의 `ce22d6e`에서 재개했습니다. 현재 경로는 `G:\Dittoches_KEEP_20260917\01_CurrentProject\DittochesMulti`입니다. 아구몬의 주둥이 전체 폭과 앞면 곡률을 보완하고, 눈과 주변 피부를 함께 앞쪽으로 돌렸습니다. 팔꿈치를 몸 쪽으로 모으고 손목을 안쪽으로 돌렸습니다. 공개 팬 모델의 원작 재현도를 개선하는 검토본이며 최종 외형 승인을 받은 상태는 아닙니다.

검토 과정에서 볼·눈 주변에 팔/상체 관절 가중치가 남아 얼굴이 끌리는 문제를 확인했습니다. `faithful_sculpt.py`에서 목과 아래턱의 연결을 부드럽게 유지하면서 위 얼굴·눈·눈썹을 Head에 연결했습니다. 혀의 턱 움직임은 유지하고 정점당 최대 4개 가중치를 정규화합니다. `animate_faithful_models.py`의 새 바인딩에서도 얼굴을 팔 영역으로 분류하지 않도록 수정했습니다.

검증은 아구몬 6클립의 실제 Blender 표면 변형·바닥 관통·반복 연결과 상부 얼굴의 Head 상대 변형, 실제 Edge의 WebGL 재생·타임라인·공격 후 대기 복귀를 확인했습니다. 13종/78클립 GLB 구조·시간·루프·가중치·삼각형 및 텍스처 수 검사도 통과했습니다. 새 얼굴 검사는 어제 모델에서 실패하고 수정본에서 통과합니다(상부 얼굴 최대 상대 오차: 이전 약 0.33048, 현재 약 0.000000414 모델 단위; 클립별 13자세 표본). 검사 실패 시 종료 코드도 실패를 반환하도록 보완했습니다.

`promote_faithful_motion.py --ids agumon`으로 검증된 아구몬만 반영했습니다. 다른 14종 GLB의 SHA-256은 수정 전과 같습니다. 비교 버튼의 아구몬 **수정 전**은 10월 1일 최종본이며, **수정 후**는 이번 수정본입니다. 아구몬·워그레이몬·홀리엔젤몬 3종의 같은 시점 비교와 복원 검사도 통과했습니다. 갤러리는 **15종/90클립**, 미준비 **19종**, Unity 게임 미적용 상태입니다.

- 수정 전 전체 보존: `ArtSource/FacePassBackup-20261002` (`review`, `gallery`, `tools`, 기존 검사 결과·문서). 기존 NaturalPassBackup은 그대로입니다.
- 편집본·내보내기·최신 렌더: `ArtSource/AnimatedReview/agumon`. 실제 갤러리: `ArtSource/FaithfulGallery/agumon`.
- 검사 결과: `Builds/FaithfulMotionValidation/review-face20261002/browser-report.json`, `published-report.json`, `Builds/FaithfulNaturalValidation/glb-report.json`, `comparison-report.json`.
- 이번 전후·보존·이전 모델 회귀 증거: `Builds/AgumonFaceReview-20261002`. 후보 중간 캡처보다 `FaithfulNaturalValidation/agumon-after.png`가 최종 반영 화면입니다.

모델·백업·렌더·보고서는 Git 제외 대상이며 USB 로컬에 저장했습니다. 코드와 문서는 이번 세션에서 커밋/푸시하지 않았습니다. 다음은 치아·입술선·눈 표현과 손가락 세부 조형, 종별 걷기 접지의 추가 육안 검토입니다. 이후 워그레이몬·천사형과 미준비 19종을 이어갑니다. 이번 결과를 전체 3D 완성이나 정식 게임 적용으로 설명하지 마세요.

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
