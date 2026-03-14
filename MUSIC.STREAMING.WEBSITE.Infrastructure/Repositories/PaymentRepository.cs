using System.Data;
using Dapper;
using MUSIC.STREAMING.WEBSITE.Core.Entities;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;

namespace MUSIC.STREAMING.WEBSITE.Infrastructure.Repositories;

public class PaymentRepository : BaseRepository<Payment>, IPaymentRepository
{
    public PaymentRepository(IDbConnection connection) : base(connection) { }

    public async Task<Payment?> GetByOrderIdAsync(string orderId)
    {
        var sql = "SELECT * FROM payments WHERE order_id = @OrderId LIMIT 1;";
        return await _connection.QueryFirstOrDefaultAsync<Payment>(sql, new { OrderId = orderId });
    }

    public async Task<IEnumerable<Payment>> GetByUserIdAsync(Guid userId)
    {
        var sql = "SELECT * FROM payments WHERE user_id = @UserId ORDER BY created_at DESC;";
        return await _connection.QueryAsync<Payment>(sql, new { UserId = userId });
    }

    public async Task<Payment?> GetPendingPaymentAsync(Guid userId, Guid planId)
    {
        var sql = @"SELECT * FROM payments 
                     WHERE user_id = @UserId AND plan_id = @PlanId AND status = 'Pending' 
                     AND created_at > @ExpireTime
                     ORDER BY created_at DESC LIMIT 1;";
        return await _connection.QueryFirstOrDefaultAsync<Payment>(sql, new
        {
            UserId = userId,
            PlanId = planId,
            ExpireTime = DateTime.UtcNow.AddMinutes(-15) // Payment pending hết hiệu lực sau 15 phút
        });
    }

    public async Task<int> UpdateStatusAsync(Guid paymentId, string status, string? transactionId, string? responseCode)
    {
        var sql = @"UPDATE payments 
                     SET status = @Status, 
                         transaction_id = @TransactionId, 
                         response_code = @ResponseCode, 
                         updated_at = @UpdatedAt 
                     WHERE payment_id = @PaymentId;";
        return await _connection.ExecuteAsync(sql, new
        {
            PaymentId = paymentId,
            Status = status,
            TransactionId = transactionId,
            ResponseCode = responseCode,
            UpdatedAt = DateTime.UtcNow
        });
    }
}
