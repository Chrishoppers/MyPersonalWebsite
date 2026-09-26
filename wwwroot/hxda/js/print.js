/* ============================================================
   回响档案 · 打印系统
   对应大纲第十九章
   ============================================================ */

(function () {
    // 只在有打印按钮的页面生效
    const printBtn = document.getElementById('printLink');
    if (!printBtn) return;

    // 记录打印次数
    const pc = Store.get('printed_count', 0) + 1;
    Store.set('printed_count', pc);
    Store.set('seen_source_code', true);
    Store.set('found_node7', true);

    // 覆盖默认的打印行为
    printBtn.addEventListener('click', e => {
        e.preventDefault();

        // 先把隐藏注释插入到当前 DOM（只影响打印视图）
        injectPrintComments();

        // 调用真实打印
        setTimeout(() => window.print(), 50);
    });

    // CSS：只在打印时显示注释
    const style = document.createElement('style');
    style.textContent = `
        @media print {
            .forum-header,
            .online,
            .pager,
            .footer-links,
            .search-area,
            .top-link,
            .notice {
                display: none !important;
            }
            body {
                background: #fff !important;
                color: #000 !important;
            }
            .print-only {
                display: block !important;
                font-family: "Courier New", monospace;
                font-size: 12px;
                color: #666;
                margin: 8px 0;
                white-space: pre-wrap;
            }
            .post-list {
                border: 1px solid #000 !important;
                background: #fff !important;
            }
            .post-list th {
                background: #eee !important;
                color: #000 !important;
            }
            .post-list td {
                color: #000 !important;
                border-bottom: 1px solid #ddd !important;
            }
        }
        .print-only {
            display: none;
        }
    `;
    document.head.appendChild(style);

    function injectPrintComments() {
        if (document.querySelector('.print-only')) return;
        const c = document.createElement('div');
        c.className = 'print-only';
        c.textContent =
            '<!-- 节点7：归档完成。等待下一位。 -->\n' +
            '<!-- 下一节点：无名者。 -->\n' +
            '<!-- 如果你能看到这里，说明你不是留言者。 -->\n' +
            '<!-- 你是阅读者。 -->\n' +
            '<!-- 但阅读者也会被归档。 -->\n' +
            '<!-- 第43页：不要回头。 -->\n' +
            '<!-- EchoSystem v0.9 -->';
        const main = document.querySelector('table.post-list');
        if (main) main.parentNode.insertBefore(c, main);
        else document.body.insertBefore(c, document.body.firstChild);
    }

    // 打印完成后（窗口聚焦）清除打印注释
    window.addEventListener('afterprint', () => {
        const c = document.querySelector('.print-only');
        if (c) c.remove();
    });
})();