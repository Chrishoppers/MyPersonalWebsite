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
                    <h2 style='color: #0D6EFD;'>Chris Hopper 个人网站</h2>
                    <p>您好！</p>
                    <p>您正在重置密码，请使用以下验证码：</p>
                    <div style='background: #f0f4ff; padding: 15px; text-align: center; font-size: 32px; letter-spacing: 8px; font-weight: bold; color: #0D6EFD;'>
                        {code}
                    </div>
                    <p style='color: #888; font-size: 14px;'>验证码有效期为 <strong>10 分钟</strong>。</p>
                    <p style='color: #888; font-size: 14px;'>如果这不是您的操作，请忽略此邮件。</p>
                    <hr style='border: none; border-top: 1px solid #eee;'>
                    <p style='color: #aaa; font-size: 12px;'>此邮件由系统自动发送，请勿直接回复。</p>
                </div>
            ";

            await SendEmailAsync(toEmail, "【Chris Hopper 个人网站】重置密码验证码", html);
        }

        public async Task SendVerificationCodeAsync(string toEmail, string code)
        {
            var html = $@"
                <div style='font-family: Arial; max-width: 600px; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                    <h2 style='color: #0D6EFD;'>Chris Hopper 个人网站</h2>
                    <p>您好！</p>
                    <p>您正在注册账号，请使用以下验证码完成邮箱验证：</p>
                    <div style='background: #f0f4ff; padding: 15px; text-align: center; font-size: 32px; letter-spacing: 8px; font-weight: bold; color: #0D6EFD;'>
                        {code}
                    </div>
                    <p style='color: #888; font-size: 14px;'>验证码有效期为 <strong>10 分钟</strong>。</p>
                    <p style='color: #888; font-size: 14px;'>如果这不是您的操作，请忽略此邮件。</p>
                    <hr style='border: none; border-top: 1px solid #eee;'>
                    <p style='color: #aaa; font-size: 12px;'>此邮件由系统自动发送，请勿直接回复。</p>
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
                    <p>您在留言板上的留言收到了管理员的回复：</p>
                    <div style='background-color: #f8f9fa; padding: 15px; border-radius: 8px; margin: 10px 0;'>
                        <p style='color: #888; font-size: 14px;'><strong>您的留言：</strong></p>
                        <p>{originalContent}</p>
                        <hr style='border: none; border-top: 1px solid #ddd;'>
                        <p style='color: #0D6EFD;'><strong>管理员回复：</strong></p>
                        <p>{replyContent}</p>
                        <p style='color: #888; font-size: 14px;'>回复时间：{DateTime.Now:yyyy-MM-dd HH:mm}</p>
                    </div>
                    <p style='color: #888; font-size: 14px;'>您可以点击下方链接查看完整内容：</p>
                    <a href='https://chris-hopper.org/Message/Index' style='display: inline-block; padding: 10px 20px; background-color: #0D6EFD; color: white; text-decoration: none; border-radius: 5px;'>查看留言板</a>
                    <hr style='border: none; border-top: 1px solid #eee;'>
                    <p style='color: #aaa; font-size: 12px;'>此邮件由系统自动发送，请勿直接回复。</p>
                </div>
            ";

            await SendEmailAsync(toEmail, "【Chris Hopper 个人网站】您的留言收到了回复", html);
        }

        public async Task SendAdminNewMessageNotificationAsync(string visitorName, string content, int messageId)
        {
            var html = $@"
                <div style='font-family: Arial; max-width: 600px; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                    <h2 style='color: #0D6EFD;'>📝 新留言待审核</h2>
                    <p>您好，管理员！</p>
                    <p>有一条新留言需要您的审核：</p>
                    <div style='background-color: #f8f9fa; padding: 15px; border-radius: 8px; margin: 10px 0;'>
                        <p><strong>👤 留言者：</strong>{visitorName}</p>
                        <p><strong>💬 内容：</strong>{content}</p>
                        <p><strong>⏰ 时间：</strong>{DateTime.Now:yyyy-MM-dd HH:mm}</p>
                    </div>
                    <p style='margin-top: 15px;'>
                        <a href='https://chris-hopper.org/Admin/Messages' style='display: inline-block; padding: 10px 20px; background-color: #0D6EFD; color: white; text-decoration: none; border-radius: 5px;'>🔍 前往审核</a>
                    </p>
                    <hr style='border: none; border-top: 1px solid #eee;'>
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
                    <p>您好，管理员！</p>
                    <p>有用户刚刚注册了账号：</p>
                    <div style='background-color: #f8f9fa; padding: 15px; border-radius: 8px; margin: 10px 0;'>
                        <p><strong>👤 用户名：</strong>{username}</p>
                        <p><strong>📧 邮箱：</strong>{email}</p>
                        <p><strong>⏰ 时间：</strong>{DateTime.Now:yyyy-MM-dd HH:mm}</p>
                    </div>
                    <hr style='border: none; border-top: 1px solid #eee;'>
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
                    <p>您好，管理员！</p>
                    <p>刚刚发布了一篇新博客：</p>
                    <div style='background-color: #f8f9fa; padding: 15px; border-radius: 8px; margin: 10px 0;'>
                        <p><strong>📝 标题：</strong>{blogTitle}</p>
                        <p><strong>⏰ 时间：</strong>{DateTime.Now:yyyy-MM-dd HH:mm}</p>
                    </div>
                    <p style='color: #aaa; font-size: 12px;'>此邮件由系统自动发送，请勿直接回复。</p>
                </div>
            ";

            await SendEmailAsync("2908685235@qq.com", "📖 新博客发布", html);
        }

        public async Task SendAdminNewContactRequestNotificationAsync(string identity, string platform, string authCode, string howKnowMe, string relationship, string username, string userEmail)
        {
            var html = $@"
                <div style='font-family: Arial; max-width: 600px; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                    <h2 style='color: #a855f7;'>🔑 新授权码申请</h2>
                    <p>您好，管理员！</p>
                    <p>有新的联系方式授权申请：</p>
                    <div style='background-color: #f8f9fa; padding: 15px; border-radius: 8px; margin: 10px 0;'>
                        <p><strong>👤 申请人：</strong>{identity}</p>
                        <p><strong>📧 用户邮箱：</strong>{userEmail}</p>
                        <p><strong>👤 用户名：</strong>{username}</p>
                        <p><strong>📱 平台：</strong>{(platform == "WeChat" ? "微信" : "QQ")}</p>
                        <p><strong>🔑 授权码：</strong><code style='background:#eee;padding:2px 8px;border-radius:4px;font-size:16px;letter-spacing:1px;'>{authCode}</code></p>
                        <p><strong>👋 怎么认识：</strong>{howKnowMe}</p>
                        <p><strong>🤝 关系：</strong>{relationship}</p>
                        <p><strong>⏰ 时间：</strong>{DateTime.Now:yyyy-MM-dd HH:mm}</p>
                    </div>
                    <p style='margin-top: 15px;'>
                        <a href='https://chris-hopper.org/Admin/ContactRequests' style='display: inline-block; padding: 10px 20px; background-color: #a855f7; color: white; text-decoration: none; border-radius: 5px;'>🔍 前往查看</a>
                    </p>
                    <hr style='border: none; border-top: 1px solid #eee;'>
                    <p style='color: #aaa; font-size: 12px;'>此邮件由系统自动发送，请勿直接回复。</p>
                </div>
            ";

            await SendEmailAsync("2908685235@qq.com", "🔑 新授权码申请", html);
        }
    }
}
