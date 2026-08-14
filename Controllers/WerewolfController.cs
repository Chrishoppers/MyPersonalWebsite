using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MyPersonalWebsite.Hubs;
using MyPersonalWebsite.Models.Werewolf;

namespace MyPersonalWebsite.Controllers
{
    public class WerewolfController : Controller
    {
        private readonly IHubContext<WerewolfHub> _hubContext;

        public WerewolfController(IHubContext<WerewolfHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public IActionResult Host()
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1) return RedirectToAction("Login", "Auth");
            return View();
        }

        public IActionResult GameControl(string roomId)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1) return RedirectToAction("Login", "Auth");

            var game = WerewolfHub.GetGame(roomId);
            if (game == null) return RedirectToAction("Host");

            ViewBag.RoomId = roomId;
            return View();
        }

        public IActionResult Player(string? roomId)
        {
            ViewBag.RoomId = roomId;
            return View();
        }

        public IActionResult Rules(string roomId)
        {
            ViewBag.RoomId = roomId;
            var game = WerewolfHub.GetGame(roomId);
            if (game != null)
            {
                ViewBag.PlayerCount = game.PlayerCount;
                ViewBag.SelectedRoles = game.Players.Where(p => p.IsGod).Select(p => p.Role).Distinct().ToList();
            }
            return View();
        }

        public IActionResult HostControl(string roomId)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1) return RedirectToAction("Login", "Auth");

            ViewBag.RoomId = roomId;
            return View();
        }

        [HttpGet]
        public IActionResult ValidateRoom(string roomId)
        {
            var game = WerewolfHub.GetGame(roomId);
            if (game == null) return Json(new { success = false, message = "房间不存在" });

            return Json(new
            {
                success = true,
                playerCount = game.PlayerCount,
                isStarted = game.Phase != GamePhase.Setup && game.Phase != GamePhase.Seating
            });
        }

        [HttpGet]
        public IActionResult GetGameState(string roomId)
        {
            var game = WerewolfHub.GetGame(roomId);
            if (game == null) return Json(new { success = false, message = "房间不存在" });

            return Json(new
            {
                success = true,
                phase = game.Phase.ToString(),
                day = game.Day,
                night = game.Night,
                isGameOver = game.IsGameOver,
                winner = game.Winner,
                players = game.Players.Select(p => new
                {
                    p.SeatNumber,
                    p.Nickname,
                    p.AvatarEmoji,
                    p.IsAlive,
                    p.IsReady,
                    p.IsSpectator,
                    Role = p.Role.ToString(),
                    IsSheriff = p.IsSheriff
                }),
                sheriffId = game.SheriffId
            });
        }
    }
}
