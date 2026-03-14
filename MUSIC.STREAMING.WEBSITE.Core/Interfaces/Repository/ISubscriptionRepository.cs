using MUSIC.STREAMING.WEBSITE.Core.Entities;

namespace MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;

public interface ISubscriptionRepository : IBaseRepository<Subscription>
{
    /// <summary>
    /// Lấy subscription active hiện tại của user
    /// </summary>
    Task<Subscription?> GetActiveSubscriptionAsync(Guid userId);

    /// <summary>
    /// Lấy tất cả subscription của user
    /// </summary>
    Task<IEnumerable<Subscription>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Lấy danh sách subscription đã hết hạn nhưng chưa được xử lý
    /// </summary>
    Task<IEnumerable<Subscription>> GetExpiredActiveSubscriptionsAsync();

    /// <summary>
    /// Cập nhật trạng thái subscription
    /// </summary>
    Task<int> UpdateStatusAsync(Guid subscriptionId, string status);
}
