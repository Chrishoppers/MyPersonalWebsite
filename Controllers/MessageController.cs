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

        public MessageController(DataSyncService dataSync, BrevoEmailService emailService)
        {
            _dataSync = dataSync;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index()
        {
            var messages = await _dataSync.GetMessagesAsync();

            var userId = HttpContext.Session.GetInt32("UserId");
            var likedIds = new HashSet<int>();

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

                await _dataSync.AddMessageAsync(message);

                if (!user.IsAdmin)
                {
                    try
                    {
                        await _emailService.SendAdminNewMessageNotificationAsync(
                            message.VisitorName,
                            message.Content,
                            0
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
                // TODO: 实现留言点赞逻辑
                return Json(new { success = true, isLiked = true, likeCount = 1, message = "点赞成功" });
            }
            catch
            {
                return Json(new { success = false, message = "点赞失败，请稍后重试" });
            }
        }
        // ============================================================
// 获取弹幕数据（API）
// ============================================================
[HttpGet]
public async Task<IActionResult> GetDanmakuData()
{
    try
    {
        var messages = await _dataSync.GetMessagesAsync();
        // 只返回已审核的留言
        var approved = messages.Where(m => m.IsApproved).ToList();
        return Json(new 
        { 
            success = true, 
            messages = approved.Select(m => new
            {
                id = m.Id,
                userId = m.UserId,
                visitorName = m.VisitorName,
                email = m.Email,
                content = m.Content,
                createTime = m.CreateTime,
                likeCount = m.LikeCount,
                isReported = m.IsReported
            })
        });
    }
    catch (Exception ex)
    {
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

            // TODO: 实现举报逻辑
            return Json(new { success = true, message = "举报已提交" });
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
