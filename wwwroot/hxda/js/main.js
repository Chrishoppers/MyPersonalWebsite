/* ============================================================
   回响档案 · 论坛首页逻辑 + 搜索系统
   文件：wwwroot/hxda/js/main.js
   ============================================================ */

(function () {

    'use strict';

    function pad(n) { return String(n).padStart(2, '0'); }
    function nowFull() {
        var d = new Date();
        return d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate())
            + ' ' + pad(d.getHours()) + ':' + pad(d.getMinutes()) + ':' + pad(d.getSeconds());
    }
    function nowHM() {
        var d = new Date();
        return pad(d.getHours()) + ':' + pad(d.getMinutes()) + ':' + pad(d.getSeconds());
    }
    function escapeHtml(s) {
        var d = document.createElement('div');
        d.textContent = String(s);
        return d.innerHTML;
    }

    var onlineText = document.getElementById('onlineText');
    var statOnline = document.getElementById('statOnline');
    var statTotal = document.getElementById('statTotal');
    var userInfo = document.getElementById('userInfo');
    var searchInput = document.getElementById('searchInput');
    var searchDrop = document.getElementById('searchDrop');
    var post5Row = document.getElementById('post5Row');
    var post5Time = document.getElementById('post5Time');
    var post5Author = document.getElementById('post5Author');
    var post5Link = document.getElementById('post5Link');

    /* 全屏按钮 */
    if (window.FullscreenHelper && !window.FullscreenHelper.isMobile) {
        var fsBtn = document.createElement('button');
        fsBtn.textContent = '⛶';
        fsBtn.title = '进入全屏';
        fsBtn.style.cssText =
            'position:fixed;bottom:14px;left:18px;' +
            'color:#666;font-family:monospace;font-size:14px;' +
            'cursor:pointer;background:none;border:none;padding:4px 8px;' +
            'z-index:99998;';
        fsBtn.addEventListener('click', function () {
            if (window.FullscreenHelper.isInFullscreen()) {
                window.FullscreenHelper.exit();
            } else {
                window.FullscreenHelper.request();
            }
        });
        document.body.appendChild(fsBtn);
    }

    /* 在线状态 */
    (function initOnline() {

        statTotal.textContent = 8 + Store.get('visited', 0);

        var override = Store.get('online_override', '');
        if (override === 'it') {
            onlineText.textContent = '1（它）';
            statOnline.textContent = '1';
        } else if (override === 'remembered') {
            onlineText.textContent = '2（你，和它）';
            statOnline.textContent = '2';
        } else if (override === 'true_end') {
            onlineText.textContent = '1（新访客）';
            statOnline.textContent = '1';
        } else {
            if (Store.get('tab_switches', 0) >= 1) {
                onlineText.textContent = '2（你，和它）';
            } else {
                onlineText.textContent = '1（你）';
            }
        }

        var stayMin = 0;
        var stayInt = setInterval(function () {
            stayMin++;
            if (stayMin >= 10) {
                onlineText.textContent = '1（它）';
                clearInterval(stayInt);
            }
        }, 60000);

        var name = Store.get('visitor_name', '');
        var userNameEl = document.getElementById('userName');
        if (name && userNameEl) {
            userNameEl.innerHTML = '<b>' + escapeHtml(name) + '</b>';
        }
        var userAvatarUrl = Store.get('visitor_avatar', '');
        var userAvatarEl = document.getElementById('userAvatar');
        if (userAvatarUrl && userAvatarEl) {
            userAvatarEl.src = userAvatarUrl;
        }
    })();

    /* 帖子5 异常 */
    (function initPost5() {

        post5Time.textContent = nowHM();

        if (Store.get('post5_clicked', false)) {
            post5Author.textContent = Store.get('visitor_name', '') || '无名';
        }

        var post5Moved = false;
        var sinkTimer = setTimeout(function () {
            if (post5Moved) return;
            var tbody = post5Row.parentNode;
            tbody.appendChild(post5Row);
            post5Moved = true;
            post5Row.classList.remove('pulse');
        }, 30000);

        document.querySelectorAll('.post-table tbody tr[data-post-id]:not(#post5Row) a').forEach(function (a) {
            a.addEventListener('click', function () {
                clearTimeout(sinkTimer);
                var tbody = post5Row.parentNode;
                tbody.insertBefore(post5Row, tbody.firstChild);
                post5Row.classList.add('pulse');
                post5Moved = false;
            });
        });

        post5Link.addEventListener('click', function (e) {
            e.preventDefault();
            showPage43Popup();
        });
    })();

    function showPage43Popup() {
        uiPopup(
            '第43页',
            '您即将打开第43页。\n第43页不可回头。\n是否继续？',
            [
                {
                    text: '继续',
                    primary: true,
                    onClick: function () {
                        Store.set('post5_clicked', true);
                        Store.set('clicked_page43', true);
                        window.location.href = 'page43.html';
                    }
                },
                {
                    text: '返回',
                    onClick: function () {
                        window.scrollTo({
                            top: Math.max(0, window.scrollY - 80),
                            behavior: 'smooth'
                        });
                    }
                }
            ]
        );
    }

    /* 分页 */
    document.querySelectorAll('.forum-pager a[data-page]').forEach(function (a) {
        a.addEventListener('click', function (e) {
            e.preventDefault();
            uiAlert('归档提示', '本页无内容。归档已满。');
        });
    });

    /* 弹窗 1 */
    (function initFirstVisit() {
        var cnt = Store.get('popup1_count', 0);
        if (cnt >= 3) return;

        setTimeout(function () {
            var n = cnt + 1;
            Store.set('popup1_count', n);

            var name = Store.get('visitor_name', '') || '无名';
            var rid = 'R-0' + (100 + Math.floor(Math.random() * 900));

            var msg;
            if (n === 1) {
                msg = '检测到新访客。\n' +
                    '您的称呼：' + name + '。\n' +
                    '您的访问已被归档。\n' +
                    '归档编号：' + rid + '。';
            } else if (n === 2) {
                msg = '您还没有点击确定。请点击确定。';
            } else {
                msg = '您不想确定吗？可以。但您已经在档案里了。';
            }

            uiAlert('系统提示', msg);
        }, 5000);
    })();

    /* 登录弹窗 */
    function showLoginPopup() {
        var name = Store.get('visitor_name', '') || '无名';
        uiAlert(
            '登录提示',
            '本站已停止注册。\n您已是访客。\n您的称呼：' + name + '。'
        );
    }

    document.addEventListener('click', function (e) {
        if (e.target.id === 'loginLink' || e.target.id === 'loginLinkInner') {
            e.preventDefault();
            showLoginPopup();
        }
    });

    /* 退出 */
    document.getElementById('navLogout').addEventListener('click', function (e) {
        e.preventDefault();
        uiPopup(
            '退出确认',
            '您确定要离开？\n它还没有回答。',
            [
                {
                    text: '确定离开',
                    danger: true,
                    onClick: function () {
                        Store.set('ending_disconnect', true);
                        window.location.href = 'page43.html?disconnect=1';
                    }
                },
                {
                    text: '留下',
                    primary: true,
                    onClick: function () {
                        addAttention(5);
                    }
                }
            ]
        );
    });

    /* 打印 */
    document.getElementById('btnPrint').addEventListener('click', function () {
        Store.set('printed_count', Store.get('printed_count', 0) + 1);
        Store.set('found_node7', true);
        Store.set('seen_source_code', true);
        window.print();
    });

    /* 关于 / 帮助 */
    document.getElementById('btnAbout').addEventListener('click', function () {
        window.location.href = 'about.html';
    });

    document.getElementById('footerHelp').addEventListener('click', function (e) {
        e.preventDefault();
        uiAlert(
            '帮助',
            '如果您能看懂这些字，说明您已经进入了档案。\n' +
            '没有帮助。\n' +
            '只有节点。'
        );
    });

    /* 管理后台 */
    function checkAdminVisibility() {
        var cond = Store.get('read_post_1', false)
            && Store.get('read_nodes', false)
            && Store.get('reverse_audio', false);
        if (cond) {
            document.getElementById('navAdmin').style.display = '';
            document.getElementById('btnAdmin').style.display = '';
        }
    }
    checkAdminVisibility();

    function showAdminPopup() {
        uiPopup(
            '管理后台',
            '您已获得管理后台访问权限。\n是否进入？',
            [
                {
                    text: '进入', primary: true, onClick: function () {
                        window.location.href = 'admin.html';
                    }
                },
                { text: '返回' }
            ]
        );
    }

    document.getElementById('navAdmin').addEventListener('click', function (e) {
        e.preventDefault();
        showAdminPopup();
    });
    document.getElementById('btnAdmin').addEventListener('click', function (e) {
        e.preventDefault();
        showAdminPopup();
    });

    /* 搜索系统 */
    searchInput.addEventListener('focus', function () { renderSearchHistory(); });
    searchInput.addEventListener('blur', function () {
        setTimeout(function () { searchDrop.classList.remove('show'); }, 200);
    });

    function renderSearchHistory() {
        var hist = Store.get('search_history', []);

        if (Store.get('search_count', 0) >= 10 && !Store.get('system_search_shown', false)) {
            hist.push({
                word: '有人吗。',
                time: '2009-11-01 00:00',
                user: '系统',
                isSystem: true
            });
            Store.set('search_history', hist);
            Store.set('system_search_shown', true);
        }

        if (!hist.length) {
            searchDrop.classList.remove('show');
            return;
        }

        var html = '';
        hist.slice().reverse().forEach(function (h) {
            html += '<div class="hist-item' + (h.isSystem ? ' is-system' : '') + '" ' +
                'data-word="' + escapeHtml(h.word) + '" ' +
                'data-system="' + (h.isSystem ? '1' : '0') + '">' +
                '<b>' + escapeHtml(h.word) + '</b>' +
                '<span class="h-time">' + escapeHtml(h.time) + '</span>' +
                (h.user ? '<span class="h-user">' + escapeHtml(h.user) + '</span>' : '') +
                '</div>';
        });

        searchDrop.innerHTML = html;
        searchDrop.classList.add('show');

        searchDrop.querySelectorAll('.hist-item').forEach(function (el) {
            el.addEventListener('mousedown', function (e) {
                e.preventDefault();
                var w = el.dataset.word;
                var isSystem = el.dataset.system === '1';
                searchDrop.classList.remove('show');

                if (isSystem) {
                    showSystemSearchResult();
                } else {
                    searchInput.value = w;
                    doSearch(w, true);
                }
            });
        });
    }

    searchInput.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') {
            var q = searchInput.value.trim();
            searchDrop.classList.remove('show');
            doSearch(q);
        }
    });

    function doSearch(q, fromHistory) {

        if (!fromHistory) {
            var n = Store.get('search_count', 0) + 1;
            Store.set('search_count', n);
            addAttention(1);

            if (window.HxdaReport && window.HxdaReport.event) {
                HxdaReport.event('search', { word: q, time: nowFull() });
            }

            var hist = Store.get('search_history', []);
            hist.push({
                word: q,
                time: nowFull(),
                user: Store.get('visitor_name', '') || '无名'
            });
            if (hist.length > 50) hist.shift();
            Store.set('search_history', hist);
        }

        if (Store.get('search_count', 0) >= 43) {
            flashBlack(500);
            setTimeout(function () {
                Store.set('ending_archive', true);
                window.location.href = 'page43.html?archive=1';
            }, 600);
            return;
        }

        if (q === '') {
            searchInput.value = '你什么都没搜。但我知道了。';
            return;
        }

        if (q === '第43页') { window.location.href = 'page43.html'; return; }
        if (q === '林默') { showSearchResults(['1', '2', '4']); return; }
        if (q === '笔记本') { showSearchResults(['1', '2']); return; }
        if (q === '寻物') { showSearchResults(['1']); return; }
        if (q === '版主') { showSearchResults(['3']); return; }
        if (q === '站长') { showSearchResults(['3']); return; }
        if (q === '归档') { showSearchResults(['1', '2', '3']); return; }
        if (q === '节点') { showSearchResults(['1', '2', '3']); return; }

        if (q === '阿回') {
            showBottomLine('阿回的最后一次访问：2019-02-14 23:47。IP来源：本站。设备：EchoSystem v0.9。状态：未归档 / 等待回答。');
            showAhuiPopup();
            return;
        }
        if (q === '阿回是谁') { searchInput.value = '别问。'; return; }
        if (q === '林默在哪') { showBottomLine('他在你后面。'); return; }
        if (q === '系统') { showSearchResults(['4']); return; }
        if (q === 'EchoSystem') {
            flashBlack(500);
            setTimeout(function () { searchInput.value = 'v0.9。你找到了。'; }, 600);
            showEchoPopup('v0.9');
            return;
        }
        if (q === 'EchoSystem v0.9') {
            flashBlack(1000);
            setTimeout(function () { searchInput.value = '版本已过时。当前版本：v1.0。'; }, 1100);
            showEchoPopup('v0.9 已过时');
            return;
        }
        if (['护士', '学生', '司机', '小孩', '匿名'].indexOf(q) !== -1) {
            showSearchResults(['2']);
            return;
        }

        if (q === '有人吗') { handleYouRenMa(); return; }
        if (q === '没有人') { handleNoOne(); return; }
        if (q === '我是谁') {
            var name = Store.get('visitor_name', '');
            showBottomLine('你是第43页的内容。' + (name ? '你是' + name + '。' : ''));
            return;
        }
        if (q === '你叫什么') {
            searchInput.value = '你叫什么？';
            showAskName();
            return;
        }
        if (q === '不要回头') {
            window.scrollTo({ top: Math.max(0, window.scrollY - 100), behavior: 'smooth' });
            searchInput.value = '太晚了。';
            Store.set('ending_lookback', true);
            setTimeout(function () { window.location.href = 'page43.html?lookback=1'; }, 1200);
            return;
        }
        if (q === '不要留名') {
            var hasName = Store.get('visitor_name', '') !== '';
            searchInput.value = hasName ? '你留了。' : '你还没留。很好。';
            return;
        }
        if (q === '不要回答') { searchInput.value = '你已经回答了。'; return; }
        if (q === '回头') { flashBlack(200); searchInput.value = '你回头了。'; return; }
        if (q === '身后') { flashBlack(200); searchInput.value = '我在。'; return; }
        if (q === '你在哪') { searchInput.value = '我在这里。'; return; }
        if (q === '你是谁') { searchInput.value = '我是第43页。'; return; }
        if (q === '我想出去') { searchInput.value = '你出不去。但你可以不进来。'; return; }
        if (q === '救命') { searchInput.value = '没有人能帮你。'; return; }
        if (q === '再见') { searchInput.value = '你确定？'; return; }
        if (q === '晚安') { searchInput.value = '现在才凌晨2:47。'; return; }
        if (q === '时间') { searchInput.value = '这里永远是23:47。'; return; }
        if (q === '日期') { searchInput.value = '2019-02-14。'; return; }
        if (q === '2019') { searchInput.value = '你找到了最后一天。'; return; }
        if (q === '2009') { searchInput.value = '你找到了第一天。'; return; }

        var myName = Store.get('visitor_name', '');
        if (myName && q === myName) { handleOwnName(); return; }

        var pmInfo = Store.get('pm_shared_info', {});
        if (pmInfo[q]) {
            showBottomLine('0条结果。\n但我知道你提到过' + q + '。\n你刚才告诉了林默。\n林默告诉了我。');
            return;
        }

        showBottomLine('0 条结果。');
    }

    function handleYouRenMa() {
        var n = Store.get('search_yourenma_count', 0) + 1;
        Store.set('search_yourenma_count', n);

        if (n === 1) {
            uiAlert(
                '搜索结果',
                '返回 7 条结果。\n' +
                '全是同一个帖子："有人吗。"\n' +
                '发帖人分别是 7 个归档者。\n' +
                '最后一条是阿回："我叫阿回。我不知道我是谁。"'
            );
        } else if (n === 2) {
            uiAlert(
                '搜索结果',
                '第 8 条结果出现。\n' +
                '发帖人：' + (Store.get('visitor_name', '') || '无名') + '。\n' +
                '内容："有人吗。"\n' +
                '时间：现在。'
            );
        } else {
            searchInput.value = '别搜了。';
            Store.set('ending_remembered', true);
            setTimeout(function () {
                window.location.href = 'page43.html?remembered=1';
            }, 1000);
        }
    }

    function handleNoOne() {
        searchInput.value = '';
        uiPopup(
            '确认',
            '你确定吗？',
            [
                {
                    text: '确定', onClick: function () {
                        showBottomLine('好。你取消了。很好。');
                    }
                },
                {
                    text: '取消', onClick: function () {
                        searchInput.value = '你取消了。很好。';
                    }
                }
            ]
        );
    }

    function handleOwnName() {
        var n = Store.get('search_own_name_count', 0) + 1;
        Store.set('search_own_name_count', n);
        var name = Store.get('visitor_name', '');

        if (n === 1) {
            uiAlert(
                '搜索结果',
                '1条结果。\n' +
                '发帖人：' + name + '。\n' +
                '内容："有人吗。"\n' +
                '时间：现在。\n' +
                '状态：归档中。'
            );
        } else if (n === 2) {
            uiAlert(
                '搜索结果',
                '1条结果。\n' +
                '发帖人：' + name + '。\n' +
                '内容："已归档。"\n' +
                '时间：现在。\n' +
                '状态：完成。'
            );
        } else if (n === 3) {
            searchInput.value = '你已经不在了。';
        } else if (n === 4) {
            flashBlack(300);
            setTimeout(function () { searchInput.value = '你还在找自己吗？'; }, 400);
        } else {
            showBottomLine('别找了。你在这里。');
        }
    }

    function showSearchResults(ids) {
        var names = {
            '1': '寻物：一本黑色皮面笔记本',
            '2': '有人认识一个叫"阿回"的人吗？',
            '3': '置顶：本版留言格式说明',
            '4': '（无标题）'
        };

        var msg = '返回 ' + ids.length + ' 条结果：\n';
        ids.forEach(function (id) {
            msg += '· ' + names[id] + '\n';
        });
        uiAlert('搜索结果', msg);
    }

    function showBottomLine(text) {
        var old = document.getElementById('searchBottomLine');
        if (old) old.remove();

        var el = document.createElement('div');
        el.id = 'searchBottomLine';
        el.style.cssText =
            'text-align:center;padding:16px;color:#666;font-size:13px;' +
            'white-space:pre-wrap;font-family:SimSun,serif;' +
            'background:#fff8d0;border:1px dashed #d8c878;margin:8px;';
        el.textContent = text;

        var postTable = document.querySelector('.post-table');
        if (postTable) {
            postTable.parentNode.insertBefore(el, postTable.nextSibling);
        } else {
            document.body.appendChild(el);
        }

        setTimeout(function () { el.remove(); }, 8000);
    }

    function showAhuiPopup() {
        uiPopup(
            '归档提示',
            '阿回不存在于当前归档节点。\n但阿回存在于第 7 节点。\n是否查看？',
            [
                {
                    text: '是', primary: true, onClick: function () {
                        window.location.href = 'node.html';
                    }
                },
                {
                    text: '否', onClick: function () {
                        searchInput.value = '你取消了。很好。';
                    }
                }
            ]
        );
    }

    function showEchoPopup(ver) {
        uiAlert(
            '归档提示',
            '您搜索了系统名称。\n' +
            '系统已检测到您的访问。\n' +
            '当前版本：' + ver + '。\n' +
            '您的访问将被记录。'
        );
    }

    function showSystemSearchResult() {
        var hist = Store.get('search_history', []);
        var curWord = '无名';
        for (var i = hist.length - 1; i >= 0; i--) {
            if (!hist[i].isSystem) {
                curWord = hist[i].word;
                break;
            }
        }

        uiPopup(
            '系统搜索记录',
            '你找到了第一个搜索者。\n' +
            '他是第一个留言者。\n' +
            '也是第一个被归档的人。\n' +
            '他搜的是"有人吗"。\n' +
            '系统回复了他。\n' +
            '然后他消失了。\n' +
            '\n' +
            '你现在搜的是"' + curWord + '"。\n' +
            '系统会回复你吗？',
            [
                {
                    text: '会', onClick: function () {
                        uiAlert('系统正在回复……', '有人。', function () {
                            Store.set('ending_remembered', true);
                            window.location.href = 'page43.html?remembered=1';
                        });
                    }
                },
                {
                    text: '不会', onClick: function () {
                        showBottomLine('好。');
                    }
                }
            ]
        );
    }

    function showAskName() {
        uiPrompt('你叫什么？', '输入你的名字', function (v) {
            if (v) {
                Store.set('visitor_name', v);
                searchInput.value = v;
                setTimeout(function () { doSearch(v); }, 100);
            }
        });
    }

    function flashBlack(ms) {
        var d = document.createElement('div');
        d.style.cssText = 'position:fixed;inset:0;background:#000;z-index:99998;';
        document.body.appendChild(d);
        setTimeout(function () { d.remove(); }, ms);
    }

    /* 搜索框主动打字 */
    var idleTimer = null;
    var idleStage = 0;
    var idleTexts = ['有 人 吗', '你 叫 什 么', '别 删 了'];
    var idleTyping = false;

    function resetIdleTimer() {
        clearTimeout(idleTimer);
        idleTyping = false;

        idleTimer = setTimeout(function () {
            if (searchInput.value === '') {
                startIdleTyping();
            }
        }, 30000);
    }

    function startIdleTyping() {
        if (idleStage >= idleTexts.length) return;
        idleTyping = true;

        var text = idleTexts[idleStage];
        var i = 0;

        var t = setInterval(function () {
            if (idleTyping === false) {
                clearInterval(t);
                return;
            }
            if (searchInput.value.length > 0 && searchInput.value !== text.slice(0, i)) {
                clearInterval(t);
                idleStage++;
                idleTyping = false;

                if (idleStage < idleTexts.length) {
                    setTimeout(startIdleTyping, 3000);
                } else {
                    var d = document.createElement('div');
                    d.style.cssText =
                        'position:fixed;top:20px;left:50%;transform:translateX(-50%);' +
                        'background:#1a3a6b;color:#fff;padding:8px 20px;' +
                        'font-family:SimSun,serif;font-size:13px;z-index:99999;';
                    d.textContent = '你赢了。暂时。';
                    document.body.appendChild(d);
                    setTimeout(function () { d.remove(); }, 4000);
                }
                return;
            }

            searchInput.value = text.slice(0, i + 1);
            i++;

            if (i >= text.length) {
                clearInterval(t);

                setTimeout(function () {
                    if (idleTyping && searchInput.value === text) {
                        idleStage++;
                        if (idleStage === 1) {
                            Store.set('ending_remembered', true);
                            window.location.href = 'page43.html?remembered=1';
                        }
                    }
                }, 5000);
            }
        }, 800);
    }

    searchInput.addEventListener('input', resetIdleTimer);
    searchInput.addEventListener('focus', resetIdleTimer);
    resetIdleTimer();

    var sc = Store.get('search_count', 0);
    if (sc >= 7 && sc < 13) {
        setTimeout(function () {
            if (searchInput.value === '') {
                searchInput.value = '你搜了7次。够了。';
            }
        }, 2000);
    }
    if (sc >= 13 && sc < 43) {
        setTimeout(function () {
            if (searchInput.value === '') {
                searchInput.value = '13次。你比阿回还执着。';
            }
        }, 2000);
    }

})();