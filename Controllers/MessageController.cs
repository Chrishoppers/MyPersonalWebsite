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
        private readonly AppDbContext _context;

        public MessageController(AppDbContext context)
        {
            _context = context;
        }

        // ===== 留言大屏 =====
        public async Task<IActionResult> Index()
        {
            var messages = await _context.Messages
                .OrderByDescending(m => m.CreateTime)
                .ToListAsync();

            ViewBag.CurrentUserId = HttpContext.Session.GetInt32("UserId");
            ViewBag.IsAdmin = HttpContext.Session.GetInt32("IsAdmin") ?? 0;

            return View(messages);
        }

        // ===== 发布留言页面 =====
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

        // ===== 发布留言提交 =====
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
            if (user == null)
            {
                ModelState.AddModelError("", "用户不存在");
                return View();
            }

            if (user.IsBanned)
            {
                ModelState.AddModelError("", "您的账号已被封禁");
                return View();
            }

            if (ModelState.IsValid)
            {
                message.UserId = userId.Value;
                message.VisitorName = user.Username;
                message.Email = user.Email;
                message.CreateTime = DateTime.Now;
                message.LikeCount = 0;
                message.IsApproved = true;

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                TempData["Success"] = "留言发布成功！";
                return RedirectToAction("Index");
            }

            return View(message);
        }

        // ===== 点赞 =====
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
                var message = await _context.Messages.FindAsync(messageId);
                if (message == null)
                {
                    return Json(new { success = false, message = "留言不存在" });
                }

                if (message.UserId == userId.Value)
                {
                    return Json(new { success = false, message = "不能给自己的留言点赞" });
                }

                message.LikeCount++;
                await _context.SaveChangesAsync();

                return Json(new { success = true, likeCount = message.LikeCount });
            }
            catch
            {
                return Json(new { success = false, message = "点赞失败" });
            }
        }

        // ===== 管理员删除 =====
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
    }
}
