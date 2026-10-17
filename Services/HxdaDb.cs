using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyPersonalWebsite.Models;

namespace MyPersonalWebsite.Services
{
    public class HxdaDb
    {
        private readonly HttpClient _http;
        private readonly ILogger<HxdaDb> _logger;
        private readonly string _baseUrl;
        private readonly string _token;
        private readonly string _configError;

        public HxdaDb(IConfiguration config, IHttpClientFactory httpFactory, ILogger<HxdaDb> logger)
        {
            _http = httpFactory.CreateClient();
            _logger = logger;

            var url = config["Turso:Url"] ?? "";
            _token = config["Turso:Token"] ?? "";

            if (string.IsNullOrEmpty(url))
            {
                _configError = "Turso:Url 未配置";
                _logger.LogError(_configError);
                _baseUrl = "";
                return;
            }
            if (string.IsNullOrEmpty(_token))
            {
                _configError = "Turso:Token 未配置";
                _logger.LogError(_configError);
                _baseUrl = "";
                return;
            }

            _baseUrl = url.Replace("libsql://", "https://").TrimEnd('/');
            _logger.LogInformation("[HxdaDb] BaseUrl = " + _baseUrl);
        }

        public string ConfigError => _configError;
        public bool IsConfigured => string.IsNullOrEmpty(_configError);

        /* ============================================================
           底层：执行 SQL
           ============================================================ */
        private async Task<JsonElement?> ExecuteAsync(string sql, Dictionary<string, object> args = null, int retry = 2)
        {
            if (!IsConfigured) throw new Exception(_configError);

            var url = _baseUrl + "/v2/pipeline";

            var orderedArgs = new List<object>();
            if (args != null && args.Count > 0)
            {
                var regex = new Regex(@"@(\w+)");
                sql = regex.Replace(sql, match =>
                {
                    var name = match.Groups[1].Value;
                    orderedArgs.Add(args.ContainsKey(name) ? args[name] : null);
                    return "?";
                });
            }

            var stmt = new Dictionary<string, object> { ["sql"] = sql };
            if (orderedArgs.Count > 0)
            {
                var argList = new List<object>();
                foreach (var v in orderedArgs)
                {
                    string type = "text";
                    string value = "";
                    if (v == null) { type = "null"; }
                    else if (v is int || v is long || v is short) { type = "integer"; value = v.ToString(); }
                    else if (v is bool) { type = "integer"; value = (bool)v ? "1" : "0"; }
                    else { value = v.ToString(); }
                    argList.Add(new Dictionary<string, object> { ["type"] = type, ["value"] = value });
                }
                stmt["args"] = argList;
            }

            var body = new { requests = new object[] { new { type = "execute", stmt = stmt }, new { type = "close" } } };
            var jsonBody = JsonSerializer.Serialize(body);

            for (int i = 0; i < retry; i++)
            {
                try
                {
                    var req = new HttpRequestMessage(HttpMethod.Post, url);
                    req.Headers.Add("Authorization", "Bearer " + _token);
                    req.Headers.Add("Connection", "close");
                    req.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                    var resp = await _http.SendAsync(req);
                    var json = await resp.Content.ReadAsStringAsync();

                    if (!resp.IsSuccessStatusCode)
                    {
                        _logger.LogError("[HxdaDb] HTTP " + (int)resp.StatusCode + ": " + json);
                        throw new Exception("Turso HTTP " + (int)resp.StatusCode + ": " + json);
                    }

                    return JsonDocument.Parse(json).RootElement;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("[HxdaDb] ExecuteAsync 尝试 " + (i + 1) + " 失败: " + ex.Message);
                    if (i == retry - 1) throw;
                    await Task.Delay(800 * (i + 1));
                }
            }
            return null;
        }

        private async Task<List<Dictionary<string, object>>> QueryAsync(string sql, Dictionary<string, object> args = null)
        {
            var list = new List<Dictionary<string, object>>();
            try
            {
                var result = await ExecuteAsync(sql, args);
                if (result == null) return list;

                var root = result.Value;
                if (!root.TryGetProperty("results", out var results)) return list;
                if (results.GetArrayLength() == 0) return list;

                var first = results[0];
                if (!first.TryGetProperty("response", out var response)) return list;
                if (!response.TryGetProperty("result", out var res)) return list;
                if (!res.TryGetProperty("cols", out var cols)) return list;
                if (!res.TryGetProperty("rows", out var rows)) return list;

                var colNames = new List<string>();
                foreach (var c in cols.EnumerateArray())
                {
                    colNames.Add(c.GetProperty("name").GetString() ?? "");
                }

                foreach (var row in rows.EnumerateArray())
                {
                    var dict = new Dictionary<string, object>();
                    for (int i = 0; i < colNames.Count; i++)
                    {
                        var cell = row[i];
                        string val = null;
                        if (cell.ValueKind == JsonValueKind.Object && cell.TryGetProperty("value", out var v))
                        {
                            val = v.ValueKind == JsonValueKind.Null ? null : v.ToString();
                        }
                        else if (cell.ValueKind != JsonValueKind.Null)
                        {
                            val = cell.ToString();
                        }
                        dict[colNames[i]] = val;
                    }
                    list.Add(dict);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("[HxdaDb] QueryAsync 失败: " + ex.Message + "\nSQL: " + sql);
            }
            return list;
        }

        /* ============================================================
           玩家
           ============================================================ */
        public async Task UpsertPlayer(HxdaPlayer p)
        {
            if (!IsConfigured || p == null || string.IsNullOrEmpty(p.Id)) return;
            try
            {
                var sql = @"
                    INSERT INTO hxda_players
                        (id, visitor_name, real_name, signature_image, signature_date,
                         admin_logged_in, online_override, attention, refresh_count,
                         tab_switches, search_count, node_read, pm_messages, endings_seen,
                         current_page, gate_passed, gate_signature_name, is_online,
                         last_seen_at, created_at)
                    VALUES
                        (@id, @visitor_name, @real_name, @signature_image, @signature_date,
                         @admin_logged_in, @online_override, @attention, @refresh_count,
                         @tab_switches, @search_count, @node_read, @pm_messages, @endings_seen,
                         @current_page, @gate_passed, @gate_signature_name, @is_online,
                         @last_seen_at, datetime('now'))
                    ON CONFLICT(id) DO UPDATE SET
                        visitor_name = @visitor_name,
                        real_name = @real_name,
                        signature_image = @signature_image,
                        signature_date = @signature_date,
                        admin_logged_in = @admin_logged_in,
                        online_override = @online_override,
                        attention = @attention,
                        refresh_count = @refresh_count,
                        tab_switches = @tab_switches,
                        search_count = @search_count,
                        node_read = @node_read,
                        pm_messages = @pm_messages,
                        endings_seen = @endings_seen,
                        current_page = @current_page,
                        gate_passed = @gate_passed,
                        gate_signature_name = @gate_signature_name,
                        is_online = @is_online,
                        last_seen_at = @last_seen_at
                ";

                await ExecuteAsync(sql, new Dictionary<string, object>
                {
                    ["id"] = p.Id,
                    ["visitor_name"] = p.VisitorName ?? "",
                    ["real_name"] = p.RealName ?? "",
                    ["signature_image"] = p.SignatureImage ?? "",
                    ["signature_date"] = p.SignatureDate ?? "",
                    ["admin_logged_in"] = p.AdminLoggedIn ? 1 : 0,
                    ["online_override"] = p.OnlineOverride ?? "",
                    ["attention"] = p.Attention,
                    ["refresh_count"] = p.RefreshCount,
                    ["tab_switches"] = p.TabSwitches,
                    ["search_count"] = p.SearchCount,
                    ["node_read"] = p.NodeRead ?? "[]",
                    ["pm_messages"] = p.PmMessages ?? "[]",
                    ["endings_seen"] = p.EndingsSeen ?? "[]",
                    ["current_page"] = p.CurrentPage ?? "",
                    ["gate_passed"] = p.GatePassed ? 1 : 0,
                    ["gate_signature_name"] = p.GateSignatureName ?? "",
                    ["is_online"] = p.IsOnline ? 1 : 0,
                    ["last_seen_at"] = DateTime.UtcNow.ToString("o")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("[HxdaDb] UpsertPlayer 失败: " + ex.Message);
            }
        }

        public async Task<List<HxdaPlayer>> GetAllPlayers()
        {
            var rows = await QueryAsync("SELECT * FROM hxda_players ORDER BY last_seen_at DESC");
            var list = new List<HxdaPlayer>();
            foreach (var row in rows)
            {
                try { list.Add(MapPlayer(row)); } catch { }
            }
            return list;
        }

        public async Task<HxdaPlayer> GetPlayer(string id)
        {
            var rows = await QueryAsync("SELECT * FROM hxda_players WHERE id = @id",
                new Dictionary<string, object> { ["id"] = id ?? "" });
            if (rows.Count == 0) return null;
            try { return MapPlayer(rows[0]); } catch { return null; }
        }

        public async Task<List<HxdaPlayer>> GetOnlinePlayers()
        {
            var rows = await QueryAsync("SELECT * FROM hxda_players ORDER BY last_seen_at DESC LIMIT 100");
            var list = new List<HxdaPlayer>();
            foreach (var row in rows)
            {
                try
                {
                    var p = MapPlayer(row);
                    if (p.IsOnlineNow) list.Add(p);
                }
                catch { }
            }
            return list;
        }

        public async Task DeletePlayer(string id)
        {
            await ExecuteAsync("DELETE FROM hxda_players WHERE id = @id", new Dictionary<string, object> { ["id"] = id ?? "" });
            await ExecuteAsync("DELETE FROM hxda_events WHERE player_id = @id", new Dictionary<string, object> { ["id"] = id ?? "" });
            await ExecuteAsync("DELETE FROM hxda_interventions WHERE player_id = @id", new Dictionary<string, object> { ["id"] = id ?? "" });
        }

        public async Task ClearAllPlayers()
        {
            await ExecuteAsync("DELETE FROM hxda_players");
            await ExecuteAsync("DELETE FROM hxda_events");
            await ExecuteAsync("DELETE FROM hxda_interventions");
        }

        private HxdaPlayer MapPlayer(Dictionary<string, object> row)
        {
            string Get(string k) => row.ContainsKey(k) && row[k] != null ? row[k].ToString() : "";
            int GetInt(string k) { int v; return row.ContainsKey(k) && row[k] != null && int.TryParse(row[k].ToString(), out v) ? v : 0; }
            bool GetBool(string k) { return row.ContainsKey(k) && row[k] != null && row[k].ToString() == "1"; }

            return new HxdaPlayer
            {
                Id = Get("id"),
                VisitorName = Get("visitor_name"),
                RealName = Get("real_name"),
                SignatureImage = Get("signature_image"),
                SignatureDate = Get("signature_date"),
                AdminLoggedIn = GetBool("admin_logged_in"),
                OnlineOverride = Get("online_override"),
                Attention = GetInt("attention"),
                RefreshCount = GetInt("refresh_count"),
                TabSwitches = GetInt("tab_switches"),
                SearchCount = GetInt("search_count"),
                NodeRead = Get("node_read"),
                PmMessages = Get("pm_messages"),
                EndingsSeen = Get("endings_seen"),
                CurrentPage = Get("current_page"),
                GatePassed = GetBool("gate_passed"),
                GateSignatureName = Get("gate_signature_name"),
                IsOnline = GetBool("is_online"),
                LastSeenAt = Get("last_seen_at"),
                CreatedAt = Get("created_at")
            };
        }

        /* ============================================================
           事件
           ============================================================ */
        public async Task LogEvent(string playerId, string type, string data)
        {
            if (!IsConfigured || string.IsNullOrEmpty(playerId)) return;
            try
            {
                await ExecuteAsync(
                    "INSERT INTO hxda_events (player_id, event_type, event_data) VALUES (@player_id, @type, @data)",
                    new Dictionary<string, object> { ["player_id"] = playerId, ["type"] = type ?? "", ["data"] = data ?? "" });
            }
            catch (Exception ex) { _logger.LogError("[HxdaDb] LogEvent 失败: " + ex.Message); }
        }

        public async Task<List<HxdaEvent>> GetEvents(string playerId, int limit = 200)
        {
            var rows = await QueryAsync(
                "SELECT * FROM hxda_events WHERE player_id = @id ORDER BY created_at DESC LIMIT " + limit,
                new Dictionary<string, object> { ["id"] = playerId ?? "" });
            var list = new List<HxdaEvent>();
            foreach (var r in rows)
            {
                try
                {
                    list.Add(new HxdaEvent
                    {
                        Id = int.TryParse(r.ContainsKey("id") ? r["id"]?.ToString() : "0", out var i) ? i : 0,
                        PlayerId = r.ContainsKey("player_id") ? r["player_id"]?.ToString() ?? "" : "",
                        EventType = r.ContainsKey("event_type") ? r["event_type"]?.ToString() ?? "" : "",
                        EventData = r.ContainsKey("event_data") ? r["event_data"]?.ToString() ?? "" : "",
                        CreatedAt = r.ContainsKey("created_at") ? r["created_at"]?.ToString() ?? "" : ""
                    });
                }
                catch { }
            }
            return list;
        }

        public async Task<List<HxdaEvent>> GetRecentEvents(int limit = 500)
        {
            var rows = await QueryAsync("SELECT * FROM hxda_events ORDER BY created_at DESC LIMIT " + limit);
            var list = new List<HxdaEvent>();
            foreach (var r in rows)
            {
                try
                {
                    list.Add(new HxdaEvent
                    {
                        Id = int.TryParse(r.ContainsKey("id") ? r["id"]?.ToString() : "0", out var i) ? i : 0,
                        PlayerId = r.ContainsKey("player_id") ? r["player_id"]?.ToString() ?? "" : "",
                        EventType = r.ContainsKey("event_type") ? r["event_type"]?.ToString() ?? "" : "",
                        EventData = r.ContainsKey("event_data") ? r["event_data"]?.ToString() ?? "" : "",
                        CreatedAt = r.ContainsKey("created_at") ? r["created_at"]?.ToString() ?? "" : ""
                    });
                }
                catch { }
            }
            return list;
        }

        public async Task<List<HxdaEvent>> GetPlayerTimeline(string playerId)
        {
            var rows = await QueryAsync(
                "SELECT * FROM hxda_events WHERE player_id = @id ORDER BY created_at ASC",
                new Dictionary<string, object> { ["id"] = playerId ?? "" });
            var list = new List<HxdaEvent>();
            foreach (var r in rows)
            {
                try
                {
                    list.Add(new HxdaEvent
                    {
                        Id = int.TryParse(r.ContainsKey("id") ? r["id"]?.ToString() : "0", out var i) ? i : 0,
                        PlayerId = r.ContainsKey("player_id") ? r["player_id"]?.ToString() ?? "" : "",
                        EventType = r.ContainsKey("event_type") ? r["event_type"]?.ToString() ?? "" : "",
                        EventData = r.ContainsKey("event_data") ? r["event_data"]?.ToString() ?? "" : "",
                        CreatedAt = r.ContainsKey("created_at") ? r["created_at"]?.ToString() ?? "" : ""
                    });
                }
                catch { }
            }
            return list;
        }

        /* ============================================================
           干预
           ============================================================ */
        public async Task AddIntervention(string playerId, string type, string payload)
        {
            if (!IsConfigured || string.IsNullOrEmpty(playerId)) return;
            try
            {
                await ExecuteAsync(
                    "INSERT INTO hxda_interventions (player_id, type, payload, active, consumed) VALUES (@player_id, @type, @payload, 1, 0)",
                    new Dictionary<string, object> { ["player_id"] = playerId, ["type"] = type ?? "", ["payload"] = payload ?? "" });
            }
            catch (Exception ex) { _logger.LogError("[HxdaDb] AddIntervention 失败: " + ex.Message); }
        }

        public async Task<List<HxdaIntervention>> GetPendingInterventions(string playerId)
        {
            var rows = await QueryAsync(
                "SELECT * FROM hxda_interventions WHERE player_id = @id AND active = 1 AND consumed = 0 ORDER BY created_at ASC",
                new Dictionary<string, object> { ["id"] = playerId ?? "" });
            var list = new List<HxdaIntervention>();
            foreach (var r in rows)
            {
                try
                {
                    list.Add(new HxdaIntervention
                    {
                        Id = int.TryParse(r.ContainsKey("id") ? r["id"]?.ToString() : "0", out var i) ? i : 0,
                        PlayerId = r.ContainsKey("player_id") ? r["player_id"]?.ToString() ?? "" : "",
                        Type = r.ContainsKey("type") ? r["type"]?.ToString() ?? "" : "",
                        Payload = r.ContainsKey("payload") ? r["payload"]?.ToString() ?? "" : "",
                        Active = r.ContainsKey("active") && r["active"]?.ToString() == "1",
                        Consumed = r.ContainsKey("consumed") && r["consumed"]?.ToString() == "1",
                        CreatedAt = r.ContainsKey("created_at") ? r["created_at"]?.ToString() ?? "" : ""
                    });
                }
                catch { }
            }
            return list;
        }

        public async Task MarkInterventionsConsumed(List<int> ids)
        {
            if (ids == null || ids.Count == 0) return;
            try
            {
                var idList = string.Join(",", ids);
                await ExecuteAsync("UPDATE hxda_interventions SET consumed = 1 WHERE id IN (" + idList + ")");
            }
            catch (Exception ex) { _logger.LogError("[HxdaDb] MarkInterventionsConsumed 失败: " + ex.Message); }
        }

        public async Task DeleteIntervention(int id)
        {
            try
            {
                await ExecuteAsync("DELETE FROM hxda_interventions WHERE id = @id",
                    new Dictionary<string, object> { ["id"] = id.ToString() });
            }
            catch { }
        }

        /* ============================================================
           搜索记录
           ============================================================ */
        public async Task<List<HxdaSearchRecord>> GetAllSearchLogs()
        {
            var rows = await QueryAsync(@"
                SELECT p.id as player_id, p.visitor_name, p.real_name, p.current_page,
                       json_extract(e.event_data, '$.word') as word,
                       json_extract(e.event_data, '$.time') as time,
                       e.created_at
                FROM hxda_events e
                LEFT JOIN hxda_players p ON p.id = e.player_id
                WHERE e.event_type = 'search'
                ORDER BY e.created_at DESC
                LIMIT 500");
            var list = new List<HxdaSearchRecord>();
            foreach (var r in rows)
            {
                try
                {
                    list.Add(new HxdaSearchRecord
                    {
                        PlayerId = r.ContainsKey("player_id") ? r["player_id"]?.ToString() ?? "" : "",
                        VisitorName = r.ContainsKey("visitor_name") ? r["visitor_name"]?.ToString() ?? "" : "",
                        RealName = r.ContainsKey("real_name") ? r["real_name"]?.ToString() ?? "" : "",
                        CurrentPage = r.ContainsKey("current_page") ? r["current_page"]?.ToString() ?? "" : "",
                        Word = r.ContainsKey("word") ? r["word"]?.ToString() ?? "" : "",
                        Time = r.ContainsKey("time") ? r["time"]?.ToString() ?? "" : "",
                        CreatedAt = r.ContainsKey("created_at") ? r["created_at"]?.ToString() ?? "" : ""
                    });
                }
                catch { }
            }
            return list;
        }

        public async Task<Dictionary<string, int>> GetSearchWordStats()
        {
            var rows = await QueryAsync(@"
                SELECT json_extract(event_data, '$.word') as word, COUNT(*) as cnt
                FROM hxda_events
                WHERE event_type = 'search'
                GROUP BY word
                ORDER BY cnt DESC
                LIMIT 50");
            var stats = new Dictionary<string, int>();
            foreach (var r in rows)
            {
                try
                {
                    var w = r.ContainsKey("word") ? r["word"]?.ToString() : null;
                    if (string.IsNullOrEmpty(w)) continue;
                    stats[w] = int.TryParse(r.ContainsKey("cnt") ? r["cnt"]?.ToString() : "0", out var c) ? c : 0;
                }
                catch { }
            }
            return stats;
        }

        public async Task<List<HxdaPmRecord>> GetAllPmLogs()
        {
            var players = await GetAllPlayers();
            var list = new List<HxdaPmRecord>();
            foreach (var p in players)
            {
                foreach (var m in p.PmMessagesList)
                {
                    list.Add(new HxdaPmRecord
                    {
                        PlayerId = p.Id ?? "",
                        VisitorName = p.VisitorName ?? "",
                        RealName = p.RealName ?? "",
                        Role = m.Role ?? "",
                        Content = m.Content ?? "",
                        Time = m.Time ?? ""
                    });
                }
            }
            return list.OrderByDescending(x => x.Time).ToList();
        }

        /* ============================================================
           清理
           ============================================================ */
        public async Task CleanupOldPlayers(int days)
        {
            await ExecuteAsync("DELETE FROM hxda_players WHERE last_seen_at < datetime('now', '-' || @days || ' days')",
                new Dictionary<string, object> { ["days"] = days.ToString() });
        }
        public async Task CleanupOldEvents(int days)
        {
            await ExecuteAsync("DELETE FROM hxda_events WHERE created_at < datetime('now', '-' || @days || ' days')",
                new Dictionary<string, object> { ["days"] = days.ToString() });
        }
        public async Task CleanupConsumedInterventions()
        {
            await ExecuteAsync("DELETE FROM hxda_interventions WHERE consumed = 1");
        }
        public async Task CleanupOfflinePlayers()
        {
            await ExecuteAsync("DELETE FROM hxda_players WHERE last_seen_at < datetime('now', '-1 hour')");
        }
    }
}