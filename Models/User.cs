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
        public string? BanNote { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeleteReason { get; set; }
        public string? DeleteNote { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsAvatarApproved { get; set; } = false;
        public DateTime? AvatarSubmittedAt { get; set; }

        // ⭐ 新增：待审核的邮箱和昵称
        public string? PendingEmail { get; set; }
        public string? PendingUsername { get; set; }
        public bool IsEmailChangeApproved { get; set; } = false;
        public bool IsUsernameChangeApproved { get; set; } = false;

        public ICollection<Message>? Messages { get; set; }
    }
}
