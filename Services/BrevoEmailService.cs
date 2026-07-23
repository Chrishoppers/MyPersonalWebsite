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
        // ⭐ 辅助方法：UTC 转中国时区
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
                // 如果时区转换失败，直接返回原时间
                return utcTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
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

            // ⭐ 转换为中国时区
            var registerTimeStr = FormatChinaTime(registerTime);

            // ⭐ 完整头像 URL
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

      // ============================================================
// 头像审核邮件（管理员）- Base64 直接显示
// ============================================================

public async Task SendAdminAvatarVerificationAsync(string username, string email, int userId, string avatarData, DateTime submittedAt)
{
    var baseUrl = "https://chris-hopper.org";
    var approveUrl = $"{baseUrl}/Admin/ApproveAvatar?userId={userId}";
    var rejectUrl = $"{baseUrl}/Admin/RejectAvatar?userId={userId}";

    // ⭐ 头像已经是 data:image/xxx;base64, 直接使用
    var avatarHtml = string.IsNullOrEmpty(avatarData)
        ? "<p style='color:#555;'>未上传头像</p>"
        : $"<img src='{avatarData}' style='width:120px;height:120px;border-radius:50%;object-fit:cover;border:2px solid #f59e0b;' />";

    // ⭐ 转换为中国时区
    var submittedAtStr = FormatChinaTime(submittedAt);

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
                <div style='margin:10px 0;'>
                    {avatarHtml}
                </div>
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
                <p><strong>时间：</strong>{DateTime.Now:yyyy-MM-dd HH:mm}</p>
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
        // 10. 管理员回复留言通知（发送给用户）
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
        // 11. 管理员通知：新授权码申请
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
        // 12. 用户操作通知
        // ============================================================

        // ============================================================
// 用户操作通知（含自动登录的"查看详情"按钮）
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

    // ⭐ 自动登录链接（用于"查看详情"按钮）
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
    }
}
