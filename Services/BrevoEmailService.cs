using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using MyPersonalWebsite.Models;

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
        // ⭐ 核心发送方法 - 支持附件
        // ============================================================

        public async Task<bool> SendEmailWithAttachmentAsync(
            string to,
            string subject,
            string htmlContent,
            byte[]? attachmentData = null,
            string? attachmentName = null)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    Console.WriteLine("⚠️ Brevo API Key 未配置");
                    return false;
                }

                var request = new
                {
                    sender = new { email = "chris@chris-hopper.org", name = "Chris hopper 个人网站" },
                    to = new[] { new { email = to } },
                    subject = subject,
                    htmlContent = htmlContent,
                    attachment = attachmentData != null && !string.IsNullOrEmpty(attachmentName)
                        ? new[]
                        {
                            new
                            {
                                content = Convert.ToBase64String(attachmentData),
                                name = attachmentName
                            }
                        }
                        : null
                };

                var options = new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                var json = JsonSerializer.Serialize(request, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);

                var response = await _httpClient.PostAsync("https://api.brevo.com/v3/smtp/email", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✅ 邮件发送成功: {to}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ 邮件发送失败 ({response.StatusCode}): {responseBody}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 邮件发送异常: {ex.Message}");
                return false;
            }
        }

        // ============================================================
        // 无附件版本（兼容旧代码）
        // ============================================================

        public async Task<bool> SendEmailAsync(string to, string subject, string htmlContent)
        {
            return await SendEmailWithAttachmentAsync(to, subject, htmlContent, null, null);
        }

        // ============================================================
        // 辅助方法
        // ============================================================

        private string FormatChinaTime(DateTime utcTime)
        {
            try
            {
                var chinaTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai"));
                return chinaTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch
            {
                return utcTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }

        // ============================================================
        // 1. 发送邮箱验证码
        // ============================================================

        public async Task SendVerificationCodeAsync(string toEmail, string code)
        {
            var html = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #2a2a3e; border-radius: 16px; background: #0a0a0f; color: #e0e0e0;'>
                    <h2 style='color: #8B5CF6;'>✌️ 嘿，是你吗？</h2>
                    <p>有人在 <strong>Chris hopper 的个人网站</strong> 用这个邮箱注册了账号。</p>
                    <p>如果是你，请用这个验证码完成注册：</p>
                    <div style='background: #1a1a2e; padding: 15px; text-align: center; font-size: 32px; letter-spacing: 8px; font-weight: bold; color: #8B5CF6; border-radius: 8px;'>
                        {code}
                    </div>
                    <p style='color: #888; font-size: 14px;'>⏳ 10 分钟内有效。</p>
                    <hr style='border: none; border-top: 1px solid #2a2a3e;'>
                    <p style='color: #555; font-size: 12px;'>💌 系统自动发送，不用回复。</p>
                </div>
            ";

            await SendEmailAsync(toEmail, "【Chris hopper 个人网站】邮箱验证码 ✌️", html);
        }

        // ============================================================
        // 2. 密码重置验证码
        // ============================================================

        public async Task SendPasswordResetEmailAsync(string toEmail, string code)
        {
            var html = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #2a2a3e; border-radius: 16px; background: #0a0a0f; color: #e0e0e0;'>
                    <h2 style='color: #8B5CF6;'>🔑 密码重置请求</h2>
                    <p>有人在 <strong>Chris hopper 的个人网站</strong> 申请重置密码。</p>
                    <p>如果是你，用这个验证码重置：</p>
                    <div style='background: #1a1a2e; padding: 15px; text-align: center; font-size: 32px; letter-spacing: 8px; font-weight: bold; color: #8B5CF6; border-radius: 8px;'>
                        {code}
                    </div>
                    <p style='color: #888; font-size: 14px;'>⏳ 10 分钟内有效。</p>
                    <hr style='border: none; border-top: 1px solid #2a2a3e;'>
                    <p style='color: #555; font-size: 12px;'>💌 系统自动发送，不用回复。</p>
                </div>
            ";

            await SendEmailAsync(toEmail, "【Chris hopper 个人网站】密码重置验证码 🔑", html);
        }

        // ============================================================
        // 3. 新用户审核邮件（管理员）
        // ============================================================

        public async Task SendAdminNewUserVerificationAsync(string username, string email, int userId, string? avatarUrl, DateTime registerTime)
        {
            var baseUrl = "https://chris-hopper.org";
            var approveUrl = $"{baseUrl}/Admin/ApproveUser?userId={userId}";
            var rejectUrl = $"{baseUrl}/Admin/RejectUser?userId={userId}";
            var registerTimeStr = FormatChinaTime(registerTime);

            var fullAvatarUrl = string.IsNullOrEmpty(avatarUrl) ? null : (avatarUrl.StartsWith("http") ? avatarUrl : $"{baseUrl}{avatarUrl}");

            var avatarHtml = string.IsNullOrEmpty(fullAvatarUrl)
                ? "<p style='color:#555;'>未上传头像</p>"
                : $"<img src='{fullAvatarUrl}' style='width:80px;height:80px;border-radius:50%;object-fit:cover;border:2px solid #8B5CF6;' />";

            var html = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #2a2a3e; border-radius: 16px; background: #0a0a0f; color: #e0e0e0;'>
                    <h2 style='color: #8B5CF6;'>📝 新用户审核</h2>
                    <p>有新用户完成邮箱验证，等待审核：</p>
                    <div style='background: #1a1a2e; border-radius: 12px; padding: 16px; margin: 16px 0; border: 1px solid #2a2a3e;'>
                        <p><strong>👤 用户名：</strong>{username}</p>
                        <p><strong>📧 邮箱：</strong>{email}</p>
                        <p><strong>🆔 用户ID：</strong>{userId}</p>
                        <p><strong>⏰ 注册时间：</strong>{registerTimeStr}</p>
                        <p><strong>🖼️ 头像：</strong></p>
                        <div style='text-align:center;margin:10px 0;'>{avatarHtml}</div>
                    </div>
                    <div style='display: flex; gap: 12px; margin: 16px 0; flex-wrap: wrap;'>
                        <a href='{approveUrl}' style='display: inline-block; padding: 12px 32px; background: #28a745; color: white; text-decoration: none; border-radius: 8px; font-weight: 600;'>✅ 通过审核</a>
                        <a href='{rejectUrl}' style='display: inline-block; padding: 12px 32px; background: #dc3545; color: white; text-decoration: none; border-radius: 8px; font-weight: 600;'>❌ 拒绝审核</a>
                    </div>
                    <p style='color: #888; font-size: 14px;'>点击按钮后，系统将自动通知用户。</p>
                    <hr style='border: none; border-top: 1px solid #2a2a3e;'>
                    <p style='color: #555; font-size: 12px;'>此邮件由系统自动发送，请勿直接回复。</p>
                </div>
            ";

            await SendEmailAsync(_adminEmail, $"📝 新用户审核 - {username}", html);
        }

        // ============================================================
        // 4. 头像审核邮件（管理员）
        // ============================================================

        public async Task SendAdminAvatarVerificationAsync(string username, string email, int userId, string avatarData, DateTime submittedAt)
        {
            var baseUrl = "https://chris-hopper.org";
            var approveUrl = $"{baseUrl}/Admin/ApproveAvatar?userId={userId}";
            var rejectUrl = $"{baseUrl}/Admin/RejectAvatar?userId={userId}";
            var submittedAtStr = FormatChinaTime(submittedAt);

            var avatarHtml = string.IsNullOrEmpty(avatarData)
                ? "<p style='color:#555;'>未上传头像</p>"
                : $"<img src='{avatarData}' style='width:120px;height:120px;border-radius:50%;object-fit:cover;border:2px solid #f59e0b;' />";

            var html = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #2a2a3e; border-radius: 16px; background: #0a0a0f; color: #e0e0e0;'>
                    <h2 style='color: #f59e0b;'>🖼️ 头像审核</h2>
                    <p>用户 <strong>{username}</strong> 上传了新头像，等待审核：</p>
                    <div style='background: #1a1a2e; border-radius: 12px; padding: 16px; margin: 16px 0; border: 1px solid #2a2a3e; text-align:center;'>
                        <p><strong>👤 用户名：</strong>{username}</p>
                        <p><strong>📧 邮箱：</strong>{email}</p>
                        <p><strong>🆔 用户ID：</strong>{userId}</p>
                        <p><strong>⏰ 提交时间：</strong>{submittedAtStr}</p>
                        <p><strong>🖼️ 新头像：</strong></p>
                        <div style='margin:10px 0;'>{avatarHtml}</div>
                    </div>
                    <div style='display: flex; gap: 12px; margin: 16px 0; flex-wrap: wrap;'>
                        <a href='{approveUrl}' style='display: inline-block; padding: 12px 32px; background: #28a745; color: white; text-decoration: none; border-radius: 8px; font-weight: 600;'>✅ 通过</a>
                        <a href='{rejectUrl}' style='display: inline-block; padding: 12px 32px; background: #dc3545; color: white; text-decoration: none; border-radius: 8px; font-weight: 600;'>❌ 拒绝</a>
                    </div>
                    <p style='color: #888; font-size: 14px;'>点击按钮后，系统将自动通知用户。</p>
                    <hr style='border: none; border-top: 1px solid #2a2a3e;'>
                    <p style='color: #555; font-size: 12px;'>此邮件由系统自动发送，请勿直接回复。</p>
                </div>
            ";

            await SendEmailAsync(_adminEmail, $"🖼️ 头像审核 - {username}", html);
        }

        // ============================================================
        // 5. 昵称修改审核邮件（管理员）
        // ============================================================

        public async Task SendAdminUsernameVerificationAsync(string username, string email, int userId, string oldUsername, string newUsername)
        {
            var baseUrl = "https://chris-hopper.org";
            var approveUrl = $"{baseUrl}/Admin/ApproveUsername?userId={userId}";
            var rejectUrl = $"{baseUrl}/Admin/RejectUsername?userId={userId}";

            var html = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #2a2a3e; border-radius: 16px; background: #0a0a0f; color: #e0e0e0;'>
                    <h2 style='color: #ec4899;'>✏️ 昵称修改审核</h2>
                    <p>用户 <strong>{username}</strong> 申请修改昵称：</p>
                    <div style='background: #1a1a2e; border-radius: 12px; padding: 16px; margin: 16px 0; border: 1px solid #2a2a3e;'>
                        <p><strong>👤 当前昵称：</strong><span style='color:#888;'>{oldUsername}</span></p>
                        <p><strong>🆕 新昵称：</strong><span style='color:#8B5CF6;font-size:1.2rem;font-weight:600;'>{newUsername}</span></p>
                        <p><strong>📧 邮箱：</strong>{email}</p>
                        <p><strong>🆔 用户ID：</strong>{userId}</p>
                    </div>
                    <div style='display: flex; gap: 12px; margin: 16px 0; flex-wrap: wrap;'>
                        <a href='{approveUrl}' style='display: inline-block; padding: 12px 32px; background: #28a745; color: white; text-decoration: none; border-radius: 8px; font-weight: 600;'>✅ 通过</a>
                        <a href='{rejectUrl}' style='display: inline-block; padding: 12px 32px; background: #dc3545; color: white; text-decoration: none; border-radius: 8px; font-weight: 600;'>❌ 拒绝</a>
                    </div>
                    <p style='color: #888; font-size: 14px;'>点击按钮后，系统将自动通知用户。</p>
                    <hr style='border: none; border-top: 1px solid #2a2a3e;'>
                    <p style='color: #555; font-size: 12px;'>此邮件由系统自动发送，请勿直接回复。</p>
                </div>
            ";

            await SendEmailAsync(_adminEmail, $"✏️ 昵称修改审核 - {username}", html);
        }

        // ============================================================
        // 6. 邮箱修改审核邮件（管理员）
        // ============================================================

        public async Task SendAdminEmailVerificationAsync(string username, string email, int userId, string oldEmail, string newEmail)
        {
            var baseUrl = "https://chris-hopper.org";
            var approveUrl = $"{baseUrl}/Admin/ApproveEmail?userId={userId}";
            var rejectUrl = $"{baseUrl}/Admin/RejectEmail?userId={userId}";

            var html = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #2a2a3e; border-radius: 16px; background: #0a0a0f; color: #e0e0e0;'>
                    <h2 style='color: #06b6d4;'>📧 邮箱修改审核</h2>
                    <p>用户 <strong>{username}</strong> 申请修改邮箱：</p>
                    <div style='background: #1a1a2e; border-radius: 12px; padding: 16px; margin: 16px 0; border: 1px solid #2a2a3e;'>
                        <p><strong>📧 当前邮箱：</strong><span style='color:#888;'>{oldEmail}</span></p>
                        <p><strong>🆕 新邮箱：</strong><span style='color:#8B5CF6;font-size:1.1rem;font-weight:600;'>{newEmail}</span></p>
                        <p><strong>👤 用户名：</strong>{username}</p>
                        <p><strong>🆔 用户ID：</strong>{userId}</p>
                    </div>
                    <div style='display: flex; gap: 12px; margin: 16px 0; flex-wrap: wrap;'>
                        <a href='{approveUrl}' style='display: inline-block; padding: 12px 32px; background: #28a745; color: white; text-decoration: none; border-radius: 8px; font-weight: 600;'>✅ 通过</a>
                        <a href='{rejectUrl}' style='display: inline-block; padding: 12px 32px; background: #dc3545; color: white; text-decoration: none; border-radius: 8px; font-weight: 600;'>❌ 拒绝</a>
                    </div>
                    <p style='color: #888; font-size: 14px;'>点击按钮后，系统将自动通知用户。</p>
                    <hr style='border: none; border-top: 1px solid #2a2a3e;'>
                    <p style='color: #555; font-size: 12px;'>此邮件由系统自动发送，请勿直接回复。</p>
                </div>
            ";

            await SendEmailAsync(_adminEmail, $"📧 邮箱修改审核 - {username}", html);
        }

        // ============================================================
        // 7. 留言审核邮件（管理员）
        // ============================================================

        public async Task SendAdminNewMessageNotificationAsync(string visitorName, string content, int messageId)
        {
            var baseUrl = "https://chris-hopper.org";
            var approveUrl = $"{baseUrl}/Admin/ApproveMessage?messageId={messageId}";
            var rejectUrl = $"{baseUrl}/Admin/RejectMessage?messageId={messageId}";

            var html = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #2a2a3e; border-radius: 16px; background: #0a0a0f; color: #e0e0e0;'>
                    <h2 style='color: #4facfe;'>💬 新留言待审核</h2>
                    <p>有新留言需要审核：</p>
                    <div style='background: #1a1a2e; padding: 15px; border-radius: 8px; margin: 10px 0; border: 1px solid #2a2a3e;'>
                        <p><strong>留言者：</strong>{visitorName}</p>
                        <p><strong>内容：</strong>{content}</p>
                        <p><strong>时间：</strong>{FormatChinaTime(DateTime.UtcNow)}</p>
                        <p><strong>留言ID：</strong>{messageId}</p>
                    </div>
                    <div style='display: flex; gap: 12px; margin: 16px 0; flex-wrap: wrap;'>
                        <a href='{approveUrl}' style='display: inline-block; padding: 12px 32px; background: #28a745; color: white; text-decoration: none; border-radius: 8px; font-weight: 600;'>✅ 通过</a>
                        <a href='{rejectUrl}' style='display: inline-block; padding: 12px 32px; background: #dc3545; color: white; text-decoration: none; border-radius: 8px; font-weight: 600;'>🗑️ 删除</a>
                    </div>
                    <hr style='border: none; border-top: 1px solid #2a2a3e;'>
                    <p style='color: #555; font-size: 12px;'>此邮件由系统自动发送，请勿直接回复。</p>
                </div>
            ";

            await SendEmailAsync(_adminEmail, "💬 新留言待审核", html);
        }

        // ============================================================
        // 8. 管理员通知：新博客发布
        // ============================================================

        public async Task SendAdminNewBlogNotificationAsync(string blogTitle)
        {
            var html = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #2a2a3e; border-radius: 16px; background: #0a0a0f; color: #e0e0e0;'>
                    <h2 style='color: #4facfe;'>📖 新博客发布</h2>
                    <p>新博客已发布：<strong>{blogTitle}</strong></p>
                    <p>时间：{FormatChinaTime(DateTime.UtcNow)}</p>
                    <hr style='border: none; border-top: 1px solid #2a2a3e;'>
                    <p style='color: #555; font-size: 12px;'>此邮件由系统自动发送，请勿直接回复。</p>
                </div>
            ";

            await SendEmailAsync(_adminEmail, "📖 新博客发布", html);
        }

        // ============================================================
        // 9. 管理员回复留言通知（发送给用户）
        // ============================================================

        public async Task SendReplyNotificationAsync(string toEmail, string userName, string originalContent, string replyContent)
        {
            var html = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #2a2a3e; border-radius: 16px; background: #0a0a0f; color: #e0e0e0;'>
                    <h2 style='color: #8B5CF6;'>💬 你的留言被回复了</h2>
                    <p>你好 <strong>{userName}</strong>！</p>
                    <p>你在留言板上的留言收到了管理员的回复：</p>
                    <div style='background: #1a1a2e; padding: 15px; border-radius: 8px; margin: 10px 0; border: 1px solid #2a2a3e;'>
                        <p><strong>你的留言：</strong>{originalContent}</p>
                        <hr style='border: none; border-top: 1px solid #2a2a3e;'>
                        <p><strong>管理员回复：</strong>{replyContent}</p>
                        <p style='color: #888; font-size: 14px;'>回复时间：{FormatChinaTime(DateTime.UtcNow)}</p>
                    </div>
                    <a href='https://chris-hopper.org/Message/Index' style='display: inline-block; padding: 10px 20px; background: #8B5CF6; color: white; text-decoration: none; border-radius: 8px;'>查看留言板</a>
                    <hr style='border: none; border-top: 1px solid #2a2a3e;'>
                    <p style='color: #555; font-size: 12px;'>💌 系统自动发送，不用回复。</p>
                </div>
            ";

            await SendEmailAsync(toEmail, "【Chris hopper 个人网站】你的留言收到了回复 💬", html);
        }

        // ============================================================
        // 10. 管理员通知：新授权码申请
        // ============================================================

        public async Task SendAdminNewContactRequestNotificationAsync(string identity, string platform, string authCode, string howKnowMe, string relationship, string username, string userEmail)
        {
            var html = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #2a2a3e; border-radius: 16px; background: #0a0a0f; color: #e0e0e0;'>
                    <h2 style='color: #a855f7;'>🔑 新授权码申请</h2>
                    <p>有新用户申请联系方式：</p>
                    <div style='background: #1a1a2e; padding: 15px; border-radius: 8px; margin: 10px 0; border: 1px solid #2a2a3e;'>
                        <p><strong>👤 申请人：</strong>{identity}</p>
                        <p><strong>📱 平台：</strong>{(platform == "WeChat" ? "微信" : "QQ")}</p>
                        <p><strong>🔑 授权码：</strong><span style='color:#a855f7;font-weight:bold;font-size:1.2rem;'>{authCode}</span></p>
                        <p><strong>👤 用户名：</strong>{username}</p>
                        <p><strong>📧 邮箱：</strong>{userEmail}</p>
                        <p><strong>👋 怎么认识：</strong>{howKnowMe}</p>
                        <p><strong>🤝 关系：</strong>{relationship}</p>
                        <p><strong>⏰ 时间：</strong>{FormatChinaTime(DateTime.UtcNow)}</p>
                    </div>
                    <hr style='border: none; border-top: 1px solid #2a2a3e;'>
                    <p style='color: #555; font-size: 12px;'>此邮件由系统自动发送，请勿直接回复。</p>
                </div>
            ";

            await SendEmailAsync(_adminEmail, "🔑 新授权码申请", html);
        }

        // ============================================================
        // 11. 用户操作通知
        // ============================================================

        public async Task SendUserActionNotificationAsync(string toEmail, string username, string actionType, string reason, string note, string? loginToken = null)
        {
            var baseUrl = "https://chris-hopper.org";
            var actionMap = new Dictionary<string, string>
            {
                { "approve", "审核通过" },
                { "reject", "审核拒绝" },
                { "avatar_approve", "头像审核通过" },
                { "avatar_reject", "头像审核拒绝" },
                { "username_approve", "昵称修改通过" },
                { "username_reject", "昵称修改拒绝" },
                { "email_approve", "邮箱修改通过" },
                { "email_reject", "邮箱修改拒绝" },
                { "message_approve", "留言审核通过" },
                { "message_reject", "留言审核拒绝" },
                { "ban", "封禁" },
                { "unban", "解封" },
                { "delete", "删除账号" },
                { "activate", "账号激活" },
                { "notification", "系统通知" }
            };

            var actionName = actionMap.ContainsKey(actionType) ? actionMap[actionType] : actionType;

            var color = actionType == "approve" || actionType == "avatar_approve" || actionType == "username_approve" || actionType == "email_approve" || actionType == "message_approve" ? "#28a745" :
                        actionType == "reject" || actionType == "avatar_reject" || actionType == "username_reject" || actionType == "email_reject" ? "#dc3545" :
                        actionType == "ban" || actionType == "delete" ? "#dc3545" : "#0D6EFD";

            var detailUrl = !string.IsNullOrEmpty(loginToken)
                ? $"{baseUrl}/Auth/AutoLogin?token={loginToken}"
                : $"{baseUrl}/Home/Notifications";

            var extraMessage = "";
            if (actionType == "approve")
                extraMessage = "<p style='color: #28a745; font-weight: 600;'>🎉 欢迎加入 Chris hopper 的个人网站！</p>";
            else if (actionType == "reject")
                extraMessage = "<p style='color: #dc3545;'>❌ 如有疑问，请联系管理员。</p>";
            else if (actionType == "notification")
                extraMessage = "<p style='color: #8B5CF6; font-weight: 600;'>📬 这是一条系统通知，点击下方按钮查看详情。</p>";
            else if (actionType == "message_approve")
                extraMessage = "<p style='color: #28a745; font-weight: 600;'>💬 你的留言已通过审核，点击下方按钮查看。</p>";
            else if (actionType == "message_reject")
                extraMessage = "<p style='color: #dc3545;'>💬 你的留言已被删除，点击下方按钮查看。</p>";

            var html = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #2a2a3e; border-radius: 16px; background: #0a0a0f; color: #e0e0e0;'>
                    <div style='text-align: center; margin-bottom: 16px;'>
                        <span style='font-size: 2.5rem;'>✌️</span>
                    </div>
                    <h2 style='color: {color}; text-align: center;'>📧 账号通知</h2>
                    <p>您好 <strong>{username}</strong>！</p>
                    <p>您在 <strong>Chris hopper 个人网站</strong> 的账号已被管理员 <strong>{actionName}</strong>。</p>
                    <div style='background: #1a1a2e; padding: 15px; border-radius: 8px; margin: 10px 0; border: 1px solid #2a2a3e;'>
                        <p><strong>📌 原因：</strong>{reason}</p>
                        {(string.IsNullOrEmpty(note) ? "" : $"<p><strong>📝 备注：</strong>{note}</p>")}
                        <p><strong>⏰ 时间：</strong>{FormatChinaTime(DateTime.UtcNow)}</p>
                    </div>
                    {extraMessage}
                    <div style='margin: 20px 0; text-align: center;'>
                        <a href='{detailUrl}' style='display: inline-block; padding: 14px 48px; background: linear-gradient(135deg, #8B5CF6, #EC4899); color: white; text-decoration: none; border-radius: 40px; font-weight: 600; font-size: 1rem; box-shadow: 0 4px 24px rgba(108,60,225,0.2);'>
                            👁️ 查看详情
                        </a>
                        <p style='color: rgba(255,255,255,0.12); font-size: 0.7rem; margin-top: 0.3rem;'>
                            🔒 点击后自动登录，无需输入密码
                        </p>
                    </div>
                    <hr style='border: none; border-top: 1px solid #2a2a3e;'>
                    <p style='color: #555; font-size: 12px; text-align: center;'>💌 系统自动发送，不用回复。</p>
                </div>
            ";

            await SendEmailAsync(toEmail, $"【Chris hopper 个人网站】账号{actionName}通知", html);
        }

        // ============================================================
        // ⭐ 12. 资源申请 - 管理员通知
        // ============================================================

        public async Task SendResourceRequestNotificationAsync(ResourceRequest request)
        {
            var baseUrl = "https://chris-hopper.org";

            var verifyStatusText = request.VerifyStatus switch
            {
                "auto_verified" => "✅ 自动验证通过",
                "manual_required" => "⚠️ 待人工核验",
                "rejected" => "❌ 验证未通过",
                _ => "⏳ 未验证"
            };

            var html = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #2a2a3e; border-radius: 16px; background: #0a0a0f; color: #e0e0e0;'>
                    <h2 style='color: #8B5CF6;'>📦 新资源申请</h2>
                    <p>有新的资源申请需要处理：</p>
                    <div style='background: #1a1a2e; padding: 15px; border-radius: 8px; margin: 10px 0; border: 1px solid #2a2a3e;'>
                        <p><strong>👤 申请人：</strong>{request.UserName}</p>
                        <p><strong>📧 邮箱：</strong>{request.UserEmail}</p>
                        <p><strong>📝 人物/CP：</strong>{request.CharacterName}</p>
                        <p><strong>📱 平台：</strong>{(string.IsNullOrEmpty(request.Platform1) ? "未选择" : request.Platform1 + (string.IsNullOrEmpty(request.Platform2) ? "" : " + " + request.Platform2))}</p>
                        <p><strong>📂 资源类型：</strong>{request.ResourceType}</p>
                        <p><strong>⚙️ 人物设定：</strong>{request.CharacterSetting}</p>
                        <p><strong>📊 偏好：</strong>小说:{request.NovelPreference} · 漫画:{request.ComicPreference} · 图片:{request.ImagePreference}</p>
                        {(!string.IsNullOrEmpty(request.VerifyPlatform) ? $"<p><strong>🔍 验证平台：</strong>{request.VerifyPlatform} - {request.VerifyAccountId}</p>" : "")}
                        <p><strong>🔐 验证状态：</strong>{verifyStatusText}</p>
                        <p><strong>📝 备注：</strong>{(string.IsNullOrEmpty(request.AdminNote) ? "无" : request.AdminNote)}</p>
                        <p><strong>⏰ 申请时间：</strong>{FormatChinaTime(request.CreatedAt)}</p>
                        <p><strong>🆔 申请ID：</strong>#{request.Id}</p>
                    </div>
                    <div style='margin: 16px 0;'>
                        <a href='{baseUrl}/Admin/ProcessResource/{request.Id}'
                           style='display: inline-block; padding: 12px 32px; background: #8B5CF6; color: white; text-decoration: none; border-radius: 8px; font-weight: 600;'>
                            🔍 立即处理
                        </a>
                    </div>
                    <hr style='border: none; border-top: 1px solid #2a2a3e;'>
                    <p style='color: #555; font-size: 12px;'>此邮件由系统自动发送，请勿直接回复。</p>
                </div>
            ";

            await SendEmailAsync(_adminEmail, $"📦 新资源申请 - {request.CharacterName}", html);
        }

        // ============================================================
        // ⭐ 13. 资源处理结果 - 带附件发送给用户
        // ============================================================

        public async Task SendResourceResultEmailAsync(ResourceRequest request, byte[]? attachmentData = null, string? attachmentName = null)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmm");
           var subject = $"{request.ResourceName}资源内容-{timestamp}";

            var foundTypes = string.IsNullOrEmpty(request.FoundTypes) ? "无" : request.FoundTypes;
            var notFoundTypes = string.IsNullOrEmpty(request.NotFoundTypes) ? "无" : request.NotFoundTypes;

            var refundInfo = "";
            if (request.RefundOption == "1day_paid" && request.Status == "refunded")
            {
                refundInfo = "<p style='color: #F59E0B;'>💰 已退款 ¥2.00</p>";
            }
            else if (request.RefundOption == "1day_paid" && request.Status == "completed")
            {
                refundInfo = "<p style='color: #00FF88;'>✅ 已在1天内处理，无需退款</p>";
            }

            var attachmentInfo = "";
            if (!string.IsNullOrEmpty(attachmentName))
            {
                attachmentInfo = $"<p style='color: #8B5CF6;'>📎 附件：{attachmentName}</p>";
            }

            var html = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #2a2a3e; border-radius: 16px; background: #0a0a0f; color: #e0e0e0;'>
                    <h2 style='color: #8B5CF6;'>📦 资源处理结果</h2>
                    <p>您好 <strong>{request.UserName}</strong>！</p>
                    <p>您的资源申请已处理完成：</p>

                    <div style='background: #1a1a2e; padding: 15px; border-radius: 8px; margin: 10px 0; border: 1px solid #2a2a3e;'>
                        <p><strong>👤 联系人：</strong>{request.PersonName}</p>
                        <p><strong>📱 平台：</strong>{request.Platform1}{(string.IsNullOrEmpty(request.Platform2) ? "" : " + " + request.Platform2)}</p>
                        <p><strong>📂 资源名称：</strong>{request.ResourceName}</p>
                        <p><strong>📊 状态：</strong>{(request.Status == "completed" ? "✅ 已完成" : request.Status == "rejected" ? "❌ 已拒绝" : request.Status == "refunded" ? "💰 已退款" : "⏳ 处理中")}</p>
                        {refundInfo}
                    </div>

                    <div style='background: #1a1a2e; padding: 15px; border-radius: 8px; margin: 10px 0; border: 1px solid #2a2a3e;'>
                        <p><strong>✅ 已找到类型：</strong><span style='color: #00FF88;'>{foundTypes}</span></p>
                        <p><strong>❌ 未找到类型：</strong><span style='color: #dc3545;'>{notFoundTypes}</span></p>
                        {attachmentInfo}
                        {(string.IsNullOrEmpty(request.AdminNote) ? "" : $@"<p><strong>📝 管理员备注：</strong>{request.AdminNote}</p>")}
                    </div>

                    <p style='color: #888; font-size: 14px;'>⏰ 处理时间：{FormatChinaTime(request.ProcessedAt ?? DateTime.Now)}</p>

                    <hr style='border: none; border-top: 1px solid #2a2a3e;'>
                    <p style='color: #555; font-size: 12px;'>此邮件为系统发送，请勿回复</p>
                </div>";

            await SendEmailWithAttachmentAsync(request.UserEmail, subject, html, attachmentData, attachmentName);
        }

        // ============================================================
        // ⭐ 14. 支付确认通知（发送给用户）
        // ============================================================

        public async Task SendPaymentConfirmedEmailAsync(ResourceRequest request)
        {
            var html = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #2a2a3e; border-radius: 16px; background: #0a0a0f; color: #e0e0e0;'>
                    <h2 style='color: #28a745;'>✅ 支付已确认</h2>
                    <p>您好 <strong>{request.UserName}</strong>！</p>
                    <p>您的支付已由管理员确认：</p>
                    <div style='background: #1a1a2e; padding: 15px; border-radius: 8px; margin: 10px 0; border: 1px solid #2a2a3e;'>
                        <p><strong>📋 订单号：</strong><span style='color:#8B5CF6;font-family:monospace;'>{request.OrderId}</span></p>
                        <p><strong>💰 金额：</strong>¥{request.Amount:F2}</p>
                        <p><strong>⏰ 确认时间：</strong>{FormatChinaTime(request.PaidAt ?? DateTime.Now)}</p>
                        {(string.IsNullOrEmpty(request.PaidNote) ? "" : $"<p><strong>📝 备注：</strong>{request.PaidNote}</p>")}
                    </div>
                    <p style='color: #888; font-size: 14px;'>管理员将尽快处理你的资源申请。</p>
                    <div style='margin: 20px 0; text-align: center;'>
                        <a href='https://chris-hopper.org/Resource/History' style='display: inline-block; padding: 12px 32px; background: #8B5CF6; color: white; text-decoration: none; border-radius: 8px; font-weight: 600;'>
                            📋 查看申请
                        </a>
                    </div>
                    <hr style='border: none; border-top: 1px solid #2a2a3e;'>
                    <p style='color: #555; font-size: 12px;'>此邮件由系统自动发送，请勿直接回复。</p>
                </div>";

            await SendEmailAsync(request.UserEmail, $"✅ 支付已确认 - {request.CharacterName}", html);
        }

        // ============================================================
        // ⭐ 15. 关注确认邮件（发送给用户）
        // ============================================================

        public async Task SendFollowConfirmedEmailAsync(ResourceRequest request)
        {
            var html = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #2a2a3e; border-radius: 16px; background: #0a0a0f; color: #e0e0e0;'>
                    <h2 style='color: #28a745;'>✅ 关注已确认</h2>
                    <p>您好 <strong>{request.UserName}</strong>！</p>
                    <p>管理员已确认你在 <strong>{request.VerifyPlatform}</strong> 的关注：</p>
                    <div style='background: #1a1a2e; padding: 15px; border-radius: 8px; margin: 10px 0; border: 1px solid #2a2a3e;'>
                        <p><strong>📱 平台：</strong>{request.VerifyPlatform}</p>
                        <p><strong>🆔 账号ID：</strong>{request.VerifyAccountId}</p>
                        <p><strong>⏰ 确认时间：</strong>{FormatChinaTime(request.FollowVerifiedAt ?? DateTime.Now)}</p>
                    </div>
                    <p style='color: #888; font-size: 14px;'>你的资源申请正在处理中，请耐心等待。</p>
                    <div style='margin: 20px 0; text-align: center;'>
                        <a href='https://chris-hopper.org/Resource/History' style='display: inline-block; padding: 12px 32px; background: #8B5CF6; color: white; text-decoration: none; border-radius: 8px; font-weight: 600;'>
                            📋 查看申请
                        </a>
                    </div>
                    <hr style='border: none; border-top: 1px solid #2a2a3e;'>
                    <p style='color: #555; font-size: 12px;'>此邮件由系统自动发送，请勿直接回复。</p>
                </div>";

            await SendEmailAsync(request.UserEmail, $"✅ 关注已确认 - {request.CharacterName}", html);
        }

        // ============================================================
        // ⭐⭐⭐ 16. 封禁通知（发送给被封禁用户）⭐⭐⭐
        // ============================================================

        public async Task SendBanNotificationAsync(string toEmail, string username, string reason, string? loginToken = null)
        {
            var baseUrl = "https://chris-hopper.org";
            var detailUrl = !string.IsNullOrEmpty(loginToken)
                ? $"{baseUrl}/Auth/AutoLogin?token={loginToken}"
                : $"{baseUrl}/Home/Notifications";

            var html = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #dc3545; border-radius: 16px; background: #0a0a0f; color: #e0e0e0;'>
                    <div style='text-align: center; margin-bottom: 16px;'>
                        <span style='font-size: 2.5rem;'>🚫</span>
                    </div>
                    <h2 style='color: #dc3545; text-align: center;'>⛔ 账户封禁通知</h2>
                    <p>您好 <strong>{username}</strong>！</p>
                    <p>您的账号在 <strong>Chris hopper 个人网站</strong> 已被管理员封禁。</p>
                    <div style='background: #1a1a2e; padding: 15px; border-radius: 8px; margin: 10px 0; border: 1px solid #dc3545;'>
                        <p><strong>📌 封禁原因：</strong>{reason}</p>
                        <p><strong>⏰ 封禁时间：</strong>{FormatChinaTime(DateTime.UtcNow)}</p>
                        <p style='color: #888; font-size: 14px;'>⚠️ 如有疑问，请联系管理员申诉。</p>
                    </div>
                    <div style='margin: 20px 0; text-align: center;'>
                        <a href='{detailUrl}' style='display: inline-block; padding: 14px 48px; background: linear-gradient(135deg, #dc3545, #a71d2a); color: white; text-decoration: none; border-radius: 40px; font-weight: 600; font-size: 1rem;'>
                            👁️ 查看详情
                        </a>
                    </div>
                    <hr style='border: none; border-top: 1px solid #2a2a3e;'>
                    <p style='color: #555; font-size: 12px; text-align: center;'>💌 系统自动发送，请勿直接回复。</p>
                </div>
            ";

            await SendEmailAsync(toEmail, "【Chris hopper 个人网站】⛔ 账户封禁通知", html);
        }

        // ============================================================
        // ⭐⭐⭐ 17. 解封通知（发送给解封用户）⭐⭐⭐
        // ============================================================

        public async Task SendUnbanNotificationAsync(string toEmail, string username, string? loginToken = null)
        {
            var baseUrl = "https://chris-hopper.org";
            var detailUrl = !string.IsNullOrEmpty(loginToken)
                ? $"{baseUrl}/Auth/AutoLogin?token={loginToken}"
                : $"{baseUrl}/Home/Notifications";

            var html = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #28a745; border-radius: 16px; background: #0a0a0f; color: #e0e0e0;'>
                    <div style='text-align: center; margin-bottom: 16px;'>
                        <span style='font-size: 2.5rem;'>🎉</span>
                    </div>
                    <h2 style='color: #28a745; text-align: center;'>✅ 账户解封通知</h2>
                    <p>您好 <strong>{username}</strong>！</p>
                    <p>您的账号在 <strong>Chris hopper 个人网站</strong> 已被管理员解封。</p>
                    <div style='background: #1a1a2e; padding: 15px; border-radius: 8px; margin: 10px 0; border: 1px solid #28a745;'>
                        <p><strong>⏰ 解封时间：</strong>{FormatChinaTime(DateTime.UtcNow)}</p>
                        <p style='color: #28a745; font-weight: 600;'>🎊 欢迎回来！</p>
                    </div>
                    <div style='margin: 20px 0; text-align: center;'>
                        <a href='{detailUrl}' style='display: inline-block; padding: 14px 48px; background: linear-gradient(135deg, #28a745, #1e7e34); color: white; text-decoration: none; border-radius: 40px; font-weight: 600; font-size: 1rem;'>
                            👁️ 查看详情
                        </a>
                    </div>
                    <hr style='border: none; border-top: 1px solid #2a2a3e;'>
                    <p style='color: #555; font-size: 12px; text-align: center;'>💌 系统自动发送，请勿直接回复。</p>
                </div>
            ";

            await SendEmailAsync(toEmail, "【Chris hopper 个人网站】✅ 账户解封通知", html);
        }
        public async Task<bool> SendEmailWithAttachmentsAsync(
    string to,
    string subject,
    string htmlContent,
    List<(byte[] Data, string Name)>? attachments = null)
{
    try
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            Console.WriteLine("⚠️ Brevo API Key 未配置");
            return false;
        }

        var attachmentList = new List<object>();
        if (attachments != null && attachments.Any())
        {
            // 限制最多200个附件
            var limited = attachments.Take(200).ToList();
            foreach (var (data, name) in limited)
            {
                attachmentList.Add(new
                {
                    content = Convert.ToBase64String(data),
                    name = name
                });
            }
        }

        var requestPayload = new
        {
            sender = new { email = "chris@chris-hopper.org", name = "Chris hopper 个人网站" },
            to = new[] { new { email = to } },
            subject = subject,
            htmlContent = htmlContent,
            attachment = attachmentList.Any() ? attachmentList : null
        };

        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(requestPayload, options);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);

        var response = await _httpClient.PostAsync("https://api.brevo.com/v3/smtp/email", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"✅ 邮件发送成功: {to}, 附件数: {attachmentList.Count}");
            return true;
        }
        else
        {
            Console.WriteLine($"❌ 邮件发送失败 ({response.StatusCode}): {responseBody}");
            return false;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ 邮件发送异常: {ex.Message}");
        return false;
    }
}
        public async Task SendResourceResultEmailWithAttachmentsAsync(
    ResourceRequest request, 
    List<(byte[] Data, string Name)>? attachments)
{
    var timestamp = DateTime.Now.ToString("yyyyMMddHHmm");
    var subject = $"【{request.PersonName}】资源内容-{timestamp}";

    var foundTypes = string.IsNullOrEmpty(request.FoundTypes) ? "无" : request.FoundTypes;
    var notFoundTypes = string.IsNullOrEmpty(request.NotFoundTypes) ? "无" : request.NotFoundTypes;

    var attachmentInfo = attachments != null && attachments.Any() 
        ? $"<p style='color: #8B5CF6;'>📎 附件数量：{attachments.Count} 个</p>" 
        : "<p style='color: rgba(255,255,255,0.1);'>📎 无附件</p>";

    var html = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #2a2a3e; border-radius: 16px; background: #0a0a0f; color: #e0e0e0;'>
            <h2 style='color: #8B5CF6;'>📦 资源处理结果</h2>
            <p>您好 <strong>{request.UserName}</strong>！</p>
            <p>您的资源申请已处理完成：</p>

            <div style='background: #1a1a2e; padding: 15px; border-radius: 8px; margin: 10px 0; border: 1px solid #2a2a3e;'>
                <p><strong>👤 联系人：</strong>{request.PersonName}</p>
                <p><strong>📱 平台：</strong>{request.Platform1}{(string.IsNullOrEmpty(request.Platform2) ? "" : " + " + request.Platform2)}</p>
                <p><strong>📂 资源名称：</strong>{request.ResourceName}</p>
                <p><strong>📊 状态：</strong>{(request.Status == "completed" ? "✅ 已完成" : request.Status == "rejected" ? "❌ 已拒绝" : request.Status == "refunded" ? "💰 已退款" : "⏳ 处理中")}</p>
            </div>

            <div style='background: #1a1a2e; padding: 15px; border-radius: 8px; margin: 10px 0; border: 1px solid #2a2a3e;'>
                <p><strong>✅ 已找到类型：</strong><span style='color: #00FF88;'>{foundTypes}</span></p>
                <p><strong>❌ 未找到类型：</strong><span style='color: #dc3545;'>{notFoundTypes}</span></p>
                {attachmentInfo}
                {(string.IsNullOrEmpty(request.AdminNote) ? "" : $@"<p><strong>📝 管理员备注：</strong>{request.AdminNote}</p>")}
            </div>

            <p style='color: #888; font-size: 14px;'>⏰ 处理时间：{request.ProcessedAt:yyyy-MM-dd HH:mm:ss}</p>

            <hr style='border: none; border-top: 1px solid #2a2a3e;'>
            <p style='color: #555; font-size: 12px;'>此邮件为系统发送，请勿回复</p>
        </div>";

    await SendEmailWithAttachmentsAsync(request.UserEmail, subject, html, attachments);
}
    }
}
