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
        private readonly AppDbContext _context;

        public BlogController(DataSyncService dataSync, AppDbContext context)
        {
            _dataSync = dataSync;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var blogs = await _dataSync.GetBlogsAsync();

            var userId = HttpContext.Session.GetInt32("UserId");
            var likedIds = new HashSet<int>();

            if (userId.HasValue)
            {
                var likes = await _context.BlogLikes
                    .Where(l => l.UserId == userId.Value)
                    .Select(l => l.BlogId)
                    .ToListAsync();
                likedIds = new HashSet<int>(likes);
            }

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

            if (userId.HasValue)
            {
                isLiked = await _context.BlogLikes
                    .AnyAsync(l => l.BlogId == id && l.UserId == userId.Value);
            }

            ViewBag.IsLiked = isLiked;
            ViewBag.CurrentUserId = userId;

            return View(blog);
        }

        // ============================================================
        // ⭐ 博客点赞（实时更新 + 双写）
        // ============================================================
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
                var blog = await _context.Blogs.FindAsync(blogId);
                if (blog == null)
                {
                    return Json(new { success = false, message = "博客不存在" });
                }

                // 检查是否已点赞
                var existingLike = await _context.BlogLikes
                    .FirstOrDefaultAsync(l => l.BlogId == blogId && l.UserId == userId.Value);

                if (existingLike != null)
                {
                    // 取消点赞
                    _context.BlogLikes.Remove(existingLike);
                    blog.LikeCount--;
                    await _context.SaveChangesAsync();

                    // 同步到 Turso
                    await _dataSync.UpdateBlogAsync(blog);
                    await _dataSync.DeleteBlogLikeAsync(blogId, userId.Value);

                    return Json(new
                    {
                        success = true,
                        isLiked = false,
                        likeCount = blog.LikeCount,
                        message = "已取消点赞"
                    });
                }
                else
                {
                    // 点赞
                    var like = new BlogLike
                    {
                        BlogId = blogId,
                        UserId = userId.Value,
                        CreateTime = DateTime.Now
                    };
                    _context.BlogLikes.Add(like);
                    blog.LikeCount++;
                    await _context.SaveChangesAsync();

                    // 同步到 Turso
                    await _dataSync.UpdateBlogAsync(blog);
                    await _dataSync.AddBlogLikeAsync(blogId, userId.Value);

                    return Json(new
                    {
                        success = true,
                        isLiked = true,
                        likeCount = blog.LikeCount,
                        message = "点赞成功"
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
