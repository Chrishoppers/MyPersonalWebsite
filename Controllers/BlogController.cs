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
    public class BlogController : Controller
    {
        private readonly DataSyncService _dataSync;

        public BlogController(DataSyncService dataSync)
        {
            _dataSync = dataSync;
        }

        public async Task<IActionResult> Index()
        {
            var blogs = await _dataSync.GetBlogsAsync();

            var userId = HttpContext.Session.GetInt32("UserId");
            var likedIds = new HashSet<int>();

            ViewBag.LikedIds = likedIds;
            ViewBag.CurrentUserId = userId;

            return View(blogs);
        }

        public async Task<IActionResult> Details(int id)
        {
            var blog = await _dataSync.GetBlogByIdAsync(id);
            if (blog == null)
            {
                return NotFound();
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            var isLiked = false;

            ViewBag.IsLiked = isLiked;
            ViewBag.CurrentUserId = userId;

            return View(blog);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleLike(int blogId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "请先登录" });
            }

            try
            {
                // TODO: 实现博客点赞逻辑
                return Json(new { success = true, isLiked = true, likeCount = 1, message = "点赞成功" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
