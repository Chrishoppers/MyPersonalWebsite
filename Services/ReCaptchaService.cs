using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace MyPersonalWebsite.Services
{
    public class ReCaptchaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _secretKey;

        public ReCaptchaService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _secretKey = configuration["ReCaptcha:SecretKey"] ?? "";
        }

        public async Task<bool> VerifyAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("❌ Token 为空");
                return false;
            }

            try
            {
                // ⭐ 使用 recaptcha.cn（国内镜像）
                var url = $"https://www.recaptcha.cn/recaptcha/api/siteverify?secret={_secretKey}&response={token}";
                Console.WriteLine($"🔍 请求 URL: {url.Replace(_secretKey, "***")}");

                var response = await _httpClient.PostAsync(url, null);
                var json = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"🔍 响应内容: {json}");

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("success", out var successElement))
                {
                    var success = successElement.GetBoolean();
                    Console.WriteLine($"✅ Success: {success}");
                    return success;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 异常: {ex.Message}");
                return false;
            }
        }
    }
}
