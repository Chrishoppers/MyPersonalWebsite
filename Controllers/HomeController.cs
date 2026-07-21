using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        // ============================================================
        // 首页
        // ============================================================
        public async Task<IActionResult> Index()
        {
            var blogs = await _dataSync.GetBlogsAsync();
            var latestBlogs = blogs.Take(3).ToList();

            ViewBag.LatestBlogs = latestBlogs;
            ViewBag.Projects = new List<Project>(); // 暂时为空

            return View();
        }

        // ============================================================
        // 关于我
        // ============================================================
        public async Task<IActionResult> About()
        {
            // TODO: 从 DataSync 读取 AboutMe 内容
            return View();
        }

        // ============================================================
        // 个人信息页面
        // ============================================================
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

        // ============================================================
        // 修改昵称/邮箱页面
        // ============================================================
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
        // 上传头像
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> UploadAvatar(IFormFile avatar)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "请先登录" });
            }

            if (avatar == null || avatar.Length == 0)
            {
                return Json(new { success = false, message = "请选择图片" });
            }

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(avatar.ContentType))
            {
                return Json(new { success = false, message = "只支持 JPG, PNG, GIF, WebP 格式" });
            }

            if (avatar.Length > 5 * 1024 * 1024)
            {
                return Json(new { success = false, message = "图片不能超过 5MB" });
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

            var message = user?.IsAvatarApproved == true ? "头像更新成功！" : "头像已提交，等待管理员审核";
            return Json(new { success = true, url = avatarUrl, message = message });
        }

        // ============================================================
        // 联系方式页面
        // ============================================================
        public IActionResult Contact()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }
            return View();
        }
    }
}
        // ============================================================
        // 申请联系方式
        // ============================================================
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
                    IsApproved = true,
                    IsUsed = false,
                    UserId = user.Id,
                    Username = user.Username,
                    UserEmail = user.Email
                };

                // TODO: 保存 contactRequest 到数据库
                // await _dataSync.AddContactRequestAsync(contactRequest);

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

                return Json(new { success = true, code = code });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "申请失败，请重试" });
            }
        }

        // ============================================================
        // 管理员查询授权码
        // ============================================================
        public async Task<IActionResult> QueryContact(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return Json(new { success = false, message = "请输入授权码" });
            }

            // TODO: 从数据库查询授权码
            // var request = await _dataSync.GetContactRequestByCodeAsync(code);

            string contactInfo = "💬 微信号：Chris_hopper";
            return Json(new
            {
                success = true,
                data = new
                {
                    Platform = "WeChat",
                    AuthorizationCode = code,
                    ContactInfo = contactInfo
                }
            });
        }

        // ============================================================
        // 错误页面
        // ============================================================
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // ============================================================
        // 生成授权码
        // ============================================================
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
