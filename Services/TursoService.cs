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
        private readonly string _dbName;

        public TursoService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            var url = Environment.GetEnvironmentVariable("TURSO_DATABASE_URL") ?? "";
            _authToken = Environment.GetEnvironmentVariable("TURSO_AUTH_TOKEN") ?? "";
            
            // 从 URL 中提取数据库名称
            // https://chris-chrishoppers.aws-ap-northeast-1.turso.io
            if (!string.IsNullOrEmpty(url))
            {
                if (url.StartsWith("libsql://"))
                    url = url.Replace("libsql://", "https://");
                
                // 提取数据库名（URL 中第一个 . 前面的部分）
                var host = new Uri(url).Host;
                _dbName = host.Split('.')[0];  // chris-chrishoppers
                _databaseUrl = $"https://{host}";
            }
            else
            {
                _databaseUrl = "";
                _dbName = "";
            }

            if (!string.IsNullOrEmpty(_databaseUrl) && !string.IsNullOrEmpty(_authToken))
                Console.WriteLine($"✅ Turso 配置: DB={_dbName}, URL={_databaseUrl}");
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
                // Turso v1 API 格式（官方文档）
                var payload = new
                {
                    sqls = new[] { sql }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");

                var url = $"{_databaseUrl}/v1/pipeline";
                Console.WriteLine($"📤 请求 Turso: {url}");
                
                var response = await _httpClient.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"⚠️ Turso 执行失败 ({response.StatusCode}): {responseBody}");
                    return false;
                }
                
                Console.WriteLine($"✅ Turso 执行成功: {sql.Substring(0, Math.Min(50, sql.Length))}...");
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
                    sqls = new[] { sql }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");

                var url = $"{_databaseUrl}/v1/pipeline";
                Console.WriteLine($"📤 查询 Turso: {url}");
                
                var response = await _httpClient.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"⚠️ Turso 查询失败 ({response.StatusCode}): {responseBody}");
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
