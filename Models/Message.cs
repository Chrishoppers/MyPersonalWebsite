using System;
using System.Collections.Generic;

namespace MyPersonalWebsite.Models
{
    public class Message
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string VisitorName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; } = DateTime.Now;
        public bool IsApproved { get; set; } = false;
        public int LikeCount { get; set; } = 0;
        public string? AdminReply { get; set; }
        public DateTime? AdminReplyTime { get; set; }
        public int ReportCount { get; set; } = 0;
        public bool IsReported { get; set; } = false;

        // 关联
        public User? User { get; set; }
        public ICollection<MessageLike>? Likes { get; set; }
    }

    public class MessageLike
    {
        public int Id { get; set; }
        public int MessageId { get; set; }
        public int UserId { get; set; }
        public DateTime CreateTime { get; set; } = DateTime.Now;

        public Message? Message { get; set; }
        public User? User { get; set; }
    }

    public class ReportRecord
    {
        public int Id { get; set; }
        public int MessageId { get; set; }
        public int UserId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; } = DateTime.Now;

        public Message? Message { get; set; }
        public User? User { get; set; }
    }
}
