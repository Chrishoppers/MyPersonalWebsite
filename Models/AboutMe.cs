using System;

namespace MyPersonalWebsite.Models
{
    public class AboutMe
    {
        public int Id { get; set; }
        public string SectionKey { get; set; } = string.Empty;  // bio, journey, goal, social
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Icon { get; set; }  // 卡片图标
        public int SortOrder { get; set; } = 0;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
