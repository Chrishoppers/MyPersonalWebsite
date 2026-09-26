/* ============================================================
   回响档案 · 图片查看器 + 音频播放器
   文件：wwwroot/hxda/js/viewer.js
   ============================================================ */

(function () {

    'use strict';

    /* ============================================================
       图片查看器
       ============================================================ */
    (function initViewer() {

        var overlay = document.getElementById('viewerOverlay');
        if (!overlay) return;

        var img = document.getElementById('viewerImage');
        var imgWrap = document.getElementById('viewerImageWrap');
        var bright = document.getElementById('ctrlBright');
        var contrast = document.getElementById('ctrlContrast');
        var scale = document.getElementById('ctrlScale');
        var info = document.getElementById('viewerInfo');
        var fileNameEl = document.getElementById('viewerFileName');

        var invert = false;
        var rotate = 0;
        var openSeconds = 0;
        var viewerTimer = null;

        var IMG_SOURCES = {
            'notebook_default.jpg': 'img/notebook_default.jpg',
            'notebook_bright.jpg': 'img/notebook_bright.jpg',
            'notebook_invert.jpg': 'img/notebook_invert.jpg',
            'notebook_rotate.jpg': 'img/notebook_rotate.jpg',
            'hand_1.png': 'img/hand_1.png',
            'hand_2.png': 'img/hand_2.png',
            'hand_3.png': 'img/hand_3.png'
        };

        function setImage(fileName) {
            img.src = IMG_SOURCES[fileName] || ('img/' + fileName);
            fileNameEl.textContent = fileName;
        }

        document.getElementById('viewerClose').addEventListener('click', closeViewer);
        overlay.addEventListener('click', function (e) {
            if (e.target === overlay) closeViewer();
        });

        function closeViewer() {
            overlay.classList.remove('active');
            if (viewerTimer) { clearInterval(viewerTimer); viewerTimer = null; }

            bright.value = 50;
            contrast.value = 50;
            scale.value = 100;
            invert = false;
            rotate = 0;
            info.style.display = 'none';
            applyFilter();
            setImage('notebook_default.jpg');

            var ov = document.getElementById('viewerHandOverlay');
            if (ov) ov.classList.remove('show');
        }

        function applyFilter() {
            var s = scale.value / 100;
            img.style.transform = 'rotate(0deg) scale(' + s + ')';
        }

        bright.addEventListener('input', function () {
            applyFilter();

            var v = parseInt(bright.value, 10);
            if (v >= 90) {
                var cnt = Store.get('viewer_adjust_bright_count', 0) + 1;
                Store.set('viewer_adjust_bright_count', cnt);
                addAttention(1);
                setImage('notebook_bright.jpg');
            } else {
                setImage('notebook_default.jpg');
            }
        });

        contrast.addEventListener('input', function () {
            applyFilter();

            var v = parseInt(contrast.value, 10);
            if (v >= 90) {
                var cnt = Store.get('viewer_adjust_contrast_count', 0) + 1;
                Store.set('viewer_adjust_contrast_count', cnt);
                addAttention(1);
                showHandOverlay('img/hand_1.png');
            } else {
                hideHandOverlay();
            }
        });

        scale.addEventListener('input', function () {
            applyFilter();
            var cnt = Store.get('viewer_adjust_scale_count', 0) + 1;
            Store.set('viewer_adjust_scale_count', cnt);
        });

        document.getElementById('btnInvert').addEventListener('click', function () {
            invert = !invert;
            if (invert) {
                setImage('notebook_invert.jpg');
                showHandOverlay('img/hand_2.png');
                Store.set('viewer_invert_done', true);
                addAttention(1);
            } else {
                setImage('notebook_default.jpg');
                hideHandOverlay();
            }
        });

        document.getElementById('btnRotate').addEventListener('click', function () {
            rotate = (rotate + 180) % 360;
            if (rotate === 180) {
                setImage('notebook_rotate.jpg');
                Store.set('viewer_rotate_done', true);
                addAttention(1);
            } else {
                setImage('notebook_default.jpg');
            }
        });

        setInterval(function () {
            var cnt = Store.get('viewer_adjust_bright_count', 0);
            if (cnt >= 5 && overlay.classList.contains('active')) {
                showHandOverlay('img/hand_3.png');
                setTimeout(hideHandOverlay, 2500);
            }
        }, 5000);

        function showHandOverlay(src) {
            var ov = document.getElementById('viewerHandOverlay');
            if (!ov) {
                ov = document.createElement('div');
                ov.id = 'viewerHandOverlay';
                ov.className = 'viewer-hand-overlay';
                var wrap = document.getElementById('viewerImageWrap');
                if (wrap) wrap.appendChild(ov);
            }
            ov.style.backgroundImage = 'url("' + src + '")';
            ov.classList.add('show');
        }

        function hideHandOverlay() {
            var ov = document.getElementById('viewerHandOverlay');
            if (ov) ov.classList.remove('show');
        }

        document.getElementById('btnFileInfo').addEventListener('click', function () {
            if (info.style.display === 'block') {
                info.style.display = 'none';
            } else {
                info.style.display = 'block';
                info.innerHTML =
                    '文件名：' + fileNameEl.textContent + '<br>' +
                    '拍摄设备：EchoSystem v0.9<br>' +
                    '创建时间：2009-11-01 00:00<br>' +
                    '分辨率：1024x768<br>' +
                    '备注：节点 1';
            }
        });

        window.ViewerHelper = window.ViewerHelper || {};
        window.ViewerHelper.openImage = function (fileName, src) {
            fileNameEl.textContent = fileName || 'notebook_default.jpg';
            img.src = src || 'img/notebook_default.jpg';

            img.onerror = function () {
                this.onerror = null;
                this.src = "data:image/svg+xml;utf8," +
                    "<svg xmlns='http://www.w3.org/2000/svg' width='600' height='400'>" +
                    "<rect width='600' height='400' fill='#222'/>" +
                    "<text x='300' y='180' font-family='SimSun,serif' font-size='16' fill='#888' text-anchor='middle'>" +
                    fileName + "</text>" +
                    "<text x='300' y='210' font-family='SimSun,serif' font-size='12' fill='#666' text-anchor='middle'>" +
                    "（本地图片未提供，功能仍可用）</text>" +
                    "<text x='300' y='260' font-family='SimSun,serif' font-size='14' fill='#a00' text-anchor='middle'>" +
                    "试着调亮度、对比度、反色、旋转</text>" +
                    "</svg>";
            };

            overlay.classList.add('active');
            openSeconds = 0;
            if (viewerTimer) clearInterval(viewerTimer);
            viewerTimer = setInterval(function () {
                openSeconds++;
                Store.set('viewer_open_seconds', openSeconds);
                if (openSeconds === 120) {
                    img.style.filter = 'brightness(0.3)';
                }
            }, 1000);
        };

    })();

    /* ============================================================
       音频播放器
       ============================================================ */
    (function initAudio() {

        var overlay = document.getElementById('audioOverlay');
        if (!overlay) return;

        var fill = document.getElementById('audioFill');
        var cur = document.getElementById('audioCur');
        var track = document.getElementById('audioTrack');

        var audio = null;
        var playing = false;
        var reverse = false;
        var speed = 1;
        var fakeTimer = null;
        var fakeCur = 0;

        document.getElementById('audioClose').addEventListener('click', closeAudio);
        overlay.addEventListener('click', function (e) {
            if (e.target === overlay) closeAudio();
        });

        function closeAudio() {
            overlay.classList.remove('active');
            stopAll();
        }

        function stopAll() {
            playing = false;
            if (fakeTimer) {
                clearInterval(fakeTimer);
                fakeTimer = null;
            }
            if (audio) {
                try { audio.pause(); } catch (e) { }
            }
        }

        document.getElementById('btnPlay').addEventListener('click', function () {
            playing = true;
            if (audio) {
                audio.playbackRate = speed;
                audio.play().catch(function () { });
            }
            startFakeProgress();
            addAttention(1);
            Store.set('audio_opened', true);
        });

        document.getElementById('btnPause').addEventListener('click', function () {
            playing = false;
            if (audio) {
                try { audio.pause(); } catch (e) { }
            }
            if (fakeTimer) {
                clearInterval(fakeTimer);
                fakeTimer = null;
            }
            if (reverse) {
                showAudioNotice('你打断了它。');
            }
        });

        function startFakeProgress() {
            if (fakeTimer) clearInterval(fakeTimer);
            fakeTimer = setInterval(function () {
                if (!playing) return;

                fakeCur += 1 * speed;
                if (fakeCur >= 43) {
                    fakeCur = 43;
                    playing = false;
                    clearInterval(fakeTimer);
                    fakeTimer = null;
                }

                var pct = (fakeCur / 43) * 100;
                fill.style.width = pct + '%';
                cur.textContent = '00:' + String(Math.floor(fakeCur)).padStart(2, '0');
            }, 1000);
        }

        document.getElementById('btnSpeed05').addEventListener('click', function () {
            speed = 0.5;
            if (audio) audio.playbackRate = 0.5;
        });
        document.getElementById('btnSpeed1').addEventListener('click', function () {
            speed = 1;
            if (audio) audio.playbackRate = 1;
        });
        document.getElementById('btnSpeed15').addEventListener('click', function () {
            speed = 1.5;
            if (audio) audio.playbackRate = 1.5;
        });
        document.getElementById('btnSpeed2').addEventListener('click', function () {
            speed = 2;
            if (audio) audio.playbackRate = 2;
        });

        document.getElementById('btnReverse').addEventListener('click', function () {
            reverse = true;
            var cnt = Store.get('audio_reverse_count', 0) + 1;
            Store.set('audio_reverse_count', cnt);
            Store.set('reverse_audio', true);
            addAttention(2);

            try {
                if (audio) {
                    try { audio.pause(); } catch (e) { }
                }
                audio = new Audio('audio/whisper_reverse.wav');
                audio.volume = 0.6;
                audio.play().catch(function () { });
            } catch (e) { }

            if (cnt >= 3) {
                showAudioNotice('你叫什么？', 5000);
            } else {
                showAudioNotice('不要留名。\n不要回头。\n不要回答。', 5000);
            }

            fakeCur = 0;
            fill.style.width = '0%';
            cur.textContent = '00:00';
            playing = true;
            startFakeProgress();
        });

        track.addEventListener('click', function (e) {
            var rect = track.getBoundingClientRect();
            var pct = (e.clientX - rect.left) / rect.width;
            fakeCur = pct * 43;
            fill.style.width = (pct * 100) + '%';
            cur.textContent = '00:' + String(Math.floor(fakeCur)).padStart(2, '0');
        });

        function showAudioNotice(text, ms) {
            var old = document.getElementById('audioNotice');
            if (old) old.remove();

            var el = document.createElement('div');
            el.id = 'audioNotice';
            el.style.cssText =
                'position:absolute;top:60px;left:50%;transform:translateX(-50%);' +
                'background:rgba(0,0,0,0.75);color:#ff6;padding:8px 16px;' +
                'font-family:SimSun,serif;font-size:13px;' +
                'pointer-events:none;' +
                'white-space:pre-wrap;' +
                'text-align:center;' +
                'border:1px solid #a00;';
            el.textContent = text;

            var box = overlay.querySelector('.overlay-box');
            if (box) box.appendChild(el);

            setTimeout(function () { el.remove(); }, ms || 3000);
        }

        window.ViewerHelper = window.ViewerHelper || {};
        window.ViewerHelper.openAudio = function () {
            overlay.classList.add('active');
            Store.set('audio_opened', true);
            addAttention(1);
        };

    })();

})();