using Microsoft.Extensions.Options;
using MyPersonalWebsite.Models;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyPersonalWebsite.Services
{
    public class ReCaptchaService
    {
        private readonly HttpClient _httpClient;
        private readonly ReCaptchaSettings _settings;

        public ReCaptchaService(HttpClient httpClient, IOptions<ReCaptchaSettings> settings)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
        }

        public async Task<bool> VerifyAsync(string token)
        {
            if (string.IsNullOrEmpty(token)) return false;

            // ⭐ 国内用户：把 google.com 改成 recaptcha.net
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://www.recaptcha.net/recaptcha/api/siteverify"
            )
            {
                Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("secret", _settings.SecretKey),
                    new KeyValuePair<string, string>("response", token)
                })
            };

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return false;

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ReCaptchaResponse>(json);

            return result?.Success == true;
        }

        private class ReCaptchaResponse
        {
            public bool Success { get; set; }
        }
    }
}