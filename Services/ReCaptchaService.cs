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
                return false;

            try
            {
                // ⭐ 使用 recaptcha.net 镜像（国内可访问）
                var response = await _httpClient.PostAsync(
                    $"https://www.recaptcha.net/recaptcha/api/siteverify?secret={_secretKey}&response={token}",
                    null
                );

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ReCaptchaResponse>(json);

                return result?.Success == true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"reCAPTCHA 验证失败: {ex.Message}");
                return false;
            }
        }

        private class ReCaptchaResponse
        {
            public bool Success { get; set; }
            public string? ChallengeTs { get; set; }
            public string? Hostname { get; set; }
            public string[]? ErrorCodes { get; set; }
        }
    }
}
