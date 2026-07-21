using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Services;
using MyPersonalWebsite.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyPersonalWebsite.Controllers
{
    public class MessageController : Controller
    {
        private readonly AppDbContext _context;
        private readonly LikeService _likeService;
        private readonly BrevoEmailService _emailService;
        private readonly IHubContext<MessageHub> _hubContext;

        public MessageController(
            AppDbContext context,
            LikeService likeService,
            BrevoEmailService emailService,
            IHubContext<MessageHub> hubContext)
        {
            _context = context;
            _likeService = likeService;
            _emailService = emailService;
            _hubContext = hubContext;
        }

        // ============================================================
        // 1. 留言大屏
        // ============================================================
        public async Task<IActionResult> Index()
        {
            var messages = await _context.Messages
                .Include(m => m.User)
                .OrderByDescending(m => m.CreateTime)
                .ToListAsync();

            var userId = HttpContext.Session.GetInt32("UserId");
            var likedIds = new HashSet<int>();

            if (userId.HasValue)
            {
                try
                {
                    var likes = await _context.MessageLikes
                        .Where(l => l.UserId == userId.Value)
                        .Select(l => l.MessageId)
                        .ToListAsync();
                    likedIds = new HashSet<int>(likes);
                }
                catch
                {
                    likedIds = new HashSet<int>();
                }
            }

            ViewBag.LikedIds = likedIds;
            ViewBag.CurrentUserId = userId;
            ViewBag.IsAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;

            return View(messages);
        }

        // ============================================================
        // 2. 创建留言（页面）
        // ============================================================
        [HttpGet]
        public IActionResult Create()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }
            return View();
        }

        // ============================================================
        // 3. 创建留言（提交）
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Message message)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null || user.IsBanned)
            {
                ModelState.AddModelError("", user?.IsBanned == true ? "您的账号已被封禁" : "请先登录");
                return View();
            }

            if (ModelState.IsValid)
            {
                message.UserId = userId.Value;
                message.VisitorName = user.Username;
                message.Email = user.Email;
                message.CreateTime = DateTime.Now;
                message.LikeCount = 0;

                if (user.IsAdmin)
                {
                    message.IsApproved = true;
                }
                else
                {
                    message.IsApproved = false;
                }

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                TempData["Success"] = user.IsAdmin ? "留言发布成功！" : "留言已提交，等待管理员审核后显示";
                return RedirectToAction("Index");
            }

            return View(message);
        }

        // ============================================================
        // 4. 点赞/取消点赞
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> ToggleLike(int messageId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "请先登录" });
            }

            try
            {
                var result = await _likeService.ToggleLikeAsync(messageId, userId.Value);
                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    likeCount = result.NewLikeCount,
                    isLiked = result.IsLiked
                });
            }
            catch
            {
                return Json(new { success = false, message = "点赞失败，请稍后重试" });
            }
        }

        // ============================================================
        // 5. 举报留言
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> Report(int messageId, string reason)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "请先登录" });
            }

            var message = await _context.Messages.FindAsync(messageId);
            if (message == null)
            {
                return Json(new { success = false, message = "留言不存在" });
            }

            if (message.UserId == userId.Value)
            {
                return Json(new { success = false, message = "不能举报自己的留言" });
            }

            var existingReport = await _context.ReportRecords
                .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId.Value);
            if (existingReport != null)
            {
                return Json(new { success = false, message = "您已举报过这条留言" });
            }

            var report = new ReportRecord
            {
                MessageId = messageId,
                UserId = userId.Value,
                Reason = reason,
                CreateTime = DateTime.Now
            };
            _context.ReportRecords.Add(report);

            message.ReportCount++;
            if (message.ReportCount >= 3)
            {
                message.IsReported = true;
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "举报已提交" });
        }

        // ============================================================
        // 6. 管理员删除留言
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                return Json(new { success = false, message = "权限不足" });
            }

            var message = await _context.Messages.FindAsync(id);
            if (message == null)
            {
                return Json(new { success = false, message = "留言不存在" });
            }

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "删除成功" });
        }

        // ============================================================
        // 7. 管理员审核留言
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                return Json(new { success = false, message = "权限不足" });
            }

            var message = await _context.Messages
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (message == null)
            {
                return Json(new { success = false, message = "留言不存在" });
            }

            message.IsApproved = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "审核通过" });
        }

        // ============================================================
        // 8. 管理员回复留言
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int messageId, string replyContent)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                return Json(new { success = false, message = "权限不足" });
            }

            var message = await _context.Messages
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
            {
                return Json(new { success = false, message = "留言不存在" });
            }

            message.AdminReply = replyContent;
            message.AdminReplyTime = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "回复成功" });
        }
    }
}
