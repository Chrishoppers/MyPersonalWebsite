using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPersonalWebsite.Models;
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

        public async Task<IActionResult> Index()
        {
            var messages = await _context.Messages
                .OrderByDescending(m => m.CreateTime)
                .ToListAsync();

            ViewBag.CurrentUserId = HttpContext.Session.GetInt32("UserId");
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
                message.IsApproved = true;

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                TempData["Success"] = "留言发布成功！";
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
                var message = await _context.Messages.FindAsync(messageId);
                if (message == null)
                {
                    return Json(new { success = false, message = "留言不存在" });
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
