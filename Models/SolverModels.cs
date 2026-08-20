using System;

namespace MyPersonalWebsite.Models
{
    public class SolverSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public List<DetectedProblem> Problems { get; set; } = new();
    }

    public class DetectedProblem
    {
        public string Id { get; set; } = string.Empty;
        public string ShortText { get; set; } = string.Empty;
        public string FullText { get; set; } = string.Empty;
        public string? AiAnswer { get; set; }
        public List<ProblemStep> Steps { get; set; } = new();
    }

    public class ProblemStep
    {
        public int Index { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
