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

            try
            {
                var url = $"https://www.google.com/recaptcha/api/siteverify?secret={_secretKey}&response={token}";
                Console.WriteLine($"🔍 请求 URL: {url.Replace(_secretKey, "***")}");

                var response = await _httpClient.PostAsync(url, null);
                var json = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"🔍 响应内容: {json}");

                // ⭐ 直接用 JsonDocument 解析，避免字段名大小写问题
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("success", out var successElement))
                {
                    var success = successElement.GetBoolean();
                    Console.WriteLine($"✅ Success: {success}");

                    if (!success && root.TryGetProperty("error-codes", out var errorCodesElement))
                    {
                        var errors = errorCodesElement.EnumerateArray();
                        foreach (var err in errors)
                        {
                            Console.WriteLine($"❌ 错误码: {err.GetString()}");
                        }
                    }

                    return success;
                }

                Console.WriteLine("❌ 无法解析 success 字段");
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
