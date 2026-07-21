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

        public BrevoEmailService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _apiKey = Environment.GetEnvironmentVariable("BREVO_API_KEY") ?? "";
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string htmlContent)
        {
            try
            {
                var request = new
                {
                    sender = new { email = "2908685235@qq.com", name = "Chris Hopper 个人网站" },
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

        public async Task SendPasswordResetEmailAsync(string toEmail, string code)
        {
            var html = $@"
                <div style='font-family: Arial; max-width: 600px; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                    <h2 style='color: #0D6EFD;'>Chris Hopper 个人网站</h2>
                    <p>您正在重置密码，请使用以下验证码：</p>
                    <div style='background: #f0f4ff; padding: 15px; text-align: center; font-size: 32px; letter-spacing: 8px; font-weight: bold; color: #0D6EFD;'>
                        {code}
                    </div>
                    <p style='color: #888; font-size: 14px;'>验证码有效期为 10 分钟。</p>
                </div>
            ";

            await SendEmailAsync(toEmail, "【Chris Hopper 个人网站】重置密码验证码", html);
        }

        public async Task SendVerificationCodeAsync(string toEmail, string code)
        {
            var html = $@"
                <div style='font-family: Arial; max-width: 600px; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                    <h2 style='color: #0D6EFD;'>Chris Hopper 个人网站</h2>
                    <p>您正在注册，请使用以下验证码：</p>
                    <div style='background: #f0f4ff; padding: 15px; text-align: center; font-size: 32px; letter-spacing: 8px; font-weight: bold; color: #0D6EFD;'>
                        {code}
                    </div>
                    <p style='color: #888; font-size: 14px;'>验证码有效期为 10 分钟。</p>
                </div>
            ";

            await SendEmailAsync(toEmail, "【Chris Hopper 个人网站】邮箱验证码", html);
        }

        public async Task SendReplyNotificationAsync(string toEmail, string userName, string originalContent, string replyContent)
        {
            var html = $@"
                <div style='font-family: Arial; max-width: 600px; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                    <h2 style='color: #0D6EFD;'>Chris Hopper 个人网站</h2>
                    <p>您好 <strong>{userName}</strong>！</p>
                    <p>您的留言收到了回复：</p>
                    <div style='background: #f8f9fa; padding: 15px; border-radius: 8px;'>
                        <p><strong>您的留言：</strong>{originalContent}</p>
                        <hr>
                        <p><strong>管理员回复：</strong>{replyContent}</p>
                    </div>
                </div>
            ";

            await SendEmailAsync(toEmail, "【Chris Hopper 个人网站】您的留言收到了回复", html);
        }

        public async Task SendAdminNewMessageNotificationAsync(string visitorName, string content, int messageId)
        {
            var html = $@"
                <div style='font-family: Arial; max-width: 600px; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                    <h2 style='color: #0D6EFD;'>📝 新留言待审核</h2>
                    <p>有一条新留言需要审核：</p>
                    <div style='background: #f8f9fa; padding: 15px; border-radius: 8px;'>
                        <p><strong>留言者：</strong>{visitorName}</p>
                        <p><strong>内容：</strong>{content}</p>
                        <p><strong>时间：</strong>{DateTime.Now:yyyy-MM-dd HH:mm}</p>
                    </div>
                    <p><a href='https://chris-hopper.org/Admin/Messages'>点击审核</a></p>
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
                    <div style='background: #f8f9fa; padding: 15px; border-radius: 8px;'>
                        <p><strong>用户名：</strong>{username}</p>
                        <p><strong>邮箱：</strong>{email}</p>
                        <p><strong>时间：</strong>{DateTime.Now:yyyy-MM-dd HH:mm}</p>
                    </div>
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
                    <div style='background: #f8f9fa; padding: 15px; border-radius: 8px;'>
                        <p><strong>申请人：</strong>{identity}</p>
                        <p><strong>平台：</strong>{platform}</p>
                        <p><strong>授权码：</strong>{authCode}</p>
                        <p><strong>用户名：</strong>{username}</p>
                        <p><strong>邮箱：</strong>{userEmail}</p>
                    </div>
                </div>
            ";

            await SendEmailAsync("2908685235@qq.com", "🔑 新授权码申请", html);
        }
    }
}
