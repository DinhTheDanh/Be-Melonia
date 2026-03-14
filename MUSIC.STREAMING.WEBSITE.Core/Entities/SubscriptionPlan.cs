using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MUSIC.STREAMING.WEBSITE.Core.Entities;

[Table("subscription_plans")]
public class SubscriptionPlan
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid PlanId { get; set; }

    /// <summary>
    /// Tên gói: "1 Tháng", "6 Tháng", "1 Năm"
    /// </summary>
    public required string PlanName { get; set; }

    /// <summary>
    /// Số tháng của gói
    /// </summary>
    public int DurationMonths { get; set; }

    /// <summary>
    /// Giá tiền (VND)
    /// </summary>
    public long Price { get; set; }

    /// <summary>
    /// Role sẽ được gán khi mua gói: Artist, ArtistPremium
    /// </summary>
    public required string RoleGranted { get; set; }

    /// <summary>
    /// Giới hạn upload bài hát (-1 = không giới hạn)
    /// </summary>
    public int UploadLimit { get; set; }

    /// <summary>
    /// Có tính năng lên lịch phát hành
    /// </summary>
    public bool CanScheduleRelease { get; set; }

    /// <summary>
    /// Có analytics nâng cao
    /// </summary>
    public bool HasAdvancedAnalytics { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
