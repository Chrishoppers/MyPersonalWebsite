using System;

namespace MyPersonalWebsite.Models
{
    public class DailyQuestion
    {
        public int Id { get; set; }
        public int? QuestionId { get; set; }  // 关联 DailyQuestionBank 的 Id
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string Pinyin { get; set; } = string.Empty;
        public string? Hint { get; set; }
        public int Difficulty { get; set; } = 1;
        public string Category { get; set; } = "综合";
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

        // 每日一问相关
        public int TotalPoints { get; set; } = 0;
        public int StreakDays { get; set; } = 0;
        public int MaxStreakDays { get; set; } = 0;
        public int TotalCorrect { get; set; } = 0;
        public int TotalAnswered { get; set; } = 0;
        public DateTime? LastAnswerDate { get; set; }

        // 验证大闯关相关
        public int MaxCombo { get; set; } = 0;
        public int MaxLevel { get; set; } = 0;
        public int GamesPlayed { get; set; } = 0;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public User? User { get; set; }
    }

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
