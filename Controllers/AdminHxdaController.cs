using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Services;
using MyPersonalWebsite.Hubs;

namespace MyPersonalWebsite.Controllers
{
    public class AdminHxdaController : Controller
    {
        private readonly HxdaDb _db;
        private readonly IHubContext<HxdaHub> _hub;

        public AdminHxdaController(HxdaDb db, IHubContext<HxdaHub> hub)
        {
            _db = db;
            _hub = hub;
        }

        private bool IsAdmin()
        {
            return (HttpContext.Session.GetInt32("IsAdmin") ?? 0) == 1;
        }

        /* ============ 玩家列表 ============ */
        public async Task<IActionResult> Players()
        {
            if (!IsAdmin()) return Redirect("/Auth/Login");
            var players = await _db.GetAllPlayers();
            return View("~/Views/Admin/HxdaPlayers.cshtml", players);
        }

        /* ============ 玩家详情 ============ */
        public async Task<IActionResult> PlayerDetail(string id)
        {
            if (!IsAdmin()) return Redirect("/Auth/Login");
            var player = await _db.GetPlayer(id);
            if (player == null) return NotFound();
            ViewBag.Events = await _db.GetEvents(id);
            return View("~/Views/Admin/HxdaPlayerDetail.cshtml", player);
        }

        /* ============ 实时在线 ============ */
        public async Task<IActionResult> Live()
        {
            if (!IsAdmin()) return Redirect("/Auth/Login");
            var players = await _db.GetOnlinePlayers();
            return View("~/Views/Admin/HxdaLive.cshtml", players);
        }

        /* ============ 干预 ============ */
        public async Task<IActionResult> Control(string id)
        {
            if (!IsAdmin()) return Redirect("/Auth/Login");
            var player = await _db.GetPlayer(id);
            if (player == null) return NotFound();
            return View("~/Views/Admin/HxdaControl.cshtml", player);
        }

        [HttpPost]
        public async Task<IActionResult> SendCommand(string playerId, string type, string payload)
        {
            if (!IsAdmin()) return Unauthorized();
            await _db.AddIntervention(playerId, type, payload ?? "");
            await _db.LogEvent(playerId, "intervention", $"{type}:{payload}");

            await _hub.Clients.Group("player_" + playerId)
                .SendAsync("Command", new { type, payload });

            return Ok(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetInterventions(string playerId)
        {
            if (!IsAdmin()) return Unauthorized();
            var items = await _db.GetInterventionsByPlayer(playerId);
            return Ok(new
            {
                success = true,
                items = items.Select(i => new
                {
                    i.Id, i.Type, i.Payload, i.CreatedAt, i.Consumed
                })
            });
        }

        [HttpPost]
        public async Task<IActionResult> SendCommandAll(string type, string payload)
        {
            if (!IsAdmin()) return Unauthorized();
            var online = await _db.GetOnlinePlayers();
            foreach (var p in online)
            {
                await _db.AddIntervention(p.Id, type, payload ?? "");
                await _db.LogEvent(p.Id, "intervention", $"{type}:{payload}");

                await _hub.Clients.Group("player_" + p.Id)
                    .SendAsync("Command", new { type, payload });
            }
            return Ok(new { success = true, count = online.Count });
        }

        /* ============ 结局统计 ============ */
        public async Task<IActionResult> Endings()
        {
            if (!IsAdmin()) return Redirect("/Auth/Login");
            var players = await _db.GetAllPlayers();
            var stats = new Dictionary<string, int>();
            foreach (var p in players)
                foreach (var e in p.EndingsSeenList)
                {
                    if (!stats.ContainsKey(e)) stats[e] = 0;
                    stats[e]++;
                }
            ViewBag.Stats = stats;
            ViewBag.Players = players;
            return View("~/Views/Admin/HxdaEndings.cshtml");
        }

        /* ============ 搜索记录 ============ */
        public async Task<IActionResult> SearchLog()
        {
            if (!IsAdmin()) return Redirect("/Auth/Login");
            var logs = await _db.GetAllSearchLogs();
            ViewBag.Stats = await _db.GetSearchWordStats();
            return View("~/Views/Admin/HxdaSearchLog.cshtml", logs);
        }

        /* ============ 私信记录 ============ */
        public async Task<IActionResult> PmLog()
        {
            if (!IsAdmin()) return Redirect("/Auth/Login");
            var logs = await _db.GetAllPmLogs();
            return View("~/Views/Admin/HxdaPmLog.cshtml", logs);
        }

        /* ============ 时间线 ============ */
        public async Task<IActionResult> Timeline()
        {
            if (!IsAdmin()) return Redirect("/Auth/Login");
            var events = await _db.GetRecentEvents(500);
            ViewBag.Players = await _db.GetAllPlayers();
            return View("~/Views/Admin/HxdaTimeline.cshtml", events);
        }

        /* ============ 导出 ============ */
        public async Task<IActionResult> Export()
        {
            if (!IsAdmin()) return Redirect("/Auth/Login");
            return View("~/Views/Admin/HxdaExport.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> ExportCsv()
        {
            if (!IsAdmin()) return Unauthorized();
            var players = await _db.GetAllPlayers();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("id,visitor_name,real_name,attention,node_read,endings_seen,current_page,last_seen_at,created_at");
            foreach (var p in players)
            {
                sb.AppendLine($"\"{p.Id}\",\"{p.VisitorName}\",\"{p.RealName}\",{p.Attention},\"{p.NodeRead}\",\"{p.EndingsSeen}\",\"{p.CurrentPage}\",\"{p.LastSeenAt}\",\"{p.CreatedAt}\"");
            }
            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"hxda_players_{DateTime.Now:yyyyMMdd}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> ExportJson()
        {
            if (!IsAdmin()) return Unauthorized();
            var players = await _db.GetAllPlayers();
            var json = System.Text.Json.JsonSerializer.Serialize(players, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            return File(bytes, "application/json", $"hxda_players_{DateTime.Now:yyyyMMdd}.json");
        }

        [HttpGet]
        public async Task<IActionResult> ExportFullJson()
        {
            if (!IsAdmin()) return Unauthorized();
            var players = await _db.GetAllPlayers();
            var events = await _db.GetRecentEvents(10000);
            var data = new { players, events, exportedAt = DateTime.UtcNow };
            var json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            return File(bytes, "application/json", $"hxda_full_{DateTime.Now:yyyyMMdd}.json");
        }

        /* ============ 清理 ============ */
        public async Task<IActionResult> Cleanup()
        {
            if (!IsAdmin()) return Redirect("/Auth/Login");
            return View("~/Views/Admin/HxdaCleanup.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> DoCleanup(string action, int days = 7)
        {
            if (!IsAdmin()) return Unauthorized();
            switch (action)
            {
                case "old_players": await _db.CleanupOldPlayers(days); break;
                case "old_events": await _db.CleanupOldEvents(days); break;
                case "consumed_interventions": await _db.CleanupConsumedInterventions(); break;
                case "offline_players": await _db.CleanupOfflinePlayers(); break;
                case "all": await _db.ClearAllPlayers(); break;
            }
            return Ok(new { success = true });
        }

        /* ============ 回放 ============ */
        public async Task<IActionResult> Replay(string id)
        {
            if (!IsAdmin()) return Redirect("/Auth/Login");
            var player = await _db.GetPlayer(id);
            if (player == null) return NotFound();
            ViewBag.Events = await _db.GetPlayerTimeline(id);
            return View("~/Views/Admin/HxdaReplay.cshtml", player);
        }

        /* ============ 删除玩家 ============ */
        [HttpPost]
        public async Task<IActionResult> DeletePlayer(string id)
        {
            if (!IsAdmin()) return Unauthorized();
            await _db.DeletePlayer(id);
            return Ok(new { success = true });
        }

        /* ============ 清空所有 ============ */
        [HttpPost]
        public async Task<IActionResult> ClearAll()
        {
            if (!IsAdmin()) return Unauthorized();
            await _db.ClearAllPlayers();
            return Ok(new { success = true });
        }

        /* ============ 删除干预 ============ */
        [HttpPost]
        public async Task<IActionResult> DeleteIntervention(int id)
        {
            if (!IsAdmin()) return Unauthorized();
            await _db.DeleteIntervention(id);
            return Ok(new { success = true });
        }
    }
}