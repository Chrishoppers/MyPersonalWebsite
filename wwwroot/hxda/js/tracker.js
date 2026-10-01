/* ============================================================
   回响档案 · 行为追踪 + 注意值
   文件：wwwroot/hxda/js/tracker.js
   加载顺序：3
   ============================================================ */

(function () {

    'use strict';

    window.addAttention = function (n) {
        var v = Store.get('attention', 0) + n;
        Store.set('attention', v);
        updateAttentionLevel(v);
        checkAttentionLevelPopup(v);
        return v;
    };

    window.getAttention = function () {
        return Store.get('attention', 0);
    };

    function updateAttentionLevel(v) {
        var level =
            v >= 26 ? 'extreme' :
                v >= 21 ? 'very-high' :
                    v >= 16 ? 'high' :
                        v >= 11 ? 'mid' :
                            v >= 6 ? 'low' : 'none';

            document.body.dataset.attention = String(v);
            document.body.dataset.attentionLevel = level;

            /* dark-mode 只改颜色，不改 transform / filter，可以留 body */
            if (v >= 21 && !document.body.classList.contains('dark-mode')) {
                document.body.classList.add('dark-mode');
            }

            /* transform / filter 类的效果，移到 .forum-wrap */
            var wrap = document.querySelector('.forum-wrap');
            if (wrap) {
                wrap.dataset.attention = String(v);
                wrap.dataset.attentionLevel = level;

                if (v >= 16) wrap.classList.add('attention-drift');
                else wrap.classList.remove('attention-drift');

                if (v >= 26) wrap.classList.add('attention-critical');
                else wrap.classList.remove('attention-critical');
            }
        }

    updateAttentionLevel(Store.get('attention', 0));
    setInterval(function () {
        updateAttentionLevel(Store.get('attention', 0));
    }, 3000);

    function checkAttentionLevelPopup(v) {
        var levels = [6, 11, 16, 21, 26];
        var shown = Store.get('attention_level_shown', 0);
        for (var i = 0; i < levels.length; i++) {
            var lv = levels[i];
            if (v >= lv && shown < lv) {
                Store.set('attention_level_shown', lv);
                showAttentionPopup(lv);
                break;
            }
        }
    }

    function showAttentionPopup(lv) {
        var msgs = {
            6: '您的访问已被多次记录。当前记录次数：6。请继续。',
            11: '当前记录次数：11。您开始被注意到了。',
            16: '当前记录次数：16。您已经被注意到了。',
            21: '当前记录次数：21。您已经被记住了。',
            26: '当前记录次数：26。您已经是档案的一部分了。'
        };
        var titleColors = {
            6: '#1a3a6b', 11: '#2a3a6b', 16: '#4a2a5b', 21: '#6b1a3b', 26: '#8b0a1b'
        };
        var d = document.createElement('div');
        d.style.cssText =
            'position:fixed;inset:0;background:rgba(0,0,0,0.7);z-index:99999;' +
            'display:flex;align-items:center;justify-content:center;';
        d.innerHTML =
            '<div style="background:#f4f4f4;border:2px solid #888;max-width:440px;width:90%;' +
            'font-family:SimSun,serif;color:#222;">' +
            '  <div style="background:' + (titleColors[lv] || '#1a3a6b') + ';color:#fff;padding:8px 14px;font-weight:bold;">访客记录</div>' +
            '  <div style="padding:18px 22px;line-height:1.9;">' +
            '    <p style="margin:0 0 14px;">' + msgs[lv] + '</p>' +
            '    <div style="text-align:right;">' +
            '      <button class="old-btn" data-ok="1">确定</button>' +
            '    </div>' +
            '  </div>' +
            '</div>';
        document.body.appendChild(d);
        d.querySelector('[data-ok]').addEventListener('click', function () { d.remove(); });
        setTimeout(function () {
            if (document.body.contains(d)) {
                d.remove();
                showAttentionPopupRepeat(lv);
            }
        }, 10000);
    }

    function showAttentionPopupRepeat(lv) {
        var closeCount = Store.get('attention_popup_close_count', 0) + 1;
        Store.set('attention_popup_close_count', closeCount);
        var msg = '您关闭了弹窗。但您关不掉档案。';
        if (closeCount < 3) msg = '您还没有点击确定。请点击确定。';
        var d = document.createElement('div');
        d.style.cssText =
            'position:fixed;inset:0;background:rgba(0,0,0,0.7);z-index:99999;' +
            'display:flex;align-items:center;justify-content:center;';
        d.innerHTML =
            '<div style="background:#f4f4f4;border:2px solid #888;max-width:440px;width:90%;' +
            'font-family:SimSun,serif;color:#222;">' +
            '  <div style="background:#6b1a1a;color:#fff;padding:8px 14px;font-weight:bold;">访客记录</div>' +
            '  <div style="padding:18px 22px;line-height:1.9;">' +
            '    <p style="margin:0 0 14px;">' + msg + '</p>' +
            '    <div style="text-align:right;">' +
            '      <button class="old-btn" data-ok="1">确定</button>' +
            '    </div>' +
            '  </div>' +
            '</div>';
        document.body.appendChild(d);
        d.querySelector('[data-ok]').addEventListener('click', function () { d.remove(); });
    }

    (function () {
        var rc = Store.get('refresh_count', 0) + 1;
        Store.set('refresh_count', rc);
        if ([3, 5, 7, 13, 43].indexOf(rc) !== -1) showRefreshPopup(rc);
        if (rc >= 3) {
            document.addEventListener('DOMContentLoaded', function () { addAttention(3); });
        }
    })();

    function showRefreshPopup(n) {
        var msgs = {
            3: '您刷新了3次。档案不需要刷新。档案一直在。',
            5: '您刷新了5次。它在等您停下来。',
            7: '您刷新了7次。这是阿回的次数。',
            13: '您刷新了13次。这是林默的次数。',
            43: '您刷新了43次。您就是第43页。'
        };
        if (n === 43) {
            setTimeout(function () {
                if (window.location.pathname.indexOf('page43') === -1) {
                    Store.set('ending_archive', true);
                    window.location.href = 'page43.html?archive=1';
                }
            }, 2000);
        }
        setTimeout(function () {
            var d = document.createElement('div');
            d.style.cssText =
                'position:fixed;inset:0;background:rgba(0,0,0,0.75);z-index:99999;' +
                'display:flex;align-items:center;justify-content:center;';
            d.innerHTML =
                '<div style="background:#f4f4f4;border:2px solid #888;max-width:440px;width:90%;' +
                'font-family:SimSun,serif;color:#222;">' +
                '  <div style="background:#1a3a6b;color:#fff;padding:8px 14px;font-weight:bold;">刷新记录</div>' +
                '  <div style="padding:18px 22px;line-height:1.9;">' +
                '    <p style="margin:0 0 14px;">' + msgs[n] + '</p>' +
                '    <div style="text-align:right;">' +
                '      <button class="old-btn" data-ok="1">确定</button>' +
                '    </div>' +
                '  </div>' +
                '</div>';
            document.body.appendChild(d);
            d.querySelector('[data-ok]').addEventListener('click', function () { d.remove(); });
        }, 800);
    }

    document.addEventListener('visibilitychange', function () {
        if (document.hidden) {
            Store.set('tab_switches', Store.get('tab_switches', 0) + 1);
        } else {
            var n = Store.get('tab_switches', 0);
            if (n >= 1) {
                addAttention(2);
                showTabPopup(n);
            }
        }
    });

    function showTabPopup(n) {
        var msgs = {
            1: '您刚才离开了。您现在回来了。欢迎回来。',
            2: '您又离开了。您又回来了。您为什么离开？',
            3: '您离开了3次。您每次都回来。您在找什么？',
            4: '别走了。留下来。',
            5: '您走不掉了。您已经在这里了。'
        };
        var text = msgs[Math.min(n, 5)];
        if (!text) return;
        var d = document.createElement('div');
        d.style.cssText =
            'position:fixed;top:20px;left:50%;transform:translateX(-50%);' +
            'background:#1a3a6b;color:#fff;padding:8px 20px;' +
            'font-family:SimSun,serif;font-size:13px;z-index:99999;' +
            'border:1px solid #0d1b3e;';
        d.textContent = text;
        document.body.appendChild(d);
        setTimeout(function () {
            d.style.transition = 'opacity 0.5s';
            d.style.opacity = '0';
            setTimeout(function () { d.remove(); }, 500);
        }, 5000);
    }

    var resizeCount = 0;
    var lastResizeTime = 0;
    window.addEventListener('resize', function () {
        var now = Date.now();
        if (now - lastResizeTime < 500) {
            resizeCount++;
            if (resizeCount >= 3) { resizeCount = 0; addAttention(1); }
        } else { resizeCount = 1; }
        lastResizeTime = now;
    });

    setInterval(function () { addAttention(1); }, 60000);

    window.oldForumPopup = function (title, message, buttons) {
        var d = document.createElement('div');
        d.style.cssText =
            'position:fixed;inset:0;background:rgba(0,0,0,0.7);z-index:99999;' +
            'display:flex;align-items:center;justify-content:center;';
        var btnsHtml = '';
        var list = buttons && buttons.length ? buttons : [{ text: '确定', cb: null }];
        list.forEach(function (b, i) {
            btnsHtml += '<button class="old-btn" data-i="' + i + '" style="margin-left:8px;">' + b.text + '</button>';
        });
        d.innerHTML =
            '<div style="background:#f4f4f4;border:2px solid #888;max-width:480px;width:90%;' +
            'font-family:SimSun,serif;color:#222;">' +
            '  <div style="background:#1a3a6b;color:#fff;padding:8px 14px;font-weight:bold;">' + title + '</div>' +
            '  <div style="padding:18px 22px;line-height:1.9;">' +
            '    <div style="margin:0 0 14px;white-space:pre-wrap;">' + message + '</div>' +
            '    <div style="text-align:right;">' + btnsHtml + '</div>' +
            '  </div>' +
            '</div>';
        document.body.appendChild(d);
        d.querySelectorAll('button').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var i = parseInt(btn.dataset.i);
                var cb = list[i] && list[i].cb;
                d.remove();
                if (cb) cb();
            });
        });
        return d;
    };

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-now]').forEach(function (el) {
            el.textContent = StoreUtils.nowFull();
        });
        document.querySelectorAll('[data-name]').forEach(function (el) {
            el.textContent = Store.get('visitor_name', '') || '无名';
        });
    });

})();