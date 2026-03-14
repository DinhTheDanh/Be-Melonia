using System.Data;
using Dapper;
using MUSIC.STREAMING.WEBSITE.Core.Entities;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;

namespace MUSIC.STREAMING.WEBSITE.Infrastructure.Repositories;

public class SubscriptionRepository : BaseRepository<Subscription>, ISubscriptionRepository
{
    public SubscriptionRepository(IDbConnection connection) : base(connection) { }

    public async Task<Subscription?> GetActiveSubscriptionAsync(Guid userId)
    {
        var sql = @"SELECT * FROM subscriptions 
                     WHERE user_id = @UserId AND status = 'Active' AND end_date > @Now 
                     ORDER BY end_date DESC LIMIT 1;";
        return await _connection.QueryFirstOrDefaultAsync<Subscription>(sql, new
        {
            UserId = userId,
            Now = DateTime.UtcNow
        });
    }

    public async Task<IEnumerable<Subscription>> GetByUserIdAsync(Guid userId)
    {
        var sql = "SELECT * FROM subscriptions WHERE user_id = @UserId ORDER BY created_at DESC;";
        return await _connection.QueryAsync<Subscription>(sql, new { UserId = userId });
    }

    public async Task<IEnumerable<Subscription>> GetExpiredActiveSubscriptionsAsync()
    {
        var sql = @"SELECT * FROM subscriptions 
                     WHERE status = 'Active' AND end_date <= @Now;";
        return await _connection.QueryAsync<Subscription>(sql, new { Now = DateTime.UtcNow });
    }

    public async Task<int> UpdateStatusAsync(Guid subscriptionId, string status)
    {
        var sql = @"UPDATE subscriptions 
                     SET status = @Status, updated_at = @UpdatedAt 
                     WHERE subscription_id = @SubscriptionId;";
        return await _connection.ExecuteAsync(sql, new
        {
            SubscriptionId = subscriptionId,
            Status = status,
            UpdatedAt = DateTime.UtcNow
        });
    }
}
