using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Entities;

namespace MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;

public interface INotificationRepository
{
    Task<Guid> CreateAsync(Notification notification);
    Task<PagingResult<NotificationDto>> GetByUserIdAsync(Guid userId, int pageIndex, int pageSize);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId);
    Task<int> MarkAllAsReadAsync(Guid userId);
}
