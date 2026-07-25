using System;

namespace MyPersonalWebsite.Models
{
    public class DailyQuestion
    {
        public int Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string Pinyin { get; set; } = string.Empty;
        public string? Hint { get; set; }
        public int Difficulty { get; set; } = 1;
        public DateTime Date { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class UserDailyAnswer
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int QuestionId { get; set; }
        public string? Answer { get; set; }
        public bool IsCorrect { get; set; }
        public DateTime AnswerDate { get; set; }
    }

    public class UserGameStats
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TotalPoints { get; set; }
        public int StreakDays { get; set; }
        public int MaxStreakDays { get; set; }
        public int TotalCorrect { get; set; }
        public int TotalAnswered { get; set; }
        public DateTime? LastAnswerDate { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    // 排行榜显示模型
    public class RankItem
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public bool IsAvatarApproved { get; set; }
        public int TotalPoints { get; set; }
        public int StreakDays { get; set; }
        public int TotalCorrect { get; set; }
        public int Rank { get; set; }
    }

    // 今日答题状态
    public class TodayAnswerStatus
    {
        public bool HasAnswered { get; set; }
        public bool IsCorrect { get; set; }
        public string? UserAnswer { get; set; }
        public DailyQuestion? Question { get; set; }
        public UserGameStats? Stats { get; set; }
        public int TodayPoints { get; set; } = 10;
    }
}
