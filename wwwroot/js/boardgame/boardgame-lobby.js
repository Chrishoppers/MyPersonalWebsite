(() => {
    let currentType = '';
    let currentRoom = null;
    const $ = id => document.getElementById(id);

    function toast(message) {
        const el = $('toast'); el.textContent = message; el.classList.add('show');
        clearTimeout(el._timer); el._timer = setTimeout(() => el.classList.remove('show'), 3000);
    }

    async function hasPrivacyAgreement() {
        try {
            const response = await fetch('/api/boardgame/privacy-status');
            if (!response.ok) return false;
            const result = await response.json();
            if (result.agreed) localStorage.setItem('bg_privacy_agreed_v1', '1');
            return result.agreed === true;
        } catch { return false; }
    }

    function readReturnUrl() { return encodeURIComponent('/BoardGame/Index'); }

    window.openMode = async (type, title) => {
        if (!(await hasPrivacyAgreement())) {
            if (confirm('开始对局前请先阅读并同意隐私声明。现在前往阅读吗？'))
                location.href = `/BoardGame/Privacy?returnUrl=${readReturnUrl()}`;
            return;
        }
        currentType = type;
        currentRoom = null;
        $('modalTitle').textContent = `${title} · 选择模式`;
        $('qrSection').hidden = true;
        $('modeModal').classList.add('show');
        $('modeModal').setAttribute('aria-hidden', 'false');
    };

    window.closeModal = () => {
        $('modeModal').classList.remove('show');
        $('modeModal').setAttribute('aria-hidden', 'true');
    };

    window.startMatch = async kind => {
        if (!(await hasPrivacyAgreement())) { toast('请先同意隐私声明'); return; }
        try {
            const isInvite = kind === 'qr';
            const response = await fetch(isInvite ? '/api/boardgame/create' : '/api/boardgame/match', {
                method: 'POST', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(isInvite ? { gameType: currentType, mode: 'pvp' } : { gameType: currentType })
            });
            const result = await response.json();
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
        if (!(await hasPrivacyAgreement())) { toast('请先同意隐私声明'); return; }
        try {
            const response = await fetch('/api/boardgame/create', {
                method: 'POST', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ gameType: currentType, mode: 'ai', aiLevel: level })
            });
            const result = await response.json();
            if (!response.ok) { toast(result.error || '创建对局失败'); return; }
            location.href = `/BoardGame/Play?type=${encodeURIComponent(currentType)}&mode=ai&level=${encodeURIComponent(level)}&code=${encodeURIComponent(result.roomCode)}`;
        } catch { toast('创建对局失败，请稍后重试'); }
    };

    window.goPlay = () => {
        if (!currentRoom?.roomCode) return;
        location.href = `/BoardGame/Play?type=${encodeURIComponent(currentType)}&mode=pvp&code=${encodeURIComponent(currentRoom.roomCode)}`;
    };

    $('modeModal').addEventListener('click', event => { if (event.target === $('modeModal')) closeModal(); });
    document.addEventListener('keydown', event => { if (event.key === 'Escape') closeModal(); });
})();
