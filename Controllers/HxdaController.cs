using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Services;

namespace MyPersonalWebsite.Controllers
{
    [Route("api/hxda")]
    [ApiController]
    public class HxdaController : ControllerBase
    {
        private readonly HxdaDb _db;

        public HxdaController(HxdaDb db)
        {
            _db = db;
        }

        [HttpPost("report")]
        public async Task<IActionResult> Report([FromBody] HxdaPlayer data)
        {
            if (string.IsNullOrEmpty(data?.Id))
                return Ok(new { success = false });

            data.LastSeenAt = DateTime.UtcNow.ToString("o");
            await _db.UpsertPlayer(data);
            return Ok(new { success = true });
        }

        [HttpGet("poll")]
        public async Task<IActionResult> Poll([FromQuery] string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
                return Ok(new { commands = new List<object>() });

            var pending = await _db.GetPendingInterventions(playerId);
            var ids = new List<int>();
            var cmds = new List<object>();

            foreach (var i in pending)
            {
                ids.Add(i.Id);
                cmds.Add(new { type = i.Type, payload = i.Payload });
            }

            if (ids.Count > 0)
                await _db.MarkInterventionsConsumed(ids);

            return Ok(new { commands = cmds });
        }

        [HttpPost("event")]
        public async Task<IActionResult> Event([FromBody] HxdaEventPayload data)
        {
            if (string.IsNullOrEmpty(data?.PlayerId)) return Ok(new { success = false });
            await _db.LogEvent(data.PlayerId, data.Type, data.Data ?? "");
            return Ok(new { success = true });
        }
    }

    public class HxdaEventPayload
    {
        public string PlayerId { get; set; }
        public string Type { get; set; }
        public string Data { get; set; }
    }
}