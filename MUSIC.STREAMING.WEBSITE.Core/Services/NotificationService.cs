using Microsoft.Extensions.Logging;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Entities;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

namespace MUSIC.STREAMING.WEBSITE.Core.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepo;
    private readonly INotificationHubService _hubService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(INotificationRepository notificationRepo, INotificationHubService hubService, ILogger<NotificationService> logger)
    {
        _notificationRepo = notificationRepo;
        _hubService = hubService;
        _logger = logger;
    }

    public async Task<Result<PagingResult<NotificationDto>>> GetNotificationsAsync(Guid userId, int pageIndex, int pageSize)
    {
        try
        {
            var result = await _notificationRepo.GetByUserIdAsync(userId, pageIndex, pageSize);
            return Result<PagingResult<NotificationDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thông báo cho user {UserId}", userId);
            return Result<PagingResult<NotificationDto>>.Failure("Lỗi khi lấy danh sách thông báo");
        }
    }

    public async Task<Result<UnreadCountDto>> GetUnreadCountAsync(Guid userId)
    {
        try
        {
            var count = await _notificationRepo.GetUnreadCountAsync(userId);
            return Result<UnreadCountDto>.Success(new UnreadCountDto { UnreadCount = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đếm thông báo chưa đọc cho user {UserId}", userId);
            return Result<UnreadCountDto>.Failure("Lỗi khi đếm thông báo chưa đọc");
        }
    }

    public async Task<Result> MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        try
        {
            var success = await _notificationRepo.MarkAsReadAsync(notificationId, userId);
            if (!success)
                return Result.NotFound("Thông báo không tồn tại hoặc đã được đọc");

            return Result.Success("Notification marked as read");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đánh dấu đã đọc notification {NotificationId}", notificationId);
            return Result.Failure("Lỗi khi đánh dấu đã đọc");
        }
    }

    public async Task<Result<int>> MarkAllAsReadAsync(Guid userId)
    {
        try
        {
            var count = await _notificationRepo.MarkAllAsReadAsync(userId);
            return Result<int>.Success(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đánh dấu tất cả đã đọc cho user {UserId}", userId);
            return Result<int>.Failure("Lỗi khi đánh dấu tất cả đã đọc");
        }
    }

    public async Task<Result<Guid>> SendNotificationAsync(SendNotificationDto dto)
    {
        try
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = dto.UserId,
                Title = dto.Title,
                Message = dto.Message,
                Type = dto.Type,
                IsRead = false,
                RelatedEntityId = dto.RelatedEntityId,
                CreatedAt = DateTime.UtcNow
            };

            var id = await _notificationRepo.CreateAsync(notification);

            // Push real-time via SignalR
            await _hubService.SendToUserAsync(dto.UserId, "ReceiveNotification", new
            {
                notification.Id,
                notification.Title,
                notification.Message,
                notification.Type,
                notification.CreatedAt
            });

            return Result<Guid>.Success(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gửi thông báo cho user {UserId}", dto.UserId);
            return Result<Guid>.Failure("Lỗi khi gửi thông báo");
        }
    }

    public async Task SendSystemNotificationAsync(Guid userId, string title, string message, string type, Guid? relatedEntityId = null)
    {
        try
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                RelatedEntityId = relatedEntityId,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepo.CreateAsync(notification);

            // Push real-time via SignalR
            await _hubService.SendToUserAsync(userId, "ReceiveNotification", new
            {
                notification.Id,
                notification.Title,
                notification.Message,
                notification.Type,
                notification.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gửi system notification cho user {UserId}", userId);
        }
    }
}
