using System.Data;
using Dapper;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Entities;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;

namespace MUSIC.STREAMING.WEBSITE.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly IDbConnection _connection;

    public NotificationRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<Guid> CreateAsync(Notification notification)
    {
        var sql = @"
            INSERT INTO notifications (id, user_id, title, message, type, is_read, related_entity_id, created_at)
            VALUES (@Id, @UserId, @Title, @Message, @Type, @IsRead, @RelatedEntityId, @CreatedAt)";

        await _connection.ExecuteAsync(sql, new
        {
            notification.Id,
            notification.UserId,
            notification.Title,
            notification.Message,
            notification.Type,
            notification.IsRead,
            notification.RelatedEntityId,
            notification.CreatedAt
        });

        return notification.Id;
    }

    public async Task<PagingResult<NotificationDto>> GetByUserIdAsync(Guid userId, int pageIndex, int pageSize)
    {
        var countSql = "SELECT COUNT(*) FROM notifications WHERE user_id = @UserId";
        var totalCount = await _connection.ExecuteScalarAsync<int>(countSql, new { UserId = userId });

        var offset = (pageIndex - 1) * pageSize;
        var dataSql = @"
            SELECT 
                id AS Id,
                title AS Title,
                message AS Message,
                type AS Type,
                is_read AS IsRead,
                related_entity_id AS RelatedEntityId,
                created_at AS CreatedAt
            FROM notifications
            WHERE user_id = @UserId
            ORDER BY created_at DESC
            LIMIT @Limit OFFSET @Offset";

        var data = await _connection.QueryAsync<NotificationDto>(dataSql, new
        {
            UserId = userId,
            Limit = pageSize,
            Offset = offset
        });

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        return new PagingResult<NotificationDto>
        {
            Data = data,
            TotalRecords = totalCount,
            TotalPages = totalPages,
            FromRecord = totalCount == 0 ? 0 : offset + 1,
            ToRecord = totalCount == 0 ? 0 : Math.Min(pageIndex * pageSize, totalCount)
        };
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        var sql = "SELECT COUNT(*) FROM notifications WHERE user_id = @UserId AND is_read = 0";
        return await _connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
    }

    public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        var sql = @"
            UPDATE notifications 
            SET is_read = 1 
            WHERE id = @Id AND user_id = @UserId AND is_read = 0";
        var rows = await _connection.ExecuteAsync(sql, new { Id = notificationId, UserId = userId });
        return rows > 0;
    }

    public async Task<int> MarkAllAsReadAsync(Guid userId)
    {
        var sql = @"
            UPDATE notifications 
            SET is_read = 1 
            WHERE user_id = @UserId AND is_read = 0";
        return await _connection.ExecuteAsync(sql, new { UserId = userId });
    }
}
