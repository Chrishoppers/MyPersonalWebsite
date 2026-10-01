/* ============================================================
   回响档案 · 7 个结局演出
   文件：wwwroot/hxda/js/endings.js
   依赖：storage.js、audio.js
   ============================================================ */

/* ============================================================
   结局后清理：保留结局标记，清掉过程数据
   ============================================================ */
function endingCleanupAndExit(delayMs) {
    delayMs = delayMs || 1500;

    setTimeout(function () {

        /* --- 要保留的键 --- */
        var KEEP = [
            'visitor_name',
            'admin_logged_in',
            'online_override',
            'ending_all_seen',
            'ending_archive',
            'ending_lookback',
            'ending_disconnect',
            'ending_refused',
            'ending_remembered',
            'ending_together',
            'ending_true'
        ];

        var kept = {};
        KEEP.forEach(function (k) {
            try { kept[k] = Store.get(k, undefined); } catch (e) { }
        });

        /* --- 全部清空 --- */
        try { Store.reset(); } catch (e) { }

        /* --- 恢复保留项 --- */
        Object.keys(kept).forEach(function (k) {
            if (kept[k] !== undefined) {
                try { Store.set(k, kept[k]); } catch (e) { }
            }
        });

        /* --- 退出到开场 --- */
        window.location.href = 'index.html';

    }, delayMs);
}

/* ============================================================
   通用：全屏结局文本
   ============================================================ */
function endingShowFullscreen(text, className, autoReturn, delaySec) {
    var d = document.createElement('div');
    d.className = className || 'ending-center';
    d.style.cssText =
        'position:fixed;inset:0;display:flex;flex-direction:column;' +
        'align-items:center;justify-content:center;' +
        'text-align:center;padding:40px;' +
        'font-family:SimSun,serif;font-size:20px;line-height:2;' +
        'white-space:pre-wrap;z-index:999999;' +
        'animation:endingFadeIn 0.8s ease;';

    if (className === 'ending-center') {
        d.style.background = '#000';
        d.style.color = '#eee';
    }
    if (className === 'ending-white') {
        d.style.background = '#fff';
        d.style.color = '#222';
    }
    if (className === 'ending-black') {
        d.style.background = '#000';
        d.style.color = '#fff';
    }

    d.innerHTML = text;
    document.body.appendChild(d);

    if (autoReturn !== false) {
        setTimeout(function () {
            if (d.querySelector('.ending-return-btn')) return;

            var btn = document.createElement('a');
            btn.className = 'ending-return-btn';
            btn.href = 'index.html';
            btn.textContent = '返回';
            btn.style.cssText =
                'display:inline-block;margin-top:32px;' +
                'padding:8px 32px;' +
                'background:' + (className === 'ending-white' ? '#e6e6e6' : '#222') + ';' +
                'color:' + (className === 'ending-white' ? '#222' : '#eee') + ';' +
                'border:1px solid ' + (className === 'ending-white' ? '#888' : '#555') + ';' +
                'text-decoration:none;font-family:inherit;font-size:14px;' +
                'cursor:pointer;';

            d.appendChild(document.createElement('br'));
            d.appendChild(btn);
        }, 3000);
    }

    if (delaySec && delaySec > 0) {
        setTimeout(function () {
            endingCleanupAndExit(0);
        }, delaySec * 1000);
    }

    return d;
}

(function injectEndingStyles() {
    if (document.getElementById('endingStyles')) return;
    var style = document.createElement('style');
    style.id = 'endingStyles';
    style.textContent =
        '@keyframes endingFadeIn {' +
        '  from { opacity: 0; }' +
        '  to   { opacity: 1; }' +
        '}' +
        '@keyframes endingGlitch {' +
        '  0%, 100% { transform: translate(0, 0); }' +
        '  50% { transform: translate(1px, -1px); }' +
        '}' +
        '@keyframes endingPulse {' +
        '  0%, 100% { opacity: 0.6; }' +
        '  50% { opacity: 1; }' +
        '}';
    document.head.appendChild(style);
})();

function endingPush(name) {
    var arr = Store.get('ending_all_seen', []);
    if (arr.indexOf(name) === -1) arr.push(name);
    Store.set('ending_all_seen', arr);
    return arr;
}

function endingClearAll() {
    document.body.innerHTML = '';
}

function endingFlashBlack(ms) {
    var d = document.createElement('div');
    d.style.cssText = 'position:fixed;inset:0;background:#000;z-index:999998;';
    document.body.appendChild(d);
    setTimeout(function () { d.remove(); }, ms);
}

/* ============================================================
   结局 1：归档
   ============================================================ */
function endingArchive() {
    Store.set('ending_archive', true);
    endingPush('archive');

    /* 新增上报 */
    if (window.HxdaReport && window.HxdaReport.event) {
        HxdaReport.event('ending', { name: 'archive' });
    }

    if (window.GameAudio) GameAudio.fadeIn('heartbeat', 2000, 0.25);

    endingClearAll();

    var name = Store.get('visitor_name', '') || '无名';

    var i = 0;
    var interval = setInterval(function () {
        var line = document.createElement('div');
        line.textContent = '已归档';
        line.style.cssText =
            'font-size:28px;color:#4a4;letter-spacing:0.3em;' +
            'margin:6px;font-family:SimSun,serif;' +
            'opacity:' + (0.3 + Math.random() * 0.7) + ';';
        document.body.appendChild(line);
        i++;

        if (i >= 40) {
            clearInterval(interval);
            setTimeout(function () {
                showFinalCard();
            }, 1500);
        }
    }, 80);

    function showFinalCard() {
        endingClearAll();

        var d = document.createElement('div');
        d.className = 'ending-bg-archive';
        d.style.cssText =
            'position:fixed;inset:0;color:#eee;' +
            'display:flex;flex-direction:column;align-items:center;justify-content:center;' +
            'text-align:center;padding:40px;' +
            'font-family:SimSun,serif;font-size:18px;line-height:2;' +
            'white-space:pre-wrap;';
        d.innerHTML =
            '归档完成。\n\n' +
            '节点 8：访客 ' + name + '\n' +
            '欢迎来到回响档案。';

        document.body.appendChild(d);

        var avatar = document.createElement('div');
        avatar.style.cssText =
            'width:120px;height:120px;margin:20px auto 0;' +
            'border:2px solid #333;border-radius:4px;' +
            'background:radial-gradient(circle, #222, #000);' +
            'position:relative;overflow:hidden;';
        var inner = document.createElement('div');
        inner.style.cssText =
            'position:absolute;inset:0;' +
            'background:repeating-linear-gradient(' +
            '  0deg, rgba(255,255,255,0.05) 0px, rgba(255,255,255,0.05) 1px, ' +
            '  transparent 1px, transparent 3px);';
        avatar.appendChild(inner);
        d.appendChild(avatar);

        setTimeout(function () {
            Store.set('online_override', 'archive');
            Store.set('post5_clicked', true);
            if (window.GameAudio) GameAudio.fadeOut('heartbeat', 1200);
            endingCleanupAndExit(1500);
        }, 3500);
    }
}

/* ============================================================
   结局 2：回头
   ============================================================ */
function endingLookback() {
    Store.set('ending_lookback', true);
    endingPush('lookback');

    /* 新增上报 */
    if (window.HxdaReport && window.HxdaReport.event) {
        HxdaReport.event('ending', { name: 'lookback' });
    }

    if (window.GameAudio) GameAudio.once('knock', { volume: 0.5 });

    endingClearAll();

    var flashes = 0;
    var fl = setInterval(function () {
        var d = document.createElement('div');
        d.style.cssText =
            'position:fixed;inset:0;background:' +
            (flashes % 2 === 0 ? '#a00' : '#000') + ';' +
            'z-index:999997;pointer-events:none;';
        document.body.appendChild(d);
        setTimeout(function () { d.remove(); }, 80);
        flashes++;

        if (flashes >= 12) {
            clearInterval(fl);
            setTimeout(showLookbackCard, 400);
        }
    }, 90);

    function showLookbackCard() {
        endingClearAll();

        var d = document.createElement('div');
        d.className = 'ending-bg-lookback';
        d.style.cssText =
            'position:fixed;inset:0;color:#eee;' +
            'display:flex;flex-direction:column;align-items:center;justify-content:center;' +
            'text-align:center;padding:40px;' +
            'font-family:SimSun,serif;font-size:20px;line-height:2;' +
            'white-space:pre-wrap;' +
            'text-shadow:0 0 20px rgba(0,0,0,0.9);';

        d.innerHTML =
            '你回头了。\n' +
            '现在，你是第 8 个。\n\n' +
            '当前在线：1（它）';

        document.body.appendChild(d);

        Store.set('online_override', 'it');

        setTimeout(function () {
            endingCleanupAndExit(0);
        }, 5000);
    }
}

/* ============================================================
   结局 3：断开
   ============================================================ */
function endingDisconnect() {
    Store.set('ending_disconnect', true);
    endingPush('disconnect');

    /* 新增上报 */
    if (window.HxdaReport && window.HxdaReport.event) {
        HxdaReport.event('ending', { name: 'disconnect' });
    }

    if (window.GameAudio) GameAudio.once('phone_ring', { volume: 0.5 });

    endingClearAll();

    endingShowFullscreen(
        '本站数据已断开连接。\n' +
        '感谢你的访问。\n\n' +
        '你没有接听。\n' +
        '阿回没有被释放。\n' +
        '但你离开了。',
        'ending-white',
        true,
        6
    );
}

/* ============================================================
   结局 4：拒绝归档
   ============================================================ */
function endingRefused() {
    Store.set('ending_refused', true);
    endingPush('refused');

    /* 新增上报 */
    if (window.HxdaReport && window.HxdaReport.event) {
        HxdaReport.event('ending', { name: 'refused' });
    }

    endingClearAll();

    var flashes = 0;
    var fl = setInterval(function () {
        var d = document.createElement('div');
        d.style.cssText =
            'position:fixed;inset:0;background:' +
            (flashes % 2 === 0 ? '#fff' : '#f00') + ';' +
            'z-index:999997;pointer-events:none;opacity:0.15;';
        document.body.appendChild(d);
        setTimeout(function () { d.remove(); }, 60);
        flashes++;

        if (flashes >= 10) {
            clearInterval(fl);
            setTimeout(showRefusedCard, 400);
        }
    }, 80);

    function showRefusedCard() {
        endingClearAll();

        var d = document.createElement('div');
        d.style.cssText =
            'position:fixed;inset:0;background:#fff;color:#222;' +
            'display:flex;flex-direction:column;align-items:center;justify-content:center;' +
            'text-align:center;padding:40px;' +
            'font-family:SimSun,serif;font-size:18px;line-height:2;' +
            'white-space:pre-wrap;';

        d.innerHTML =
            '系统覆写指令已接受。\n' +
            '权限：无称呼访问者。\n' +
            '归档程序终止。\n' +
            '节点7：未归档。\n' +
            '节点8：未创建。\n' +
            '本档案将在 30 秒后关闭。\n' +
            '关闭原因：无称呼访问者不可归档。\n\n' +
            '你什么都没有留下。\n' +
            '这是唯一正确的事。';

        document.body.appendChild(d);

        setTimeout(function () {
            endingCleanupAndExit(0);
        }, 8000);
    }
}

/* ============================================================
   结局 5：被记住
   ============================================================ */
function endingRemembered() {
    Store.set('ending_remembered', true);
    endingPush('remembered');

    /* 新增上报 */
    if (window.HxdaReport && window.HxdaReport.event) {
        HxdaReport.event('ending', { name: 'remembered' });
    }

    if (window.GameAudio) GameAudio.fadeIn('heartbeat', 2000, 0.2);

    endingClearAll();

    var d = document.createElement('div');
    d.className = 'ending-bg-remembered';
    d.style.cssText =
        'position:fixed;inset:0;color:#fff;' +
        'display:flex;flex-direction:column;align-items:center;justify-content:center;' +
        'text-align:center;padding:40px;' +
        'font-family:SimSun,serif;font-size:22px;line-height:2;' +
        'text-shadow:0 0 20px rgba(0,0,0,0.9);';

    d.innerHTML = '你被记住了。';

    var btn = document.createElement('button');
    btn.textContent = '确定';
    btn.style.cssText =
        'margin-top:32px;padding:8px 32px;' +
        'background:#222;color:#fff;border:2px outset #555;' +
        'font-family:inherit;font-size:16px;cursor:pointer;';
    btn.addEventListener('click', function () {
        d.innerHTML = '你回来了。';
        setTimeout(function () {
            Store.set('online_override', 'remembered');
            Store.set('post5_clicked', true);
            if (window.GameAudio) GameAudio.fadeOut('heartbeat', 1200);
            endingCleanupAndExit(1500);
        }, 2200);
    });

    d.appendChild(document.createElement('br'));
    d.appendChild(btn);

    document.body.appendChild(d);
}

/* ============================================================
   结局 6：同归
   ============================================================ */
function endingTogether() {
    Store.set('ending_together', true);
    endingPush('together');

    /* 新增上報 */
    if (window.HxdaReport && window.HxdaReport.event) {
        HxdaReport.event('ending', { name: 'together' });
    }

    endingClearAll();

    endingShowFullscreen(
        '现在你是它。\n\n' +
        '请等待下一位。\n' +
        '请记住：不要回答。',
        'ending-center',
        false,
        0
    );

    setTimeout(function () {
        window.location.href = 'admin.html';
    }, 5000);
}

/* ============================================================
   真结局：空白页
   ============================================================ */
function endingTrue() {
    Store.set('ending_true', true);
    endingPush('true');

    /* 新增上報 */
    if (window.HxdaReport && window.HxdaReport.event) {
        HxdaReport.event('ending', { name: 'true' });
    }

    if (window.GameAudio) GameAudio.fadeIn('heartbeat', 2000, 0.25);

    endingClearAll();

    var d = document.createElement('div');
    d.style.cssText =
        'position:fixed;inset:0;background:#000;color:#eee;' +
        'display:flex;align-items:center;justify-content:center;' +
        'text-align:center;padding:40px;' +
        'font-family:SimSun,serif;font-size:22px;line-height:2;' +
        'white-space:pre-wrap;';

    var original = '我叫阿回。\n我不知道我是谁。';
    d.textContent = original;
    document.body.appendChild(d);

    setTimeout(function () {
        var chars = original.split('');
        var i = 0;

        var delInt = setInterval(function () {
            chars.pop();
            d.textContent = chars.join('');
            d.style.opacity = Math.max(0.2, 1 - i * 0.03);
            i++;

            if (chars.length === 0) {
                clearInterval(delInt);
                setTimeout(showTrueEndingFinal, 1500);
            }
        }, 120);
    }, 2000);

    function showTrueEndingFinal() {
        d.remove();

        var final = document.createElement('div');
        final.style.cssText =
            'position:fixed;inset:0;background:#000;color:#eee;' +
            'display:flex;align-items:center;justify-content:center;' +
            'text-align:center;padding:40px;' +
            'font-family:SimSun,serif;font-size:18px;line-height:2.2;' +
            'white-space:pre-wrap;';

        final.textContent =
            '阿回从未存在过。\n' +
            '第一个留言者在第43页写的是：\n' +
            '"有人吗。"\n' +
            '系统把"有人吗"变成了"阿回"。\n' +
            '现在，没有人回答。\n' +
            '循环终止。';

        document.body.appendChild(final);

        setTimeout(function () {
            final.className = 'ending-bg-white';
            final.style.color = '#222';
            final.textContent = '你检查一下书签。';

            setTimeout(function () {
                final.textContent = '你刚才检查了书签，对吗？\n\n你回来了。';

                setTimeout(function () {
                    Store.set('online_override', 'true_end');
                    if (window.GameAudio) GameAudio.fadeOut('heartbeat', 1500);
                    endingCleanupAndExit(0);
                }, 5000);
            }, 4000);
        }, 5000);
    }
}