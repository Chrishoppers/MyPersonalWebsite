using MyPersonalWebsite.Services;

namespace MyPersonalWebsite.Services
{
    public class DailyQuestionScheduler : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DailyQuestionScheduler> _logger;

        public DailyQuestionScheduler(IServiceProvider serviceProvider, ILogger<DailyQuestionScheduler> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("📅 每日一问题时任务已启动");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;
                    var tomorrow = now.Date.AddDays(1);
                    var delay = tomorrow - now;

                    _logger.LogInformation($"⏳ 下次更新在 {delay.Hours} 小时 {delay.Minutes} 分钟后");

                    await Task.Delay(delay, stoppingToken);

                    if (!stoppingToken.IsCancellationRequested)
                    {
                        await UpdateTodayQuestionAsync();
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ 定时任务出错: {ex.Message}");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }

            _logger.LogInformation("📅 每日一问题时任务已停止");
        }

        private async Task UpdateTodayQuestionAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dailyService = scope.ServiceProvider.GetRequiredService<DailyQuestionService>();

                _logger.LogInformation("🔄 正在更新今日题目...");
                await dailyService.InitializeTodayQuestionAsync();
                _logger.LogInformation("✅ 今日题目已更新");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ 更新今日题目失败: {ex.Message}");
                throw;
            }
        }
    }
}
