using System;

namespace MyPersonalWebsite.Models
{
    public class PasswordReset
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; } = false;
        public string Email { get; set; } = string.Empty;
    }
}
