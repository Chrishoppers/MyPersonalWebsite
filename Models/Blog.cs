using System;
using System.Collections.Generic;

namespace MyPersonalWebsite.Models
{
    public class Blog
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public DateTime PublishDate { get; set; } = DateTime.Now;
        public string? CoverImageUrl { get; set; }

        // ⭐ 新增：点赞功能
        public int LikeCount { get; set; } = 0;
        public ICollection<BlogLike>? Likes { get; set; }
    }

    // ⭐ 博客点赞记录表
    public class BlogLike
    {
        public int Id { get; set; }
        public int BlogId { get; set; }
        public int UserId { get; set; }
        public DateTime CreateTime { get; set; } = DateTime.Now;

        public Blog? Blog { get; set; }
        public User? User { get; set; }
    }
}