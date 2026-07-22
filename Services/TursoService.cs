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
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            var url = Environment.GetEnvironmentVariable("TURSO_DATABASE_URL") ?? "";
            _authToken = Environment.GetEnvironmentVariable("TURSO_AUTH_TOKEN") ?? "";
            
            if (!string.IsNullOrEmpty(url))
            {
                if (url.StartsWith("libsql://"))
                    url = url.Replace("libsql://", "https://");
                
                _databaseUrl = url.TrimEnd('/');
            }
            else
            {
                _databaseUrl = "";
            }

            if (!string.IsNullOrEmpty(_databaseUrl) && !string.IsNullOrEmpty(_authToken))
                Console.WriteLine($"✅ Turso 配置: URL={_databaseUrl}");
            else
                Console.WriteLine("⚠️ Turso 未配置");
        }

        public async Task<bool> ExecuteSqlAsync(string sql)
        {
            if (string.IsNullOrEmpty(_databaseUrl) || string.IsNullOrEmpty(_authToken))
            {
                Console.WriteLine("⚠️ Turso 未配置，跳过执行");
                return false;
            }

            try
            {
                var payload = new
                {
                    requests = new[]
                    {
                        new
                        {
                            type = "execute",
                            stmt = new
                            {
                                sql = sql,
                                args = new object[] { }
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");

                var url = $"{_databaseUrl}/v2/pipeline";
                
                var response = await _httpClient.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"⚠️ Turso 执行失败 ({response.StatusCode}): {responseBody.Substring(0, Math.Min(200, responseBody.Length))}");
                    return false;
                }
                
                Console.WriteLine($"✅ Turso 执行成功");
                return true;
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("⚠️ Turso 请求超时");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Turso 执行异常: {ex.Message}");
                return false;
            }
        }

        public async Task<string> QueryAsync(string sql)
        {
            if (string.IsNullOrEmpty(_databaseUrl) || string.IsNullOrEmpty(_authToken))
            {
                Console.WriteLine("⚠️ Turso 未配置，返回空结果");
                return "{}";
            }

            try
            {
                var payload = new
                {
                    requests = new[]
                    {
                        new
                        {
                            type = "execute",
                            stmt = new
                            {
                                sql = sql,
                                args = new object[] { }
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");

                var url = $"{_databaseUrl}/v2/pipeline";
                
                var response = await _httpClient.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"⚠️ Turso 查询失败 ({response.StatusCode}): {responseBody.Substring(0, Math.Min(200, responseBody.Length))}");
                    return "{}";
                }
                
                Console.WriteLine($"✅ Turso 查询成功");
                return responseBody;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Turso 查询异常: {ex.Message}");
                return "{}";
            }
        }
    }
}
