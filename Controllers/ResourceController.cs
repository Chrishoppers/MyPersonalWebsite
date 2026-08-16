using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MyPersonalWebsite.Controllers
{
    public class ResourceController : Controller
    {
        private readonly DataSyncService _dataSync;
        private readonly BrevoEmailService _emailService;
        private readonly AppDbContext _context;

        public ResourceController(DataSyncService dataSync, BrevoEmailService emailService, AppDbContext context)
        {
            _dataSync = dataSync;
            _emailService = emailService;
            _context = context;
        }

        // ============================================================
        // 1. 资源大厅
        // ============================================================
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = await _dataSync.GetUserByIdAsync(userId.Value);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (user.IsBanned)
            {
                TempData["Error"] = "您的账号已被封禁，无法使用资源系统";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.User = user;
            return View();
        }

        // ============================================================
        // 2. 提交资源申请
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Submit()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = await _dataSync.GetUserByIdAsync(userId.Value);
            if (user == null || user.IsBanned)
            {
                TempData["Error"] = user?.IsBanned == true ? "账号已被封禁" : "用户不存在";
                return RedirectToAction("Index", "Home");
            }

            // 检查是否有未处理的事项
            var pendingRequests = await _dataSync.GetPendingResourceRequestsAsync(userId.Value);
            if (pendingRequests.Any())
            {
                TempData["Warning"] = "您有未处理的资源申请，请等待管理员处理后再提交新的申请";
                return RedirectToAction("History");
            }

            ViewBag.User = user;
            ViewBag.UserName = user.Username;
            ViewBag.UserEmail = user.Email;

            return View(new ResourceRequest
            {
                UserId = userId.Value,
                UserName = user.Username,
                UserEmail = user.Email,
                RefundOption = "2weeks_free"
            });
        }

        // Controllers/ResourceController.cs - Submit POST 方法

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Submit(ResourceRequest request)
{
    var userId = HttpContext.Session.GetInt32("UserId");
    if (!userId.HasValue)
    {
        return RedirectToAction("Login", "Auth");
    }

    var user = await _dataSync.GetUserByIdAsync(userId.Value);
    if (user == null || user.IsBanned)
    {
        TempData["Error"] = "账号不可用";
        return RedirectToAction("Index", "Home");
    }

    // 验证
    if (string.IsNullOrEmpty(request.PersonName) || string.IsNullOrEmpty(request.ResourceName))
    {
        ModelState.AddModelError("", "请填写必填项");
        ViewBag.User = user;
        ViewBag.UserName = user.Username;
        ViewBag.UserEmail = user.Email;
        return View(request);
    }

    // 检查是否有未处理的事项
    var pendingRequests = await _dataSync.GetPendingResourceRequestsAsync(userId.Value);
    if (pendingRequests.Any())
    {
        TempData["Error"] = "您有未处理的资源申请，请等待管理员处理";
        return RedirectToAction("History");
    }

    // 设置退款选项
    var now = DateTime.Now;
    request.RefundOption = string.IsNullOrEmpty(request.RefundOption) ? "2weeks_free" : request.RefundOption;
    
    switch (request.RefundOption)
    {
        case "1day_paid":
            request.RefundAmount = 2.00m;
            request.RefundDeadline = now.AddDays(1);
            break;
        case "2weeks_free":
            request.RefundAmount = 0;
            request.RefundDeadline = now.AddDays(14);
            break;
        default:
            request.RefundAmount = 0;
            request.RefundDeadline = now.AddDays(14);
            request.RefundOption = "2weeks_free";
            break;
    }

    // 保存申请
    request.UserId = userId.Value;
    request.UserName = user.Username;
    request.UserEmail = user.Email;
    request.Status = "pending";
    request.CreatedAt = now;
    request.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
    request.UserAgent = Request.Headers["User-Agent"].ToString();

    await _dataSync.AddResourceRequestAsync(request);

    // ⭐ 发送通知邮件给管理员（包含申请详情和直达链接）
    try
    {
        await _emailService.SendResourceRequestNotificationAsync(request);
        Console.WriteLine($"✅ 管理员通知邮件已发送: {request.Id}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ 管理员通知邮件发送失败: {ex.Message}");
    }

    TempData["Success"] = "✅ 资源申请已提交，管理员将尽快处理";
    return RedirectToAction("History");
}

        // ============================================================
        // 3. 查看历史记录（15天内）
        // ============================================================
        public async Task<IActionResult> History()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            var requests = await _dataSync.GetResourceRequestsByUserIdAsync(userId.Value);
            var recentRequests = requests
                .Where(r => r.CreatedAt >= DateTime.Now.AddDays(-15))
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            ViewBag.HasPending = recentRequests.Any(r => r.Status == "pending" || r.Status == "processing");
            return View(recentRequests);
        }

        // ============================================================
        // 4. 查看申请详情
        // ============================================================
        public async Task<IActionResult> Detail(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            var request = await _dataSync.GetResourceRequestByIdAsync(id);
            if (request == null || request.UserId != userId.Value)
            {
                return NotFound();
            }

            return View(request);
        }
    }
}
