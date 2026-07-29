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
            Console.WriteLine($"🔍 SecretKey 长度: {_secretKey?.Length ?? 0}");
        }

        public async Task<bool> VerifyAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("❌ Token 为空");
                return false;
            }

            Console.WriteLine($"🔍 Token 前20字符: {token.Substring(0, Math.Min(20, token.Length))}...");
            Console.WriteLine($"🔍 SecretKey: {_secretKey}");

            try
            {
                // 尝试用 google.com（备用）
                var url = $"https://www.google.com/recaptcha/api/siteverify?secret={_secretKey}&response={token}";
                Console.WriteLine($"🔍 请求 URL: {url}");

                var response = await _httpClient.PostAsync(url, null);
                var json = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"🔍 响应内容: {json}");

                var result = JsonSerializer.Deserialize<ReCaptchaResponse>(json);

                if (result != null)
                {
                    Console.WriteLine($"✅ Success: {result.Success}");
                    if (result.ErrorCodes != null && result.ErrorCodes.Length > 0)
                    {
                        Console.WriteLine($"❌ 错误码: {string.Join(", ", result.ErrorCodes)}");
                    }
                }

                return result?.Success == true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 异常: {ex.Message}");
                return false;
            }
        }

        private class ReCaptchaResponse
        {
            public bool Success { get; set; }
            public string[]? ErrorCodes { get; set; }
        }
    }
}
