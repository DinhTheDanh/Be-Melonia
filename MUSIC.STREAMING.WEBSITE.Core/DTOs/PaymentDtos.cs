namespace MUSIC.STREAMING.WEBSITE.Core.DTOs;

// ==================== Payment DTOs ====================

/// <summary>
/// DTO để tạo thanh toán mới
/// </summary>
public class CreatePaymentDto
{
    /// <summary>
    /// ID của gói subscription
    /// </summary>
    public Guid PlanId { get; set; }

    /// <summary>
    /// Phương thức thanh toán: VNPay, Momo
    /// </summary>
    public string PaymentMethod { get; set; } = "VNPay";
}

/// <summary>
/// Response trả về sau khi tạo payment
/// </summary>
public class PaymentResponseDto
{
    public Guid PaymentId { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentUrl { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO hiển thị lịch sử thanh toán
/// </summary>
public class PaymentHistoryDto
{
    public Guid PaymentId { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ==================== Subscription DTOs ====================

/// <summary>
/// DTO hiển thị thông tin subscription hiện tại
/// </summary>
public class SubscriptionDto
{
    public Guid SubscriptionId { get; set; }
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string RoleGranted { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int DaysRemaining { get; set; }

    // Feature flags
    public int UploadLimit { get; set; }
    public bool CanScheduleRelease { get; set; }
    public bool HasAdvancedAnalytics { get; set; }
}

/// <summary>
/// DTO hiển thị gói subscription plan
/// </summary>
public class SubscriptionPlanDto
{
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public int DurationMonths { get; set; }
    public long Price { get; set; }
    public string RoleGranted { get; set; } = string.Empty;
    public int UploadLimit { get; set; }
    public bool CanScheduleRelease { get; set; }
    public bool HasAdvancedAnalytics { get; set; }
}

// ==================== Feature Authorization DTOs ====================

/// <summary>
/// DTO chứa thông tin quyền của user dựa trên subscription
/// </summary>
public class UserFeatureDto
{
    /// <summary>
    /// Số bài hát tối đa được upload (-1 = không giới hạn, 0 = không được upload)
    /// </summary>
    public int UploadLimit { get; set; }

    /// <summary>
    /// Số bài đã upload
    /// </summary>
    public int CurrentUploadCount { get; set; }

    /// <summary>
    /// Còn có thể upload không
    /// </summary>
    public bool CanUpload => UploadLimit == -1 || CurrentUploadCount < UploadLimit;

    /// <summary>
    /// Có quyền lên lịch phát hành
    /// </summary>
    public bool CanScheduleRelease { get; set; }

    /// <summary>
    /// Có analytics nâng cao
    /// </summary>
    public bool HasAdvancedAnalytics { get; set; }

    /// <summary>
    /// Subscription hiện tại có active không
    /// </summary>
    public bool HasActiveSubscription { get; set; }

    /// <summary>
    /// Tên gói hiện tại
    /// </summary>
    public string? CurrentPlanName { get; set; }

    /// <summary>
    /// Ngày hết hạn
    /// </summary>
    public DateTime? SubscriptionEndDate { get; set; }
}

// ==================== VNPay DTOs ====================

/// <summary>
/// Cấu hình VNPay từ appsettings
/// </summary>
public class VnPayConfig
{
    public string TmnCode { get; set; } = string.Empty;
    public string HashSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string IpnUrl { get; set; } = string.Empty;
    public string Version { get; set; } = "2.1.0";
    public string Command { get; set; } = "pay";
    public string CurrCode { get; set; } = "VND";
    public string Locale { get; set; } = "vn";
}

/// <summary>
/// Dữ liệu callback từ VNPay (IPN/Return URL)
/// </summary>
public class VnPayCallbackDto
{
    public string vnp_TmnCode { get; set; } = string.Empty;
    public long vnp_Amount { get; set; }
    public string vnp_BankCode { get; set; } = string.Empty;
    public string vnp_BankTranNo { get; set; } = string.Empty;
    public string vnp_CardType { get; set; } = string.Empty;
    public string vnp_OrderInfo { get; set; } = string.Empty;
    public string vnp_PayDate { get; set; } = string.Empty;
    public string vnp_ResponseCode { get; set; } = string.Empty;
    public string vnp_TransactionNo { get; set; } = string.Empty;
    public string vnp_TransactionStatus { get; set; } = string.Empty;
    public string vnp_TxnRef { get; set; } = string.Empty;
    public string vnp_SecureHash { get; set; } = string.Empty;
}
