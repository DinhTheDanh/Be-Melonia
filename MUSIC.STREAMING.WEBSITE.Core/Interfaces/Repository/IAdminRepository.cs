using MUSIC.STREAMING.WEBSITE.Core.DTOs;

namespace MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;

public interface IAdminRepository
{
    // Dashboard
    Task<DashboardStatsDto> GetDashboardStatsAsync();

    // Users
    Task<AdminPagingResult<AdminUserDto>> GetUsersAsync(string keyword, int pageIndex, int pageSize, string? role);
    Task<bool> ToggleBanUserAsync(Guid userId);
    Task<bool?> GetUserBanStatusAsync(Guid userId);
    Task<string?> GetUserRoleAsync(Guid userId);
    Task<bool> SetUserRoleAsync(Guid userId, string role);

    // Artists
    Task<AdminPagingResult<AdminArtistDto>> GetArtistsAsync(string keyword, int pageIndex, int pageSize);

    // Songs
    Task<AdminPagingResult<AdminSongDto>> GetSongsAsync(string keyword, int pageIndex, int pageSize);
    Task<bool> DeleteSongAsync(Guid songId);

    // Subscriptions
    Task<AdminPagingResult<AdminSubscriptionDto>> GetSubscriptionsAsync(string keyword, int pageIndex, int pageSize, string? status);

    // Payments
    Task<AdminPagingResult<AdminPaymentDto>> GetPaymentsAsync(string keyword, int pageIndex, int pageSize, string? status);
    Task<AdminPagingResult<AdminPaymentDto>> GetPendingPaymentsAsync(int pageIndex, int pageSize);
    Task<bool> ApprovePaymentAsync(Guid paymentId);
    Task<bool> RejectPaymentAsync(Guid paymentId);
    Task<AdminUpdatePaymentStatusResponseDto?> UpdatePaymentStatusAsync(Guid paymentId, string newStatus);
    Task<CancelExpiredPaymentsResponseDto> CancelExpiredPaymentsAsync(int daysThreshold);

    // Helpers
    Task<dynamic?> GetPaymentWithPlanAsync(Guid paymentId);
}
