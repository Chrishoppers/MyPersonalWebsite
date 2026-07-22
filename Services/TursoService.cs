using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyPersonalWebsite.Services
{
    public class TursoService
    {
        private readonly HttpClient _httpClient;
        private readonly string _databaseUrl;
        private readonly string _authToken;

        public TursoService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _databaseUrl = Environment.GetEnvironmentVariable("TURSO_DATABASE_URL") ?? "";
            _authToken = Environment.GetEnvironmentVariable("TURSO_AUTH_TOKEN") ?? "";
        }

        public async Task<bool> ExecuteSqlAsync(string sql)
        {
            if (string.IsNullOrEmpty(_databaseUrl) || string.IsNullOrEmpty(_authToken))
                return false;

            try
            {
                // 确保 URL 使用 HTTPS
                var url = _databaseUrl;
                if (url.StartsWith("libsql://"))
                {
                    url = url.Replace("libsql://", "https://");
                }

                var request = new { sqls = new[] { sql } };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");

                var response = await _httpClient.PostAsync($"{url}/v1/pipeline", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Turso SQL 执行失败: {ex.Message}");
                return false;
            }
        }

        public async Task<string> QueryAsync(string sql)
        {
            if (string.IsNullOrEmpty(_databaseUrl) || string.IsNullOrEmpty(_authToken))
                return "{}";

            try
            {
                var url = _databaseUrl;
                if (url.StartsWith("libsql://"))
                {
                    url = url.Replace("libsql://", "https://");
                }

                var request = new { sqls = new[] { sql } };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");

                var response = await _httpClient.PostAsync($"{url}/v1/pipeline", content);
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Turso 查询失败: {ex.Message}");
                return "{}";
            }
        }
    }
}p
