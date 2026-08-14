// Controllers/PartyController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MyPersonalWebsite.Hubs;
using System;

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
        // 派对主界面（入口）
        // ============================================================
        public IActionResult Host()
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                return RedirectToAction("Login", "Auth");
            }
            return View();
        }

        // ============================================================
        // 创建房间 - 成功后跳转到狼人杀主控端
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> CreateRoom(string roomName, int maxPlayers = 12)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                return Json(new { success = false, message = "权限不足" });
            }

            try
            {
                // 生成房间码
                var roomId = GenerateRoomCode();
                
                // 可以在这里保存房间信息到数据库或内存
                // 暂时用 TempData 传递
                TempData["RoomId"] = roomId;
                TempData["RoomName"] = roomName;

                return Json(new { 
                    success = true, 
                    roomId = roomId,
                    redirectUrl = $"/Werewolf/Host?roomId={roomId}"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============================================================
        // 玩家加入派对
        // ============================================================
        public IActionResult Player(string? roomId)
        {
            ViewBag.RoomId = roomId;
            return View();
        }

        // ============================================================
        // 验证房间
        // ============================================================
        [HttpGet]
        public IActionResult ValidateRoom(string roomId)
        {
            // 这里可以验证房间是否存在
            // 暂时返回成功
            return Json(new { success = true, roomName = "狼人杀派对" });
        }

        private string GenerateRoomCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = new Random();
            var parts = new string[2];
            for (int i = 0; i < 2; i++)
            {
                char[] part = new char[4];
                for (int j = 0; j < 4; j++)
                {
                    part[j] = chars[random.Next(chars.Length)];
                }
                parts[i] = new string(part);
            }
            return $"{parts[0]}-{parts[1]}";
        }
    }
}
