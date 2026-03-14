using MUSIC.STREAMING.WEBSITE.Core.Entities;

namespace MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;

public interface ISubscriptionPlanRepository : IBaseRepository<SubscriptionPlan>
{
    /// <summary>
    /// Lấy tất cả gói đang active
    /// </summary>
    Task<IEnumerable<SubscriptionPlan>> GetActivePlansAsync();

    /// <summary>
    /// Lấy plan theo tên
    /// </summary>
    Task<SubscriptionPlan?> GetByNameAsync(string planName);
}
