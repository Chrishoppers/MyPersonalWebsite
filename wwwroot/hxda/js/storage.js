/* ============================================================
   回响档案 · localStorage 管理
   文件：wwwroot/hxda/js/storage.js
   加载顺序：1
   ============================================================ */

(function () {

    'use strict';

    var PREFIX = 'hxda_';

    window.Store = {
        get: function (key, def) {
            try {
                var v = localStorage.getItem(PREFIX + key);
                return v === null ? def : JSON.parse(v);
            } catch (e) { return def; }
        },
        set: function (key, val) {
            try { localStorage.setItem(PREFIX + key, JSON.stringify(val)); } catch (e) { }
        },
        del: function (key) {
            try { localStorage.removeItem(PREFIX + key); } catch (e) { }
        },
        reset: function () {
            try {
                Object.keys(localStorage)
                    .filter(function (k) { return k.indexOf(PREFIX) === 0; })
                    .forEach(function (k) { localStorage.removeItem(k); });
            } catch (e) { }
        }
    };

    var DEFAULTS = {
        visitor_name: '',
        attention: 0,
        visited: 0,
        stay_seconds: 0,
        search_count: 0,
        refresh_count: 0,
        tab_switches: 0,
        settings_opened_count: 0,
        verify_opened_count: 0,
        search_history: [],
        search_yourenma_count: 0,
        search_own_name_count: 0,
        system_search_shown: false,
        read_post_1: false,
        read_post_2: false,
        read_post_3: false,
        read_post_4: false,
        post5_clicked: false,
        post4_visited: false,
        post4_admin_url: false,
        tooltip_liukong_seen: false,
        tooltip_yourenma_seen: false,
        tooltip_buhuitou_seen: false,
        node_opened: false,
        node_read: [],
        node_read_count: 0,
        node8_name: '',
        read_nodes: false,
        viewer_adjust_bright_count: 0,
        viewer_adjust_contrast_count: 0,
        viewer_adjust_scale_count: 0,
        viewer_invert_done: false,
        viewer_rotate_done: false,
        viewer_open_seconds: 0,
        audio_opened: false,
        audio_reverse_count: 0,
        reverse_audio: false,
        printed_count: 0,
        seen_source_code: false,
        found_node7: false,
        verify_seen_source: false,
        verify_seen_file_info: false,
        verify_seen_audio_diag: false,
        verify_seen_advanced: false,
        verify_reverse_played: false,
        theme_switch_count: 0,
        font_switch_count: 0,
        notify_switch_count: 0,
        theme_locked: false,
        font_locked: false,
        notify_locked: false,
        sign_saved: false,
        dark_mode: false,
        font_size: 'medium',
        notify_on: false,
        signature: '',
        seen_hidden_text_dark: false,
        font_large_hint: false,
        font_small_post5_big: false,
        pm_opened: false,
        pm_first_open_time: 0,
        pm_last_active: 0,
        pm_messages: [],
        pm_shared_info: {},
        pm_asked_yourenma: false,
        pm_archived: false,
        pm_ended: false,
        pm_lin_said_whoami: false,
        pm_lin_said_connect: false,
        pm_lin_said_override: false,
        pm_lin_said_comein: false,
        pm_stage: 1,
        discovered_help: true,
        discovered_list: false,
        discovered_read: false,
        discovered_whoami: false,
        discovered_disconnect: false,
        discovered_connect: false,
        discovered_override: false,
        discovered_page43: false,
        discovered_become_admin: false,
        discovered_takeover: false,
        popup1_count: 0,
        popup5_count: 0,
        popup6_count: 0,
        attention_level_shown: 0,
        visited_404: [],
        visited_404_count: 0,
        infinity_opened: false,
        infinity_block_count: 0,
        ending_archive: false,
        ending_lookback: false,
        ending_disconnect: false,
        ending_refused: false,
        ending_remembered: false,
        ending_together: false,
        ending_true: false,
        ending_all_seen: [],
        override_active: false,
        override_start_time: 0,
        online_override: '',
        terminal_history: [],
        terminal_help_count: 0,
        terminal_list_count: 0,
        terminal_invalid_count: 0,
        terminal_whoami_count: 0,
        read7_warn_count: 0,
        found_empty_cell: false,
        edge_peek_seen: false,
        esc_secret_count: 0,
        fullscreen_auto_shown: false,
        fullscreen_hint_shown: false,
        title_hint_seen_1: 0,
        title_hint_seen_2: 0,
        title_hint_seen_3: 0,
        title_hint_seen_4: 0,
        title_hint_seen_5: 0,
        found_logo_secret: false,
        attention_popup_close_count: 0,
        pm_blur_count: 0,
        ahui_said_noone: false,
        system_said_exit: false,
        gate_time_passed: false,
        gate_mic_passed: false,
        gate_mic_db: 0,
        gate_mic_test_count: 0,
        gate_read_done: false,
        gate_read_scrolled: false,
        gate_read_checked: [],
        gate_signature_name: '',
        gate_signature_drawn: false,
        gate_signature_date: '',
        gate_passed: false,
        admin_logged_in: false,
    };

    Object.keys(DEFAULTS).forEach(function (k) {
        try {
            if (localStorage.getItem(PREFIX + k) === null) {
                Store.set(k, DEFAULTS[k]);
            }
        } catch (e) { }
    });

    Store.set('visited', Store.get('visited', 0) + 1);

    setInterval(function () {
        Store.set('stay_seconds', Store.get('stay_seconds', 0) + 1);
    }, 1000);

    window.StoreUtils = {
        getName: function () { return Store.get('visitor_name', ''); },
        hasName: function () { return Store.get('visitor_name', '') !== ''; },
        addAttention: function (n) {
            var v = Store.get('attention', 0) + n;
            Store.set('attention', v);
            return v;
        },
        getAttention: function () { return Store.get('attention', 0); },
        getAttentionLevel: function () {
            var a = Store.get('attention', 0);
            if (a >= 26) return 'extreme';
            if (a >= 21) return 'very-high';
            if (a >= 16) return 'high';
            if (a >= 11) return 'mid';
            if (a >= 6) return 'low';
            return 'none';
        },
        nowHM: function () {
            var d = new Date();
            var pad = function (n) { return String(n).padStart(2, '0'); };
            return pad(d.getHours()) + ':' + pad(d.getMinutes());
        },
        nowFull: function () {
            var d = new Date();
            var pad = function (n) { return String(n).padStart(2, '0'); };
            return d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate())
                + ' ' + pad(d.getHours()) + ':' + pad(d.getMinutes()) + ':' + pad(d.getSeconds());
        },
        escapeHtml: function (s) {
            var d = document.createElement('div');
            d.textContent = String(s);
            return d.innerHTML;
        }
    };

})();