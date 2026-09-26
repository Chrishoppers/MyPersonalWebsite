/* ============================================================
   回响档案 · 恐怖氛围系统
   文件：wwwroot/hxda/js/horror.js
   加载顺序：5
   依赖：storage.js、tracker.js
   ============================================================ */

(function () {

    'use strict';

    /* ============================================================
       状态
       ============================================================ */
    const H = {
        mouseX: 0,
        mouseY: 0,
        lastMouseMove: Date.now(),
        started: Date.now(),
        vignetteEl: null,
        scanlineEl: null,
        audioCtx: null,
        soundEnabled: true
    };

    /* ============================================================
       1. 扫描线（CRT 效果）
       ============================================================ */
    function injectScanlines() {
        const el = document.createElement('div');
        el.className = 'horror-scanlines';
        document.body.appendChild(el);
        H.scanlineEl = el;

        /* 高注意值时扫描线增强 */
        updateScanlineOpacity();
        setInterval(updateScanlineOpacity, 3000);
    }

    function updateScanlineOpacity() {
        if (!H.scanlineEl) return;
        const a = Store.get('attention', 0);
        let opacity;
        if (a >= 26) opacity = '0.7';
        else if (a >= 21) opacity = '0.6';
        else if (a >= 16) opacity = '0.5';
        else if (a >= 11) opacity = '0.45';
        else opacity = '0.4';
        H.scanlineEl.style.opacity = opacity;
    }

    /* ============================================================
       2. 暗角（跟随鼠标）
       ============================================================ */
    function injectVignette() {
        const el = document.createElement('div');
        el.className = 'horror-vignette';
        document.body.appendChild(el);
        H.vignetteEl = el;

        document.addEventListener('mousemove', function (e) {
            H.mouseX = e.clientX;
            H.mouseY = e.clientY;
            H.lastMouseMove = Date.now();

            const x = (e.clientX / window.innerWidth) * 100;
            const y = (e.clientY / window.innerHeight) * 100;

            const a = Store.get('attention', 0);
            const edgeDark =
                a >= 26 ? 0.6 :
                    a >= 21 ? 0.55 :
                        a >= 16 ? 0.5 :
                            a >= 11 ? 0.45 : 0.4;

            el.style.background =
                'radial-gradient(circle at ' + x + '% ' + y + '%,' +
                '  transparent 0%,' +
                '  transparent 40%,' +
                '  rgba(0,0,0,0.15) 75%,' +
                '  rgba(0,0,0,' + edgeDark + ') 100%)';
        });
    }

    /* ============================================================
       3. 老显示器闪动
       ============================================================ */
    function injectFlicker() {
        const el = document.createElement('div');
        el.style.cssText =
            'position:fixed;inset:0;z-index:9995;pointer-events:none;' +
            'background:#fff;opacity:0;mix-blend-mode:overlay;';
        document.body.appendChild(el);

        function flick() {
            const delay = 8000 + Math.random() * 22000;
            setTimeout(function () {
                el.style.opacity = '0.02';
                setTimeout(function () { el.style.opacity = '0'; }, 40);
                setTimeout(function () {
                    el.style.opacity = '0.015';
                    setTimeout(function () { el.style.opacity = '0'; }, 30);
                }, 80);
                flick();
            }, delay);
        }
        flick();
    }

    /* ============================================================
       4. 空闲检测（鼠标 15 秒不动）
       ============================================================ */
    function trackIdle() {
        setInterval(function () {
            const idleTime = Date.now() - H.lastMouseMove;
            const isIdle = idleTime > 15000;

            /* 空闲超过 15 秒 → 边缘泛红 */
            document.body.style.boxShadow =
                isIdle ? 'inset 0 0 200px rgba(120,0,0,0.15)' : '';

            /* 空闲 30 秒 → 触发一次"看"的效果 */
            if (idleTime > 30000 && idleTime < 31000) {
                onPlayerIdle();
            }
        }, 1000);
    }

    function onPlayerIdle() {
        if (Math.random() < 0.5) {
            const old = document.title;
            document.title = '……';
            setTimeout(function () { document.title = old; }, 1500);
        }
    }

    /* ============================================================
       5. 键盘 / 鼠标音效（Web Audio 合成，不依赖文件）
       ============================================================ */
    function initAudioContext() {
        try {
            H.audioCtx = new (window.AudioContext || window.webkitAudioContext)();
        } catch (e) { }
    }

    function playKeyClick(volume) {
        if (!H.soundEnabled || !H.audioCtx) return;
        try {
            const osc = H.audioCtx.createOscillator();
            const gain = H.audioCtx.createGain();
            osc.type = 'square';
            osc.frequency.value = 800 + Math.random() * 400;
            gain.gain.setValueAtTime(volume, H.audioCtx.currentTime);
            gain.gain.exponentialRampToValueAtTime(0.001, H.audioCtx.currentTime + 0.03);
            osc.connect(gain);
            gain.connect(H.audioCtx.destination);
            osc.start();
            osc.stop(H.audioCtx.currentTime + 0.03);
        } catch (e) { }
    }

    function injectTypingSound() {
        document.addEventListener('keydown', function (e) {
            if (e.ctrlKey || e.metaKey || e.altKey) return;
            playKeyClick(0.02);
        });
        document.addEventListener('click', function () {
            playKeyClick(0.015);
        });
    }

    /* ============================================================
       6. 页面标题会被改
       ============================================================ */
    function titleWhisper() {
        const original = document.title;

        setInterval(function () {
            const a = Store.get('attention', 0);
            if (a < 11) return;
            if (Math.random() < 0.15) {
                document.title = '回来了';
                setTimeout(function () { document.title = original; }, 400);
            }
        }, 20000);
    }

    /* ============================================================
       7. 滚动条也会动
       ============================================================ */
    function injectScrollbarWhisper() {
        document.addEventListener('scroll', function () {
            const a = Store.get('attention', 0);
            if (a >= 16 && Math.random() < 0.1) {
                const now = window.scrollY;
                window.scrollTo(0, now + (Math.random() > 0.5 ? 1 : -1));
            }
        });
    }

    /* ============================================================
       8. 隐藏水印（玩家名字）
       ============================================================ */
    function injectWatermark() {
        const name = Store.get('visitor_name', '');
        if (!name) return;

        setInterval(function () {
            if (Math.random() > 0.3) return;

            const wm = document.createElement('div');
            wm.textContent = name;
            wm.style.cssText =
                'position:fixed;' +
                'left:' + (Math.random() * 90) + '%;' +
                'top:' + (Math.random() * 90) + '%;' +
                'z-index:9994;pointer-events:none;' +
                'color:rgba(0,0,0,0.025);' +
                'font-family:SimSun,serif;' +
                'font-size:' + (40 + Math.random() * 40) + 'px;' +
                'font-weight:bold;' +
                'transform:rotate(' + (Math.random() * 60 - 30) + 'deg);' +
                'white-space:nowrap;';
            document.body.appendChild(wm);

            setTimeout(function () { wm.remove(); }, 3000 + Math.random() * 4000);
        }, 25000);
    }

    /* ============================================================
       9. 页面"呼吸"（注意值 16+ 时整页轻微呼吸）
       ============================================================ */
    function injectBreathing() {
        setInterval(function () {
            const a = Store.get('attention', 0);
            if (a < 16) {
                document.body.classList.remove('attention-breathing');
                return;
            }
            if (a >= 16 && a < 21) {
                document.body.classList.add('attention-breathing');
            }
        }, 3000);
    }

    /* ============================================================
       10. 鼠标轨迹（注意值 6+ 时淡红线条跟随）
       ============================================================ */
    function injectMouseTrail() {
        const canvas = document.createElement('canvas');
        canvas.id = 'horrorTrailCanvas';
        canvas.style.cssText =
            'position:fixed;inset:0;pointer-events:none;z-index:9993;';
        document.body.appendChild(canvas);

        const ctx = canvas.getContext('2d');
        let w = canvas.width = window.innerWidth;
        let h = canvas.height = window.innerHeight;

        window.addEventListener('resize', function () {
            w = canvas.width = window.innerWidth;
            h = canvas.height = window.innerHeight;
        });

        const points = [];

        document.addEventListener('mousemove', function (e) {
            const a = Store.get('attention', 0);
            if (a < 6) return;
            points.push({ x: e.clientX, y: e.clientY, t: Date.now() });
            if (points.length > 30) points.shift();
        });

        function draw() {
            ctx.clearRect(0, 0, w, h);
            const a = Store.get('attention', 0);
            if (a < 6) { requestAnimationFrame(draw); return; }

            const alpha = Math.min(0.15, (a - 5) / 100);
            ctx.strokeStyle = 'rgba(120,0,0,' + alpha + ')';
            ctx.lineWidth = 1;

            for (let i = 1; i < points.length; i++) {
                const p1 = points[i - 1];
                const p2 = points[i];
                const age = (Date.now() - p2.t) / 2000;
                if (age > 1) continue;
                ctx.beginPath();
                ctx.moveTo(p1.x, p1.y);
                ctx.lineTo(p2.x, p2.y);
                ctx.stroke();
            }

            /* 移除过期点 */
            while (points.length && Date.now() - points[0].t > 2000) {
                points.shift();
            }

            requestAnimationFrame(draw);
        }
        draw();
    }

    /* ============================================================
       11. 页面"漏出"东西（注意值 26+ 时偶尔在帖子列表上显示红字）
       ============================================================ */
    function injectLeaks() {
        setInterval(function () {
            const a = Store.get('attention', 0);
            if (a < 26) return;
            if (Math.random() > 0.08) return;

            /* 在页面某处插入一行极淡的红字 */
            const leaks = [
                '你 在 看 什 么',
                '别 看 了',
                '我 在 这 里',
                '回 头',
                '你 身 后 有 人'
            ];

            const d = document.createElement('div');
            d.textContent = leaks[Math.floor(Math.random() * leaks.length)];
            d.style.cssText =
                'position:fixed;' +
                'left:' + (10 + Math.random() * 80) + '%;' +
                'top:' + (10 + Math.random() * 80) + '%;' +
                'color:rgba(120,0,0,0.15);' +
                'font-family:SimSun,serif;' +
                'font-size:' + (16 + Math.random() * 20) + 'px;' +
                'font-weight:bold;' +
                'letter-spacing:4px;' +
                'z-index:9992;' +
                'pointer-events:none;' +
                'transform:rotate(' + (Math.random() * 40 - 20) + 'deg);';
            document.body.appendChild(d);

            setTimeout(function () { d.remove(); }, 2000);
        }, 15000);
    }

    /* ============================================================
       12. 音效控制（供其他模块调用）
       ============================================================ */
    window.HorrorAudio = {
        enable: function () { H.soundEnabled = true; },
        disable: function () { H.soundEnabled = false; },
        isEnabled: function () { return H.soundEnabled; },
        playClick: function () { playKeyClick(0.02); }
    };

    /* ============================================================
       初始化
       ============================================================ */
    document.addEventListener('DOMContentLoaded', function () {
        injectScanlines();
        injectVignette();
        injectFlicker();
        trackIdle();
        injectTypingSound();
        titleWhisper();
        injectScrollbarWhisper();
        injectWatermark();
        injectBreathing();
        injectMouseTrail();
        injectLeaks();

        /* 首次点击时初始化 AudioContext */
        document.addEventListener('click', function initAudioOnce() {
            initAudioContext();
            document.removeEventListener('click', initAudioOnce);
        }, { once: true });

        /* 页面离开时暂停所有动画（避免内存泄漏） */
        window.addEventListener('beforeunload', function () {
            if (H.audioCtx) {
                try { H.audioCtx.close(); } catch (e) { }
            }
        });
    });

})();