using Microsoft.Extensions.Logging;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

namespace MUSIC.STREAMING.WEBSITE.Core.Services;

public class AdminService : IAdminService
{
    private readonly IAdminRepository _adminRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<AdminService> _logger;

    private static readonly HashSet<string> ValidRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "User", "Artist", "ArtistPremium", "Admin"
    };

    private static readonly HashSet<string> ValidPaymentStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pending", "Success", "Failed", "Rejected", "Cancelled"
    };

    public AdminService(IAdminRepository adminRepository, INotificationService notificationService, ILogger<AdminService> logger)
    {
        _adminRepository = adminRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    // ==================== Dashboard ====================
    public async Task<Result<DashboardStatsDto>> GetDashboardStatsAsync()
    {
        try
        {
            var stats = await _adminRepository.GetDashboardStatsAsync();
            return Result<DashboardStatsDto>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy dashboard stats");
            return Result<DashboardStatsDto>.Failure("Lỗi khi lấy thống kê dashboard");
        }
    }

    // ==================== Users ====================
    public async Task<Result<AdminPagingResult<AdminUserDto>>> GetUsersAsync(string keyword, int pageIndex, int pageSize, string? role)
    {
        try
        {
            var result = await _adminRepository.GetUsersAsync(keyword, pageIndex, pageSize, role);
            return Result<AdminPagingResult<AdminUserDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách users");
            return Result<AdminPagingResult<AdminUserDto>>.Failure("Lỗi khi lấy danh sách users");
        }
    }

    public async Task<Result<ToggleBanResponseDto>> ToggleBanUserAsync(Guid userId)
    {
        try
        {
            var currentBanStatus = await _adminRepository.GetUserBanStatusAsync(userId);
            if (currentBanStatus == null)
                return Result<ToggleBanResponseDto>.NotFound("User không tồn tại");

            var success = await _adminRepository.ToggleBanUserAsync(userId);
            if (!success)
                return Result<ToggleBanResponseDto>.Failure("Không thể thay đổi trạng thái ban");

            var newIsBanned = !currentBanStatus.Value;
            return Result<ToggleBanResponseDto>.Success(new ToggleBanResponseDto
            {
                Success = true,
                Message = newIsBanned ? "User has been banned" : "User has been unbanned",
                IsBanned = newIsBanned
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi toggle ban user {UserId}", userId);
            return Result<ToggleBanResponseDto>.Failure("Lỗi khi thay đổi trạng thái ban");
        }
    }

    // ==================== Artists ====================
    public async Task<Result<AdminPagingResult<AdminArtistDto>>> GetArtistsAsync(string keyword, int pageIndex, int pageSize)
    {
        try
        {
            var result = await _adminRepository.GetArtistsAsync(keyword, pageIndex, pageSize);
            return Result<AdminPagingResult<AdminArtistDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách artists");
            return Result<AdminPagingResult<AdminArtistDto>>.Failure("Lỗi khi lấy danh sách artists");
        }
    }

    // ==================== Songs ====================
    public async Task<Result<AdminPagingResult<AdminSongDto>>> GetSongsAsync(string keyword, int pageIndex, int pageSize)
    {
        try
        {
            var result = await _adminRepository.GetSongsAsync(keyword, pageIndex, pageSize);
            return Result<AdminPagingResult<AdminSongDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách songs");
            return Result<AdminPagingResult<AdminSongDto>>.Failure("Lỗi khi lấy danh sách songs");
        }
    }

    public async Task<Result> DeleteSongAsync(Guid songId)
    {
        try
        {
            var success = await _adminRepository.DeleteSongAsync(songId);
            if (!success)
                return Result.NotFound("Bài hát không tồn tại");

            return Result.Success("Song deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa song {SongId}", songId);
            return Result.Failure("Lỗi khi xóa bài hát");
        }
    }

    // ==================== Subscriptions ====================
    public async Task<Result<AdminPagingResult<AdminSubscriptionDto>>> GetSubscriptionsAsync(string keyword, int pageIndex, int pageSize, string? status)
    {
        try
        {
            var result = await _adminRepository.GetSubscriptionsAsync(keyword, pageIndex, pageSize, status);
            return Result<AdminPagingResult<AdminSubscriptionDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách subscriptions");
            return Result<AdminPagingResult<AdminSubscriptionDto>>.Failure("Lỗi khi lấy danh sách subscriptions");
        }
    }

    // ==================== Payments ====================
    public async Task<Result<AdminPagingResult<AdminPaymentDto>>> GetPaymentsAsync(string keyword, int pageIndex, int pageSize, string? status)
    {
        try
        {
            var result = await _adminRepository.GetPaymentsAsync(keyword, pageIndex, pageSize, status);
            return Result<AdminPagingResult<AdminPaymentDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách payments");
            return Result<AdminPagingResult<AdminPaymentDto>>.Failure("Lỗi khi lấy danh sách payments");
        }
    }

    public async Task<Result<AdminPagingResult<AdminPaymentDto>>> GetPendingPaymentsAsync(int pageIndex, int pageSize)
    {
        try
        {
            var result = await _adminRepository.GetPendingPaymentsAsync(pageIndex, pageSize);
            return Result<AdminPagingResult<AdminPaymentDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách pending payments");
            return Result<AdminPagingResult<AdminPaymentDto>>.Failure("Lỗi khi lấy danh sách pending payments");
        }
    }

    public async Task<Result> ApprovePaymentAsync(Guid paymentId)
    {
        try
        {
            // Get payment info for notification before approving
            var paymentInfo = await _adminRepository.GetPaymentWithPlanAsync(paymentId);

            var success = await _adminRepository.ApprovePaymentAsync(paymentId);
            if (!success)
                return Result.NotFound("Payment không tồn tại hoặc không ở trạng thái Pending");

            // Send notification
            if (paymentInfo != null)
            {
                await _notificationService.SendSystemNotificationAsync(
                    (Guid)paymentInfo.UserId,
                    "Payment Approved",
                    $"Your payment for {(string)paymentInfo.PlanName} has been approved!",
                    "payment_approved",
                    paymentId);
            }

            return Result.Success("Payment approved and subscription activated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi approve payment {PaymentId}", paymentId);
            return Result.Failure("Lỗi khi duyệt payment");
        }
    }

    public async Task<Result> RejectPaymentAsync(Guid paymentId)
    {
        try
        {
            // Get payment info for notification
            var paymentInfo = await _adminRepository.GetPaymentWithPlanAsync(paymentId);

            var success = await _adminRepository.RejectPaymentAsync(paymentId);
            if (!success)
                return Result.NotFound("Payment không tồn tại hoặc không ở trạng thái Pending");

            // Send notification
            if (paymentInfo != null)
            {
                await _notificationService.SendSystemNotificationAsync(
                    (Guid)paymentInfo.UserId,
                    "Payment Rejected",
                    $"Your payment for {(string)paymentInfo.PlanName} was rejected",
                    "payment_rejected",
                    paymentId);
            }

            return Result.Success("Payment rejected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi reject payment {PaymentId}", paymentId);
            return Result.Failure("Lỗi khi từ chối payment");
        }
    }

    // ==================== Set Role ====================
    public async Task<Result<AdminSetRoleResponseDto>> SetUserRoleAsync(Guid userId, AdminSetRoleDto dto)
    {
        try
        {
            if (!ValidRoles.Contains(dto.Role))
                return Result<AdminSetRoleResponseDto>.Failure($"Role không hợp lệ. Chỉ chấp nhận: {string.Join(", ", ValidRoles)}");

            var currentRole = await _adminRepository.GetUserRoleAsync(userId);
            if (currentRole == null)
                return Result<AdminSetRoleResponseDto>.NotFound("User không tồn tại");

            // Normalize role
            var normalizedRole = ValidRoles.First(r => r.Equals(dto.Role, StringComparison.OrdinalIgnoreCase));

            var success = await _adminRepository.SetUserRoleAsync(userId, normalizedRole);
            if (!success)
                return Result<AdminSetRoleResponseDto>.Failure("Không thể cập nhật role");

            // Send notification to user
            await _notificationService.SendSystemNotificationAsync(
                userId,
                "Role Updated",
                $"Your role has been updated to {normalizedRole}",
                "role_changed");

            return Result<AdminSetRoleResponseDto>.Success(new AdminSetRoleResponseDto
            {
                Message = "Role updated successfully",
                UserId = userId,
                NewRole = normalizedRole
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi set role cho user {UserId}", userId);
            return Result<AdminSetRoleResponseDto>.Failure("Lỗi khi cập nhật role");
        }
    }

    // ==================== Update Payment Status ====================
    public async Task<Result<AdminUpdatePaymentStatusResponseDto>> UpdatePaymentStatusAsync(Guid paymentId, AdminUpdatePaymentStatusDto dto)
    {
        try
        {
            if (!ValidPaymentStatuses.Contains(dto.Status))
                return Result<AdminUpdatePaymentStatusResponseDto>.Failure(
                    $"Status không hợp lệ. Chỉ chấp nhận: {string.Join(", ", ValidPaymentStatuses)}");

            var normalizedStatus = ValidPaymentStatuses.First(s => s.Equals(dto.Status, StringComparison.OrdinalIgnoreCase));

            var result = await _adminRepository.UpdatePaymentStatusAsync(paymentId, normalizedStatus);
            if (result == null)
                return Result<AdminUpdatePaymentStatusResponseDto>.NotFound("Payment không tồn tại");

            // Send notification based on status
            var paymentInfo = await _adminRepository.GetPaymentWithPlanAsync(paymentId);
            if (paymentInfo != null)
            {
                var (notifType, notifTitle, notifMessage) = normalizedStatus switch
                {
                    "Success" => ("payment_approved", "Payment Approved", $"Your payment for {(string)paymentInfo.PlanName} has been approved!"),
                    "Rejected" => ("payment_rejected", "Payment Rejected", $"Your payment for {(string)paymentInfo.PlanName} was rejected"),
                    "Cancelled" => ("payment_expired", "Payment Cancelled", $"Your payment for {(string)paymentInfo.PlanName} has been cancelled"),
                    _ => ("system", "Payment Updated", $"Your payment status has been updated to {normalizedStatus}")
                };

                await _notificationService.SendSystemNotificationAsync(
                    (Guid)paymentInfo.UserId, notifTitle, notifMessage, notifType, paymentId);
            }

            return Result<AdminUpdatePaymentStatusResponseDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi update payment status {PaymentId}", paymentId);
            return Result<AdminUpdatePaymentStatusResponseDto>.Failure("Lỗi khi cập nhật trạng thái payment");
        }
    }

    // ==================== Cancel Expired Payments ====================
    public async Task<Result<CancelExpiredPaymentsResponseDto>> CancelExpiredPaymentsAsync(int daysThreshold)
    {
        try
        {
            var result = await _adminRepository.CancelExpiredPaymentsAsync(daysThreshold);

            // Send notifications to each affected user
            // Note: For performance, this is fire-and-forget style
            foreach (var paymentId in result.CancelledPaymentIds)
            {
                var paymentInfo = await _adminRepository.GetPaymentWithPlanAsync(paymentId);
                if (paymentInfo != null)
                {
                    await _notificationService.SendSystemNotificationAsync(
                        (Guid)paymentInfo.UserId,
                        "Payment Cancelled",
                        $"Your pending payment for {(string)paymentInfo.PlanName} has expired and was cancelled",
                        "payment_expired",
                        paymentId);
                }
            }

            return Result<CancelExpiredPaymentsResponseDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cancel expired payments");
            return Result<CancelExpiredPaymentsResponseDto>.Failure("Lỗi khi hủy payment hết hạn");
        }
    }
}
