/* ============================================================
   回响档案 · 自动全屏
   文件：wwwroot/hxda/js/fullscreen.js
   加载顺序：2
   ============================================================ */

(function () {

    'use strict';

    var isMobile = /Mobi|Android|iPhone|iPad|iPod/i.test(navigator.userAgent);
    var isIOS = /iPhone|iPad|iPod/i.test(navigator.userAgent);

    function isFullscreenSupported() {
        return !!(
            document.documentElement.requestFullscreen ||
            document.documentElement.webkitRequestFullscreen ||
            document.documentElement.msRequestFullscreen ||
            document.documentElement.mozRequestFullScreen
        );
    }

    function isInFullscreen() {
        return !!(
            document.fullscreenElement ||
            document.webkitFullscreenElement ||
            document.msFullscreenElement ||
            document.mozFullScreenElement
        );
    }

    function requestFullscreen() {
        var el = document.documentElement;
        try {
            if (el.requestFullscreen) el.requestFullscreen().catch(function () { });
            else if (el.webkitRequestFullscreen) el.webkitRequestFullscreen();
            else if (el.msRequestFullscreen) el.msRequestFullscreen();
            else if (el.mozRequestFullScreen) el.mozRequestFullScreen();
        } catch (e) { }
    }

    function exitFullscreen() {
        try {
            if (document.exitFullscreen) document.exitFullscreen().catch(function () { });
            else if (document.webkitExitFullscreen) document.webkitExitFullscreen();
            else if (document.msExitFullscreen) document.msExitFullscreen();
            else if (document.mozCancelFullScreen) document.mozCancelFullScreen();
        } catch (e) { }
    }

    function showHint() {
        if (document.getElementById('fullscreenHint')) {
            document.getElementById('fullscreenHint').classList.add('show');
            return;
        }
        var bar = document.createElement('div');
        bar.id = 'fullscreenHint';
        bar.className = 'fullscreen-hint show';
        bar.innerHTML =
            '<span>⚠ 你退出了全屏。档案不需要小窗口。</span>' +
            '<span class="hint-btn" id="fsHintBtn">重新进入全屏</span>' +
            '<span class="hint-btn" id="fsHintDismiss" style="margin-left:6px;background:#333;">忽略</span>';
        document.body.appendChild(bar);
        document.getElementById('fsHintBtn').addEventListener('click', function () {
            requestFullscreen();
            setTimeout(function () {
                if (isInFullscreen()) bar.classList.remove('show');
            }, 300);
        });
        document.getElementById('fsHintDismiss').addEventListener('click', function () {
            bar.classList.remove('show');
        });
    }

    function hideHint() {
        var bar = document.getElementById('fullscreenHint');
        if (bar) bar.classList.remove('show');
    }

    var firstInteractionDone = Store.get('fullscreen_auto_shown', false);

    function onFirstInteraction() {
        if (firstInteractionDone) return;
        firstInteractionDone = true;
        Store.set('fullscreen_auto_shown', true);
        if (isMobile) return;
        if (isInFullscreen()) return;
        requestFullscreen();
    }

    if (!isMobile) {
        document.addEventListener('click', onFirstInteraction);
        document.addEventListener('keydown', onFirstInteraction);
    }

    if (!isMobile) {
        var events = ['fullscreenchange', 'webkitfullscreenchange', 'mozfullscreenchange', 'MSFullscreenChange'];
        var lastInFullscreen = isInFullscreen();
        events.forEach(function (evt) {
            document.addEventListener(evt, function () {
                var nowIn = isInFullscreen();
                if (lastInFullscreen && !nowIn) {
                    showHint();
                    Store.set('fullscreen_hint_shown', true);
                } else if (!lastInFullscreen && nowIn) {
                    hideHint();
                }
                lastInFullscreen = nowIn;
            });
        });
    }

    window.addEventListener('beforeunload', function () {
        if (isInFullscreen()) exitFullscreen();
    });

    window.FullscreenHelper = {
        request: requestFullscreen,
        exit: exitFullscreen,
        isInFullscreen: isInFullscreen,
        isSupported: isFullscreenSupported,
        isMobile: isMobile,
        showHint: showHint,
        hideHint: hideHint
    };

    document.addEventListener('DOMContentLoaded', function () {
        if (isMobile || isInFullscreen() || !isFullscreenSupported()) return;
        if (Store.get('fullscreen_hint_shown', false)) return;

        var btn = document.createElement('div');
        btn.id = 'fsManualBtn';
        btn.title = '进入全屏';
        btn.textContent = '⛶';
        btn.style.cssText =
            'position:fixed;bottom:10px;right:10px;width:28px;height:28px;' +
            'line-height:26px;text-align:center;' +
            'background:rgba(26,58,107,0.7);color:#fff;font-size:16px;' +
            'cursor:pointer;z-index:99998;border:1px solid #0d1b3e;' +
            'user-select:none;opacity:0.4;transition:opacity 0.3s;';
        btn.addEventListener('mouseenter', function () { btn.style.opacity = '1'; });
        btn.addEventListener('mouseleave', function () { btn.style.opacity = '0.4'; });
        btn.addEventListener('click', function () { requestFullscreen(); });
        document.body.appendChild(btn);
    });

})();