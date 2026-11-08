const BoardGameClient = (() => {
    let connection, config, room, mySide, myName = '玩家', myAvatar = '😊', isGuest = false;
    let renderer, aiThinking = false, gameOver = false;
    const $ = id => document.getElementById(id);
    const sideNames = { black: '黑方', white: '白方', red: '红方' };
    const nextSide = side => config.gameType === 'xiangqi'
        ? (side === 'red' ? 'black' : 'red')
        : (side === 'white' ? 'black' : 'white');

    function toast(message) {
        const el = $('toast'); if (!el) return;
        el.textContent = message; el.classList.add('show');
        clearTimeout(el._timer); el._timer = setTimeout(() => el.classList.remove('show'), 3000);
    }

    // 详细错误弹窗：默认折叠技术详情，可展开查看错误代码/堆栈
    function showRoomError(error, stage) {
        const overlay = $('errorOverlay');
        if (!overlay) { toast('连接房间失败'); return; }
        const message = (error && (error.message || error.toString())) || '无法连接到棋局房间';
        $('errorMessage').textContent = message;
        const detail = [
            '阶段：' + stage,
            '房间码：' + (config.roomCode || '(无)'),
            '棋类：' + (config.gameType || '(无)'),
            '模式：' + (config.mode || '(无)'),
            '错误类型：' + (error && error.name ? error.name : 'Error'),
            '错误信息：' + message,
            '堆栈：',
            (error && error.stack ? error.stack : '(无堆栈信息)')
        ].join('\n');
        $('errorDetail').textContent = detail;
        $('errorDetail').hidden = true;
        $('errorToggle').textContent = '查看详细错误 ▾';
        overlay.classList.add('show');
        overlay.setAttribute('aria-hidden', 'false');
    }

    window.toggleErrorDetail = () => {
        const detail = $('errorDetail'), toggle = $('errorToggle');
        if (!detail || !toggle) return;
        const hidden = detail.hidden;
        detail.hidden = !hidden;
        toggle.textContent = hidden ? '收起详细错误 ▴' : '查看详细错误 ▾';
    };

    window.closeError = () => {
        const overlay = $('errorOverlay');
        if (overlay) { overlay.classList.remove('show'); overlay.setAttribute('aria-hidden', 'true'); }
        location.href = '/BoardGame/Index';
    };

    async function boot(options) {
        config = options;
        const renderers = { gomoku: window.GomokuBoard, xiangqi: window.XiangqiBoard, chess: window.ChessBoard };
        renderer = renderers[config.gameType];
        if (!renderer) { toast('未知棋类'); return; }
        if (!config.roomCode) { toast('缺少房间码'); return; }

        try {
            const response = await fetch('/api/boardgame/me');
            if (response.ok) {
                const me = await response.json();
                myName = me.name || myName; myAvatar = me.avatar || myAvatar; isGuest = me.isGuest === true;
            }
        } catch { }

        renderer.mount($('board'), move => {
            if (!room || gameOver || aiThinking || room.status !== 'playing' || room.currentTurn !== mySide) return;
            connection.invoke('MakeMove', config.roomCode, move.from, move.to, move.extra || null, config.token || null).catch(() => toast('走子发送失败，请检查连接'));
        });

        connection = new signalR.HubConnectionBuilder()
            .withUrl('/boardGameHub')
            .withAutomaticReconnect()
            .build();

        connection.on('RoomUpdated', update => {
            if (!room) return;
            Object.assign(room, update);
            updatePlayerUI(); updateTurnUI(room.currentTurn); updateReadyUI();
        });
        connection.on('MoveMade', move => {
            if (!room) return;
            if (move.state) renderer.setState(move.state);
            else renderer.applyMove(move.from, move.to, move.side);
            room.currentTurn = move.turn; room.boardState = move.state; room.moveCount++;
            updateTurnUI(move.turn); appendMove(move);
            if (move.status && move.status !== 'Ongoing') showGameOver(move.winner, move.status);
        });
        connection.on('MoveRejected', message => toast(`无法落子：${message || '走法无效'}`));
        connection.on('AiThinking', active => { aiThinking = active; updateTurnUI(room?.currentTurn); });
        connection.on('ChatMessage', appendChat);
        connection.on('ChatWarning', warning => toast(warning.message));
        connection.on('ChatDisabled', warning => {
            toast(warning.message); $('chatInput').disabled = true; $('sendChatBtn').disabled = true;
            $('chatInput').placeholder = '聊天已关闭';
        });
        connection.on('GameOver', result => showGameOver(result.winner, result.outcome, result.reason));
        connection.on('GameStarted', () => { if (room) room.status = 'playing'; updateReadyUI(); updateTurnUI(room && room.currentTurn); toast('🎉 双方已准备，对局开始！'); });
        connection.on('GuestKicked', () => { if (room) { room.guestName = null; room.guestAvatar = null; room.guestIsGuest = 0; } updatePlayerUI(); updateReadyUI(); toast('对方已被房主移出房间'); });
        connection.onreconnecting(() => { $('turnPill').textContent = '正在重连…'; });
        connection.onreconnected(async () => {
            try { room = await connection.invoke('JoinRoom', config.roomCode, config.token || null); applyRoomState(); toast('已重新连接'); }
            catch (error) { console.error('❌ 重连失败:', error); showRoomError(error, '重新连接'); }
        });

        try {
            await connection.start();
            room = await connection.invoke('JoinRoom', config.roomCode, config.token || null);
            applyRoomState();
        } catch (error) {
            console.error('❌ 连接房间失败:', error);
            showRoomError(error, '加入房间');
            $('turnPill').textContent = '连接失败';
        }
        $('sendChatBtn').addEventListener('click', sendChat);
        $('chatInput').addEventListener('keydown', event => { if (event.key === 'Enter') sendChat(); });
    }

    function applyRoomState() {
        if (!room) return;
        mySide = room.mySide;
        renderer.setState(room.boardState);
        $('gameTitle').textContent = { gomoku: '⚫ 五子棋', xiangqi: '🀄 中国象棋', chess: '♞ 国际象棋' }[config.gameType] || '棋盘对局';
        $('roomCodeDisplay').textContent = room.roomCode;
        updatePlayerUI(); updateTurnUI(room.currentTurn);
        if (room.chatEnabled === 0) {
            $('chatInput').disabled = true; $('sendChatBtn').disabled = true; $('chatInput').placeholder = '聊天已关闭';
        }
        if (room.status === 'waiting') $('turnPill').textContent = '等待对手加入…';
        if (room.status === 'finished') showGameOver(room.winner, room.winner ? 'Win' : 'Draw', room.resultReason);
        updateReadyUI();
    }

    function updatePlayerUI() {
        const isHost = mySide === (config.gameType === 'xiangqi' ? 'red' : config.gameType === 'chess' ? 'white' : 'black');
        $('myName').textContent = myName; $('myAvatar').textContent = myAvatar;
        $('myAvatar').classList.toggle('is-guest', isGuest); $('myRole').textContent = sideNames[mySide] || mySide;
        const aiOpponent = room.mode === 'ai';
        $('oppName').textContent = aiOpponent ? 'DeepSeek AI' : (isHost ? (room.guestName || '等待对手加入…') : (room.hostName || '对手'));
        $('oppAvatar').textContent = aiOpponent ? '🤖' : (isHost ? (room.guestAvatar || '❔') : (room.hostAvatar || '❔'));
        $('oppAvatar').classList.toggle('is-guest', !aiOpponent && (isHost ? room.guestIsGuest === 1 : room.hostIsGuest === 1));
        $('oppRole').textContent = sideNames[nextSide(mySide)] || nextSide(mySide);
    }

    function updateTurnUI(turn) {
        if (!turn || !room) return;
        const myTurn = turn === mySide && !aiThinking;
        $('turnPill').textContent = aiThinking ? '🤖 AI 思考中…' : (room.status === 'waiting' ? '等待对手加入…' : myTurn ? '● 你的回合' : `○ 对方回合（${sideNames[turn] || turn}）`);
        $('playerMe').classList.toggle('active', myTurn && !gameOver);
        $('playerOpp').classList.toggle('active', !myTurn && !gameOver && room.status === 'playing');
    }

    function appendMove(move) {
        const list = $('movesList');
        list.querySelector('.latest')?.classList.remove('latest');
        const item = document.createElement('div'); item.className = 'bg-move-item latest';
        item.textContent = `${move.moveText || `${move.from || ''}→${move.to}`} (${sideNames[move.side] || move.side})`;
        list.appendChild(item); list.scrollTop = list.scrollHeight;
    }

    function appendChat(message) {
        const item = document.createElement('div');
        item.className = `bg-chat-msg${message.senderName === myName ? ' mine' : ''}`;
        const sender = document.createElement('span'); sender.className = 'sender';
        sender.textContent = `${message.senderName}${message.isGuest ? ' · 游客' : ''}：`;
        const content = document.createElement('span'); content.textContent = message.content;
        item.append(sender, content); $('chatList').appendChild(item); $('chatList').scrollTop = $('chatList').scrollHeight;
    }

    function sendChat() {
        const input = $('chatInput'); const content = input.value.trim();
        if (!content || !connection || !room || room.status !== 'playing') return;
        connection.invoke('SendChat', config.roomCode, content).catch(() => toast('消息发送失败'));
        input.value = '';
    }

    function resign() {
        if (gameOver || !connection) return;
        connection.invoke('Resign', config.roomCode).catch(() => toast('认输请求发送失败'));
    }

    function updateReadyUI() {
        const readyBtn = $('readyBtn'), kickBtn = $('kickBtn'), resignBtn = $('resignBtn');
        if (!room) return;
        const hostSide = config.gameType === 'xiangqi' ? 'red' : config.gameType === 'chess' ? 'white' : 'black';
        const isHost = mySide === hostSide;
        const isPvp = room.mode === 'pvp';
        const waiting = room.status === 'waiting';
        const hasGuest = !!room.guestName;
        const myReady = isHost ? room.hostReady === 1 : room.guestReady === 1;
        if (readyBtn) {
            readyBtn.style.display = (isPvp && waiting) ? 'inline-flex' : 'none';
            readyBtn.textContent = myReady ? '✅ 已准备' : '🟢 准备';
            readyBtn.disabled = !!myReady;
        }
        if (kickBtn) kickBtn.style.display = (isPvp && waiting && isHost && hasGuest) ? 'inline-flex' : 'none';
        if (resignBtn) resignBtn.style.display = (room.status === 'playing') ? 'inline-flex' : 'none';
    }

    window.setReady = () => {
        if (!connection) return;
        connection.invoke('SetReady', config.roomCode, config.token || null).catch(() => toast('准备失败，请重试'));
    };

    window.kickPlayer = () => {
        if (!connection || !confirm('确定将对方移出房间吗？')) return;
        connection.invoke('KickPlayer', config.roomCode, config.token || null).catch(() => toast('踢出失败'));
    };

    window.closeGameOver = () => {
        const layer = $('gameOverLayer');
        if (layer) { layer.classList.remove('show'); layer.setAttribute('aria-hidden', 'true'); }
    };

    function showGameOver(winner, outcome, reason) {
        if (gameOver) return;
        gameOver = true;
        const draw = outcome === 'Draw' || !winner;
        const won = winner === mySide;
        $('goIcon').textContent = draw ? '🤝' : won ? '🏆' : '💔';
        $('goTitle').textContent = draw ? '平局' : won ? '你赢了！' : '你输了';
        $('goReason').textContent = reason || (draw ? '棋逢对手，握手言和' : '本局已结束');
        $('gameOverLayer').classList.add('show');
        $('gameOverLayer').setAttribute('aria-hidden', 'false');
        updateTurnUI(room?.currentTurn);
    }

    return { boot, resign };
})();
