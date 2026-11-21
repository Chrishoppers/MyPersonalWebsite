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
            var wrap = document.querySelector('.forum-wrap');

            if (a < 16) {
                document.body.classList.remove('attention-breathing');
                if (wrap) wrap.classList.remove('attention-breathing');
                return;
            }
            if (a >= 16 && a < 21) {
                document.body.classList.add('attention-breathing');
                if (wrap) wrap.classList.add('attention-breathing');
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
                '你 身 后 有 人',
                '第 43 页 还 在',
                '你 的 名 字 我 记 得',
                '书 签 里 多 了 什 么',
                '这 不 是 第 一 次',
                '你 已 经 来 过',
                '它 在 等 你 回 答',
                '别 关 掉 这 个 窗 口',
                '你 的 鼠 标 在 动 吗',
                '它 就 在 你 后 面',
                '我 看 得 见 你 的 屏 幕',
                '窗 外 有 人 在 看 你'
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
       13. 鼠标指针偶尔消失（注意值 11+ 时，你会短暂失去对鼠标的控制）
       ============================================================ */
    function injectCursorGlitch() {
        setInterval(function () {
            var a = Store.get('attention', 0);
            if (a < 11) return;
            if (Math.random() > 0.12) return;
            document.body.style.cursor = 'none';
            setTimeout(function () {
                document.body.style.cursor = '';
            }, 200 + Math.random() * 400);
        }, 12000);
    }

    /* ============================================================
       14. 跳吓（注意值 21+，随机全屏鬼脸 + 尖叫）
       ============================================================ */
    function playScream() {
        if (!H.audioCtx) return;
        try {
            var t = H.audioCtx.currentTime;
            var osc = H.audioCtx.createOscillator();
            var gain = H.audioCtx.createGain();
            osc.type = 'sawtooth';
            osc.frequency.setValueAtTime(700, t);
            osc.frequency.exponentialRampToValueAtTime(1800, t + 0.12);
            osc.frequency.exponentialRampToValueAtTime(240, t + 0.45);
            gain.gain.setValueAtTime(0.001, t);
            gain.gain.exponentialRampToValueAtTime(0.65, t + 0.03);
            gain.gain.exponentialRampToValueAtTime(0.001, t + 0.55);
            osc.connect(gain); gain.connect(H.audioCtx.destination);
            osc.start(t); osc.stop(t + 0.55);
        } catch (e) { }
    }

    function injectJumpScare() {
        var scareImages = ['ghost.png', 'face1.png', 'face2.png', 'face3.png', 'skull.png', 'demon.png', 'scream.png', 'blood_eyes.png'];
        function scare() {
            var a = Store.get('attention', 0);
            if (a < 21) return;
            var overlay = document.createElement('div');
            overlay.style.cssText = 'position:fixed;inset:0;z-index:999999;background:#000;display:flex;align-items:center;justify-content:center;pointer-events:none;overflow:hidden;';
            var img = document.createElement('img');
            img.src = '/images/horror/' + scareImages[Math.floor(Math.random() * scareImages.length)];
            img.style.cssText = 'width:100vw;height:100vh;object-fit:cover;filter:contrast(1.35) brightness(0.65) saturate(1.2);';
            img.onerror = function () { this.style.display = 'none'; };
            overlay.appendChild(img);
            document.body.appendChild(overlay);
            playScream();
            var start = Date.now();
            var shake = setInterval(function () {
                var e = (Date.now() - start) / 300;
                if (e >= 1) { clearInterval(shake); overlay.remove(); return; }
                overlay.style.transform = 'translate(' + ((Math.random() - 0.5) * 24) + 'px,' + ((Math.random() - 0.5) * 24) + 'px) scale(' + (1 + Math.random() * 0.06) + ')';
            }, 30);
        }
        function loop() {
            scare();
            setTimeout(loop, 50000 + Math.random() * 80000);
        }
        setTimeout(loop, 40000 + Math.random() * 40000);
    }

    /* ============================================================
       15. 眼睛跟着鼠标（注意值 11+ 时右下角出现，瞳孔追踪）
       ============================================================ */
    function injectEyes() {
        var c = document.createElement('canvas');
        c.id = 'horrorEyes';
        c.style.cssText = 'position:fixed;right:18px;bottom:18px;z-index:9991;pointer-events:none;opacity:0;transition:opacity 1.5s;';
        document.body.appendChild(c);
        var ctx = c.getContext('2d');
        c.width = 120; c.height = 56;

        function draw() {
            var a = Store.get('attention', 0);
            c.style.opacity = a >= 11 ? '1' : '0';
            ctx.clearRect(0, 0, 120, 56);
            if (a < 11) { requestAnimationFrame(draw); return; }
            var ex = window.innerWidth - 78, ey = window.innerHeight - 46;
            var dx = H.mouseX - ex, dy = H.mouseY - ey;
            var ang = Math.atan2(dy, dx);
            var px = Math.cos(ang) * 6, py = Math.sin(ang) * 4;
            var red = a >= 21;
            var blink = (Date.now() % 4000) < 90;
            for (var i = 0; i < 2; i++) {
                var ox = 30 + i * 60;
                ctx.fillStyle = red ? 'rgba(190,25,25,0.55)' : 'rgba(255,255,255,0.35)';
                ctx.beginPath(); ctx.ellipse(ox, 28, 22, blink ? 3 : 20, 0, 0, Math.PI * 2); ctx.fill();
                if (!blink) {
                    ctx.fillStyle = red ? '#a00' : '#000';
                    ctx.beginPath(); ctx.arc(ox + px, 28 + py, 8, 0, Math.PI * 2); ctx.fill();
                }
            }
            requestAnimationFrame(draw);
        }
        draw();
    }

    /* ============================================================
       16. 假系统提示（摄像头/麦克风/位置）
       ============================================================ */
    function injectCameraParanoia() {
        var msgs = [
            '📷 摄像头已开启',
            '🎙️ 麦克风正在录制',
            '🖥️ 屏幕正在被共享',
            '📍 位置已获取',
            '🔍 正在扫描你的文件夹',
            '👁️ 有人正在观看此页面'
        ];
        function show() {
            var a = Store.get('attention', 0);
            if (a < 16) return;
            var d = document.createElement('div');
            d.style.cssText =
                'position:fixed;top:16px;right:16px;z-index:9999;background:rgba(0,0,0,0.85);' +
                'color:#f66;font-family:"Courier New",monospace;font-size:12px;padding:6px 12px;' +
                'border-radius:6px;border:1px solid rgba(255,0,0,0.4);pointer-events:none;';
            d.textContent = msgs[Math.floor(Math.random() * msgs.length)];
            document.body.appendChild(d);
            setTimeout(function () { d.remove(); }, 2200);
        }
        setInterval(function () {
            if (Math.random() < 0.3) show();
        }, 20000);
    }

    /* ============================================================
       17. 低语（合成耳语声）
       ============================================================ */
    function injectWhisper() {
        function whisper() {
            var a = Store.get('attention', 0);
            if (a < 16 || !H.audioCtx) return;
            try {
                var t = H.audioCtx.currentTime;
                var buf = H.audioCtx.createBuffer(1, Math.floor(H.audioCtx.sampleRate * 2.2), H.audioCtx.sampleRate);
                var data = buf.getChannelData(0);
                for (var i = 0; i < data.length; i++) {
                    data[i] = (Math.random() * 2 - 1) * 0.06 * (0.4 + 0.6 * Math.sin(i / 28));
                }
                var src = H.audioCtx.createBufferSource(); src.buffer = buf;
                var f = H.audioCtx.createBiquadFilter(); f.type = 'bandpass';
                f.frequency.setValueAtTime(750, t);
                f.frequency.linearRampToValueAtTime(1500, t + 1.2);
                f.frequency.linearRampToValueAtTime(550, t + 2.2);
                var g = H.audioCtx.createGain();
                g.gain.setValueAtTime(0.13, t);
                g.gain.exponentialRampToValueAtTime(0.001, t + 2.2);
                src.connect(f); f.connect(g); g.connect(H.audioCtx.destination);
                src.start(t); src.stop(t + 2.2);
            } catch (e) { }
        }
        setInterval(function () {
            if (Math.random() < 0.4) whisper();
        }, 28000);
    }

    /* ============================================================
       18. 假蓝屏（注意值 26+，模拟系统崩溃）
       ============================================================ */
    function injectFakeCrash() {
        function crash() {
            var a = Store.get('attention', 0);
            if (a < 26) return;
            var d = document.createElement('div');
            d.style.cssText =
                'position:fixed;inset:0;z-index:1000000;background:#0a7bff;color:#fff;' +
                'font-family:"Segoe UI",Arial,sans-serif;padding:8vh 8vw;pointer-events:none;';
            d.innerHTML =
                '<div style="font-size:8rem;">:(</div>' +
                '<div style="font-size:1.6rem;margin-top:1rem;">你的电脑遇到问题，需要重新启动。</div>' +
                '<div style="font-size:1rem;margin-top:0.6rem;color:rgba(255,255,255,0.85);">我们只收集某些错误信息，然后为你重新启动。</div>' +
                '<div style="font-size:1rem;margin-top:2rem;color:rgba(255,255,255,0.7);"><span id="crashPct">0% 完成</span></div>';
            document.body.appendChild(d);
            if (window.GameAudio) GameAudio.once('static', { volume: 0.25 });
            var pct = 0;
            var iv = setInterval(function () {
                pct += 2 + Math.random() * 7;
                var p = Math.floor(Math.min(100, pct));
                var el = document.getElementById('crashPct');
                if (el) el.textContent = p + '% 完成';
                if (p >= 100) {
                    clearInterval(iv);
                    setTimeout(function () { d.remove(); }, 350);
                }
            }, 110);
        }
        setInterval(function () {
            if (Math.random() < 0.08) crash();
        }, 30000);
    }

    /* ============================================================
       19. 归档倒计时（注意值 26+，10 秒倒数后归档）
       ============================================================ */
    function injectArchiveCountdown() {
        function countdown() {
            var a = Store.get('attention', 0);
            if (a < 26) return;
            var d = document.createElement('div');
            d.style.cssText =
                'position:fixed;inset:0;z-index:1000000;background:#000;' +
                'display:flex;flex-direction:column;align-items:center;justify-content:center;pointer-events:none;';
            var txt = document.createElement('div');
            txt.style.cssText =
                'color:#f00;font-size:2.2rem;font-family:"Courier New",monospace;' +
                'font-weight:900;letter-spacing:0.2em;text-align:center;padding:0 1rem;';
            var num = document.createElement('div');
            num.style.cssText =
                'color:#f00;font-size:6rem;font-family:"Courier New",monospace;' +
                'font-weight:900;margin-top:1rem;text-shadow:0 0 40px rgba(255,0,0,0.8);';
            d.appendChild(txt); d.appendChild(num);
            document.body.appendChild(d);
            var t = 10;
            num.textContent = t;
            txt.textContent = '你将在 ' + t + ' 秒后被归档';
            var iv = setInterval(function () {
                t--;
                if (t <= 0) {
                    clearInterval(iv);
                    txt.textContent = '归档完成。';
                    num.style.fontSize = '3rem';
                    num.textContent = '节点 8：' + (Store.get('visitor_name', '') || '你');
                    if (window.GameAudio) GameAudio.once('static', { volume: 0.3 });
                    setTimeout(function () { d.remove(); }, 2500);
                    return;
                }
                txt.textContent = '你将在 ' + t + ' 秒后被归档';
                num.textContent = t;
                d.style.background = (Math.random() < 0.5) ? '#1a0000' : '#000';
            }, 1000);
        }
        setInterval(function () {
            if (Math.random() < 0.06) countdown();
        }, 40000);
    }

    /* ============================================================
       20. 姓名侵蚀（注意值 16+，页面文字突然变成你的真名）
       ============================================================ */
    function injectNameCorruption() {
        function corrupt() {
            var a = Store.get('attention', 0);
            if (a < 16) return;
            var name = Store.get('gate_signature_name', '') || Store.get('visitor_name', '');
            if (!name) return;
            var walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT, {
                acceptNode: function (node) {
                    var p = node.parentElement;
                    if (p && !p.closest('script') && !p.closest('style') && node.textContent.trim().length > 2) {
                        return NodeFilter.FILTER_ACCEPT;
                    }
                    return NodeFilter.FILTER_REJECT;
                }
            });
            var nodes = [];
            var n;
            while (n = walker.nextNode()) nodes.push(n);
            if (!nodes.length) return;
            var target = nodes[Math.floor(Math.random() * nodes.length)];
            var orig = target.textContent;
            var parent = target.parentElement;
            target.textContent = name;
            if (parent) parent.style.color = 'rgba(160,0,0,0.75)';
            setTimeout(function () {
                target.textContent = orig;
                if (parent) parent.style.color = '';
            }, 1600);
        }
        setInterval(function () {
            if (Math.random() < 0.25) corrupt();
        }, 15000);
    }

    /* ============================================================
       21. 幽灵光标（注意值 21+，一个红点光标自己乱动）
       ============================================================ */
    function injectGhostCursor() {
        var el = document.createElement('div');
        el.style.cssText =
            'position:fixed;width:14px;height:14px;border-radius:50%;' +
            'background:rgba(200,0,0,0.7);z-index:9998;pointer-events:none;' +
            'box-shadow:0 0 12px rgba(255,0,0,0.6);display:none;';
        document.body.appendChild(el);
        function move() {
            var a = Store.get('attention', 0);
            if (a < 21) return;
            el.style.display = 'block';
            var x = Math.random() * window.innerWidth, y = Math.random() * window.innerHeight;
            el.style.left = x + 'px'; el.style.top = y + 'px';
            var steps = 12, i = 0;
            var iv = setInterval(function () {
                i++;
                el.style.left = (x + (Math.random() - 0.5) * 120) + 'px';
                el.style.top = (y + (Math.random() - 0.5) * 120) + 'px';
                if (i >= steps) { clearInterval(iv); el.style.display = 'none'; }
            }, 70);
        }
        setInterval(function () {
            if (Math.random() < 0.2) move();
        }, 18000);
    }

    /* ============================================================
       22. 假摄像头画面（注意值 21+，黑暗画面里一个模糊人影）
       ============================================================ */
    function injectCameraFeed() {
        function feed() {
            var a = Store.get('attention', 0);
            if (a < 21) return;
            var d = document.createElement('div');
            d.style.cssText = 'position:fixed;inset:0;z-index:1000000;background:#000;pointer-events:none;overflow:hidden;';
            var c = document.createElement('canvas');
            c.width = window.innerWidth; c.height = window.innerHeight;
            c.style.cssText = 'position:absolute;inset:0;width:100%;height:100%;';
            d.appendChild(c);
            var rec = document.createElement('div');
            rec.style.cssText = 'position:absolute;top:18px;left:18px;color:#f00;font-family:"Courier New",monospace;font-size:16px;z-index:2;';
            rec.innerHTML = '● REC 00:00:13';
            d.appendChild(rec);
            var ts = document.createElement('div');
            ts.style.cssText = 'position:absolute;bottom:18px;left:18px;color:rgba(255,255,255,0.4);font-family:"Courier New",monospace;font-size:12px;z-index:2;';
            ts.textContent = 'CAM_03 · ' + new Date().toLocaleString();
            d.appendChild(ts);
            document.body.appendChild(d);

            var ctx = c.getContext('2d');
            var start = Date.now();
            var duration = 4200;
            function frame() {
                var e = (Date.now() - start) / duration;
                if (e >= 1) { d.remove(); return; }
                ctx.fillStyle = '#050505';
                ctx.fillRect(0, 0, c.width, c.height);
                for (var i = 0; i < 320; i++) {
                    var g = Math.floor(Math.random() * 60);
                    ctx.fillStyle = 'rgb(' + g + ',' + g + ',' + g + ')';
                    ctx.fillRect(Math.random() * c.width, Math.random() * c.height, 1.5, 1.5);
                }
                ctx.save();
                ctx.filter = 'blur(18px)';
                var cx = c.width / 2 + Math.sin(Date.now() / 300) * 9;
                var cy = c.height * 0.62;
                var sc = 1 + Math.sin(Date.now() / 500) * 0.04;
                ctx.fillStyle = 'rgba(75,75,88,0.6)';
                ctx.beginPath(); ctx.arc(cx, cy - 92 * sc, 27 * sc, 0, Math.PI * 2); ctx.fill();
                ctx.beginPath(); ctx.ellipse(cx, cy, 42 * sc, 82 * sc, 0, 0, Math.PI * 2); ctx.fill();
                ctx.restore();
                requestAnimationFrame(frame);
            }
            frame();
        }
        setInterval(function () {
            if (Math.random() < 0.1) feed();
        }, 35000);
    }

    /* ============================================================
       23. 页面倒转（注意值 16+，整页 180° 翻转）
       ============================================================ */
    function injectFlip() {
        function flip() {
            var a = Store.get('attention', 0);
            if (a < 16) return;
            document.body.style.transition = 'transform 0.5s ease-in-out';
            document.body.style.transform = 'rotate(180deg)';
            setTimeout(function () {
                document.body.style.transition = 'transform 0.5s ease-in-out';
                document.body.style.transform = '';
            }, 2400);
        }
        setInterval(function () {
            if (Math.random() < 0.1) flip();
        }, 25000);
    }

    /* ============================================================
       24. 文字消失（注意值 21+，页面文字一行行消失）
       ============================================================ */
    function injectLineErasure() {
        function erase() {
            var a = Store.get('attention', 0);
            if (a < 21) return;
            var walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT, {
                acceptNode: function (node) {
                    var p = node.parentElement;
                    if (p && !p.closest('script') && !p.closest('style') && node.textContent.trim().length > 1) {
                        return NodeFilter.FILTER_ACCEPT;
                    }
                    return NodeFilter.FILTER_REJECT;
                }
            });
            var nodes = [];
            var n;
            while (n = walker.nextNode()) nodes.push(n);
            if (!nodes.length) return;
            var count = 6 + Math.floor(Math.random() * 10);
            var chosen = [];
            for (var i = 0; i < count && i < nodes.length; i++) {
                chosen.push(nodes[Math.floor(Math.random() * nodes.length)]);
            }
            chosen.forEach(function (t, idx) {
                var orig = t.textContent;
                setTimeout(function () {
                    t.textContent = '';
                    setTimeout(function () { t.textContent = orig; }, 1800 + Math.random() * 2200);
                }, idx * 160);
            });
        }
        setInterval(function () {
            if (Math.random() < 0.15) erase();
        }, 20000);
    }

    /* ============================================================
       25. 信号中断（注意值 21+，满屏雪花噪点）
       ============================================================ */
    function injectSignalLoss() {
        function loss() {
            var a = Store.get('attention', 0);
            if (a < 21) return;
            var d = document.createElement('div');
            d.style.cssText =
                'position:fixed;inset:0;z-index:1000000;pointer-events:none;' +
                'background:repeating-linear-gradient(0deg,#000 0px,#000 2px,#fff 2px,#fff 3px,#000 3px,#000 5px);' +
                'mix-blend-mode:screen;opacity:0.9;';
            document.body.appendChild(d);
            if (window.GameAudio) GameAudio.once('static', { volume: 0.35 });
            setTimeout(function () { d.remove(); }, 420);
        }
        setInterval(function () {
            if (Math.random() < 0.12) loss();
        }, 25000);
    }

    /* ============================================================
       26. 文字乱码（注意值 16+，页面文字变成乱码）
       ============================================================ */
    function injectTextGlitch() {
        var glitchChars = '▓▒░█▀▄▌▐■□◢◣◤◥#%&@!?/\\|<>{}[]~☠';
        function glitch() {
            var a = Store.get('attention', 0);
            if (a < 16) return;
            var walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT, {
                acceptNode: function (node) {
                    var p = node.parentElement;
                    if (p && !p.closest('script') && !p.closest('style') && node.textContent.trim().length > 1) {
                        return NodeFilter.FILTER_ACCEPT;
                    }
                    return NodeFilter.FILTER_REJECT;
                }
            });
            var nodes = [];
            var n;
            while (n = walker.nextNode()) nodes.push(n);
            if (!nodes.length) return;
            var count = 8 + Math.floor(Math.random() * 12);
            var chosen = [];
            for (var i = 0; i < count; i++) {
                chosen.push(nodes[Math.floor(Math.random() * nodes.length)]);
            }
            chosen.forEach(function (t) {
                var orig = t.textContent;
                var glitched = '';
                for (var j = 0; j < orig.length; j++) {
                    if (orig[j] !== ' ' && orig[j] !== '\n' && Math.random() < 0.5) {
                        glitched += glitchChars[Math.floor(Math.random() * glitchChars.length)];
                    } else {
                        glitched += orig[j];
                    }
                }
                t.textContent = glitched;
                setTimeout(function () { t.textContent = orig; }, 1100 + Math.random() * 1500);
            });
        }
        setInterval(function () {
            if (Math.random() < 0.25) glitch();
        }, 12000);
    }

    /* ============================================================
       27. 血往下流（注意值 21+，屏幕顶部流下血痕）
       ============================================================ */
    function injectBloodDrip() {
        function drip() {
            var a = Store.get('attention', 0);
            if (a < 21) return;
            var c = document.createElement('canvas');
            c.width = window.innerWidth; c.height = window.innerHeight;
            c.style.cssText = 'position:fixed;inset:0;z-index:9997;pointer-events:none;';
            document.body.appendChild(c);
            var ctx = c.getContext('2d');
            var drops = [];
            for (var i = 0; i < 14; i++) {
                drops.push({ x: Math.random() * c.width, y: -30 - Math.random() * c.height, len: 40 + Math.random() * 130, w: 2 + Math.random() * 5, sp: 3 + Math.random() * 5 });
            }
            var start = Date.now();
            function frame() {
                var e = (Date.now() - start) / 3000;
                if (e >= 1) { c.remove(); return; }
                ctx.clearRect(0, 0, c.width, c.height);
                ctx.fillStyle = 'rgba(110,0,0,0.55)';
                ctx.fillRect(0, 0, c.width, 5);
                ctx.fillStyle = 'rgba(130,0,0,0.7)';
                for (var i = 0; i < drops.length; i++) {
                    var d = drops[i];
                    d.y += d.sp;
                    if (d.y > c.height) { d.y = -30 - Math.random() * c.height; d.x = Math.random() * c.width; }
                    ctx.fillRect(d.x, d.y, d.w, d.len);
                }
                requestAnimationFrame(frame);
            }
            frame();
        }
        setInterval(function () {
            if (Math.random() < 0.12) drip();
        }, 30000);
    }

    /* ============================================================
       28. 高频尖啸（注意值 26+，突然刺耳高频）
       ============================================================ */
    function injectShriek() {
        function shriek() {
            var a = Store.get('attention', 0);
            if (a < 26 || !H.audioCtx) return;
            try {
                var t = H.audioCtx.currentTime;
                var osc = H.audioCtx.createOscillator();
                var gain = H.audioCtx.createGain();
                osc.type = 'sine';
                osc.frequency.setValueAtTime(1200, t);
                osc.frequency.exponentialRampToValueAtTime(5200, t + 0.5);
                osc.frequency.exponentialRampToValueAtTime(180, t + 0.9);
                gain.gain.setValueAtTime(0.001, t);
                gain.gain.exponentialRampToValueAtTime(0.38, t + 0.02);
                gain.gain.exponentialRampToValueAtTime(0.001, t + 1);
                osc.connect(gain); gain.connect(H.audioCtx.destination);
                osc.start(t); osc.stop(t + 1);
            } catch (e) { }
        }
        setInterval(function () {
            if (Math.random() < 0.07) shriek();
        }, 35000);
    }

    /* ============================================================
       29. 全屏反色闪（注意值 16+，画面瞬间反色）
       ============================================================ */
    function injectNegativeFlash() {
        function neg() {
            var a = Store.get('attention', 0);
            if (a < 16) return;
            document.body.style.filter = 'invert(1) hue-rotate(90deg)';
            setTimeout(function () { document.body.style.filter = ''; }, 180 + Math.random() * 250);
        }
        setInterval(function () {
            if (Math.random() < 0.12) neg();
        }, 18000);
    }

    /* ============================================================
       30. 输入框自动打字（注意值 21+，输入框自己出现文字）
       ============================================================ */
    function injectAutoType() {
        function type() {
            var a = Store.get('attention', 0);
            if (a < 21) return;
            var inputs = document.querySelectorAll('input[type="text"], textarea');
            if (!inputs.length) return;
            var el = inputs[Math.floor(Math.random() * inputs.length)];
            var name = Store.get('visitor_name', '') || Store.get('gate_signature_name', '') || '你是谁';
            var msgs = ['你是谁', '我看到你了', name, '不要回答', '救命', '我在你后面', '它来了'];
            var text = msgs[Math.floor(Math.random() * msgs.length)];
            var i = 0;
            try { el.focus(); } catch (e) { }
            var iv = setInterval(function () {
                i++;
                el.value = text.slice(0, i);
                if (i >= text.length) {
                    clearInterval(iv);
                    setTimeout(function () { el.value = ''; try { el.blur(); } catch (e) { } }, 2500);
                }
            }, 120);
        }
        setInterval(function () {
            if (Math.random() < 0.12) type();
        }, 25000);
    }

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
        injectCursorGlitch();
        injectJumpScare();
        injectEyes();
        injectCameraParanoia();
        injectWhisper();
        injectFakeCrash();
        injectArchiveCountdown();
        injectNameCorruption();
        injectGhostCursor();
        injectCameraFeed();
        injectFlip();
        injectLineErasure();
        injectSignalLoss();
        injectTextGlitch();
        injectBloodDrip();
        injectShriek();
        injectNegativeFlash();
        injectAutoType();

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