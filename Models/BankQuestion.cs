using System;

namespace MyPersonalWebsite.Models
{
    public class BankQuestion
    {
        public int Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string Pinyin { get; set; } = string.Empty;
        public string? Hint { get; set; }
        public int Difficulty { get; set; } = 1;
        public string Category { get; set; } = "综合";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? UsedAt { get; set; }
        public int UseCount { get; set; } = 0;
    }
}
