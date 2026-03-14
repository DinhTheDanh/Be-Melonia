using MUSIC.STREAMING.WEBSITE.Core.DTOs;

namespace MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

public interface ISubscriptionService
{
    /// <summary>
    /// Lấy tất cả gói subscription đang hoạt động
    /// </summary>
    Task<Result<IEnumerable<SubscriptionPlanDto>>> GetAllPlansAsync();

    /// <summary>
    /// Lấy subscription active của user
    /// </summary>
    Task<Result<SubscriptionDto>> GetActiveSubscriptionAsync(Guid userId);

    /// <summary>
    /// Tạo subscription sau khi thanh toán thành công
    /// </summary>
    Task<Result<SubscriptionDto>> CreateSubscriptionAsync(Guid userId, Guid planId);

    /// <summary>
    /// Kiểm tra và xử lý các subscription hết hạn
    /// </summary>
    Task ProcessExpiredSubscriptionsAsync();

    /// <summary>
    /// Lấy lịch sử subscription
    /// </summary>
    Task<Result<IEnumerable<SubscriptionDto>>> GetSubscriptionHistoryAsync(Guid userId);
}
