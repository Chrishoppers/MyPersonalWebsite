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
        private readonly PlatformVerifyService _platformVerifyService;

        public ResourceController(
            DataSyncService dataSync,
            BrevoEmailService emailService,
            AppDbContext context,
            PlatformVerifyService platformVerifyService)
        {
            _dataSync = dataSync;
            _emailService = emailService;
            _context = context;
            _platformVerifyService = platformVerifyService;
        }
        // Controllers/ResourceController.cs - Index 方法

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

    // ⭐ 如果是受限用户，显示欢迎信息
    if (user.IsRestricted)
    {
        ViewBag.WelcomeMessage = "👋 欢迎来到资源专区！你通过二维码注册，可在此提交资源申请。";
    }

    ViewBag.User = user;
    return View();
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
        // 2. 提交资源申请 (GET)
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

            var pendingRequests = await _dataSync.GetPendingResourceRequestsAsync(userId.Value);
            if (pendingRequests.Any())
            {
                TempData["Warning"] = "您有未处理的资源申请，请等待管理员处理后再提交新的申请";
                return RedirectToAction("History");
            }

            ViewBag.User = user;
            ViewBag.UserName = user.Username;
            ViewBag.UserEmail = user.Email;
            ViewBag.SupportedPlatforms = _platformVerifyService.GetSupportedPlatforms();

            return View(new ResourceRequest
            {
                UserId = userId.Value,
                UserName = user.Username,
                UserEmail = user.Email,
                ResourceType = "一人",
                CharacterSetting = "都行",
                NovelPreference = "不需要",
                ComicPreference = "不需要",
                ImagePreference = "不需要"
            });
        }

        // ============================================================
        // 3. 提交资源申请 (POST)
        // ============================================================
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

            // ============================================================
            // 1. 验证平台选择
            // ============================================================
            var platforms = new List<string>();
            if (!string.IsNullOrEmpty(request.Platform1)) platforms.Add(request.Platform1);
            if (!string.IsNullOrEmpty(request.Platform2)) platforms.Add(request.Platform2);

            if (platforms.Count == 0)
            {
                ModelState.AddModelError("", "请至少选择一个平台");
                ViewBag.User = user;
                return View(request);
            }

            if (platforms.Count > 2)
            {
                ModelState.AddModelError("", "最多选择2个平台");
                ViewBag.User = user;
                return View(request);
            }

            if (platforms.Contains("其他") && string.IsNullOrEmpty(request.PlatformOther))
            {
                ModelState.AddModelError("", "请填写其他平台名称");
                ViewBag.User = user;
                return View(request);
            }

            // ============================================================
            // 2. 验证人物/CP名字
            // ============================================================
            if (string.IsNullOrEmpty(request.CharacterName) || request.CharacterName.Length < 2)
            {
                ModelState.AddModelError("", "请输入人物/CP名字（至少2个字符）");
                ViewBag.User = user;
                return View(request);
            }

            // ============================================================
            // 3. 验证里克特量表
            // ============================================================
            if (request.NovelPreference == "不需要" &&
                request.ComicPreference == "不需要" &&
                request.ImagePreference == "不需要")
            {
                ModelState.AddModelError("", "小说、漫画、图片中至少有一项不能选择'不需要'");
                ViewBag.User = user;
                return View(request);
            }

            // ============================================================
            // 4. 验证免责声明
            // ============================================================
            if (!request.AgreeToBLContent)
            {
                ModelState.AddModelError("", "请确认申请内容为男同性向（BL）内容");
                ViewBag.User = user;
                return View(request);
            }

            if (!request.AgreeToTerms)
            {
                ModelState.AddModelError("", "请阅读并同意完整免责声明");
                ViewBag.User = user;
                return View(request);
            }

            // ============================================================
            // 5. 验证关注
            // ============================================================
            if (!string.IsNullOrEmpty(request.VerifyPlatform) && !string.IsNullOrEmpty(request.VerifyAccountId))
            {
                var (isValid, message, displayName, verifyStatus) = await _platformVerifyService.VerifyFollowAsync(
                    request.VerifyPlatform,
                    request.VerifyAccountId
                );

                request.IsFollowVerified = isValid;
                request.FollowVerifyError = isValid ? null : message;
                request.FollowVerifiedAt = DateTime.Now;
                request.VerifyStatus = verifyStatus;

                if (verifyStatus == "rejected")
                {
                    ModelState.AddModelError("", message);
                    ViewBag.User = user;
                    return View(request);
                }

                if (verifyStatus == "manual_required")
                {
                    TempData["Warning"] = $"⚠️ {message}，管理员将人工核验";
                }
                else if (verifyStatus == "auto_verified")
                {
                    TempData["Success"] = "✅ 关注验证通过！";
                }
            }

            // ============================================================
            // 6. 检查是否有未处理的事项
            // ============================================================
            var pendingRequests = await _dataSync.GetPendingResourceRequestsAsync(userId.Value);
            if (pendingRequests.Any())
            {
                TempData["Error"] = "您有未处理的资源申请，请等待管理员处理";
                return RedirectToAction("History");
            }
             // ============================================================
    // 保存申请（自动生成订单号）
    // ============================================================
    var now = DateTime.Now;
    request.UserId = userId.Value;
    request.UserName = user.Username;
    request.UserEmail = user.Email;
    request.Status = "pending";
    request.Amount = 2.00m;  // 默认2元
    request.CreatedAt = now;
    request.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
    request.UserAgent = Request.Headers["User-Agent"].ToString();

    await _dataSync.AddResourceRequestAsync(request);

    // ⭐ 通知管理员
    try
    {
        await _emailService.SendResourceRequestNotificationAsync(request);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"管理员通知邮件发送失败: {ex.Message}");
    }

    // ⭐ 跳转到支付页面
    return RedirectToAction("Pay", new { id = request.Id });

            

           
        }

        // ============================================================
        // 4. 验证关注 (AJAX)
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> VerifyFollow(string platform, string accountId)
        {
            if (string.IsNullOrEmpty(platform) || string.IsNullOrEmpty(accountId))
            {
                return Json(new { success = false, message = "平台和账号ID不能为空" });
            }

            var (isValid, message, displayName, verifyStatus) = await _platformVerifyService.VerifyFollowAsync(platform, accountId);

            return Json(new
            {
                success = true,
                isValid = isValid,
                message = message,
                displayName = displayName,
                verifyStatus = verifyStatus
            });
        }

        // ============================================================
        // 5. 历史记录
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
        // 6. 申请详情
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
        // Controllers/ResourceController.cs

// Controllers/ResourceController.cs - Pay 方法

/// <summary>
/// 支付页面（只支持微信）
/// </summary>
// Controllers/ResourceController.cs - Pay 方法

[HttpGet]
public async Task<IActionResult> Pay(int id)
{
    var userId = HttpContext.Session.GetInt32("UserId");
    if (!userId.HasValue)
        return RedirectToAction("Login", "Auth");

    var request = await _dataSync.GetResourceRequestByIdAsync(id);
    if (request == null || request.UserId != userId.Value)
        return NotFound();

    if (request.IsPaid)
    {
        TempData["Message"] = "该订单已支付";
        return RedirectToAction("History", "Resource");
    }

    // ⭐ 设置支付方式为微信（模型已包含 PaymentMethod 字段）
    request.PaymentMethod = "wechat";
    await _dataSync.UpdateResourceRequestAsync(request);

    ViewBag.QRCodeUrl = "/images/payment/wechat_qr.jpg";

    return View(request);
}
/// <summary>
/// 检查支付状态（AJAX）
/// </summary>
[HttpGet]
public async Task<IActionResult> CheckPayment(int requestId)
{
    var userId = HttpContext.Session.GetInt32("UserId");
    if (!userId.HasValue)
        return Json(new { paid = false });

    var request = await _dataSync.GetResourceRequestByIdAsync(requestId);
    if (request == null || request.UserId != userId.Value)
        return Json(new { paid = false });

    return Json(new { paid = request.IsPaid });
}
    }
}
