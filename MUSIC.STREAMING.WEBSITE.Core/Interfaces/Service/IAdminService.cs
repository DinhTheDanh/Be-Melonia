using MUSIC.STREAMING.WEBSITE.Core.DTOs;

namespace MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

public interface IAdminService
{
    // Dashboard
    Task<Result<DashboardStatsDto>> GetDashboardStatsAsync();

    // Users
    Task<Result<AdminPagingResult<AdminUserDto>>> GetUsersAsync(string keyword, int pageIndex, int pageSize, string? role);
    Task<Result<ToggleBanResponseDto>> ToggleBanUserAsync(Guid userId);
    Task<Result<AdminSetRoleResponseDto>> SetUserRoleAsync(Guid userId, AdminSetRoleDto dto);

    // Artists
    Task<Result<AdminPagingResult<AdminArtistDto>>> GetArtistsAsync(string keyword, int pageIndex, int pageSize);

    // Songs
    Task<Result<AdminPagingResult<AdminSongDto>>> GetSongsAsync(string keyword, int pageIndex, int pageSize);
    Task<Result> DeleteSongAsync(Guid songId);

    // Subscriptions
    Task<Result<AdminPagingResult<AdminSubscriptionDto>>> GetSubscriptionsAsync(string keyword, int pageIndex, int pageSize, string? status);

    // Payments
    Task<Result<AdminPagingResult<AdminPaymentDto>>> GetPaymentsAsync(string keyword, int pageIndex, int pageSize, string? status);
    Task<Result<AdminPagingResult<AdminPaymentDto>>> GetPendingPaymentsAsync(int pageIndex, int pageSize);
    Task<Result> ApprovePaymentAsync(Guid paymentId);
    Task<Result> RejectPaymentAsync(Guid paymentId);
    Task<Result<AdminUpdatePaymentStatusResponseDto>> UpdatePaymentStatusAsync(Guid paymentId, AdminUpdatePaymentStatusDto dto);
    Task<Result<CancelExpiredPaymentsResponseDto>> CancelExpiredPaymentsAsync(int daysThreshold);
}
