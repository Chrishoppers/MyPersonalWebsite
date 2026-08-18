using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MyPersonalWebsite.Services
{
    public class PlatformVerifyService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PlatformVerifyService> _logger;

        public PlatformVerifyService(HttpClient httpClient, ILogger<PlatformVerifyService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// 验证平台用户ID是否存在
        /// </summary>
        public async Task<(bool IsValid, string Message, string? DisplayName)> VerifyPlatformUserAsync(string platform, string userId)
        {
            if (string.IsNullOrEmpty(platform) || string.IsNullOrEmpty(userId))
                return (false, "平台和ID不能为空", null);

            platform = platform.ToLower();

            try
            {
                switch (platform)
                {
                    case "douyin":
                        return await VerifyDouyinAsync(userId);
                    case "kuaishou":
                        return await VerifyKuaishouAsync(userId);
                    case "bilibili":
                    case "b站":
                        return await VerifyBilibiliAsync(userId);
                    case "xiaohongshu":
                    case "小红书":
                        return await VerifyXiaohongshuAsync(userId);
                    case "weibo":
                    case "微博":
                        return await VerifyWeiboAsync(userId);
                    case "zhihu":
                    case "知乎":
                        return await VerifyZhihuAsync(userId);
                    default:
                        // 不支持的平台，跳过验证（允许用户提交）
                        return (true, "平台不支持自动验证，请管理员手动核实", null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"平台验证失败 ({platform}): {ex.Message}");
                return (false, $"验证失败: {ex.Message}", null);
            }
        }

        /// <summary>
        /// 验证抖音用户（通过公开API）
        /// </summary>
        private async Task<(bool IsValid, string Message, string? DisplayName)> VerifyDouyinAsync(string userId)
        {
            // 抖音用户主页: https://www.douyin.com/user/{userId}
            // 注意：抖音可能需要模拟浏览器请求，这里使用HEAD请求检查页面是否存在
            
            try
            {
                var url = $"https://www.douyin.com/user/{userId}";
                var request = new HttpRequestMessage(HttpMethod.Head, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                
                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    return (true, "✅ 抖音用户存在", null);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return (false, "❌ 抖音用户不存在", null);
                }
                else
                {
                    return (false, $"⚠️ 验证失败 (HTTP {response.StatusCode})", null);
                }
            }
            catch (Exception ex)
            {
                // 如果请求失败，可能是反爬，返回未知状态
                return (true, "⚠️ 无法自动验证，请管理员手动核实", null);
            }
        }

        /// <summary>
        /// 验证快手用户
        /// </summary>
        private async Task<(bool IsValid, string Message, string? DisplayName)> VerifyKuaishouAsync(string userId)
        {
            try
            {
                var url = $"https://www.kuaishou.com/profile/{userId}";
                var request = new HttpRequestMessage(HttpMethod.Head, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                
                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    return (true, "✅ 快手用户存在", null);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return (false, "❌ 快手用户不存在", null);
                }
                else
                {
                    return (false, $"⚠️ 验证失败 (HTTP {response.StatusCode})", null);
                }
            }
            catch
            {
                return (true, "⚠️ 无法自动验证，请管理员手动核实", null);
            }
        }

        /// <summary>
        /// 验证B站用户
        /// </summary>
        private async Task<(bool IsValid, string Message, string? DisplayName)> VerifyBilibiliAsync(string userId)
        {
            try
            {
                // B站API: https://api.bilibili.com/x/space/acc/info?mid={userId}
                var url = $"https://api.bilibili.com/x/space/acc/info?mid={userId}";
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    
                    var code = root.GetProperty("code").GetInt32();
                    if (code == 0)
                    {
                        var data = root.GetProperty("data");
                        var name = data.GetProperty("name").GetString();
                        return (true, $"✅ B站用户存在", name);
                    }
                    else if (code == -404)
                    {
                        return (false, "❌ B站用户不存在", null);
                    }
                    else
                    {
                        return (false, $"⚠️ B站验证失败 (code: {code})", null);
                    }
                }
                else
                {
                    // 降级：检查用户主页
                    var homeUrl = $"https://space.bilibili.com/{userId}";
                    var headRequest = new HttpRequestMessage(HttpMethod.Head, homeUrl);
                    headRequest.Headers.Add("User-Agent", "Mozilla/5.0");
                    var headResponse = await _httpClient.SendAsync(headRequest);
                    
                    if (headResponse.IsSuccessStatusCode)
                    {
                        return (true, "✅ B站用户存在", null);
                    }
                    else
                    {
                        return (false, "❌ B站用户不存在", null);
                    }
                }
            }
            catch
            {
                return (true, "⚠️ 无法自动验证，请管理员手动核实", null);
            }
        }

        /// <summary>
        /// 验证小红书用户
        /// </summary>
        private async Task<(bool IsValid, string Message, string? DisplayName)> VerifyXiaohongshuAsync(string userId)
        {
            try
            {
                var url = $"https://www.xiaohongshu.com/user/profile/{userId}";
                var request = new HttpRequestMessage(HttpMethod.Head, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                
                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    return (true, "✅ 小红书用户存在", null);
                }
                else
                {
                    return (true, "⚠️ 无法自动验证，请管理员手动核实", null);
                }
            }
            catch
            {
                return (true, "⚠️ 无法自动验证，请管理员手动核实", null);
            }
        }

        /// <summary>
        /// 验证微博用户
        /// </summary>
        private async Task<(bool IsValid, string Message, string? DisplayName)> VerifyWeiboAsync(string userId)
        {
            try
            {
                var url = $"https://weibo.com/u/{userId}";
                var request = new HttpRequestMessage(HttpMethod.Head, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                
                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    return (true, "✅ 微博用户存在", null);
                }
                else
                {
                    return (true, "⚠️ 无法自动验证，请管理员手动核实", null);
                }
            }
            catch
            {
                return (true, "⚠️ 无法自动验证，请管理员手动核实", null);
            }
        }

        /// <summary>
        /// 验证知乎用户
        /// </summary>
        private async Task<(bool IsValid, string Message, string? DisplayName)> VerifyZhihuAsync(string userId)
        {
            try
            {
                var url = $"https://www.zhihu.com/people/{userId}";
                var request = new HttpRequestMessage(HttpMethod.Head, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                
                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    return (true, "✅ 知乎用户存在", null);
                }
                else
                {
                    return (true, "⚠️ 无法自动验证，请管理员手动核实", null);
                }
            }
            catch
            {
                return (true, "⚠️ 无法自动验证，请管理员手动核实", null);
            }
        }
    }
}
