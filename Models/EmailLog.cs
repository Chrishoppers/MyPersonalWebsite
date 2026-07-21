using System;

namespace MyPersonalWebsite.Models
{
    public class EmailLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.Now;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }

        public User? User { get; set; }
    }
}
