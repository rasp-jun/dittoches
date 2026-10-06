import * as THREE from 'three';
import { signatureGeometry, renderSignature } from './technique-signatures.js';
import { PoseTransition } from './pose-transition.js';

const smooth = (a,b,x) => { const t=THREE.MathUtils.clamp((x-a)/(b-a),0,1); return t*t*(3-2*t); };
const v=(x=0,y=0,z=0)=>new THREE.Vector3(x,y,z);
const mouthOffsets={koromon:[0,-.60,.76],tsunomon:[0,-.59,.60],mochimon:[0,-.35,.40],tanemon:[0,-.30,.52],pyocomon:[0,-.30,.51],tokomon:[0,-.48,.60],agumon:[0,.02,.64],gabumon:[0,-.02,.39],piyomon:[0,.05,.39],patamon:[0,-.26,.40],greymon:[0,-.15,.40],garurumon:[0,-.04,.48],metalgarurumon:[0,-.03,.40]};

/** Deterministic clip-time effects: pause, scrubbing, speed and repeat agree. */
export class TechniqueEffects {
  constructor(scene) {
    this.group=new THREE.Group();this.group.name='Technique presentation';scene.add(this.group);
    this.dummy=new THREE.Object3D();this.color=new THREE.Color();this.pool={};this.limit=160;
    const geometry={orb:new THREE.SphereGeometry(1,20,14),ring:new THREE.TorusGeometry(1,.027,5,36),
      arc:new THREE.TorusGeometry(1,.025,5,30,Math.PI*.82),cone:new THREE.ConeGeometry(1,1,7),
      beam:new THREE.CylinderGeometry(1,1,1,10,1,true),petal:new THREE.OctahedronGeometry(1,0)};
    const bird=new THREE.BufferGeometry();bird.setAttribute('position',new THREE.Float32BufferAttribute([-1,0,0,-.23,.08,.13,0,0,.4, 0,0,.4,.23,.08,.13,1,0,0, -.2,0,.1,0,.12,-.30,.2,0,.1],3));bird.computeVertexNormals();geometry.bird=bird;
    Object.assign(geometry,signatureGeometry());
    for(const [name,shape] of Object.entries(geometry)) {
      const material=new THREE.MeshBasicMaterial({color:0xffffff,transparent:true,opacity:name==='beam'?.40:.72,depthWrite:false,
        blending:THREE.NormalBlending,side:THREE.DoubleSide,toneMapped:false});
      shape.setAttribute('effectOpacity',new THREE.InstancedBufferAttribute(new Float32Array(this.limit),1));
      if(name==='bird'){material.blending=THREE.NormalBlending;material.opacity=.82;}
      if(['orb','beam','flame','plume','core','crystal','rod'].includes(name)) {
        const beam=name==='beam';
        material.onBeforeCompile=(shader)=>{
          shader.vertexShader=shader.vertexShader.replace('void main() {','varying vec3 glowNormal; varying vec3 glowView; varying float glowAxial;\nvoid main() {').replace('#include <project_vertex>','#include <project_vertex>\nvec3 effectNormal=normal/vec3(dot(instanceMatrix[0].xyz,instanceMatrix[0].xyz),dot(instanceMatrix[1].xyz,instanceMatrix[1].xyz),dot(instanceMatrix[2].xyz,instanceMatrix[2].xyz)); glowNormal = normalize(normalMatrix * mat3(instanceMatrix) * effectNormal); glowView = normalize(-mvPosition.xyz); glowAxial=uv.y;');
          const edge=beam?'pow(max(0.0,dot(normalize(glowNormal),normalize(glowView))),1.8)*smoothstep(0.0,.14,glowAxial)*(1.0-smoothstep(.75,1.0,glowAxial))':'pow(max(0.0,dot(normalize(glowNormal),normalize(glowView))),2.4)';
          const flame=name==='flame'||name==='plume';
          const solid=['core','crystal','rod'].includes(name);
          const opacity=solid?'1.0':flame?'pow(abs(dot(normalize(glowNormal),normalize(glowView))),.85)*smoothstep(0.0,.12,glowAxial)*(1.0-smoothstep(.88,1.0,glowAxial))':edge;
          const shading=solid?'outgoingLight *= .52+.48*abs(dot(normalize(glowNormal),normalize(glowView)));':'';
          shader.fragmentShader=shader.fragmentShader.replace('void main() {','varying vec3 glowNormal; varying vec3 glowView; varying float glowAxial;\nvoid main() {').replace('#include <opaque_fragment>',shading+'\ndiffuseColor.a *= '+opacity+';\n#include <opaque_fragment>');
        };
        material.customProgramCacheKey=()=> 'soft-technique-'+name+'-v3';
        material.opacity=beam?.65:['core','crystal','rod'].includes(name)?.96:.86;
      }
      const soft=material.onBeforeCompile;
      material.onBeforeCompile=(shader)=>{
        soft(shader);
        shader.vertexShader=shader.vertexShader.replace('void main() {','attribute float effectOpacity; varying float particleOpacity;\nvoid main() {\nparticleOpacity=effectOpacity;');
        shader.fragmentShader=shader.fragmentShader.replace('void main() {','varying float particleOpacity;\nvoid main() {').replace('#include <opaque_fragment>','diffuseColor.a *= particleOpacity;\n#include <opaque_fragment>');
      };
      const mesh=new THREE.InstancedMesh(shape,material,this.limit);mesh.count=0;mesh.frustumCulled=false;
      mesh.instanceMatrix.setUsage(THREE.DynamicDrawUsage);this.group.add(mesh);this.pool[name]=mesh;
    }
    this.clear();
  }
  clear(){for(const mesh of Object.values(this.pool))mesh.count=0;this.info={active:false,particles:0,phase:'idle'};}
  snapshot(){return Object.fromEntries(Object.entries(this.pool).filter(([,m])=>m.count).map(([name,m])=>[name,{
    matrices:Array.from(m.instanceMatrix.array.slice(0,m.count*16)),
    colors:Array.from(m.instanceColor.array.slice(0,m.count*3)),
    opacity:Array.from(m.geometry.attributes.effectOpacity.array.slice(0,m.count))
  }]));}
  bind(model,entry,clips=[],animationRoot=null){
    this.clear();this.model=model;this.entry=entry;this.nodes={};this.anchors={};model.updateMatrixWorld(true);
    model.traverse(n=>{if(n.isBone)this.nodes[n.name]=n;});
    const id=entry.id,scale=model.scale.x;
    const native=id==='birdramon'||id==='kuwagamon';this.unit=native?new THREE.Box3().setFromObject(model,true).getSize(v()).y/2.4:scale;
    const names=id==='birdramon'?{head:'J_head_021',chest:'J_body_06',leftHand:'J_wingB_L_010',rightHand:'J_wingB_R_016',leftWing:'J_wingB_L_010',rightWing:'J_wingB_R_016'}:
      id==='kuwagamon'?{head:'joint10_09',chest:'SPINE3_03',leftHand:'joint28_025',rightHand:'joint28_070',leftWing:'joint36_017',rightWing:'joint90_019'}:
      {head:'Head',chest:'Spine',leftHand:'HandL',rightHand:'HandR',leftWing:'WingL',rightWing:'WingR'};
    const attachment=(key,name,offset=[0,0,0])=>{
      const node=this.nodes[name];if(!node)return;
      const point=node.getWorldPosition(v()).add(v(...offset).multiplyScalar(this.unit));
      this.anchors[key]={node,local:node.worldToLocal(point)};
    };
    for(const [key,name] of Object.entries(names))attachment(key,name,key.includes('Hand')?[0,-.02,.05]:[0,0,0]);
    attachment('leftLowerWing','LowerWingL');attachment('rightLowerWing','LowerWingR');
    attachment('mouth',names.head,mouthOffsets[id]||(id==='birdramon'?[0,-.02,.16]:[0,0,.28]));
    attachment('head',names.head,id==='shellmon'?[0,.32,.18]:id==='atlur'?[0,.40,.15]:[0,.08,.22]);
    attachment('chest',names.chest,id==='seraphimon'?[0,.10,.50]:[0,.02,.22]);
    if(id==='tentomon')for(const s of ['left','right'])attachment(s+'Wing',names[s+'Wing'],[(s==='left'?-1:1)*.18,.03,0]);
    this.launches={};
    if(animationRoot) {
      // Sample the actual release pose once, then restore every transform. Each
      // projectile keeps its release origin even when the emitter recoils. This
      // also works when a review starts by seeking directly to a later frame.
      const poses=new PoseTransition(model),saved=poses.capture();
      const sampler=new THREE.AnimationMixer(animationRoot);
      for(const mode of ['Attack','Skill']) {
        const spec=entry.techniques?.[mode],clip=clips.find(c=>c.name===mode);
        if(!spec||!clip)continue;
        this.launches[mode]=[];
        for(let i=0;i<(spec.shots||1);i++) {
          sampler.stopAllAction();
          const action=sampler.clipAction(clip).reset().play();action.paused=true;
          action.time=clip.duration*(spec.release+i*(spec.effect==='seven'?.017:.029));
          sampler.update(0);model.updateMatrixWorld(true);
          this.launches[mode].push(Object.fromEntries([...Object.keys(this.anchors),'hands','wings','overhead','sword'].map(key=>[key,this.point(key)])));
        }
      }
      sampler.stopAllAction();sampler.uncacheRoot(animationRoot);poses.restore(saved);model.updateMatrixWorld(true);
    }
  }
  launchPoint(mode,index,key){return this.launches?.[mode]?.[index]?.[key]?.clone()||this.point(key);}
  point(key) {
    if(key==='hands')return this.point('leftHand').add(this.point('rightHand')).multiplyScalar(.5);
    if(key==='wings')return this.point('leftWing').add(this.point('rightWing')).multiplyScalar(.5);
    if(key==='overhead')return this.point('hands').add(v(0,.26*this.unit,0));
    if(key==='sword')return this.point('leftHand');
    const a=this.anchors[key]||this.anchors.chest;
    return a?a.node.localToWorld(a.local.clone()):v(0,1.3,0);
  }
  put(type,position,scale,color,fade=1,rotation=null){
    const mesh=this.pool[type];if(!mesh||mesh.count>=this.limit||fade<=.01)return;
    this.dummy.position.copy(position);this.dummy.scale.set(...(Array.isArray(scale)?scale:[scale,scale,scale]));
    this.dummy.quaternion.identity();if(rotation?.isQuaternion)this.dummy.quaternion.copy(rotation);else if(rotation)this.dummy.rotation.set(...rotation);
    this.dummy.updateMatrix();mesh.setMatrixAt(mesh.count,this.dummy.matrix);this.color.set(color);mesh.setColorAt(mesh.count,this.color);mesh.geometry.attributes.effectOpacity.setX(mesh.count,fade);mesh.count++;
  }
  beam(a,b,radius,color,fade=1){const delta=b.clone().sub(a);this.put('beam',a.clone().add(b).multiplyScalar(.5),[radius,delta.length(),radius],color,fade,new THREE.Quaternion().setFromUnitVectors(v(0,1,0),delta.normalize()));}
  update(mode,time,duration,enabled=true,previous=false){
    this.clear();const spec=this.entry?.techniques?.[mode];
    if(!spec||!enabled||previous||!duration||!this.model)return this.info;
    this.model.updateMatrixWorld(true);const u=time/duration,unit=this.unit;
    if(u<.10||u>=.97)return this.info;
    const release=spec.release,end=spec.end,charge=smooth(.12,release-.09,u)*(1-smooth(release-.015,release+.065,u));
    const energy=smooth(release-.055,release+.04,u)*(1-smooth(end,.95,u));
    const origin=this.point(spec.origin),color=spec.color,effect=spec.effect;
    const signature=renderSignature(this,{mode,u,unit,origin,color,effect,charge,energy,release,end,spec});
    if(!signature)throw new Error('Unsupported technique effect: '+effect);
    let count=0;
    for(const mesh of Object.values(this.pool)){mesh.instanceMatrix.needsUpdate=true;mesh.geometry.attributes.effectOpacity.needsUpdate=true;if(mesh.instanceColor)mesh.instanceColor.needsUpdate=true;count+=mesh.count;}
    this.info={active:count>0,particles:count,phase:u<release?'charge':u<end?'release':'recover',effect,origin:spec.origin,emitter:origin.toArray(),progress:u,signature:!!signature,shapes:Object.fromEntries(Object.entries(this.pool).filter(([,m])=>m.count).map(([name,m])=>[name,m.count]))};
    return this.info;
  }
}
