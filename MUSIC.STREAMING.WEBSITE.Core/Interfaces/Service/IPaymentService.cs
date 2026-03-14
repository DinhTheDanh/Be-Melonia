using MUSIC.STREAMING.WEBSITE.Core.DTOs;

namespace MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

public interface IPaymentService
{
    /// <summary>
    /// Tạo payment mới và trả về URL thanh toán VNPay
    /// </summary>
    Task<Result<PaymentResponseDto>> CreatePaymentAsync(Guid userId, CreatePaymentDto dto, string ipAddress);

    /// <summary>
    /// Xử lý callback từ VNPay (IPN - Instant Payment Notification)
    /// </summary>
    Task<Result<string>> ProcessVnPayIpnAsync(Dictionary<string, string> queryParams);

    /// <summary>
    /// Xử lý return URL từ VNPay (redirect user về)
    /// </summary>
    Task<Result<PaymentResponseDto>> ProcessVnPayReturnAsync(Dictionary<string, string> queryParams);

    /// <summary>
    /// Lấy lịch sử thanh toán của user
    /// </summary>
    Task<Result<IEnumerable<PaymentHistoryDto>>> GetPaymentHistoryAsync(Guid userId);

    /// <summary>
    /// Lấy thông tin chi tiết payment
    /// </summary>
    Task<Result<PaymentResponseDto>> GetPaymentByIdAsync(Guid paymentId, Guid userId);
}
