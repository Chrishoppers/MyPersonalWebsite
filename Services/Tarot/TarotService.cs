using System.Text;
using System.Text.Json;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Services.Tarot;

namespace MyPersonalWebsite.Services.Tarot;

/// <summary>前端提交的一张已抽牌。</summary>
public sealed class DrawnCardDto
{
    /// <summary>牌阵位置名称，如「现状（中间主位）」。</summary>
    public string PositionName { get; set; } = "";
    /// <summary>牌 Id（0-77）。</summary>
    public int CardId { get; set; }
    /// <summary>是否逆位。</summary>
    public bool Reversed { get; set; }
}

/// <summary>塔罗解读结果。</summary>
public sealed class TarotReadingResult
{
    public bool Success { get; init; }
    /// <summary>true 表示 AI 调用失败，使用了本地牌义兜底解读。</summary>
    public bool Fallback { get; init; }
    public string Content { get; init; } = "";
    /// <summary>AI 调用失败时的错误详情（便于排查），成功时为空。</summary>
    public string ErrorDetail { get; init; } = "";
    /// <summary>本次调用输入 tokens。</summary>
    public int PromptTokens { get; init; }
    /// <summary>本次调用输出 tokens。</summary>
    public int CompletionTokens { get; init; }
}

/// <summary>一条历史占卜记录。</summary>
public sealed class TarotHistoryItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string Question { get; set; } = "";
    public string Spread { get; set; } = "";
    /// <summary>抽到的牌（JSON 数组，含位置/牌名/图片/正逆位）。</summary>
    public string CardsJson { get; set; } = "";
    /// <summary>解读内容（Markdown）。</summary>
    public string ResultJson { get; set; } = "";
    public string CreatedAt { get; set; } = "";
    public bool IsDaily { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
}

/// <summary>成本统计项（管理员）。</summary>
public sealed class TarotStatItem
{
    public string Username { get; set; } = "";
    public int UserId { get; set; }
    public int CallCount { get; set; }
    public long TotalTokens { get; set; }
    public long PromptTokens { get; set; }
    public long CompletionTokens { get; set; }
    public double Cost { get; set; }
}

/// <summary>用户每日塔罗配额（管理页使用）。</summary>
public sealed class TarotQuotaItem
{
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public int DailyLimit { get; set; } = 10;
}

/// <summary>
/// 塔罗牌 AI 解读服务：调用 DeepSeek 生成解读，失败时用内置牌义兜底；并负责占卜历史的持久化。
/// </summary>
public sealed class TarotService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<TarotService> _logger;
    private readonly DataSyncService _dataSync;
    private readonly BrevoEmailService _emailService;

    public TarotService(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<TarotService> logger,
        DataSyncService dataSync,
        BrevoEmailService emailService)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
        _dataSync = dataSync;
        _emailService = emailService;
    }

    public async Task<TarotReadingResult> InterpretAsync(string question, string spread, List<DrawnCardDto> cards, string? verdict = null)
    {
        if (cards == null || cards.Count == 0)
        {
            return new TarotReadingResult { Success = false, Content = "没有抽到任何牌。" };
        }

        question = string.IsNullOrWhiteSpace(question) ? "（未填写具体问题）" : question.Trim();

        var system = BuildSystemPrompt();
        var user = BuildUserPrompt(question, spread, cards, verdict);

        // 尝试调用 DeepSeek
        string? errorDetail = null;
        try
        {
            var r = await CallDeepSeekAsync(system, user);
            errorDetail = r.Error;
            if (!string.IsNullOrWhiteSpace(r.Content))
            {
                return new TarotReadingResult { Success = true, Fallback = false, Content = r.Content.Trim(), PromptTokens = r.PromptTokens, CompletionTokens = r.CompletionTokens };
            }
        }
        catch (Exception ex)
        {
            errorDetail = ex.Message;
            _logger.LogWarning(ex, "塔罗 AI 解读调用失败，回退到本地牌义解读");
        }

        return new TarotReadingResult
        {
            Success = true,
            Fallback = true,
            Content = BuildFallback(question, cards),
            ErrorDetail = errorDetail ?? "",
        };
    }

    /// <summary>针对已有解读进行追问/补充说明，生成续接回复。</summary>
    public async Task<TarotReadingResult> InterpretFollowupAsync(
        string originalQuestion, string spread, string previousContent, List<DrawnCardDto> cards, string message)
    {
        var system = BuildSystemPrompt();

        var sb = new StringBuilder();
        sb.AppendLine("以下是之前的一次塔罗占卜：");
        sb.AppendLine($"原提问：{originalQuestion}");
        sb.AppendLine($"牌阵：{spread}");
        sb.AppendLine("抽到的牌：");
        foreach (var d in cards)
        {
            var card = TarotDeck.GetById(d.CardId);
            if (card == null) continue;
            var o = d.Reversed ? "逆位" : "正位";
            sb.AppendLine($"- {d.PositionName}：{card.Name}【{o}】");
        }
        sb.AppendLine();
        sb.AppendLine($"之前的解读：{previousContent}");
        sb.AppendLine();
        sb.AppendLine($"用户的追问 / 补充说明：{message}");
        sb.AppendLine();
        sb.AppendLine("请针对这次追问或补充，结合原牌阵与牌面，给出有针对性的补充解读与答复（Markdown 格式，温暖、清晰）。");

        string? errorDetail = null;
        try
        {
            var r = await CallDeepSeekAsync(system, sb.ToString());
            errorDetail = r.Error;
            if (!string.IsNullOrWhiteSpace(r.Content))
            {
                return new TarotReadingResult { Success = true, Fallback = false, Content = r.Content.Trim(), PromptTokens = r.PromptTokens, CompletionTokens = r.CompletionTokens };
            }
        }
        catch (Exception ex)
        {
            errorDetail = ex.Message;
            _logger.LogWarning(ex, "塔罗追问解读失败");
        }

        return new TarotReadingResult
        {
            Success = true,
            Fallback = true,
            Content = "抱歉，追问解读暂时无法完成，请稍后再试。",
            ErrorDetail = errorDetail ?? "",
        };
    }

    private static string BuildSystemPrompt() =>
        "你是一位资深、温柔且富有洞察力的塔罗牌占卜师，精通伟特塔罗牌与多种经典牌阵（含凯尔特十字、三张牌、五张牌时间之流、二选一、关系牌阵、是否牌阵、每日一签、四季牌阵、事业牌阵、马蹄铁牌阵、黄道十二宫、财富牌阵、学业牌阵等）。" +
        "你会用中文为求问者解读，语气真诚、温暖，兼顾牌面象征、正逆位含义与每个牌阵位置的意义。" +
        "你会给出清晰、有深度且积极正向的指引，不夸大、不迷信，也不提供医疗、法律、投资等专业建议。" +
        "输出使用 Markdown 格式（适度使用标题、列表与粗体），结构清晰、分段合理，并在结尾温和地提醒「塔罗牌是自我觉察的镜子，最终选择权始终在你手中」。";

    private static string BuildUserPrompt(string question, string spread, List<DrawnCardDto> cards, string? verdict)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"求问者的提问：{question}");
        sb.AppendLine($"所用牌阵：{spread}");
        if (!string.IsNullOrWhiteSpace(verdict))
        {
            sb.AppendLine($"牌阵初步结论：{verdict}");
        }
        sb.AppendLine();
        sb.AppendLine("抽到的牌如下（每张牌标注了牌阵位置、牌名与正逆位）：");
        sb.AppendLine();

        foreach (var d in cards)
        {
            var card = TarotDeck.GetById(d.CardId);
            if (card == null) continue;

            var orientation = d.Reversed ? "逆位" : "正位";
            var meaning = d.Reversed ? card.Reversed : card.Upright;
            sb.AppendLine($"- **{d.PositionName}**：{card.Name}（{card.NameEn}）【{orientation}】—— 牌义：{meaning}");
        }

        sb.AppendLine();
        sb.AppendLine("请按以下结构解读（务必覆盖所有抽出的牌）：");
        sb.AppendLine("## 整体解读");
        sb.AppendLine("（围绕求问者的问题，概括整个牌阵传递的核心信息）");
        sb.AppendLine("## 逐牌解读");
        sb.AppendLine("（逐张牌结合其位置含义解读，并串联彼此关系）");
        sb.AppendLine("## 建议与总结");
        sb.AppendLine("（给出 2-3 条可落地的建议，并温暖收尾）");
        sb.AppendLine();
        sb.AppendLine("## AI 追问");
        sb.AppendLine("（最后，如果还需要进一步了解求问者的情况才能给出更精准解读，请提出 2-3 个你最想了解的问题，每行一个，以「- 」开头；若信息已足够，只写「- 无」）");

        return sb.ToString();
    }

    private sealed class DeepSeekCallResult
    {
        public string Content = "";
        public string? Error;
        public int PromptTokens;
        public int CompletionTokens;
    }

    private async Task<DeepSeekCallResult> CallDeepSeekAsync(string system, string user)
    {
        var apiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY") ?? _config["DeepSeek:ApiKey"];
        var apiUrl = _config["DeepSeek:ApiUrl"] ?? "https://api.deepseek.com/chat/completions";
        var model = _config["DeepSeek:Model"] ?? "deepseek-v4-pro";

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("DeepSeek API Key 未配置");
            return new DeepSeekCallResult { Content = "", Error = "DeepSeek API Key 未配置" };
        }

        var payload = new
        {
            model,
            messages = new object[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user },
            },
            temperature = 0.9,
            max_tokens = 3000,
            stream = false,
            thinking = new { type = "disabled" },
        };

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(60);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
            };
            request.Headers.Add("Authorization", $"Bearer {apiKey}");

            using var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"DeepSeek 解读失败 ({response.StatusCode}): {body}");
                return new DeepSeekCallResult { Content = "", Error = $"HTTP {(int)response.StatusCode}: {body}" };
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            try
            {
                var content = root.GetProperty("choices")[0]
                               .GetProperty("message")
                               .GetProperty("content")
                               .GetString() ?? "";
                int prompt = 0, completion = 0;
                if (root.TryGetProperty("usage", out var usage))
                {
                    if (usage.TryGetProperty("prompt_tokens", out var pt) && pt.ValueKind == JsonValueKind.Number) prompt = pt.GetInt32();
                    if (usage.TryGetProperty("completion_tokens", out var ct) && ct.ValueKind == JsonValueKind.Number) completion = ct.GetInt32();
                }
                return new DeepSeekCallResult { Content = content, PromptTokens = prompt, CompletionTokens = completion };
            }
            catch
            {
                return new DeepSeekCallResult { Content = "", Error = "响应格式异常: " + body };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DeepSeek 请求异常");
            return new DeepSeekCallResult { Content = "", Error = ex.Message };
        }
    }

    private static string BuildFallback(string question, List<DrawnCardDto> cards)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## 整体解读");
        sb.AppendLine();
        sb.AppendLine($"围绕你的提问「{question}」，牌阵整体呈现出一幅需要你静心感受的图景。以下是逐张牌的含义提示，愿它照亮你前行的方向。");
        sb.AppendLine();
        sb.AppendLine("## 逐牌解读");
        sb.AppendLine();

        foreach (var d in cards)
        {
            var card = TarotDeck.GetById(d.CardId);
            if (card == null) continue;

            var orientation = d.Reversed ? "逆位" : "正位";
            var meaning = d.Reversed ? card.Reversed : card.Upright;
            sb.AppendLine($"- **{d.PositionName}**：{card.Name}（{card.NameEn}）· {orientation}");
            sb.AppendLine($"  - 牌义：{meaning}");
        }

        sb.AppendLine();
        sb.AppendLine("## 建议与总结");
        sb.AppendLine();
        sb.AppendLine("静下心，感受这些牌带给你的第一直觉。塔罗牌是自我觉察的镜子，最终的选择权始终在你手中。🌙✨");

        return sb.ToString();
    }

    // ============================================================
    // 占卜历史
    // ============================================================

    public async Task SaveReadingAsync(int userId, string question, string spread, List<DrawnCardDto> cards, string resultContent, bool isDaily = false, int promptTokens = 0, int completionTokens = 0)
    {
        try
        {
            var cardsInfo = cards.Select(c =>
            {
                var card = TarotDeck.GetById(c.CardId);
                return new
                {
                    positionName = c.PositionName,
                    cardId = c.CardId,
                    name = card?.Name ?? "",
                    nameEn = card?.NameEn ?? "",
                    imageUrl = card?.ImageUrl ?? "",
                    arcana = card?.Arcana ?? "",
                    reversed = c.Reversed,
                };
            }).ToList();

            var cardsJson = JsonSerializer.Serialize(cardsInfo);
            var sql = $@"INSERT INTO TarotReadings (UserId, Question, Spread, CardsJson, ResultJson, CreatedAt, IsDaily, PromptTokens, CompletionTokens)
VALUES ({userId}, '{Esc(question)}', '{Esc(spread)}', '{Esc(cardsJson)}', '{Esc(resultContent)}', '{DateTime.Now:yyyy-MM-dd HH:mm:ss}', {(isDaily ? 1 : 0)}, {promptTokens}, {completionTokens})";
            await _dataSync.ExecuteSqlAsync(sql);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "保存塔罗占卜历史失败");
        }
    }

    public async Task<List<TarotHistoryItem>> GetHistoryAsync(int userId, int limit = 20)
    {
        limit = Math.Clamp(limit, 1, 50);
        var json = await _dataSync.QueryAsync($"SELECT * FROM TarotReadings WHERE UserId = {userId} ORDER BY Id DESC LIMIT {limit}");
        return ParseHistory(json);
    }

    // ============================================================
    // 配额 & 每日一签
    // ============================================================

    public async Task<int> GetDailyLimitAsync(int userId)
    {
        var json = await _dataSync.QueryAsync($"SELECT TarotDailyLimit FROM Users WHERE Id = {userId}");
        return ParseSingleInt(json, 10);
    }

    public async Task SetDailyLimitAsync(int userId, int limit)
    {
        limit = Math.Clamp(limit, 0, 1000);
        await _dataSync.ExecuteSqlAsync($"UPDATE Users SET TarotDailyLimit = {limit} WHERE Id = {userId}");
    }

    public async Task SetDailyLimitAndNotifyAsync(int userId, int limit)
    {
        await SetDailyLimitAsync(userId, limit);
        try
        {
            await _dataSync.AddNotificationAsync(new Notification
            {
                UserId = userId,
                Title = "🔮 塔罗牌每日次数已调整",
                Message = $"管理员已将你的每日塔罗测试次数调整为 {limit} 次。",
                Type = "info",
                CreatedAt = DateTime.Now,
            });

            var user = await _dataSync.GetUserByIdAsync(userId);
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                await _emailService.SendTarotQuotaAdjustedEmailAsync(user.Email, user.Username, limit);
            }
        }
        catch (Exception ex) { _logger.LogWarning(ex, "塔罗配额调整通知失败"); }
    }

    public async Task NotifyQuotaExhaustedAsync(int userId, int limit)
    {
        try
        {
            await _dataSync.AddNotificationAsync(new Notification
            {
                UserId = userId,
                Title = "🔮 今日塔罗次数已用完",
                Message = $"你今天的塔罗测试次数（{limit} 次）已用完，明天 0 点自动重置。",
                Type = "warning",
                CreatedAt = DateTime.Now,
            });

            var user = await _dataSync.GetUserByIdAsync(userId);
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                await _emailService.SendTarotQuotaExhaustedEmailAsync(user.Email, user.Username, limit);
            }
        }
        catch (Exception ex) { _logger.LogWarning(ex, "塔罗次数用完通知失败"); }
    }

    public async Task<int> GetTodayCountAsync(int userId)
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd") + " 00:00:00";
        var json = await _dataSync.QueryAsync($"SELECT COUNT(*) AS Cnt FROM TarotReadings WHERE UserId = {userId} AND IsDaily = 0 AND CreatedAt >= '{today}'");
        return ParseSingleInt(json, 0);
    }

    public async Task<TarotHistoryItem?> GetTodayDailyAsync(int userId)
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd") + " 00:00:00";
        var json = await _dataSync.QueryAsync($"SELECT * FROM TarotReadings WHERE UserId = {userId} AND IsDaily = 1 AND CreatedAt >= '{today}' ORDER BY Id DESC LIMIT 1");
        var list = ParseHistory(json);
        return list.Count > 0 ? list[0] : null;
    }

    public async Task<List<TarotHistoryItem>> GetAllReadingsAsync(int limit = 300)
    {
        limit = Math.Clamp(limit, 1, 1000);
        var json = await _dataSync.QueryAsync($"SELECT t.*, u.Username FROM TarotReadings t LEFT JOIN Users u ON u.Id = t.UserId ORDER BY t.Id DESC LIMIT {limit}");
        return ParseHistory(json);
    }

    /// <summary>成本统计（按用户汇总 token 用量与估算金额）。</summary>
    public async Task<List<TarotStatItem>> GetStatsAsync()
    {
        var json = await _dataSync.QueryAsync(@"
SELECT t.UserId, COALESCE(u.Username, '未知') AS Username,
       COUNT(*) AS CallCount,
       COALESCE(SUM(t.PromptTokens), 0) AS PromptTokens,
       COALESCE(SUM(t.CompletionTokens), 0) AS CompletionTokens,
       COALESCE(SUM(t.PromptTokens + t.CompletionTokens), 0) AS TotalTokens
FROM TarotReadings t LEFT JOIN Users u ON u.Id = t.UserId
GROUP BY t.UserId, u.Username ORDER BY CallCount DESC");
        var list = ParseStatList(json);
        foreach (var item in list)
        {
            item.Cost = CalculateCost(item.PromptTokens, item.CompletionTokens);
        }
        return list;
    }

    private double CalculateCost(long promptTokens, long completionTokens)
    {
        var inputPrice = double.TryParse(_config["DeepSeek:InputPricePerMillion"] ?? "2.0", out var ip) ? ip : 2.0;
        var outputPrice = double.TryParse(_config["DeepSeek:OutputPricePerMillion"] ?? "8.0", out var op) ? op : 8.0;
        return promptTokens / 1_000_000.0 * inputPrice + completionTokens / 1_000_000.0 * outputPrice;
    }

    public async Task<List<TarotQuotaItem>> GetAllQuotasAsync()
    {
        var json = await _dataSync.QueryAsync("SELECT Id, Username, TarotDailyLimit FROM Users WHERE IsDeleted = 0 ORDER BY Id ASC");
        return ParseQuotaList(json);
    }

    /// <summary>检测最近 2 小时内是否有相同/类似提问，返回提示文案（无则 null）。</summary>
    public async Task<string?> GetSimilarQuestionWarningAsync(int userId, string question)
    {
        if (string.IsNullOrWhiteSpace(question)) return null;
        var since = DateTime.Now.AddHours(-2).ToString("yyyy-MM-dd HH:mm:ss");
        var json = await _dataSync.QueryAsync($"SELECT Question FROM TarotReadings WHERE UserId = {userId} AND IsDaily = 0 AND CreatedAt >= '{since}' AND Question IS NOT NULL");
        var questions = ParseQuestionList(json);
        foreach (var q in questions)
        {
            if (AreSimilar(question, q))
            {
                return "⚠️ 你最近短时间内询问过相同或类似的问题。塔罗牌反映的是当下的心境，反复追问同一问题会越来越不准确，建议先静心沉淀，或换一个角度提问。";
            }
        }
        return null;
    }

    private static List<TarotHistoryItem> ParseHistory(string json)
    {
        var list = new List<TarotHistoryItem>();
        if (string.IsNullOrEmpty(json) || json == "{}") return list;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("results", out var results) || results.GetArrayLength() == 0) return list;
            if (!results[0].TryGetProperty("response", out var response)) return list;
            if (!response.TryGetProperty("result", out var result)) return list;
            if (!result.TryGetProperty("rows", out var rows)) return list;
            if (!result.TryGetProperty("cols", out var cols)) return list;

            var names = new string[cols.GetArrayLength()];
            for (var i = 0; i < names.Length; i++)
                names[i] = cols[i].GetProperty("name").GetString() ?? "";

            foreach (var row in rows.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Array) continue;
                var item = new TarotHistoryItem();
                var len = Math.Min(names.Length, row.GetArrayLength());
                for (var c = 0; c < len; c++)
                {
                    var cell = CellStr(row[c]);
                    switch (names[c])
                    {
                        case "Id": item.Id = int.TryParse(cell, out var id) ? id : 0; break;
                        case "UserId": item.UserId = int.TryParse(cell, out var uid) ? uid : 0; break;
                        case "Username": item.Username = cell; break;
                        case "Question": item.Question = cell; break;
                        case "Spread": item.Spread = cell; break;
                        case "CardsJson": item.CardsJson = cell; break;
                        case "ResultJson": item.ResultJson = cell; break;
                        case "CreatedAt": item.CreatedAt = cell; break;
                        case "IsDaily": item.IsDaily = cell == "1" || cell == "true"; break;
                        case "PromptTokens": item.PromptTokens = int.TryParse(cell, out var pt) ? pt : 0; break;
                        case "CompletionTokens": item.CompletionTokens = int.TryParse(cell, out var ct) ? ct : 0; break;
                    }
                }
                list.Add(item);
            }
        }
        catch
        {
            // 解析失败返回空列表
        }

        return list;
    }

    private static List<string> ParseQuestionList(string json)
    {
        var list = new List<string>();
        if (string.IsNullOrEmpty(json) || json == "{}") return list;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("results", out var results) || results.GetArrayLength() == 0) return list;
            if (!results[0].TryGetProperty("response", out var response)) return list;
            if (!response.TryGetProperty("result", out var result)) return list;
            if (!result.TryGetProperty("rows", out var rows)) return list;
            if (!result.TryGetProperty("cols", out var cols)) return list;

            var names = new string[cols.GetArrayLength()];
            for (var i = 0; i < names.Length; i++)
                names[i] = cols[i].GetProperty("name").GetString() ?? "";

            var questionIdx = -1;
            for (var i = 0; i < names.Length; i++)
                if (names[i] == "Question") { questionIdx = i; break; }
            if (questionIdx < 0) return list;

            foreach (var row in rows.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Array || row.GetArrayLength() <= questionIdx) continue;
                var cell = CellStr(row[questionIdx]);
                if (!string.IsNullOrWhiteSpace(cell)) list.Add(cell);
            }
        }
        catch { }
        return list;
    }

    private static bool AreSimilar(string a, string b)
    {
        var na = Normalize(a);
        var nb = Normalize(b);
        if (na.Length == 0 || nb.Length == 0) return false;
        if (na == nb) return true;
        if (na.Length < 4 || nb.Length < 4) return false;
        var dist = Levenshtein(na, nb);
        var maxLen = Math.Max(na.Length, nb.Length);
        return (1.0 - (double)dist / maxLen) >= 0.75;
    }

    private static string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new StringBuilder();
        foreach (var ch in s)
        {
            if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch));
        }
        return sb.ToString();
    }

    private static int Levenshtein(string a, string b)
    {
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;
        var prev = new int[b.Length + 1];
        var curr = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++) prev[j] = j;
        for (var i = 1; i <= a.Length; i++)
        {
            curr[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                curr[j] = Math.Min(Math.Min(curr[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
            }
            (prev, curr) = (curr, prev);
        }
        return prev[b.Length];
    }

    private static int ParseSingleInt(string json, int defaultValue)
    {
        if (string.IsNullOrEmpty(json) || json == "{}") return defaultValue;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
            {
                var r = results[0];
                if (r.TryGetProperty("response", out var response) && response.TryGetProperty("result", out var result))
                {
                    if (result.TryGetProperty("rows", out var rows) && rows.GetArrayLength() > 0)
                    {
                        var row = rows[0];
                        if (row.ValueKind == JsonValueKind.Array && row.GetArrayLength() > 0)
                        {
                            return int.TryParse(CellStr(row[0]), out var n) ? n : defaultValue;
                        }
                    }
                }
            }
        }
        catch { }
        return defaultValue;
    }

    private static List<TarotStatItem> ParseStatList(string json)
    {
        var list = new List<TarotStatItem>();
        if (string.IsNullOrEmpty(json) || json == "{}") return list;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("results", out var results) || results.GetArrayLength() == 0) return list;
            if (!results[0].TryGetProperty("response", out var response)) return list;
            if (!response.TryGetProperty("result", out var result)) return list;
            if (!result.TryGetProperty("rows", out var rows)) return list;
            if (!result.TryGetProperty("cols", out var cols)) return list;

            var names = new string[cols.GetArrayLength()];
            for (var i = 0; i < names.Length; i++)
                names[i] = cols[i].GetProperty("name").GetString() ?? "";

            foreach (var row in rows.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Array) continue;
                var item = new TarotStatItem();
                var len = Math.Min(names.Length, row.GetArrayLength());
                for (var c = 0; c < len; c++)
                {
                    var cell = CellStr(row[c]);
                    switch (names[c])
                    {
                        case "UserId": item.UserId = int.TryParse(cell, out var uid) ? uid : 0; break;
                        case "Username": item.Username = cell; break;
                        case "CallCount": item.CallCount = int.TryParse(cell, out var cc) ? cc : 0; break;
                        case "PromptTokens": item.PromptTokens = long.TryParse(cell, out var pt) ? pt : 0; break;
                        case "CompletionTokens": item.CompletionTokens = long.TryParse(cell, out var ct) ? ct : 0; break;
                        case "TotalTokens": item.TotalTokens = long.TryParse(cell, out var tt) ? tt : 0; break;
                    }
                }
                list.Add(item);
            }
        }
        catch { }
        return list;
    }

    private static List<TarotQuotaItem> ParseQuotaList(string json)    {
        var list = new List<TarotQuotaItem>();
        if (string.IsNullOrEmpty(json) || json == "{}") return list;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("results", out var results) || results.GetArrayLength() == 0) return list;
            if (!results[0].TryGetProperty("response", out var response)) return list;
            if (!response.TryGetProperty("result", out var result)) return list;
            if (!result.TryGetProperty("rows", out var rows)) return list;
            if (!result.TryGetProperty("cols", out var cols)) return list;

            var names = new string[cols.GetArrayLength()];
            for (var i = 0; i < names.Length; i++)
                names[i] = cols[i].GetProperty("name").GetString() ?? "";

            foreach (var row in rows.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Array) continue;
                var item = new TarotQuotaItem();
                var len = Math.Min(names.Length, row.GetArrayLength());
                for (var c = 0; c < len; c++)
                {
                    var cell = CellStr(row[c]);
                    switch (names[c])
                    {
                        case "Id": item.UserId = int.TryParse(cell, out var id) ? id : 0; break;
                        case "Username": item.Username = cell; break;
                        case "TarotDailyLimit": item.DailyLimit = int.TryParse(cell, out var lim) ? lim : 10; break;
                    }
                }
                list.Add(item);
            }
        }
        catch { }

        return list;
    }

    private static string CellStr(JsonElement cell)
    {
        if (cell.ValueKind == JsonValueKind.Object)
        {
            if (cell.TryGetProperty("value", out var v)) return CellStr(v);
            if (cell.TryGetProperty("Value", out var v2)) return CellStr(v2);
            return cell.ToString();
        }
        if (cell.ValueKind == JsonValueKind.Null) return "";
        if (cell.ValueKind == JsonValueKind.String) return cell.GetString() ?? "";
        return cell.ToString();
    }

    private static string Esc(string v) => (v ?? "").Replace("'", "''");
}
