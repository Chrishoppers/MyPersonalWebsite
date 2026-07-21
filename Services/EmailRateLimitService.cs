using System;
using System.Threading.Tasks;

namespace MyPersonalWebsite.Services
{
    public class EmailRateLimitService
    {
        public async Task<(bool IsAllowed, string Message, int Remaining)> CanSendEmailAsync(int userId, bool isAdmin)
        {
            // 管理员不限流
            if (isAdmin)
            {
                return await Task.FromResult((true, "管理员不限流", 999));
            }

            // 暂不实现限流，直接放行
            return await Task.FromResult((true, "已发送", 8));
        }

        public async Task LogEmailAsync(int userId, string email, string type, bool isSuccess, string? errorMessage = null)
        {
            // 暂不实现日志
            await Task.CompletedTask;
        }
    }
}
