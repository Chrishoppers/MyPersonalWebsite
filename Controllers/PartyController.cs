using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MyPersonalWebsite.Hubs;
using System;
using System.Linq;

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
        // 主控端页面 - 仅 admin 可访问
        // ============================================================
        public IActionResult Host()
        {
            // 检查 Session 中的 IsAdmin
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin");
            
            // 如果 Session 中没有 IsAdmin，检查用户是否在数据库中为 admin
            if (!isAdmin.HasValue)
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId.HasValue)
                {
                    // 从数据库检查用户是否是 admin
                    try
                    {
                        using var scope = HttpContext.RequestServices.CreateScope();
                        var dataSync = scope.ServiceProvider.GetRequiredService<MyPersonalWebsite.Services.DataSyncService>();
                        var user = dataSync.GetUserByIdAsync(userId.Value).GetAwaiter().GetResult();
                        if (user != null && user.IsAdmin)
                        {
                            HttpContext.Session.SetInt32("IsAdmin", 1);
                            isAdmin = 1;
                        }
                        else
                        {
                            HttpContext.Session.SetInt32("IsAdmin", 0);
                            isAdmin = 0;
                        }
                    }
                    catch
                    {
                        isAdmin = 0;
                    }
                }
                else
                {
                    isAdmin = 0;
                }
            }

            if (isAdmin != 1)
            {
                // 不是管理员，跳转到登录页
                return RedirectToAction("Login", "Auth");
            }

            var username = HttpContext.Session.GetString("Username") ?? "admin";
            ViewBag.Username = username;
            return View();
        }

        // ============================================================
        // 玩家端页面（任何人可访问）
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

        // ============================================================
        // API: 获取所有房间
        // ============================================================
        [HttpGet]
        public IActionResult GetAllRooms()
        {
            try
            {
                var rooms = PartyHub.GetAllRooms();
                return Json(new { success = true, rooms });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
