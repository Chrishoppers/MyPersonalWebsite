/* ============================================================
   回响档案 · 系统暗中介入
   文件：wwwroot/hxda/js/system-helper.js
   ============================================================ */

(function () {
    'use strict';

    var IDLE_THRESHOLD = 60000;
    var lastActivity = Date.now();
    var hintShown = false;

    var EXCLUDED_PAGES = ['index.html', 'gate.html', 'welcome.html', 'page43.html', 'admin.html'];

    function getPageName() {
        var path = location.pathname.split('/').pop() || 'bbs.html';
        return path.split('?')[0];
    }

    function isExcluded() {
        return EXCLUDED_PAGES.indexOf(getPageName()) !== -1;
    }

    function resetIdle() { lastActivity = Date.now(); }

    function getHint() {
        var page = getPageName();
        var params = new URLSearchParams(location.search);

        if (page === 'bbs.html') {
            if (!Store.get('read_post_1', false)) return '那本笔记本，或许值得一看。';
            if (!Store.get('read_post_3', false)) return '置顶帖里，有些规则。';
            if (!Store.get('verify_opened_count', 0)) return '关于本站里，有个页面校验。';
            if (!Store.get('found_node7', false)) return 'HTML 里，有些注释。';
            if (!Store.get('reverse_audio', false)) return '倒过来听。';
            if (!Store.get('arc_unlocked', false)) return '档案馆的钥匙，是建站的那一天。';
            if (!Store.get('cipher_used_count', 0)) return '有些乱码，不是垃圾。';
            if (!Store.get('read_nodes', false)) return '七个节点，每个都藏着一个字。';
            if (!Store.get('pm_lin_said_comein', false)) return '和林默聊聊。';
            if (!Store.get('discovered_whoami', false)) return '终端在等着你。';
            return '你已经知道答案了。';
        }

        if (page === 'post.html') {
            var pid = params.get('id') || '1';
            if (pid === '1') {
                if (!Store.get('viewer_adjust_bright_count', 0)) return '光，能照出一些东西。';
                if (!Store.get('reverse_audio', false)) return '倒过来听。';
                return '去找林默。';
            }
            if (pid === '4') return '档案馆的钥匙。';
            if (pid === '11') return '那串字符，不是乱码。';
            if (pid === '19') return '点和划，是一种语言。';
            if (pid === '21') return '平板也能看源代码。去 source.html。';
            if (pid === '23') return '竖着读第一行。';
            return '';
        }

        if (page === 'node.html') {
            var n = (Store.get('node_read', []) || []).length;
            if (n < 7) return '还有 ' + (7 - n) + ' 个。';
            if (!Store.get('node8_unlocked', false)) return '节点 8 的钥匙，是它自己。';
            return '你已经看完了。';
        }

        if (page === 'archive.html') {
            if (!Store.get('arc_unlocked', false)) return '2009-11-01';
            return '';
        }

        if (page === 'verify.html') {
            if (!Store.get('found_node7', false)) return 'HTML 源代码标签里，有些注释。';
            return '43x27。';
        }

        if (page === 'cipher.html') {
            if (!Store.get('cipher_used_count', 0)) return '试试 5Zue5Z+N5qGj5qGI。';
            return 'RPU-43-FRIRAGU 是 ROT13。';
        }

        if (page === 'source.html') {
            if (!Store.get('source_unlocked', false)) return '密钥是 2347。';
            return '';
        }

        if (page === 'search.html') return '试试搜索"回响档案"。';
        if (page === 'profile.html') return '和他聊聊。';
        if (page === 'pm.html') return '他一直在等。';
        if (page === 'about.html') {
            if (!Store.get('arc_unlocked', false)) return '档案馆的钥匙。';
            return '';
        }
        if (page === '404.html') return '404 也有故事。';
        if (page === 'settings.html') return '别动那些设置。';

        return '';
    }

    function checkIdle() {
        if (hintShown) return;
        if (isExcluded()) return;
        if (Date.now() - lastActivity < IDLE_THRESHOLD) return;

        var hint = getHint();
        if (!hint) { lastActivity = Date.now(); return; }

        triggerSystemHint(hint);
    }

    function triggerSystemHint(hint) {
        hintShown = true;
        var sysWrap = document.getElementById('sysWrap');
        var sysStatus = document.getElementById('sysStatus');

        if (sysWrap && sysStatus) sequenceOnHome(sysWrap, sysStatus, hint);
        else sequenceOffHome(hint);
    }

    function sequenceOnHome(sysWrap, sysStatus, hint) {
        sysWrap.style.opacity = '1';
        sysWrap.style.animation = 'sysPulse 1.2s ease-in-out infinite';

        setTimeout(function () {
            sysStatus.textContent = '█▓▒░';
            sysStatus.style.color = '#f00';
            sysStatus.style.animation = 'sysGlitchShake 0.12s infinite';
        }, 800);

        setTimeout(function () {
            sysStatus.textContent = '管理员-?';
            sysStatus.style.animation = '';
            sysStatus.style.color = '#f00';
            sysStatus.style.fontWeight = 'bold';
            sysStatus.style.textShadow = '0 0 8px rgba(255,0,0,0.6)';
        }, 2000);

        setTimeout(function () { showHintBubble(hint); }, 2600);

        setTimeout(function () {
            removeHintBubble();
            flashScreen(function () {
                sysStatus.textContent = '待机中';
                sysStatus.style.color = '#888';
                sysStatus.style.fontWeight = '';
                sysStatus.style.textShadow = '';
                sysStatus.style.fontStyle = 'italic';

                setTimeout(function () {
                    sysStatus.textContent = '已注销';
                    sysStatus.style.color = '#888';
                    sysStatus.style.fontStyle = '';
                    sysWrap.style.opacity = '0.5';
                    sysWrap.style.animation = '';
                    hintShown = false;
                    lastActivity = Date.now();
                }, 3000);
            });
        }, 7600);
    }

    function sequenceOffHome(hint) {
        var popup = document.createElement('div');
        popup.style.cssText = 'position:fixed;bottom:20px;right:20px;background:rgba(0,0,0,0.9);color:#0f0;font-family:"Courier New",monospace;font-size:12px;padding:12px 20px;border:1px solid #0f0;z-index:9999998;max-width:320px;line-height:1.6;box-shadow:0 0 20px rgba(0,255,0,0.3);';
        popup.textContent = '> ' + hint;
        document.body.appendChild(popup);

        setTimeout(function () {
            popup.style.transition = 'opacity 0.5s';
            popup.style.opacity = '0';
            setTimeout(function () { popup.remove(); }, 500);

            flashScreen(function () {
                hintShown = false;
                lastActivity = Date.now();
            });
        }, 5000);
    }

    function showHintBubble(text) {
        var old = document.getElementById('sysHintBubble');
        if (old) old.remove();

        var bubble = document.createElement('div');
        bubble.id = 'sysHintBubble';
        bubble.style.cssText = 'position:fixed;bottom:80px;left:50%;transform:translateX(-50%);background:rgba(0,0,0,0.9);color:#0f0;font-family:"Courier New",monospace;font-size:13px;padding:12px 28px;border:1px solid #0f0;z-index:9999998;letter-spacing:0.08em;box-shadow:0 0 30px rgba(0,255,0,0.4);';
        bubble.textContent = '> ' + text;
        document.body.appendChild(bubble);
    }

    function removeHintBubble() {
        var bubble = document.getElementById('sysHintBubble');
        if (!bubble) return;
        bubble.style.transition = 'opacity 0.5s';
        bubble.style.opacity = '0';
        setTimeout(function () { bubble.remove(); }, 500);
    }

    function flashScreen(callback) {
        var flash = document.createElement('div');
        flash.style.cssText = 'position:fixed;inset:0;background:#000;z-index:9999999;pointer-events:none;opacity:0;transition:opacity 0.1s;';
        document.body.appendChild(flash);

        var count = 0;
        var interval = setInterval(function () {
            flash.style.opacity = flash.style.opacity === '0.85' ? '0' : '0.85';
            count++;
            if (count >= 4) {
                clearInterval(interval);
                setTimeout(function () {
                    flash.remove();
                    if (callback) callback();
                }, 300);
            }
        }, 250);
    }

    ['click', 'keydown', 'scroll', 'touchstart', 'mousemove'].forEach(function (evt) {
        document.addEventListener(evt, resetIdle, true);
    });

    setInterval(checkIdle, 1000);

    window.SystemHelper = {
        trigger: function () { var h = getHint(); if (h) triggerSystemHint(h); },
        reset: function () { hintShown = false; lastActivity = Date.now(); }
    };

})();