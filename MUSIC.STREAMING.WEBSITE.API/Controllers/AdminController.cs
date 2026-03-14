using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MUSIC.STREAMING.WEBSITE.API.Extensions;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

namespace MUSIC.STREAMING.WEBSITE.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly INotificationService _notificationService;

    public AdminController(IAdminService adminService, INotificationService notificationService)
    {
        _adminService = adminService;
        _notificationService = notificationService;
    }

    // ==================== 1. Dashboard Stats ====================

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var result = await _adminService.GetDashboardStatsAsync();
        return result.ToActionResult();
    }

    // ==================== 2. User Management ====================

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? keyword,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 15,
        [FromQuery] string? role = null)
    {
        var result = await _adminService.GetUsersAsync(keyword ?? "", pageIndex, pageSize, role);
        return result.ToActionResult();
    }

    [HttpPost("users/{userId}/toggle-ban")]
    public async Task<IActionResult> ToggleBanUser(Guid userId)
    {
        var result = await _adminService.ToggleBanUserAsync(userId);
        return result.ToActionResult();
    }

    // ==================== 3. Artist Management ====================

    [HttpGet("artists")]
    public async Task<IActionResult> GetArtists(
        [FromQuery] string? keyword,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 15)
    {
        var result = await _adminService.GetArtistsAsync(keyword ?? "", pageIndex, pageSize);
        return result.ToActionResult();
    }

    // ==================== 4. Song Management ====================

    [HttpGet("songs")]
    public async Task<IActionResult> GetSongs(
        [FromQuery] string? keyword,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 15)
    {
        var result = await _adminService.GetSongsAsync(keyword ?? "", pageIndex, pageSize);
        return result.ToActionResult();
    }

    [HttpDelete("songs/{songId}")]
    public async Task<IActionResult> DeleteSong(Guid songId)
    {
        var result = await _adminService.DeleteSongAsync(songId);
        return result.ToActionResult();
    }

    // ==================== 5. Subscription Management ====================

    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions(
        [FromQuery] string? keyword,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 15,
        [FromQuery] string? status = null)
    {
        var result = await _adminService.GetSubscriptionsAsync(keyword ?? "", pageIndex, pageSize, status);
        return result.ToActionResult();
    }

    // ==================== 6. Payment Management ====================

    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments(
        [FromQuery] string? keyword,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 15,
        [FromQuery] string? status = null)
    {
        var result = await _adminService.GetPaymentsAsync(keyword ?? "", pageIndex, pageSize, status);
        return result.ToActionResult();
    }

    [HttpGet("payments/pending")]
    public async Task<IActionResult> GetPendingPayments(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 15)
    {
        var result = await _adminService.GetPendingPaymentsAsync(pageIndex, pageSize);
        return result.ToActionResult();
    }

    [HttpPost("payments/{paymentId}/approve")]
    public async Task<IActionResult> ApprovePayment(Guid paymentId)
    {
        var result = await _adminService.ApprovePaymentAsync(paymentId);
        return result.ToActionResult();
    }

    [HttpPost("payments/{paymentId}/reject")]
    public async Task<IActionResult> RejectPayment(Guid paymentId)
    {
        var result = await _adminService.RejectPaymentAsync(paymentId);
        return result.ToActionResult();
    }

    // ==================== 7. Set User Role ====================

    [HttpPost("users/{userId}/set-role")]
    public async Task<IActionResult> SetUserRole(Guid userId, [FromBody] AdminSetRoleDto dto)
    {
        var result = await _adminService.SetUserRoleAsync(userId, dto);
        return result.ToActionResult();
    }

    // ==================== 8. Update Payment Status ====================

    [HttpPut("payments/{paymentId}/status")]
    public async Task<IActionResult> UpdatePaymentStatus(Guid paymentId, [FromBody] AdminUpdatePaymentStatusDto dto)
    {
        var result = await _adminService.UpdatePaymentStatusAsync(paymentId, dto);
        return result.ToActionResult();
    }

    // ==================== 9. Cancel Expired Payments ====================

    [HttpPost("payments/cancel-expired")]
    public async Task<IActionResult> CancelExpiredPayments([FromQuery] int daysThreshold = 15)
    {
        var result = await _adminService.CancelExpiredPaymentsAsync(daysThreshold);
        return result.ToActionResult();
    }

    // ==================== 10. Send Notification ====================

    [HttpPost("notifications/send")]
    public async Task<IActionResult> SendNotification([FromBody] SendNotificationDto dto)
    {
        var result = await _notificationService.SendNotificationAsync(dto);
        return result.ToActionResult();
    }
}
