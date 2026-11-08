window.GomokuBoard = (() => {
    const N = 15;
    let canvas, context, onMove, state, hover, size = 0, cell = 0, pad = 0, last = null, pixelRatio = 1, lastTime = 0, animating = false;

    function mount(element, callback) {
        canvas = element; context = canvas.getContext('2d'); onMove = callback;
        canvas.addEventListener('click', handleClick);
        canvas.addEventListener('pointermove', handlePointerMove);
        canvas.addEventListener('pointerleave', () => { hover = null; draw(); });
        window.addEventListener('resize', resize);
        resize();
    }

    function resize() {
        if (!canvas) return;
        const wrapper = canvas.parentElement;
        size = Math.max(220, Math.min(wrapper.clientWidth - 32, window.innerHeight * 0.72, 680));
        pixelRatio = Math.min(window.devicePixelRatio || 1, 2);
        canvas.style.width = `${size}px`; canvas.style.height = `${size}px`;
        canvas.width = Math.round(size * pixelRatio); canvas.height = Math.round(size * pixelRatio);
        context.setTransform(pixelRatio, 0, 0, pixelRatio, 0, 0);
        cell = size / N; pad = cell / 2;
        draw();
    }

    function setState(value) {
        try { state = typeof value === 'string' ? JSON.parse(value) : structuredClone(value); }
        catch { return; }
        draw();
    }

    function applyMove(from, to, side, stateAfter) {
        if (stateAfter) setState(stateAfter);
        else if (state && to) {
            const [x, y] = to.split(',').map(Number);
            if (Number.isInteger(x) && Number.isInteger(y) && state.cells[y]?.[x] !== undefined) state.cells[y][x] = side === 'black' ? '1' : '2';
        }
        if (to) { const [x, y] = to.split(',').map(Number); last = { x, y }; lastTime = Date.now(); }
        draw();
        animateDrop();
    }

    function animateDrop() {
        if (animating) return;
        animating = true;
        const tick = () => {
            if (Date.now() - lastTime < 260) { draw(); requestAnimationFrame(tick); }
            else { animating = false; draw(); }
        };
        requestAnimationFrame(tick);
    }

    function draw() {
        if (!context || !state) return;
        const gradient = context.createLinearGradient(0, 0, size, size);
        gradient.addColorStop(0, '#221e2f'); gradient.addColorStop(1, '#0f0d18');
        context.fillStyle = gradient; context.fillRect(0, 0, size, size);
        context.save(); context.globalAlpha = 0.035; context.strokeStyle = '#a78bfa'; context.lineWidth = 1;
        for (let y = 8; y < size; y += 14) {
            context.beginPath(); context.moveTo(0, y);
            for (let x = 0; x <= size; x += 16) context.lineTo(x, y + Math.sin(x * 0.025 + y) * 1.6);
            context.stroke();
        }
        context.restore();
        context.strokeStyle = 'rgba(255,255,255,.16)'; context.lineWidth = Math.max(1, cell * 0.018);
        for (let i = 0; i < N; i++) {
            const point = pad + i * cell;
            context.beginPath(); context.moveTo(pad, point); context.lineTo(size - pad, point); context.stroke();
            context.beginPath(); context.moveTo(point, pad); context.lineTo(point, size - pad); context.stroke();
        }
        context.fillStyle = '#a78bfa';
        [[3, 3], [3, 11], [7, 7], [11, 3], [11, 11]].forEach(([x, y]) => {
            context.beginPath(); context.arc(pad + x * cell, pad + y * cell, cell * 0.075, 0, Math.PI * 2); context.fill();
        });
        if (hover && state.cells[hover.y]?.[hover.x] === '') {
            context.beginPath(); context.arc(center(hover.x), center(hover.y), cell * 0.4, 0, Math.PI * 2);
            context.fillStyle = 'rgba(255,255,255,.14)'; context.fill();
        }
        for (let y = 0; y < N; y++) for (let x = 0; x < N; x++) {
            const piece = state.cells[y]?.[x];
            if (piece === '1' || piece === '2') {
                const isLast = last && last.x === x && last.y === y;
                const progress = isLast ? Math.min(1, (Date.now() - lastTime) / 260) : 1;
                drawStone(x, y, piece === '1', easeOutBack(progress));
            }
        }
        if (last) {
            context.beginPath(); context.arc(center(last.x), center(last.y), cell * 0.14, 0, Math.PI * 2);
            context.strokeStyle = '#e33b44'; context.lineWidth = 2; context.stroke();
        }
    }

    function center(point) { return pad + point * cell; }
    function easeOutBack(t) {
        const c1 = 1.70158, c3 = c1 + 1;
        return Math.max(0, 1 + c3 * Math.pow(t - 1, 3) + c1 * Math.pow(t - 1, 2));
    }
    function drawStone(x, y, black, scale = 1) {
        const cx = center(x), cy = center(y), radius = cell * 0.42 * scale;
        context.save(); context.shadowColor = 'rgba(0,0,0,.45)'; context.shadowBlur = cell * 0.16; context.shadowOffsetY = cell * 0.05;
        context.beginPath(); context.arc(cx, cy, radius, 0, Math.PI * 2); context.fillStyle = black ? '#0a0a12' : '#f8f8f8'; context.fill(); context.restore();
        const shine = context.createRadialGradient(cx - radius * 0.3, cy - radius * 0.34, radius * 0.08, cx, cy, radius);
        if (black) { shine.addColorStop(0, '#5b5b6e'); shine.addColorStop(0.55, '#20202c'); shine.addColorStop(1, '#06060c'); }
        else { shine.addColorStop(0, '#fff'); shine.addColorStop(0.65, '#eee'); shine.addColorStop(1, '#bdbdbd'); }
        context.beginPath(); context.arc(cx, cy, radius, 0, Math.PI * 2); context.fillStyle = shine; context.fill();
        context.strokeStyle = black ? 'rgba(255,255,255,.4)' : 'rgba(0,0,0,.12)'; context.lineWidth = 1; context.stroke();
    }

    function pointFromEvent(event) {
        const rect = canvas.getBoundingClientRect();
        return {
            x: Math.round(((event.clientX - rect.left) / rect.width * size - pad) / cell),
            y: Math.round(((event.clientY - rect.top) / rect.height * size - pad) / cell)
        };
    }
    function handleClick(event) {
        if (!onMove || !state) return;
        const point = pointFromEvent(event);
        if (point.x < 0 || point.x >= N || point.y < 0 || point.y >= N || state.cells[point.y][point.x] !== '') return;
        onMove({ from: null, to: `${point.x},${point.y}` });
    }
    function handlePointerMove(event) {
        const point = pointFromEvent(event);
        hover = point.x >= 0 && point.x < N && point.y >= 0 && point.y < N ? point : null;
        draw();
    }

    return { mount, setState, applyMove };
})();
