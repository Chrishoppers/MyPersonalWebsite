using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MyPersonalWebsite.Services
{
    public class PlatformVerifyService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PlatformVerifyService> _logger;

        // ⭐ 我的各平台账号（用户需要关注我）
        private readonly Dictionary<string, string> _myAccounts = new()
        {
            { "抖音", "chris_hopper" },
            { "快手", "Chris_hopper" },
            { "B站", "chris_hopper" },
            { "小红书", "chris_hopper" },
            { "微博", "chris_hopper" },
            { "知乎", "chris_hopper" }
        };

        public PlatformVerifyService(HttpClient httpClient, ILogger<PlatformVerifyService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public string GetMyAccount(string platform)
        {
            return _myAccounts.GetValueOrDefault(platform, "");
        }

        public List<string> GetSupportedPlatforms()
        {
            return new List<string> { "抖音", "快手", "B站", "小红书", "微博", "知乎" };
        }

        /// <summary>
        /// 验证是否关注了我
        /// 返回: (是否验证通过, 显示消息, 用户显示名, 验证状态)
        /// </summary>
        public async Task<(bool IsValid, string Message, string? DisplayName, string VerifyStatus)> VerifyFollowAsync(
            string platform,
            string accountId)
        {
            if (string.IsNullOrEmpty(platform) || string.IsNullOrEmpty(accountId))
                return (false, "平台和账号ID不能为空", null, "pending");

            var myAccount = _myAccounts.GetValueOrDefault(platform, "");
            if (string.IsNullOrEmpty(myAccount))
                return (false, $"⚠️ 平台 '{platform}' 暂不支持验证", null, "pending");

            try
            {
                switch (platform)
                {
                    case "抖音":
                        return await VerifyDouyinFollowAsync(accountId, myAccount);
                    case "快手":
                        return await VerifyKuaishouFollowAsync(accountId, myAccount);
                    case "B站":
                        return await VerifyBilibiliFollowAsync(accountId, myAccount);
                    case "小红书":
                        return await VerifyXiaohongshuFollowAsync(accountId, myAccount);
                    case "微博":
                        return await VerifyWeiboFollowAsync(accountId, myAccount);
                    case "知乎":
                        return await VerifyZhihuFollowAsync(accountId, myAccount);
                    default:
                        return (false, $"⚠️ 平台 '{platform}' 暂不支持验证", null, "pending");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"关注验证失败 ({platform}): {ex.Message}");
                return (false, $"验证失败: {ex.Message}", null, "pending");
            }
        }

        // ============================================================
        // 各平台验证方法
        // ============================================================

        /// <summary>
        /// 验证抖音关注
        /// </summary>
        private async Task<(bool IsValid, string Message, string? DisplayName, string VerifyStatus)> VerifyDouyinFollowAsync(
            string userId,
            string myAccount)
        {
            try
            {
                var url = $"https://www.douyin.com/user/{userId}";
                var request = new HttpRequestMessage(HttpMethod.Head, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return (true, $"✅ 用户 {userId} 存在，请提交申请后等待管理员手动确认是否关注 @{myAccount}", null, "manual_required");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return (false, $"❌ 用户 {userId} 不存在，请检查账号ID是否正确", null, "rejected");
                }
                else
                {
                    return (false, $"⚠️ 无法自动验证 (HTTP {response.StatusCode})，请重试或联系管理员", null, "manual_required");
                }
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("403"))
            {
                return (false, $"⚠️ 抖音反爬限制，需要管理员人工核验是否关注 @{myAccount}", null, "manual_required");
            }
            catch
            {
                return (false, $"⚠️ 无法自动验证，需要管理员人工核验是否关注 @{myAccount}", null, "manual_required");
            }
        }

        /// <summary>
        /// 验证快手关注
        /// </summary>
        private async Task<(bool IsValid, string Message, string? DisplayName, string VerifyStatus)> VerifyKuaishouFollowAsync(
            string userId,
            string myAccount)
        {
            try
            {
                var url = $"https://www.kuaishou.com/profile/{userId}";
                var request = new HttpRequestMessage(HttpMethod.Head, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return (true, $"✅ 用户 {userId} 存在，请提交申请后等待管理员手动确认是否关注 @{myAccount}", null, "manual_required");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return (false, $"❌ 用户 {userId} 不存在，请检查账号ID是否正确", null, "rejected");
                }
                else
                {
                    return (false, $"⚠️ 无法自动验证 (HTTP {response.StatusCode})，请重试或联系管理员", null, "manual_required");
                }
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("403"))
            {
                return (false, $"⚠️ 快手反爬限制，需要管理员人工核验是否关注 @{myAccount}", null, "manual_required");
            }
            catch
            {
                return (false, $"⚠️ 无法自动验证，需要管理员人工核验是否关注 @{myAccount}", null, "manual_required");
            }
        }

        /// <summary>
        /// 验证B站关注
        /// </summary>
        private async Task<(bool IsValid, string Message, string? DisplayName, string VerifyStatus)> VerifyBilibiliFollowAsync(
            string userId,
            string myAccount)
        {
            try
            {
                var url = $"https://api.bilibili.com/x/space/acc/info?mid={userId}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("code", out var codeElement))
                    {
                        var code = codeElement.GetInt32();
                        if (code == 0)
                        {
                            var data = root.GetProperty("data");
                            var name = data.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : userId;
                            return (true, $"✅ B站用户 {name} 存在，请提交申请后等待管理员手动确认是否关注 @{myAccount}", name, "manual_required");
                        }
                        else if (code == -404)
                        {
                            return (false, $"❌ B站用户 {userId} 不存在，请检查账号ID是否正确", null, "rejected");
                        }
                        else
                        {
                            return (false, $"⚠️ B站API返回未知状态 (code: {code})，请重试或联系管理员", null, "manual_required");
                        }
                    }
                    else
                    {
                        return (false, $"⚠️ B站API响应异常，需要管理员人工核验", null, "manual_required");
                    }
                }
                else
                {
                    var homeUrl = $"https://space.bilibili.com/{userId}";
                    var headRequest = new HttpRequestMessage(HttpMethod.Head, homeUrl);
                    headRequest.Headers.Add("User-Agent", "Mozilla/5.0");

                    var headResponse = await _httpClient.SendAsync(headRequest);

                    if (headResponse.IsSuccessStatusCode)
                    {
                        return (true, $"✅ B站用户 {userId} 存在，请提交申请后等待管理员手动确认是否关注 @{myAccount}", null, "manual_required");
                    }
                    else
                    {
                        return (false, $"❌ B站用户 {userId} 不存在，请检查账号ID是否正确", null, "rejected");
                    }
                }
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("403"))
            {
                return (false, $"⚠️ B站反爬限制，需要管理员人工核验是否关注 @{myAccount}", null, "manual_required");
            }
            catch
            {
                return (false, $"⚠️ 无法自动验证，需要管理员人工核验是否关注 @{myAccount}", null, "manual_required");
            }
        }

        /// <summary>
        /// 验证小红书关注
        /// </summary>
        private async Task<(bool IsValid, string Message, string? DisplayName, string VerifyStatus)> VerifyXiaohongshuFollowAsync(
            string userId,
            string myAccount)
        {
            try
            {
                var url = $"https://www.xiaohongshu.com/user/profile/{userId}";
                var request = new HttpRequestMessage(HttpMethod.Head, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return (true, $"✅ 用户 {userId} 存在，请提交申请后等待管理员手动确认是否关注 @{myAccount}", null, "manual_required");
                }
                else
                {
                    return (false, $"⚠️ 无法自动验证，需要管理员人工核验是否关注 @{myAccount}", null, "manual_required");
                }
            }
            catch
            {
                return (false, $"⚠️ 无法自动验证，需要管理员人工核验是否关注 @{myAccount}", null, "manual_required");
            }
        }

        /// <summary>
        /// 验证微博关注
        /// </summary>
        private async Task<(bool IsValid, string Message, string? DisplayName, string VerifyStatus)> VerifyWeiboFollowAsync(
            string userId,
            string myAccount)
        {
            try
            {
                var url = $"https://weibo.com/u/{userId}";
                var request = new HttpRequestMessage(HttpMethod.Head, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return (true, $"✅ 用户 {userId} 存在，请提交申请后等待管理员手动确认是否关注 @{myAccount}", null, "manual_required");
                }
                else
                {
                    return (false, $"⚠️ 无法自动验证，需要管理员人工核验是否关注 @{myAccount}", null, "manual_required");
                }
            }
            catch
            {
                return (false, $"⚠️ 无法自动验证，需要管理员人工核验是否关注 @{myAccount}", null, "manual_required");
            }
        }

        /// <summary>
        /// 验证知乎关注
        /// </summary>
        private async Task<(bool IsValid, string Message, string? DisplayName, string VerifyStatus)> VerifyZhihuFollowAsync(
            string userId,
            string myAccount)
        {
            try
            {
                var url = $"https://www.zhihu.com/people/{userId}";
                var request = new HttpRequestMessage(HttpMethod.Head, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return (true, $"✅ 用户 {userId} 存在，请提交申请后等待管理员手动确认是否关注 @{myAccount}", null, "manual_required");
                }
                else
                {
                    return (false, $"⚠️ 无法自动验证，需要管理员人工核验是否关注 @{myAccount}", null, "manual_required");
                }
            }
            catch
            {
                return (false, $"⚠️ 无法自动验证，需要管理员人工核验是否关注 @{myAccount}", null, "manual_required");
            }
        }

        /// <summary>
        /// 管理员手动验证通过
        /// </summary>
        public (bool IsValid, string Message, string VerifyStatus) ManualVerify(bool approved)
        {
            if (approved)
            {
                return (true, "✅ 管理员已人工核验通过", "auto_verified");
            }
            else
            {
                return (false, "❌ 管理员人工核验未通过", "rejected");
            }
        }
    }
}
