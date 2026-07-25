using MyPersonalWebsite.Models;
using System.Text.Json;

namespace MyPersonalWebsite.Services
{
    public class GameSuggestionService
    {
        private readonly TursoService _tursoService;
        private readonly DataSyncService _dataSync;
        private readonly bool _tursoAvailable;

        public GameSuggestionService(TursoService tursoService, DataSyncService dataSync)
        {
            _tursoService = tursoService;
            _dataSync = dataSync;
            var url = Environment.GetEnvironmentVariable("TURSO_DATABASE_URL") ?? "";
            var token = Environment.GetEnvironmentVariable("TURSO_AUTH_TOKEN") ?? "";
            _tursoAvailable = !string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(token);
        }

        // ============================================================
        // 获取所有建议（按投票数排序）
        // ============================================================
        public async Task<List<GameSuggestion>> GetAllSuggestionsAsync()
        {
            if (!_tursoAvailable) return new List<GameSuggestion>();

            var result = await _tursoService.QueryAsync(
                "SELECT * FROM GameSuggestions ORDER BY Votes DESC, CreatedAt DESC"
            );
            return ParseSuggestionList(result);
        }

        // ============================================================
        // 获取建议详情
        // ============================================================
        public async Task<GameSuggestion?> GetSuggestionByIdAsync(int id)
        {
            if (!_tursoAvailable) return null;

            var result = await _tursoService.QueryAsync($"SELECT * FROM GameSuggestions WHERE Id = {id}");
            return ParseSuggestion(result);
        }

        // ============================================================
        // 添加建议
        // ============================================================
        public async Task<bool> AddSuggestionAsync(int userId, string gameName, string? description)
        {
            if (!_tursoAvailable) return false;

            var maxIdResult = await _tursoService.QueryAsync("SELECT MAX(Id) as MaxId FROM GameSuggestions");
            var maxId = ParseMaxId(maxIdResult);
            var newId = maxId + 1;

            var sql = $@"INSERT INTO GameSuggestions (
                Id, UserId, GameName, Description, Votes, Status, CreatedAt
            ) VALUES (
                {newId}, {userId}, '{EscapeSql(gameName)}',
                {(string.IsNullOrEmpty(description) ? "NULL" : $"'{EscapeSql(description)}'")},
                0, 'pending', '{DateTime.Now:yyyy-MM-dd HH:mm:ss}'
            )";

            var result = await _tursoService.ExecuteSqlAsync(sql);
            return result;
        }

        // ============================================================
        // 投票
        // ============================================================
        public async Task<(bool Success, string Message)> ToggleVoteAsync(int suggestionId, int userId)
        {
            if (!_tursoAvailable) return (false, "系统不可用");

            // 检查是否已投票
            var checkResult = await _tursoService.QueryAsync(
                $"SELECT * FROM GameSuggestionVotes WHERE SuggestionId = {suggestionId} AND UserId = {userId}"
            );

            var hasVoted = !checkResult.Contains("\"rows\":[]");

            if (hasVoted)
            {
                // 取消投票
                await _tursoService.ExecuteSqlAsync(
                    $"DELETE FROM GameSuggestionVotes WHERE SuggestionId = {suggestionId} AND UserId = {userId}"
                );
                await _tursoService.ExecuteSqlAsync(
                    $"UPDATE GameSuggestions SET Votes = Votes - 1 WHERE Id = {suggestionId}"
                );
                return (true, "已取消投票");
            }
            else
            {
                // 投票
                await _tursoService.ExecuteSqlAsync($@"
                    INSERT INTO GameSuggestionVotes (SuggestionId, UserId, VotedAt)
                    VALUES ({suggestionId}, {userId}, '{DateTime.Now:yyyy-MM-dd HH:mm:ss}')
                ");
                await _tursoService.ExecuteSqlAsync(
                    $"UPDATE GameSuggestions SET Votes = Votes + 1 WHERE Id = {suggestionId}"
                );
                return (true, "投票成功！");
            }
        }

        // ============================================================
        // 更新状态（管理员）
        // ============================================================
        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            if (!_tursoAvailable) return false;

            var validStatuses = new[] { "pending", "approved", "developing", "completed", "rejected" };
            if (!validStatuses.Contains(status)) return false;

            await _tursoService.ExecuteSqlAsync($@"
                UPDATE GameSuggestions SET
                    Status = '{status}',
                    UpdatedAt = '{DateTime.Now:yyyy-MM-dd HH:mm:ss}'
                WHERE Id = {id}
            ");
            return true;
        }

        // ============================================================
        // 检查用户是否已投票
        // ============================================================
        public async Task<bool> HasUserVotedAsync(int suggestionId, int userId)
        {
            if (!_tursoAvailable) return false;

            var result = await _tursoService.QueryAsync(
                $"SELECT * FROM GameSuggestionVotes WHERE SuggestionId = {suggestionId} AND UserId = {userId}"
            );
            return !result.Contains("\"rows\":[]");
        }

        // ============================================================
        // 删除建议（管理员）
        // ============================================================
        public async Task<bool> DeleteSuggestionAsync(int id)
        {
            if (!_tursoAvailable) return false;

            // 先删除所有投票
            await _tursoService.ExecuteSqlAsync($"DELETE FROM GameSuggestionVotes WHERE SuggestionId = {id}");
            await _tursoService.ExecuteSqlAsync($"DELETE FROM GameSuggestions WHERE Id = {id}");
            return true;
        }

        // ============================================================
        // 解析方法
        // ============================================================
        private List<GameSuggestion> ParseSuggestionList(string json)
        {
            var list = new List<GameSuggestion>();
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
                {
                    var firstResult = results[0];
                    if (firstResult.TryGetProperty("response", out var response) &&
                        response.TryGetProperty("result", out var result))
                    {
                        if (result.TryGetProperty("rows", out var rows) && rows.GetArrayLength() > 0)
                        {
                            var cols = result.GetProperty("cols");

                            for (int r = 0; r < rows.GetArrayLength(); r++)
                            {
                                var row = rows[r];
                                if (row.ValueKind != JsonValueKind.Array) continue;

                                var s = new GameSuggestion();
                                for (int i = 0; i < cols.GetArrayLength(); i++)
                                {
                                    var colName = cols[i].GetProperty("name").GetString();
                                    var element = row[i];

                                    switch (colName)
                                    {
                                        case "Id": s.Id = GetIntFromRow(element); break;
                                        case "UserId": s.UserId = GetIntFromRow(element); break;
                                        case "GameName": s.GameName = GetStringFromRow(element); break;
                                        case "Description": s.Description = GetStringOrNullFromRow(element); break;
                                        case "Votes": s.Votes = GetIntFromRow(element); break;
                                        case "Status": s.Status = GetStringFromRow(element); break;
                                        case "CreatedAt": s.CreatedAt = GetDateTimeFromRow(element) ?? DateTime.Now; break;
                                        case "UpdatedAt": s.UpdatedAt = GetDateTimeFromRow(element); break;
                                    }
                                }
                                list.Add(s);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ 解析建议列表 JSON 失败: {ex.Message}");
            }
            return list;
        }

        private GameSuggestion? ParseSuggestion(string json)
        {
            var list = ParseSuggestionList(json);
            return list.FirstOrDefault();
        }

        private int ParseMaxId(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
                {
                    var firstResult = results[0];
                    if (firstResult.TryGetProperty("response", out var response) &&
                        response.TryGetProperty("result", out var result))
                    {
                        if (result.TryGetProperty("rows", out var rows) && rows.GetArrayLength() > 0)
                        {
                            var row = rows[0];
                            if (row.ValueKind == JsonValueKind.Array && row.GetArrayLength() > 0)
                            {
                                var val = GetValueFromRow(row[0]);
                                if (val is JsonElement je && je.ValueKind != JsonValueKind.Null)
                                {
                                    if (je.ValueKind == JsonValueKind.Number)
                                        return je.GetInt32();
                                    if (je.ValueKind == JsonValueKind.String)
                                        return int.TryParse(je.GetString(), out var parsed) ? parsed : 0;
                                }
                            }
                        }
                    }
                }
                return 0;
            }
            catch { return 0; }
        }

        // ============================================================
        // 工具方法
        // ============================================================
        private string EscapeSql(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("'", "''");
        }

        private object? GetValueFromRow(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("value", out var val))
                    return val;
                if (element.TryGetProperty("Value", out var val2))
                    return val2;
                return element;
            }
            return element;
        }

        private int GetIntFromRow(JsonElement element)
        {
            try
            {
                var val = GetValueFromRow(element);
                if (val is JsonElement je)
                {
                    if (je.ValueKind == JsonValueKind.Null) return 0;
                    if (je.ValueKind == JsonValueKind.Number) return je.GetInt32();
                    if (je.ValueKind == JsonValueKind.String)
                    {
                        var str = je.GetString();
                        if (int.TryParse(str, out var result))
                            return result;
                        return 0;
                    }
                    return 0;
                }
                var strVal = val?.ToString();
                if (int.TryParse(strVal, out var result2))
                    return result2;
                return 0;
            }
            catch { return 0; }
        }

        private string GetStringFromRow(JsonElement element)
        {
            try
            {
                var val = GetValueFromRow(element);
                if (val is JsonElement je)
                    return je.ValueKind == JsonValueKind.Null ? "" : je.GetString() ?? "";
                return val?.ToString() ?? "";
            }
            catch { return ""; }
        }

        private string? GetStringOrNullFromRow(JsonElement element)
        {
            try
            {
                var val = GetValueFromRow(element);
                if (val is JsonElement je)
                {
                    if (je.ValueKind == JsonValueKind.Null) return null;
                    return je.GetString();
                }
                return val?.ToString();
            }
            catch { return null; }
        }

        private DateTime? GetDateTimeFromRow(JsonElement element)
        {
            try
            {
                var val = GetStringFromRow(element);
                if (string.IsNullOrEmpty(val)) return null;
                return DateTime.Parse(val);
            }
            catch { return null; }
        }
    }
}
