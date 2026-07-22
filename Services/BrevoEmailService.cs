using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MyPersonalWebsite.Services
{
    public class BrevoEmailService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _adminEmail = "2908685235@qq.com";

        public BrevoEmailService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _apiKey = Environment.GetEnvironmentVariable("BREVO_API_KEY") ?? "";
        }

        // ============================================================
        // 核心发送方法
        // ============================================================

        public async Task<bool> SendEmailAsync(string to, string subject, string htmlContent)
        {
            try
            {
                var request = new
                {
                    sender = new { email = "chris@chris-hopper.org", name = "Chris hopper 个人网站" },
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
        // 1. 管理员审核邮件（含通过/拒绝按钮）
        // ============================================================

        public async Task SendAdminVerificationRequestAsync(string username, string email, int userId)
        {
            var baseUrl = "https://chris-hopper.org";
            var approveUrl = $"{baseUrl}/Admin/VerifyUser?userId={userId}&action=approve";
            var rejectUrl = $"{baseUrl}/Admin/VerifyUser?userId={userId}&action=reject";

            var html = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                    <h2 style='color: #0D6EFD;'>📧 新用户邮箱审核</h2>
                    <p>您好，管理员！</p>
                    <p>有新用户注册，需要审核邮箱：</p>

                    <div style='background-color: #f8f9fa; padding: 15px; border-radius: 8px; margin: 15px 0;'>
                        <p><strong>👤 用户名：</strong>{username}</p>
                        <p><strong>📧 邮箱：</strong>{email}</p>
                        <p><strong>🆔 用户ID：</strong>{userId}</p>
                        <p><strong>⏰ 注册时间：</strong>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
                    </div>

                    <p style='margin: 20px 0;'>请点击下方按钮进行审核：</p>

                    <div style='display: flex; gap: 15px; margin: 20px 0; flex-wrap: wrap;'>
                        <a href='{approveUrl}' style='display: inline-block; padding: 12px 30px; background-color: #28a745; color: white; text-decoration: none; border-radius: 8px; font-weight: 600;'>
                            ✅ 通过审核
                        </a>
                        <a href='{rejectUrl}' style='display: inline-block; padding: 12px 30px; background-color: #dc3545; color: white; text-decoration: none; border-radius: 8px; font-weight: 600;'>
                            ❌ 拒绝审核
                        </a>
                    </div>

                    <p style='color: #888; font-size: 14px;'>点击按钮后，系统将自动通知用户审核结果。</p>
                    <hr style='border: none; border-top: 1px solid #eee;'>
                    <p style='color: #aaa; font-size: 12px;'>此邮件由系统自动发送，请勿直接回复。</p>
                </div>
            ";

            await SendEmailAsync(_adminEmail, "📧 新用户邮箱审核请求", html);
        }

        // ============================================================
        // 2. 密码重置验证码
        // ============================================================

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

            await SendEmailAsync(toEmail, "【Chris Hopper 个人网站】密码重置验证码 🔑", html);
        }

        // ============================================================
        // 3. 管理员回复留言通知
        // ============================================================

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

            await SendEmailAsync(toEmail, "【Chris Hopper 个人网站】你的留言收到了回复 💬", html);
        }

        // ============================================================
        // 4. 用户操作通知
        // ============================================================

        public async Task SendUserActionNotificationAsync(string toEmail, string username, string actionType, string reason, string note)
        {
            var actionMap = new Dictionary<string, string>
            {
                { "ban", "封禁" },
                { "unban", "解封" },
                { "delete", "删除账号" },
                { "activate", "账号激活" },
                { "verify_approve", "邮箱审核通过" },
                { "verify_reject", "邮箱审核拒绝" }
            };

            var actionName = actionMap.ContainsKey(actionType) ? actionMap[actionType] : actionType;

            var color = actionType == "verify_approve" ? "#28a745" : 
                        actionType == "verify_reject" ? "#dc3545" : 
                        actionType == "ban" ? "#dc3545" : 
                        actionType == "delete" ? "#dc3545" : "#0D6EFD";

            var extraMessage = "";
            if (actionType == "verify_approve")
                extraMessage = "<p style='color: #28a745; font-weight: 600;'>🎉 欢迎加入 Chris Hopper 的个人网站！现在你可以使用账号登录了。</p>";
            else if (actionType == "verify_reject")
                extraMessage = "<p style='color: #dc3545;'>❌ 如有疑问，请联系管理员。</p>";

            var html = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                    <h2 style='color: {color};'>📧 账号通知</h2>
                    <p>您好 <strong>{username}</strong>！</p>
                    <p>您在 <strong>Chris Hopper 个人网站</strong> 的账号已被管理员 <strong>{actionName}</strong>。</p>
                    <div style='background-color: #f8f9fa; padding: 15px; border-radius: 8px; margin: 10px 0;'>
                        <p><strong>📌 原因：</strong>{reason}</p>
                        {(string.IsNullOrEmpty(note) ? "" : $"<p><strong>📝 备注：</strong>{note}</p>")}
                        <p><strong>⏰ 时间：</strong>{DateTime.Now:yyyy-MM-dd HH:mm}</p>
                    </div>
                    {extraMessage}
                    <p style='color: #888; font-size: 14px;'>如有疑问，请联系管理员。</p>
                    <hr style='border: none; border-top: 1px solid #eee;'>
                    <p style='color: #aaa; font-size: 12px;'>💌 系统自动发送，不用回复。</p>
                </div>
            ";

            await SendEmailAsync(toEmail, $"【Chris Hopper 个人网站】账号{actionName}通知", html);
        }

        // ============================================================
        // 5. 管理员通知：新留言待审核
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

            await SendEmailAsync(_adminEmail, "📝 新留言待审核", html);
        }

        // ============================================================
        // 6. 管理员通知：新博客发布
        // ============================================================

        public async Task SendAdminNewBlogNotificationAsync(string blogTitle)
        {
            var html = $@"
                <div style='font-family: Arial; max-width: 600px; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                    <h2 style='color: #4facfe;'>📖 新博客发布</h2>
                    <p>新博客已发布：<strong>{blogTitle}</strong></p>
                    <p>时间：{DateTime.Now:yyyy-MM-dd HH:mm}</p>
                </div>
            ";

            await SendEmailAsync(_adminEmail, "📖 新博客发布", html);
        }

        // ============================================================
        // 7. 管理员通知：新授权码申请
        // ============================================================

        public async Task SendAdminNewContactRequestNotificationAsync(string identity, string platform, string authCode, string howKnowMe, string relationship, string username, string userEmail)
        {
            var html = $@"
                <div style='font-family: Arial; max-width: 600px; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                    <h2 style='color: #a855f7;'>🔑 新授权码申请</h2>
                    <div style='background: #f8f9fa; padding: 15px; border-radius: 8px; margin: 10px 0;'>
                        <p><strong>申请人：</strong>{identity}</p>
                        <p><strong>平台：</strong>{(platform == "WeChat" ? "微信" : "QQ")}</p>
                        <p><strong>授权码：</strong>{authCode}</p>
                        <p><strong>用户名：</strong>{username}</p>
                        <p><strong>邮箱：</strong>{userEmail}</p>
                    </div>
                    <hr>
                    <p style='color: #aaa; font-size: 12px;'>此邮件由系统自动发送，请勿直接回复。</p>
                </div>
            ";

            await SendEmailAsync(_adminEmail, "🔑 新授权码申请", html);
        }
    }
}
