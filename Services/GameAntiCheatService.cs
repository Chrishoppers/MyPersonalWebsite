using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyPersonalWebsite.Models;      // ← 新增
using MyPersonalWebsite.Services;

namespace MyPersonalWebsite.Services
{
    public class GameAntiCheatService
    {
        private readonly DataSyncService _dataSync;
        
        // 正常答题时间范围（秒）
        private readonly (double min, double max) _normalTimeRange = (1.5, 60);
        
        // 异常时间阈值
        private readonly double _fastAnswerThreshold = 1.0;      // 少于1秒视为秒答
        private readonly double _slowAnswerMultiplier = 3.0;     // 超过平均时间3倍视为异常
        
        public GameAntiCheatService(DataSyncService dataSync)
        {
            _dataSync = dataSync;
        }
        
        /// <summary>
        /// 验证答题时间是否异常
        /// </summary>
        public (bool isCheat, string reason, int penalty) ValidateAnswerTime(
            int userId, 
            string sessionId,
            int level, 
            string questionType, 
            double elapsedSeconds,
            bool isCorrect,
            List<GameAnswerLog> historyLogs)
        {
            // 1. 秒答检测（正确且用时少于1秒）
            if (isCorrect && elapsedSeconds < _fastAnswerThreshold)
            {
                return (true, $"fast_answer: {elapsedSeconds:F2}s", 5);
            }
            
            // 2. 超慢检测（用时超过正常范围上限）
            if (elapsedSeconds > _normalTimeRange.max)
            {
                // 检查该题型的历史平均用时
                var typeLogs = historyLogs.Where(l => l.QuestionType == questionType).ToList();
                if (typeLogs.Count >= 3)
                {
                    var avgTime = typeLogs.Average(l => l.ElapsedSeconds);
                    if (elapsedSeconds > avgTime * _slowAnswerMultiplier && elapsedSeconds > 15)
                    {
                        return (true, $"slow_answer: {elapsedSeconds:F2}s (avg: {avgTime:F2}s)", 5);
                    }
                }
            }
            
            // 3. 答题节奏检测（连续多题用时异常规律）
            if (historyLogs.Count >= 5)
            {
                var recent = historyLogs.OrderByDescending(l => l.Id).Take(5).ToList();
                var intervals = new List<double>();
                for (int i = 1; i < recent.Count; i++)
                {
                    intervals.Add(Math.Abs(recent[i].ElapsedSeconds - recent[i-1].ElapsedSeconds));
                }
                if (intervals.Count >= 3)
                {
                    var avgInterval = intervals.Average();
                    var stdDev = Math.Sqrt(intervals.Average(x => Math.Pow(x - avgInterval, 2)));
                    // 如果间隔时间标准差小于0.3秒，说明答题节奏过于规律
                    if (stdDev < 0.3 && intervals.Count >= 4)
                    {
                        return (true, $"rhythmic_pattern: std={stdDev:F2}s", 5);
                    }
                }
            }
            
            return (false, "", 0);
        }
        
        /// <summary>
        /// 验证关卡进度是否合法（防跳关）
        /// </summary>
        public (bool isValid, string reason) ValidateLevelProgress(
            int userId,
            string sessionId,
            int currentLevel,
            List<GameAnswerLog> historyLogs)
        {
            // 如果是第1关，无需验证
            if (currentLevel == 1) return (true, "");
            
            // 检查是否已经答过当前关卡
            if (historyLogs.Any(l => l.Level == currentLevel))
            {
                return (false, $"level_already_completed: {currentLevel}");
            }
            
            // 检查上一关是否已完成
            var previousLevel = currentLevel - 1;
            if (!historyLogs.Any(l => l.Level == previousLevel))
            {
                return (false, $"level_skip: from {previousLevel} to {currentLevel}");
            }
            
            // 检查关卡顺序（最近完成的关卡应该是 currentLevel - 1）
            var lastCompleted = historyLogs.OrderByDescending(l => l.Id).FirstOrDefault();
            if (lastCompleted != null && lastCompleted.Level != previousLevel)
            {
                return (false, $"level_out_of_order: last={lastCompleted.Level}, current={currentLevel}");
            }
            
            return (true, "");
        }
        
        /// <summary>
        /// 计算最终得分（含惩罚）
        /// </summary>
        public int CalculateFinalScore(
            int baseScore,
            int cheatCount,
            int passedCount,
            bool micEnabled,
            bool camEnabled,
            int penaltyMic = 8,
            int penaltyCam = 5)
        {
            var finalScore = baseScore;
            
            // 作弊惩罚：每次作弊扣5分
            finalScore -= cheatCount * 5;
            
            // 权限惩罚
            if (!micEnabled) finalScore -= passedCount * penaltyMic;
            if (!camEnabled) finalScore -= passedCount * penaltyCam;
            
            return Math.Max(0, finalScore);
        }
    }
}
