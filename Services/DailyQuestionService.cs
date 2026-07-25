using MyPersonalWebsite.Models;
using System.Text.Json;

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

            if (_tursoAvailable)
                Console.WriteLine("✅ DailyQuestionService: Turso 已连接");
            else
                Console.WriteLine("⚠️ DailyQuestionService: Turso 未配置");
        }

        // ============================================================
        // 初始化今日题目
        // ============================================================
        public async Task InitializeTodayQuestionAsync()
        {
            if (!_tursoAvailable) return;

            var today = DateTime.Today.ToString("yyyy-MM-dd");

            var checkResult = await _tursoService.QueryAsync(
                $"SELECT COUNT(*) as Count FROM DailyQuestions WHERE Date = '{today}'"
            );

            if (!checkResult.Contains("\"rows\":[]"))
            {
                Console.WriteLine($"✅ 今日题目已存在: {today}");
                return;
            }

            var availableResult = await _tursoService.QueryAsync(
                @"SELECT Id, Question, Answer, Pinyin, Hint, Difficulty, Category 
                  FROM DailyQuestionBank 
                  WHERE IsActive = 1 
                  ORDER BY UseCount ASC, RANDOM() 
                  LIMIT 1"
            );

            var available = ParseAvailableQuestion(availableResult);
            if (available == null)
            {
                Console.WriteLine("⚠️ 题库为空，请先初始化题库");
                return;
            }

            var sql = $@"INSERT INTO DailyQuestions (
                QuestionId, Date, CreatedAt
            ) VALUES (
                {available.Id}, '{today}', '{DateTime.Now:yyyy-MM-dd HH:mm:ss}'
            )";

            await _tursoService.ExecuteSqlAsync(sql);

            await _tursoService.ExecuteSqlAsync(
                $"UPDATE DailyQuestionBank SET UseCount = UseCount + 1, UsedAt = '{DateTime.Now:yyyy-MM-dd HH:mm:ss}' WHERE Id = {available.Id}"
            );

            Console.WriteLine($"✅ 今日题目已创建: {available.Question}");
        }

        // ============================================================
        // 获取今日题目
        // ============================================================
        public async Task<DailyQuestion?> GetTodayQuestionAsync()
        {
            if (!_tursoAvailable) return null;

            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var result = await _tursoService.QueryAsync($@"
                SELECT dq.Id, dq.QuestionId, dq.Date, 
                       b.Question, b.Answer, b.Pinyin, b.Hint, b.Difficulty, b.Category
                FROM DailyQuestions dq
                JOIN DailyQuestionBank b ON dq.QuestionId = b.Id
                WHERE dq.Date = '{today}'
                LIMIT 1
            ");

            return ParseDailyQuestionWithCategory(result);
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
        public async Task<(bool Success, bool IsCorrect, int Points, string Message, string? CorrectAnswer)> 
            SubmitAnswerAsync(int userId, string answer)
        {
            if (!_tursoAvailable)
                return (false, false, 0, "数据库不可用", null);

            if (await HasAnsweredTodayAsync(userId))
                return (false, false, 0, "今天已经答过题了，明天再来吧！", null);

            var question = await GetTodayQuestionAsync();
            if (question == null)
                return (false, false, 0, "今日题目不存在，请稍后再试", null);

            var isCorrect = MatchByPinyin(answer, question.Pinyin);

            if (isCorrect)
            {
                await RecordAnswerAsync(userId, question.Id, answer, true);
                await UpdateUserStatsAsync(userId, true);
                var stats = await GetUserStatsAsync(userId);
                var points = stats?.TotalPoints ?? 0;
                return (true, true, points, "🎉 答对了！+10分", question.Answer);
            }
            else
            {
                await RecordAnswerAsync(userId, question.Id, answer, false);
                await UpdateUserStatsAsync(userId, false);
                return (true, false, 0, "❌ 答错了", question.Answer);
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

            var newStreak = stats.StreakDays;
            var newMaxStreak = stats.MaxStreakDays;
            var newPoints = stats.TotalPoints + (isCorrect ? 10 : 0);
            var newCorrect = stats.TotalCorrect + (isCorrect ? 1 : 0);

            if (isCorrect)
            {
                if (stats.LastAnswerDate?.ToString("yyyy-MM-dd") == today.AddDays(-1).ToString("yyyy-MM-dd"))
                {
                    newStreak++;
                }
                else if (stats.LastAnswerDate?.ToString("yyyy-MM-dd") != today.ToString("yyyy-MM-dd"))
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
        // 获取今日答题状态
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
                var answerData = ParseUserAnswer(result);
                if (answerData != null)
                {
                    status.IsCorrect = answerData.Value.IsCorrect;
                    status.UserAnswer = answerData.Value.Answer;
                }
            }

            return status;
        }

        // ============================================================
        // 拼音匹配
        // ============================================================
        private bool MatchByPinyin(string userInput, string correctPinyin)
        {
            if (string.IsNullOrEmpty(userInput) || string.IsNullOrEmpty(correctPinyin))
                return false;

            var clean = userInput.Replace(" ", "").Replace("嗯", "").Replace("啊", "").Replace("吧", "");
            return clean.Contains(correctPinyin, StringComparison.OrdinalIgnoreCase) || 
                   correctPinyin.Contains(clean, StringComparison.OrdinalIgnoreCase);
        }

        // ============================================================
        // 解析方法
        // ============================================================

        private DailyQuestion? ParseAvailableQuestion(string json)
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
                                    case "Category": q.Category = GetStringFromRow(element); break;
                                }
                            }
                            return q;
                        }
                    }
                }
                return null;
            }
            catch { return null; }
        }

        private DailyQuestion? ParseDailyQuestionWithCategory(string json)
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
                                    case "QuestionId": q.QuestionId = GetIntFromRow(element); break;
                                    case "Date": q.Date = DateTime.Parse(GetStringFromRow(element)); break;
                                    case "Question": q.Question = GetStringFromRow(element); break;
                                    case "Answer": q.Answer = GetStringFromRow(element); break;
                                    case "Pinyin": q.Pinyin = GetStringFromRow(element); break;
                                    case "Hint": q.Hint = GetStringOrNullFromRow(element); break;
                                    case "Difficulty": q.Difficulty = GetIntFromRow(element); break;
                                    case "Category": q.Category = GetStringFromRow(element); break;
                                }
                            }
                            return q;
                        }
                    }
                }
                return null;
            }
            catch { return null; }
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
            catch { return null; }
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

        // ============================================================
        // 解析用户答案（修复版）
        // ============================================================
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
                                {
                                    isCorrect = GetIntFromRow(element) == 1;
                                }
                                else if (colName == "Answer")
                                {
                                    answer = GetStringFromRow(element);
                                }
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
