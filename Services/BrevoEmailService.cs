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
                    <p style='color: #888; font-size: 14px;'>⏳ 10 分钟内有效。</p>
                    <hr>
                    <p style='color: #aaa; font-size: 12px;'>💌 系统自动发送，不用回复。</p>
                </div>
            ";

            await SendEmailAsync(toEmail, "【Chris Hopper 个人网站】密码重置验证码", html);
        }

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
                    <p style='color: #888; font-size: 14px;'>⏳ 10 分钟内有效。</p>
                    <hr>
                    <p style='color: #aaa; font-size: 12px;'>💌 系统自动发送，不用回复。</p>
                </div>
            ";

            await SendEmailAsync(toEmail, "【Chris Hopper 个人网站】邮箱验证码", html);
        }

        public async Task SendReplyNotificationAsync(string toEmail, string userName, string originalContent, string replyContent)
        {
            // ... 类似实现
        }

        public async Task SendAdminNewMessageNotificationAsync(string visitorName, string content, int messageId)
        {
            // ... 类似实现
        }

        public async Task SendAdminNewUserNotificationAsync(string username, string email)
        {
            // ... 类似实现
        }

        public async Task SendAdminNewBlogNotificationAsync(string blogTitle)
        {
            // ... 类似实现
        }

        public async Task SendUserActionNotificationAsync(string toEmail, string username, string actionType, string reason, string note)
        {
            // ... 类似实现
        }
    }
}
