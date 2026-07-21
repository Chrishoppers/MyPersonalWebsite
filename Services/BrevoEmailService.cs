using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyPersonalWebsite.Services
{
    public class BrevoEmailService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly EmailRateLimitService _rateLimitService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BrevoEmailService(
            HttpClient httpClient,
            EmailRateLimitService rateLimitService,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _apiKey = Environment.GetEnvironmentVariable("BREVO_API_KEY") ?? "";
            _rateLimitService = rateLimitService;
            _httpContextAccessor = httpContextAccessor;
        }

        // ============================================================
        // 私有方法
        // ============================================================

        private int? GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.Session.GetInt32("UserId");
        }

        private bool IsAdmin()
        {
            return (_httpContextAccessor.HttpContext?.Session.GetInt32("IsAdmin") ?? 0) == 1;
        }

        // ============================================================
        // 受限制的邮件发送（普通用户每天8封）
        // ============================================================

        public async Task<(bool Success, string Message)> SendEmailWithLimitAsync(string to, string subject, string htmlContent, string type)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return (false, "请先登录");
            }

            var isAdmin = IsAdmin();
            var (canSend, message, remaining) = await _rateLimitService.CanSendEmailAsync(userId.Value, isAdmin);

            if (!canSend)
            {
                return (false, message);
            }

            try
            {
                var request = new
                {
                    sender = new { email = "hello@chris-hopper.org", name = "Chris Hopper 个人网站" },
                    to = new[] { new { email = to } },
                    subject = subject,
                    htmlContent = htmlContent
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);

                var response = await _httpClient.PostAsync("https://api.brevo.com/v3/smtp/email", content);
                var isSuccess = response.IsSuccessStatusCode;

                await _rateLimitService.LogEmailAsync(userId.Value, to, type, isSuccess, isSuccess ? null : response.StatusCode.ToString());

                if (isSuccess)
                {
                    return (true, $"✅ 邮件发送成功（今日剩余 {remaining - 1} 封）");
                }
                else
                {
                    return (false, "邮件发送失败，请稍后重试");
                }
            }
            catch (Exception ex)
            {
                await _rateLimitService.LogEmailAsync(userId.Value, to, type, false, ex.Message);
                return (false, "邮件发送失败，请稍后重试");
            }
        }

        // ============================================================
        // 不受限制的邮件发送（管理员通知）
        // ============================================================

        public async Task<bool> SendEmailAsync(string to, string subject, string htmlContent)
        {
            try
            {
                var request = new
                {
                    sender = new { email = "hello@chris-hopper.org", name = "Chris Hopper 个人网站" },
                    to = new[] { new { email = to } },
                    subject = subject,
                    htmlContent = htmlContent
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);

                var response = await _httpClient.PostAsync("https://api.brevo.com/v3/smtp/email", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // ============================================================
        // 邮件模板（普通用户，受限制）
        // ============================================================

        public async Task SendVerificationCodeAsync(string toEmail, string code)
        {
            var html = $@"
                <div style='font-family: Arial; max-width: 600px; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                    <h2 style='color: #0D6EFD;'>✌️ 嘿，是你吗？</h2>
                    <p>有人在 <strong>Chris Hopper 的个人网站</strong> 用这个邮箱注册了账号。</p>
                    <p>如果是你，请用这个验证码完成注册：</p>
                    <div style='background: #f0f4ff; padding: 15px; text-align: center; font-size: 32px; letter-spacing: 8px; font-weight: bold; color: #0D6EFD;'>
                        {code}
                    </div>
                    <p style='color: #888; font-size: 14px;'>⏳ 10 分钟内有效，过期要重新申请哦。</p>
                    <p style='color: #888; font-size: 14px;'>🙅‍♂️ 如果不是你操作的？那直接忽略就好，不用管它。</p>
                    <hr>
                    <p style='color: #aaa; font-size: 12px;'>💌 系统自动发送，不用回复。</p>
                </div>
            ";

            var result = await SendEmailWithLimitAsync(toEmail, "【Chris Hopper 个人网站】嘿，验证码来啦 ✌️", html, "verification");
            if (!result.Success)
            {
                throw new Exception(result.Message);
            }
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string code)
        {
            var html = $@"
                <div style='font-family: Arial; max-width: 600px; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                    <h2 style='color: #0D6EFD;'>🔑 密码重置请求</h2>
                    <p>有人在 <strong>Chris Hopper 的个人网站</strong> 申请重置密码。</p>
                    <p>如果是你，用这个验证码重置：</p>
                    <div style='background: #f0f4ff; padding: 15px; text-align: center; font-size: 32px; letter-spacing: 8px; font-weight: bold; color: #0D6EFD;'>
                        {code}
                    </div>
                    <p style='color: #888; font-size: 14px;'>⏳ 10 分钟内有效，过期再来一次就行。</p>
                    <p style='color: #888; font-size: 14px;'>🙅‍♂️ 如果这不是你操作的？直接忽略，密码不会变。</p>
                    <hr>
                    <p style='color: #aaa; font-size: 12px;'>💌 系统自动发送，不用回复。</p>
                </div>
            ";

            var result = await SendEmailWithLimitAsync(toEmail, "【Chris Hopper 个人网站】密码重置验证码 🔑", html, "reset");
            if (!result.Success)
            {
                throw new Exception(result.Message);
            }
        }

        public async Task SendReplyNotificationAsync(string toEmail, string userName, string originalContent, string replyContent)
        {
            var html = $@"
                <div style='font-family: Arial; max-width: 600px; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                    <h2 style='color: #0D6EFD;'>💬 你的留言被回复了</h2>
                    <p>你好 <strong>{userName}</strong>！</p>
                    <p>你在留言板上的留言收到了管理员的回复：</p>
                    <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; margin: 10px 0;'>
                        <p><strong>你的留言：</strong>{originalContent}</p>
                        <hr>
                        <p><strong>管理员回复：</strong>{replyContent}</p>
                        <p style='color: #888; font-size: 14px;'>回复时间：{DateTime.Now:yyyy-MM-dd HH:mm}</p>
                    </div>
                    <a href='https://chris-hopper.org/Message/Index'>查看留言板</a>
                    <hr>
                    <p style='color: #aaa; font-size: 12px;'>💌 系统自动发送，不用回复。</p>
                </div>
            ";

            var result = await SendEmailWithLimitAsync(toEmail, "【Chris Hopper 个人网站】你的留言收到了回复 💬", html, "reply");
            if (!result.Success)
            {
                throw new Exception(result.Message);
            }
        }

        // ============================================================
        // 管理员通知（不受限制）
        // ============================================================

        public async Task SendAdminNewMessageNotificationAsync(string visitorName, string content, int messageId)
        {
            var html = $@"
                <div style='font-family: Arial; max-width: 600px; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                    <h2 style='color: #0D6EFD;'>📝 新留言待审核</h2>
                    <p>有一条新留言需要审核：</p>
                    <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; margin: 10px 0;'>
                        <p><strong>留言者：</strong>{visitorName}</p>
                        <p><strong>内容：</strong>{content}</p>
                        <p><strong>时间：</strong>{DateTime.Now:yyyy-MM-dd HH:mm}</p>
                    </div>
                    <a href='https://chris-hopper.org/Admin/Messages'>点击审核</a>
                    <hr>
                    <p style='color: #aaa; font-size: 12px;'>此邮件由系统自动发送，请勿直接回复。</p>
                </div>
            ";

            await SendEmailAsync("2908685235@qq.com", "📝 新留言待审核", html);
        }

        public async Task SendAdminNewUserNotificationAsync(string username, string email)
        {
            var html = $@"
                <div style='font-family: Arial; max-width: 600px; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                    <h2 style='color: #28a745;'>👤 新用户注册</h2>
                    <p>有新用户注册了：</p>
                    <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; margin: 10px 0;'>
                        <p><strong>用户名：</strong>{username}</p>
                        <p><strong>邮箱：</strong>{email}</p>
                        <p><strong>时间：</strong>{DateTime.Now:yyyy-MM-dd HH:mm}</p>
                    </div>
                    <hr>
                    <p style='color: #aaa; font-size: 12px;'>此邮件由系统自动发送，请勿直接回复。</p>
                </div>
            ";

            await SendEmailAsync("2908685235@qq.com", "👤 新用户注册", html);
        }

        public async Task SendAdminNewBlogNotificationAsync(string blogTitle)
        {
            var html = $@"
                <div style='font-family: Arial; max-width: 600px; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                    <h2 style='color: #4facfe;'>📖 新博客发布</h2>
                    <p>新博客已发布：<strong>{blogTitle}</strong></p>
                </div>
            ";

            await SendEmailAsync("2908685235@qq.com", "📖 新博客发布", html);
        }

        public async Task SendAdminNewContactRequestNotificationAsync(string identity, string platform, string authCode, string howKnowMe, string relationship, string username, string userEmail)
        {
            var html = $@"
                <div style='font-family: Arial; max-width: 600px; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                    <h2 style='color: #a855f7;'>🔑 新授权码申请</h2>
                    <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; margin: 10px 0;'>
                        <p><strong>申请人：</strong>{identity}</p>
                        <p><strong>平台：</strong>{platform}</p>
                        <p><strong>授权码：</strong>{authCode}</p>
                        <p><strong>用户名：</strong>{username}</p>
                        <p><strong>邮箱：</strong>{userEmail}</p>
                    </div>
                    <hr>
                    <p style='color: #aaa; font-size: 12px;'>此邮件由系统自动发送，请勿直接回复。</p>
                </div>
            ";

            await SendEmailAsync("2908685235@qq.com", "🔑 新授权码申请", html);
        }

        public async Task SendUserActionNotificationAsync(string toEmail, string username, string actionType, string reason, string note)
        {
            var actionMap = new Dictionary<string, string>
            {
                { "ban", "封禁" },
                { "unban", "解封" },
                { "delete", "删除账号" }
            };

            var actionName = actionMap.ContainsKey(actionType) ? actionMap[actionType] : actionType;

            var html = $@"
                <div style='font-family: Arial; max-width: 600px; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                    <h2 style='color: #dc3545;'>⚠️ 账号通知</h2>
                    <p>您好 <strong>{username}</strong>！</p>
                    <p>您的账号在 <strong>Chris Hopper 个人网站</strong> 已被管理员 <strong>{actionName}</strong>。</p>
                    <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; margin: 10px 0;'>
                        <p><strong>📌 原因：</strong>{reason}</p>
                        <p><strong>📝 备注：</strong>{note ?? "无"}</p>
                        <p><strong>⏰ 时间：</strong>{DateTime.Now:yyyy-MM-dd HH:mm}</p>
                    </div>
                    <p style='color: #888; font-size: 14px;'>如有疑问，请联系管理员。</p>
                    <hr>
                    <p style='color: #aaa; font-size: 12px;'>💌 系统自动发送，不用回复。</p>
                </div>
            ";

            await SendEmailAsync(toEmail, $"【Chris Hopper 个人网站】账号{actionName}通知", html);
        }
    }
}
