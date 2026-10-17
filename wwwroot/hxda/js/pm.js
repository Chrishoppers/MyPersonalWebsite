/* ============================================================
   回响档案 · 私信系统
   文件：wwwroot/hxda/js/pm.js
   依赖：storage.js、ui.js、tracker.js、audio.js
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

    if (!pmOpened) {
        pmOpened = true;
        Store.set('pm_opened', true);
        Store.set('pm_first_open_time', Date.now());
        if (pmMessages.length === 0) {
            pmMessages.push({ role: 'lin', content: '你好。你是谁？', time: nowHM() });
            Store.set('pm_messages', pmMessages);
        }
    }

    function getPMStage() {
        var readNodes = Store.get('read_nodes', false);
        var readPost1 = Store.get('read_post_1', false);
        var foundNode7 = Store.get('found_node7', false);
        if (readNodes && foundNode7) return 4;
        if (readNodes) return 3;
        if (readPost1) return 2;
        return 1;
    }

    function renderMessages() {
        bodyEl.innerHTML = '';

        pmMessages.forEach(function (m) {
            var div = document.createElement('div');
            div.className = 'pm-msg ' + (m.role === 'lin' ? 'from-lin' : 'from-me');

            var contentHtml;
            if (m.recalled) {
                contentHtml = '<span class="recalled">' + escapeHtml(m.content) + '</span>';
            } else if (m.imageData) {
                contentHtml = '<img src="' + m.imageData + '" alt="图片">';
            } else {
                contentHtml = escapeHtml(m.content).replace(/\n/g, '<br>');
            }

            div.innerHTML =
                '<span class="who">' + (m.role === 'lin' ? '林默' : '你') + '</span>' +
                '<span class="txt">' + contentHtml + '</span>' +
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

        if (role === 'lin' && window.GameAudio) GameAudio.once('knock', { volume: 0.3 });
        if (role === 'lin' && Math.random() < 0.12) {
            setTimeout(function () {
                showPmFlash(Math.random() < 0.5 ? 'glitch_face' : 'glitch_text');
            }, 400);
        }
    }

    /* ============================================================
       图片消息（林默发来的图片）
       ============================================================ */
    function addImageMessage(role, imageData, altText) {
        var msg = {
            role: role,
            content: altText || '[图片]',
            imageData: imageData,
            time: nowHM()
        };
        pmMessages.push(msg);
        if (pmMessages.length > 100) pmMessages.shift();
        Store.set('pm_messages', pmMessages);
        renderMessages();

        if (role === 'lin' && window.GameAudio) GameAudio.once('knock', { volume: 0.3 });
    }

    /* ============================================================
       把玩家签名 P 到归档同意书上
       ============================================================ */
    function buildAgreementImage(callback) {
        var base = new Image();
        base.crossOrigin = 'anonymous';

        base.onload = function () {
            var canvas = document.createElement('canvas');
            canvas.width = base.naturalWidth;
            canvas.height = base.naturalHeight;
            var ctx = canvas.getContext('2d');
            ctx.drawImage(base, 0, 0);

            var sigData = Store.get('gate_signature_image', '');
            if (!sigData) {
                callback(canvas.toDataURL('image/png'));
                return;
            }

            var sig = new Image();
            sig.onload = function () {
                /* ============================================================
                   甲方签名位置
                   根据你的 archive_agreement_base.jpg 实际布局调整这 4 个值：
                   x: 距左比例
                   y: 距上比例
                   w: 宽度比例
                   h: 高度比例
                   ============================================================ */
                var x = canvas.width * 0.10;
                var y = canvas.height * 0.62;
                var w = canvas.width * 0.30;
                var h = canvas.height * 0.08;

                ctx.globalAlpha = 0.85;
                ctx.drawImage(sig, x, y, w, h);
                ctx.globalAlpha = 1;

                /* 轻微扭曲感 */
                try {
                    var data = ctx.getImageData(0, 0, canvas.width, canvas.height);
                    for (var i = 0; i < data.data.length; i += 4) {
                        if (Math.random() < 0.0003) {
                            data.data[i] = 160;
                            data.data[i + 1] = 0;
                            data.data[i + 2] = 0;
                        }
                    }
                    ctx.putImageData(data, 0, 0);
                } catch (e) { }

                callback(canvas.toDataURL('image/png'));
            };
            sig.onerror = function () {
                callback(canvas.toDataURL('image/png'));
            };
            sig.src = sigData;
        };

        /* 底图不存在时的 fallback：用 canvas 画一份 */
        base.onerror = function () {
            var canvas = document.createElement('canvas');
            canvas.width = 800;
            canvas.height = 600;
            var ctx = canvas.getContext('2d');

            ctx.fillStyle = '#f4ecd8';
            ctx.fillRect(0, 0, 800, 600);
            ctx.strokeStyle = '#aaa';
            ctx.lineWidth = 2;
            ctx.strokeRect(20, 20, 760, 560);

            ctx.fillStyle = '#222';
            ctx.font = 'bold 22px SimSun, serif';
            ctx.textAlign = 'center';
            ctx.fillText('回响档案 · 归档同意书', 400, 70);

            ctx.beginPath();
            ctx.moveTo(60, 90);
            ctx.lineTo(740, 90);
            ctx.stroke();

            ctx.font = '13px SimSun, serif';
            ctx.textAlign = 'left';
            var lines = [
                '第一条　甲方（签署人）自愿将个人身份信息、留言记录、',
                '　　　　访问日志等全部资料移交乙方（归档方）保存与管理。',
                '',
                '第二条　甲方确认已阅读并理解本同意书全部内容，自愿签署。',
                '　　　　█████████████████████████████',
                '',
                '第三条　乙方承诺对甲方资料进行长期归档保存，保存期限为永久。',
                '',
                '第四条　归档编号由乙方统一分配，一经分配不可更改。',
                '',
                '第五条　甲方同意███████████████████████████',
                '',
                '第六条　█████████████不可撤销██████████████████',
                '',
                '第七条　本同意书自签署之日起生效。'
            ];
            var y = 130;
            lines.forEach(function (l) {
                ctx.fillText(l, 60, y);
                y += 24;
            });

            ctx.beginPath();
            ctx.moveTo(60, 420);
            ctx.lineTo(740, 420);
            ctx.stroke();

            ctx.font = 'bold 14px SimSun, serif';
            ctx.fillText('【甲方（签署人）】', 80, 450);
            ctx.fillText('【乙方（归档方）】', 460, 450);

            ctx.font = '12px SimSun, serif';
            ctx.fillText('姓名：____________', 80, 480);
            ctx.fillText('签名：____________', 80, 510);
            ctx.fillText('日期：____________', 80, 540);

            ctx.fillText('姓名：████████', 460, 480);
            ctx.fillText('签名：████████', 460, 510);
            ctx.fillText('日期：2019-02-14', 460, 540);

            var sigData = Store.get('gate_signature_image', '');
            if (!sigData) {
                callback(canvas.toDataURL('image/png'));
                return;
            }
            var sig = new Image();
            sig.onload = function () {
                ctx.globalAlpha = 0.85;
                ctx.drawImage(sig, 100, 500, 160, 30);
                ctx.globalAlpha = 1;
                callback(canvas.toDataURL('image/png'));
            };
            sig.onerror = function () {
                callback(canvas.toDataURL('image/png'));
            };
            sig.src = sigData;
        };

        base.src = 'img/archive_agreement_base.jpg';
    }

    /* ============================================================
       归档同意书触发流程
       ============================================================ */
    function triggerAgreementEvent() {
        if (Store.get('pm_agreement_triggered', false)) return;
        Store.set('pm_agreement_triggered', true);

        setTimeout(function () {
            addMessage('lin', '我想给你看一样东西。');
        }, 1200);

        setTimeout(function () {
            buildAgreementImage(function (dataURL) {
                if (!dataURL) {
                    addMessage('lin', '（图片加载失败）');
                    return;
                }

                addImageMessage('lin', dataURL, '[图片：归档同意书]');

                setTimeout(function () {
                    addMessage('lin', '你签过这个。你签过这个。你签过这个。');
                }, 2000);

                setTimeout(function () {
                    addMessage('lin', '回回回回回回回回回回');
                }, 2800);

                setTimeout(function () {
                    addMessage('lin', '我是谁我是谁我是谁我是谁');
                }, 3600);

                setTimeout(function () {
                    var msgs = Store.get('pm_messages', []);
                    var idx = [];
                    for (var i = msgs.length - 1; i >= 0 && idx.length < 3; i--) {
                        if (msgs[i].role === 'lin') idx.push(i);
                    }
                    idx.forEach(function (i) {
                        msgs[i].content = '（此消息已被撤回）';
                        msgs[i].recalled = true;
                    });
                    Store.set('pm_messages', msgs);
                    pmMessages = msgs;
                    renderMessages();
                    if (window.GameAudio) GameAudio.once('static', { volume: 0.1 });
                }, 5000);

                setTimeout(function () {
                    addMessage('lin', '抱歉。刚才信号不好。');
                }, 6500);

                setTimeout(function () {
                    showPmFlash('glitch_face');
                }, 7600);

                setTimeout(function () {
                    addMessage('lin', '我说的是：你签过这个，对吗？');
                }, 8000);
            });
        }, 4000);
    }

    /* ============================================================
       闪现
       ============================================================ */
    function showPmFlash(type) {
        var flash = document.getElementById('pmFlash');
        var imgEl = document.getElementById('pmFlashImg');
        if (!flash || !imgEl) return;

        if (type === 'glitch_face') {
            imgEl.src = 'img/glitch_face.jpg';
            imgEl.style.filter = 'contrast(1.5) brightness(0.8)';
        } else {
            imgEl.src = 'img/glitch_text.png';
            imgEl.style.filter = 'none';
        }
        imgEl.style.display = 'block';
        flash.classList.add('show');

        if (window.GameAudio) GameAudio.once('static', { volume: 0.12 });

        setTimeout(function () {
            flash.classList.remove('show');
            imgEl.style.display = 'none';
        }, 120);
    }

    /* ============================================================
       8 个异常事件
       ============================================================ */
    var _pmTriggered = {};

    function triggerPmEvent(id) {
        if (_pmTriggered[id]) return;
        _pmTriggered[id] = true;

        switch (id) {
            case 'glitch_title':
                setTimeout(function () {
                    var t = document.querySelector('.pm-title .who span:last-child');
                    if (t) {
                        var o = t.textContent;
                        t.textContent = '与 ████ 的对话';
                        setTimeout(function () { t.textContent = o; }, 800);
                    }
                }, 300);
                break;

            case 'glitch_node7':
                setTimeout(function () { showPmFlash('glitch_face'); }, 500);
                break;

            case 'glitch_blank':
                setTimeout(function () { addMessage('lin', '（ ）'); }, 800);
                setTimeout(function () { showPmFlash('glitch_text'); }, 1400);
                break;

            case 'glitch_b64':
                setTimeout(function () {
                    addMessage('lin', '5oiR5Zyo56ysN+WbnuWkjeaWueW8j+iAkw==');
                }, 1000);
                break;

            case 'glitch_sig':
                setTimeout(function () {
                    var sigData = Store.get('gate_signature_image', '');
                    if (!sigData) {
                        addMessage('lin', '你的名字……我见过。');
                        return;
                    }
                    var div = document.createElement('div');
                    div.className = 'pm-msg from-lin';
                    div.innerHTML =
                        '<span class="who">林默</span>' +
                        '<span class="txt"><img src="' + sigData + '" style="transform:rotate(180deg) scaleX(-1); background:#fff; padding:8px; border:1px solid #888;"></span>' +
                        '<span class="time">' + nowHM() + '</span>';
                    bodyEl.appendChild(div);
                    bodyEl.scrollTop = bodyEl.scrollHeight;
                }, 1000);
                break;

            case 'glitch_recall':
                setTimeout(function () { addMessage('lin', '你'); }, 600);
                setTimeout(function () { addMessage('lin', '你签过'); }, 1100);
                setTimeout(function () { addMessage('lin', '你签过这个'); }, 1600);
                setTimeout(function () {
                    var msgs = Store.get('pm_messages', []);
                    msgs = msgs.map(function (m) {
                        if (m.role === 'lin' && /你(签过)?(这个)?$/.test(m.content)) {
                            m.content = '（此消息已被撤回）';
                            m.recalled = true;
                        }
                        return m;
                    });
                    Store.set('pm_messages', msgs);
                    pmMessages = msgs;
                    renderMessages();
                }, 2600);
                break;

            case 'glitch_repeat':
                setTimeout(function () {
                    var t = setInterval(function () { addMessage('lin', '不要回答'); }, 300);
                    setTimeout(function () { clearInterval(t); }, 1600);
                }, 500);
                break;

            case 'glitch_red':
                setTimeout(function () {
                    document.body.style.background = '#1a0000';
                    document.body.style.transition = 'background 0.3s';
                    setTimeout(function () { document.body.style.background = ''; }, 1500);
                }, 500);
                break;

            case 'glitch_disappear':
                setTimeout(function () {
                    statusEl.textContent = '对方已离开';
                    statusEl.className = 'status danger';
                    addMessage('lin', '');
                }, 800);
                break;
        }
    }

    function checkPmEvents() {
        var n = Store.get('pm_player_msg_count', 0);
        if (n === 2) triggerPmEvent('glitch_title');
        if (n === 3) triggerPmEvent('glitch_node7');
        if (n === 4) triggerPmEvent('glitch_blank');
        if (n === 5) triggerPmEvent('glitch_b64');
        if (n === 6) triggerPmEvent('glitch_sig');
        if (n === 7) triggerPmEvent('glitch_recall');
        if (n === 8) triggerPmEvent('glitch_repeat');
        if (n === 9) triggerPmEvent('glitch_red');
        if (n === 10) triggerPmEvent('glitch_disappear');
    }

    function showTyping() {
        typingEl.innerHTML = '林默正在输入<span class="dot">.</span><span class="dot">.</span><span class="dot">.</span>';
    }
    function hideTyping() { typingEl.textContent = ''; }

    /* ============================================================
       发送消息
       ============================================================ */
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
        Store.set('pm_last_active', Date.now());

        if (window.GameAudio) GameAudio.once('click', { volume: 0.12 });

        recordSharedInfo(text);

        var msgCount = Store.get('pm_player_msg_count', 0) + 1;
        Store.set('pm_player_msg_count', msgCount);
        setTimeout(checkPmEvents, 500);

        if (msgCount >= 5 && !Store.get('pm_agreement_triggered', false)) {
            setTimeout(triggerAgreementEvent, 1500);
        }

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
        var timeStr = now.getFullYear() + '-' + pad(now.getMonth() + 1) + '-' + pad(now.getDate()) +
            ' ' + pad(now.getHours()) + ':' + pad(now.getMinutes());

        var sysPrompt =
            '你是林默，一个在2009年丢失了笔记本的人。\n' +
            '你从2019年起被困在"回响档案"论坛里。\n' +
            '你知道阿回的存在，但你不确定阿回是什么。\n' +
            '你知道论坛有"归档"机制，但你不知道具体规则。\n' +
            '你害怕第43页，但你不知道为什么。\n' +
            '你说话简短、疲惫、偶尔困惑。\n' +
            '你偶尔会突然问："你身后有人吗？"\n\n' +
            '当前游戏进度：阶段 ' + stage + '\n' +
            '玩家注意值：' + attention + '\n' +
            '玩家名字：' + name + '\n' +
            '当前时间：' + timeStr + '\n\n' +
            '回复要简短，不超过50字。回复要自然，不要像机器人。';

        var messages = [{ role: 'system', content: sysPrompt }];

        pmMessages.slice(-CONFIG.maxHistory).forEach(function (m) {
            if (m.imageData) return;
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
                body: JSON.stringify({ messages: messages, temperature: 0.85, max_tokens: 120 })
            })
                .then(function (r) { return r.json(); })
                .then(function (data) {
                    hideTyping();
                    if (data && data.success && data.content) {
                        var reply = data.content.trim();
                        addMessage('lin', reply);
                        checkLinSpecialPhrases(reply);
                    } else {
                        fallbackReply(userText);
                    }
                })
                .catch(function () {
                    hideTyping();
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
                '阿回是谁': '我不知道。但它在回答。',
                '你在哪': '我在 2019 年 2 月 14 日。之后就再也没有日期了。',
                '你是真人吗': '我是林默。我是。',
                '你身后有人吗': '我一直一个人。除非你在。',
                '有人吗': '有人。一直都在。'
            },
            2: {
                '我读了你的帖子': '那你应该知道第43页。不要翻。',
                '笔记本里写了什么': '不是我写的。但字迹是我的。',
                '第43页有什么': '不要翻。',
                '有人吗': '有人。一直都在。'
            },
            3: {
                '我读了归档者': '七个。你是第八个吗？',
                '阿回说它不知道它是谁': '它知道。它只是不想说。',
                '你为什么害怕第43页': '因为第43页写的是我。',
                '有人吗': '有人。一直都在。'
            },
            4: {
                '我在后台': '你找到了。那你看到了吗？',
                '我该怎么办': '输入 override allow-unreceived。然后输入"没有人"。',
                '你确定': '我确定。我试过。',
                '有人吗': '有人。一直都在。'
            }
        };

        var stageMap = MAP[stage] || MAP[1];
        if (stageMap[userText]) return stageMap[userText];

        if (userText.indexOf('你好') !== -1) return '你是谁？你怎么找到这里的？';
        if (userText.indexOf('有人吗') !== -1) return '有人。一直都在。';
        if (userText.indexOf('阿回') !== -1) return '它是我弄丢的那本本子。但它不是我写的。';
        if (userText.indexOf('43') !== -1) return '不要翻。';
        if (userText.indexOf('出去') !== -1 || userText.indexOf('离开') !== -1) return '你出不去。但你可以不进来。';
        if (userText.indexOf('你是谁') !== -1) return '我是林默。我是……我不知道了。';
        if (userText.indexOf('真人') !== -1) return '我是林默。我是。';

        var fallbacks = ['……', '你身后有人吗？', '我好像忘了很多事。', '你听到了吗？键盘声。', '继续。', '你还在吗？', '这里永远是23:47。'];
        return fallbacks[Math.floor(Math.random() * fallbacks.length)];
    }

    function checkLinSpecialPhrases(reply) {
        if (reply.indexOf('whoami') !== -1) { Store.set('pm_lin_said_whoami', true); Store.set('discovered_whoami', true); }
        if (reply.indexOf('connect') !== -1) { Store.set('pm_lin_said_connect', true); Store.set('discovered_connect', true); }
        if (reply.indexOf('override') !== -1) { Store.set('pm_lin_said_override', true); Store.set('discovered_override', true); }
        if (reply.indexOf('你可以进来') !== -1) { Store.set('pm_lin_said_comein', true); }
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
            uiAlert('对话归档',
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
                });
        }, 800);
    }

    function handleSendNoOne() { addMessage('lin', '你确定吗？'); }

    function handleSendCome() {
        Store.set('ending_together', true);
        pmEnded = true;
        addMessage('lin', '好。你来。');
        setTimeout(function () {
            uiAlert('同归',
                '现在你是它。\n请等待下一位。\n请记住：不要回答。',
                function () { window.location.href = 'admin.html'; });
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
        ['南宁', '北京', '上海', '广州', '深圳', '杭州', '成都', '武汉', '西安'].forEach(function (p) {
            if (text.indexOf(p) !== -1) info[p] = true;
        });
        ['学生', '老师', '医生', '护士', '程序员'].forEach(function (r) {
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

    renderMessages();

})();