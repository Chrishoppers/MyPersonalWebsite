using System;
using System.Collections.Generic;

namespace MyPersonalWebsite.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? VerificationCode { get; set; }
        public DateTime? VerificationCodeExpiry { get; set; }
        public bool IsEmailVerified { get; set; } = false;
        public bool IsAdmin { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastLoginAt { get; set; }
        public bool IsBanned { get; set; } = false;
        public DateTime? BanExpiry { get; set; }
        public string? BanReason { get; set; }

        // ⭐ 头像
        public string? AvatarUrl { get; set; }

        // 关联
        public ICollection<Message>? Messages { get; set; }
        public ICollection<MessageLike>? Likes { get; set; }
    }
}
