using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;

namespace MyPersonalWebsite.Services
{
    public class RateLimitService
    {
        private readonly ConcurrentDictionary<string, RateLimitInfo> _records = new();

        // 检查是否允许注册
        public bool CanRegister(string ip)
        {
            var key = $"{ip}_register";
            if (!_records.TryGetValue(key, out var info))
            {
                _records[key] = new RateLimitInfo
                {
                    Count = 1,
                    FirstAttempt = DateTime.Now,
                    LastAttempt = DateTime.Now
                };
                return true;
            }

            // 1小时内最多3次注册尝试
            if (info.Count >= 3 && DateTime.Now - info.FirstAttempt < TimeSpan.FromHours(1))
            {
                return false;
            }

            // 如果超过1小时，重置计数
            if (DateTime.Now - info.FirstAttempt >= TimeSpan.FromHours(1))
            {
                info.Count = 1;
                info.FirstAttempt = DateTime.Now;
                return true;
            }

            info.Count++;
            info.LastAttempt = DateTime.Now;
            return true;
        }

        // 获取剩余等待时间（分钟）
        public int GetRemainingMinutes(string ip)
        {
            var key = $"{ip}_register";
            if (_records.TryGetValue(key, out var info))
            {
                var elapsed = DateTime.Now - info.FirstAttempt;
                if (elapsed < TimeSpan.FromHours(1))
                {
                    return (int)Math.Ceiling((TimeSpan.FromHours(1) - elapsed).TotalMinutes);
                }
            }
            return 0;
        }

        private class RateLimitInfo
        {
            public int Count { get; set; }
            public DateTime FirstAttempt { get; set; }
            public DateTime LastAttempt { get; set; }
        }
    }
}