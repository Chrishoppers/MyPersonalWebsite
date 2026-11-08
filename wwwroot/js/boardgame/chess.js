window.ChessBoard = (() => {
    const SIZE = 8;
    const CHARS = { K: '♔', Q: '♕', R: '♖', B: '♗', N: '♘', P: '♙', k: '♚', q: '♛', r: '♜', b: '♝', n: '♞', p: '♟' };
    const PROMOTIONS = [
        { code: 'Q', white: '♕', black: '♛', label: '后 · Queen' },
        { code: 'R', white: '♖', black: '♜', label: '车 · Rook' },
        { code: 'B', white: '♗', black: '♝', label: '象 · Bishop' },
        { code: 'N', white: '♘', black: '♞', label: '马 · Knight' }
    ];
    let canvas, context, onMove, state, selected = null, boardSize = 0, cellSize = 0, ratio = 1, lastMove = null;

    function mount(element, callback) {
        canvas = element; context = canvas.getContext('2d'); onMove = callback;
        canvas.addEventListener('click', handleClick); window.addEventListener('resize', resize); resize();
    }

    function resize() {
        if (!canvas) return;
        boardSize = Math.max(220, Math.min(canvas.parentElement.clientWidth - 32, window.innerHeight * 0.72, 680));
        ratio = Math.min(window.devicePixelRatio || 1, 2);
        canvas.style.width = `${boardSize}px`; canvas.style.height = `${boardSize}px`;
        canvas.width = Math.round(boardSize * ratio); canvas.height = Math.round(boardSize * ratio);
        context.setTransform(ratio, 0, 0, ratio, 0, 0); cellSize = boardSize / SIZE; draw();
    }

    function setState(value) {
        if (!value) return;
        try { state = typeof value === 'string' ? JSON.parse(value) : JSON.parse(JSON.stringify(value)); selected = null; draw(); }
        catch { }
    }

    function applyMove(from, to, side, stateAfter) {
        if (stateAfter) { setState(stateAfter); return; }
        if (!state || !from) return;
        const [fx, fy] = from.split(',').map(Number), [tx, ty] = to.split(',').map(Number);
        const piece = state.cells[fy][fx];
        if (!piece) return;
        if (piece.toLowerCase() === 'p' && state.cells[ty][tx] === '' && fx !== tx) state.cells[fy][tx] = '';
        state.cells[ty][tx] = piece; state.cells[fy][fx] = '';
        if (piece.toLowerCase() === 'k' && Math.abs(fx - tx) === 2) {
            const rookFrom = tx > fx ? 7 : 0, rookTo = tx > fx ? 5 : 3;
            state.cells[ty][rookTo] = state.cells[ty][rookFrom]; state.cells[ty][rookFrom] = '';
        }
        if (piece === 'P' && ty === 0) state.cells[ty][tx] = 'Q';
        if (piece === 'p' && ty === 7) state.cells[ty][tx] = 'q';
        lastMove = { from: { x: fx, y: fy }, to: { x: tx, y: ty } };
        selected = null; draw();
    }

    function draw() {
        if (!context || !state) return;
        for (let y = 0; y < SIZE; y++) for (let x = 0; x < SIZE; x++) {
            context.fillStyle = (x + y) % 2 === 0 ? '#3c3750' : '#26222f';
            context.fillRect(x * cellSize, y * cellSize, cellSize, cellSize);
        }
        if (lastMove) {
            context.fillStyle = 'rgba(251,191,36,.22)';
            context.fillRect(lastMove.from.x * cellSize, lastMove.from.y * cellSize, cellSize, cellSize);
            context.fillStyle = 'rgba(251,191,36,.34)';
            context.fillRect(lastMove.to.x * cellSize, lastMove.to.y * cellSize, cellSize, cellSize);
        }
        if (selected) {
            context.fillStyle = 'rgba(251,191,36,.55)';
            context.fillRect(selected.x * cellSize, selected.y * cellSize, cellSize, cellSize);
        }
        context.font = `${cellSize * .82}px "Segoe UI Symbol","Apple Symbols",serif`;
        context.textAlign = 'center'; context.textBaseline = 'middle';
        for (let y = 0; y < SIZE; y++) for (let x = 0; x < SIZE; x++) {
            const piece = state.cells[y]?.[x]; if (!piece) continue;
            const symbol = CHARS[piece] || piece, cx = (x + .5) * cellSize, cy = (y + .5) * cellSize;
            const white = piece === piece.toUpperCase();
            const grad = context.createLinearGradient(cx, cy - cellSize * .42, cx, cy + cellSize * .42);
            if (white) { grad.addColorStop(0, '#ffffff'); grad.addColorStop(0.55, '#ecebf3'); grad.addColorStop(1, '#b4b4c9'); }
            else { grad.addColorStop(0, '#48475e'); grad.addColorStop(0.55, '#16151f'); grad.addColorStop(1, '#050509'); }
            context.save();
            context.shadowColor = 'rgba(0,0,0,.55)'; context.shadowBlur = cellSize * .16; context.shadowOffsetY = cellSize * .05;
            context.fillStyle = grad; context.fillText(symbol, cx, cy + cellSize * .02); context.restore();
            context.lineWidth = Math.max(1, cellSize * .015);
            context.strokeStyle = white ? 'rgba(115,105,150,.75)' : 'rgba(205,200,228,.6)';
            context.strokeText(symbol, cx, cy + cellSize * .02);
        }
        context.font = `bold ${cellSize * .14}px sans-serif`; context.fillStyle = 'rgba(255,255,255,.45)';
        for (let i = 0; i < SIZE; i++) {
            context.textAlign = 'left'; context.textBaseline = 'bottom'; context.fillText(String.fromCharCode(97 + i), i * cellSize + 4, boardSize - 3);
            context.textAlign = 'right'; context.textBaseline = 'top'; context.fillText(String(SIZE - i), boardSize - 4, i * cellSize + 3);
        }
    }

    function handleClick(event) {
        if (!state || !onMove) return;
        const rect = canvas.getBoundingClientRect();
        const x = Math.floor((event.clientX - rect.left) / rect.width * SIZE);
        const y = Math.floor((event.clientY - rect.top) / rect.height * SIZE);
        if (x < 0 || x >= SIZE || y < 0 || y >= SIZE) return;
        if (!selected) {
            if (state.cells[y][x]) selected = { x, y };
            draw(); return;
        }
        if (x === selected.x && y === selected.y) { selected = null; draw(); return; }
        const from = `${selected.x},${selected.y}`, to = `${x},${y}`;
        const piece = state.cells[selected.y][selected.x];
        const promotes = piece?.toLowerCase() === 'p' && ((piece === 'P' && y === 0) || (piece === 'p' && y === 7));
        selected = null; draw();
        if (promotes) {
            showPromotionDialog(piece === 'P').then(extra => { if (extra) onMove({ from, to, extra }); });
        } else onMove({ from, to });
    }

    function showPromotionDialog(isWhite) {
        return new Promise(resolve => {
            const mask = document.createElement('div'); mask.className = 'bg-promo-mask'; mask.setAttribute('role', 'dialog'); mask.setAttribute('aria-modal', 'true');
            const panel = document.createElement('div'); panel.className = 'bg-promo-panel';
            const title = document.createElement('div'); title.className = 'bg-promo-title'; title.textContent = '👑 兵临城下 · 升变';
            const subtitle = document.createElement('div'); subtitle.className = 'bg-promo-sub'; subtitle.textContent = `${isWhite ? '白方' : '黑方'}兵升变 · 请选择新棋子`;
            const options = document.createElement('div'); options.className = 'bg-promo-options';
            const cancel = document.createElement('button'); cancel.type = 'button'; cancel.className = 'bg-promo-cancel'; cancel.textContent = '取消升变';
            panel.append(title, subtitle, options, cancel); mask.appendChild(panel);
            let finished = false;
            const finish = result => {
                if (finished) return; finished = true; document.removeEventListener('keydown', onKeyDown);
                mask.classList.remove('show'); window.setTimeout(() => mask.remove(), 400); resolve(result);
            };
            const onKeyDown = event => { if (event.key === 'Escape') finish(null); };
            PROMOTIONS.forEach((choice, index) => {
                const button = document.createElement('button'); button.type = 'button'; button.className = 'bg-promo-option';
                button.style.setProperty('--promo-delay', `${index * 70}ms`);
                const icon = document.createElement('span'); icon.className = 'icon'; icon.textContent = isWhite ? choice.white : choice.black;
                const label = document.createElement('span'); label.className = 'label'; label.textContent = choice.label;
                button.append(icon, label);
                button.addEventListener('click', () => {
                    button.classList.add('selecting');
                    options.querySelectorAll('.bg-promo-option').forEach(item => { if (item !== button) item.classList.add('fading'); });
                    cancel.disabled = true; window.setTimeout(() => finish(choice.code), 450);
                });
                options.appendChild(button);
            });
            cancel.addEventListener('click', () => finish(null));
            mask.addEventListener('click', event => { if (event.target === mask) finish(null); });
            document.addEventListener('keydown', onKeyDown);
            document.body.appendChild(mask); requestAnimationFrame(() => mask.classList.add('show'));
            options.querySelector('button')?.focus();
        });
    }

    return { mount, setState, applyMove };
})();
