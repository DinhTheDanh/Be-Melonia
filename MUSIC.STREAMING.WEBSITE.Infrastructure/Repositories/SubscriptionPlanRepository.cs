using System.Data;
using Dapper;
using MUSIC.STREAMING.WEBSITE.Core.Entities;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;

namespace MUSIC.STREAMING.WEBSITE.Infrastructure.Repositories;

public class SubscriptionPlanRepository : BaseRepository<SubscriptionPlan>, ISubscriptionPlanRepository
{
    public SubscriptionPlanRepository(IDbConnection connection) : base(connection) { }

    public async Task<IEnumerable<SubscriptionPlan>> GetActivePlansAsync()
    {
        var sql = "SELECT * FROM subscription_plans WHERE is_active = 1 ORDER BY duration_months ASC;";
        return await _connection.QueryAsync<SubscriptionPlan>(sql);
    }

    public async Task<SubscriptionPlan?> GetByNameAsync(string planName)
    {
        var sql = "SELECT * FROM subscription_plans WHERE plan_name = @PlanName AND is_active = 1 LIMIT 1;";
        return await _connection.QueryFirstOrDefaultAsync<SubscriptionPlan>(sql, new { PlanName = planName });
    }
}
