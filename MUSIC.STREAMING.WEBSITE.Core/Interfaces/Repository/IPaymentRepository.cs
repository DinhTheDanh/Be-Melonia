using MUSIC.STREAMING.WEBSITE.Core.Entities;

namespace MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;

public interface IPaymentRepository : IBaseRepository<Payment>
{
    /// <summary>
    /// Lấy payment theo OrderId (mã đơn hàng gửi VNPay)
    /// </summary>
    Task<Payment?> GetByOrderIdAsync(string orderId);

    /// <summary>
    /// Lấy danh sách payment của user
    /// </summary>
    Task<IEnumerable<Payment>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Lấy payment pending chưa thanh toán của user cho plan cụ thể
    /// </summary>
    Task<Payment?> GetPendingPaymentAsync(Guid userId, Guid planId);

    /// <summary>
    /// Cập nhật trạng thái payment
    /// </summary>
    Task<int> UpdateStatusAsync(Guid paymentId, string status, string? transactionId, string? responseCode);
}
