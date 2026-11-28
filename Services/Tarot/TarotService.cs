using System.Text;
using System.Text.Json;
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
}

/// <summary>一条历史占卜记录。</summary>
public sealed class TarotHistoryItem
{
    public int Id { get; set; }
    public string Question { get; set; } = "";
    public string Spread { get; set; } = "";
    /// <summary>抽到的牌（JSON 数组，含位置/牌名/图片/正逆位）。</summary>
    public string CardsJson { get; set; } = "";
    /// <summary>解读内容（Markdown）。</summary>
    public string ResultJson { get; set; } = "";
    public string CreatedAt { get; set; } = "";
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

    public TarotService(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<TarotService> logger,
        DataSyncService dataSync)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
        _dataSync = dataSync;
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
            var (content, err) = await CallDeepSeekAsync(system, user);
            errorDetail = err;
            if (!string.IsNullOrWhiteSpace(content))
            {
                return new TarotReadingResult { Success = true, Fallback = false, Content = content.Trim() };
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

    private static string BuildSystemPrompt() =>
        "你是一位资深、温柔且富有洞察力的塔罗牌占卜师，精通伟特塔罗牌与多种经典牌阵（含凯尔特十字、三张牌、单张牌、五张牌时间之流、二选一、关系牌阵、是否牌阵、每日一签等）。" +
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

        return sb.ToString();
    }

    private async Task<(string content, string? error)> CallDeepSeekAsync(string system, string user)
    {
        var apiKey = _config["DeepSeek:ApiKey"];
        var apiUrl = _config["DeepSeek:ApiUrl"] ?? "https://api.deepseek.com/chat/completions";
        var model = _config["DeepSeek:Model"] ?? "deepseek-v4-pro";

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("DeepSeek API Key 未配置");
            return ("", "DeepSeek API Key 未配置");
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
                return ("", $"HTTP {(int)response.StatusCode}: {body}");
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            try
            {
                var content = root.GetProperty("choices")[0]
                               .GetProperty("message")
                               .GetProperty("content")
                               .GetString() ?? "";
                return (content, null);
            }
            catch
            {
                return ("", "响应格式异常: " + body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DeepSeek 请求异常");
            return ("", ex.Message);
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

    public async Task SaveReadingAsync(int userId, string question, string spread, List<DrawnCardDto> cards, string resultContent)
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
            var sql = $@"INSERT INTO TarotReadings (UserId, Question, Spread, CardsJson, ResultJson, CreatedAt)
VALUES ({userId}, '{Esc(question)}', '{Esc(spread)}', '{Esc(cardsJson)}', '{Esc(resultContent)}', '{DateTime.Now:yyyy-MM-dd HH:mm:ss}')";
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
                        case "Question": item.Question = cell; break;
                        case "Spread": item.Spread = cell; break;
                        case "CardsJson": item.CardsJson = cell; break;
                        case "ResultJson": item.ResultJson = cell; break;
                        case "CreatedAt": item.CreatedAt = cell; break;
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
