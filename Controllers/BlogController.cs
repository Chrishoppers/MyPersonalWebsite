using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPersonalWebsite.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MyPersonalWebsite.Controllers
{
    public class BlogController : Controller
    {
        private readonly AppDbContext _context;

        public BlogController(AppDbContext context)
        {
            _context = context;
        }

        // ===== 博客列表 =====
        public IActionResult Index()
        {
            var blogs = _context.Blogs
                .Include(b => b.Likes)
                .OrderByDescending(b => b.PublishDate)
                .ToList();

            var userId = HttpContext.Session.GetInt32("UserId");
            var likedIds = new HashSet<int>();
            if (userId.HasValue)
            {
                var likes = _context.BlogLikes
                    .Where(l => l.UserId == userId.Value)
                    .Select(l => l.BlogId)
                    .ToList();
                likedIds = new HashSet<int>(likes);
            }

            ViewBag.LikedIds = likedIds;
            ViewBag.CurrentUserId = userId;

            return View(blogs);
        }

        // ===== 博客详情 =====
        public IActionResult Details(int id)
        {
            var blog = _context.Blogs
                .Include(b => b.Likes)
                .FirstOrDefault(b => b.Id == id);

            if (blog == null)
            {
                return NotFound();
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            var isLiked = false;
            if (userId.HasValue)
            {
                isLiked = _context.BlogLikes.Any(l => l.BlogId == id && l.UserId == userId.Value);
            }

            ViewBag.IsLiked = isLiked;
            ViewBag.CurrentUserId = userId;

            return View(blog);
        }

        // ===== 点赞/取消点赞 (AJAX) =====
        [HttpPost]
        public async Task<IActionResult> ToggleLike(int blogId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "请先登录" });
            }

            var blog = await _context.Blogs.FindAsync(blogId);
            if (blog == null)
            {
                return Json(new { success = false, message = "博客不存在" });
            }

            var existingLike = await _context.BlogLikes
                .FirstOrDefaultAsync(l => l.BlogId == blogId && l.UserId == userId.Value);

            if (existingLike != null)
            {
                // 取消点赞
                _context.BlogLikes.Remove(existingLike);
                blog.LikeCount--;
                await _context.SaveChangesAsync();
                return Json(new { success = true, isLiked = false, likeCount = blog.LikeCount, message = "已取消点赞" });
            }
            else
            {
                // 点赞
                _context.BlogLikes.Add(new BlogLike
                {
                    BlogId = blogId,
                    UserId = userId.Value,
                    CreateTime = DateTime.Now
                });
                blog.LikeCount++;
                await _context.SaveChangesAsync();
                return Json(new { success = true, isLiked = true, likeCount = blog.LikeCount, message = "点赞成功" });
            }
        }
    }
}