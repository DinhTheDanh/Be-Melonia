using MUSIC.STREAMING.WEBSITE.Core.DTOs;

namespace MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

public interface IFeatureAuthorizationService
{
    /// <summary>
    /// Lấy toàn bộ quyền/feature của user dựa trên subscription hiện tại
    /// </summary>
    Task<Result<UserFeatureDto>> GetUserFeaturesAsync(Guid userId);

    /// <summary>
    /// Kiểm tra user có thể upload thêm bài hát không
    /// </summary>
    Task<Result<bool>> CanUploadSongAsync(Guid userId);

    /// <summary>
    /// Kiểm tra user có quyền lên lịch phát hành không
    /// </summary>
    Task<Result<bool>> CanScheduleReleaseAsync(Guid userId);

    /// <summary>
    /// Kiểm tra user có quyền xem analytics nâng cao không
    /// </summary>
    Task<Result<bool>> HasAdvancedAnalyticsAsync(Guid userId);
}
