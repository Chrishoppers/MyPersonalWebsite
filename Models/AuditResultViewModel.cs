namespace MyPersonalWebsite.Models
{
    public class AuditResultViewModel
    {
        public bool Success { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Detail { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string IconType { get; set; } = "info"; // success, fail, info
    }
}
