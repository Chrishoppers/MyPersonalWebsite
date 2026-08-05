@{
    ViewData["Title"] = "🧩 验证大闯关";
}

<style>
    * {
        box-sizing: border-box;
    }

    .game-container {
        max-width: 650px;
        margin: 0 auto;
        padding: 1rem 1.2rem 2rem;
        min-height: 100vh;
        font-family: 'Inter', -apple-system, sans-serif;
    }

    .game-header {
        text-align: center;
        padding: 0.5rem 0 1rem;
    }

    .game-header h1 {
        font-size: 2rem;
        font-weight: 800;
        background: linear-gradient(135deg, #8B5CF6 0%, #EC4899 50%, #F59E0B 100%);
        -webkit-background-clip: text;
        -webkit-text-fill-color: transparent;
        background-clip: text;
        margin: 0;
    }

    .game-header .subtitle {
        color: rgba(255,255,255,0.15);
        font-size: 0.8rem;
    }

    /* 状态栏 */
    .status-bar {
        display: flex;
        justify-content: space-between;
        align-items: center;
        flex-wrap: wrap;
        gap: 0.2rem 0.8rem;
        background: rgba(255,255,255,0.02);
        border: 1px solid rgba(255,255,255,0.04);
        border-radius: 16px;
        padding: 0.6rem 1.2rem;
        margin-bottom: 1rem;
    }

    .status-item {
        color: rgba(255,255,255,0.25);
        font-size: 0.7rem;
        display: flex;
        align-items: center;
        gap: 0.2rem;
    }

    .status-item .value {
        color: #fff;
        font-weight: 700;
        font-size: 1rem;
    }

    .status-item .value.lives {
        color: #EC4899;
    }
    .status-item .value.level {
        color: #8B5CF6;
    }
    .status-item .value.score {
        color: #F59E0B;
    }
    .status-item .value.combo {
        color: #34D399;
    }

    /* 进度条 */
    .progress-bar {
        width: 100%;
        height: 3px;
        background: rgba(255,255,255,0.04);
        border-radius: 10px;
        margin-bottom: 1.2rem;
        overflow: hidden;
    }

    .progress-bar .fill {
        height: 100%;
        border-radius: 10px;
        background: linear-gradient(135deg, #8B5CF6, #EC4899);
        transition: width 0.5s ease;
        width: 0%;
    }

    /* 挑战卡片 */
    .challenge-card {
        background: rgba(255,255,255,0.02);
        backdrop-filter: blur(20px);
        border: 1px solid rgba(255,255,255,0.04);
        border-radius: 24px;
        padding: 1.5rem 1.8rem;
        min-height: 280px;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        position: relative;
        overflow: hidden;
        transition: all 0.3s ease;
    }

    .challenge-card .type-badge {
        position: absolute;
        top: 0.8rem;
        right: 1rem;
        font-size: 0.5rem;
        padding: 0.1rem 0.6rem;
        border-radius: 20px;
        background: rgba(139,92,246,0.08);
        color: #8B5CF6;
        border: 1px solid rgba(139,92,246,0.04);
    }

    .challenge-card .question-text {
        color: rgba(255,255,255,0.5);
        font-size: 1rem;
        text-align: center;
        line-height: 1.6;
        margin-bottom: 1rem;
    }

    .challenge-card .question-text .icon {
        font-size: 1.3rem;
    }

    /* SVG 图片 */
    .challenge-card .captcha-image {
        border-radius: 12px;
        border: 1px solid rgba(255,255,255,0.04);
        background: rgba(255,255,255,0.02);
        max-width: 100%;
        margin: 0.2rem 0 0.8rem;
        cursor: pointer;
        transition: transform 0.3s ease;
    }

    .challenge-card .captcha-image:hover {
        transform: scale(1.02);
    }

    .challenge-card .color-display {
        font-size: 2rem;
        padding: 0.3rem 1.5rem;
        border-radius: 12px;
        background: rgba(255,255,255,0.02);
        margin: 0.2rem 0 0.8rem;
        letter-spacing: 4px;
    }

    /* 选项 */
    .options-grid {
        display: grid;
        grid-template-columns: repeat(2, 1fr);
        gap: 0.5rem;
        width: 100%;
        max-width: 380px;
    }

    .options-grid .option-btn {
        padding: 0.7rem 0.8rem;
        border: 2px solid rgba(255,255,255,0.04);
        border-radius: 14px;
        background: rgba(255,255,255,0.02);
        color: rgba(255,255,255,0.5);
        font-size: 0.9rem;
        font-weight: 500;
        cursor: pointer;
        transition: all 0.3s ease;
        font-family: inherit;
        text-align: center;
    }

    .options-grid .option-btn:hover:not(:disabled) {
        border-color: rgba(139,92,246,0.2);
        background: rgba(139,92,246,0.04);
        color: #fff;
        transform: translateY(-2px);
    }

    .options-grid .option-btn:disabled {
        cursor: not-allowed;
        opacity: 0.3;
    }

    .options-grid .option-btn.correct {
        border-color: #28a745;
        background: rgba(40,167,69,0.08);
        color: #28a745;
    }

    .options-grid .option-btn.wrong {
        border-color: #dc3545;
        background: rgba(220,53,69,0.08);
        color: #dc3545;
    }

    /* 计时器 */
    .timer-section {
        width: 100%;
        margin-top: 0.8rem;
    }

    .timer-bar {
        width: 100%;
        height: 3px;
        background: rgba(255,255,255,0.04);
        border-radius: 10px;
        overflow: hidden;
    }

    .timer-bar .timer-fill {
        height: 100%;
        border-radius: 10px;
        background: linear-gradient(135deg, #34D399, #F59E0B);
        transition: width 0.3s linear;
        width: 100%;
    }

    .timer-bar .timer-fill.warning {
        background: linear-gradient(135deg, #F59E0B, #EC4899);
    }

    .timer-bar .timer-fill.danger {
        background: linear-gradient(135deg, #EC4899, #dc3545);
    }

    .timer-text {
        text-align: right;
        color: rgba(255,255,255,0.06);
        font-size: 0.55rem;
        margin-top: 0.1rem;
    }

    /* 反馈 */
    .feedback {
        text-align: center;
        margin-top: 0.8rem;
        padding: 0.5rem 1.2rem;
        border-radius: 12px;
        font-size: 0.9rem;
        font-weight: 600;
        animation: fadeIn 0.4s ease;
        width: 100%;
        display: none;
    }

    .feedback.show {
        display: block;
    }

    .feedback.correct {
        background: rgba(40,167,69,0.06);
        color: #28a745;
        border: 1px solid rgba(40,167,69,0.06);
    }

    .feedback.wrong {
        background: rgba(220,53,69,0.06);
        color: #dc3545;
        border: 1px solid rgba(220,53,69,0.06);
    }

    .feedback .fun-message {
        font-weight: 400;
        color: rgba(255,255,255,0.3);
        font-size: 0.8rem;
        margin-top: 0.1rem;
    }

    .feedback .points-gain {
        font-weight: 400;
        color: #F59E0B;
        font-size: 0.8rem;
    }

    /* 游戏结束 */
    .game-over {
        text-align: center;
        padding: 1.5rem 0.5rem;
    }

    .game-over .big-icon {
        font-size: 3.5rem;
        margin-bottom: 0.3rem;
    }

    .game-over .title {
        color: #fff;
        font-size: 1.5rem;
        font-weight: 700;
        margin-bottom: 0.2rem;
    }

    .game-over .sub {
        color: rgba(255,255,255,0.2);
        font-size: 0.85rem;
        margin-bottom: 0.5rem;
    }

    .game-over .final-score {
        font-size: 2.5rem;
        font-weight: 800;
        color: #F59E0B;
        margin: 0.3rem 0;
    }

    .game-over .stats-grid {
        display: grid;
        grid-template-columns: repeat(3, 1fr);
        gap: 0.4rem;
        max-width: 300px;
        margin: 0.5rem auto 1.2rem;
    }

    .game-over .stats-grid .stat-item {
        background: rgba(255,255,255,0.02);
        border-radius: 12px;
        padding: 0.5rem;
        border: 1px solid rgba(255,255,255,0.03);
    }

    .game-over .stats-grid .stat-item .stat-value {
        color: #fff;
        font-size: 1.1rem;
        font-weight: 700;
    }

    .game-over .stats-grid .stat-item .stat-label {
        color: rgba(255,255,255,0.15);
        font-size: 0.5rem;
        text-transform: uppercase;
        letter-spacing: 0.04em;
    }

    .btn-restart {
        padding: 0.7rem 2.5rem;
        border: none;
        border-radius: 40px;
        background: linear-gradient(135deg, #8B5CF6, #EC4899);
        color: #fff;
        font-weight: 600;
        font-size: 0.9rem;
        cursor: pointer;
        transition: all 0.3s ease;
        font-family: inherit;
        box-shadow: 0 4px 24px rgba(108,60,225,0.15);
    }

    .btn-restart:hover {
        transform: translateY(-3px);
        box-shadow: 0 8px 40px rgba(108,60,225,0.25);
    }

    /* Toast */
    .toast-container {
        position: fixed;
        bottom: 2rem;
        right: 2rem;
        z-index: 2000;
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
        align-items: flex-end;
    }

    .toast-msg {
        padding: 0.5rem 1.2rem;
        border-radius: 40px;
        color: #fff;
        font-weight: 500;
        animation: slideUp 0.4s ease;
        font-size: 0.8rem;
        backdrop-filter: blur(10px);
        background: rgba(0, 0, 0, 0.6);
        border: 1px solid rgba(255, 255, 255, 0.04);
    }

    .toast-msg.success {
        background: rgba(40, 167, 69, 0.8);
    }
    .toast-msg.error {
        background: rgba(220, 53, 69, 0.8);
    }
    .toast-msg.info {
        background: rgba(79, 172, 254, 0.8);
    }

    @@keyframes fadeIn {
        from {
            opacity: 0;
            transform: translateY(10px);
        }
        to {
            opacity: 1;
            transform: translateY(0);
        }
    }

    @@keyframes slideUp {
        from {
            opacity: 0;
            transform: translateY(20px) scale(0.95);
        }
        to {
            opacity: 1;
            transform: translateY(0) scale(1);
        }
    }

    @@keyframes shake {
        0%,
        100% {
            transform: translateX(0);
        }
        25% {
            transform: translateX(-8px);
        }
        75% {
            transform: translateX(8px);
        }
    }

    .shake {
        animation: shake 0.4s ease;
    }

    @@media (max-width: 640px) {
        .game-header h1 {
            font-size: 1.4rem;
        }
        .challenge-card {
            padding: 1rem 1rem;
            min-height: 240px;
        }
        .options-grid {
            grid-template-columns: 1fr 1fr;
            gap: 0.3rem;
        }
        .status-bar {
            padding: 0.4rem 0.8rem;
            justify-content: center;
            gap: 0.1rem 0.6rem;
        }
        .status-item {
            font-size: 0.6rem;
        }
        .status-item .value {
            font-size: 0.85rem;
        }
        .game-over .final-score {
            font-size: 2rem;
        }
    }
</style>

<div class="game-container">

    <!-- 头部 -->
    <div class="game-header">
        <h1>🧩 验证大闯关</h1>
        <p class="subtitle">20种模式 × 5关 = 100关 · 证明你是人类</p>
    </div>

    <!-- 状态栏 -->
    <div class="status-bar" id="statusBar">
        <div class="status-item">❤️ 生命 <span class="value lives" id="livesDisplay">❤️❤️❤️</span></div>
        <div class="status-item">📊 关卡 <span class="value level" id="levelDisplay">1</span></div>
        <div class="status-item">🏆 积分 <span class="value score" id="scoreDisplay">0</span></div>
        <div class="status-item">🔥 连击 <span class="value combo" id="comboDisplay">0</span></div>
        <div class="status-item">✅ 通关 <span class="value" id="passedDisplay" style="color:#8B5CF6;">0</span></div>
    </div>

    <!-- 进度条 -->
    <div class="progress-bar">
        <div class="fill" id="progressFill"></div>
    </div>

    <!-- 挑战卡片 -->
    <div class="challenge-card" id="challengeCard">
        <div class="type-badge" id="typeBadge">第 1 关</div>
        <div class="question-text" id="questionText">加载中...</div>
        <div id="imageContainer" style="display:none;"><div class="captcha-image" id="captchaImage"></div></div>
        <div id="colorContainer" style="display:none;"><div class="color-display" id="colorDisplay">██████</div></div>
        <div class="options-grid" id="optionsGrid"></div>
        <div class="timer-section">
            <div class="timer-bar"><div class="timer-fill" id="timerFill"></div></div>
            <div class="timer-text" id="timerText">⏱ 15s</div>
        </div>
        <div class="feedback" id="feedback"></div>
    </div>

</div>

<!-- Toast -->
<div class="toast-container" id="toastContainer"></div>

<script>
    var token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

    // ============================================================
    // 20种验证类型定义（纯 JavaScript）
    // ============================================================
    var CHALLENGE_TYPES = [
        { name: '文字识别', icon: '👁️' },
        { name: '算术计算', icon: '🧮' },
        { name: '汉字笔画', icon: '📝' },
        { name: '颜色识别', icon: '🎨' },
        { name: '找不同', icon: '🔍' },
        { name: '倒序识别', icon: '🔄' },
        { name: '缺失字母', icon: '🔤' },
        { name: '快速点击', icon: '⚡' },
        { name: '成语填空', icon: '📖' },
        { name: '中文数字', icon: '🔢' },
        { name: '大小写转换', icon: '🔤' },
        { name: '读音识别', icon: '🔊' },
        { name: '反色识别', icon: '🎨' },
        { name: '镜像字母', icon: '🪞' },
        { name: '键盘相邻', icon: '⌨️' },
        { name: '汉字拆分', icon: '✂️' },
        { name: '数字记忆', icon: '🧠' },
        { name: '方向判断', icon: '🧭' },
        { name: '字符计数', icon: '🔢' },
        { name: '终极混合', icon: '💀' }
    ];

    // 字符集
    var CHARS = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
    var CHARS_ARR = CHARS.split('');
    var ALPHABET = 'ABCDEFGHJKLMNPQRSTUVWXYZ'.split('');

    // ============================================================
    // 游戏状态
    // ============================================================
    var gameState = {
        level: 1,
        score: 0,
        lives: 3,
        combo: 0,
        maxCombo: 0,
        passed: 0,
        isPlaying: false,
        isAnswered: false,
        timer: null,
        timeLeft: 15,
        totalLevels: 100,
        currentChallenge: null
    };

    // ============================================================
    // DOM 引用
    // ============================================================
    var dom = {
        levelDisplay: document.getElementById('levelDisplay'),
        scoreDisplay: document.getElementById('scoreDisplay'),
        livesDisplay: document.getElementById('livesDisplay'),
        comboDisplay: document.getElementById('comboDisplay'),
        passedDisplay: document.getElementById('passedDisplay'),
        progressFill: document.getElementById('progressFill'),
        typeBadge: document.getElementById('typeBadge'),
        questionText: document.getElementById('questionText'),
        optionsGrid: document.getElementById('optionsGrid'),
        imageContainer: document.getElementById('imageContainer'),
        captchaImage: document.getElementById('captchaImage'),
        colorContainer: document.getElementById('colorContainer'),
        colorDisplay: document.getElementById('colorDisplay'),
        feedback: document.getElementById('feedback'),
        timerFill: document.getElementById('timerFill'),
        timerText: document.getElementById('timerText'),
        challengeCard: document.getElementById('challengeCard')
    };

    // ============================================================
    // 工具函数
    // ============================================================
    function randomInt(min, max) {
        return Math.floor(Math.random() * (max - min + 1)) + min;
    }

    function randomPick(arr) {
        return arr[Math.floor(Math.random() * arr.length)];
    }

    function shuffle(arr) {
        for (var i = arr.length - 1; i > 0; i--) {
            var j = randomInt(0, i);
            var temp = arr[i];
            arr[i] = arr[j];
            arr[j] = temp;
        }
        return arr;
    }

    function generateSvg(text, distortion, lineCount) {
        var chars = text.split('');
        var spacing = Math.floor(280 / chars.length);
        var lines = '';
        for (var i = 0; i < lineCount; i++) {
            var r = randomInt(100, 220);
            var g = randomInt(100, 220);
            var b = randomInt(100, 220);
            lines += '<line x1="' + randomInt(0, 320) + '" y1="' + randomInt(0, 90) +
                '" x2="' + randomInt(0, 320) + '" y2="' + randomInt(0, 90) +
                '" stroke="rgb(' + r + ',' + g + ',' + b + ')" stroke-width="' + randomInt(1, 3) +
                '" opacity="0.4"/>';
        }
        var texts = '';
        for (var j = 0; j < chars.length; j++) {
            var r2 = randomInt(10, 80);
            var g2 = randomInt(10, 80);
            var b2 = randomInt(10, 80);
            var angle = randomInt(-distortion, distortion);
            var x = 20 + j * spacing + randomInt(-5, 5);
            texts += '<text x="' + x + '" y="50" font-family="Arial, sans-serif" font-size="36" font-weight="bold" fill="rgb(' +
                r2 + ',' + g2 + ',' + b2 + ')" transform="rotate(' + angle + ' ' + x + ' 50)" text-anchor="middle" dominant-baseline="central">' +
                chars[j] + '</text>';
        }
        return '<svg xmlns="http://www.w3.org/2000/svg" width="320" height="90" viewBox="0 0 320 90">' +
            '<rect width="320" height="90" rx="10" fill="#f0f0f0"/>' + lines + texts + '</svg>';
    }

    function generateQuickSvg(target) {
        return generateSvg(target, 10, 10);
    }

    function generateOptions(correct, count) {
        var options = [correct];
        while (options.length < count) {
            var fake = '';
            for (var i = 0; i < correct.length; i++) {
                fake += randomPick(CHARS_ARR);
            }
            if (options.indexOf(fake) === -1) {
                options.push(fake);
            }
        }
        return shuffle(options);
    }

    function generateNumberOptions(correct, count, level) {
        var options = [correct.toString()];
        var range = Math.max(3, 5 + Math.floor(level / 10));
        while (options.length < count) {
            var fake = correct + randomInt(-range, range);
            if (fake < 0) fake = randomInt(1, 20);
            var str = fake.toString();
            if (options.indexOf(str) === -1 && str !== correct.toString()) {
                options.push(str);
            }
        }
        return shuffle(options);
    }

    function generateOptionsChar(correct, count) {
        var options = [correct];
        while (options.length < count) {
            var c = randomPick(CHARS_ARR);
            if (options.indexOf(c) === -1) {
                options.push(c);
            }
        }
        return shuffle(options);
    }

    // ============================================================
    // 20种挑战生成器
    // ============================================================
    var generators = {

        // 0: 文字识别
        text: function(level, progress) {
            var len = Math.min(3 + progress, 6);
            var text = '';
            for (var i = 0; i < len; i++) text += randomPick(CHARS_ARR);
            var svg = generateSvg(text, 5 + progress * 4, 10 + progress * 3);
            return {
                question: '👁️ 请输入下方图片中的文字',
                imageSvg: svg,
                correctAnswer: text.toUpperCase(),
                options: generateOptions(text, 4),
                timeLimit: 15
            };
        },

        // 1: 算术计算
        arithmetic: function(level, progress) {
            var maxNum = 10 + progress * 8;
            var a = randomInt(5, maxNum);
            var b = randomInt(1, Math.floor(maxNum / 2));
            var ops = ['+', '-', '×'];
            var op = randomPick(ops);
            var result = op === '+' ? a + b : op === '-' ? a - b : a * b;
            return {
                question: '🧮 ' + a + ' ' + op + ' ' + b + ' = ?',
                correctAnswer: result.toString(),
                options: generateNumberOptions(result, 4, level),
                timeLimit: 12
            };
        },

        // 2: 汉字笔画
        stroke: function(level, progress) {
            var chars = '一二三四五六七八九十人大小天地日月水火山石木花草鸟鱼马牛羊虫风云雨雪';
            var ch = randomPick(chars.split(''));
            var strokeMap = {
                '一': 1,
                '二': 2,
                '三': 3,
                '四': 5,
                '五': 4,
                '六': 4,
                '七': 2,
                '八': 2,
                '九': 2,
                '十': 2,
                '人': 2,
                '大': 3,
                '天': 4,
                '地': 6,
                '日': 4,
                '月': 4,
                '水': 4,
                '火': 4,
                '山': 3,
                '石': 5,
                '木': 4,
                '花': 7,
                '草': 9,
                '鸟': 5,
                '鱼': 8,
                '马': 3,
                '牛': 4,
                '羊': 6,
                '虫': 6,
                '云': 4,
                '风': 4,
                '雨': 8,
                '雪': 11,
                '星': 9
            };
            var stroke = strokeMap[ch] || randomInt(3, 10);
            return {
                question: '📝 「' + ch + '」字有几画？',
                correctAnswer: stroke.toString(),
                options: generateNumberOptions(stroke, 4, level),
                timeLimit: 12
            };
        },

        // 3: 颜色识别
        color: function(level, progress) {
            var colors = [
                { name: '红色', hex: '#FF0000' },
                { name: '蓝色', hex: '#0055FF' },
                { name: '绿色', hex: '#00AA00' },
                { name: '黄色', hex: '#DDBB00' },
                { name: '紫色', hex: '#8800CC' },
                { name: '橙色', hex: '#FF6600' },
                { name: '粉色', hex: '#FF4499' },
                { name: '青色', hex: '#00CCCC' },
                { name: '棕色', hex: '#8B4513' },
                { name: '金色', hex: '#DAA520' }
            ];
            var c = randomPick(colors);
            var options = [c.name];
            var pool = colors.filter(function(x) { return x.name !== c.name; });
            while (options.length < 4 && pool.length > 0) {
                var idx = randomInt(0, pool.length - 1);
                options.push(pool[idx].name);
                pool.splice(idx, 1);
            }
            return {
                question: '🎨 下面文字是什么颜色？',
                displayText: '<span style="color:' + c.hex +
                ';font-size:2rem;font-weight:bold;">██████</span>',
                correctAnswer: c.name,
                options: shuffle(options),
                timeLimit: 10
            };
        },

        // 4: 找不同
        findDifferent: function(level, progress) {
            var target = randomPick(CHARS_ARR);
            var options = [target];
            while (options.length < 4) {
                var c = randomPick(CHARS_ARR);
                if (options.indexOf(c) === -1) options.push(c);
            }
            return {
                question: '🔍 哪个字符与其他的不同？',
                correctAnswer: target,
                options: shuffle(options),
                timeLimit: 10
            };
        },

        // 5: 倒序识别
        reverse: function(level, progress) {
            var len = Math.min(3 + progress, 5);
            var text = '';
            for (var i = 0; i < len; i++) text += randomPick(CHARS_ARR);
            var reversed = text.split('').reverse().join('');
            var svg = generateSvg(text, 10 + progress * 4, 10 + progress * 3);
            return {
                question: '🔄 图片中的文字是什么？（倒过来了）',
                imageSvg: svg,
                correctAnswer: reversed.toUpperCase(),
                options: generateOptions(reversed, 4),
                timeLimit: 14
            };
        },

        // 6: 缺失字母
        missingLetter: function(level, progress) {
            var len = Math.min(3 + progress, 6);
            var word = '';
            for (var i = 0; i < len; i++) word += randomPick(ALPHABET);
            var idx = randomInt(0, len - 1);
            var correct = word[idx];
            var display = word.split('');
            display[idx] = '_';
            return {
                question: '🔤 补全单词：' + display.join(''),
                correctAnswer: correct,
                options: generateOptionsChar(correct, 4),
                timeLimit: 10
            };
        },

        // 7: 快速点击
        quickTap: function(level, progress) {
            var target = randomPick(CHARS_ARR);
            var options = [target];
            while (options.length < 4) {
                var c = randomPick(CHARS_ARR);
                if (options.indexOf(c) === -1) options.push(c);
            }
            var svg = generateQuickSvg(target);
            return {
                question: '⚡ 快速找到目标字符！',
                imageSvg: svg,
                correctAnswer: target,
                options: shuffle(options),
                timeLimit: Math.max(3, 8 - Math.floor(level / 8))
            };
        },

        // 8: 成语填空
        idiom: function(level, progress) {
            var idioms = ['一马当先', '龙飞凤舞', '画蛇添足', '守株待兔', '狐假虎威',
                '马到成功', '鸟语花香', '鱼目混珠', '鹤立鸡群', '龙腾虎跃',
                '画龙点睛', '亡羊补牢', '杯弓蛇影', '指鹿为马', '马不停蹄'
            ];
            var idiom = randomPick(idioms);
            var idx = randomInt(0, idiom.length - 1);
            var correct = idiom[idx];
            var display = idiom.split('');
            display[idx] = '□';
            return {
                question: '📖 补全成语：' + display.join(''),
                correctAnswer: correct,
                options: generateOptionsChar(correct, 4),
                timeLimit: 10
            };
        },

        // 9: 中文数字
        chineseNumber: function(level, progress) {
            var cn = ['零', '一', '二', '三', '四', '五', '六', '七', '八', '九', '十'];
            var num = randomInt(0, 10);
            return {
                question: '🔢 「' + cn[num] + '」对应的数字是？',
                correctAnswer: num.toString(),
                options: generateNumberOptions(num, 4, level),
                timeLimit: 8
            };
        },

        // 10: 大小写转换
        caseConversion: function(level, progress) {
            var c = randomPick(ALPHABET);
            var isUpper = Math.random() > 0.5;
            return {
                question: isUpper ? '🔤 字母「' + c + '」的小写是？' : '🔤 字母「' + c.toLowerCase() + '」的大写是？',
                correctAnswer: isUpper ? c.toLowerCase() : c,
                options: generateOptionsChar(isUpper ? c.toLowerCase() : c, 4),
                timeLimit: 8
            };
        },

        // 11: 读音识别
        pinyin: function(level, progress) {
            var c = randomPick(ALPHABET);
            return {
                question: '🔊 字母「' + c + '」的读音是？',
                correctAnswer: c,
                options: generateOptionsChar(c, 4),
                timeLimit: 8
            };
        },

        // 12: 反色识别
        inverseColor: function(level, progress) {
            var colors = [
                { name: '黑色', hex: '#000000' },
                { name: '白色', hex: '#FFFFFF' }
            ];
            var idx = Math.random() > 0.5 ? 0 : 1;
            var color = colors[idx];
            var bgColor = idx === 0 ? '#FFFFFF' : '#000000';
            return {
                question: '🎨 下面文字是什么颜色？（注意背景）',
                displayText: '<span style="color:' + color.hex + ';background:' + bgColor +
                    ';padding:0.3rem 1.5rem;border-radius:8px;font-size:2rem;font-weight:bold;">██████</span>',
                correctAnswer: color.name,
                options: ['黑色', '白色'],
                timeLimit: 8
            };
        },

        // 13: 镜像字母
        mirror: function(level, progress) {
            var c = randomPick(ALPHABET);
            var mirrorMap = { 'A': 'A', 'B': 'B', 'C': 'C', 'D': 'D', 'E': 'E',
                'H': 'H', 'I': 'I', 'M': 'M', 'O': 'O', 'T': 'T',
                'U': 'U', 'V': 'V', 'W': 'W', 'X': 'X', 'Y': 'Y'
            };
            var correct = mirrorMap[c] || c;
            return {
                question: '🪞 字母「' + c + '」的镜像字母是？',
                correctAnswer: correct,
                options: generateOptionsChar(correct, 4),
                timeLimit: 8
            };
        },

        // 14: 键盘相邻
        keyboard: function(level, progress) {
            var c = randomPick(ALPHABET);
            return {
                question: '⌨️ 键盘上「' + c + '」的右边键是？',
                correctAnswer: c,
                options: generateOptionsChar(c, 4),
                timeLimit: 8
            };
        },

        // 15: 汉字拆分
        splitChar: function(level, progress) {
            var chars = ['明', '林', '从', '众', '晶', '森', '焱', '磊', '鑫', '淼'];
            var c = randomPick(chars);
            var count = c.length;
            return {
                question: '✂️ 「' + c + '」可以拆成几个相同字？',
                correctAnswer: count.toString(),
                options: generateNumberOptions(count, 4, level),
                timeLimit: 8
            };
        },

        // 16: 数字记忆
        memory: function(level, progress) {
            var len = Math.min(3 + progress, 6);
            var text = '';
            for (var i = 0; i < len; i++) text += randomInt(0, 9);
            return {
                question: '🧠 记住这个数字：' + text + ' （然后选择它）',
                correctAnswer: text,
                options: generateOptions(text, 4),
                timeLimit: 12
            };
        },

        // 17: 方向判断
        direction: function(level, progress) {
            var dirs = ['上', '下', '左', '右'];
            var dir = randomPick(dirs);
            var opposite = { '上': '下', '下': '上', '左': '右', '右': '左' };
            return {
                question: '🧭 请选择「' + dir + '」的相反方向',
                correctAnswer: opposite[dir],
                options: ['上', '下', '左', '右'],
                timeLimit: 8
            };
        },

        // 18: 字符计数
        countChar: function(level, progress) {
            var len = Math.min(6 + progress, 10);
            var text = '';
            for (var i = 0; i < len; i++) text += randomPick(CHARS_ARR);
            var target = randomPick(CHARS_ARR);
            var count = 0;
            for (var j = 0; j < text.length; j++) {
                if (text[j] === target) count++;
            }
            return {
                question: '🔢 字符「' + target + '」在「' + text + '」中出现几次？',
                correctAnswer: count.toString(),
                options: generateNumberOptions(count, 4, level),
                timeLimit: 10
            };
        },

        // 19: 终极混合
        ultimate: function(level, progress) {
            // 随机调用前19种中的一种
            var keys = ['text', 'arithmetic', 'stroke', 'color', 'findDifferent',
                'reverse', 'missingLetter', 'quickTap', 'idiom', 'chineseNumber',
                'caseConversion', 'pinyin', 'inverseColor', 'mirror', 'keyboard',
                'splitChar', 'memory', 'direction', 'countChar'
            ];
            var key = randomPick(keys);
            var result = generators[key](level, progress);
            result.question = '💀 终极挑战！' + result.question;
            result.timeLimit = Math.max(3, (result.timeLimit || 10) - 2);
            return result;
        }
    };

    // 按顺序排列的生成器键
    var GENERATOR_KEYS = ['text', 'arithmetic', 'stroke', 'color', 'findDifferent',
        'reverse', 'missingLetter', 'quickTap', 'idiom', 'chineseNumber',
        'caseConversion', 'pinyin', 'inverseColor', 'mirror', 'keyboard',
        'splitChar', 'memory', 'direction', 'countChar', 'ultimate'
    ];

    // ============================================================
    // 生成挑战
    // ============================================================
    function generateChallenge(level) {
        var typeIndex = Math.floor((level - 1) / 5);
        if (typeIndex >= GENERATOR_KEYS.length) typeIndex = GENERATOR_KEYS.length - 1;
        var key = GENERATOR_KEYS[typeIndex];
        var progress = ((level - 1) % 5) + 1;
        var generator = generators[key];
        var result = generator(level, progress);
        result.typeIndex = typeIndex;
        result.typeName = CHALLENGE_TYPES[typeIndex]?.name || '未知';
        result.typeIcon = CHALLENGE_TYPES[typeIndex]?.icon || '🧩';
        result.level = level;
        result.points = 10 + Math.floor(level / 5);
        return result;
    }

    // ============================================================
    // 游戏逻辑
    // ============================================================
    function startGame() {
        gameState.level = 1;
        gameState.score = 0;
        gameState.lives = 3;
        gameState.combo = 0;
        gameState.maxCombo = 0;
        gameState.passed = 0;
        gameState.isPlaying = true;
        gameState.isAnswered = false;
        updateUI();
        loadChallenge();
    }

    function loadChallenge() {
        if (!gameState.isPlaying) return;
        if (gameState.level > gameState.totalLevels) {
            gameWin();
            return;
        }

        gameState.isAnswered = false;
        dom.feedback.className = 'feedback';
        dom.feedback.style.display = 'none';

        var challenge = generateChallenge(gameState.level);
        gameState.currentChallenge = challenge;
        renderChallenge(challenge);
    }

    function renderChallenge(challenge) {
        dom.typeBadge.textContent = '第 ' + challenge.level + ' 关 · ' + challenge.typeName;

        var questionText = challenge.question || '识别以下内容';
        dom.questionText.innerHTML = questionText;

        dom.imageContainer.style.display = 'none';
        dom.colorContainer.style.display = 'none';

        if (challenge.imageSvg) {
            dom.imageContainer.style.display = 'block';
            dom.captchaImage.innerHTML = challenge.imageSvg;
        } else if (challenge.displayText) {
            dom.colorContainer.style.display = 'block';
            dom.colorDisplay.innerHTML = challenge.displayText;
        }

        var options = challenge.options || ['A', 'B', 'C', 'D'];
        var letters = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H'];
        var html = '';
        for (var i = 0; i < options.length; i++) {
            html += '<button class="option-btn" data-index="' + i + '" data-answer="' + options[i] + '">' +
                letters[i] + '. ' + options[i] + '</button>';
        }
        dom.optionsGrid.innerHTML = html;

        dom.optionsGrid.querySelectorAll('.option-btn').forEach(function(btn) {
            btn.addEventListener('click', function() {
                if (gameState.isAnswered) return;
                var answer = this.dataset.answer;
                if (answer) submitAnswer(challenge, answer, this);
            });
        });

        var timeLimit = challenge.timeLimit || 15;
        startTimer(timeLimit);
        updateUI();
    }

    function submitAnswer(challenge, answer, btn) {
        if (gameState.isAnswered) return;
        gameState.isAnswered = true;
        clearInterval(gameState.timer);

        var isCorrect = answer === challenge.correctAnswer;

        dom.optionsGrid.querySelectorAll('.option-btn').forEach(function(b) {
            b.disabled = true;
            if (b.dataset.answer === challenge.correctAnswer) {
                b.classList.add('correct');
            }
        });

        if (!isCorrect) {
            btn.classList.add('wrong');
            dom.challengeCard.classList.add('shake');
            setTimeout(function() {
                dom.challengeCard.classList.remove('shake');
            }, 500);
        }

        var points = 0;
        if (isCorrect) {
            var bonus = Math.min(gameState.combo, 10);
            points = challenge.points || 10 + Math.floor(gameState.level / 5);
            points += bonus * 2;
            gameState.score += points;
            gameState.combo++;
            gameState.passed++;
            if (gameState.combo > gameState.maxCombo) {
                gameState.maxCombo = gameState.combo;
            }
            showFeedback(true, points, challenge.funMessage);
        } else {
            gameState.combo = 0;
            gameState.lives--;
            showFeedback(false, 0, '');
        }

        updateUI();

        if (gameState.lives <= 0) {
            setTimeout(gameOver, 1200);
            return;
        }

        if (isCorrect) {
            gameState.level++;
            setTimeout(loadChallenge, 1500);
        } else {
            setTimeout(loadChallenge, 1500);
        }
    }

    function showFeedback(isCorrect, points, funMessage) {
        var el = dom.feedback;
        el.style.display = 'block';
        el.className = 'feedback show ' + (isCorrect ? 'correct' : 'wrong');

        if (isCorrect) {
            el.innerHTML = '✅ 答对了！ +' + points + ' 分' +
                (funMessage ? '<div class="fun-message">' + funMessage + '</div>' : '');
        } else {
            el.innerHTML = '❌ 答错了！连击中断';
        }
    }

    function startTimer(limit) {
        clearInterval(gameState.timer);
        gameState.timeLeft = limit;
        updateTimerDisplay();

        gameState.timer = setInterval(function() {
            gameState.timeLeft--;
            updateTimerDisplay();

            if (gameState.timeLeft <= 0) {
                clearInterval(gameState.timer);
                if (!gameState.isAnswered) {
                    gameState.isAnswered = true;
                    dom.optionsGrid.querySelectorAll('.option-btn').forEach(function(b) {
                        b.disabled = true;
                    });
                    gameState.combo = 0;
                    gameState.lives--;
                    updateUI();
                    showFeedback(false, 0, '⏱ 时间到！');
                    if (gameState.lives <= 0) {
                        setTimeout(gameOver, 1200);
                    } else {
                        setTimeout(loadChallenge, 1500);
                    }
                }
            }
        }, 1000);
    }

    function updateTimerDisplay() {
        var pct = (gameState.timeLeft / Math.max(3, 20)) * 100;
        dom.timerFill.style.width = pct + '%';
        dom.timerText.textContent = '⏱ ' + gameState.timeLeft + 's';

        dom.timerFill.className = 'timer-fill';
        if (pct < 30) dom.timerFill.classList.add('danger');
        else if (pct < 50) dom.timerFill.classList.add('warning');
    }

    function updateUI() {
        dom.levelDisplay.textContent = gameState.level;
        dom.scoreDisplay.textContent = gameState.score;
        dom.comboDisplay.textContent = gameState.combo;
        dom.passedDisplay.textContent = gameState.passed;

        var hearts = '❤️'.repeat(gameState.lives) + '🖤'.repeat(Math.max(0, 3 - gameState.lives));
        dom.livesDisplay.textContent = hearts || '💀';

        var progress = (gameState.passed / gameState.totalLevels) * 100;
        dom.progressFill.style.width = Math.min(progress, 100) + '%';
    }

    function gameOver() {
        gameState.isPlaying = false;
        clearInterval(gameState.timer);

        var finalScore = gameState.score;

        dom.challengeCard.innerHTML = `
            <div class="game-over">
                <div class="big-icon">${finalScore > 100 ? '🏆' : '💪'}</div>
                <div class="title">${finalScore > 100 ? '🎉 挑战结束！' : '😅 继续加油！'}</div>
                <div class="sub">你成功通过了 ${gameState.passed} 关</div>
                <div class="final-score">${finalScore}</div>
                <div class="stats-grid">
                    <div class="stat-item">
                        <div class="stat-value">${gameState.passed}</div>
                        <div class="stat-label">通关</div>
                    </div>
                    <div class="stat-item">
                        <div class="stat-value">${gameState.maxCombo}</div>
                        <div class="stat-label">最高连击</div>
                    </div>
                    <div class="stat-item">
                        <div class="stat-value">${gameState.level - 1}</div>
                        <div class="stat-label">到达关卡</div>
                    </div>
                </div>
                <button class="btn-restart" onclick="startGame()">🔄 再来一次</button>
            </div>
        `;
    }

    function gameWin() {
        gameState.isPlaying = false;
        clearInterval(gameState.timer);

        dom.challengeCard.innerHTML = `
            <div class="game-over">
                <div class="big-icon">👑</div>
                <div class="title">🎉 恭喜通关！</div>
                <div class="sub">你通过了全部 100 关！你是真正的人类之光！</div>
                <div class="final-score">${gameState.score}</div>
                <div class="stats-grid">
                    <div class="stat-item">
                        <div class="stat-value">100</div>
                        <div class="stat-label">通关</div>
                    </div>
                    <div class="stat-item">
                        <div class="stat-value">${gameState.maxCombo}</div>
                        <div class="stat-label">最高连击</div>
                    </div>
                    <div class="stat-item">
                        <div class="stat-value">🏆</div>
                        <div class="stat-label">传说</div>
                    </div>
                </div>
                <button class="btn-restart" onclick="startGame()">🔄 再来一次</button>
            </div>
        `;
    }

    function showToast(message, type) {
        var container = document.getElementById('toastContainer');
        var toast = document.createElement('div');
        toast.className = 'toast-msg ' + (type || 'info');
        toast.textContent = message;
        container.appendChild(toast);
        setTimeout(function() {
            toast.style.opacity = '0';
            toast.style.transform = 'translateX(20px)';
            setTimeout(function() { toast.remove(); }, 300);
        }, 3000);
    }

    // ============================================================
    // 启动
    // ============================================================
    document.addEventListener('DOMContentLoaded', startGame);
</script>
