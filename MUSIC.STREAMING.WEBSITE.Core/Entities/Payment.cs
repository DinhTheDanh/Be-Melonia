using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MUSIC.STREAMING.WEBSITE.Core.Entities;

[Table("payments")]
public class Payment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid PaymentId { get; set; }

    public Guid UserId { get; set; }

    public Guid PlanId { get; set; }

    /// <summary>
    /// Số tiền thanh toán (VND)
    /// </summary>
    public long Amount { get; set; }

    /// <summary>
    /// Pending, Success, Failed
    /// </summary>
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// VNPay, Momo
    /// </summary>
    public string PaymentMethod { get; set; } = "VNPay";

    /// <summary>
    /// Mã giao dịch từ VNPay
    /// </summary>
    public string? TransactionId { get; set; }

    /// <summary>
    /// Mã đơn hàng gửi đến VNPay (unique)
    /// </summary>
    public required string OrderId { get; set; }

    /// <summary>
    /// URL thanh toán QR từ VNPay
    /// </summary>
    public string? PaymentUrl { get; set; }

    /// <summary>
    /// Mã response từ VNPay
    /// </summary>
    public string? ResponseCode { get; set; }

    /// <summary>
    /// Thông tin bảo mật hash
    /// </summary>
    public string? SecureHash { get; set; }

    /// <summary>
    /// IP người dùng tạo giao dịch
    /// </summary>
    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
