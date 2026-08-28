using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Collections.Generic;

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

            if (user.IsRestricted)
            {
                ViewBag.WelcomeMessage = "👋 欢迎来到资源专区！你通过二维码注册，可在此提交资源申请。";
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
        // 3. 提交资源申请 (POST) - 直接跳转到历史记录，不经过支付
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(ResourceRequest request)
        {
            Console.WriteLine("=========================================");
            Console.WriteLine("🔍 Submit POST 被调用！");
            Console.WriteLine($"🔍 当前用户ID: {HttpContext.Session.GetInt32("UserId")}");
            Console.WriteLine($"🔍 CharacterName: {request.CharacterName}");
            Console.WriteLine($"🔍 Platform1: {request.Platform1}");
            Console.WriteLine($"🔍 Platform2: {request.Platform2}");
            Console.WriteLine("=========================================");

            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                Console.WriteLine("❌ 用户未登录，跳转到 Login");
                return RedirectToAction("Login", "Auth");
            }

            var user = await _dataSync.GetUserByIdAsync(userId.Value);
            if (user == null || user.IsBanned)
            {
                Console.WriteLine($"❌ 用户不可用: IsBanned={user?.IsBanned}");
                TempData["Error"] = "账号不可用";
                return RedirectToAction("Index", "Home");
            }

            Console.WriteLine($"✅ 用户验证通过: {user.Username}");

            // ============================================================
            // 1. 验证平台选择
            // ============================================================
            var platforms = new List<string>();
            if (!string.IsNullOrEmpty(request.Platform1)) platforms.Add(request.Platform1);
            if (!string.IsNullOrEmpty(request.Platform2)) platforms.Add(request.Platform2);

            Console.WriteLine($"📱 平台列表: {string.Join(", ", platforms)}");

            if (platforms.Count == 0)
            {
                Console.WriteLine("❌ 平台选择为空");
                ModelState.AddModelError("", "请至少选择一个平台");
                ViewBag.User = user;
                return View(request);
            }

            if (platforms.Count > 2)
            {
                Console.WriteLine("❌ 平台选择超过2个");
                ModelState.AddModelError("", "最多选择2个平台");
                ViewBag.User = user;
                return View(request);
            }

            if (platforms.Contains("其他") && string.IsNullOrEmpty(request.PlatformOther))
            {
                Console.WriteLine("❌ 其他平台名称为空");
                ModelState.AddModelError("", "请填写其他平台名称");
                ViewBag.User = user;
                return View(request);
            }

            // ============================================================
            // 2. 验证人物/CP名字
            // ============================================================
            if (string.IsNullOrEmpty(request.CharacterName) || request.CharacterName.Length < 2)
            {
                Console.WriteLine($"❌ 人物名字无效: {request.CharacterName}");
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
                Console.WriteLine("❌ 里克特量表全选不需要");
                ModelState.AddModelError("", "小说、漫画、图片中至少有一项不能选择'不需要'");
                ViewBag.User = user;
                return View(request);
            }

            // ============================================================
            // 4. 验证免责声明
            // ============================================================
            if (!request.AgreeToBLContent)
            {
                Console.WriteLine("❌ 未同意 BL 内容声明");
                ModelState.AddModelError("", "请确认申请内容为男同性向（BL）内容");
                ViewBag.User = user;
                return View(request);
            }

            if (!request.AgreeToTerms)
            {
                Console.WriteLine("❌ 未同意免责声明");
                ModelState.AddModelError("", "请阅读并同意完整免责声明");
                ViewBag.User = user;
                return View(request);
            }

            // ============================================================
            // 5. 平台验证关注
            // ============================================================
            if (string.IsNullOrEmpty(request.VerifyPlatform))
            {
                Console.WriteLine("❌ 验证平台为空");
                ModelState.AddModelError("", "请选择需要验证的平台");
                ViewBag.User = user;
                return View(request);
            }

            if (string.IsNullOrEmpty(request.VerifyAccountId))
            {
                Console.WriteLine("❌ 验证账号ID为空");
                ModelState.AddModelError("", "请输入你的平台账号ID");
                ViewBag.User = user;
                return View(request);
            }

            Console.WriteLine($"🔍 开始验证关注: {request.VerifyPlatform} - {request.VerifyAccountId}");

            var (isValid, message, displayName, verifyStatus) = await _platformVerifyService.VerifyFollowAsync(
                request.VerifyPlatform,
                request.VerifyAccountId
            );

            request.IsFollowVerified = isValid;
            request.FollowVerifyError = isValid ? null : message;
            request.FollowVerifiedAt = DateTime.Now;
            request.VerifyStatus = verifyStatus;

            Console.WriteLine($"✅ 验证结果: isValid={isValid}, verifyStatus={verifyStatus}");

            if (verifyStatus == "rejected")
            {
                Console.WriteLine($"❌ 验证被拒绝: {message}");
                ModelState.AddModelError("", message);
                ViewBag.User = user;
                return View(request);
            }

            // ============================================================
            // 6. 检查是否有未处理的事项 - 暂时注释掉
            // ============================================================
            // var pendingRequests = await _dataSync.GetPendingResourceRequestsAsync(userId.Value);
            // if (pendingRequests.Any())
            // {
            //     TempData["Warning"] = "您有未处理的资源申请，请等待管理员处理后再提交新的申请";
            //     return RedirectToAction("History");
            // }

            // ============================================================
            // 7. ⭐⭐⭐ 保存申请 ⭐⭐⭐
            // ============================================================
            Console.WriteLine("💾 开始保存申请...");

            var now = DateTime.Now;
            
            // 获取最大 ID
            var maxIdResult = await _dataSync.QueryAsync("SELECT MAX(Id) as MaxId FROM ResourceRequests");
            Console.WriteLine($"📊 MaxId 查询结果: {maxIdResult}");
            
            var maxId = ParseMaxId(maxIdResult);
            var newId = maxId + 1;
            
            Console.WriteLine($"📊 新ID: {newId}");
            
            request.Id = newId;
            request.UserId = userId.Value;
            request.UserName = user.Username;
            request.UserEmail = user.Email;
            request.Amount = 2.00m;
            request.Status = "pending";
            request.CreatedAt = now;
            request.ResourceName = request.CharacterName;  // 使用人物/CP名字作为资源名称
            request.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            request.UserAgent = Request.Headers["User-Agent"].ToString();
            request.OrderId = $"REQ_{DateTime.Now:yyyyMMddHHmmss}_{newId}_{new Random().Next(1000, 9999)}";

            Console.WriteLine($"📋 订单号: {request.OrderId}");

            await _dataSync.AddResourceRequestAsync(request);

            if (request.Id == 0)
            {
                Console.WriteLine("❌ 保存失败，ID 为 0");
                TempData["Error"] = "提交失败，请重试";
                return View(request);
            }

            Console.WriteLine($"✅ 资源申请已保存，ID: {request.Id}, 订单号: {request.OrderId}");

            // 通知管理员
            try
            {
                await _emailService.SendResourceRequestNotificationAsync(request);
                Console.WriteLine("✅ 管理员通知邮件已发送");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 管理员通知邮件发送失败: {ex.Message}");
            }

            // ============================================================
            // 8. ⭐⭐⭐ 直接跳转到历史记录，不再跳转支付页面 ⭐⭐⭐
            // ============================================================
            TempData["Success"] = "✅ 申请已提交成功，请等待管理员处理！";
            return RedirectToAction("History", "Resource");
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
            
            // 显示成功消息
            if (TempData["Success"] != null)
            {
                ViewBag.SuccessMessage = TempData["Success"];
            }
            
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

        // ============================================================
        // 辅助方法：解析 MaxId
        // ============================================================
        private int ParseMaxId(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
                {
                    var firstResult = results[0];
                    if (firstResult.TryGetProperty("response", out var response) &&
                        response.TryGetProperty("result", out var result))
                    {
                        if (result.TryGetProperty("rows", out var rows) && rows.GetArrayLength() > 0)
                        {
                            var row = rows[0];
                            if (row.ValueKind == JsonValueKind.Array && row.GetArrayLength() > 0)
                            {
                                var val = row[0];
                                if (val.ValueKind == JsonValueKind.Object && val.TryGetProperty("value", out var v))
                                {
                                    val = v;
                                }
                                if (val.ValueKind == JsonValueKind.Number)
                                {
                                    return val.GetInt32();
                                }
                                if (val.ValueKind == JsonValueKind.String)
                                {
                                    return int.TryParse(val.GetString(), out var parsed) ? parsed : 0;
                                }
                            }
                        }
                    }
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}
