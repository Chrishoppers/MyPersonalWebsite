(() => {
    let currentType = '';
    let currentRoom = null;
    let pendingType = '';
    let pendingTitle = '';
    const $ = id => document.getElementById(id);

    function toast(message) {
        const el = $('toast'); el.textContent = message; el.classList.add('show');
        clearTimeout(el._timer); el._timer = setTimeout(() => el.classList.remove('show'), 3000);
    }

    function openModeModal(type, title) {
        currentType = type;
        currentRoom = null;
        $('modalTitle').textContent = `${title} · 选择模式`;
        $('qrSection').hidden = true;
        $('modeModal').classList.add('show');
        $('modeModal').setAttribute('aria-hidden', 'false');
    }

    // 点击棋类：每次都弹出隐私声明确认（纯前端，不查库、不持久化）
    window.openMode = (type, title) => {
        pendingType = type;
        pendingTitle = title;
        const modal = $('privacyModal');
        if (modal) { modal.classList.add('show'); modal.setAttribute('aria-hidden', 'false'); }
    };

    window.closeModal = () => {
        $('modeModal').classList.remove('show');
        $('modeModal').setAttribute('aria-hidden', 'true');
    };

    window.closePrivacyModal = () => {
        $('privacyModal').classList.remove('show');
        $('privacyModal').setAttribute('aria-hidden', 'true');
        pendingType = ''; pendingTitle = '';
    };

    // 同意：直接进入该棋类的模式选择，不再上传/查询数据库
    window.agreePrivacy = () => {
        $('privacyModal').classList.remove('show');
        $('privacyModal').setAttribute('aria-hidden', 'true');
        const type = pendingType, title = pendingTitle;
        pendingType = ''; pendingTitle = '';
        if (type) openModeModal(type, title);
    };

    window.startMatch = async kind => {
        try {
            const isInvite = kind === 'qr';
            const response = await fetch(isInvite ? '/api/boardgame/create' : '/api/boardgame/match', {
                method: 'POST', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(isInvite ? { gameType: currentType, mode: 'pvp' } : { gameType: currentType })
            });
            const result = await response.json();
            if (response.status === 401) { toast('请先登录后再创建/匹配对局'); setTimeout(() => location.href = '/Auth/Login?returnUrl=' + encodeURIComponent('/BoardGame/Index'), 800); return; }
            if (!response.ok) { toast(result.error || '匹配失败'); return; }
            currentRoom = result;
            if (kind === 'qr') {
                $('qrSection').hidden = false;
                const image = document.createElement('img'); image.src = result.qrBase64; image.alt = '棋局邀请二维码';
                const caption = document.createElement('p'); caption.textContent = '使用手机相机扫码加入';
                $('qrWrap').replaceChildren(image, caption);
                $('roomCodeDisplay').textContent = result.roomCode;
                return;
            }
            if (kind === 'online' && result.isCreator) toast('房间已创建，正在等待对手加入…');
            goPlay();
        } catch { toast('匹配失败，请稍后重试'); }
    };

    window.startAI = async level => {
        try {
            const response = await fetch('/api/boardgame/create', {
                method: 'POST', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ gameType: currentType, mode: 'ai', aiLevel: level })
            });
            const result = await response.json();
            if (response.status === 401) { toast('请先登录后再创建对局'); setTimeout(() => location.href = '/Auth/Login?returnUrl=' + encodeURIComponent('/BoardGame/Index'), 800); return; }
            if (!response.ok) { toast(result.error || '创建对局失败'); return; }
            location.href = `/BoardGame/Play?type=${encodeURIComponent(currentType)}&mode=ai&level=${encodeURIComponent(level)}&code=${encodeURIComponent(result.roomCode)}`;
        } catch { toast('创建对局失败，请稍后重试'); }
    };

    window.goPlay = () => {
        if (!currentRoom?.roomCode) return;
        location.href = `/BoardGame/Play?type=${encodeURIComponent(currentType)}&mode=pvp&code=${encodeURIComponent(currentRoom.roomCode)}`;
    };

    $('modeModal').addEventListener('click', event => { if (event.target === $('modeModal')) closeModal(); });
    $('privacyModal').addEventListener('click', event => { if (event.target === $('privacyModal')) closePrivacyModal(); });
    document.addEventListener('keydown', event => {
        if (event.key !== 'Escape') return;
        if ($('privacyModal').classList.contains('show')) closePrivacyModal();
        else closeModal();
    });
})();
