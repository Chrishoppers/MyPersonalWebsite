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
            try
            {
                var request = new
                {
                    sqls = new[] { sql }
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");

                var response = await _httpClient.PostAsync($"{_databaseUrl}/v1/pipeline", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> QueryAsync(string sql)
        {
            try
            {
                var request = new
                {
                    sqls = new[] { sql }
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");

                var response = await _httpClient.PostAsync($"{_databaseUrl}/v1/pipeline", content);
                var result = await response.Content.ReadAsStringAsync();
                return result;
            }
            catch
            {
                return "{}";
            }
        }
    }
}
