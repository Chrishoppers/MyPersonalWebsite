/* ============================================================
   回响档案 · 音频管理器
   文件：wwwroot/hxda/js/audio.js
   依赖：无（可选 Store）
   所有音频均为 .mp3
   ============================================================ */

(function () {
    'use strict';

    var AUDIO_BASE = 'audio/';

    var FILES = {
        rain: 'rain.mp3',
        fan: 'fan.mp3',
        welcome_chime: 'welcome_chime.mp3',
        whisper_reverse: 'whisper_reverse.mp3',
        heartbeat: 'heartbeat.mp3',
        knock: 'knock.mp3',
        phone_ring: 'phone_ring.mp3',
        page_turn: 'page_turn.mp3',
        static: 'static.mp3',
        breath: 'breath.mp3',
        keyboard: 'keyboard.mp3',
        click: 'click.mp3'
    };

    var VOLUME = {
        rain: 0.10,
        fan: 0.05,
        welcome_chime: 0.20,
        whisper_reverse: 0.60,
        heartbeat: 0.35,
        knock: 0.50,
        phone_ring: 0.45,
        page_turn: 0.40,
        static: 0.10,
        breath: 0.30,
        keyboard: 0.15,
        click: 0.12
    };

    var LOOP = {
        rain: true,
        fan: true,
        heartbeat: true,
        static: true,
        welcome_chime: false,
        whisper_reverse: false,
        knock: false,
        phone_ring: false,
        page_turn: false,
        breath: false,
        keyboard: false,
        click: false
    };

    var pool = {};
    var fadeTimers = {};
    var muted = false;

    function getAudio(name) {
        if (pool[name]) return pool[name];
        if (!FILES[name]) return null;

        var a = new Audio(AUDIO_BASE + FILES[name]);
        a.volume = (VOLUME[name] != null) ? VOLUME[name] : 0.5;
        a.loop = !!LOOP[name];
        a.preload = 'auto';

        a.addEventListener('ended', function () {
            if (a.loop) {
                try { a.currentTime = 0; a.play().catch(function () { }); } catch (e) { }
            }
        });

        pool[name] = a;
        return a;
    }

    function play(name, opts) {
        opts = opts || {};
        if (muted) return null;
        var a = getAudio(name);
        if (!a) return null;

        if (opts.volume != null) a.volume = opts.volume;
        if (opts.loop != null) a.loop = opts.loop;
        if (opts.currentTime != null) {
            try { a.currentTime = opts.currentTime; } catch (e) { }
        }
        if (opts.restart) {
            try { a.currentTime = 0; } catch (e) { }
        }

        try {
            var p = a.play();
            if (p && p.catch) p.catch(function () { });
        } catch (e) { }
        return a;
    }

    function stop(name) {
        var a = pool[name];
        if (!a) return;
        try { a.pause(); } catch (e) { }
    }

    function stopAll() {
        Object.keys(pool).forEach(function (k) {
            try { pool[k].pause(); } catch (e) { }
        });
    }

    function pause(name) { stop(name); }
    function resume(name) { play(name); }

    function fadeTo(name, targetVol, durationMs, onDone) {
        var a = getAudio(name);
        if (!a) return;
        durationMs = durationMs || 800;

        if (fadeTimers[name]) {
            clearInterval(fadeTimers[name]);
            fadeTimers[name] = null;
        }

        var startVol = a.volume;
        var startTime = Date.now();
        var diff = targetVol - startVol;

        if (targetVol > 0 && a.paused) {
            try {
                var p = a.play();
                if (p && p.catch) p.catch(function () { });
            } catch (e) { }
        }

        fadeTimers[name] = setInterval(function () {
            var t = (Date.now() - startTime) / durationMs;
            if (t >= 1) {
                a.volume = Math.max(0, Math.min(1, targetVol));
                clearInterval(fadeTimers[name]);
                fadeTimers[name] = null;
                if (targetVol <= 0) {
                    try { a.pause(); a.currentTime = 0; } catch (e) { }
                }
                if (typeof onDone === 'function') onDone();
                return;
            }
            a.volume = Math.max(0, Math.min(1, startVol + diff * t));
        }, 30);
    }

    function fadeIn(name, durationMs, targetVol) {
        var a = getAudio(name);
        if (!a) return;
        if (muted) return;
        if (targetVol == null) targetVol = (VOLUME[name] != null) ? VOLUME[name] : 0.5;

        a.volume = 0;
        try {
            var p = a.play();
            if (p && p.catch) p.catch(function () { });
        } catch (e) { }

        fadeTo(name, targetVol, durationMs || 800);
    }

    function fadeOut(name, durationMs, onDone) {
        fadeTo(name, 0, durationMs || 800, onDone);
    }

    function once(name, opts) {
        opts = opts || {};
        if (muted) return null;
        var a = getAudio(name);
        if (!a) return null;

        var clone = a.cloneNode(true);
        clone.volume = (opts.volume != null) ? opts.volume : a.volume;
        clone.loop = false;

        try {
            var p = clone.play();
            if (p && p.catch) p.catch(function () { });
        } catch (e) { }
        return clone;
    }

    function setLoop(name, loop) {
        var a = getAudio(name);
        if (a) a.loop = !!loop;
    }

    function mute() {
        muted = true;
        Object.keys(pool).forEach(function (k) {
            try { pool[k].pause(); } catch (e) { }
        });
    }

    function unmute() { muted = false; }
    function isMuted() { return muted; }

    window.GameAudio = {
        play: play,
        stop: stop,
        stopAll: stopAll,
        pause: pause,
        resume: resume,
        fadeIn: fadeIn,
        fadeOut: fadeOut,
        fadeTo: fadeTo,
        once: once,
        setLoop: setLoop,
        mute: mute,
        unmute: unmute,
        isMuted: isMuted,
        FILES: FILES,
        VOLUME: VOLUME
    };

    window.addEventListener('beforeunload', function () {
        stopAll();
    });

})();