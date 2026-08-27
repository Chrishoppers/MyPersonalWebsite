using System;

namespace MyPersonalWebsite.Models
{
    /// <summary>
    /// 用户行为日志
    /// </summary>
    public class UserActivityLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        
        // 行为类型
        public string ActionType { get; set; } = string.Empty; 
        // Login, Logout, Register, SubmitResource, ViewResource, 
        // PostMessage, PostBlog, Like, Comment, Report, Ban, Unban, Delete
        
        public string ActionCategory { get; set; } = string.Empty;
        // Auth, Resource, Message, Blog, Game, Admin, System
        
        public string Description { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty; // JSON格式详细信息
        
        // 请求信息
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? RequestPath { get; set; }
        public string? Referer { get; set; }
        
        // 关联ID（如资源ID、留言ID等）
        public int? TargetId { get; set; }
        public string? TargetType { get; set; }
        
        // 状态
        public string Status { get; set; } = "success"; // success, failed, warning
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // 导航属性
        public User? User { get; set; }
    }

    /// <summary>
    /// 用户行为统计
    /// </summary>
    public class UserActivityStats
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public int TotalActions { get; set; }
        public int LoginCount { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime? LastActionAt { get; set; }
        public int ResourceSubmissions { get; set; }
        public int MessagesPosted { get; set; }
        public int BlogsPosted { get; set; }
        public int ReportsSubmitted { get; set; }
        public int ViolationCount { get; set; }
        public int WarningCount { get; set; }
        public string RiskLevel { get; set; } = "low"; // low, medium, high, critical
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
