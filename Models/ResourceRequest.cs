using System;

namespace MyPersonalWebsite.Models
{
    public class ResourceRequest
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        
        // 表单内容
        public string PersonName { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
        public string ResourceName { get; set; } = string.Empty;
        public string ResourceUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        // 退款选项
        public string RefundOption { get; set; } = "2weeks_free"; // 1day_paid, 2weeks_free
        public decimal RefundAmount { get; set; } = 0;
        public DateTime? RefundDeadline { get; set; }
        
        // 状态
        public string Status { get; set; } = "pending";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ProcessedAt { get; set; }
        
        // 管理员处理结果
        public string FoundTypes { get; set; } = string.Empty;
        public string NotFoundTypes { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string AdminNote { get; set; } = string.Empty;
        public string AdminName { get; set; } = string.Empty;
        
        // 附加信息
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        
        public User? User { get; set; }
    }
}
