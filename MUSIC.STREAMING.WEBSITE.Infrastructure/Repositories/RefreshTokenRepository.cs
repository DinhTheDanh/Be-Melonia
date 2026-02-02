using System;
using System.Data;
using Dapper;
using MUSIC.STREAMING.WEBSITE.Core.Entities;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;

namespace MUSIC.STREAMING.WEBSITE.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IDbConnection _connection;

    public RefreshTokenRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<RefreshToken> CreateAsync(RefreshToken refreshToken)
    {
        var sql = @"
            INSERT INTO refresh_tokens (user_id, token, expires_at, created_at)
            VALUES (@UserId, @Token, @ExpiresAt, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        var id = await _connection.ExecuteScalarAsync<int>(sql, new
        {
            refreshToken.UserId,
            refreshToken.Token,
            refreshToken.ExpiresAt,
            refreshToken.CreatedAt
        });

        refreshToken.Id = id;
        return refreshToken;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        var sql = @"
            SELECT id as Id, user_id as UserId, token as Token, 
                   expires_at as ExpiresAt, created_at as CreatedAt, revoked_at as RevokedAt
            FROM refresh_tokens 
            WHERE token = @Token";

        return await _connection.QueryFirstOrDefaultAsync<RefreshToken>(sql, new { Token = token });
    }

    public async Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid userId)
    {
        var sql = @"
            SELECT id as Id, user_id as UserId, token as Token, 
                   expires_at as ExpiresAt, created_at as CreatedAt, revoked_at as RevokedAt
            FROM refresh_tokens 
            WHERE user_id = @UserId AND revoked_at IS NULL AND expires_at > NOW()";

        return await _connection.QueryAsync<RefreshToken>(sql, new { UserId = userId });
    }

    public async Task RevokeAsync(string token)
    {
        var sql = "UPDATE refresh_tokens SET revoked_at = NOW() WHERE token = @Token";
        await _connection.ExecuteAsync(sql, new { Token = token });
    }

    public async Task RevokeAllByUserIdAsync(Guid userId)
    {
        var sql = "UPDATE refresh_tokens SET revoked_at = NOW() WHERE user_id = @UserId AND revoked_at IS NULL";
        await _connection.ExecuteAsync(sql, new { UserId = userId });
    }

    public async Task CleanupExpiredTokensAsync()
    {
        var sql = "DELETE FROM refresh_tokens WHERE expires_at < NOW() OR revoked_at IS NOT NULL";
        await _connection.ExecuteAsync(sql);
    }
}
