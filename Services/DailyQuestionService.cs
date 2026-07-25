using MyPersonalWebsite.Models;
using System.Text.Json;
using System.Text;

namespace MyPersonalWebsite.Services
{
    public class DailyQuestionService
    {
        private readonly TursoService _tursoService;
        private readonly DataSyncService _dataSync;
        private readonly bool _tursoAvailable;

        public DailyQuestionService(TursoService tursoService, DataSyncService dataSync)
        {
            _tursoService = tursoService;
            _dataSync = dataSync;
            var url = Environment.GetEnvironmentVariable("TURSO_DATABASE_URL") ?? "";
            var token = Environment.GetEnvironmentVariable("TURSO_AUTH_TOKEN") ?? "";
            _tursoAvailable = !string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(token);
        }

        // ============================================================
        // 初始化今日题目（每天自动生成）
        // ============================================================
        public async Task InitializeTodayQuestionAsync()
        {
            if (!_tursoAvailable) return;

            var today = DateTime.Today.ToString("yyyy-MM-dd");

            // 检查今天是否已有题目
            var checkResult = await _tursoService.QueryAsync(
                $"SELECT COUNT(*) as Count FROM DailyQuestions WHERE Date = '{today}'"
            );

            if (!checkResult.Contains("\"rows\":[]"))
            {
                Console.WriteLine($"✅ 今日题目已存在: {today}");
                return;
            }

            // 获取默认题库
            var defaultQuestions = DailyQuestionData.GetDefaultQuestions();
            var todayQuestion = defaultQuestions.FirstOrDefault(q => q.Date.ToString("yyyy-MM-dd") == today);

            if (todayQuestion == null)
            {
                Console.WriteLine($"⚠️ 没有找到 {today} 的题目");
                return;
            }

            // 插入今日题目
            var sql = $@"INSERT INTO DailyQuestions (
                Question, Answer, Pinyin, Hint, Difficulty, Date, CreatedAt
            ) VALUES (
                '{EscapeSql(todayQuestion.Question)}',
                '{EscapeSql(todayQuestion.Answer)}',
                '{EscapeSql(todayQuestion.Pinyin)}',
                {(string.IsNullOrEmpty(todayQuestion.Hint) ? "NULL" : $"'{EscapeSql(todayQuestion.Hint)}'")},
                {todayQuestion.Difficulty},
                '{today}',
                '{DateTime.Now:yyyy-MM-dd HH:mm:ss}'
            )";

            await _tursoService.ExecuteSqlAsync(sql);
            Console.WriteLine($"✅ 今日题目已创建: {todayQuestion.Question}");
        }

        // ============================================================
        // 获取今日题目
        // ============================================================
        public async Task<DailyQuestion?> GetTodayQuestionAsync()
        {
            if (!_tursoAvailable) return null;

            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var result = await _tursoService.QueryAsync(
                $"SELECT * FROM DailyQuestions WHERE Date = '{today}'"
            );

            return ParseDailyQuestion(result);
        }

        // ============================================================
        // 检查用户今日是否已答题
        // ============================================================
        public async Task<bool> HasAnsweredTodayAsync(int userId)
        {
            if (!_tursoAvailable) return false;

            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var result = await _tursoService.QueryAsync(
                $"SELECT COUNT(*) as Count FROM UserDailyAnswers WHERE UserId = {userId} AND AnswerDate = '{today}'"
            );

            return !result.Contains("\"rows\":[]");
        }

        // ============================================================
        // 提交答案
        // ============================================================
        public async Task<(bool Success, bool IsCorrect, int Points, string Message)> SubmitAnswerAsync(
            int userId, string answer, bool isSkip = false)
        {
            if (!_tursoAvailable)
                return (false, false, 0, "数据库不可用");

            // 检查是否已答题
            if (await HasAnsweredTodayAsync(userId))
                return (false, false, 0, "今天已经答过题了，明天再来吧！");

            var question = await GetTodayQuestionAsync();
            if (question == null)
                return (false, false, 0, "今日题目不存在，请稍后再试");

            if (isSkip)
            {
                // 跳过不扣分，但记录已答题
                await RecordAnswerAsync(userId, question.Id, "", false);
                return (true, false, 0, "⏭️ 已跳过，明天再来吧！");
            }

            // 拼音匹配
            var isCorrect = MatchByPinyin(answer, question.Pinyin);

            if (isCorrect)
            {
                await RecordAnswerAsync(userId, question.Id, answer, true);
                await UpdateUserStatsAsync(userId, true);

                // 获取当前积分
                var stats = await GetUserStatsAsync(userId);
                var points = stats?.TotalPoints ?? 0;

                return (true, true, points, "🎉 答对了！继续加油！");
            }
            else
            {
                await RecordAnswerAsync(userId, question.Id, answer, false);
                await UpdateUserStatsAsync(userId, false);

                return (true, false, 0, $"❌ 答错了，正确答案是：{question.Answer}");
            }
        }

        // ============================================================
        // 记录答案
        // ============================================================
        private async Task RecordAnswerAsync(int userId, int questionId, string answer, bool isCorrect)
        {
            if (!_tursoAvailable) return;

            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var sql = $@"INSERT INTO UserDailyAnswers (
                UserId, QuestionId, Answer, IsCorrect, AnswerDate
            ) VALUES (
                {userId}, {questionId}, '{EscapeSql(answer)}',
                {(isCorrect ? 1 : 0)}, '{today}'
            )";

            await _tursoService.ExecuteSqlAsync(sql);
            Console.WriteLine($"✅ 答案已记录: UserId={userId}, IsCorrect={isCorrect}");
        }

        // ============================================================
        // 更新用户统计
        // ============================================================
        private async Task UpdateUserStatsAsync(int userId, bool isCorrect)
        {
            if (!_tursoAvailable) return;

            var stats = await GetUserStatsAsync(userId);
            var today = DateTime.Today;

            if (stats == null)
            {
                // 创建新记录
                var sql = $@"INSERT INTO UserGameStats (
                    UserId, TotalPoints, StreakDays, MaxStreakDays,
                    TotalCorrect, TotalAnswered, LastAnswerDate, UpdatedAt
                ) VALUES (
                    {userId}, {(isCorrect ? 10 : 0)},
                    {(isCorrect ? 1 : 0)},
                    {(isCorrect ? 1 : 0)},
                    {(isCorrect ? 1 : 0)},
                    1, '{today:yyyy-MM-dd}', '{DateTime.Now:yyyy-MM-dd HH:mm:ss}'
                )";
                await _tursoService.ExecuteSqlAsync(sql);
                return;
            }

            // 更新现有记录
            var newStreak = stats.StreakDays;
            var newMaxStreak = stats.MaxStreakDays;
            var newPoints = stats.TotalPoints + (isCorrect ? 10 : 0);
            var newCorrect = stats.TotalCorrect + (isCorrect ? 1 : 0);

            // 检查连续天数
            if (isCorrect)
            {
                if (stats.LastAnswerDate?.ToString("yyyy-MM-dd") == today.AddDays(-1).ToString("yyyy-MM-dd"))
                {
                    newStreak++;
                }
                else if (stats.LastAnswerDate?.ToString("yyyy-MM-dd") == today.ToString("yyyy-MM-dd"))
                {
                    // 今天已经更新过了，不重复计算
                }
                else
                {
                    newStreak = 1;
                }

                if (newStreak > newMaxStreak)
                    newMaxStreak = newStreak;
            }
            else
            {
                newStreak = 0;
            }

            var sql2 = $@"UPDATE UserGameStats SET
                TotalPoints = {newPoints},
                StreakDays = {newStreak},
                MaxStreakDays = {newMaxStreak},
                TotalCorrect = {newCorrect},
                TotalAnswered = TotalAnswered + 1,
                LastAnswerDate = '{today:yyyy-MM-dd}',
                UpdatedAt = '{DateTime.Now:yyyy-MM-dd HH:mm:ss}'
            WHERE UserId = {userId}";

            await _tursoService.ExecuteSqlAsync(sql2);
        }

        // ============================================================
        // 获取用户统计
        // ============================================================
        public async Task<UserGameStats?> GetUserStatsAsync(int userId)
        {
            if (!_tursoAvailable) return null;

            var result = await _tursoService.QueryAsync(
                $"SELECT * FROM UserGameStats WHERE UserId = {userId}"
            );

            return ParseUserStats(result);
        }

        // ============================================================
        // 获取排行榜
        // ============================================================
        public async Task<List<RankItem>> GetRankingAsync(int limit = 50)
        {
            if (!_tursoAvailable) return new List<RankItem>();

            var users = await _dataSync.GetAllUsersAsync();
            var userIds = users.Select(u => u.Id).ToList();

            if (!userIds.Any())
                return new List<RankItem>();

            var result = await _tursoService.QueryAsync(
                $@"SELECT UserId, TotalPoints, StreakDays, TotalCorrect
                   FROM UserGameStats
                   ORDER BY TotalPoints DESC, StreakDays DESC
                   LIMIT {limit}"
            );

            var stats = ParseRankList(result);
            var rankedItems = new List<RankItem>();

            for (int i = 0; i < stats.Count; i++)
            {
                var stat = stats[i];
                var user = users.FirstOrDefault(u => u.Id == stat.UserId);
                if (user != null)
                {
                    rankedItems.Add(new RankItem
                    {
                        UserId = user.Id,
                        Username = user.Username,
                        AvatarUrl = user.AvatarUrl,
                        IsAvatarApproved = user.IsAvatarApproved,
                        TotalPoints = stat.TotalPoints,
                        StreakDays = stat.StreakDays,
                        TotalCorrect = stat.TotalCorrect,
                        Rank = i + 1
                    });
                }
            }

            return rankedItems;
        }

        // ============================================================
        // 获取今日答题状态（含题目、用户统计、是否已答）
        // ============================================================
        public async Task<TodayAnswerStatus> GetTodayStatusAsync(int userId)
        {
            var status = new TodayAnswerStatus();

            var question = await GetTodayQuestionAsync();
            if (question != null)
                status.Question = question;

            var stats = await GetUserStatsAsync(userId);
            if (stats != null)
                status.Stats = stats;

            var hasAnswered = await HasAnsweredTodayAsync(userId);
            status.HasAnswered = hasAnswered;

            if (hasAnswered)
            {
                var today = DateTime.Today.ToString("yyyy-MM-dd");
                var result = await _tursoService.QueryAsync(
                    $"SELECT IsCorrect, Answer FROM UserDailyAnswers WHERE UserId = {userId} AND AnswerDate = '{today}'"
                );
                var answer = ParseUserAnswer(result);
                if (answer != null)
                {
                    status.IsCorrect = answer.IsCorrect;
                    status.UserAnswer = answer.Answer;
                }
            }

            return status;
        }

        // ============================================================
        // 拼音匹配（核心）
        // ============================================================
        private bool MatchByPinyin(string userInput, string correctPinyin)
        {
            if (string.IsNullOrEmpty(userInput) || string.IsNullOrEmpty(correctPinyin))
                return false;

            // 去除空格和语气词
            var clean = userInput.Replace(" ", "").Replace("嗯", "").Replace("啊", "").Replace("吧", "");

            // 尝试匹配拼音（用户说的可能是中文，也可能是拼音）
            // 使用 pinyin-pro 库在前端匹配，这里只做简单包含匹配
            // 真正的拼音匹配在前端完成
            return clean.Contains(correctPinyin) || correctPinyin.Contains(clean);
        }

        // ============================================================
        // 解析方法
        // ============================================================
        private DailyQuestion? ParseDailyQuestion(string json)
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
                            var cols = result.GetProperty("cols");

                            var q = new DailyQuestion();
                            for (int i = 0; i < cols.GetArrayLength(); i++)
                            {
                                var colName = cols[i].GetProperty("name").GetString();
                                var element = row[i];

                                switch (colName)
                                {
                                    case "Id": q.Id = GetIntFromRow(element); break;
                                    case "Question": q.Question = GetStringFromRow(element); break;
                                    case "Answer": q.Answer = GetStringFromRow(element); break;
                                    case "Pinyin": q.Pinyin = GetStringFromRow(element); break;
                                    case "Hint": q.Hint = GetStringOrNullFromRow(element); break;
                                    case "Difficulty": q.Difficulty = GetIntFromRow(element); break;
                                    case "Date": q.Date = DateTime.Parse(GetStringFromRow(element)); break;
                                }
                            }
                            return q;
                        }
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private UserGameStats? ParseUserStats(string json)
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
                            var cols = result.GetProperty("cols");

                            var stats = new UserGameStats();
                            for (int i = 0; i < cols.GetArrayLength(); i++)
                            {
                                var colName = cols[i].GetProperty("name").GetString();
                                var element = row[i];

                                switch (colName)
                                {
                                    case "UserId": stats.UserId = GetIntFromRow(element); break;
                                    case "TotalPoints": stats.TotalPoints = GetIntFromRow(element); break;
                                    case "StreakDays": stats.StreakDays = GetIntFromRow(element); break;
                                    case "MaxStreakDays": stats.MaxStreakDays = GetIntFromRow(element); break;
                                    case "TotalCorrect": stats.TotalCorrect = GetIntFromRow(element); break;
                                    case "TotalAnswered": stats.TotalAnswered = GetIntFromRow(element); break;
                                    case "LastAnswerDate":
                                        var dateStr = GetStringOrNullFromRow(element);
                                        if (!string.IsNullOrEmpty(dateStr))
                                            stats.LastAnswerDate = DateTime.Parse(dateStr);
                                        break;
                                }
                            }
                            return stats;
                        }
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private List<UserGameStats> ParseRankList(string json)
        {
            var list = new List<UserGameStats>();
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
                                if (row.ValueKind != JsonValueKind.Array)
                                    continue;

                                var stats = new UserGameStats();
                                for (int i = 0; i < cols.GetArrayLength(); i++)
                                {
                                    var colName = cols[i].GetProperty("name").GetString();
                                    var element = row[i];

                                    switch (colName)
                                    {
                                        case "UserId": stats.UserId = GetIntFromRow(element); break;
                                        case "TotalPoints": stats.TotalPoints = GetIntFromRow(element); break;
                                        case "StreakDays": stats.StreakDays = GetIntFromRow(element); break;
                                        case "TotalCorrect": stats.TotalCorrect = GetIntFromRow(element); break;
                                    }
                                }
                                list.Add(stats);
                            }
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        private (bool IsCorrect, string Answer)? ParseUserAnswer(string json)
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
                            var cols = result.GetProperty("cols");

                            bool isCorrect = false;
                            string answer = "";

                            for (int i = 0; i < cols.GetArrayLength(); i++)
                            {
                                var colName = cols[i].GetProperty("name").GetString();
                                var element = row[i];

                                if (colName == "IsCorrect")
                                    isCorrect = GetIntFromRow(element) == 1;
                                if (colName == "Answer")
                                    answer = GetStringFromRow(element);
                            }

                            return (isCorrect, answer);
                        }
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        // ============================================================
        // 工具方法
        // ============================================================
        private string EscapeSql(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("'", "''");
        }

        private string GetStringFromRow(JsonElement element)
        {
            try
            {
                if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("value", out var val))
                    return val.GetString() ?? "";
                if (element.ValueKind == JsonValueKind.String)
                    return element.GetString() ?? "";
                return element.ToString();
            }
            catch { return ""; }
        }

        private string? GetStringOrNullFromRow(JsonElement element)
        {
            try
            {
                if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("value", out var val))
                    return val.ValueKind == JsonValueKind.Null ? null : val.GetString();
                if (element.ValueKind == JsonValueKind.String)
                    return element.GetString();
                if (element.ValueKind == JsonValueKind.Null)
                    return null;
                return element.ToString();
            }
            catch { return null; }
        }

        private int GetIntFromRow(JsonElement element)
        {
            try
            {
                if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("value", out var val))
                {
                    if (val.ValueKind == JsonValueKind.Number)
                        return val.GetInt32();
                    if (val.ValueKind == JsonValueKind.String)
                        return int.TryParse(val.GetString(), out var result) ? result : 0;
                }
                if (element.ValueKind == JsonValueKind.Number)
                    return element.GetInt32();
                if (element.ValueKind == JsonValueKind.String)
                    return int.TryParse(element.GetString(), out var result) ? result : 0;
                return 0;
            }
            catch { return 0; }
        }
    }
}
