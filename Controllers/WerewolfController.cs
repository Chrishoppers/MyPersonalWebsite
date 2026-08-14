using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MyPersonalWebsite.Hubs;
using MyPersonalWebsite.Models.Werewolf;
using System;

namespace MyPersonalWebsite.Controllers
{
    public class WerewolfController : Controller
    {
        private readonly IHubContext<WerewolfHub> _hubContext;

        public WerewolfController(IHubContext<WerewolfHub> hubContext)
        {
            _hubContext = hubContext;
        }

        // ============================================================
        // 主控端 - 游戏设置
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
        // 主控端 - 游戏控制中心
        // ============================================================
        public IActionResult GameControl(string roomId)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                return RedirectToAction("Login", "Auth");
            }

            var game = WerewolfHub.GetGame(roomId);
            if (game == null)
            {
                return RedirectToAction("Host");
            }

            ViewBag.RoomId = roomId;
            return View();
        }

        // ============================================================
        // 玩家端
        // ============================================================
        public IActionResult Player(string? roomId)
        {
            ViewBag.RoomId = roomId;
            return View();
        }

        // ============================================================
        // 规则页面
        // ============================================================
        public IActionResult Rules(string roomId)
        {
            ViewBag.RoomId = roomId;
            var game = WerewolfHub.GetGame(roomId);
            if (game != null)
            {
                ViewBag.PlayerCount = game.PlayerCount;
                ViewBag.SelectedRoles = game.Players.Where(p => p.IsGod).Select(p => p.RoleDisplay).Distinct().ToList();
            }
            return View();
        }

        // ============================================================
        // API: 验证房间
        // ============================================================
        [HttpGet]
        public IActionResult ValidateRoom(string roomId)
        {
            var game = WerewolfHub.GetGame(roomId);
            if (game == null)
            {
                return Json(new { success = false, message = "房间不存在" });
            }
            return Json(new
            {
                success = true,
                playerCount = game.PlayerCount,
                isStarted = game.Phase != GamePhase.Setup && game.Phase != GamePhase.Seating
            });
        }

        // ============================================================
        // API: 获取游戏状态
        // ============================================================
        [HttpGet]
        public IActionResult GetGameState(string roomId)
        {
            var game = WerewolfHub.GetGame(roomId);
            if (game == null)
            {
                return Json(new { success = false, message = "房间不存在" });
            }

            var players = game.Players.Select(p => new
            {
                p.SeatNumber,
                p.Nickname,
                p.AvatarEmoji,
                p.IsAlive,
                p.IsReady,
                p.IsSpectator,
                Role = p.Role.ToString(),
                IsSheriff = p.IsSheriff,
                IsGod = p.IsGod
            });

            return Json(new
            {
                success = true,
                phase = game.Phase.ToString(),
                day = game.Day,
                night = game.Night,
                isGameOver = game.IsGameOver,
                winner = game.Winner,
                players = players,
                sheriffId = game.SheriffId
            });
        }

        // ============================================================
        // API: 获取所有房间
        // ============================================================
        [HttpGet]
        public IActionResult GetAllRooms()
        {
            var rooms = WerewolfHub.GetAllGames();
            return Json(new
            {
                success = true,
                rooms = rooms.Select(r => new
                {
                    r.RoomId,
                    r.PlayerCount,
                    r.Phase,
                    r.Day,
                    r.Night,
                    r.IsGameOver
                })
            });
        }
    }
}
