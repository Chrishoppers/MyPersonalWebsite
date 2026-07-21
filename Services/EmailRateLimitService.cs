using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace MyPersonalWebsite.Services
{
    public class EmailRateLimitService
    {
        private readonly IMemoryCache _cache;
        private readonly int _dailyLimit = 8;

        public EmailRateLimitService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public Task<(bool IsAllowed, string Message, int Remaining)> CanSendEmailAsync(int userId, bool isAdmin)
        {
            if (isAdmin)
            {
                return Task.FromResult((true, "管理员不限流", 999));
            }

            var key = $"email_limit_{userId}_{DateTime.Today:yyyyMMdd}";
            var todayCount = _cache.TryGetValue(key, out int count) ? count : 0;

            if (todayCount >= _dailyLimit)
            {
                return Task.FromResult((false, $"⚠️ 今日邮件已发 {_dailyLimit} 封，已达上限，请明天再试", 0));
            }

            var remaining = _dailyLimit - todayCount;
            return Task.FromResult((true, $"今日剩余 {remaining} 封", remaining));
        }

        public Task LogEmailAsync(int userId, string email, string type, bool isSuccess, string? errorMessage = null)
        {
            var key = $"email_limit_{userId}_{DateTime.Today:yyyyMMdd}";
            var todayCount = _cache.TryGetValue(key, out int count) ? count : 0;
            _cache.Set(key, todayCount + 1, DateTimeOffset.Now.AddDays(1));
            return Task.CompletedTask;
        }
    }
}
