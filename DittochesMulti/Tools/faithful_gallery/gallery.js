import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { TechniqueEffects } from './technique-effects.js';
import { PoseTransition } from './pose-transition.js';
import { finishCharacter } from './character-finish.js';
import { RoomEnvironment } from 'three/addons/environments/RoomEnvironment.js';

const $ = (id) => document.getElementById(id);
const manifestURL = new URL('/ArtSource/FaithfulGallery/manifest.json', location.href);
const state = { entries: [], pending: [], scope: 34, selected: null, loading: false, ready: false, errors: [], clips: [], meshCount: 0, triangles: 0, frames: 0 };
let renderer, scene, camera, controls, current, mixer, activeAction, clips = [], sequence = 0;
let currentBox = new THREE.Box3(), filtered = [], floor, keyLight, clock = new THREE.Clock();
let motionIndex = 0;
const commonMotions = ['Idle', 'Walk', 'Run', 'Attack', 'Skill', 'Guard', 'Dodge', 'Hit', 'Victory', 'Down'];
const motionLabels = { Idle: '대기', Walk: '걷기', Run: '달리기', Attack: '공격', Skill: '특수 공격', Guard: '방어', Dodge: '회피', Hit: '피격', Victory: '승리', Down: '쓰러짐' };
state.demo = { motions: commonMotions, active: false, waiting: false, model: 0, motion: 0, elapsed: 0, ids: [] };
let comparisonPose = null, techniqueEffects;
let poseTransition = null, pendingIdle = null;
const loader = new GLTFLoader();

function plain(value, fallback = '—') {
  if (value === undefined || value === null || value === '') return fallback;
  if (Array.isArray(value)) return value.map((v) => plain(v, '')).filter(Boolean).join('\n');
  if (typeof value === 'object') return value.fullName || value.label || value.name || value.displayName || JSON.stringify(value);
  return String(value);
}

function localURL(path) {
  const url = new URL(path, manifestURL);
  if (url.origin !== location.origin) throw new Error('모델과 참고 이미지는 로컬 파일이어야 합니다.');
  return url.href;
}

function safeExternal(value) {
  try {
    const url = new URL(typeof value === 'object' ? value.url : value);
    return ['http:', 'https:'].includes(url.protocol) ? url.href : null;
  } catch { return null; }
}

function error(message, cause) {
  $('loading').hidden = true;
  $('error-box').hidden = false;
  $('error-box').textContent = message;
  state.errors.push(cause ? String(cause.message || cause) : message);
  state.loading = false;
  state.ready = false;
  if (cause) console.error(cause);
}

function busy(message) {
  $('loading').hidden = false;
  $('loading-text').textContent = message;
  $('error-box').hidden = true;
  state.loading = true;
  state.ready = false;
}

function setupScene() {
  renderer = new THREE.WebGLRenderer({ canvas: $('canvas'), antialias: true, alpha: true, preserveDrawingBuffer: true });
  renderer.setPixelRatio(Math.min(devicePixelRatio, 2));
  renderer.outputColorSpace = THREE.SRGBColorSpace;
  renderer.toneMapping = THREE.ACESFilmicToneMapping;
  renderer.toneMappingExposure = .95;
  renderer.shadowMap.enabled = true;
  renderer.shadowMap.type = THREE.PCFSoftShadowMap;
  scene = new THREE.Scene();
  // Render particle transparency against the same backdrop as the studio.
  scene.background = new THREE.Color(0xe8edef);
  techniqueEffects = new TechniqueEffects(scene);
  camera = new THREE.PerspectiveCamera(36, 1, .01, 100);
  camera.position.set(3, 2, 6);
  controls = new OrbitControls(camera, renderer.domElement);
  controls.enableDamping = true;
  controls.dampingFactor = .07;
  controls.autoRotateSpeed = .65;
  controls.maxPolarAngle = Math.PI * .51;
  controls.target.set(0, 1.2, 0);
  const pmrem = new THREE.PMREMGenerator(renderer);
  const room = new RoomEnvironment();
  scene.environment = pmrem.fromScene(room, .04).texture;
  scene.environmentIntensity = .48;
  room.dispose();
  pmrem.dispose();
  scene.add(new THREE.HemisphereLight(0xf7fbff, 0x879096, .62));
  keyLight = new THREE.DirectionalLight(0xfff6e9, 1.6);
  keyLight.position.set(4, 7, 5);
  keyLight.castShadow = true;
  keyLight.shadow.mapSize.set(2048, 2048);
  keyLight.shadow.camera.left = -4;
  keyLight.shadow.camera.right = 4;
  keyLight.shadow.camera.top = 5;
  keyLight.shadow.camera.bottom = -4;
  keyLight.shadow.camera.near = .1;
  keyLight.shadow.camera.far = 20;
  keyLight.shadow.normalBias = .035;
  keyLight.shadow.bias = -.00012;
  keyLight.shadow.radius = 3;
  scene.add(keyLight);
  const fill = new THREE.DirectionalLight(0xd7e7fa, .4);
  fill.position.set(-5, 3, 2);
  scene.add(fill);
  const rim = new THREE.DirectionalLight(0xe3f3ff, 1.05);
  rim.position.set(-1, 5, -4);
  scene.add(rim);
  floor = new THREE.Mesh(new THREE.PlaneGeometry(200, 200), new THREE.ShadowMaterial({ color: 0x566978, opacity: .17 }));
  floor.rotation.x = -Math.PI / 2;
  floor.position.y = -.005;
  floor.receiveShadow = true;
  scene.add(floor);
  const ring = new THREE.Mesh(new THREE.RingGeometry(1.64, 1.649, 100), new THREE.MeshBasicMaterial({ color: 0xcfd9dd, transparent: true, opacity: .6, side: THREE.DoubleSide }));
  ring.rotation.x = -Math.PI / 2;
  ring.position.y = -.004;
  scene.add(ring);
  const resize = () => {
    const rect = $('viewport').getBoundingClientRect();
    renderer.setSize(Math.max(1, rect.width), Math.max(1, rect.height), false);
    camera.aspect = Math.max(1, rect.width) / Math.max(1, rect.height);
    camera.updateProjectionMatrix();
  };
  new ResizeObserver(resize).observe($('viewport'));
  resize();
  renderer.setAnimationLoop(() => {
    const rawDelta = clock.getDelta();
    const dt = document.hidden || rawDelta > .5 ? 0 : Math.min(rawDelta, .1);
    if (mixer && state.ready) {
      const playing = !activeAction?.paused;
      poseTransition?.beforeUpdate();
      mixer.update(playing ? dt : 0);
      poseTransition?.apply(playing ? dt * mixer.timeScale : 0);
      poseTransition?.record(playing ? dt * mixer.timeScale : 0);
      // Do not change the mixer's active actions inside its finished callback.
      if (pendingIdle !== null) {
        const idle = pendingIdle;
        pendingIdle = null;
        playClip(idle);
      }
    }
    updateDemo(dt);
    if (activeAction) {
      state.animationTime = activeAction.time;
      state.animationPaused = activeAction.paused;
      state.animationDuration = activeAction.getClip().duration;
      $('animation-seek').value = String(activeAction.time / state.animationDuration || 0);
      $('animation-time').textContent = `${activeAction.time.toFixed(2)} / ${state.animationDuration.toFixed(2)}초`;
    }
    updateTechniqueEffects();
    controls.update();
    renderer.render(scene, camera);
    state.frames += 1;
  });
}

function disposeObject(object) {
  if (!object) return;
  const geometries = new Set(), materials = new Set(), textures = new Set();
  object.traverse((node) => {
    if (node.geometry) geometries.add(node.geometry);
    for (const material of Array.isArray(node.material) ? node.material : [node.material]) {
      if (!material) continue;
      materials.add(material);
      for (const value of Object.values(material)) if (value?.isTexture) textures.add(value);
    }
    if (node.skeleton) node.skeleton.dispose();
  });
  geometries.forEach((value) => value.dispose());
  materials.forEach((value) => value.dispose());
  textures.forEach((value) => { value.dispose(); value.source?.data?.close?.(); });
  object.removeFromParent();
}

function frame(view = 'three-quarter') {
  if (!current || currentBox.isEmpty()) return;
  const center = currentBox.getCenter(new THREE.Vector3());
  const size = currentBox.getSize(new THREE.Vector3());
  const radius = currentBox.getBoundingSphere(new THREE.Sphere()).radius;
  const verticalFov = THREE.MathUtils.degToRad(camera.fov);
  const horizontalFov = 2 * Math.atan(Math.tan(verticalFov / 2) * camera.aspect);
  const distance = radius / Math.sin(Math.min(verticalFov, horizontalFov) / 2) * 1.12;
  const target = center.clone();
  const entry = state.entries.find((item) => item.id === state.selected);
  const angle = THREE.MathUtils.degToRad(Number(entry?.frontAngle || 0));
  let direction;
  if (view === 'front') direction = new THREE.Vector3(0, .025, 1);
  else if (view === 'back') direction = new THREE.Vector3(0, .025, -1);
  else if (view === 'side') direction = new THREE.Vector3(1, .025, 0);
  else direction = Array.isArray(entry?.cameraDirection) ? new THREE.Vector3(...entry.cameraDirection) : new THREE.Vector3(.38, .17, 1);
  if (!(view === 'three-quarter' && Array.isArray(entry?.cameraDirection))) direction.applyAxisAngle(new THREE.Vector3(0, 1, 0), angle);
  direction.normalize();
  camera.near = Math.max(.005, radius / 150);
  camera.far = Math.max(50, distance * 12);
  camera.position.copy(target).addScaledVector(direction, distance);
  camera.updateProjectionMatrix();
  controls.target.copy(target);
  controls.minDistance = radius * .22;
  controls.maxDistance = radius * 15;
  controls.update();
  state.bounds = { width: size.x, height: size.y, depth: size.z };
}

function renderList() {
  const needle = $('search').value.trim().toLocaleLowerCase();
  filtered = state.entries.filter((entry) => (plain(entry.name, '') + ' ' + entry.id).toLocaleLowerCase().includes(needle));
  $('model-list').replaceChildren();
  for (const entry of filtered) {
    const button = document.createElement('button');
    button.className = 'model-item' + (entry.id === state.selected ? ' active' : '');
    button.dataset.id = entry.id;
    button.setAttribute('aria-pressed', String(entry.id === state.selected));
    const thumbnail = entry.thumbnail || entry.reference;
    if (thumbnail) {
      const image = document.createElement('img');
      image.src = localURL(thumbnail);
      image.alt = '';
      image.loading = 'lazy';
      image.addEventListener('error', () => image.hidden = true, { once: true });
      button.append(image);
    }
    const label = document.createElement('span');
    const title = document.createElement('strong');
    title.textContent = plain(entry.name, entry.id);
    const subtitle = document.createElement('small');
    subtitle.textContent = entry.model ? entry.id.toUpperCase() : '모델 준비 중';
    label.append(title, subtitle);
    button.append(label);
    button.addEventListener('click', () => selectModel(entry.id));
    $('model-list').append(button);
  }
  const pending = state.pending.filter((entry) => (plain(entry.name, '') + ' ' + entry.id).toLocaleLowerCase().includes(needle));
  if (pending.length) {
    const section = document.createElement('section');
    section.className = 'pending-section';
    const title = document.createElement('h2');
    title.textContent = `모델 준비 중 · ${pending.length}종`;
    section.append(title);
    for (const entry of pending) {
      const item = document.createElement('div');
      item.className = 'pending-item';
      const name = document.createElement('span');
      name.textContent = plain(entry.name, entry.id);
      const label = document.createElement('small');
      label.textContent = '준비 중';
      item.append(name, label);
      section.append(item);
    }
    $('model-list').append(section);
  }
  $('list-empty').hidden = filtered.length > 0 || pending.length > 0;
}

function showTechniques(entry) {
  const data = entry?.techniques;
  $('technique-card').hidden = !data;
  if (!data) return;
  for (const mode of ['Attack', 'Skill']) {
    const row = $('technique-' + mode.toLowerCase());
    row.querySelector('strong').textContent = data[mode].name;
    row.setAttribute('aria-current', String(state.activeClip === mode));
  }
  const active = data[state.activeClip] || data.Skill;
  $('technique-description').textContent = state.showPrevious ? '수정 전 공격 동작입니다.' : active.description;
  $('technique-source').href = safeExternal(data.source);
}

function showMetadata(entry) {
  showTechniques(entry);
  $('model-name').textContent = plain(entry.name, entry.id);
  $('model-subtitle').textContent = state.showPrevious ? '수정 전 모습 · 같은 시점과 각도에서 비교하고 있습니다.' : '아래에서 모션을 바꾸고, 드래그하여 자세를 돌려보세요.';
  $('compare-before').hidden = !entry.previousModel;
  $('compare-before').textContent = state.showPrevious ? '수정 후 보기' : '수정 전 보기';
  $('compare-before').setAttribute('aria-pressed', String(!!state.showPrevious));
  const index = state.entries.findIndex((item) => item.id === entry.id);
  $('model-number').textContent = `${String(index + 1).padStart(2, '0')} / ${String(state.entries.length).padStart(2, '0')}`;
  $('source-author').textContent = plain(entry.author);
  $('source-license').textContent = plain(entry.license);
  $('source-notes').textContent = plain(entry.notes, '');
  const source = safeExternal(entry.source);
  $('source-link').hidden = !source;
  if (source) $('source-link').href = source;
  $('reference-image').hidden = !entry.reference;
  $('reference-missing').hidden = !!entry.reference;
  $('reference-link').removeAttribute('href');
  if (entry.reference) {
    const url = localURL(entry.reference);
    $('reference-image').src = url;
    $('reference-image').alt = `${plain(entry.name, entry.id)} 공식 원작 참고 이미지`;
    $('reference-link').href = url;
  } else $('reference-image').removeAttribute('src');
}

function setAnimations(gltf) {
  poseTransition = null;
  pendingIdle = null;
  clips = [...(gltf.animations || [])].sort((a, b) => {
    const rank = (clip) => commonMotions.includes(clip.name) ? commonMotions.indexOf(clip.name) : 100;
    return rank(a) - rank(b);
  });
  state.clips = clips.map((clip) => clip.name);
  $('animation-select').replaceChildren();
  $('animation-select').disabled = clips.length === 0;
  $('animation-play').disabled = clips.length === 0;
  $('animation-speed').disabled = clips.length === 0;
  for (const id of ['animation-seek', 'frame-back', 'frame-next', 'animation-repeat']) $(id).disabled = clips.length === 0;
  activeAction = null;
  if (clips.length === 0) {
    $('animation-select').add(new Option('모션 준비 중', ''));
    $('animation-note').textContent = '이 모델에는 재생 가능한 모션이 아직 없습니다.';
    $('animation-play').textContent = '재생';
    mixer = null;
    return;
  }
  mixer = new THREE.AnimationMixer(gltf.scene);
  poseTransition = new PoseTransition(gltf.scene);
  mixer.addEventListener('finished', ({ action }) => {
    if (action !== activeAction) return;
    if (state.demo.active || /^down$/i.test(action.getClip().name)) {
      $('animation-play').textContent = '다시 재생';
      return;
    }
    const idle = clips.findIndex((clip) => /idle|stand|wait/i.test(clip.name));
    if (idle >= 0 && idle !== motionIndex) pendingIdle = idle;
    else $('animation-play').textContent = '다시 재생';
  });
  mixer.timeScale = Number($('animation-speed').value);
  const labels = { idle: '대기', walk: '걷기', run: '달리기', attack: '공격', skill: '특수 공격', dodge: '회피', hit: '피격', victory: '승리', move: '이동', damage: '피격', guard: '방어', win: '승리', eat: '먹기', down: '쓰러짐', getup: '일어나기', attack01: '공격 1', attack02: '공격 2', special01: '특수 공격', 'take 001': '기본 동작' };
  clips.forEach((clip, i) => $('animation-select').add(new Option((labels[clip.name.toLowerCase()] || clip.name || `모션 ${i + 1}`) + (commonMotions.includes(clip.name) ? '' : ' · 원본'), String(i))));
  $('animation-note').textContent = `${clips.length}개 모션`;
  renderMotionButtons();
  const requested = new URLSearchParams(location.search).get('motion')?.toLowerCase();
  let preferred = requested ? clips.findIndex((clip) => clip.name.toLowerCase() === requested || requested === 'walk' && clip.name.toLowerCase() === 'move') : -1;
  if (preferred < 0) preferred = Math.max(0, clips.findIndex((clip) => /idle|stand|wait/i.test(clip.name)));
  $('animation-select').value = String(preferred);
  playClip(preferred);
}

function playClip(index, fade = true) {
  if (!mixer || !clips[index]) return;
  const source = activeAction && fade ? poseTransition.capture() : null;
  const velocity = activeAction && !activeAction.paused && fade ? poseTransition.captureVelocity() : null;
  const previousClip = activeAction?.getClip();
  const locomotion = /^(Walk|Run)$/;
  const phase = fade && previousClip && locomotion.test(previousClip.name) && locomotion.test(clips[index].name)
    ? (activeAction.time / previousClip.duration) % 1 : 0;
  poseTransition.clear();
  poseTransition.resetHistory();
  pendingIdle = null;
  mixer.stopAllAction();
  const next = mixer.clipAction(clips[index]);
  const repeat = $('animation-repeat').checked || /^(idle|walk|run|move|stand|wait|take 001)$/i.test(clips[index].name);
  next.reset().setEffectiveWeight(1).setEffectiveTimeScale(1);
  next.setLoop(repeat ? THREE.LoopRepeat : THREE.LoopOnce, repeat ? Infinity : 1);
  next.clampWhenFinished = true;
  next.play();
  next.time = phase * clips[index].duration;
  activeAction = next;
  mixer.update(0);
  const fadeSeconds = { Hit: .09, Dodge: .12, Attack: .16, Skill: .22, Down: .18, Idle: .28, Walk: .24, Run: .24 }[clips[index].name] || .22;
  if (source) poseTransition.begin(source, Math.min(fadeSeconds, clips[index].duration*.2), velocity);
  motionIndex = index;
  $('animation-select').value = String(index);
  $('animation-play').textContent = '일시 정지';
  state.activeClip = clips[index].name;
  showTechniques(state.entries.find((item) => item.id === state.selected));
  state.animationDuration = clips[index].duration;
  state.animationTime = next.time;
  state.animationPaused = false;
  for (const button of $('motion-buttons').children) button.setAttribute('aria-pressed', String(button.dataset.motion === state.activeClip));
}

function renderMotionButtons() {
  $('motion-buttons').replaceChildren();
  for (const name of commonMotions) {
    const index = clips.findIndex((clip) => clip.name === name);
    if (index < 0) continue;
    const button = document.createElement('button');
    button.textContent = motionLabels[name];
    button.dataset.motion = name;
    button.title = state.entries.find((item) => item.id === state.selected)?.techniques?.[name]?.name || motionLabels[name];
    button.setAttribute('aria-pressed', 'false');
    button.addEventListener('click', () => { stopDemo(); playClip(index); });
    $('motion-buttons').append(button);
  }
}

function stopDemo() {
  state.demo.active = false;
  state.demo.waiting = false;
  $('combat-demo').textContent = '기술 시연';
  $('combat-demo').setAttribute('aria-pressed', 'false');
  $('demo-toggle').textContent = '전체 시연';
  $('demo-toggle').setAttribute('aria-pressed', 'false');
  $('demo-next').hidden = true;
  $('demo-status').textContent = '34종 · 종마다 10가지 동작';
}

function playDemoMotion() {
  const demo = state.demo, name = demo.motions[demo.motion];
  const index = clips.findIndex((clip) => clip.name === name);
  if (index < 0) { stopDemo(); return; }
  demo.elapsed = 0;
  demo.waiting = false;
  playClip(index);
  $('demo-status').textContent = `${demo.model + 1} / ${demo.ids.length}종 · ${motionLabels[name]} ${demo.motion + 1} / ${demo.motions.length}`;
}

async function startDemo(combat = false) {
  if (!state.ready) return;
  stopDemo();
  const ids = state.entries.filter((entry) => entry.model).map((entry) => entry.id);
  const initial = Math.max(0, ids.indexOf(state.selected));
  Object.assign(state.demo, { motions: combat ? ['Attack', 'Skill'] : commonMotions, active: true, waiting: true, model: 0, motion: 0, elapsed: 0, ids: [...ids.slice(initial), ...ids.slice(0, initial)] });
  $('animation-repeat').checked = false;
  $('combat-demo').textContent = combat ? '기술 시연 중지' : '기술 시연';
  $('combat-demo').setAttribute('aria-pressed', String(combat));
  $('demo-toggle').textContent = '시연 중지';
  $('demo-toggle').setAttribute('aria-pressed', 'true');
  $('demo-next').hidden = false;
  if (state.showPrevious) {
    state.showPrevious = false;
    await selectModel(state.selected, true);
  }
  if (state.demo.active && state.ready) playDemoMotion();
}

async function advanceDemo(nextModel = false) {
  const demo = state.demo;
  if (!demo.active || demo.waiting) return;
  demo.waiting = true;
  demo.motion = nextModel ? demo.motions.length : demo.motion + 1;
  if (demo.motion >= demo.motions.length) {
    demo.motion = 0;
    demo.model++;
    if (demo.model >= demo.ids.length) {
      stopDemo();
      $('demo-status').textContent = `${demo.ids.length}종 · ${demo.ids.length * demo.motions.length}가지 동작 시연 완료`;
      return;
    }
    await selectModel(demo.ids[demo.model], true);
  }
  if (demo.active && state.ready) playDemoMotion();
  else stopDemo();
}

function updateDemo(dt) {
  const demo = state.demo;
  if (!demo.active || demo.waiting || !state.ready || !activeAction) return;
  // Pausing a motion also pauses the demonstration timer.
  if (activeAction.paused && activeAction.time < activeAction.getClip().duration - .001) return;
  demo.elapsed += dt * Number($('animation-speed').value);
  const duration = activeAction.getClip().duration;
  const hold = state.activeClip === 'Down' ? .8 : .35;
  if (demo.elapsed >= (state.activeClip === 'Idle' ? 1.6 : duration) + hold) void advanceDemo();
}

function updateTechniqueEffects() {
  state.techniqueEffect = techniqueEffects.update(state.activeClip, activeAction?.time || 0, activeAction?.getClip().duration || 1, $('technique-effects').checked, state.showPrevious || !state.ready);
}

function seekMotion(seconds) {
  if (!mixer || !activeAction) return;
  poseTransition.clear();
  poseTransition.resetHistory();
  pendingIdle = null;
  mixer.stopAllAction();
  activeAction.reset().setEffectiveWeight(1).play();
  activeAction.time = THREE.MathUtils.clamp(seconds, 0, activeAction.getClip().duration);
  activeAction.paused = true;
  mixer.update(0);
  state.animationTime = activeAction.time;
  state.animationPaused = true;
  updateTechniqueEffects();
  $('animation-play').textContent = '재생';
}

async function selectModel(id, automated = false) {
  if (!automated) stopDemo();
  const entry = state.entries.find((candidate) => candidate.id === id);
  if (!entry) return;
  if (state.selected !== id) comparisonPose = null;
  if (state.selected !== id || !entry.previousModel) state.showPrevious = false;
  techniqueEffects?.clear();
  const token = ++sequence;
  state.selected = id;
  state.clips = [];
  state.meshCount = 0;
  state.triangles = 0;
  renderList();
  showMetadata(entry);
  history.replaceState(null, '', '#' + encodeURIComponent(id));
  if (!entry.model) {
    if (mixer) mixer.stopAllAction();
    disposeObject(current);
    current = null;
    mixer = null;
    setAnimations({ animations: [] });
    error('이 캐릭터의 모델은 준비 중입니다. 다른 캐릭터를 선택해 주세요.');
    return;
  }
  busy(`${plain(entry.name, id)} 모델을 불러오고 있습니다.`);
  try {
    const modelPath = state.showPrevious && entry.previousModel ? entry.previousModel : entry.model;
    const gltf = await loader.loadAsync(localURL(modelPath), (progress) => {
      if (token !== sequence || !progress.total) return;
      $('loading-text').textContent = `${plain(entry.name, id)} 불러오는 중 · ${Math.round(progress.loaded / progress.total * 100)}%`;
    });
    if (token !== sequence) { disposeObject(gltf.scene); return; }
    if (mixer) { mixer.stopAllAction(); mixer.uncacheRoot(mixer.getRoot()); }
    disposeObject(current);
    current = new THREE.Group();
    current.add(gltf.scene);
    current.updateMatrixWorld(true);
    const box = new THREE.Box3().setFromObject(current, true);
    const size = box.getSize(new THREE.Vector3());
    if (box.isEmpty() || !Number.isFinite(size.length()) || size.length() < 1e-8) {
      disposeObject(current);
      current = null;
      throw new Error('빈 모델이거나 정상적인 크기를 계산할 수 없습니다.');
    }
    const scale = 2.8 / Math.max(size.y, size.x * .75, size.z * .75);
    current.scale.setScalar(scale);
    const center = box.getCenter(new THREE.Vector3());
    current.position.set(-center.x * scale, -box.min.y * scale, -center.z * scale);
    if (Array.isArray(entry.rotation)) current.rotation.set(...entry.rotation.map(THREE.MathUtils.degToRad));
    current.traverse((node) => {
      if (!node.isMesh) return;
      node.castShadow = true;
      // Original game meshes often contain overlapping shells.  Ground contact
      // shadows remain, but self-shadowing creates false speckles on those shells.
      node.receiveShadow = false;
      node.frustumCulled = false;
      state.meshCount++;
      state.triangles += node.geometry.index ? node.geometry.index.count / 3 : node.geometry.attributes.position.count / 3;
      for (const material of Array.isArray(node.material) ? node.material : [node.material]) {
        if (material) material.wireframe = $('wireframe').checked;
      }
    });
    state.surfaceFinish = state.showPrevious ? null : finishCharacter(current, renderer, entry.id);
    scene.add(current);
    current.updateMatrixWorld(true);
    currentBox.setFromObject(current, true);
    if (Math.abs(currentBox.min.y) > .001) {
      current.position.y -= currentBox.min.y;
      current.updateMatrixWorld(true);
      currentBox.setFromObject(current, true);
    }
    // Reserve headroom for the raised hands/energy sphere and the tall portal.
    if (entry.id === 'wargreymon') currentBox.max.y += .95 * scale;
    if (entry.id === 'holyangemon') currentBox.max.y += .35 * scale;
    frame();
    setAnimations(gltf);
    mixer?.update(0);
    techniqueEffects.bind(current, entry, clips, gltf.scene);
    $('loading').hidden = true;
    state.loading = false;
    state.ready = true;
    state.selected = id;
    document.title = `${plain(entry.name, id)} · 디지몬 3D 갤러리`;
  } catch (cause) {
    if (token !== sequence) return;
    error('모델을 불러오지 못했습니다.\n새로고침하거나 다른 캐릭터를 선택해 주세요.\n' + plain(cause.message, ''), cause);
  }
}

async function loadManifest() {
  busy('캐릭터 목록을 불러오고 있습니다.');
  try {
    const response = await fetch(manifestURL, { cache: 'no-store' });
    if (!response.ok) throw new Error(`모델 목록 파일을 읽을 수 없습니다 (${response.status}).`);
    const manifest = await response.json();
    const entries = Array.isArray(manifest) ? manifest : manifest.entries || manifest.models || manifest.characters || [];
    state.entries = entries.filter((entry) => entry && typeof entry.id === 'string');
    state.pending = Array.isArray(manifest.pending) ? manifest.pending : [];
    state.scope = Number(manifest.scope) || state.entries.length + state.pending.length;
    if (!state.entries.length) throw new Error('아직 갤러리에 등록된 모델이 없습니다.');
    const available = state.entries.filter((entry) => entry.model).length;
    const unfinished = Math.max(state.pending.length, state.scope - available);
    const motionCount = state.entries.reduce((sum, entry) => sum + (entry.animations?.length || 0), 0);
    $('total-label').textContent = unfinished
      ? `${available} / ${state.scope}종 · ${unfinished}종 준비 중`
      : `${available} / ${state.scope}종 · ${motionCount}개 모션`;
    const brandNote = document.querySelector('.brand small');
    brandNote.textContent = plain(manifest.status, '공개 외부 모델 · 원작 외형 비교 중');
    renderList();
    const requested = decodeURIComponent(location.hash.slice(1));
    const initial = state.entries.find((entry) => entry.id === requested && entry.model)
      || state.entries.find((entry) => entry.id === state.selected && entry.model)
      || state.entries.find((entry) => entry.model) || state.entries[0];
    await selectModel(initial.id);
    const demo = new URLSearchParams(location.search).get('demo');
    if (demo === '1' || demo === 'skills') await startDemo(demo === 'skills');
  } catch (cause) {
    error('갤러리 모델 목록이 준비되지 않았습니다.\n모델 준비가 끝나면 오른쪽 위의 새로고침을 눌러 주세요.\n' + plain(cause.message, ''), cause);
  }
}

$('search').addEventListener('input', renderList);
$('reload').addEventListener('click', loadManifest);
$('reference-image').addEventListener('error', () => {
  $('reference-image').hidden = true;
  $('reference-missing').hidden = false;
});
$('reset-camera').addEventListener('click', () => frame());
$('compare-before').addEventListener('click', async () => {
  if (!current || state.loading) return;
  stopDemo();
  const id = state.selected;
  if (!state.showPrevious) comparisonPose = { name: state.activeClip, progress: activeAction ? activeAction.time / activeAction.getClip().duration : 0, paused: activeAction?.paused };
  const { name, progress, paused } = comparisonPose || { name: state.activeClip, progress: 0, paused: true };
  const position = camera.position.clone(), target = controls.target.clone();
  state.showPrevious = !state.showPrevious;
  await selectModel(id);
  if (!state.ready || state.selected !== id) return;
  const index = clips.findIndex((clip) => clip.name === name);
  if (index >= 0) {
    playClip(index, false);
    seekMotion(progress * activeAction.getClip().duration);
    activeAction.paused = !!paused;
    $('animation-play').textContent = paused ? '재생' : '일시 정지';
  } else {
    const idle = Math.max(0, clips.findIndex((clip) => /^idle$/i.test(clip.name)));
    playClip(idle, false);
    seekMotion(0);
    $('model-subtitle').textContent = '수정 전에는 이 모션이 없어 대기 자세로 비교합니다.';
  }
  camera.position.copy(position);
  controls.target.copy(target);
  controls.update();
});
$('front-view').addEventListener('click', () => frame('front'));
$('side-view').addEventListener('click', () => frame('side'));
$('back-view').addEventListener('click', () => frame('back'));
$('auto-rotate').addEventListener('change', () => controls.autoRotate = $('auto-rotate').checked);
$('wireframe').addEventListener('change', () => current?.traverse((node) => {
  if (!node.isMesh) return;
  for (const material of Array.isArray(node.material) ? node.material : [node.material]) {
    if (material) material.wireframe = $('wireframe').checked;
  }
}));
$('combat-demo').addEventListener('click', () => state.demo.active && state.demo.motions.length === 2 ? stopDemo() : startDemo(true));
$('demo-toggle').addEventListener('click', () => state.demo.active ? stopDemo() : startDemo());
$('demo-next').addEventListener('click', () => advanceDemo(true));
$('animation-select').addEventListener('change', () => { stopDemo(); playClip(Number($('animation-select').value)); });
$('animation-speed').addEventListener('change', () => { if (mixer) mixer.timeScale = Number($('animation-speed').value); });
$('animation-repeat').addEventListener('change', () => { stopDemo(); playClip(motionIndex, false); });
$('animation-seek').addEventListener('input', () => { stopDemo(); seekMotion(Number($('animation-seek').value) * (activeAction?.getClip().duration || 0)); });
$('frame-back').addEventListener('click', () => { stopDemo(); seekMotion((activeAction?.time || 0) - 1 / 30); });
$('frame-next').addEventListener('click', () => { stopDemo(); seekMotion((activeAction?.time || 0) + 1 / 30); });
$('animation-play').addEventListener('click', () => {
  if (!activeAction) return;
  if (activeAction.time >= activeAction.getClip().duration - .00001) { playClip(motionIndex, false); return; }
  activeAction.paused = !activeAction.paused;
  $('animation-play').textContent = activeAction.paused ? '재생' : '일시 정지';
});
$('screenshot').addEventListener('click', () => {
  if (!renderer || !state.ready) return;
  renderer.render(scene, camera);
  renderer.domElement.toBlob((blob) => {
    if (!blob) return;
    const url = URL.createObjectURL(blob), link = document.createElement('a');
    link.download = `${state.selected}${state.showPrevious ? '-before' : ''}-3d.png`;
    link.href = url;
    link.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }, 'image/png');
});
window.addEventListener('keydown', (event) => {
  if (['INPUT', 'TEXTAREA', 'SELECT'].includes(event.target.tagName)) return;
  if (!['ArrowUp', 'ArrowDown', 'PageUp', 'PageDown'].includes(event.key) || !filtered.length) return;
  event.preventDefault();
  const delta = ['ArrowDown', 'PageDown'].includes(event.key) ? 1 : -1;
  const index = filtered.findIndex((entry) => entry.id === state.selected);
  selectModel(filtered[(index + delta + filtered.length) % filtered.length].id);
});
function poseSnapshot() {
  const positions = [];
  let bones = 0;
  current?.updateMatrixWorld(true);
  current?.traverse((node) => {
    if (node.isBone) bones++;
    if (!node.isSkinnedMesh) return;
    node.skeleton.update();
    const attribute = node.geometry.attributes.position;
    for (let i = 0; i < attribute.count; i += Math.max(1, Math.floor(attribute.count / 48))) {
      const point = new THREE.Vector3();
      node.getVertexPosition(i, point);
      point.applyMatrix4(node.matrixWorld);
      positions.push(point.x, point.y, point.z);
    }
  });
  return { bones, positions };
}
window.faithfulGallery = { state, selectModel, frame, playClip, seekMotion, poseSnapshot, effectSnapshot: () => techniqueEffects.snapshot(), startDemo, stopDemo, advanceDemo, reload: loadManifest };
try {
  setupScene();
  loadManifest();
} catch (cause) {
  error('이 브라우저에서 3D 화면을 시작할 수 없습니다.\nEdge 또는 Chrome의 하드웨어 가속을 켜고 다시 열어 주세요.', cause);
}
