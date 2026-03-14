using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MUSIC.STREAMING.WEBSITE.Core.Entities;

[Table("subscriptions")]
public class Subscription
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid SubscriptionId { get; set; }

    public Guid UserId { get; set; }

    public Guid PlanId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    /// <summary>
    /// Active, Expired, Cancelled
    /// </summary>
    public string Status { get; set; } = "Active";

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
