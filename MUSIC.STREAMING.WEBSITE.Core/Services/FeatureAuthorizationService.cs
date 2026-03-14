using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

namespace MUSIC.STREAMING.WEBSITE.Core.Services;

public class FeatureAuthorizationService : IFeatureAuthorizationService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly IDbConnection _connection;
    private readonly ILogger<FeatureAuthorizationService> _logger;

    public FeatureAuthorizationService(
        ISubscriptionRepository subscriptionRepository,
        ISubscriptionPlanRepository planRepository,
        IDbConnection connection,
        ILogger<FeatureAuthorizationService> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _planRepository = planRepository;
        _connection = connection;
        _logger = logger;
    }

    public async Task<Result<UserFeatureDto>> GetUserFeaturesAsync(Guid userId)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetActiveSubscriptionAsync(userId);

            if (subscription == null)
            {
                // User không có subscription → không có feature nào
                return Result<UserFeatureDto>.Success(new UserFeatureDto
                {
                    UploadLimit = 0,
                    CurrentUploadCount = 0,
                    CanScheduleRelease = false,
                    HasAdvancedAnalytics = false,
                    HasActiveSubscription = false,
                    CurrentPlanName = null,
                    SubscriptionEndDate = null
                });
            }

            var plan = await _planRepository.GetByIdAsync(subscription.PlanId);
            if (plan == null)
            {
                return Result<UserFeatureDto>.Failure("Không tìm thấy thông tin gói subscription");
            }

            // Đếm số bài hát user đã upload
            var uploadCount = await CountUserSongsAsync(userId);

            return Result<UserFeatureDto>.Success(new UserFeatureDto
            {
                UploadLimit = plan.UploadLimit,
                CurrentUploadCount = uploadCount,
                CanScheduleRelease = plan.CanScheduleRelease,
                HasAdvancedAnalytics = plan.HasAdvancedAnalytics,
                HasActiveSubscription = true,
                CurrentPlanName = plan.PlanName,
                SubscriptionEndDate = subscription.EndDate
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user features for UserId={UserId}", userId);
            return Result<UserFeatureDto>.Failure("Lỗi khi kiểm tra quyền");
        }
    }

    public async Task<Result<bool>> CanUploadSongAsync(Guid userId)
    {
        try
        {
            var featuresResult = await GetUserFeaturesAsync(userId);
            if (featuresResult.IsFailure)
                return Result<bool>.Failure(featuresResult.Error!);

            var features = featuresResult.Data!;

            if (!features.HasActiveSubscription)
                return Result<bool>.Success(false);

            // -1 = không giới hạn
            if (features.UploadLimit == -1)
                return Result<bool>.Success(true);

            return Result<bool>.Success(features.CurrentUploadCount < features.UploadLimit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking upload permission for UserId={UserId}", userId);
            return Result<bool>.Failure("Lỗi khi kiểm tra quyền upload");
        }
    }

    public async Task<Result<bool>> CanScheduleReleaseAsync(Guid userId)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetActiveSubscriptionAsync(userId);
            if (subscription == null)
                return Result<bool>.Success(false);

            var plan = await _planRepository.GetByIdAsync(subscription.PlanId);
            return Result<bool>.Success(plan?.CanScheduleRelease ?? false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking schedule release permission for UserId={UserId}", userId);
            return Result<bool>.Failure("Lỗi khi kiểm tra quyền lên lịch phát hành");
        }
    }

    public async Task<Result<bool>> HasAdvancedAnalyticsAsync(Guid userId)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetActiveSubscriptionAsync(userId);
            if (subscription == null)
                return Result<bool>.Success(false);

            var plan = await _planRepository.GetByIdAsync(subscription.PlanId);
            return Result<bool>.Success(plan?.HasAdvancedAnalytics ?? false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking analytics permission for UserId={UserId}", userId);
            return Result<bool>.Failure("Lỗi khi kiểm tra quyền analytics");
        }
    }

    /// <summary>
    /// Đếm số bài hát mà user đã upload (thông qua song_artists)
    /// </summary>
    private async Task<int> CountUserSongsAsync(Guid userId)
    {
        var sql = @"SELECT COUNT(DISTINCT sa.song_id) 
                     FROM song_artists sa 
                     WHERE sa.artist_id = @UserId;";
        return await _connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
    }
}
