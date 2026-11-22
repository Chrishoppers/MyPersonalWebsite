/* 休闲小游戏通用辅助：音效 + 积分提交 + 排行榜 */
window.CasualGame = (function () {
    'use strict';

    /* ===================== 音效（Web Audio，零依赖） ===================== */
    var AC = null, master = null;
    function ensureAudio() {
        if (!AC) {
            try {
                var Ctx = window.AudioContext || window.webkitAudioContext;
                AC = new Ctx();
                master = AC.createGain();
                master.gain.value = 0.5;
                master.connect(AC.destination);
            } catch (e) { AC = null; }
        }
        if (AC && AC.state === 'suspended') AC.resume();
        return AC;
    }
    function tone(freq, dur, type, vol, when, slideTo) {
        if (!ensureAudio()) return;
        var t0 = AC.currentTime + (when || 0);
        var o = AC.createOscillator();
        o.type = type || 'sine';
        o.frequency.setValueAtTime(freq, t0);
        if (slideTo) o.frequency.exponentialRampToValueAtTime(slideTo, t0 + dur);
        var g = AC.createGain();
        g.gain.setValueAtTime(0.0001, t0);
        g.gain.exponentialRampToValueAtTime(vol || 0.3, t0 + 0.012);
        g.gain.exponentialRampToValueAtTime(0.0001, t0 + dur);
        o.connect(g); g.connect(master);
        o.start(t0); o.stop(t0 + dur + 0.03);
    }
    function noise(dur, vol, freq, type) {
        if (!ensureAudio()) return;
        var t0 = AC.currentTime;
        var len = Math.floor(AC.sampleRate * dur);
        var buf = AC.createBuffer(1, len, AC.sampleRate);
        var d = buf.getChannelData(0);
        for (var i = 0; i < len; i++) d[i] = Math.random() * 2 - 1;
        var src = AC.createBufferSource(); src.buffer = buf;
        var f = AC.createBiquadFilter(); f.type = type || 'lowpass'; f.frequency.value = freq || 1200;
        var g = AC.createGain(); g.gain.setValueAtTime(vol || 0.4, t0); g.gain.exponentialRampToValueAtTime(0.0001, t0 + dur);
        src.connect(f); f.connect(g); g.connect(master);
        src.start(t0); src.stop(t0 + dur + 0.03);
    }
    var Sound = {
        click: function () { tone(620, 0.06, 'square', 0.1, 0, 320); },
        correct: function () { tone(660, 0.1, 'triangle', 0.24); tone(880, 0.16, 'triangle', 0.24, 0.07); },
        wrong: function () { tone(210, 0.22, 'sawtooth', 0.2, 0, 110); },
        pop: function () { tone(520, 0.12, 'sine', 0.3, 0, 90); noise(0.05, 0.22, 1500, 'highpass'); },
        flip: function () { tone(480, 0.05, 'triangle', 0.14, 0, 700); },
        match: function () { tone(523, 0.1, 'triangle', 0.24); tone(784, 0.16, 'triangle', 0.24, 0.06); },
        win: function () { [523, 659, 784, 1047].forEach(function (f, i) { tone(f, 0.18, 'triangle', 0.3, i * 0.09); }); },
        lose: function () { [400, 320, 260].forEach(function (f, i) { tone(f, 0.2, 'sawtooth', 0.2, i * 0.12); }); },
        tick: function () { tone(950, 0.04, 'square', 0.08); },
        levelup: function () { [440, 554, 659, 880].forEach(function (f, i) { tone(f, 0.16, 'square', 0.2, i * 0.07); }); }
    };

    /* ===================== 网络 ===================== */
    function post(url, data) {
        return fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data || {})
        }).then(function (r) { return r.json(); }).catch(function () { return { ok: false, msg: '网络错误' }; });
    }
    function getJson(url) {
        return fetch(url).then(function (r) { return r.json(); }).catch(function () { return { ok: false, msg: '网络错误' }; });
    }

    /* ===================== 工具 ===================== */
    function escapeHtml(s) {
        return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }

    /* ===================== 排行榜渲染 ===================== */
    var MEDALS = ['🥇', '🥈', '🥉'];
    function renderLeaderboard(container, list, highlightUserId) {
        if (!container) return;
        if (!list || !list.length) {
            container.innerHTML = '<div class="lb-empty">暂无记录，快来抢占第一！</div>';
            return;
        }
        var html = '';
        for (var i = 0; i < list.length; i++) {
            var it = list[i];
            var medal = i < 3 ? MEDALS[i] : '<span class="lb-rank">' + (i + 1) + '</span>';
            var me = highlightUserId && it.userId === highlightUserId;
            html += '<div class="lb-row' + (me ? ' me' : '') + '">' +
                '<span class="lb-medal">' + medal + '</span>' +
                '<span class="lb-name">' + escapeHtml(it.username || ('玩家' + it.userId)) + '</span>' +
                (it.maxCombo > 0 ? '<span class="lb-combo">🔥' + it.maxCombo + '</span>' : '') +
                '<span class="lb-score">' + it.bestScore + '</span>' +
                '</div>';
        }
        container.innerHTML = html;
    }

    /* ===================== 漂浮分数 ===================== */
    function floatText(x, y, text, color, cls) {
        var d = document.createElement('div');
        d.className = 'float-score' + (cls ? ' ' + cls : '');
        d.textContent = text;
        if (color) d.style.color = color;
        d.style.left = x + 'px';
        d.style.top = y + 'px';
        document.body.appendChild(d);
        setTimeout(function () { d.remove(); }, 900);
        return d;
    }

    return {
        Sound: Sound,
        submit: function (gameKey, score, combo) { return post('/api/casual/submit', { gameKey: gameKey, score: score, combo: combo }); },
        myBest: function (gameKey) { return getJson('/api/casual/mybest?gameKey=' + encodeURIComponent(gameKey)); },
        loadLeaderboard: function (gameKey, container, highlightUserId) {
            return getJson('/api/casual/leaderboard?gameKey=' + encodeURIComponent(gameKey) + '&limit=20')
                .then(function (res) { renderLeaderboard(container, res && res.data, highlightUserId); return res; });
        },
        escapeHtml: escapeHtml,
        floatText: floatText
    };
})();
