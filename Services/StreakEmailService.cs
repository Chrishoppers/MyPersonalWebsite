using MyPersonalWebsite.Models;
using System.Text;

namespace MyPersonalWebsite.Services
{
    public class StreakEmailService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StreakEmailService> _logger;

        // ⭐ 趣味文案池
        private readonly List<string> _subjectLines = new()
        {
            "🌅 早安！今天的知识小炸弹已就绪！",
            "⏰ 滴！您的每日脑力充电提醒！",
            "🧠 大脑说：该活动活动了！",
            "📚 今日份的知识盲盒等你来拆！",
            "🚀 连续打卡第{N}天！快来续费你的学霸光环！",
            "💪 你的知识肌肉该锻炼了！",
            "🎯 今天的答案在向你招手！",
            "🌈 坚持的第{N}天，你超棒的！",
            "⚡ 一道题，唤醒一天的脑细胞！",
            "🎮 每日一问已刷新，快来领取你的积分！",
            "🧩 今天的题目比昨天难一点点哦~",
            "🏆 坚持到现在，你已经赢过很多人了！",
        };

        private readonly List<string> _bodyTemplates = new()
        {
            "今天天气不错，适合涨点知识~ 🌞\n\n{question}",
            "一杯咖啡的时间，就能变得更聪明 ☕\n\n{question}",
            "你坚持的第{N}天，已经超过{percent}%的人！\n\n{question}",
            "知识就像肌肉，每天练一点才会变强 💪\n\n{question}",
            "今天的题目有点意思，来试试？\n\n{question}",
            "如果你今天答对，连对记录会变成{N}天哦！\n\n{question}",
            "每一次答题，都是在为未来的自己投资 🚀\n\n{question}",
            "你已经坚持{N}天了，今天也要继续哦！\n\n{question}",
            "听说坚持21天会形成习惯，你已经{N}天啦！\n\n{question}",
            "放松一下，来道题玩玩~ 😄\n\n{question}",
        };

        public StreakEmailService(IServiceProvider serviceProvider, ILogger<StreakEmailService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("📧 连对邮件提醒服务已启动");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 计算到明天 10:00 的时间
                    var now = DateTime.Now;
                    var targetTime = now.Date.AddDays(1).AddHours(10); // 明天 10:00
                    var delay = targetTime - now;

                    _logger.LogInformation($"⏳ 下次连对邮件发送时间: {targetTime:yyyy-MM-dd HH:mm}");

                    await Task.Delay(delay, stoppingToken);

                    if (!stoppingToken.IsCancellationRequested)
                    {
                        await SendStreakEmailsAsync();
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ 连对邮件发送失败: {ex.Message}");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }

            _logger.LogInformation("📧 连对邮件提醒服务已停止");
        }

        private async Task SendStreakEmailsAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dataSync = scope.ServiceProvider.GetRequiredService<DataSyncService>();
                var emailService = scope.ServiceProvider.GetRequiredService<BrevoEmailService>();
                var dailyService = scope.ServiceProvider.GetRequiredService<DailyQuestionService>();

                // 获取所有开启邮件提醒的用户
                var users = await dataSync.GetAllUsersAsync();
                var subscribedUsers = users.Where(u => u.IsStreakEmailEnabled && !u.IsDeleted).ToList();

                if (!subscribedUsers.Any())
                {
                    _logger.LogInformation("📧 没有开启连对邮件提醒的用户");
                    return;
                }

                _logger.LogInformation($"📧 准备向 {subscribedUsers.Count} 位用户发送连对邮件");

                // 获取今日题目
                var todayQuestion = await dailyService.GetTodayQuestionAsync();
                if (todayQuestion == null)
                {
                    _logger.LogWarning("⚠️ 今日题目不存在，跳过发送");
                    return;
                }

                var random = new Random();
                int sentCount = 0;

                foreach (var user in subscribedUsers)
                {
                    try
                    {
                        // 获取用户的连续天数
                        var stats = await dailyService.GetUserStatsAsync(user.Id);
                        var streakDays = stats?.StreakDays ?? 0;

                        // 如果连续天数为0，不发送（但可以选择发送"回来吧"类型邮件）
                        if (streakDays == 0)
                        {
                            // 连续天数为0，发送"回归"邮件
                            await SendReturnEmailAsync(user, todayQuestion, emailService, random);
                            sentCount++;
                            continue;
                        }

                        // 如果今天已经答过题，不发送
                        var hasAnswered = await dailyService.HasAnsweredTodayAsync(user.Id);
                        if (hasAnswered)
                        {
                            continue;
                        }

                        // 生成今日登录Token
                        var loginToken = await dataSync.CreateLoginTokenAsync(user.Id);

                        // 随机选择文案
                        var subject = _subjectLines[random.Next(_subjectLines.Count)]
                            .Replace("{N}", streakDays.ToString());

                        var body = _bodyTemplates[random.Next(_bodyTemplates.Count)]
                            .Replace("{N}", streakDays.ToString())
                            .Replace("{percent}", (Math.Min(streakDays * 2, 95)).ToString())
                            .Replace("{question}", todayQuestion.Question);

                        // 构建邮件
                        var html = BuildEmailHtml(user.Username, body, todayQuestion, loginToken, streakDays);

                        await emailService.SendEmailAsync(user.Email, subject, html);

                        // 更新最后发送天数
                        user.LastStreakEmailDay = streakDays;
                        await dataSync.UpdateUserAsync(user);

                        sentCount++;
                        _logger.LogInformation($"📧 已发送连对邮件给 {user.Username} (连续{streakDays}天)");

                        // 防止发送太快
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"❌ 发送邮件给 {user.Username} 失败: {ex.Message}");
                    }
                }

                _logger.LogInformation($"📧 连对邮件发送完成！共发送 {sentCount} 封");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ 连对邮件发送任务失败: {ex.Message}");
                throw;
            }
        }

        private async Task SendReturnEmailAsync(User user, DailyQuestion question, BrevoEmailService emailService, Random random)
        {
            var returnSubjects = new[]
            {
                "👋 好久不见！今天的题在等你哦~",
                "💭 你不在的日子，题目都寂寞了...",
                "🎯 回来看看？今天这道题挺有意思的！",
            };

            var returnBodies = new[]
            {
                "你有多久没答题啦？今天这道题可简单了~ 快来试试！\n\n{question}",
                "别让连对记录归零呀！今天开始也不晚 💪\n\n{question}",
                "知识不等人，今天的题目已经准备好了！\n\n{question}",
            };

            var subject = returnSubjects[random.Next(returnSubjects.Length)];
            var body = returnBodies[random.Next(returnBodies.Length)]
                .Replace("{question}", question.Question);

            var loginToken = await _dataSync.CreateLoginTokenAsync(user.Id);
            var html = BuildEmailHtml(user.Username, body, question, loginToken, 0);

            await emailService.SendEmailAsync(user.Email, subject, html);
            _logger.LogInformation($"📧 已发送回归邮件给 {user.Username}");
        }

        private string BuildEmailHtml(string username, string body, DailyQuestion question, string loginToken, int streakDays)
        {
            var baseUrl = "https://chris-hopper.org";

            // 生成趣味进度条
            var progressBar = GenerateProgressBar(streakDays);

            return $@"
            <div style='font-family: 'Inter', -apple-system, sans-serif; max-width: 580px; margin: 0 auto; padding: 0; background: #0a0a0f; border-radius: 24px; border: 1px solid rgba(255,255,255,0.04); overflow: hidden;'>
                <!-- 顶部渐变条 -->
                <div style='height: 4px; background: linear-gradient(135deg, #8B5CF6, #EC4899, #F59E0B);'></div>

                <!-- 头部 -->
                <div style='padding: 28px 32px 0 32px; text-align: center;'>
                    <div style='font-size: 2.8rem; margin-bottom: 4px;'>📚</div>
                    <h2 style='color: #fff; font-weight: 700; font-size: 1.5rem; margin: 0;'>
                        每日一问
                    </h2>
                    <p style='color: rgba(255,255,255,0.2); font-size: 0.8rem; margin: 4px 0 0 0;'>
                        { (streakDays > 0 ? $"🔥 连续 {streakDays} 天" : "💪 重新出发") }
                    </p>
                </div>

                <!-- 进度条 -->
                {progressBar}

                <!-- 正文 -->
                <div style='padding: 16px 32px 24px 32px;'>
                    <p style='color: rgba(255,255,255,0.4); font-size: 0.95rem; line-height: 1.6; margin: 0 0 12px 0;'>
                        Hi <strong style='color: #fff;'>{username}</strong> 👋
                    </p>

                    <p style='color: rgba(255,255,255,0.3); font-size: 0.9rem; line-height: 1.7; margin: 0 0 16px 0; white-space: pre-wrap;'>
                        {body}
                    </p>

                    <!-- 题目预览 -->
                    <div style='background: rgba(139,92,246,0.04); border: 1px solid rgba(139,92,246,0.06); border-radius: 16px; padding: 16px 20px; margin: 12px 0 20px 0;'>
                        <div style='color: rgba(255,255,255,0.15); font-size: 0.65rem; text-transform: uppercase; letter-spacing: 0.04em;'>📌 今日题目</div>
                        <div style='color: #fff; font-size: 1.05rem; font-weight: 500; margin-top: 4px;'>
                            {question.Question}
                        </div>
                        { (!string.IsNullOrEmpty(question.Hint) ? $@"
                        <div style='color: rgba(255,255,255,0.15); font-size: 0.75rem; margin-top: 4px;'>
                            💡 {question.Hint}
                        </div>" : "" ) }
                    </div>

                    <!-- 按钮 -->
                    <div style='text-align: center; margin: 20px 0 8px 0;'>
                        <a href='{baseUrl}/Auth/AutoLogin?token={loginToken}&returnUrl=/DailyQuestion'
                           style='display: inline-block; padding: 14px 48px; background: linear-gradient(135deg, #8B5CF6, #EC4899); color: #fff; text-decoration: none; border-radius: 40px; font-weight: 600; font-size: 0.95rem; box-shadow: 0 4px 24px rgba(108,60,225,0.15);'>
                            ✍️ 立即答题
                        </a>
                        <p style='color: rgba(255,255,255,0.08); font-size: 0.6rem; margin-top: 6px;'>
                            🔒 点击后自动登录 · 无需输入密码
                        </p>
                    </div>

                    <!-- 取消订阅 -->
                    <div style='text-align: center; margin-top: 20px; padding-top: 16px; border-top: 1px solid rgba(255,255,255,0.02);'>
                        <a href='{baseUrl}/Home/Profile#email-settings'
                           style='color: rgba(255,255,255,0.08); text-decoration: none; font-size: 0.65rem;'>
                            不想收到这些邮件？点此关闭
                        </a>
                    </div>
                </div>
            </div>";
        }

        private string GenerateProgressBar(int streakDays)
        {
            var target = streakDays >= 100 ? 100 : streakDays >= 60 ? 60 : streakDays >= 30 ? 30 : streakDays >= 14 ? 14 : streakDays >= 7 ? 7 : streakDays >= 3 ? 3 : 1;
            var percent = Math.Min((double)streakDays / target * 100, 100);

            var milestones = new Dictionary<int, string>
            {
                { 3, "🌸 初出茅庐" },
                { 7, "🌟 小有成就" },
                { 14, "🔥 渐入佳境" },
                { 30, "💪 月满学霸" },
                { 60, "🏆 知识王者" },
                { 100, "👑 百天传说" }
            };

            var title = "🏃 继续坚持！";
            var nextMilestone = "下一个目标: ";
            foreach (var m in milestones.OrderBy(m => m.Key))
            {
                if (streakDays < m.Key)
                {
                    nextMilestone += $"{m.Key}天 ({m.Value})";
                    break;
                }
                else
                {
                    title = m.Value;
                }
            }

            if (streakDays >= 100) nextMilestone = "🏆 传说达成！";

            return $@"
            <div style='padding: 8px 32px 0 32px;'>
                <div style='display: flex; justify-content: space-between; align-items: center; margin-bottom: 4px;'>
                    <span style='color: rgba(255,255,255,0.15); font-size: 0.65rem;'>🔥 连对 {streakDays} 天</span>
                    <span style='color: rgba(255,255,255,0.1); font-size: 0.55rem;'>{title}</span>
                </div>
                <div style='width: 100%; height: 4px; background: rgba(255,255,255,0.04); border-radius: 4px; overflow: hidden;'>
                    <div style='width: {percent}%; height: 100%; background: linear-gradient(135deg, #8B5CF6, #EC4899); border-radius: 4px; transition: width 0.3s;'></div>
                </div>
                <div style='text-align: right; color: rgba(255,255,255,0.05); font-size: 0.5rem; margin-top: 2px;'>
                    {nextMilestone}
                </div>
            </div>";
        }
    }
}
