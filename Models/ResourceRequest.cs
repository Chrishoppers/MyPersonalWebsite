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
        public string VerifyStatus { get; set; } = "pending";

        // ============================================================
        // 7. 免责声明
        // ============================================================
        public bool AgreeToBLContent { get; set; } = false;
        public bool AgreeToTerms { get; set; } = false;

        // ============================================================
        // 8. ⭐ 联系人信息（修复缺失字段）
        // ============================================================
        public string PersonName { get; set; } = string.Empty;      // 联系人姓名
        public string Description { get; set; } = string.Empty;     // 备注说明
        public string ResourceName { get; set; } = string.Empty;    // 资源名称

        // ============================================================
        // 9. ⭐ 退款相关（修复缺失字段）
        // ============================================================
        public string RefundOption { get; set; } = "2weeks_free";   // 1day_paid / 2weeks_free
        public decimal RefundAmount { get; set; } = 0;
        public DateTime? RefundDeadline { get; set; }

        // ============================================================
        // 10. ⭐ 文件相关
        // ============================================================
        public string FileUrl { get; set; } = string.Empty;

        // ============================================================
        // 11. ⭐ 支付相关（只保留微信）
        // ============================================================
        public string OrderId { get; set; } = string.Empty;          // 订单号
        public decimal Amount { get; set; } = 2.00m;                 // 支付金额（默认2元）
        public bool IsPaid { get; set; } = false;                    // 是否已支付
        public DateTime? PaidAt { get; set; }                        // 支付确认时间
        public string? PaidNote { get; set; }                        // 管理员备注
        public string? AdminPaidBy { get; set; }                    // 确认收款的管理员

        // ============================================================
        // 12. 状态
        // ============================================================
        public string Status { get; set; } = "pending";   // pending, paid, processing, completed, rejected, refunded
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
