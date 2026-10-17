/* ============================================================
   回响档案 · 全能解密器
   文件：wwwroot/hxda/js/cipher.js
   ============================================================ */

(function () {
    'use strict';

    function b64decode(str) {
        try { return decodeURIComponent(escape(atob(str))); }
        catch (e) { return null; }
    }

    function hex2str(hex) {
        try {
            var s = hex.replace(/\s/g, '');
            var r = '';
            for (var i = 0; i < s.length; i += 2) r += String.fromCharCode(parseInt(s.substr(i, 2), 16));
            return decodeURIComponent(escape(r));
        } catch (e) { return null; }
    }

    var MORSE = {
        '.-': 'A', '-...': 'B', '-.-.': 'C', '-..': 'D', '.': 'E',
        '..-.': 'F', '--.': 'G', '....': 'H', '..': 'I', '.---': 'J',
        '-.-': 'K', '.-..': 'L', '--': 'M', '-.': 'N', '---': 'O',
        '.--.': 'P', '--.-': 'Q', '.-.': 'R', '...': 'S', '-': 'T',
        '..-': 'U', '...-': 'V', '.--': 'W', '-..-': 'X', '-.--': 'Y',
        '--..': 'Z', '.----': '1', '..---': '2', '...--': '3',
        '....-': '4', '.....': '5', '-....': '6', '--...': '7',
        '---..': '8', '----.': '9', '-----': '0', '/': ' '
    };

    function morseDecode(str) {
        var s = str.trim().replace(/[\|｜]/g, '/');
        var words = s.split(/\s*\/\s*|\s{3,}/);
        return words.map(function (w) {
            return w.trim().split(/\s+/).map(function (c) {
                return MORSE[c] || '?';
            }).join('');
        }).join(' ');
    }

    function caesarDecode(str, shift) {
        var res = '';
        for (var i = 0; i < str.length; i++) {
            var c = str.charCodeAt(i);
            if (c >= 65 && c <= 90) res += String.fromCharCode(((c - 65 - shift + 26) % 26) + 65);
            else if (c >= 97 && c <= 122) res += String.fromCharCode(((c - 97 - shift + 26) % 26) + 97);
            else res += str[i];
        }
        return res;
    }

    function rot13(str) { return caesarDecode(str, 13); }

    function railFence(str, rails) {
        if (rails < 2) return str;
        var len = str.length;
        var grid = [];
        for (var i = 0; i < rails; i++) grid.push([]);
        var row = 0, dir = 1;
        for (var j = 0; j < len; j++) {
            grid[row].push(str[j]);
            if (row === 0) dir = 1;
            else if (row === rails - 1) dir = -1;
            row += dir;
        }
        return grid.flat().join('');
    }

    function reverseStr(str) { return str.split('').reverse().join(''); }

    var KEYBOARD = 'qwertyuiopasdfghjklzxcvbnm';
    function keyboardShift(str) {
        var res = '';
        for (var i = 0; i < str.length; i++) {
            var idx = KEYBOARD.indexOf(str[i].toLowerCase());
            if (idx !== -1) {
                var sh = KEYBOARD[(idx + 1) % 26];
                res += str[i] === str[i].toUpperCase() ? sh.toUpperCase() : sh;
            } else res += str[i];
        }
        return res;
    }

    function acrostic(str) {
        var lines = str.split(/\n/);
        var res = '';
        lines.forEach(function (l) { l = l.trim(); if (l) res += l[0]; });
        return res;
    }

    function telestich(str) {
        var lines = str.split(/\n/);
        var res = '';
        lines.forEach(function (l) { l = l.trim(); if (l) res += l[l.length - 1]; });
        return res;
    }

    function binaryDecode(str) {
        try {
            var s = str.replace(/\s/g, '');
            var r = '';
            for (var i = 0; i < s.length; i += 8) {
                var b = s.substr(i, 8);
                if (b.length === 8) r += String.fromCharCode(parseInt(b, 2));
            }
            return r;
        } catch (e) { return null; }
    }

    function caseBinary(str) {
        var s = str.replace(/[^a-zA-Z]/g, '');
        var bits = '';
        for (var i = 0; i < s.length; i++) bits += (s[i] === s[i].toUpperCase() ? '1' : '0');
        return binaryDecode(bits);
    }

    var PUNCT_MAP = { '。': '0', '，': '1', '？': '2', '！': '3', '；': '4', '：': '5', '、': '6' };
    function punctDecode(str) {
        var d = '';
        for (var i = 0; i < str.length; i++) if (PUNCT_MAP[str[i]] !== undefined) d += PUNCT_MAP[str[i]];
        return d;
    }

    var EMOJI_MAP = { '🔴': '0', '🟠': '1', '🟡': '2', '🟢': '3', '🔵': '4', '🟣': '5', '⚫': '6', '⚪': '7', '🟤': '8', '🔘': '9' };
    function emojiDecode(str) {
        var d = '';
        for (var i = 0; i < str.length; i++) if (EMOJI_MAP[str[i]] !== undefined) d += EMOJI_MAP[str[i]];
        return d;
    }

    function asciiDecode(str) {
        try {
            var arr = str.trim().split(/[\s,，]+/);
            var r = '';
            for (var i = 0; i < arr.length; i++) {
                var n = parseInt(arr[i], 10);
                if (!isNaN(n)) r += String.fromCharCode(n);
            }
            return r;
        } catch (e) { return null; }
    }

    function urlDecode(str) {
        try { return decodeURIComponent(str); }
        catch (e) { return null; }
    }

    function autoDecode(input) {
        var s = input.trim();
        if (!s) return null;

        if (/^[A-Za-z0-9+/]{8,}={0,2}$/.test(s)) {
            var b = b64decode(s);
            if (b) return { type: 'Base64', value: b };
        }

        if (/^[0-9a-fA-F\s]{8,}$/.test(s) && s.replace(/\s/g, '').length % 2 === 0) {
            var h = hex2str(s);
            if (h) return { type: '十六进制', value: h };
        }

        if (/^[.\-\s/|｜]{8,}$/.test(s) && /[.\-]/.test(s)) {
            var m = morseDecode(s);
            if (m) return { type: '摩斯电码', value: m };
        }

        if (/^[01\s]{8,}$/.test(s)) {
            var bin = binaryDecode(s);
            if (bin) return { type: '二进制', value: bin };
        }

        if (/%[0-9a-fA-F]{2}/.test(s)) {
            var u = urlDecode(s);
            if (u !== s) return { type: 'URL 编码', value: u };
        }

        if (/[🔴🟠🟡🟢🔵🟣⚫⚪🟤🔘]/.test(s)) {
            var e = emojiDecode(s);
            if (e) return { type: 'Emoji 编码', value: e };
        }

        if (/[。，？！；：、]{3,}/.test(s)) {
            var p = punctDecode(s);
            if (p) return { type: '标点编码', value: p };
        }

        if (/^\d{2,3}[\s,]+\d{2,3}/.test(s)) {
            var a = asciiDecode(s);
            if (a) return { type: 'ASCII', value: a };
        }

        var all = [];
        for (var sh = 1; sh < 26; sh++) {
            var c = caesarDecode(s, sh);
            if (/[\u4e00-\u9fa5]/.test(c) || /\b(THE|YOU|ME|CAN|HEAR|ECHO|SEVENTH)\b/i.test(c)) {
                all.push('凯撒(' + sh + ')：' + c);
            }
        }
        if (all.length) return { type: '凯撒', value: all.join('\n') };

        var r = reverseStr(s);
        if (/[\u4e00-\u9fa5]/.test(r) || /\b(THE|YOU|ME|CAN|HEAR)\b/i.test(r)) {
            return { type: '反转', value: r };
        }

        if (/[a-zA-Z]{8,}/.test(s) && /[a-z]/.test(s) && /[A-Z]/.test(s)) {
            var cb = caseBinary(s);
            if (cb) return { type: '大小写二进制', value: cb };
        }

        var k = keyboardShift(s);
        if (k !== s) return { type: '键盘移位', value: k };

        if (s.indexOf('\n') !== -1) {
            var ac = acrostic(s);
            if (ac.length > 1) return { type: '藏头诗', value: ac };
            var te = telestich(s);
            if (te.length > 1) return { type: '藏尾诗', value: te };
        }

        return { type: '未知', value: null };
    }

    function bruteForce(input) {
        var s = input.trim();
        var result = [];

        var b = b64decode(s);
        if (b && b !== s) result.push('Base64 → ' + b);

        var h = hex2str(s);
        if (h && h !== s) result.push('十六进制 → ' + h);

        if (/^[.\-\s/|｜]+$/.test(s)) {
            var m = morseDecode(s);
            if (m) result.push('摩斯 → ' + m);
        }

        if (/^[01\s]+$/.test(s)) {
            var bin = binaryDecode(s);
            if (bin) result.push('二进制 → ' + bin);
        }

        for (var sh = 1; sh < 26; sh++) {
            var c = caesarDecode(s, sh);
            if (c !== s) result.push('凯撒(' + sh + ') → ' + c);
        }

        var r = reverseStr(s);
        if (r !== s) result.push('反转 → ' + r);

        for (var rail = 2; rail <= 5; rail++) {
            result.push('栅栏(' + rail + ') → ' + railFence(s, rail));
        }

        if (/[a-zA-Z]{8,}/.test(s) && /[a-z]/.test(s) && /[A-Z]/.test(s)) {
            var cb = caseBinary(s);
            if (cb) result.push('大小写二进制 → ' + cb);
        }

        var k = keyboardShift(s);
        if (k !== s) result.push('键盘移位 → ' + k);

        return result;
    }

    window.Cipher = {
        b64decode: b64decode, hex2str: hex2str, morseDecode: morseDecode,
        caesarDecode: caesarDecode, rot13: rot13, railFence: railFence,
        reverseStr: reverseStr, keyboardShift: keyboardShift,
        acrostic: acrostic, telestich: telestich,
        binaryDecode: binaryDecode, caseBinary: caseBinary,
        punctDecode: punctDecode, emojiDecode: emojiDecode,
        asciiDecode: asciiDecode, urlDecode: urlDecode,
        autoDecode: autoDecode, bruteForce: bruteForce
    };

})();
