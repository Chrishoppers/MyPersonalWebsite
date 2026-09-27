/* ============================================================
   回响档案 · 启动门禁
   文件：wwwroot/hxda/js/gate.js
   依赖：storage.js、fullscreen.js、ui.js、audio.js
   方案 B：每次打开 gate 都重新走门禁
   ============================================================ */

(function () {
    'use strict';

    var CONFIG = {
        OPEN_HOUR: 20,
        CLOSE_HOUR: 5,
        MIC_DB_THRESHOLD: 50,
        MIC_STABLE_SECONDS: 3,
        ADMIN_CODE: 'echo-admin'
    };

    var stepTime = document.getElementById('stepTime');
    var stepMic = document.getElementById('stepMic');
    var stepNotice = document.getElementById('stepNotice');
    var stepSign = document.getElementById('stepSign');
    var stepPass = document.getElementById('stepPass');

    function showStep(el) {
        [stepTime, stepMic, stepNotice, stepSign, stepPass].forEach(function (s) {
            if (s) s.style.display = 'none';
        });
        if (el) el.style.display = 'block';
        window.scrollTo(0, 0);
    }

    /* ============================================================
       竖屏拦截
       ============================================================ */
    function isMobileDevice() {
        var ua = navigator.userAgent || '';
        return /Mobi|Android|iPhone|iPad|iPod|Windows Phone/i.test(ua);
    }

    function isPhoneScreen() {
        var shortSide = Math.min(window.screen.width, window.screen.height);
        return shortSide < 600;
    }

    function isPortrait() {
        if (window.matchMedia) {
            return window.matchMedia('(orientation: portrait)').matches;
        }
        return window.innerHeight > window.innerWidth;
    }

    function shouldBlockOrientation() {
        if (Store.get('admin_logged_in', false)) return false;
        if (!isMobileDevice()) return false;
        if (!isPhoneScreen()) return false;
        if (!isPortrait()) return false;
        return true;
    }

    function showOrientationBlock() {
        var el = document.getElementById('orientationBlock');
        if (el) el.classList.add('active');
    }

    function hideOrientationBlock() {
        var el = document.getElementById('orientationBlock');
        if (el) el.classList.remove('active');
    }

    function resumeStep() {
        if (!Store.get('gate_time_passed', false)) {
            renderTimeStep();
            showStep(stepTime);
            return;
        }
        if (!Store.get('gate_mic_passed', false)) {
            goMic();
            return;
        }
        if (!Store.get('gate_read_done', false)) {
            goNotice();
            return;
        }
        if (!Store.get('gate_signature_name', '')) {
            goSign();
            return;
        }
        goPass();
    }

    function onOrientationChange() {
        if (shouldBlockOrientation()) {
            showOrientationBlock();
        } else {
            var block = document.getElementById('orientationBlock');
            var wasBlocked = block && block.classList.contains('active');
            hideOrientationBlock();
            if (wasBlocked) resumeStep();
        }
    }

    /* ============================================================
       步骤 1：时间检测
       ============================================================ */
    function checkTime() {
        if (Store.get('admin_logged_in', false)) {
            Store.set('gate_time_passed', true);
            return { pass: true, reason: 'admin' };
        }

        var now = new Date();
        var h = now.getHours();
        var open = CONFIG.OPEN_HOUR;
        var close = CONFIG.CLOSE_HOUR;

        var pass;
        if (open > close) {
            pass = (h >= open) || (h < close);
        } else {
            pass = (h >= open) && (h < close);
        }

        return { pass: pass, hour: h };
    }

    function renderTimeStep() {
        var r = checkTime();

        if (r.pass) {
            stepTime.innerHTML =
                '<div class="gate-box">' +
                '  <div class="gate-head">环境测试 · 时间</div>' +
                '  <div class="gate-body">' +
                '    <p>当前时间：' + String(r.hour).padStart(2, '0') + ':00</p>' +
                '    <p class="gate-ok">时间检测通过。</p>' +
                '    <div class="gate-actions">' +
                '      <button class="old-btn old-btn-primary" id="btnTimeNext">继续</button>' +
                '      <button class="old-btn" id="btnTimeHome">返回首页</button>' +
                '    </div>' +
                '  </div>' +
                '</div>';

            document.getElementById('btnTimeNext').addEventListener('click', function () {
                Store.set('gate_time_passed', true);
                if (window.GameAudio) GameAudio.once('click', { volume: 0.15 });
                goMic();
            });
            document.getElementById('btnTimeHome').addEventListener('click', function () {
                window.location.href = 'index.html';
            });
        } else {
            stepTime.innerHTML =
                '<div class="gate-box">' +
                '  <div class="gate-head">环境测试 · 时间</div>' +
                '  <div class="gate-body">' +
                '    <p>当前时间：' + String(r.hour).padStart(2, '0') + ':00</p>' +
                '    <p class="gate-warn">档案尚未开放。</p>' +
                '    <p>开放时间：每日 <b>20:00</b> 至次日 <b>05:00</b>。</p>' +
                '    <p class="gate-tiny">请在开放时间重新访问。</p>' +
                '    <div class="gate-actions">' +
                '      <button class="old-btn old-btn-primary" id="btnTimeRetry">重新检测</button>' +
                '      <button class="old-btn" id="btnTimeHome">返回首页</button>' +
                '    </div>' +
                '  </div>' +
                '</div>';

            document.getElementById('btnTimeRetry').addEventListener('click', renderTimeStep);
            document.getElementById('btnTimeHome').addEventListener('click', function () {
                window.location.href = 'index.html';
            });
        }
    }

    /* ============================================================
       步骤 2：麦克风分贝检测
       ============================================================ */
    var micStream = null;
    var micCtx = null;
    var micRaf = null;

    function stopMic() {
        if (micRaf) { cancelAnimationFrame(micRaf); micRaf = null; }
        if (micStream) {
            micStream.getTracks().forEach(function (t) { try { t.stop(); } catch (e) { } });
            micStream = null;
        }
        if (micCtx) {
            try { micCtx.close(); } catch (e) { }
            micCtx = null;
        }
    }

    function goMic() {
        showStep(stepMic);

        stepMic.innerHTML =
            '<div class="gate-box">' +
            '  <div class="gate-head">环境测试 · 麦克风</div>' +
            '  <div class="gate-body">' +
            '    <p>本测试需要检测环境安静程度。</p>' +
            '    <p>麦克风仅在测试期间开启，测试完成后立即关闭。</p>' +
            '    <p>音频不会被录制、保存、上传或分析内容。</p>' +
            '    <p class="gate-tiny">安静阈值：低于 ' + CONFIG.MIC_DB_THRESHOLD + ' dB，持续 ' + CONFIG.MIC_STABLE_SECONDS + ' 秒。</p>' +
            '    <div class="gate-actions">' +
            '      <button class="old-btn old-btn-primary" id="btnMicStart">开始检测</button>' +
            '      <button class="old-btn" id="btnMicHome">返回首页</button>' +
            '    </div>' +
            '    <div id="micMeter" style="display:none;margin-top:14px;">' +
            '      <div style="background:#ddd;height:14px;border:1px solid #888;position:relative;">' +
            '        <div id="micFill" style="background:#1a3a6b;height:100%;width:0%;transition:width 0.1s;"></div>' +
            '      </div>' +
            '      <div id="micDbText" style="font-size:12px;color:#666;margin-top:6px;">-- dB</div>' +
            '      <div id="micStatus" style="font-size:12px;color:#666;margin-top:4px;"></div>' +
            '    </div>' +
            '    <div id="micMsg" class="msg"></div>' +
            '  </div>' +
            '</div>';

        document.getElementById('btnMicStart').addEventListener('click', startMicTest);
        document.getElementById('btnMicHome').addEventListener('click', function () {
            stopMic();
            window.location.href = 'index.html';
        });
    }

    function startMicTest() {
        var btn = document.getElementById('btnMicStart');
        btn.disabled = true;
        btn.textContent = '检测中...';

        navigator.mediaDevices.getUserMedia({ audio: true })
            .then(function (stream) {
                micStream = stream;
                micCtx = new (window.AudioContext || window.webkitAudioContext)();
                var source = micCtx.createMediaStreamSource(stream);
                var analyser = micCtx.createAnalyser();
                analyser.fftSize = 2048;
                source.connect(analyser);

                var data = new Uint8Array(analyser.frequencyBinCount);
                var stableCount = 0;

                document.getElementById('micMeter').style.display = 'block';
                var fill = document.getElementById('micFill');
                var dbText = document.getElementById('micDbText');
                var status = document.getElementById('micStatus');

                function loop() {
                    analyser.getByteTimeDomainData(data);
                    var sum = 0;
                    for (var i = 0; i < data.length; i++) {
                        var v = (data[i] - 128) / 128;
                        sum += v * v;
                    }
                    var rms = Math.sqrt(sum / data.length);
                    var db = Math.max(0, Math.min(100, 20 * Math.log10(rms + 1e-6) + 100));

                    fill.style.width = Math.min(100, db) + '%';
                    dbText.textContent = db.toFixed(1) + ' dB';

                    Store.set('gate_mic_db', db);

                    if (db < CONFIG.MIC_DB_THRESHOLD) {
                        stableCount++;
                        status.textContent = '环境安静。保持 ' +
                            (CONFIG.MIC_STABLE_SECONDS - Math.floor(stableCount / 30)) + ' 秒...';
                        if (stableCount >= CONFIG.MIC_STABLE_SECONDS * 30) {
                            onMicPass();
                            return;
                        }
                    } else {
                        stableCount = 0;
                        status.textContent = '环境太吵。请保持安静。';
                    }

                    micRaf = requestAnimationFrame(loop);
                }
                loop();
            })
            .catch(function (err) {
                stopMic();
                document.getElementById('micMsg').textContent =
                    '无法访问麦克风。请检查浏览器权限后重试。';
                btn.disabled = false;
                btn.textContent = '重新检测';
                Store.set('gate_mic_test_count', Store.get('gate_mic_test_count', 0) + 1);
            });
    }

    function onMicPass() {
        stopMic();
        Store.set('gate_mic_passed', true);
        Store.set('gate_mic_test_count', Store.get('gate_mic_test_count', 0) + 1);

        if (window.GameAudio) GameAudio.once('click', { volume: 0.15 });

        document.getElementById('micStatus').textContent = '检测通过。麦克风已关闭。';
        document.getElementById('micFill').style.background = '#0a0';

        setTimeout(function () {
            goNotice();
        }, 1200);
    }

    /* ============================================================
       步骤 3：须知确认
       ============================================================ */
    var NOTICE_ITEMS = [
        { key: 'health', text: '我确认自己没有光敏性癫痫、心脏病、严重心理疾病史。' },
        { key: 'age', text: '我确认自己年龄在 16 岁以上。' },
        { key: 'mic', text: '我了解麦克风仅用于环境分贝检测，测试后立即关闭，不会被录制或上传。' },
        { key: 'privacy', text: '我了解本游戏数据仅存储在本地 localStorage 中。' },
        { key: 'beta', text: '我了解当前为内测版本，不代表最终品质。' },
        { key: 'secret', text: '我承诺不将本测试内容外传、录屏、截图或公开讨论。' },
        { key: 'stop', text: '我了解如果感到不适，应立即停止测试。' }
    ];

    function goNotice() {
        showStep(stepNotice);

        var itemsHtml = NOTICE_ITEMS.map(function (it) {
            return '<label class="gate-check">' +
                '<input type="checkbox" data-key="' + it.key + '" disabled> ' +
                it.text +
                '</label>';
        }).join('');

        stepNotice.innerHTML =
            '<div class="gate-box gate-notice">' +
            '  <div class="gate-head">测试者须知</div>' +
            '  <div class="gate-scroll" id="noticeScroll">' +
            NOTICE_BODY_HTML +
            '  </div>' +
            '  <div class="gate-body">' +
            '    <p class="gate-tiny" id="noticeHint">请滚动到底部以继续。</p>' +
            '    <div id="noticeChecks" style="opacity:0.4;pointer-events:none;">' +
            itemsHtml +
            '    </div>' +
            '    <div class="gate-actions">' +
            '      <button class="old-btn old-btn-primary" id="btnNoticeNext" disabled>继续</button>' +
            '      <button class="old-btn" id="btnNoticeHome">返回首页</button>' +
            '    </div>' +
            '  </div>' +
            '</div>';

        var scrollEl = document.getElementById('noticeScroll');
        var checksEl = document.getElementById('noticeChecks');
        var hintEl = document.getElementById('noticeHint');
        var nextBtn = document.getElementById('btnNoticeNext');

        var scrolledToBottom = false;

        scrollEl.addEventListener('scroll', function () {
            if (scrolledToBottom) return;
            if (scrollEl.scrollTop + scrollEl.clientHeight >= scrollEl.scrollHeight - 10) {
                scrolledToBottom = true;
                Store.set('gate_read_scrolled', true);
                hintEl.textContent = '请逐条勾选以下内容。';
                checksEl.style.opacity = '1';
                checksEl.style.pointerEvents = 'auto';
                checksEl.querySelectorAll('input').forEach(function (i) { i.disabled = false; });
            }
        });

        /* 诡异低语：滚动到附近浮现，离开后消失 */
        var whispers = scrollEl.querySelectorAll('.gate-whisper');
        if (whispers.length && window.IntersectionObserver) {
            var whisperObserver = new IntersectionObserver(function (entries) {
                entries.forEach(function (entry) {
                    if (entry.isIntersecting) {
                        entry.target.classList.add('show');
                    } else {
                        entry.target.classList.remove('show');
                    }
                });
            }, { root: scrollEl, threshold: 0.5 });
            whispers.forEach(function (w) { whisperObserver.observe(w); });
        } else {
            whispers.forEach(function (w) { w.classList.add('show'); });
        }

        checksEl.addEventListener('change', function () {
            var all = checksEl.querySelectorAll('input');
            var checked = checksEl.querySelectorAll('input:checked');
            var keys = [];
            checked.forEach(function (c) { keys.push(c.dataset.key); });
            Store.set('gate_read_checked', keys);

            nextBtn.disabled = (checked.length !== all.length);
            if (!nextBtn.disabled) {
                Store.set('gate_read_done', true);
            }
        });

        nextBtn.addEventListener('click', function () {
            if (window.GameAudio) GameAudio.once('click', { volume: 0.15 });
            goSign();
        });

        document.getElementById('btnNoticeHome').addEventListener('click', function () {
            window.location.href = 'index.html';
        });
    }

    /* ============================================================
       步骤 4：真实签名
       ============================================================ */
    function goSign() {
        showStep(stepSign);

        stepSign.innerHTML =
            '<div class="gate-box">' +
            '  <div class="gate-head">真实签名</div>' +
            '  <div class="gate-body">' +
            '    <p>请输入你的真实姓名，并在下方手写签名。</p>' +
            '    <p class="gate-tiny">签名仅存储在本地，不会上传。</p>' +
            '    <div style="margin:12px 0;">' +
            '      <label>姓名：</label>' +
            '      <input type="text" id="signName" maxlength="20" placeholder="请输入真实姓名" autocomplete="off">' +
            '    </div>' +
            '    <div style="margin:12px 0;">' +
            '      <label>手写签名：</label>' +
            '      <canvas id="signCanvas" width="400" height="140" ' +
            '              style="border:1px solid #888;background:#fff;display:block;margin-top:6px;cursor:crosshair;touch-action:none;"></canvas>' +
            '      <button class="old-btn" id="btnSignClear" style="margin-top:6px;">清除</button>' +
            '    </div>' +
            '    <div class="gate-tiny">日期：<span id="signDate"></span></div>' +
            '    <div id="signMsg" class="msg"></div>' +
            '    <div class="gate-actions">' +
            '      <button class="old-btn old-btn-primary" id="btnSignNext" disabled>完成</button>' +
            '      <button class="old-btn" id="btnSignHome">返回首页</button>' +
            '    </div>' +
            '  </div>' +
            '</div>';

        document.getElementById('signDate').textContent =
            new Date().toLocaleString('zh-CN');

        var canvas = document.getElementById('signCanvas');
        var ctx = canvas.getContext('2d');
        var drawing = false;
        var hasDrawn = false;

        ctx.strokeStyle = '#000';
        ctx.lineWidth = 2;
        ctx.lineCap = 'round';

        function pos(e) {
            var rect = canvas.getBoundingClientRect();
            var x = (e.touches ? e.touches[0].clientX : e.clientX) - rect.left;
            var y = (e.touches ? e.touches[0].clientY : e.clientY) - rect.top;
            return { x: x, y: y };
        }

        function start(e) {
            e.preventDefault();
            drawing = true;
            var p = pos(e);
            ctx.beginPath();
            ctx.moveTo(p.x, p.y);
        }
        function move(e) {
            if (!drawing) return;
            e.preventDefault();
            var p = pos(e);
            ctx.lineTo(p.x, p.y);
            ctx.stroke();
            hasDrawn = true;
            checkSignReady();
        }
        function end() {
            drawing = false;
        }

        canvas.addEventListener('mousedown', start);
        canvas.addEventListener('mousemove', move);
        canvas.addEventListener('mouseup', end);
        canvas.addEventListener('mouseleave', end);
        canvas.addEventListener('touchstart', start, { passive: false });
        canvas.addEventListener('touchmove', move, { passive: false });
        canvas.addEventListener('touchend', end);

        document.getElementById('btnSignClear').addEventListener('click', function () {
            ctx.clearRect(0, 0, canvas.width, canvas.height);
            hasDrawn = false;
            checkSignReady();
        });

        document.getElementById('signName').addEventListener('input', checkSignReady);

        function checkSignReady() {
            var name = document.getElementById('signName').value.trim();
            var valid = name.length >= 2 && hasDrawn;
            document.getElementById('btnSignNext').disabled = !valid;
        }

        document.getElementById('btnSignNext').addEventListener('click', function () {
            var name = document.getElementById('signName').value.trim();
            Store.set('gate_signature_name', name);
            Store.set('gate_signature_drawn', true);
            Store.set('gate_signature_date', new Date().toISOString());
            Store.set('gate_passed', true);
            if (window.GameAudio) GameAudio.once('page_turn', { volume: 0.3 });
            goPass();
        });

        document.getElementById('btnSignHome').addEventListener('click', function () {
            window.location.href = 'index.html';
        });
    }

    /* ============================================================
       步骤 5：通过
       ============================================================ */
    function goPass() {
        showStep(stepPass);
        stepPass.innerHTML =
            '<div class="gate-box">' +
            '  <div class="gate-head">通过</div>' +
            '  <div class="gate-body">' +
            '    <p>环境测试与须知确认已完成。</p>' +
            '    <p>正在进入档案...</p>' +
            '  </div>' +
            '</div>';

        setTimeout(function () {
            window.location.href = 'welcome.html';
        }, 1500);
    }

    /* ============================================================
       隐藏管理员入口
       ============================================================ */
    function initHiddenAdminEntry() {

        var aPressCount = 0;
        var aPressTimer = null;

        document.addEventListener('keydown', function (e) {
            var tag = (document.activeElement && document.activeElement.tagName) || '';
            if (tag === 'INPUT' || tag === 'TEXTAREA') return;

            if (e.key === 'a' || e.key === 'A') {
                aPressCount++;
                clearTimeout(aPressTimer);
                aPressTimer = setTimeout(function () {
                    aPressCount = 0;
                }, 2000);

                if (aPressCount >= 5) {
                    aPressCount = 0;
                    promptAdminCode();
                }
            }
        });

        var dot = document.getElementById('adminDot');
        if (dot) {
            dot.addEventListener('dblclick', function (e) {
                e.preventDefault();
                promptAdminCode();
            });
        }
    }

    function promptAdminCode() {
        if (Store.get('admin_logged_in', false)) {
            var again = confirm('管理员已登录。\n是否退出管理员模式？');
            if (again) {
                Store.set('admin_logged_in', false);
                alert('已退出管理员模式。');
            }
            return;
        }

        var code = prompt('请输入管理员密码：');
        if (code === null) return;

        if (code === CONFIG.ADMIN_CODE) {
            Store.set('admin_logged_in', true);
            Store.set('gate_time_passed', true);
            alert('管理员已登录。\n时间限制已解除。\n请继续完成麦克风检测。');

            /* 不跳转，直接进入麦克风检测 */
            goMic();
        } else {
            alert('密码错误。');
        }
    }

    /* ============================================================
       初始化
       方案 B：每次打开 gate 都重新走门禁
       ============================================================ */
    document.addEventListener('DOMContentLoaded', function () {

        /* --- 每次进入 gate 都从头开始 --- */
        Store.set('gate_time_passed', false);
        Store.set('gate_mic_passed', false);
        Store.set('gate_read_done', false);
        Store.set('gate_read_scrolled', false);
        Store.set('gate_read_checked', []);
        Store.set('gate_signature_name', '');
        Store.set('gate_signature_drawn', false);
        Store.set('gate_passed', false);

        /* --- 重置注意值（A+B 方案） --- */
        Store.set('attention', 0);
        Store.set('refresh_count', 0);
        Store.set('tab_switches', 0);
        Store.set('attention_level_shown', 0);

        initHiddenAdminEntry();

        if (shouldBlockOrientation()) {
            showOrientationBlock();
        } else {
            renderTimeStep();
            showStep(stepTime);
        }

        window.addEventListener('resize', onOrientationChange);
        window.addEventListener('orientationchange', onOrientationChange);

        var btnHome = document.getElementById('btnOrientationHome');
        if (btnHome) {
            btnHome.addEventListener('click', function () {
                window.location.href = 'index.html';
            });
        }
    });

})();