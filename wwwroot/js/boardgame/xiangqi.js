window.XiangqiBoard = (() => {
    const W = 9, H = 10;
    const chars = { K: '帅', A: '仕', B: '相', N: '馬', R: '車', C: '炮', P: '兵', k: '将', a: '士', b: '象', n: '馬', r: '車', c: '砲', p: '卒' };
    let canvas, context, onMove, state, selected = null, cell = 0, padX = 0, padY = 0, width = 0, height = 0, ratio = 1;

    function mount(element, callback) {
        canvas = element; context = canvas.getContext('2d'); onMove = callback;
        canvas.addEventListener('click', handleClick); window.addEventListener('resize', resize); resize();
    }
    function resize() {
        if (!canvas) return;
        const box = canvas.parentElement;
        width = Math.max(270, Math.min(box.clientWidth - 32, 680));
        height = width * 10 / 9;
        ratio = Math.min(window.devicePixelRatio || 1, 2);
        canvas.style.width = `${width}px`; canvas.style.height = `${height}px`;
        canvas.width = Math.round(width * ratio); canvas.height = Math.round(height * ratio);
        context.setTransform(ratio, 0, 0, ratio, 0, 0);
        cell = Math.min((width - 48) / (W - 1), (height - 48) / (H - 1));
        padX = (width - cell * (W - 1)) / 2; padY = (height - cell * (H - 1)) / 2;
        draw();
    }
    function setState(value) {
        try { state = typeof value === 'string' ? JSON.parse(value) : JSON.parse(JSON.stringify(value)); selected = null; draw(); }
        catch { }
    }
    function applyMove(from, to, side, stateAfter) {
        if (stateAfter) setState(stateAfter);
        else if (state && from) {
            const [fx, fy] = from.split(',').map(Number), [tx, ty] = to.split(',').map(Number);
            state.cells[ty][tx] = state.cells[fy][fx]; state.cells[fy][fx] = '';
        }
        selected = null; draw();
    }
    function draw() {
        if (!context || !state) return;
        const gradient = context.createLinearGradient(0, 0, width, height);
        gradient.addColorStop(0, '#221e2f'); gradient.addColorStop(1, '#0f0d18');
        context.fillStyle = gradient; context.fillRect(0, 0, width, height);
        context.save(); context.globalAlpha = 0.035; context.strokeStyle = '#a78bfa';
        for (let y = 8; y < height; y += 14) {
            context.beginPath(); context.moveTo(0, y);
            for (let x = 0; x <= width; x += 16) context.lineTo(x, y + Math.sin(x * 0.02 + y) * 1.5);
            context.stroke();
        }
        context.restore();
        context.strokeStyle = 'rgba(255,255,255,.16)'; context.lineWidth = 1.4;
        for (let y = 0; y < H; y++) {
            context.beginPath(); context.moveTo(px(0), py(y)); context.lineTo(px(W - 1), py(y)); context.stroke();
        }
        for (let x = 0; x < W; x++) {
            context.beginPath(); context.moveTo(px(x), py(0)); context.lineTo(px(x), py(H - 1)); context.stroke();
        }
        context.fillStyle = 'rgba(139,92,246,.14)'; context.fillRect(padX + cell, py(4) + 1, cell * 7, cell - 2);
        context.fillStyle = '#fbbf24'; context.font = `bold ${cell * 0.48}px serif`; context.textAlign = 'center'; context.textBaseline = 'middle';
        context.fillText('楚　河', px(2), py(4.5)); context.fillText('漢　界', px(6), py(4.5));
        context.strokeStyle = 'rgba(255,255,255,.16)'; context.lineWidth = 1.4;
        [[3, 0, 5, 2], [5, 0, 3, 2], [3, 7, 5, 9], [5, 7, 3, 9]].forEach(([x1, y1, x2, y2]) => {
            context.beginPath(); context.moveTo(px(x1), py(y1)); context.lineTo(px(x2), py(y2)); context.stroke();
        });
        if (selected) {
            context.beginPath(); context.arc(px(selected.x), py(selected.y), cell * 0.48, 0, Math.PI * 2);
            context.strokeStyle = '#fbbf24'; context.lineWidth = 3; context.stroke();
        }
        for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
            const piece = state.cells[y]?.[x]; if (piece) drawPiece(x, y, piece);
        }
    }
    function px(x) { return padX + x * cell; }
    function py(y) { return padY + y * cell; }
    function drawPiece(x, y, piece) {
        const cx = px(x), cy = py(y), radius = cell * 0.43, red = piece === piece.toUpperCase();
        context.save(); context.shadowColor = 'rgba(0,0,0,.4)'; context.shadowBlur = cell * 0.16; context.shadowOffsetY = cell * 0.06;
        context.beginPath(); context.arc(cx, cy, radius, 0, Math.PI * 2); context.fillStyle = '#f5e6c8'; context.fill(); context.restore();
        context.beginPath(); context.arc(cx, cy, radius, 0, Math.PI * 2); context.strokeStyle = '#57391f'; context.lineWidth = 1.8; context.stroke();
        context.beginPath(); context.arc(cx, cy, radius * 0.82, 0, Math.PI * 2); context.strokeStyle = red ? '#b91c1c' : '#1a1a1a'; context.lineWidth = 1.3; context.stroke();
        context.fillStyle = red ? '#b91c1c' : '#1a1a1a'; context.font = `bold ${radius * 1.02}px KaiTi, STKaiti, serif`;
        context.textAlign = 'center'; context.textBaseline = 'middle'; context.fillText(chars[piece] || piece, cx, cy + 1);
    }
    function eventPoint(event) {
        const rect = canvas.getBoundingClientRect();
        return {
            x: Math.round(((event.clientX - rect.left) / rect.width * width - padX) / cell),
            y: Math.round(((event.clientY - rect.top) / rect.height * height - padY) / cell)
        };
    }
    function handleClick(event) {
        if (!state) return;
        const point = eventPoint(event);
        if (point.x < 0 || point.x >= W || point.y < 0 || point.y >= H) return;
        const piece = state.cells[point.y][point.x];
        if (selected) {
            if (point.x === selected.x && point.y === selected.y) selected = null;
            else { onMove?.({ from: `${selected.x},${selected.y}`, to: `${point.x},${point.y}` }); selected = null; }
        } else if (piece) selected = point;
        draw();
    }
    return { mount, setState, applyMove };
})();
