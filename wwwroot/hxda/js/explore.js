/* ============================================================
   回响档案 · 探索系统
   文件：wwwroot/hxda/js/explore.js
   加载顺序：6
   依赖：storage.js、tracker.js、ui.js
   ============================================================ */

(function () {

    'use strict';

    /* ============================================================
       1. 帖子标题悬停 3 秒 → 显示隐藏信息
       ============================================================ */
    function initTitleHover() {
        const links = document.querySelectorAll('.post-table .col-title a');
        if (!links.length) return;

        links.forEach(function (a) {
            let hoverTimer = null;

            a.addEventListener('mouseenter', function () {
                hoverTimer = setTimeout(function () {
                    const idMatch = a.href.match(/id=(\d+)/);
                    const id = idMatch ? idMatch[1] : null;
                    if (!id) return;
                    showTitleHint(a, id);
                }, 3000);
            });

            a.addEventListener('mouseleave', function () {
                clearTimeout(hoverTimer);
                const tip = a.parentElement.querySelector('.title-hint-tip');
                if (tip) tip.remove();
            });
        });
    }

    function showTitleHint(el, id) {
        /* 已经显示过就不再显示 */
        const key = 'title_hint_seen_' + id;
        const seenCount = Store.get(key, 0);
        if (seenCount >= 2) return;

        const hints = {
            '1': '最后编辑：2019-02-14 23:47 / 编辑者：它',
            '2': '此帖已被查看 43 次。你是第 44 个。',
            '3': '此帖自 2009 年起未被编辑。',
            '4': '此帖无回复。此帖有 ??? 个回复。',
            '5': '此帖的最后回复时间 = 你打开它的时间。'
        };

        const text = hints[id];
        if (!text) return;

        Store.set(key, seenCount + 1);
        addAttention(1);

        const tip = document.createElement('div');
        tip.className = 'title-hint-tip';
        tip.style.cssText =
            'position:absolute;background:#1a3a6b;color:#fff;' +
            'padding:4px 10px;font-size:11px;font-family:SimSun,serif;' +
            'z-index:99999;white-space:nowrap;pointer-events:none;' +
            'border:1px solid #0d1b3e;box-shadow:2px 2px 6px rgba(0,0,0,0.3);' +
            'top:100%;left:0;margin-top:4px;';
        tip.textContent = text;

        el.parentElement.style.position = 'relative';
        el.parentElement.appendChild(tip);

        setTimeout(function () { tip.remove(); }, 4000);
    }

    /* ============================================================
       2. Logo 上的「43」秘密
       ============================================================ */
    function initLogoSecret() {
        const logo = document.querySelector('.head-logo');
        if (!logo) return;

        /* 右下角插入一个不可见热区 */
        const secret = document.createElement('div');
        secret.style.cssText =
            'position:absolute;right:2px;bottom:2px;' +
            'width:24px;height:24px;' +
            'cursor:default;';
        logo.appendChild(secret);

        let clickCount = 0;

        secret.addEventListener('click', function (e) {
            e.stopPropagation();
            clickCount++;

            if (clickCount >= 5) {
                clickCount = 0;
                secret.style.background = 'rgba(255,0,0,0.1)';
                setTimeout(function () {
                    secret.style.background = '';
                }, 200);

                if (typeof uiPopup === 'function') {
                    uiPopup(
                        '第43页',
                        '你找到了 Logo 里的秘密。<br><br>' +
                        '这个数字从来没有出现在任何地方。<br>' +
                        '除了这里。<br><br>' +
                        '要不要看看第43页？',
                        [
                            {
                                text: '去看看', primary: true, onClick: function () {
                                    window.location.href = 'page43.html';
                                }
                            },
                            { text: '不去' }
                        ]
                    );
                }
                Store.set('found_logo_secret', true);
                addAttention(2);
            }
        });
    }

    /* ============================================================
       3. 帖子列表空白格（双击显示隐藏文字）
       ============================================================ */
    function initEmptyCells() {
        const table = document.querySelector('.post-table');
        if (!table) return;

        /* 在表头最后加一列 */
        const theadTr = table.querySelector('thead tr');
        if (theadTr && !theadTr.querySelector('.col-blank')) {
            const th = document.createElement('th');
            th.className = 'col-blank';
            th.style.width = '20px';
            theadTr.appendChild(th);
        }

        /* 每行加一个空白格 */
        table.querySelectorAll('tbody tr').forEach(function (tr) {
            if (tr.querySelector('.col-blank')) return;

            const td = document.createElement('td');
            td.className = 'col-blank';
            td.style.cssText = 'width:20px;cursor:default;';

            td.addEventListener('dblclick', function () {
                if (td.textContent === '阅') return;
                td.textContent = '阅';
                td.style.color = '#a00';
                td.style.fontWeight = 'bold';
                td.style.fontFamily = 'SimSun,serif';
                td.style.textAlign = 'center';
                Store.set('found_empty_cell', true);
                addAttention(1);
            });

            tr.appendChild(td);
        });
    }

    /* ============================================================
       4. 页面边缘窥视（鼠标靠近右边缘）
       ============================================================ */
    function initEdgePeek() {
        let shown = false;

        document.addEventListener('mousemove', function (e) {
            const w = window.innerWidth;
            if (e.clientX > w - 20 && !shown) {
                shown = true;

                const t = document.createElement('div');
                t.id = 'edgeText';
                t.textContent = '别往外看。';
                t.style.cssText =
                    'position:fixed;' +
                    'right:4px;top:50%;' +
                    'transform:translateY(-50%) rotate(90deg);' +
                    'transform-origin:right center;' +
                    'color:rgba(160,0,0,0.5);' +
                    'font-family:SimSun,serif;' +
                    'font-size:12px;' +
                    'pointer-events:none;' +
                    'z-index:99999;' +
                    'white-space:nowrap;';
                document.body.appendChild(t);

                Store.set('edge_peek_seen', true);

                setTimeout(function () {
                    t.remove();
                    shown = false;
                }, 2000);
            }
        });
    }

    /* ============================================================
       5. ESC 三连
       ============================================================ */
    function initEscSecret() {
        let escCount = 0;

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                escCount++;

                if (escCount >= 3) {
                    escCount = 0;

                    if (typeof uiPopup === 'function') {
                        uiPopup(
                            '取消',
                            '你想取消什么？<br><br>' +
                            '这里没有可以取消的东西。<br>' +
                            '除了你自己。',
                            [{ text: '……' }]
                        );
                    }
                    Store.set('esc_secret_count', Store.get('esc_secret_count', 0) + 1);
                    addAttention(2);
                }

                /* 3 秒内没继续按就清零 */
                clearTimeout(window._escTimer);
                window._escTimer = setTimeout(function () {
                    escCount = 0;
                }, 3000);
            }
        });
    }

    /* ============================================================
       6. 隐藏行（搜索 3+ 次后出现）
       ============================================================ */
    function initHiddenRows() {
        const table = document.querySelector('.post-table tbody');
        if (!table) return;

        const sc = Store.get('search_count', 0);
        if (sc < 3) return;

        if (document.getElementById('hiddenRows')) return;

        const hiddenTr1 = document.createElement('tr');
        hiddenTr1.id = 'hiddenRows';
        hiddenTr1.innerHTML =
            '<td class="col-icon" style="color:#aaa;">·</td>' +
            '<td class="col-title" style="color:#aaa;">（此帖已被删除）</td>' +
            '<td style="color:#aaa;">——</td>' +
            '<td style="text-align:center;color:#aaa;">0</td>' +
            '<td style="color:#aaa;">——</td>' +
            '<td class="col-blank"></td>';
        table.appendChild(hiddenTr1);

        const hiddenTr2 = document.createElement('tr');
        hiddenTr2.innerHTML =
            '<td class="col-icon" style="color:#aaa;">·</td>' +
            '<td class="col-title" style="color:#aaa;">（此帖不存在）</td>' +
            '<td style="color:#aaa;">——</td>' +
            '<td style="text-align:center;color:#aaa;">0</td>' +
            '<td style="color:#aaa;">——</td>' +
            '<td class="col-blank"></td>';
        table.appendChild(hiddenTr2);
    }

    /* ============================================================
       7. 页面边缘红晕（鼠标靠近窗口四周）
       ============================================================ */
    function initScreenEdge() {
        document.addEventListener('mousemove', function (e) {
            const w = window.innerWidth;
            const h = window.innerHeight;
            const threshold = 40;

            let near = '';
            if (e.clientX < threshold) near = 'left';
            else if (e.clientX > w - threshold) near = 'right';
            else if (e.clientY < threshold) near = 'top';
            else if (e.clientY > h - threshold) near = 'bottom';

            if (near) {
                document.body.style.boxShadow =
                    'inset 0 0 100px rgba(120,0,0,0.08)';
            } else {
                document.body.style.boxShadow = '';
            }
        });
    }

    /* ============================================================
       8. 双击页面背景（隐藏彩蛋）
       ============================================================ */
    function initBackgroundDoubleClick() {
        document.addEventListener('dblclick', function (e) {
            /* 只在纯背景上双击才触发 */
            if (e.target !== document.body &&
                !e.target.classList.contains('forum-wrap')) return;

            if (Math.random() < 0.5) {
                if (typeof uiToast === 'function') {
                    uiToast('你在双击什么？', 'warn');
                }
                addAttention(1);
            }
        });
    }

    /* ============================================================
       9. 滚动到底部（隐藏提示）
       ============================================================ */
    function initScrollBottom() {
        let triggered = false;

        window.addEventListener('scroll', function () {
            if (triggered) return;

            const scrolled = window.scrollY + window.innerHeight;
            const total = document.documentElement.scrollHeight;

            if (scrolled >= total - 50) {
                triggered = true;
                const a = Store.get('attention', 0);
                if (a >= 11 && typeof uiToast === 'function') {
                    uiToast('你看到了底部。但这不是底部。', 'warn');
                }
            }
        });
    }

    /* ============================================================
       初始化
       ============================================================ */
    document.addEventListener('DOMContentLoaded', function () {
        initTitleHover();
        initLogoSecret();
        initEmptyCells();
        initEdgePeek();
        initEscSecret();
        initHiddenRows();
        initScreenEdge();
        initBackgroundDoubleClick();
        initScrollBottom();
    });

})();