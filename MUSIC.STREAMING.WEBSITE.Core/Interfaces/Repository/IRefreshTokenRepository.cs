using System;
using MUSIC.STREAMING.WEBSITE.Core.Entities;

namespace MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;

public interface IRefreshTokenRepository
{
    /// <summary>
    /// Tạo refresh token mới
    /// </summary>
    Task<RefreshToken> CreateAsync(RefreshToken refreshToken);

    /// <summary>
    /// Lấy refresh token theo token string
    /// </summary>
    Task<RefreshToken?> GetByTokenAsync(string token);

    /// <summary>
    /// Lấy tất cả refresh token của user
    /// </summary>
    Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Revoke (thu hồi) một refresh token
    /// </summary>
    Task RevokeAsync(string token);

    /// <summary>
    /// Revoke tất cả refresh token của user
    /// </summary>
    Task RevokeAllByUserIdAsync(Guid userId);

    /// <summary>
    /// Xóa các token đã hết hạn
    /// </summary>
    Task CleanupExpiredTokensAsync();
}
