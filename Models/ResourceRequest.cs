using System;

namespace MyPersonalWebsite.Models
{
    public class ResourceRequest
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;

        // ============================================================
        // 1. 平台选择（最多2项）
        // ============================================================
        public string Platform1 { get; set; } = string.Empty;
        public string Platform2 { get; set; } = string.Empty;
        public string? PlatformOther { get; set; }

        // ============================================================
        // 2. 人物/CP名字
        // ============================================================
        public string CharacterName { get; set; } = string.Empty;

        // ============================================================
        // 3. 资源类型
        // ============================================================
        public string ResourceType { get; set; } = "一人";

        // ============================================================
        // 4. 人物设定
        // ============================================================
        public string CharacterSetting { get; set; } = "都行";

        // ============================================================
        // 5. 里克特量表
        // ============================================================
        public string NovelPreference { get; set; } = "不需要";
        public string ComicPreference { get; set; } = "不需要";
        public string ImagePreference { get; set; } = "不需要";

        // ============================================================
        // 6. 平台验证关注
        // ============================================================
        public string VerifyPlatform { get; set; } = string.Empty;
        public string VerifyAccountId { get; set; } = string.Empty;
        public bool IsFollowVerified { get; set; } = false;
        public DateTime? FollowVerifiedAt { get; set; }
        public string? FollowVerifyError { get; set; }
        public string VerifyStatus { get; set; } = "pending";  // pending, auto_verified, manual_required, rejected

        // ============================================================
        // 7. 免责声明
        // ============================================================
        public bool AgreeToBLContent { get; set; } = false;
        public bool AgreeToTerms { get; set; } = false;

        // ============================================================
        // 状态
        // ============================================================
        public string Status { get; set; } = "pending";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ProcessedAt { get; set; }

        public string FoundTypes { get; set; } = string.Empty;
        public string NotFoundTypes { get; set; } = string.Empty;
        public string AdminNote { get; set; } = string.Empty;
        public string AdminName { get; set; } = string.Empty;

        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;

        public User? User { get; set; }
    }
}
