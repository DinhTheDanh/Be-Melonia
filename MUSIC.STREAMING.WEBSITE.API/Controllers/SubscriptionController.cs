using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MUSIC.STREAMING.WEBSITE.API.Extensions;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

namespace MUSIC.STREAMING.WEBSITE.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IFeatureAuthorizationService _featureAuthService;

    public SubscriptionController(
        ISubscriptionService subscriptionService,
        IFeatureAuthorizationService featureAuthService)
    {
        _subscriptionService = subscriptionService;
        _featureAuthService = featureAuthService;
    }

    /// <summary>
    /// Lấy danh sách tất cả gói subscription (cho trang pricing)
    /// </summary>
    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllPlans()
    {
        var result = await _subscriptionService.GetAllPlansAsync();
        return result.ToActionResult();
    }

    /// <summary>
    /// Lấy subscription đang hoạt động của user
    /// </summary>
    [HttpGet("active")]
    [Authorize]
    public async Task<IActionResult> GetActiveSubscription()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            return Unauthorized(new { Message = "Vui lòng đăng nhập" });

        var userId = Guid.Parse(userIdClaim);
        var result = await _subscriptionService.GetActiveSubscriptionAsync(userId);
        return result.ToActionResult();
    }

    /// <summary>
    /// Lấy lịch sử subscription của user
    /// </summary>
    [HttpGet("history")]
    [Authorize]
    public async Task<IActionResult> GetSubscriptionHistory()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            return Unauthorized(new { Message = "Vui lòng đăng nhập" });

        var userId = Guid.Parse(userIdClaim);
        var result = await _subscriptionService.GetSubscriptionHistoryAsync(userId);
        return result.ToActionResult();
    }

    // ==================== Feature Authorization Endpoints ====================

    /// <summary>
    /// Lấy toàn bộ quyền/feature của user (dùng cho frontend check quyền)
    /// </summary>
    [HttpGet("features")]
    [Authorize]
    public async Task<IActionResult> GetUserFeatures()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            return Unauthorized(new { Message = "Vui lòng đăng nhập" });

        var userId = Guid.Parse(userIdClaim);
        var result = await _featureAuthService.GetUserFeaturesAsync(userId);
        return result.ToActionResult();
    }

    /// <summary>
    /// Kiểm tra quyền upload bài hát
    /// </summary>
    [HttpGet("features/can-upload")]
    [Authorize]
    public async Task<IActionResult> CanUploadSong()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            return Unauthorized(new { Message = "Vui lòng đăng nhập" });

        var userId = Guid.Parse(userIdClaim);
        var result = await _featureAuthService.CanUploadSongAsync(userId);
        return result.ToActionResult();
    }

    /// <summary>
    /// Kiểm tra quyền lên lịch phát hành
    /// </summary>
    [HttpGet("features/can-schedule")]
    [Authorize]
    public async Task<IActionResult> CanScheduleRelease()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            return Unauthorized(new { Message = "Vui lòng đăng nhập" });

        var userId = Guid.Parse(userIdClaim);
        var result = await _featureAuthService.CanScheduleReleaseAsync(userId);
        return result.ToActionResult();
    }

    /// <summary>
    /// Kiểm tra quyền xem analytics nâng cao
    /// </summary>
    [HttpGet("features/has-analytics")]
    [Authorize]
    public async Task<IActionResult> HasAdvancedAnalytics()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            return Unauthorized(new { Message = "Vui lòng đăng nhập" });

        var userId = Guid.Parse(userIdClaim);
        var result = await _featureAuthService.HasAdvancedAnalyticsAsync(userId);
        return result.ToActionResult();
    }
}
