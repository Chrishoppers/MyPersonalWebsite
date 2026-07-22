using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyPersonalWebsite.Controllers
{
    public class HomeController : Controller
    {
        private readonly DataSyncService _dataSync;

        public HomeController(DataSyncService dataSync)
        {
            _dataSync = dataSync;
        }

        public async Task<IActionResult> Index()
        {
            var blogs = await _dataSync.GetBlogsAsync();
            var latestBlogs = blogs.Take(3).ToList();

            ViewBag.LatestBlogs = latestBlogs;
            ViewBag.Projects = new List<Project>();

            return View();
        }

        public async Task<IActionResult> About()
        {
            return View();
        }

        public async Task<IActionResult> Profile()
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

            ViewBag.User = user;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile(string field)
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

            ViewBag.Field = field;
            ViewBag.CurrentValue = field == "username" ? user.Username : user.Email;
            ViewBag.PendingValue = field == "username" ? user.PendingUsername : user.PendingEmail;
            ViewBag.User = user;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(string field, string value)
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

            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;

            if (field == "username")
            {
                if (isAdmin == 1)
                {
                    user.Username = value;
                    user.IsUsernameChangeApproved = true;
                }
                else
                {
                    user.PendingUsername = value;
                    user.IsUsernameChangeApproved = false;
                }
            }
            else if (field == "email")
            {
                if (isAdmin == 1)
                {
                    user.Email = value;
                    user.IsEmailChangeApproved = true;
                }
                else
                {
                    user.PendingEmail = value;
                    user.IsEmailChangeApproved = false;
                }
            }

            await _dataSync.UpdateUserAsync(user);

            TempData["Success"] = isAdmin == 1 ? "修改成功！" : "修改已提交，等待管理员审核";
            return RedirectToAction("Profile");
        }

        // ============================================================
        // ⭐ 上传头像（完整修复版 - 中文不乱码）
        // ============================================================
        [HttpPost]
public async Task<IActionResult> UploadAvatar(IFormFile avatar)
{
    var userId = HttpContext.Session.GetInt32("UserId");
    if (!userId.HasValue)
    {
        return RedirectToAction("Login", "Auth");
    }

    if (avatar == null || avatar.Length == 0)
    {
        TempData["AvatarError"] = "请选择图片";
        return RedirectToAction("Profile");
    }

    var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
    if (!allowedTypes.Contains(avatar.ContentType))
    {
        TempData["AvatarError"] = "只支持 JPG, PNG, GIF, WebP 格式";
        return RedirectToAction("Profile");
    }

    if (avatar.Length > 5 * 1024 * 1024)
    {
        TempData["AvatarError"] = "图片不能超过 5MB";
        return RedirectToAction("Profile");
    }

    var fileName = $"{Guid.NewGuid():N}_{avatar.FileName}";
    var uploadPath = Path.Combine("wwwroot", "images", "avatars");

    if (!Directory.Exists(uploadPath))
    {
        Directory.CreateDirectory(uploadPath);
    }

    var filePath = Path.Combine(uploadPath, fileName);
    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await avatar.CopyToAsync(stream);
    }

    var avatarUrl = $"/images/avatars/{fileName}";
    var user = await _dataSync.GetUserByIdAsync(userId.Value);

    if (user != null)
    {
        var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
        user.IsAvatarApproved = isAdmin == 1;
        user.AvatarUrl = avatarUrl;
        user.AvatarSubmittedAt = DateTime.Now;
        await _dataSync.UpdateUserAsync(user);
    }

    var message = user?.IsAvatarApproved == true ? "🎉 头像更新成功！" : "📸 头像已提交，等待管理员审核";
    TempData["AvatarSuccess"] = message;
    TempData["AvatarUrl"] = avatarUrl;

    return RedirectToAction("Profile");
}
        public IActionResult Contact()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RequestContact([FromBody] ContactRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return Json(new { success = false, message = "请先登录" });
                }

                var user = await _dataSync.GetUserByIdAsync(userId.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "用户不存在" });
                }

                if (string.IsNullOrEmpty(request.Platform) ||
                    string.IsNullOrEmpty(request.HowKnowMe) ||
                    string.IsNullOrEmpty(request.Identity) ||
                    string.IsNullOrEmpty(request.Relationship))
                {
                    return Json(new { success = false, message = "请填写所有必填项" });
                }

                var code = GenerateAuthorizationCode();

                var contactRequest = new ContactRequest
                {
                    Platform = request.Platform,
                    AuthorizationCode = code,
                    HowKnowMe = request.HowKnowMe,
                    Identity = request.Identity,
                    Relationship = request.Relationship,
                    Remarks = request.Remarks ?? string.Empty,
                    RequestTime = DateTime.Now,
                    IsApproved = false,
                    IsUsed = false,
                    UserId = user.Id,
                    Username = user.Username,
                    UserEmail = user.Email
                };

                await _dataSync.AddContactRequestAsync(contactRequest);

                try
                {
                    var emailService = HttpContext.RequestServices.GetService<BrevoEmailService>();
                    if (emailService != null)
                    {
                        await emailService.SendAdminNewContactRequestNotificationAsync(
                            request.Identity,
                            request.Platform,
                            code,
                            request.HowKnowMe,
                            request.Relationship,
                            user.Username,
                            user.Email
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"管理员通知邮件发送失败: {ex.Message}");
                }

                var options = new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                return Json(new { success = true, code = code }, options);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "申请失败，请重试" });
            }
        }

        public async Task<IActionResult> QueryContact(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return Json(new { success = false, message = "请输入授权码" });
            }

            // 从数据库查询授权码
            var requests = await _dataSync.GetContactRequestsAsync();
            var request = requests.FirstOrDefault(r => r.AuthorizationCode == code && !r.IsUsed);

            if (request == null)
            {
                return Json(new { success = false, message = "授权码无效或已使用" });
            }

            // 标记为已使用
            request.IsUsed = true;
            request.UsedTime = DateTime.Now;
            await _dataSync.UpdateContactRequestAsync(request);

            var options = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            // 根据平台返回不同的联系方式
            string contactInfo = request.Platform == "WeChat" ? "💬 微信号：Chris_hopper" : "🐧 QQ号：2908685235";

            return Json(new
            {
                success = true,
                data = new
                {
                    Platform = request.Platform,
                    AuthorizationCode = request.AuthorizationCode,
                    ContactInfo = contactInfo
                }
            }, options);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private string GenerateAuthorizationCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = new Random();
            var parts = new string[4];
            for (int i = 0; i < 4; i++)
            {
                char[] part = new char[4];
                for (int j = 0; j < 4; j++)
                {
                    part[j] = chars[random.Next(chars.Length)];
                }
                parts[i] = new string(part);
            }
            return string.Join("-", parts);
        }
    }
}
