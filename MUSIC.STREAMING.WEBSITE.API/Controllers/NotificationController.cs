using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MUSIC.STREAMING.WEBSITE.API.Extensions;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

namespace MUSIC.STREAMING.WEBSITE.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Lấy danh sách thông báo của user hiện tại
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
    {
        var userId = Guid.Parse(User.FindFirst("UserId")?.Value!);
        var result = await _notificationService.GetNotificationsAsync(userId, pageIndex, pageSize);
        return result.ToActionResult();
    }

    /// <summary>
    /// Đếm thông báo chưa đọc
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = Guid.Parse(User.FindFirst("UserId")?.Value!);
        var result = await _notificationService.GetUnreadCountAsync(userId);
        return result.ToActionResult();
    }

    /// <summary>
    /// Đánh dấu 1 thông báo đã đọc
    /// </summary>
    [HttpPost("{notificationId}/read")]
    public async Task<IActionResult> MarkAsRead(Guid notificationId)
    {
        var userId = Guid.Parse(User.FindFirst("UserId")?.Value!);
        var result = await _notificationService.MarkAsReadAsync(notificationId, userId);
        return result.ToActionResult();
    }

    /// <summary>
    /// Đánh dấu tất cả thông báo đã đọc
    /// </summary>
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = Guid.Parse(User.FindFirst("UserId")?.Value!);
        var result = await _notificationService.MarkAllAsReadAsync(userId);
        if (result.IsSuccess)
            return Ok(new { Message = "All notifications marked as read", UpdatedCount = result.Data });
        return result.ToActionResult();
    }
}
