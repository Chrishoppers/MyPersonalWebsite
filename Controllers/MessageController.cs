using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyPersonalWebsite.Controllers
{
    public class MessageController : Controller
    {
        private readonly DataSyncService _dataSync;
        private readonly BrevoEmailService _emailService;
        private readonly AppDbContext _context;

        public MessageController(DataSyncService dataSync, BrevoEmailService emailService, AppDbContext context)
        {
            _dataSync = dataSync;
            _emailService = emailService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var messages = await _dataSync.GetMessagesAsync();

            var userId = HttpContext.Session.GetInt32("UserId");
            var likedIds = new HashSet<int>();

            if (userId.HasValue)
            {
                var likes = await _context.MessageLikes
                    .Where(l => l.UserId == userId.Value)
                    .Select(l => l.MessageId)
                    .ToListAsync();
                likedIds = new HashSet<int>(likes);
            }

            ViewBag.LikedIds = likedIds;
            ViewBag.CurrentUserId = userId;
            ViewBag.IsAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;

            return View(messages);
        }

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

        [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(Message message)
{
    var userId = HttpContext.Session.GetInt32("UserId");
    if (!userId.HasValue)
    {
        return RedirectToAction("Login", "Auth");
    }

    var user = await _dataSync.GetUserByIdAsync(userId.Value);
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
        message.IsApproved = user.IsAdmin;

        // ⭐ 先保存，获取ID
        await _dataSync.AddMessageAsync(message);

        // ⭐ 现在 message.Id 已经有值了
        if (!user.IsAdmin)
        {
            try
            {
                await _emailService.SendAdminNewMessageNotificationAsync(
                    message.VisitorName,
                    message.Content,
                    message.Id  // ← 改成 message.Id
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"管理员通知邮件发送失败: {ex.Message}");
            }
        }

        TempData["Success"] = user.IsAdmin ? "留言发布成功！" : "留言已提交，等待管理员审核后显示";
        return RedirectToAction("Index");
    }

    return View(message);
}

       // ============================================================
// ⭐ 留言点赞（完全使用 Turso）
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
        // 从 Turso 获取留言
        var message = await _dataSync.GetMessageByIdAsync(messageId);
        if (message == null)
        {
            return Json(new { success = false, message = "留言不存在" });
        }

        // 不能给自己点赞
        if (message.UserId == userId.Value)
        {
            return Json(new { success = false, message = "不能给自己的留言点赞" });
        }

        // ⭐ 检查是否已点赞（从 Turso 查询）
        var likeResult = await _dataSync.QueryAsync(
            $"SELECT * FROM MessageLikes WHERE MessageId = {messageId} AND UserId = {userId.Value}"
        );
        var hasLiked = likeResult.Contains("\"rows\":") && !likeResult.Contains("\"rows\":[]");

        if (hasLiked)
        {
            // 取消点赞
            message.LikeCount--;
            await _dataSync.UpdateMessageAsync(message);
            await _dataSync.DeleteMessageLikeAsync(messageId, userId.Value);

            return Json(new
            {
                success = true,
                isLiked = false,
                likeCount = message.LikeCount,
                message = "已取消点赞"
            });
        }
        else
        {
            // 点赞
            message.LikeCount++;
            await _dataSync.UpdateMessageAsync(message);
            await _dataSync.AddMessageLikeAsync(messageId, userId.Value);

            return Json(new
            {
                success = true,
                isLiked = true,
                likeCount = message.LikeCount,
                message = "点赞成功"
            });
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"点赞失败: {ex.Message}");
        return Json(new { success = false, message = ex.Message });
    }
}

        [HttpPost]
        public async Task<IActionResult> Report(int messageId, string reason)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "请先登录" });
            }

            try
            {
                var message = await _context.Messages.FindAsync(messageId);
                if (message == null)
                {
                    return Json(new { success = false, message = "留言不存在" });
                }

                message.ReportCount++;
                message.IsReported = true;
                await _context.SaveChangesAsync();
                await _dataSync.UpdateMessageAsync(message);

                return Json(new { success = true, message = "举报已提交" });
            }
            catch
            {
                return Json(new { success = false, message = "举报失败，请重试" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                return Json(new { success = false, message = "权限不足" });
            }

            await _dataSync.DeleteMessageAsync(id);
            return Json(new { success = true, message = "删除成功" });
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                return Json(new { success = false, message = "权限不足" });
            }

            var message = await _dataSync.GetMessageByIdAsync(id);
            if (message == null)
            {
                return Json(new { success = false, message = "留言不存在" });
            }

            message.IsApproved = true;
            await _dataSync.UpdateMessageAsync(message);

            return Json(new { success = true, message = "审核通过" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int messageId, string replyContent)
        {
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;
            if (isAdmin != 1)
            {
                return Json(new { success = false, message = "权限不足" });
            }

            var message = await _dataSync.GetMessageByIdAsync(messageId);
            if (message == null)
            {
                return Json(new { success = false, message = "留言不存在" });
            }

            message.AdminReply = replyContent;
            message.AdminReplyTime = DateTime.Now;
            await _dataSync.UpdateMessageAsync(message);

            try
            {
                await _emailService.SendReplyNotificationAsync(
                    message.Email,
                    message.VisitorName,
                    message.Content,
                    replyContent
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"回复邮件发送失败: {ex.Message}");
            }

            return Json(new { success = true, message = "回复成功" });
        }
    }
}
