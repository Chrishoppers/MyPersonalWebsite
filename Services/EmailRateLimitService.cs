using Microsoft.EntityFrameworkCore;
using MyPersonalWebsite.Models;
using System;
using System.Threading.Tasks;

namespace MyPersonalWebsite.Services
{
    public class EmailRateLimitService
    {
        private readonly AppDbContext _context;

        public EmailRateLimitService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(bool IsAllowed, string Message, int Remaining)> CanSendEmailAsync(int userId, bool isAdmin)
        {
            if (isAdmin)
            {
                return (true, "管理员不限流", 999);
            }

            var today = DateTime.Today;
            var todayCount = await _context.EmailLogs
                .CountAsync(l => l.UserId == userId && l.SentAt >= today && l.SentAt < today.AddDays(1));

            var limit = 8;

            if (todayCount >= limit)
            {
                return (false, $"⚠️ 今日邮件已发 {limit} 封，已达上限，请明天再试", 0);
            }

            return (true, $"今日剩余 {limit - todayCount} 封", limit - todayCount);
        }

        public async Task LogEmailAsync(int userId, string email, string type, bool isSuccess, string? errorMessage = null)
        {
            var log = new EmailLog
            {
                UserId = userId,
                Email = email,
                Type = type,
                SentAt = DateTime.Now,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage
            };

            _context.EmailLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
