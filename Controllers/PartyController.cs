using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MyPersonalWebsite.Hubs;

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
        // 主控端页面（仅 admin 可访问 - 使用 Session 检查）
        // ============================================================
        public IActionResult Host()
        {
            // ⭐ 使用 Session 检查是否为 admin
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                // 未登录或不是 admin，跳转到登录页
                return RedirectToAction("Login", "Auth");
            }

            var username = HttpContext.Session.GetString("Username") ?? "admin";
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
        // API: 验证房间是否存在
        // ============================================================
        [HttpGet]
        public IActionResult ValidateRoom(string roomId)
        {
            try
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
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
