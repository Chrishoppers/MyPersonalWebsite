using System;

namespace MyPersonalWebsite.Models
{
    public class MessageLike
    {
        public int Id { get; set; }
        public int MessageId { get; set; }
        public int UserId { get; set; }
        public DateTime CreateTime { get; set; } = DateTime.Now;

        public Message? Message { get; set; }
        public User? User { get; set; }
    }
}
