using MUSIC.STREAMING.WEBSITE.Core.DTOs;

namespace MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

public interface INotificationService
{
    /// <summary>
    /// Lấy danh sách thông báo của user
    /// </summary>
    Task<Result<PagingResult<NotificationDto>>> GetNotificationsAsync(Guid userId, int pageIndex, int pageSize);

    /// <summary>
    /// Đếm thông báo chưa đọc
    /// </summary>
    Task<Result<UnreadCountDto>> GetUnreadCountAsync(Guid userId);

    /// <summary>
    /// Đánh dấu 1 thông báo đã đọc
    /// </summary>
    Task<Result> MarkAsReadAsync(Guid notificationId, Guid userId);

    /// <summary>
    /// Đánh dấu tất cả đã đọc
    /// </summary>
    Task<Result<int>> MarkAllAsReadAsync(Guid userId);

    /// <summary>
    /// Gửi thông báo cho user (dùng bởi Admin hoặc hệ thống)
    /// </summary>
    Task<Result<Guid>> SendNotificationAsync(SendNotificationDto dto);

    /// <summary>
    /// Gửi thông báo hệ thống (internal - không cần Result wrapper)
    /// </summary>
    Task SendSystemNotificationAsync(Guid userId, string title, string message, string type, Guid? relatedEntityId = null);
}
