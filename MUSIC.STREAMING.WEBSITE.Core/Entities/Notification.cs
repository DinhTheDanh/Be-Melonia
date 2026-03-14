using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MUSIC.STREAMING.WEBSITE.Core.Entities;

[Table("notifications")]
public class Notification
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// payment_reminder | payment_approved | payment_rejected | payment_expired | role_changed | system | general
    /// </summary>
    public string Type { get; set; } = "general";

    public bool IsRead { get; set; } = false;

    /// <summary>
    /// Optional: PaymentId, SubscriptionId, etc.
    /// </summary>
    public Guid? RelatedEntityId { get; set; }

    public DateTime CreatedAt { get; set; }
}
