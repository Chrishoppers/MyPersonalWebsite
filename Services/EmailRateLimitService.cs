using Microsoft.EntityFrameworkCore;
using MyPersonalWebsite.Models;
using System;
using System.Threading.Tasks;

namespace MyPersonalWebsite.Services
{
    public class EmailRateLimitService
    {
        private readonly AppDbContext _context;
        private readonly int _dailyLimit = 8; // 每天最多8封
        private readonly int _adminDailyLimit = 100; // 管理员每天最多100封

        public EmailRateLimitService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(bool IsAllowed, string Message, int Remaining)> CanSendEmailAsync(int userId, bool isAdmin)
        {
            var limit = isAdmin ? _adminDailyLimit : _dailyLimit;

            var today = DateTime.Today;
            var todayCount = await _context.EmailLogs
                .CountAsync(l => l.UserId == userId && l.SentAt >= today && l.SentAt < today.AddDays(1));

            if (todayCount >= limit)
            {
                var remaining = 0;
                var message = isAdmin 
                    ? $"管理员每日邮件配额已用完（{limit}封/天）"
                    : $"⚠️ 今日邮件发送已达上限（{limit}封/天），请明天再试";

                return (false, message, remaining);
            }

            var remainingCount = limit - todayCount;
            return (true, $"今日剩余 {remainingCount} 封", remainingCount);
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
