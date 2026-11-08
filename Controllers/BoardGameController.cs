using Microsoft.AspNetCore.Mvc;

namespace MyPersonalWebsite.Controllers;

public sealed class BoardGameController : Controller
{
    public IActionResult Index() => View();

    public IActionResult Privacy(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : "/BoardGame/Index";
        return View();
    }

    public IActionResult Play(string? type, string? mode, string? level, string? code, string? token)
    {
        ViewBag.GameType = type ?? "gomoku";
        ViewBag.Mode = mode ?? "pvp";
        ViewBag.AiLevel = level ?? "";
        ViewBag.RoomCode = code ?? "";
        ViewBag.Token = token ?? "";
        return View();
    }

    [HttpGet("/BoardGame/Join/{code}")]
    public IActionResult Join(string code)
    {
        ViewBag.RoomCode = code.ToUpperInvariant();
        return View();
    }

    public IActionResult Replay(long id)
    {
        ViewBag.RoomId = id;
        return View();
    }

    public IActionResult Replays() => View();
}
