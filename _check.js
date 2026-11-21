
'use strict';
(function(){
  var canvas=document.getElementById('c'), ctx=canvas.getContext('2d');
  var W=0,H=0;
  function resize(){ var dpr=Math.min(window.devicePixelRatio||1,2); W=window.innerWidth; H=window.innerHeight; canvas.width=W*dpr; canvas.height=H*dpr; ctx.setTransform(dpr,0,0,dpr,0,0); }
  window.addEventListener('resize',function(){ resize(); makeDirt(); }); resize();

  var dirt=[], total=0, cleaned=0, sparks=[], done=false, doneT=0;
  function makeDirt(){
    dirt=[]; cleaned=0; total=0; done=false;
    var n=Math.max(60, Math.floor(W*H/9000));
    for(var i=0;i<n;i++){
      dirt.push({ x:Math.random()*W, y:Math.random()*H, r:8+Math.random()*26, a:0.25+Math.random()*0.4, streak:Math.random()<0.25 });
      total++;
    }
  }
  makeDirt();

  var AC=null,master=null,noise=null;
  function audio(){ if(!AC){ try{ AC=new (window.AudioContext||window.webkitAudioContext)(); master=AC.createGain(); master.gain.value=0.4; master.connect(AC.destination);
    var len=AC.sampleRate*0.4, b=AC.createBuffer(1,len,AC.sampleRate), d=b.getChannelData(0); for(var i=0;i<len;i++) d[i]=Math.random()*2-1; noise=b; }catch(e){} } if(AC&&AC.state==='suspended') AC.resume(); }
  function shh(){
    if(!AC) return; var t=AC.currentTime;
    var src=AC.createBufferSource(); src.buffer=noise; src.loop=true;
    var f=AC.createBiquadFilter(); f.type='bandpass'; f.frequency.setValueAtTime(1800+Math.random()*900,t); f.frequency.exponentialRampToValueAtTime(700,t+0.1);
    var g=AC.createGain(); g.gain.setValueAtTime(0.22,t); g.gain.exponentialRampToValueAtTime(0.001,t+0.12);
    src.connect(f); f.connect(g); g.connect(master); src.start(t); src.stop(t+0.14);
  }
  function chime(){
    if(!AC) return; var t=AC.currentTime;
    [660,880,1100,1320].forEach(function(fr,i){ var o=AC.createOscillator(); o.type='sine'; o.frequency.value=fr;
      var g=AC.createGain(); var s=t+i*0.09; g.gain.setValueAtTime(0.25,s); g.gain.exponentialRampToValueAtTime(0.001,s+0.3);
      o.connect(g); g.connect(master); o.start(s); o.stop(s+0.32); });
  }

  var pressing=false, px=0, py=0, lastSnd=0;
  function cleanAt(x,y){
    var R=56, c=0;
    for(var i=dirt.length-1;i>=0;i--){
      var d=dirt[i];
      var dx=d.x-x, dy=d.y-y;
      if(dx*dx+dy*dy < (R+d.r)*(R+d.r)){
        for(var k=0;k<3;k++) sparks.push({ x:d.x, y:d.y, vx:(Math.random()-0.5)*120, vy:-60-Math.random()*120, life:0, max:0.4+Math.random()*0.3, r:1.5+Math.random()*2.5 });
        dirt.splice(i,1); c++;
      }
    }
    if(c>0){
      cleaned+=c;
      var now=performance.now(); if(now-lastSnd>90){ lastSnd=now; shh(); }
    }
    if(cleaned>=total && !done){ done=true; doneT=performance.now(); chime(); }
  }
  canvas.addEventListener('pointerdown',function(e){ audio(); pressing=true; px=e.clientX; py=e.clientY; cleanAt(px,py); });
  window.addEventListener('pointermove',function(e){ if(pressing){ px=e.clientX; py=e.clientY; cleanAt(px,py); } });
  window.addEventListener('pointerup',function(){ pressing=false; });

  function draw(){
    // å¹²å‡€çš„çª—æˆ·
    var g=ctx.createLinearGradient(0,0,W,H); g.addColorStop(0,'#eaf4ff'); g.addColorStop(1,'#d5e8fb');
    ctx.fillStyle=g; ctx.fillRect(0,0,W,H);
    // çª—æ¡†
    ctx.strokeStyle='rgba(120,150,180,0.35)'; ctx.lineWidth=8;
    ctx.strokeRect(10,10,W-20,H-20);
    ctx.beginPath(); ctx.moveTo(W/2,10); ctx.lineTo(W/2,H-10); ctx.stroke();
    // æ±¡æ¸
    for(var i=0;i<dirt.length;i++){
      var d=dirt[i];
      ctx.fillStyle='rgba(120,110,100,'+d.a+')';
      if(d.streak){ ctx.save(); ctx.translate(d.x,d.y); ctx.rotate(0.4); ctx.fillRect(-d.r*2,-d.r*0.4,d.r*4,d.r*0.8); ctx.restore(); }
      else { ctx.beginPath(); ctx.arc(d.x,d.y,d.r,0,Math.PI*2); ctx.fill(); }
    }
    // æ¸…æ´ç«èŠ±
    for(var k=sparks.length-1;k>=0;k--){ var s=sparks[k]; s.life+=0.016; if(s.life>=s.max){sparks.splice(k,1);continue;} s.vy+=400*0.016; s.x+=s.vx*0.016; s.y+=s.vy*0.016; ctx.globalAlpha=1-s.life/s.max; ctx.fillStyle='#7fd4ff'; ctx.beginPath(); ctx.arc(s.x,s.y,s.r,0,Math.PI*2); ctx.fill(); }
    ctx.globalAlpha=1;
    // æµ·ç»µå…‰æ ‡
    if(pressing){ ctx.strokeStyle='rgba(90,168,232,0.6)'; ctx.lineWidth=4; ctx.beginPath(); ctx.arc(px,py,56,0,Math.PI*2); ctx.stroke(); }
    // å®Œæˆ
    if(done){
      ctx.font='bold 38px "Segoe UI Emoji",sans-serif'; ctx.textAlign='center'; ctx.fillText('âœ¨ ç„•ç„¶ä¸€æ–°ï¼', W/2, H/2);
      if(performance.now()-doneT>1500){ makeDirt(); }
    }
    var pct = Math.round(cleaned/total*100);
    document.getElementById('bar').style.width = pct+'%';
  }
  function frame(){ requestAnimationFrame(frame); draw(); }
  requestAnimationFrame(frame);
})();

