using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MyPersonalWebsite.Models;

namespace MyPersonalWebsite.Services
{
    public class HxdaDb
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly string _token;

        public HxdaDb(IConfiguration config, IHttpClientFactory httpFactory)
        {
            _http = httpFactory.CreateClient();
            var url = config["Turso:Url"] ?? "";
            _token = config["Turso:Token"] ?? "";

            if (string.IsNullOrEmpty(url))
                throw new Exception("Turso:Url 未配置，请在 appsettings.json 里添加 Turso 节");

            if (string.IsNullOrEmpty(_token))
                throw new Exception("Turso:Token 未配置，请在 appsettings.json 里添加 Turso 节");

            _baseUrl = url.Replace("libsql://", "https://").TrimEnd('/');

            Console.WriteLine("[HxdaDb] BaseUrl = " + _baseUrl);
        }

        /* ============================================================
           通用执行
           ============================================================ */
        private async Task<JsonElement> ExecuteAsync(string sql, Dictionary<string, object> args = null, int retry = 3)
        {
            var url = _baseUrl + "/v2/pipeline";

            /* 把 @name 替换成 ?，并按出现顺序重排 args */
            var orderedArgs = new List<object>();

            if (args != null && args.Count > 0)
            {
                var regex = new Regex(@"@(\w+)");
                sql = regex.Replace(sql, match =>
                {
                    var name = match.Groups[1].Value;
                    if (args.ContainsKey(name))
                        orderedArgs.Add(args[name]);
                    else
                        orderedArgs.Add(null);
                    return "?";
                });
            }

            /* 构造 stmt */
            var stmt = new Dictionary<string, object>
            {
                ["sql"] = sql
            };

            if (orderedArgs.Count > 0)
            {
                var argList = new List<object>();
                foreach (var v in orderedArgs)
                {
                    string type = "text";
                    string value = "";

                    if (v == null)
                    {
                        type = "null";
                    }
                    else if (v is int || v is long || v is short)
                    {
                        type = "integer";
                        value = v.ToString();
                    }
                    else if (v is double || v is float || v is decimal)
                    {
                        type = "float";
                        value = v.ToString();
                    }
                    else if (v is bool)
                    {
                        type = "integer";
                        value = (bool)v ? "1" : "0";
                    }
                    else
                    {
                        value = v.ToString();
                    }

                    argList.Add(new Dictionary<string, object>
                    {
                        ["type"] = type,
                        ["value"] = value
                    });
                }
                stmt["args"] = argList;
            }

            /* 构造请求体 */
            var body = new
            {
                requests = new object[]
                {
                    new { type = "execute", stmt = stmt },
                    new { type = "close" }
                }
            };

            var jsonBody = JsonSerializer.Serialize(body);

            /* 发送请求（带重试） */
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
                        throw new Exception("Turso error (" + (int)resp.StatusCode + "): " + json);
                    }

                    return JsonDocument.Parse(json).RootElement;
                }
                catch (HttpRequestException) when (i < retry - 1)
                {
                    await Task.Delay(1000 * (i + 1));
                }
            }

            throw new Exception("Turso 请求失败，已重试 " + retry + " 次");
        }

        private async Task<List<Dictionary<string, object>>> QueryAsync(string sql, Dictionary<string, object> args = null)
        {
            var result = await ExecuteAsync(sql, args);

            var list = new List<Dictionary<string, object>>();

            try
            {
                var results = result.GetProperty("results");
                if (results.GetArrayLength() == 0) return list;

                var first = results[0];
                if (!first.TryGetProperty("response", out var response)) return list;
                if (!response.TryGetProperty("result", out var res)) return list;
                if (!res.TryGetProperty("cols", out var cols)) return list;
                if (!res.TryGetProperty("rows", out var rows)) return list;

                var colNames = new List<string>();
                foreach (var c in cols.EnumerateArray())
                {
                    colNames.Add(c.GetProperty("name").GetString());
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
            catch { }

            return list;
        }

        /* ============================================================
           玩家
           ============================================================ */
        public async Task UpsertPlayer(HxdaPlayer p)
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
                     @last_seen_at, COALESCE((SELECT created_at FROM hxda_players WHERE id = @id), datetime('now')))
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
                ["admin_logged_in"] = p.AdminLoggedIn ? "1" : "0",
                ["online_override"] = p.OnlineOverride ?? "",
                ["attention"] = p.Attention.ToString(),
                ["refresh_count"] = p.RefreshCount.ToString(),
                ["tab_switches"] = p.TabSwitches.ToString(),
                ["search_count"] = p.SearchCount.ToString(),
                ["node_read"] = p.NodeRead ?? "[]",
                ["pm_messages"] = p.PmMessages ?? "[]",
                ["endings_seen"] = p.EndingsSeen ?? "[]",
                ["current_page"] = p.CurrentPage ?? "",
                ["gate_passed"] = p.GatePassed ? "1" : "0",
                ["gate_signature_name"] = p.GateSignatureName ?? "",
                ["is_online"] = p.IsOnline ? "1" : "0",
                ["last_seen_at"] = DateTime.UtcNow.ToString("o")
            });
        }

        public async Task<List<HxdaPlayer>> GetAllPlayers()
        {
            var rows = await QueryAsync("SELECT * FROM hxda_players ORDER BY last_seen_at DESC");
            return rows.Select(MapPlayer).ToList();
        }

        public async Task<HxdaPlayer> GetPlayer(string id)
        {
            var rows = await QueryAsync(
                "SELECT * FROM hxda_players WHERE id = @id",
                new Dictionary<string, object> { ["id"] = id });
            if (rows.Count == 0) return null;
            return MapPlayer(rows[0]);
        }

        public async Task<List<HxdaPlayer>> GetOnlinePlayers()
        {
            var rows = await QueryAsync(
                "SELECT * FROM hxda_players WHERE last_seen_at >= datetime('now', '-60 seconds') ORDER BY last_seen_at DESC");
            return rows.Select(MapPlayer).ToList();
        }

        public async Task DeletePlayer(string id)
        {
            await ExecuteAsync("DELETE FROM hxda_players WHERE id = @id",
                new Dictionary<string, object> { ["id"] = id });
            await ExecuteAsync("DELETE FROM hxda_events WHERE player_id = @id",
                new Dictionary<string, object> { ["id"] = id });
            await ExecuteAsync("DELETE FROM hxda_interventions WHERE player_id = @id",
                new Dictionary<string, object> { ["id"] = id });
        }

        public async Task ClearAllPlayers()
        {
            await ExecuteAsync("DELETE FROM hxda_players");
            await ExecuteAsync("DELETE FROM hxda_events");
            await ExecuteAsync("DELETE FROM hxda_interventions");
        }

        private HxdaPlayer MapPlayer(Dictionary<string, object> row)
        {
            return new HxdaPlayer
            {
                Id = row.ContainsKey("id") ? row["id"]?.ToString() : null,
                VisitorName = row.ContainsKey("visitor_name") ? row["visitor_name"]?.ToString() : null,
                RealName = row.ContainsKey("real_name") ? row["real_name"]?.ToString() : null,
                SignatureImage = row.ContainsKey("signature_image") ? row["signature_image"]?.ToString() : null,
                SignatureDate = row.ContainsKey("signature_date") ? row["signature_date"]?.ToString() : null,
                AdminLoggedIn = row.ContainsKey("admin_logged_in") && row["admin_logged_in"]?.ToString() == "1",
                OnlineOverride = row.ContainsKey("online_override") ? row["online_override"]?.ToString() : null,
                Attention = row.ContainsKey("attention") && int.TryParse(row["attention"]?.ToString(), out var a) ? a : 0,
                RefreshCount = row.ContainsKey("refresh_count") && int.TryParse(row["refresh_count"]?.ToString(), out var r) ? r : 0,
                TabSwitches = row.ContainsKey("tab_switches") && int.TryParse(row["tab_switches"]?.ToString(), out var t) ? t : 0,
                SearchCount = row.ContainsKey("search_count") && int.TryParse(row["search_count"]?.ToString(), out var s) ? s : 0,
                NodeRead = row.ContainsKey("node_read") ? row["node_read"]?.ToString() : null,
                PmMessages = row.ContainsKey("pm_messages") ? row["pm_messages"]?.ToString() : null,
                EndingsSeen = row.ContainsKey("endings_seen") ? row["endings_seen"]?.ToString() : null,
                CurrentPage = row.ContainsKey("current_page") ? row["current_page"]?.ToString() : null,
                GatePassed = row.ContainsKey("gate_passed") && row["gate_passed"]?.ToString() == "1",
                GateSignatureName = row.ContainsKey("gate_signature_name") ? row["gate_signature_name"]?.ToString() : null,
                IsOnline = row.ContainsKey("is_online") && row["is_online"]?.ToString() == "1",
                LastSeenAt = row.ContainsKey("last_seen_at") ? row["last_seen_at"]?.ToString() : null,
                CreatedAt = row.ContainsKey("created_at") ? row["created_at"]?.ToString() : null
            };
        }

        /* ============================================================
           事件日志
           ============================================================ */
        public async Task LogEvent(string playerId, string type, string data)
        {
            await ExecuteAsync(
                "INSERT INTO hxda_events (player_id, event_type, event_data) VALUES (@player_id, @type, @data)",
                new Dictionary<string, object>
                {
                    ["player_id"] = playerId,
                    ["type"] = type,
                    ["data"] = data ?? ""
                });
        }

        public async Task<List<HxdaEvent>> GetEvents(string playerId, int limit = 200)
        {
            var rows = await QueryAsync(
                "SELECT * FROM hxda_events WHERE player_id = @id ORDER BY created_at DESC LIMIT " + limit,
                new Dictionary<string, object> { ["id"] = playerId });
            return rows.Select(MapEvent).ToList();
        }

        public async Task<List<HxdaEvent>> GetRecentEvents(int limit = 500)
        {
            var rows = await QueryAsync(
                "SELECT e.*, p.visitor_name, p.real_name FROM hxda_events e LEFT JOIN hxda_players p ON p.id = e.player_id ORDER BY e.created_at DESC LIMIT " + limit);
            return rows.Select(MapEvent).ToList();
        }

        public async Task<List<HxdaEvent>> GetPlayerTimeline(string playerId)
        {
            var rows = await QueryAsync(
                "SELECT * FROM hxda_events WHERE player_id = @id ORDER BY created_at ASC",
                new Dictionary<string, object> { ["id"] = playerId });
            return rows.Select(MapEvent).ToList();
        }

        private HxdaEvent MapEvent(Dictionary<string, object> row)
        {
            return new HxdaEvent
            {
                Id = row.ContainsKey("id") && int.TryParse(row["id"]?.ToString(), out var i) ? i : 0,
                PlayerId = row.ContainsKey("player_id") ? row["player_id"]?.ToString() : null,
                EventType = row.ContainsKey("event_type") ? row["event_type"]?.ToString() : null,
                EventData = row.ContainsKey("event_data") ? row["event_data"]?.ToString() : null,
                CreatedAt = row.ContainsKey("created_at") ? row["created_at"]?.ToString() : null
            };
        }

        /* ============================================================
           干预
           ============================================================ */
        public async Task AddIntervention(string playerId, string type, string payload)
        {
            await ExecuteAsync(
                "INSERT INTO hxda_interventions (player_id, type, payload, active, consumed) VALUES (@player_id, @type, @payload, 1, 0)",
                new Dictionary<string, object>
                {
                    ["player_id"] = playerId,
                    ["type"] = type,
                    ["payload"] = payload ?? ""
                });
        }

        public async Task<List<HxdaIntervention>> GetPendingInterventions(string playerId)
        {
            var rows = await QueryAsync(
                "SELECT * FROM hxda_interventions WHERE player_id = @id AND active = 1 AND consumed = 0 ORDER BY created_at ASC",
                new Dictionary<string, object> { ["id"] = playerId });
            return rows.Select(MapIntervention).ToList();
        }

        public async Task<List<HxdaIntervention>> GetInterventionsByPlayer(string playerId, int limit = 100)
        {
            var rows = await QueryAsync(
                "SELECT * FROM hxda_interventions WHERE player_id = @id ORDER BY created_at DESC LIMIT " + limit,
                new Dictionary<string, object> { ["id"] = playerId });
            return rows.Select(MapIntervention).ToList();
        }

        public async Task MarkInterventionsConsumed(List<int> ids)
        {
            if (ids == null || ids.Count == 0) return;
            var idList = string.Join(",", ids);
            await ExecuteAsync($"UPDATE hxda_interventions SET consumed = 1 WHERE id IN ({idList})");
        }

        public async Task DeleteIntervention(int id)
        {
            await ExecuteAsync("DELETE FROM hxda_interventions WHERE id = @id",
                new Dictionary<string, object> { ["id"] = id.ToString() });
        }

        private HxdaIntervention MapIntervention(Dictionary<string, object> row)
        {
            return new HxdaIntervention
            {
                Id = row.ContainsKey("id") && int.TryParse(row["id"]?.ToString(), out var i) ? i : 0,
                PlayerId = row.ContainsKey("player_id") ? row["player_id"]?.ToString() : null,
                Type = row.ContainsKey("type") ? row["type"]?.ToString() : null,
                Payload = row.ContainsKey("payload") ? row["payload"]?.ToString() : null,
                Active = row.ContainsKey("active") && row["active"]?.ToString() == "1",
                Consumed = row.ContainsKey("consumed") && row["consumed"]?.ToString() == "1",
                CreatedAt = row.ContainsKey("created_at") ? row["created_at"]?.ToString() : null
            };
        }

        /* ============================================================
           搜索记录 / 私信记录
           ============================================================ */
        public async Task<List<HxdaSearchRecord>> GetAllSearchLogs()
        {
            var rows = await QueryAsync(@"
                SELECT p.id as player_id, p.visitor_name, p.real_name, p.current_page, p.last_seen_at,
                       json_extract(e.event_data, '$.word') as word,
                       json_extract(e.event_data, '$.time') as time,
                       e.created_at
                FROM hxda_events e
                LEFT JOIN hxda_players p ON p.id = e.player_id
                WHERE e.event_type = 'search'
                ORDER BY e.created_at DESC
                LIMIT 500
            ");
            return rows.Select(r => new HxdaSearchRecord
            {
                PlayerId = r.ContainsKey("player_id") ? r["player_id"]?.ToString() : null,
                VisitorName = r.ContainsKey("visitor_name") ? r["visitor_name"]?.ToString() : null,
                RealName = r.ContainsKey("real_name") ? r["real_name"]?.ToString() : null,
                CurrentPage = r.ContainsKey("current_page") ? r["current_page"]?.ToString() : null,
                Word = r.ContainsKey("word") ? r["word"]?.ToString() : null,
                Time = r.ContainsKey("time") ? r["time"]?.ToString() : null,
                CreatedAt = r.ContainsKey("created_at") ? r["created_at"]?.ToString() : null
            }).ToList();
        }

        public async Task<Dictionary<string, int>> GetSearchWordStats()
        {
            var rows = await QueryAsync(@"
                SELECT json_extract(event_data, '$.word') as word, COUNT(*) as cnt
                FROM hxda_events
                WHERE event_type = 'search'
                GROUP BY word
                ORDER BY cnt DESC
                LIMIT 50
            ");
            var stats = new Dictionary<string, int>();
            foreach (var row in rows)
            {
                var w = row.ContainsKey("word") ? row["word"]?.ToString() : null;
                if (string.IsNullOrEmpty(w)) continue;
                stats[w] = row.ContainsKey("cnt") && int.TryParse(row["cnt"]?.ToString(), out var c) ? c : 0;
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
                        PlayerId = p.Id,
                        VisitorName = p.VisitorName,
                        RealName = p.RealName,
                        Role = m.Role,
                        Content = m.Content,
                        Time = m.Time
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
            await ExecuteAsync(
                "DELETE FROM hxda_players WHERE last_seen_at < datetime('now', '-' || @days || ' days')",
                new Dictionary<string, object> { ["days"] = days.ToString() });
        }

        public async Task CleanupOldEvents(int days)
        {
            await ExecuteAsync(
                "DELETE FROM hxda_events WHERE created_at < datetime('now', '-' || @days || ' days')",
                new Dictionary<string, object> { ["days"] = days.ToString() });
        }

        public async Task CleanupConsumedInterventions()
        {
            await ExecuteAsync("DELETE FROM hxda_interventions WHERE consumed = 1");
        }

        public async Task CleanupOfflinePlayers()
        {
            await ExecuteAsync(
                "DELETE FROM hxda_players WHERE last_seen_at < datetime('now', '-1 hour')");
        }
    }
}