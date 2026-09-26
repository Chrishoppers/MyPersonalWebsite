/* ============================================================
   回响档案 · 通用 UI 组件
   文件：wwwroot/hxda/js/ui.js
   加载顺序：4
   ============================================================ */

(function () {

    'use strict';

    window.uiPopup = function (title, bodyHtml, buttons, opts) {
        opts = opts || {};
        var overlay = document.createElement('div');
        overlay.className = 'overlay active';
        var box = document.createElement('div');
        box.className = 'overlay-box';

        var titleBar = document.createElement('div');
        titleBar.className = 'overlay-title';
        titleBar.innerHTML =
            '<span>' + title + '</span>' +
            '<span class="x" title="关闭">✕</span>';
        box.appendChild(titleBar);

        var body = document.createElement('div');
        body.className = 'overlay-body';
        body.innerHTML = bodyHtml;
        box.appendChild(body);

        var footer = document.createElement('div');
        footer.className = 'overlay-footer';

        var list = buttons && buttons.length ? buttons : [{ text: '关闭' }];
        list.forEach(function (b) {
            var btn = document.createElement('button');
            btn.className = 'old-btn'
                + (b.primary ? ' old-btn-primary' : '')
                + (b.danger ? ' old-btn-danger' : '');
            btn.textContent = b.text;
            btn.addEventListener('click', function () {
                if (b.onClick) b.onClick();
                if (b.close !== false) uiClose(overlay);
            });
            footer.appendChild(btn);
        });
        box.appendChild(footer);
        overlay.appendChild(box);

        if (opts.maskClose !== false) {
            overlay.addEventListener('click', function (e) {
                if (e.target === overlay) uiClose(overlay);
            });
        }

        titleBar.querySelector('.x').addEventListener('click', function () {
            uiClose(overlay);
        });

        if (opts.escClose !== false) {
            var escHandler = function (e) {
                if (e.key === 'Escape') uiClose(overlay);
            };
            document.addEventListener('keydown', escHandler);
            overlay._escHandler = escHandler;
        }

        document.body.appendChild(overlay);

        var firstInput = overlay.querySelector('input, textarea');
        if (firstInput) setTimeout(function () { firstInput.focus(); }, 100);

        return overlay;
    };

    window.uiClose = function (overlay) {
        if (!overlay) return;
        if (overlay._escHandler) {
            document.removeEventListener('keydown', overlay._escHandler);
        }
        overlay.remove();
    };

    window.uiPrompt = function (title, placeholder, onConfirm) {
        var overlay = uiPopup(
            title,
            '<input type="text" id="uiPromptInput" placeholder="' + (placeholder || '') + '" autocomplete="off">',
            [
                { text: '取消' },
                {
                    text: '确定',
                    primary: true,
                    onClick: function () {
                        var v = document.getElementById('uiPromptInput').value.trim();
                        if (onConfirm) onConfirm(v);
                    }
                }
            ]
        );
        setTimeout(function () {
            var inp = overlay.querySelector('#uiPromptInput');
            if (inp) {
                inp.focus();
                inp.addEventListener('keydown', function (e) {
                    if (e.key === 'Enter') {
                        var v = inp.value.trim();
                        uiClose(overlay);
                        if (onConfirm) onConfirm(v);
                    }
                });
            }
        }, 100);
        return overlay;
    };

    window.uiConfirm = function (title, message, onYes, onNo) {
        return uiPopup(
            title,
            '<div style="white-space:pre-wrap;">' + message + '</div>',
            [
                { text: '取消', onClick: onNo },
                { text: '确定', primary: true, onClick: onYes }
            ]
        );
    };

    window.uiAlert = function (title, message, onClose) {
        return uiPopup(
            title,
            '<div style="white-space:pre-wrap;">' + message + '</div>',
            [{ text: '确定', primary: true, onClick: onClose }]
        );
    };

    window.uiToast = function (text, type) {
        var colors = { info: '#1a3a6b', warn: '#c80', error: '#a00', success: '#060' };
        var bg = colors[type] || colors.info;
        var d = document.createElement('div');
        d.style.cssText =
            'position:fixed;bottom:20px;right:20px;' +
            'background:' + bg + ';color:#fff;' +
            'padding:8px 18px;' +
            'font-family:SimSun,serif;font-size:13px;' +
            'z-index:99999;' +
            'max-width:320px;' +
            'line-height:1.6;';
        d.textContent = text;
        document.body.appendChild(d);
        setTimeout(function () {
            d.style.transition = 'opacity 0.4s, transform 0.4s';
            d.style.opacity = '0';
            d.style.transform = 'translateX(20px)';
            setTimeout(function () { d.remove(); }, 500);
        }, 3000);
    };

    window.uiInjectBackButtons = function () {
        var path = window.location.pathname;

        if (path.endsWith('index.html') ||
            path.endsWith('bbs.html') ||
            path.endsWith('/hxda/') ||
            path.endsWith('/hxda')) {
            return;
        }

        var header = document.querySelector('.forum-head .head-right');
        if (header && !header.querySelector('.back-to-bbs-top')) {
            var a = document.createElement('a');
            a.className = 'back-to-bbs-top';
            a.href = 'bbs.html';
            a.textContent = '← 返回论坛首页';
            a.style.cssText = 'color:#ffd;text-decoration:underline;font-size:11px;';
            header.appendChild(a);
        }

        var footer = document.querySelector('.forum-footer');
        if (footer && !footer.querySelector('.back-to-bbs')) {
            var a2 = document.createElement('a');
            a2.className = 'back-to-bbs';
            a2.href = 'bbs.html';
            a2.textContent = '返回论坛首页';
            a2.style.marginRight = '12px';
            footer.insertBefore(a2, footer.firstChild);
        }
    };

    document.addEventListener('DOMContentLoaded', function () {
        uiInjectBackButtons();
    });

})();