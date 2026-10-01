/* ============================================================
   回响档案 · 玩家上报 + 干预执行 + WebSocket
   文件：wwwroot/hxda/js/report.js
   依赖：storage.js
   ============================================================ */

(function () {
    'use strict';

    var API = '/api/hxda';

    function getPlayerId() {
        var id = Store.get('hxda_player_id', '');
        if (!id) {
            id = 'p_' + Date.now() + '_' + Math.random().toString(36).slice(2, 8);
            Store.set('hxda_player_id', id);
        }
        return id;
    }

    /* ============ 上报玩家 ============ */
    function report() {
        var data = {
            id: getPlayerId(),
            visitorName: Store.get('visitor_name', ''),
            realName: Store.get('gate_signature_name', ''),
            signatureImage: Store.get('gate_signature_image', ''),
            signatureDate: Store.get('gate_signature_date', ''),
            adminLoggedIn: Store.get('admin_logged_in', false),
            onlineOverride: Store.get('online_override', ''),
            attention: Store.get('attention', 0),
            refreshCount: Store.get('refresh_count', 0),
            tabSwitches: Store.get('tab_switches', 0),
            searchCount: Store.get('search_count', 0),
            nodeRead: JSON.stringify(Store.get('node_read', [])),
            pmMessages: JSON.stringify(Store.get('pm_messages', [])),
            endingsSeen: JSON.stringify(Store.get('ending_all_seen', [])),
            currentPage: location.pathname,
            gatePassed: Store.get('gate_passed', false),
            gateSignatureName: Store.get('gate_signature_name', '')
        };

        fetch(API + '/report', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        }).catch(function () { });
    }

    /* ============ 上报事件 ============ */
    function reportEvent(type, data) {
        fetch(API + '/event', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                playerId: getPlayerId(),
                type: type,
                data: typeof data === 'string' ? data : JSON.stringify(data)
            })
        }).catch(function () { });
    }

    /* ============ 定时上报 ============ */
    setInterval(report, 30000);
    report();

    window.addEventListener('beforeunload', function () {
        try {
            navigator.sendBeacon(API + '/report',
                new Blob([JSON.stringify({
                    id: getPlayerId(),
                    currentPage: '/exit',
                    lastSeenAt: new Date().toISOString()
                })], { type: 'application/json' }));
        } catch (e) { }
    });

    /* ============ 干预轮询（降级） ============ */
    function poll() {
        fetch(API + '/poll?playerId=' + getPlayerId())
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (data && data.commands) data.commands.forEach(executeCommand);
            })
            .catch(function () { });
    }
    setInterval(poll, 15000);

    /* ============ 干预执行 ============ */
    function executeCommand(cmd) {
        if (!cmd || !cmd.type) return;

        if (cmd.type === 'glitch') {
            document.body.style.transition = 'filter 0.1s';
            document.body.style.filter = 'invert(1) hue-rotate(90deg)';
            setTimeout(function () { document.body.style.filter = ''; }, 600);
        }

        if (cmd.type === 'static') {
            var s = document.createElement('div');
            s.style.cssText =
                'position:fixed;inset:0;z-index:999999;pointer-events:none;' +
                'background:repeating-linear-gradient(0deg,rgba(255,255,255,0.08) 0px,rgba(255,255,255,0.08) 1px,transparent 1px,transparent 3px);' +
                'mix-blend-mode:screen;';
            document.body.appendChild(s);
            setTimeout(function () { s.remove(); }, 3000);
        }

        if (cmd.type === 'whisper') {
            var d = document.createElement('div');
            d.style.cssText =
                'position:fixed;top:20%;left:50%;transform:translateX(-50%);' +
                'color:rgba(160,0,0,0.85);font-family:SimSun,serif;font-size:24px;' +
                'z-index:999999;pointer-events:none;text-shadow:0 0 20px rgba(0,0,0,0.8);' +
                'letter-spacing:0.2em;';
            d.textContent = cmd.payload || '……';
            document.body.appendChild(d);
            setTimeout(function () { d.remove(); }, 4000);
        }

        if (cmd.type === 'redirect') { window.location.href = cmd.payload; }
        if (cmd.type === 'alert') { alert(cmd.payload || ''); }

        if (cmd.type === 'rename') {
            document.querySelectorAll('#userName, #userInfo').forEach(function (el) {
                if (el) el.innerHTML = '<b>' + (cmd.payload || '') + '</b>';
            });
        }

        if (cmd.type === 'audio') {
            try { new Audio(cmd.payload).play().catch(function () { }); } catch (e) { }
        }

        if (cmd.type === 'attention_set') {
            var v = parseInt(cmd.payload, 10);
            if (!isNaN(v)) Store.set('attention', v);
        }

        if (cmd.type === 'lock') {
            document.body.style.pointerEvents = 'none';
            setTimeout(function () { document.body.style.pointerEvents = ''; },
                parseInt(cmd.payload, 10) || 3000);
        }
    }

    /* ============ WebSocket ============ */
    var ws = null;
    function connectWs() {
        try {
            if (typeof signalR === 'undefined') return;
            ws = new signalR.HubConnectionBuilder()
                .withUrl('/hxdaHub')
                .withAutomaticReconnect()
                .build();

            ws.on('Command', function (cmd) { executeCommand(cmd); });

            ws.start().then(function () {
                ws.invoke('JoinPlayer', getPlayerId());
            }).catch(function () { });
        } catch (e) { }
    }
    connectWs();

    setInterval(function () {
        if (ws && ws.state === 'Connected') {
            ws.invoke('Heartbeat', getPlayerId()).catch(function () { });
        }
    }, 15000);

    /* ============ 暴露接口 ============ */
    window.HxdaReport = {
        report: report,
        event: reportEvent
    };
})();