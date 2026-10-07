import * as THREE from 'three';

const V = (x=0,y=0,z=0) => new THREE.Vector3(x,y,z);
const clamp = x => Math.max(0,Math.min(1,x));
const ease = (a,b,x) => {const t=clamp((x-a)/(b-a));return t*t*(3-2*t);};
const forwardRotation = new THREE.Quaternion().setFromUnitVectors(V(0,1,0),V(0,0,1));

export function signatureGeometry() {
  const points=[];
  for(let i=0;i<=16;i++) {const t=i/16;points.push(new THREE.Vector2(Math.sin(Math.PI*t)*(.7+.3*t),t-.5));}
  const flame=new THREE.LatheGeometry(points,14);
  const feather=new THREE.Shape();
  feather.moveTo(0,-.55);feather.bezierCurveTo(-.48,-.18,-.28,.3,0,.6);
  feather.bezierCurveTo(.28,.3,.48,-.18,0,-.55);
  const ribbon=new THREE.BufferGeometry(),pos=[],alpha=[];
  for(let i=0;i<32;i++) {
    for(const [t,edge] of [[i/32,0],[i/32,1],[(i+1)/32,1],[i/32,0],[(i+1)/32,1],[(i+1)/32,0]]) {
      const a=t*Math.PI*1.2,r=1-edge*Math.sin(Math.PI*t)*.18;
      pos.push(Math.cos(a)*r,Math.sin(a)*r,0);alpha.push(Math.sin(Math.PI*t));
    }
  }
  ribbon.setAttribute('position',new THREE.Float32BufferAttribute(pos,3));ribbon.computeVertexNormals();
  return {flame,plume:new THREE.ConeGeometry(1,1,24,1,true),feather:new THREE.ShapeGeometry(feather,12),slash:ribbon,
    core:new THREE.SphereGeometry(1,24,16),crystal:new THREE.OctahedronGeometry(1),
    rod:new THREE.CylinderGeometry(1,1,1,8,1,true),disc:new THREE.CircleGeometry(1,64)};
}

// Every shape is evaluated from the clip time. No simulation history or random
// numbers: scrubbing, paused inspection and playback all produce the same frame.
export function renderSignature(fx,c) {
  const {mode,u,unit,origin,color,effect,charge,energy,release,end,spec}=c;
  const at=(p,x=0,y=0,z=0)=>p.clone().add(V(x,y,z).multiplyScalar(unit));
  const put=(kind,p,scale,tint=color,fade=1,rotation=null)=>fx.put(kind,p,
    Array.isArray(scale)?scale.map(x=>x*unit):scale*unit,tint,fade,rotation);
  const line=(a,b,r,tint=color,fade=1,solid=false)=>{
    const delta=b.clone().sub(a);if(delta.lengthSq()<1e-12)return;
    if(solid)put('rod',a.clone().lerp(b,.5),[r,delta.length()/unit,r],tint,fade,
      new THREE.Quaternion().setFromUnitVectors(V(0,1,0),delta.normalize()));
    else fx.beam(a,b,r*unit,tint,fade);
  };
  const halo=(p,r,fade,tint=color)=>put('orb',p,r,tint,fade*.60);
  const ring=(p,r,fade,tint=color,rotation=null)=>put('ring',p,r,tint,fade,rotation);
  const spark=(p,i,age,tint=color,amount=1)=>{
    const a=i*2.399,spread=age*(.20+(i%3)*.09);
    put('flame',at(p,Math.cos(a)*spread,Math.sin(a)*spread,age*.18),[.012,.10*(1-age)+.01,.012],tint,(1-age)*amount,[0,0,-a]);
  };
  const chargeRays=(p,r,amount,tint=color)=>{
    if(amount<.01)return;
    halo(p,r*1.4,amount,tint);
    for(let i=0;i<8;i++){
      const a=i*Math.PI/4+u*2,f=1-(u*3+i*.13)%1;
      line(at(p,Math.cos(a)*r*(1+f),Math.sin(a)*r*(1+f),0),at(p,Math.cos(a)*r,Math.sin(a)*r,0),.009,tint,amount*(1-f));
    }
  };
  const burst=(p,age,tint=color)=>{
    ring(p,.12+age*.40,1-age,tint);
    for(let i=0;i<9;i++)spark(p,i,age,tint);
  };
  const electric=(a,b,seed,fade,tint=color)=>{
    let last=a;
    for(let i=1;i<=10;i++){
      const t=i/10,amp=Math.sin(t*Math.PI)*.11;
      const next=a.clone().lerp(b,t).add(V(Math.sin(i*9+seed+u*19)*amp,Math.cos(i*7+seed+u*17)*amp,0).multiplyScalar(unit));
      line(last,next,.012,tint,fade,true);last=next;
    }
  };
  if(['breath','ice','water','holybeam','thunderbeam'].includes(effect)) {
    chargeRays(origin,.11+.12*charge,charge);
    if(energy<.01)return true;
    const reach=(effect==='water'?2.25:2.55)*ease(release-.05,release+.12,u),tip=at(origin,0,0,reach);
    if(effect==='breath') {
      const blue=fx.entry.id==='garurumon',hot=blue?'#d6f9ff':'#fff2a3',mid=blue?'#3c7cfa':'#ff9b24';
      const plumeRotation=new THREE.Quaternion().setFromUnitVectors(V(0,1,0),V(0,0,-1));
      put('plume',origin.clone().lerp(tip,.5),[.30,reach,.30],mid,energy*.85,plumeRotation);
      put('plume',origin.clone().lerp(tip,.45),[.17,reach*.90,.17],hot,energy,plumeRotation);
      line(origin,tip,.048,hot,energy);
      for(let i=0;i<22;i++) {
        const t=(i/22+u*1.9)%1,angle=i*2.399+u*7,width=.025+t*.20;
        const p=at(origin,Math.cos(angle)*width,Math.sin(angle)*width,reach*t);
        put('flame',p,[.045+t*.065,.36+t*.39,.045+t*.07],i%3===0?hot:mid,energy*(1-t*.55)*.65,forwardRotation);
        if(i%2===0)spark(p,i,(u*4+i*.17)%1,hot,energy*.7);
      }
    } else if(effect==='ice') {
      line(origin,tip,.13,'#b7e5f5',energy*.40);line(origin,tip,.044,'#edfaff',energy);
      for(let i=0;i<22;i++) {
        const t=(i/22+u*1.3)%1,a=i*2.399;
        put('crystal',at(origin,Math.cos(a)*(.035+t*.27),Math.sin(a)*(.035+t*.23),reach*t),[.035+t*.045,.12+t*.13,.027],i%2?'#78c9e9':'#e9fcff',energy*(1-t*.5),[.4,0,a]);
      }
      for(let i=0;i<3;i++)ring(at(origin,0,0,reach*(i+.5)/3),(.08+i*.07),energy*.55,'#c8f4ff');
    } else if(effect==='water') {
      line(origin,tip,.085,'#249ce0',energy);line(origin,tip,.035,'#efffff',energy);
      for(let i=0;i<8;i++) {
        const t=(i/8+u*2)%1;ring(at(origin,0,0,reach*t),.07+t*.07,energy*.75,'#bdefff');
        put('flame',at(origin,Math.sin(i*4)*t*.10,Math.cos(i*3)*t*.1,reach*t),[.04,.28,.04],'#6accff',energy,forwardRotation);
      }
      for(let i=0;i<18;i++) {
        const age=(u*2+i*.17)%1,a=i*2.399;
        put('flame',at(tip,Math.cos(a)*age*.48,Math.sin(a)*age*.40-age*age*.35,age*.12),[.018,.06,.018],i%2?'#ffffff':'#5cbce8',energy*(1-age),[0,0,-a]);
      }
    } else if(effect==='holybeam') {
      line(origin,tip,.11,'#efb83a',energy);line(origin,tip,.047,'#fffbe0',energy);
      ring(origin,.24,energy,'#fff3b4');
      for(let i=0;i<4;i++) {const t=(u*1.5+i/4)%1;ring(at(origin,0,0,reach*t),.12+t*.07,energy*(1-t),'#ffe39a');}
      for(let i=0;i<8;i++)spark(tip,i,(u*3+i*.1)%1,'#fff1c2',energy);
    } else {
      line(origin,tip,.085,'#8babff',energy*.7);line(origin,tip,.027,'#f6ffff',energy);
      for(let i=0;i<4;i++)electric(at(origin,Math.sin(i)*.06,Math.cos(i)*.06),tip,i*8,energy,i%2?'#c8fbff':'#6f8aff');
      ring(origin,.25,energy,'#adf2ff',[0,u*3,0]);
    }
    return true;
  }
  if(['fireball','spiral','electricorb','holyball','sun','darkorb','flower','seven','meteors','shadowbird','missiles','air','bubbles'].includes(effect)) {
    const shots=spec.shots||1;
    const source=(i,launched=false)=>{
      const point=key=>launched?fx.launchPoint(mode,i,key):fx.point(key),base=point(spec.origin);
      return effect==='seven'?at(base,Math.cos(Math.PI*.07+i*Math.PI*.86/6)*.90,.20+Math.sin(Math.PI*.07+i*Math.PI*.86/6)*.78,0):
        effect==='meteors'?point(i%2?'rightWing':'leftWing'):effect==='missiles'?at(base,(i?1:-1)*.17,0,0):base;
    };
    const orb=(p,r,fade,tint,kind)=>{
      halo(p,r*1.7,fade,tint);
      if(kind==='darkorb') {
        put('core',p,r,'#211335',fade);ring(p,r*1.08,fade,'#b784fa',[.3,u*4,.5]);
        for(let j=0;j<3;j++)put('slash',p,r*(1.25+j*.12),'#8853bd',fade*.7,[j*.9,u*3,j*2]);
      } else {
        put('core',p,r*.76,kind==='sun'?'#fff4ad':kind==='electricorb'?'#cdfaff':'#fff8df',fade);
        for(let j=0;j<(kind==='sun'?5:3);j++)put('slash',p,r*(1+j*.07),tint,fade,[j*.85,u*4+j,j*2.1]);
      }
    };
    if(effect==='seven')for(let i=0;i<7;i++) {
      const p=source(i);orb(p,.105,charge,'#ffe29c','seven');ring(p,.16,charge,'#fff1bb');
    }
    else if(effect==='meteors')for(const side of ['leftWing','rightWing'])chargeRays(fx.point(side),.16,charge,'#ffce63');
    else if(effect==='sun') {
      orb(origin,.10+charge*.54,charge,'#ff9f23','sun');chargeRays(origin,.34+charge*.3,charge);
    } else if(effect==='missiles')for(let i=0;i<2;i++)ring(source(i),.11,charge,'#ffcc76');
    else if(!['bubbles','shadowbird'].includes(effect))chargeRays(origin,.10+charge*.11,charge);
    for(let i=0;i<shots;i++) {
      const delay=shots>1?i*(effect==='seven'?.017:.029):0;
      const age=(u-release-delay)/(.94-release-delay);
      if(age<0||age>1)continue;
      const start=source(i,true),fade=1-ease(.76,1,age);
      const spread=effect==='seven'?Math.cos(i*Math.PI/3)*.16:0;
      const p=at(start,spread*age,(effect==='meteors'?-.55:effect==='bubbles'?.20:effect==='sun'?-.16:0)*age,age*2.35);
      if(effect==='missiles') {
        put('core',p,[.067,.067,.22],'#a8b9c6',fade);put('cone',at(p,0,0,.20),[.068,.16,.068],'#ecf1f5',fade,forwardRotation);
        for(let j=0;j<4;j++) {const a=j*Math.PI/2;put('feather',at(p,Math.cos(a)*.055,Math.sin(a)*.055,-.13),[.12,.14,.12],'#647c94',fade,[Math.PI/2,a,0]);}
        put('flame',at(p,0,0,-.35),[.055,.36,.055],'#ffbf42',fade,forwardRotation);
        for(let j=1;j<7;j++)halo(at(p,0,j*.008,-.35-j*.065),.035+j*.014,fade*(1-j/8),'#a5acb1');
      } else if(effect==='meteors') {
        put('feather',p,[.15,.43,.15],'#ffcf61',fade,[Math.PI/2,0,i]);
        put('flame',at(p,0,.05,-.24),[.09,.58,.09],'#ff7927',fade*.85,forwardRotation);
        for(let j=0;j<4;j++)spark(at(p,0,0,-j*.1),j,(u*3+j*.2)%1,'#ffeaa8',fade);
      } else if(effect==='shadowbird') {
        for(let j=3;j>=0;j--)put('bird',at(p,0,.015*j,-j*.16),[.72-j*.07,.72,1.0-j*.09],j?'#7995b7':'#2b4269',fade*(1-j*.23),[0,0,Math.sin(u*9)*.10]);
        put('slash',p,[.83,.34,.60],'#b5ddec',fade*.70,[.6,0,u*4]);
      } else if(effect==='air') {
        for(let j=0;j<4;j++)ring(at(p,0,0,-j*.08),.17+age*.18-j*.022,fade*(1-j*.2),'#92bfd3');
        put('slash',p,.28+age*.15,'#e5fbff',fade,[0,0,u*7]);
      } else if(effect==='bubbles') {
        const radius=.10+(i%3)*.025;halo(p,radius,color==='#b8ef9d'?.20:.13,color);
        ring(p,radius,fade,fx.entry.id==='tanemon'?'#73aa76':fx.entry.id==='mochimon'?'#b99ae7':'#85b8da');
        put('slash',at(p,-radius*.13,radius*.1,.01),radius*.78,'#ffffff',fade,[0,0,.4]);
        if(fx.entry.id==='pyocomon')for(let j=0;j<5;j++){const a=j*Math.PI*2/5;put('feather',at(p,Math.cos(a)*radius,Math.sin(a)*radius,0),[.04,.07,.04],'#ffb5d1',fade,[0,0,a]);}
      } else if(effect==='flower') {
        orb(p,.16,fade,'#e960ac','flower');
        for(let j=0;j<6;j++){const a=j*Math.PI/3+u*3;put('feather',at(p,Math.cos(a)*.16,Math.sin(a)*.16,0),[.11,.21,.10],j%2?'#ffa3d7':'#ffdbed',fade,[0,0,a-Math.PI/2]);}
        line(start,p,.022,'#ffb2d9',fade*.6);
      } else {
        const radius=effect==='sun'?.48:effect==='darkorb'?.22:effect==='seven'?.115:.14;
        orb(p,radius,fade,color,effect);
        if(effect==='electricorb')for(let j=0;j<5;j++){const a=j*1.26;electric(at(p,Math.cos(a)*.24,Math.sin(a)*.24,-.04),p,j,fade,'#9beaff');}
        if(effect==='fireball'||effect==='spiral') {
          for(let j=0;j<9;j++){
            const a=j*.85+u*14,r=effect==='spiral'?.16:.055;
            put('flame',at(p,Math.sin(a)*r,Math.cos(a)*r,-j*.055),[.035+j*.006,.20,.035],j%3?color:'#e9fff1',fade*(1-j/11),forwardRotation);
          }
        } else if(effect==='seven')line(start,p,.020,'#ffe9a5',fade*.65);
        else if(effect==='sun')for(let j=0;j<9;j++)spark(p,j,(u*3+j*.13)%1,'#ffbb43',fade);
      }
      if(age>.77 && !['bubbles','meteors','shadowbird','air'].includes(effect))burst(p,(age-.77)/.23,color);
    }
    return true;
  }
  if(effect==='gate') {
    const p=at(origin,0,0,1.25);p.y=fx.point('chest').y+.20*unit;
    const open=ease(.24,.59,u)*(1-ease(.84,.97,u)),r=.80*open;
    put('disc',p,[r,r*1.30,1],'#302454',open*.83);
    for(let i=0;i<3;i++)put('ring',at(p,0,0,.005+i*.006),[r*(1+i*.095),r*1.30*(1+i*.095),r],'#d7b1ff',open*(1-i*.18),[0,0,0]);
    for(let i=0;i<16;i++) {
      const a=i*Math.PI/8+u*.18;
      put('crystal',at(p,Math.cos(a)*r*1.09,Math.sin(a)*r*1.42,.025),[.022,.045,.012],'#fff2ff',open,[0,0,-a]);
      const f=(u*1.1+i*.071)%1;
      line(at(p,Math.cos(a)*r*(1-f),Math.sin(a)*r*1.3*(1-f),.035),at(p,Math.cos(a)*r*(1-f)*.7,Math.sin(a)*r*1.3*(1-f)*.7,.035),.008,'#a685d9',open*f);
    }
    return true;
  }
  if(['vines','whip','darkclaw'].includes(effect)) {
    const sides=effect==='whip'?['rightHand']:['leftHand','rightHand'];
    for(const side of sides) {
      const start=fx.point(side);chargeRays(start,.12,charge);
      if(energy<.01)continue;
      const strands=effect==='whip'?1:3;
      for(let strand=0;strand<strands;strand++) {
        let last=start;const reach=(effect==='darkclaw'?1.6:2.0)*energy;
        for(let j=1;j<=14;j++) {
          const f=j/14,a=f*7-u*10;
          const p=at(start,Math.sin(a)*f*.22+(strand-1)*.06,Math.cos(a)*f*.16,reach*f);
          line(last,p,effect==='vines'?.023:.016,effect==='vines'?'#447849':effect==='darkclaw'?'#392552':'#b44978',energy,true);
          if(effect==='whip')electric(last,p,j,energy*.8,'#ffd6f3');
          if(j%3===0 && effect!=='darkclaw')put('cone',p,[.030,.105,.028],effect==='vines'?'#b0db71':'#f4a2ce',energy,[0,0,a]);
          last=p;
        }
        if(effect==='darkclaw')put('slash',last,[.12,.27,.25],'#bd88ec',energy,[0,strand*.15,-.6]);
      }
    }
    return true;
  }
  if(['arc','claws','impact','bite','pincers','horn','needles'].includes(effect)) {
    const hits=effect==='needles'?[.40,.52,.65,.78]:effect==='claws'?[.47,.62]:[release];
    for(let i=0;i<hits.length;i++) {
      const age=(u-hits[i])/.17;if(age<0||age>1)continue;
      const p=effect==='bite'?fx.point('mouth'):effect==='pincers'||effect==='horn'?fx.point('head'):effect==='impact'?origin:fx.point(i%2?'rightHand':spec.origin==='rightHand'?'rightHand':'leftHand');
      const hit=at(p,0,.03,.20+age*.24),fade=Math.sin(Math.PI*age);
      if(effect==='impact'||effect==='horn') {
        burst(hit,age,effect==='horn'?'#ffde74':color);
        if(effect==='horn')put('cone',hit,[.11,.55,.11],'#fff0ba',fade,forwardRotation);
      } else if(effect==='bite'||effect==='pincers') {
        const gap=(1-age)*.25;
        for(const side of [-1,1]) {
          put('slash',at(hit,0,side*gap,0),[.31,.21,.3],color,fade,[0,0,side>0?0:Math.PI]);
          for(let j=0;j<4;j++)put('cone',at(hit,(j-1.5)*.12,side*(gap+.015),0),[.035,.11,.035],'#fff9df',fade,[0,0,side>0?Math.PI:0]);
        }
      } else {
        const count=effect==='claws'?3:1;
        for(let j=0;j<count;j++)put('slash',at(hit,j*.065,j*.035,0),[.50,.46,.5],j?color:'#fff0d1',fade,[.1,.35,(i%2?-1:1)*(.6+age*.7)]);
        if(effect==='needles')for(let j=0;j<8;j++)put('cone',at(hit,Math.sin(j*2.4)*.13,Math.cos(j*2.4)*.13,age*.28),[.014,.20,.014],'#e9ffab',fade,forwardRotation);
      }
    }
    return true;
  }
  if(effect==='lightning') {
    const endPoint=at(origin,0,0,2.25);
    for(const side of ['leftWing','rightWing']) {
      const start=fx.point(side);chargeRays(start,.16,charge,'#b1f3ff');
      if(energy>.01) {
        electric(start,endPoint,side==='leftWing'?1:7,energy,'#59b8f4');
        electric(start,endPoint,side==='leftWing'?4:11,energy*.65,'#e3ffff');
        const branch=start.clone().lerp(endPoint,.55);
        electric(branch,at(branch,side==='leftWing'?-.3:.3,.3,.28),3,energy*.6,'#95ddff');
      }
    }
    return true;
  }
  if(effect==='starlight') {
    for(const side of ['leftWing','rightWing','leftLowerWing','rightLowerWing'])chargeRays(fx.point(side),.13,charge,'#ffe9ac');
    for(let i=0;i<40;i++) {
      const age=(u-release)*2.2-(i%8)*.032;if(age<0||age>1)continue;
      const start=at(origin,Math.sin(i*2.4)*1.2,1.0+(i%4)*.15,.30+(i%7)*.18);
      const p=at(start,Math.sin(i)*age*.08,-age*2.1,age*.28),fade=Math.sin(Math.PI*age);
      put('feather',p,[.025,.09,.025],i%3?'#ffe9a5':'#ffffff',fade,[0,0,i+u*2]);
      line(p,at(p,0,.16,0),.007,'#e2b557',fade*.65);
      if(i%5===0){put('slash',p,.06,'#fff5c9',fade,[0,0,u*2]);halo(p,.08,fade,'#fff2a8');}
    }
    return true;
  }
  if(effect==='sound') {
    for(let i=0;i<4;i++) {
      const age=(u-.22-i*.14)/.35;if(age<0||age>1)continue;
      const p=at(origin,0,.08,age*1.8);ring(p,.12+age*.40,1-age,'#dc88df');
      for(let j=0;j<3;j++) {
        const a=j*2.1+u*2,center=at(p,Math.cos(a)*(.13+age*.4),Math.sin(a)*(.13+age*.4),0);
        put('core',center,[.035,.025,.018],'#71377e',1-age);
        line(center,at(center,0,.13,0),.010,'#71377e',1-age,true);
        line(at(center,0,.13,0),at(center,.065,.10,0),.012,'#71377e',1-age,true);
      }
    }
    return true;
  }
  return false;
}
