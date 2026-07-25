using System;

namespace MyPersonalWebsite.Models
{
    public class GameSuggestion
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string GameName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Votes { get; set; } = 0;
        public string Status { get; set; } = "pending"; // pending, approved, developing, completed, rejected
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public User? User { get; set; }
    }

    public class GameSuggestionVote
    {
        public int Id { get; set; }
        public int SuggestionId { get; set; }
        public int UserId { get; set; }
        public DateTime VotedAt { get; set; } = DateTime.Now;

        public GameSuggestion? Suggestion { get; set; }
        public User? User { get; set; }
    }

    public class SuggestionViewModel
    {
        public GameSuggestion Suggestion { get; set; } = new();
        public string Username { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public bool IsVoted { get; set; }
        public int VoteCount { get; set; }
    }
}
