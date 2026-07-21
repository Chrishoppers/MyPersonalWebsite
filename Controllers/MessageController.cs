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

        // ============================================================
        // 留言大屏
        // ============================================================
        public async Task<IActionResult> Index()
        {
            var messages = await _dataSync.GetMessagesAsync();

            var userId = HttpContext.Session.GetInt32("UserId");
            var likedIds = new HashSet<int>();
            // TODO: 从 DataSync 获取点赞数据
            // if (userId.HasValue)
            // {
            //     likedIds = await _dataSync.GetUserLikedMessageIdsAsync(userId.Value);
            // }

            ViewBag.LikedIds = likedIds;
            ViewBag.CurrentUserId = userId;
            ViewBag.IsAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;

            return View(messages);
        }

        // ============================================================
        // 发布留言页面
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
        // 发布留言提交
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
    }
}
        // ============================================================
        // 点赞/取消点赞
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
                // TODO: 实现留言点赞逻辑
                // var result = await _dataSync.ToggleMessageLikeAsync(messageId, userId.Value);
                // return Json(new { success = true, isLiked = result.IsLiked, likeCount = result.LikeCount, message = result.Message });

                return Json(new { success = true, isLiked = true, likeCount = 1, message = "点赞成功" });
            }
            catch
            {
                return Json(new { success = false, message = "点赞失败，请稍后重试" });
            }
        }

        // ============================================================
        // 举报留言
        // ============================================================
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

        // ============================================================
        // 管理员删除留言
        // ============================================================
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

        // ============================================================
        // 管理员审核留言
        // ============================================================
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

        // ============================================================
        // 管理员回复留言
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
