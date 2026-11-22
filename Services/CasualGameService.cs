using System.Text.Json;

namespace MyPersonalWebsite.Services
{
    /// <summary>
    /// 休闲小游戏统一积分/排行榜服务（按账号 + 游戏类型记录最佳成绩）。
    /// </summary>
    public class CasualGameService
    {
        private readonly DataSyncService _dataSync;

        // 允许的游戏类型白名单（防止 SQL 注入 + 非法类型）
        private static readonly HashSet<string> GameKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "idiom", "typing", "memory", "spy", "word"
        };

        public CasualGameService(DataSyncService dataSync) => _dataSync = dataSync;

        public static bool IsValidGameKey(string? gameKey) =>
            GameKeys.Contains((gameKey ?? "").Trim());

        private class Row
        {
            public int BestScore = 0;
            public int MaxCombo = 0;
            public int GamesPlayed = 0;
        }

        // ============================================================
        // 提交成绩（保留历史最佳）
        // ============================================================
        public async Task<object> SubmitAsync(int userId, string? username, string? gameKey, int score, int combo)
        {
            if (!IsValidGameKey(gameKey))
                return new { ok = false, msg = "无效的游戏类型" };

            var key = gameKey!.Trim().ToLower();
            score = Math.Clamp(score, 0, 1_000_000);
            combo = Math.Clamp(combo, 0, 10_000);

            var existing = await GetRowAsync(userId, key);
            var isNewBest = existing == null || score > existing.BestScore;
            var bestScore = Math.Max(existing?.BestScore ?? 0, score);
            var maxCombo = Math.Max(existing?.MaxCombo ?? 0, combo);
            var gamesPlayed = (existing?.GamesPlayed ?? 0) + 1;

            await UpsertAsync(userId, key, bestScore, maxCombo, gamesPlayed);
            var rank = await GetRankAsync(key, bestScore);

            return new { ok = true, bestScore, maxCombo, gamesPlayed, rank, isNewBest };
        }

        // ============================================================
        // 我的最佳成绩
        // ============================================================
        public async Task<object> GetMyBestAsync(int userId, string? gameKey)
        {
            if (!IsValidGameKey(gameKey))
                return new { ok = false, msg = "无效的游戏类型" };

            var key = gameKey!.Trim().ToLower();
            var row = await GetRowAsync(userId, key);
            if (row == null)
                return new { ok = true, bestScore = 0, maxCombo = 0, gamesPlayed = 0, rank = 0 };

            var rank = await GetRankAsync(key, row.BestScore);
            return new { ok = true, bestScore = row.BestScore, maxCombo = row.MaxCombo, gamesPlayed = row.GamesPlayed, rank = rank };
        }

        // ============================================================
        // 排行榜
        // ============================================================
        public async Task<List<CasualLeaderboardEntry>> GetLeaderboardAsync(string? gameKey, int limit)
        {
            if (!IsValidGameKey(gameKey)) return new List<CasualLeaderboardEntry>();
            var key = gameKey!.Trim().ToLower();
            limit = Math.Clamp(limit, 1, 100);

            var json = await _dataSync.QueryAsync(
                $"SELECT c.UserId, c.BestScore, c.MaxCombo, u.Username FROM CasualGameScores c " +
                $"LEFT JOIN Users u ON u.Id = c.UserId WHERE c.GameKey = '{key}' " +
                $"ORDER BY c.BestScore DESC LIMIT {limit}");

            var list = new List<CasualLeaderboardEntry>();
            try
            {
                ForEachRow(json, (names, cells) =>
                {
                    var e = new CasualLeaderboardEntry();
                    for (int i = 0; i < names.Length; i++)
                    {
                        switch (names[i])
                        {
                            case "UserId": e.UserId = ToInt(cells[i]); break;
                            case "BestScore": e.BestScore = ToInt(cells[i]); break;
                            case "MaxCombo": e.MaxCombo = ToInt(cells[i]); break;
                            case "Username": e.Username = ToStr(cells[i]); break;
                        }
                    }
                    list.Add(e);
                });
            }
            catch (Exception ex) { Console.WriteLine($"⚠️ 解析休闲游戏排行榜失败: {ex.Message}"); }

            for (int i = 0; i < list.Count; i++) list[i].Rank = i + 1;
            return list;
        }

        // ============================================================
        // 内部实现
        // ============================================================
        private async Task<Row?> GetRowAsync(int userId, string key)
        {
            var json = await _dataSync.QueryAsync(
                $"SELECT BestScore, MaxCombo, GamesPlayed FROM CasualGameScores WHERE UserId = {userId} AND GameKey = '{key}'");
            Row? found = null;
            try
            {
                ForEachRow(json, (names, cells) =>
                {
                    var r = new Row();
                    for (int i = 0; i < names.Length; i++)
                    {
                        switch (names[i])
                        {
                            case "BestScore": r.BestScore = ToInt(cells[i]); break;
                            case "MaxCombo": r.MaxCombo = ToInt(cells[i]); break;
                            case "GamesPlayed": r.GamesPlayed = ToInt(cells[i]); break;
                        }
                    }
                    found = r;
                });
            }
            catch (Exception ex) { Console.WriteLine($"⚠️ 解析休闲游戏记录失败: {ex.Message}"); }
            return found;
        }

        private async Task UpsertAsync(int userId, string key, int best, int combo, int played)
        {
            var sql = $@"INSERT INTO CasualGameScores (UserId, GameKey, BestScore, MaxCombo, GamesPlayed, UpdatedAt)
VALUES ({userId}, '{key}', {best}, {combo}, {played}, '{DateTime.Now:yyyy-MM-dd HH:mm:ss}')
ON CONFLICT(UserId, GameKey) DO UPDATE SET
  BestScore = {best}, MaxCombo = {combo}, GamesPlayed = {played}, UpdatedAt = '{DateTime.Now:yyyy-MM-dd HH:mm:ss}'";
            await _dataSync.ExecuteSqlAsync(sql);
        }

        private async Task<int> GetRankAsync(string key, int bestScore)
        {
            var json = await _dataSync.QueryAsync(
                $"SELECT COUNT(*) AS Cnt FROM CasualGameScores WHERE GameKey = '{key}' AND BestScore > {bestScore}");
            int count = 0;
            try
            {
                ForEachRow(json, (names, cells) =>
                {
                    for (int i = 0; i < names.Length; i++)
                        if (names[i] == "Cnt") count = ToInt(cells[i]);
                });
            }
            catch { /* 忽略 */ }
            return count + 1;
        }

        // ============================================================
        // Turso JSON 解析辅助
        // ============================================================
        private static void ForEachRow(string json, Action<string[], JsonElement[]> rowAction)
        {
            if (string.IsNullOrEmpty(json) || json == "{}") return;
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("results", out var results) || results.GetArrayLength() == 0) return;
            if (!results[0].TryGetProperty("response", out var response)) return;
            if (!response.TryGetProperty("result", out var result)) return;
            if (!result.TryGetProperty("rows", out var rows) || rows.GetArrayLength() == 0) return;
            if (!result.TryGetProperty("cols", out var cols)) return;

            var names = new string[cols.GetArrayLength()];
            for (int i = 0; i < names.Length; i++)
                names[i] = cols[i].GetProperty("name").GetString() ?? "";

            for (int r = 0; r < rows.GetArrayLength(); r++)
            {
                var row = rows[r];
                if (row.ValueKind != JsonValueKind.Array) continue;
                var cells = new JsonElement[row.GetArrayLength()];
                for (int c = 0; c < cells.Length; c++) cells[c] = row[c];
                rowAction(names, cells);
            }
        }

        private static JsonElement CellValue(JsonElement cell)
        {
            if (cell.ValueKind == JsonValueKind.Object)
            {
                if (cell.TryGetProperty("value", out var v)) return v;
                if (cell.TryGetProperty("Value", out var v2)) return v2;
            }
            return cell;
        }

        private static int ToInt(JsonElement cell)
        {
            var v = CellValue(cell);
            if (v.ValueKind == JsonValueKind.Number) return v.GetInt32();
            if (v.ValueKind == JsonValueKind.String)
                return int.TryParse(v.GetString(), out var n) ? n : 0;
            return 0;
        }

        private static string ToStr(JsonElement cell)
        {
            var v = CellValue(cell);
            if (v.ValueKind == JsonValueKind.String) return v.GetString() ?? "";
            if (v.ValueKind == JsonValueKind.Null) return "";
            return v.ToString();
        }
    }

    public class CasualLeaderboardEntry
    {
        public int Rank { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public int BestScore { get; set; }
        public int MaxCombo { get; set; }
    }
}
