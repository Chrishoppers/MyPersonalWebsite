using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyPersonalWebsite.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace MyPersonalWebsite.Controllers
{
    public class PartyController : Controller
    {
        private readonly IHubContext<PartyHub> _hubContext;

        public PartyController(IHubContext<PartyHub> hubContext)
        {
            _hubContext = hubContext;
        }

        // ============================================================
        // 主控端页面（仅 admin 可访问）
        // ============================================================
        [Authorize(Roles = "Admin")]
        public IActionResult Host()
        {
            var username = User.Identity?.Name ?? "admin";
            ViewBag.Username = username;
            return View();
        }

        // ============================================================
        // 玩家端页面（任何人可访问，包括未登录）
        // ============================================================
        public IActionResult Player(string? roomId)
        {
            ViewBag.RoomId = roomId;
            return View();
        }

        // ============================================================
        // 组队大厅（嵌入主控端，实时显示）
        // ============================================================
        public IActionResult Lobby(string roomId)
        {
            var room = PartyHub.GetRoom(roomId);
            if (room == null)
            {
                return RedirectToAction("Host");
            }
            ViewBag.Room = room;
            return View();
        }

        // ============================================================
        // API: 获取房间信息
        // ============================================================
        [HttpGet]
        public IActionResult GetRoomInfo(string roomId)
        {
            var room = PartyHub.GetRoom(roomId);
            if (room == null)
            {
                return Json(new { success = false, message = "房间不存在" });
            }
            return Json(new { success = true, room });
        }

        // ============================================================
        // API: 获取所有房间
        // ============================================================
        [HttpGet]
        public IActionResult GetAllRooms()
        {
            var rooms = PartyHub.GetAllRooms();
            return Json(new { success = true, rooms });
        }

        // ============================================================
        // API: 验证房间是否存在
        // ============================================================
        [HttpGet]
        public IActionResult ValidateRoom(string roomId)
        {
            var room = PartyHub.GetRoom(roomId);
            if (room == null)
            {
                return Json(new { success = false, message = "房间不存在" });
            }
            if (room.Status == "playing" || room.Status == "ended")
            {
                return Json(new { success = false, message = "房间已关闭" });
            }
            return Json(new { success = true, roomName = room.HostName, playerCount = room.Players.Count });
        }
    }
}
