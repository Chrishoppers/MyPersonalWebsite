using System;

namespace MyPersonalWebsite.Models
{
    public class ContactRequest
    {
        public int Id { get; set; }
        public string Platform { get; set; } = string.Empty;      // WeChat 或 QQ
        public string AuthorizationCode { get; set; } = string.Empty;

        // 申请人信息
        public string HowKnowMe { get; set; } = string.Empty;
        public string Identity { get; set; } = string.Empty;
        public string Relationship { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;

        // ⭐ 关联用户
        public int UserId { get; set; }                            // 申请人用户ID
        public string Username { get; set; } = string.Empty;       // 申请人用户名
        public string UserEmail { get; set; } = string.Empty;      // 申请人邮箱

        public DateTime RequestTime { get; set; } = DateTime.Now;

        // 状态字段
        public bool IsApproved { get; set; } = false;              // 是否已被查看
        public DateTime? ViewTime { get; set; }
        public bool IsUsed { get; set; } = false;
        public DateTime? UsedTime { get; set; }
        public string? UsedBy { get; set; }

        // 关联
        public User? User { get; set; }
    }
}