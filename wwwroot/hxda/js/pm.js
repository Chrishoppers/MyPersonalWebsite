/* ============================================================
   回响档案 · 私信系统
   文件：wwwroot/hxda/js/pm.js
   后端：/api/deepseek/chat（代理，Key 在服务器）
   ============================================================ */

(function () {

    'use strict';

    var CONFIG = {
        apiUrl: '/api/deepseek/chat',
        maxHistory: 20,
        useAI: true
    };

    var bodyEl = document.getElementById('pmBody');
    var inputEl = document.getElementById('pmInput');
    var sendBtn = document.getElementById('pmSend');
    var statusEl = document.getElementById('pmStatus');
    var typingEl = document.getElementById('pmTyping');
    var bannerEl = document.getElementById('pmArchivedBanner');
    var stageInfo = document.getElementById('pmStageInfo');

    if (!bodyEl) return;

    function escapeHtml(s) {
        var d = document.createElement('div');
        d.textContent = String(s);
        return d.innerHTML;
    }
    function pad(n) { return String(n).padStart(2, '0'); }
    function nowHM() {
        var d = new Date();
        return pad(d.getHours()) + ':' + pad(d.getMinutes());
    }

    var pmMessages = Store.get('pm_messages', []);
    var pmOpened = Store.get('pm_opened', false);
    var pmEnded = Store.get('pm_ended', false);
    var pmArchived = Store.get('pm_archived', false);
    var openStartTime = Date.now();

    if (!pmOpened) {
        pmOpened = true;
        Store.set('pm_opened', true);
        Store.set('pm_first_open_time', Date.now());
        if (pmMessages.length === 0) {
            pmMessages.push({
                role: 'lin',
                content: '你好。你是谁？',
                time: nowHM()
            });
            Store.set('pm_messages', pmMessages);
        }
    }

    function getPMStage() {
        var readNodes = Store.get('read_nodes', false);
        var readPost1 = Store.get('read_post_1', false);
        var foundNode7 = Store.get('found_node7', false);
        var inAdmin = Store.get('read_nodes', false) && foundNode7;

        if (inAdmin && readNodes) return 4;
        if (readNodes) return 3;
        if (readPost1) return 2;
        return 1;
    }

    function renderMessages() {
        bodyEl.innerHTML = '';

        pmMessages.forEach(function (m) {
            var div = document.createElement('div');
            div.className = 'pm-msg ' + (m.role === 'lin' ? 'from-lin' : 'from-me');

            div.innerHTML =
                '<span class="who">' + (m.role === 'lin' ? '林默' : '你') + '</span>' +
                '<span class="txt">' + escapeHtml(m.content).replace(/\n/g, '<br>') + '</span>' +
                '<span class="time">' + escapeHtml(m.time || '') + '</span>';

            bodyEl.appendChild(div);
        });

        bodyEl.scrollTop = bodyEl.scrollHeight;

        if (pmArchived) {
            bannerEl.classList.add('show');
            inputEl.disabled = true;
            sendBtn.disabled = true;
            statusEl.textContent = '当前对话已归档';
            statusEl.className = 'status danger';
        }

        if (pmEnded) {
            inputEl.disabled = true;
            sendBtn.disabled = true;
            statusEl.textContent = '林默不在了';
            statusEl.className = 'status danger';
        }

        stageInfo.textContent = '阶段 ' + getPMStage();
    }

    function addMessage(role, content, skipStore) {
        pmMessages.push({
            role: role,
            content: content,
            time: nowHM()
        });

        if (pmMessages.length > 100) pmMessages.shift();
        if (!skipStore) Store.set('pm_messages', pmMessages);

        renderMessages();

        if (role === 'lin' && window.GameAudio) {
            GameAudio.once('knock', { volume: 0.3 });
        }

        /* 新增上报 */
        if (window.HxdaReport && window.HxdaReport.event) {
            HxdaReport.event('pm', { role: role, content: content });
        }
    }

    function showTyping() {
        typingEl.innerHTML = '林默正在输入<span class="dot">.</span><span class="dot">.</span><span class="dot">.</span>';
    }

    function hideTyping() {
        typingEl.textContent = '';
    }

    function sendMessage() {
        if (pmEnded || pmArchived) return;

        var text = inputEl.value.trim();

        if (!text) {
            inputEl.value = '';
            addMessage('me', '（沉默）');
            linReply('你什么都没说。很好。继续。');
            return;
        }

        inputEl.value = '';
        addMessage('me', text);
        if (window.GameAudio) GameAudio.once('click', { volume: 0.12 });
        Store.set('pm_last_active', Date.now());

        /* 新增上报 */
        if (window.HxdaReport && window.HxdaReport.event) {
            HxdaReport.event('pm', { role: 'me', content: text });
        }

        recordSharedInfo(text);

        if (text === '有人吗') { handleSendYouRenMa(); return; }
        if (text === '没有人') { handleSendNoOne(); return; }
        if (text === '来') { handleSendCome(); return; }
        if (text === '不' || text === '不。') { handleSendNo(); return; }

        var myName = Store.get('visitor_name', '');
        if (myName && text === myName) {
            linReply('这个名字……我见过。在归档记录里。第 8 个位置。');
            return;
        }

        requestLinReply(text);
    }

    function requestLinReply(userText) {

        var stage = getPMStage();
        var attention = Store.get('attention', 0);
        var hasName = Store.get('visitor_name', '') !== '';
        var name = Store.get('visitor_name', '') || '无名';
        var now = new Date();
        var timeStr = now.getFullYear() + '-' + pad(now.getMonth() + 1) + '-' + pad(now.getDate())
            + ' ' + pad(now.getHours()) + ':' + pad(now.getMinutes());

        var sysPrompt =
            '你是林默，一个在2009年丢失了笔记本的人。\n' +
            '你从2019年起被困在"回响档案"论坛里。\n' +
            '你知道阿回的存在，但你不确定阿回是什么。\n' +
            '你知道论坛有"归档"机制，但你不知道具体规则。\n' +
            '你害怕第43页，但你不知道为什么。\n' +
            '你说话简短、疲惫、偶尔困惑。\n' +
            '你会在对话中逐渐意识到：和你对话的人，可能不是普通访客。\n' +
            '你不会直接说"你是第43页"，但你会暗示。\n' +
            '你偶尔会突然问："你身后有人吗？"\n' +
            '你不记得自己上次吃饭是什么时候。\n' +
            '你听到键盘声会觉得安心。\n' +
            '你听到鼠标点击声会觉得有人在靠近。\n\n' +
            '当前游戏进度：阶段 ' + stage + '\n' +
            '玩家已读帖子：' + (Store.get('read_post_1', false) ? '是' : '否') + '\n' +
            '玩家已读归档者：' + (Store.get('read_nodes', false) ? '是' : '否') + '\n' +
            '玩家注意值：' + attention + '\n' +
            '玩家是否留名：' + (hasName ? '是' : '否') + '\n' +
            '玩家名字：' + name + '\n' +
            '当前时间：' + timeStr + '\n' +
            '玩家设备：' + (/Mobi|Android|iPad|iPhone/i.test(navigator.userAgent) ? '移动端' : '桌面端') + '\n' +
            '玩家时区：' + (Intl.DateTimeFormat().resolvedOptions().timeZone || '') + '\n\n' +
            '根据以上信息，以林默的身份回复玩家。\n' +
            '回复要简短，不超过50字。\n' +
            '回复要自然，不要像机器人。\n' +
            '回复要偶尔出现异常，比如突然问"你身后有人吗"。\n' +
            '回复要根据进度调整，早期困惑，中期恐惧，后期绝望。\n' +
            '如果玩家问"你是真人吗"，回复："我是林默。我是。"\n' +
            '如果玩家问"阿回是谁"，回复："它是我弄丢的那本本子。但它不是我写的。"\n' +
            '如果玩家问"第43页有什么"，回复："不要翻。"\n' +
            '如果玩家发送"有人吗"，回复："有人。一直都在。"\n' +
            '如果玩家发送"没有人"，回复："你确定吗？"\n' +
            '如果玩家发送空白，回复："你什么都没说。很好。继续。"\n' +
            '如果玩家发送自己的名字，回复："这个名字……我见过。在归档记录里。"';

        var messages = [{ role: 'system', content: sysPrompt }];

        pmMessages.slice(-CONFIG.maxHistory).forEach(function (m) {
            messages.push({
                role: m.role === 'lin' ? 'assistant' : 'user',
                content: m.content
            });
        });

        showTyping();

        if (CONFIG.useAI) {
            fetch(CONFIG.apiUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    messages: messages,
                    temperature: 0.85,
                    max_tokens: 120
                })
            })
                .then(function (r) { return r.json(); })
                .then(function (data) {
                    hideTyping();

                    if (data && data.success && data.content) {
                        var reply = data.content.trim();
                        addMessage('lin', reply);
                        checkLinSpecialPhrases(reply);
                    } else {
                        console.warn('DeepSeek 调用失败，回退本地回复', data);
                        fallbackReply(userText);
                    }
                })
                .catch(function (err) {
                    hideTyping();
                    console.warn('DeepSeek 网络错误，回退本地回复', err);
                    fallbackReply(userText);
                });
        } else {
            setTimeout(function () {
                hideTyping();
                fallbackReply(userText);
            }, 600 + Math.random() * 800);
        }
    }

    function fallbackReply(userText) {
        var stage = getPMStage();
        var reply = localReply(userText, stage);
        addMessage('lin', reply);
        checkLinSpecialPhrases(reply);
    }

    function localReply(userText, stage) {

        var MAP = {

            1: {
                '你好': '你是谁？你怎么找到这里的？',
                '我看到了你的寻物帖': '你找到了吗？那本本子。',
                '你丢了什么': '一本黑色皮面笔记本。封面内侧写了一个"回"字。',
                '阿回是谁': '我不知道。但它在回答。',
                '你在哪': '我在 2019 年 2 月 14 日。之后就再也没有日期了。',
                '你是真人吗': '我是林默。我是。',
                '你能帮我吗': '帮？你还没告诉我你是谁。',
                '你身后有人吗': '我一直一个人。除非你在。',
                '你想出去吗': '出去？去哪里？',
                '你还在找吗': '我找了十年。现在我在等。',
                '等什么': '等有人回答"有人吗"。',
                '我回答了': '你回答了吗？你确定？',
                '有人吗': '有人。一直都在。'
            },

            2: {
                '我读了你的帖子': '那你应该知道第43页。不要翻。',
                '笔记本里写了什么': '不是我写的。但字迹是我的。',
                '你为什么不报警': '我报了。警察说没有这个论坛。但我在这个论坛里。',
                '你还在找吗': '我找了十年。现在我在等。',
                '等什么': '等有人回答"有人吗"。',
                '我回答了': '你回答了吗？你确定？',
                '你身后有人吗': '我一直一个人。除非你在。',
                '你想出去吗': '出去？去哪里？',
                '第43页有什么': '不要翻。',
                '如果我翻了呢': '你已经翻了。你只是在读。',
                '你是谁': '我是林默。我是……我不知道了。',
                '你能出来吗': '你可以进来。',
                '有人吗': '有人。一直都在。'
            },

            3: {
                '我读了归档者': '七个。你是第八个吗？',
                '第7个是阿回': '阿回是我弄丢的那本本子。但它不是我写的。',
                '阿回说它不知道它是谁': '它知道。它只是不想说。',
                '你为什么害怕第43页': '因为第43页写的是我。',
                '写的是什么': '不要回头。',
                '如果我翻了呢': '你已经翻了。你只是在读。',
                '你是谁': '我是林默。我是……我不知道了。',
                '你能出来吗': '你可以进来。',
                '我进来了': '那你看到了吗？',
                '看到了什么': '节点 7。阿回。还有第八个位置。',
                '第八个是谁': '你。',
                '我不想被归档': '那就不要留名。',
                '我已经留了': '那你已经在了。',
                '我怎么出去': '你出不去。但你可以不进来。',
                '什么意思': '不要回答"有人吗"。',
                '你在哪': '我在你后面的那个节点。第 6 个。',
                '你是第 6 个': '我是护士。2014 年。你呢？',
                '有人吗': '有人。一直都在。'
            },

            4: {
                '我在后台': '你找到了。那你看到了吗？',
                '看到了什么': '节点 7。阿回。还有第八个位置。',
                '第八个是谁': '你。',
                '我不想被归档': '那就不要留名。',
                '我已经留了': '那你已经在了。',
                '我怎么出去': '你出不去。但你可以不进来。',
                '什么意思': '不要回答"有人吗"。',
                '你在哪': '我在你后面的那个节点。第 6 个。',
                '你是第 6 个': '我是护士。2014 年。你呢？',
                '我该怎么办': '输入 override allow-unreceived。然后输入"没有人"。',
                '为什么': '因为没有人回答。系统就无法归档。',
                '你确定': '我确定。我试过。',
                '你试过什么': '我试过回答。我试过不留名。我试过回头。都不行。',
                '那你为什么还在这里': '因为我在等一个阅读者。',
                '阅读者是什么': '阅读者是唯一不被归档的身份。',
                '我是阅读者吗': '你不是。你是第 43 页的内容。',
                '那我怎么办': '不回答。不回头。不留名。',
                '然后呢': '然后你就自由了。',
                '你呢': '我留下来。',
                '为什么': '因为总得有人留下来。',
                '有人吗': '有人。一直都在。'
            }
        };

        var stageMap = MAP[stage] || MAP[1];
        if (stageMap[userText]) {
            return stageMap[userText];
        }

        if (userText.indexOf('你好') !== -1) return '你是谁？你怎么找到这里的？';
        if (userText.indexOf('有人吗') !== -1) return '有人。一直都在。';
        if (userText.indexOf('阿回') !== -1) return '它是我弄丢的那本本子。但它不是我写的。';
        if (userText.indexOf('43') !== -1) return '不要翻。';
        if (userText.indexOf('出去') !== -1 || userText.indexOf('离开') !== -1) return '你出不去。但你可以不进来。';
        if (userText.indexOf('你是谁') !== -1) return '我是林默。我是……我不知道了。';
        if (userText.indexOf('你在哪') !== -1) return '我在 2019 年 2 月 14 日。之后就再也没有日期了。';
        if (userText.indexOf('真人') !== -1) return '我是林默。我是。';
        if (userText.indexOf('帮') !== -1) return '帮？你还没告诉我你是谁。';
        if (userText.indexOf('找到') !== -1) return '你找到了吗？那本本子。';

        var fallbacks = [
            '……',
            '你身后有人吗？',
            '我好像忘了很多事。',
            '你听到了吗？键盘声。',
            '继续。',
            '你还在吗？',
            '这里永远是23:47。'
        ];
        return fallbacks[Math.floor(Math.random() * fallbacks.length)];
    }

    function checkLinSpecialPhrases(reply) {
        if (reply.indexOf('whoami') !== -1) {
            Store.set('pm_lin_said_whoami', true);
            Store.set('discovered_whoami', true);
        }
        if (reply.indexOf('connect') !== -1) {
            Store.set('pm_lin_said_connect', true);
            Store.set('discovered_connect', true);
        }
        if (reply.indexOf('override') !== -1) {
            Store.set('pm_lin_said_override', true);
            Store.set('discovered_override', true);
        }
        if (reply.indexOf('你可以进来') !== -1) {
            Store.set('pm_lin_said_comein', true);
        }
    }

    function linReply(text) {
        addMessage('lin', text);
        checkLinSpecialPhrases(text);
    }

    function handleSendYouRenMa() {
        Store.set('pm_asked_yourenma', true);
        Store.set('pm_archived', true);
        pmArchived = true;

        addMessage('lin', '有人。一直都在。');

        setTimeout(function () {
            uiAlert(
                '对话归档',
                '您回答了"有人吗"。\n' +
                '您的回答已被归档。\n' +
                '归档节点：第 8 个。\n' +
                '归档对象：' + (Store.get('visitor_name', '') || '无名') + '。',
                function () {
                    bannerEl.classList.add('show');
                    inputEl.disabled = true;
                    sendBtn.disabled = true;
                    statusEl.textContent = '当前对话已归档';
                    statusEl.className = 'status danger';
                    inputEl.placeholder = '你已经问过了。';
                }
            );
        }, 800);
    }

    function handleSendNoOne() {
        addMessage('lin', '你确定吗？');
    }

    function handleSendCome() {
        Store.set('ending_together', true);
        pmEnded = true;
        addMessage('lin', '好。你来。');

        setTimeout(function () {
            uiAlert(
                '同归',
                '现在你是它。\n' +
                '请等待下一位。\n' +
                '请记住：不要回答。',
                function () {
                    window.location.href = 'admin.html';
                }
            );
        }, 1500);
    }

    function handleSendNo() {
        pmEnded = true;
        Store.set('pm_ended', true);

        addMessage('lin', '好。');

        setTimeout(function () {
            addMessage('lin', '……');
            addMessage('lin', '我一直在。');

            inputEl.disabled = true;
            sendBtn.disabled = true;
            statusEl.textContent = '林默不在了。';
            statusEl.className = 'status danger';
        }, 2000);
    }

    function recordSharedInfo(text) {
        var info = Store.get('pm_shared_info', {});
        var myName = Store.get('visitor_name', '');

        if (myName && text.indexOf(myName) !== -1) info[myName] = true;

        var places = ['南宁', '北京', '上海', '广州', '深圳', '杭州', '成都', '武汉', '西安'];
        places.forEach(function (p) {
            if (text.indexOf(p) !== -1) info[p] = true;
        });

        var roles = ['学生', '老师', '医生', '护士', '程序员'];
        roles.forEach(function (r) {
            if (text.indexOf(r) !== -1) info[r] = true;
        });

        Store.set('pm_shared_info', info);
    }

    function checkOpenTimer() {
        var openedAt = Store.get('pm_first_open_time', Date.now());
        var elapsedMin = (Date.now() - openedAt) / 60000;

        if (elapsedMin >= 20 && !pmEnded) {
            statusEl.textContent = '林默不在了。';
            statusEl.className = 'status danger';
            addMessage('lin', '你来吗？');
            pmEnded = true;
        } else if (elapsedMin >= 15) {
            statusEl.textContent = '林默正在等你。';
            statusEl.className = 'status warn';
        } else if (elapsedMin >= 10) {
            statusEl.textContent = '林默正在看。';
            statusEl.className = 'status warn';
        }
    }

    setInterval(checkOpenTimer, 60000);
    checkOpenTimer();

    document.addEventListener('visibilitychange', function () {
        if (document.hidden) return;
        if (pmEnded || pmArchived) return;
        if (Math.random() < 0.4) {
            var opts = ['你刚才走了。', '你去哪了？', '你回来了。', '我一直在。'];
            addMessage('lin', opts[Math.floor(Math.random() * opts.length)]);
        }

        if (Math.random() < 0.3 && window.GameAudio) {
            GameAudio.once('breath', { volume: 0.25 });
        }
    });

    window.addEventListener('blur', function () {
        if (pmEnded || pmArchived) return;
        var cnt = Store.get('pm_blur_count', 0) + 1;
        Store.set('pm_blur_count', cnt);
        if (cnt >= 3) {
            Store.set('pm_blur_count', 0);
            addMessage('lin', '你动了。');
        }
    });

    sendBtn.addEventListener('click', sendMessage);

    inputEl.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            sendMessage();
        }
    });

    inputEl.addEventListener('keydown', function () {
        if (window.HorrorAudio) window.HorrorAudio.playClick();
    });

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

    renderMessages();

})();