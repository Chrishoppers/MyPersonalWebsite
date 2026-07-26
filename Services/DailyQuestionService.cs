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
        // 中国时区辅助方法
        // ============================================================
        private DateTime ChinaNow()
        {
            var chinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai");
            return TimeZoneInfo.ConvertTime(DateTime.UtcNow, chinaTimeZone);
        }

        private DateTime ChinaToday()
        {
            return ChinaNow().Date;
        }

        // ============================================================
        // 初始化今日题目
        // ============================================================
        public async Task InitializeTodayQuestionAsync()
        {
            if (!_tursoAvailable) return;

            var today = ChinaToday().ToString("yyyy-MM-dd");

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
                {available.Id}, '{today}', '{ChinaNow():yyyy-MM-dd HH:mm:ss}'
            )";

            await _tursoService.ExecuteSqlAsync(sql);

            await _tursoService.ExecuteSqlAsync(
                $"UPDATE DailyQuestionBank SET UseCount = UseCount + 1, UsedAt = '{ChinaNow():yyyy-MM-dd HH:mm:ss}' WHERE Id = {available.Id}"
            );

            Console.WriteLine($"✅ 今日题目已创建: {available.Question}");
        }

        // ============================================================
        // 获取今日题目
        // ============================================================
        public async Task<DailyQuestion?> GetTodayQuestionAsync()
        {
            if (!_tursoAvailable) return null;

            var today = ChinaToday().ToString("yyyy-MM-dd");
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

            var today = ChinaToday().ToString("yyyy-MM-dd");
            var result = await _tursoService.QueryAsync(
                $"SELECT COUNT(*) as Count FROM UserDailyAnswers WHERE UserId = {userId} AND AnswerDate = '{today}'"
            );

            try
            {
                using var doc = JsonDocument.Parse(result);
                var root = doc.RootElement;

                if (root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
                {
                    var firstResult = results[0];
                    if (firstResult.TryGetProperty("response", out var response) &&
                        response.TryGetProperty("result", out var res))
                    {
                        if (res.TryGetProperty("rows", out var rows) && rows.GetArrayLength() > 0)
                        {
                            var row = rows[0];
                            if (row.ValueKind == JsonValueKind.Array && row.GetArrayLength() > 0)
                            {
                                var element = row[0];
                                var value = element;
                                if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("value", out var v))
                                {
                                    value = v;
                                }
                                if (value.ValueKind == JsonValueKind.Number)
                                {
                                    var count = value.GetInt32();
                                    return count > 0;
                                }
                                if (value.ValueKind == JsonValueKind.String)
                                {
                                    var str = value.GetString();
                                    if (int.TryParse(str, out var count))
                                    {
                                        return count > 0;
                                    }
                                }
                            }
                        }
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
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

            if (string.IsNullOrWhiteSpace(answer))
                return (false, false, 0, "请输入答案", null);

            var isCorrect = MatchByPinyin(answer, question.Pinyin, question.Answer);

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

            var today = ChinaToday().ToString("yyyy-MM-dd");
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
    var today = ChinaToday();

    if (stats == null)
    {
        // 首次答题，直接加10分，没有连对奖励
        var sql = $@"INSERT INTO UserGameStats (
            UserId, TotalPoints, StreakDays, MaxStreakDays,
            TotalCorrect, TotalAnswered, LastAnswerDate, UpdatedAt
        ) VALUES (
            {userId}, {(isCorrect ? 10 : 0)},
            {(isCorrect ? 1 : 0)},
            {(isCorrect ? 1 : 0)},
            {(isCorrect ? 1 : 0)},
            1, '{today:yyyy-MM-dd}', '{ChinaNow():yyyy-MM-dd HH:mm:ss}'
        )";
        await _tursoService.ExecuteSqlAsync(sql);
        return;
    }

    var newStreak = stats.StreakDays;
    var newMaxStreak = stats.MaxStreakDays;
    var newCorrect = stats.TotalCorrect + (isCorrect ? 1 : 0);

    // ⭐ 基础分（答对10分，答错0分）
    var basePoints = isCorrect ? 10 : 0;
    var bonusPoints = 0;

    if (isCorrect)
    {
        // 计算新的连续天数
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

        // ⭐ 根据连续天数计算奖励
        bonusPoints = GetStreakBonus(newStreak);
    }
    else
    {
        newStreak = 0;
    }

    // ⭐ 总分 = 基础分 + 奖励分
    var newPoints = stats.TotalPoints + basePoints + bonusPoints;

    // ⭐ 如果有奖励，打印日志
    if (bonusPoints > 0)
    {
        Console.WriteLine($"🎉 连续 {newStreak} 天！额外奖励 +{bonusPoints} 分");
    }

    var sql2 = $@"UPDATE UserGameStats SET
        TotalPoints = {newPoints},
        StreakDays = {newStreak},
        MaxStreakDays = {newMaxStreak},
        TotalCorrect = {newCorrect},
        TotalAnswered = TotalAnswered + 1,
        LastAnswerDate = '{today:yyyy-MM-dd}',
        UpdatedAt = '{ChinaNow():yyyy-MM-dd HH:mm:ss}'
    WHERE UserId = {userId}";

    await _tursoService.ExecuteSqlAsync(sql2);
}

// ============================================================
// ⭐ 连对奖励计算
// ============================================================
private int GetStreakBonus(int streakDays)
{
    if (streakDays >= 100) return 100;
    if (streakDays >= 60) return 80;
    if (streakDays >= 30) return 50;
    if (streakDays >= 14) return 30;
    if (streakDays >= 7) return 20;
    if (streakDays >= 5) return 10;
    if (streakDays >= 3) return 5;
    return 0;
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
                var today = ChinaToday().ToString("yyyy-MM-dd");
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
        private bool MatchByPinyin(string userInput, string correctPinyin, string correctAnswer)
{
    if (string.IsNullOrEmpty(userInput)) return false;

    var clean = userInput.Replace(" ", "").Replace("嗯", "").Replace("啊", "").Replace("吧", "").Replace("的", "");

    // ⭐ 1. 中文匹配
    if (string.Equals(clean, correctAnswer, StringComparison.OrdinalIgnoreCase))
        return true;
    if (clean.Contains(correctAnswer, StringComparison.OrdinalIgnoreCase))
        return true;
    if (correctAnswer.Contains(clean, StringComparison.OrdinalIgnoreCase))
        return true;

    // ⭐ 2. 拼音匹配
    if (string.IsNullOrEmpty(correctPinyin)) return false;
    var cleanPinyin = correctPinyin.Replace(" ", "");
    return clean.Contains(cleanPinyin, StringComparison.OrdinalIgnoreCase) || 
           cleanPinyin.Contains(clean, StringComparison.OrdinalIgnoreCase);
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
        // 解析用户答案
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
