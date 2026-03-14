using System;
using System.Data;
using Dapper;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Entities;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;

namespace MUSIC.STREAMING.WEBSITE.Infrastructure.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(IDbConnection connection) : base(connection)
    {
    }

    public async Task<User> GetByEmailAsync(string email)
    {
        var sql = "SELECT * FROM users WHERE email = @Email LIMIT 1";
        return await _connection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email });
    }

    public async Task<User> GetByUsernameAsync(string username)
    {
        var sql = "SELECT * FROM users WHERE username = @Username LIMIT 1";
        return await _connection.QueryFirstOrDefaultAsync<User>(sql, new { Username = username });
    }

    public async Task<User> GetByUsernameOrEmailAsync(string identifier)
    {
        var sql = "SELECT * FROM users WHERE email = @Id OR username = @Id LIMIT 1";
        return await _connection.QueryFirstOrDefaultAsync<User>(sql, new { Id = identifier });
    }

    public async Task<User?> GetByResetTokenAsync(string token)
    {
        var sql = "SELECT * FROM users WHERE reset_token = @Token";
        return await _connection.QueryFirstOrDefaultAsync<User>(sql, new { Token = token });
    }

    public async Task<PagingResult<User>> GetArtistsPagingAsync(string keyword, int pageIndex, int pageSize)
    {
        var parameters = new DynamicParameters();

        string whereSql = "WHERE (role = 'Artist' OR role = 'ArtistPremium')";
        parameters.Add("Active", true);
        whereSql += " AND is_active = @Active";

        if (!string.IsNullOrEmpty(keyword))
        {
            whereSql += " AND (full_name LIKE @Keyword OR username LIKE @Keyword)";
            parameters.Add("Keyword", $"%{keyword}%");
        }

        string countSql = $"SELECT COUNT(1) FROM users {whereSql}";
        int totalRecords = await _connection.ExecuteScalarAsync<int>(countSql, parameters);

        int offset = (pageIndex - 1) * pageSize;
        parameters.Add("Offset", offset);
        parameters.Add("Limit", pageSize);

        string dataSql = $@"
            SELECT u.*, 
                   (SELECT COUNT(*) FROM user_follows uf WHERE uf.following_id = u.user_id) as FollowerCount
            FROM users u
            {whereSql} 
            ORDER BY FollowerCount DESC, 
                     CASE WHEN u.role = 'ArtistPremium' THEN 0 ELSE 1 END,
                     u.created_at DESC 
            LIMIT @Limit OFFSET @Offset";
        var items = await _connection.QueryAsync<User>(dataSql, parameters);
        int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

        int fromRecord = totalRecords == 0 ? 0 : offset + 1;

        int toRecord = totalRecords == 0 ? 0 : Math.Min(pageIndex * pageSize, totalRecords);

        return new PagingResult<User>
        {
            Data = items,
            TotalRecords = totalRecords,
            TotalPages = totalPages,
            FromRecord = fromRecord,
            ToRecord = toRecord
        };
    }

    public async Task AddUserFavoriteGenresAsync(Guid userId, List<Guid> genreIds)
    {
        var deleteSql = "DELETE FROM user_favorite_genres WHERE user_id = @UserId";
        await _connection.ExecuteAsync(deleteSql, new { UserId = userId });

        // Insert sở thích mới
        var insertSql = "INSERT INTO user_favorite_genres (user_id, genre_id) VALUES (@UserId, @GenreId)";

        // Dapper tự động loop qua List để insert nhiều dòng
        var parameters = genreIds.Select(gId => new { UserId = userId, GenreId = gId });
        await _connection.ExecuteAsync(insertSql, parameters);
    }

    public async Task<IEnumerable<Core.DTOs.GenreDto>> GetUserFavoriteGenresAsync(Guid userId)
    {
        var sql = @"
            SELECT g.id AS Id, g.name AS Name, g.image_url AS ImageUrl
            FROM user_favorite_genres ufg
            INNER JOIN genres g ON ufg.genre_id = g.id
            WHERE ufg.user_id = @UserId";
        return await _connection.QueryAsync<Core.DTOs.GenreDto>(sql, new { UserId = userId });
    }

    public async Task<Core.DTOs.ArtistStatsDto> GetArtistStatsAsync(Guid artistId)
    {
        var sql = @"
            SELECT 
                @ArtistId as ArtistId,
                (SELECT COUNT(*) FROM user_follows WHERE following_id = @ArtistId) as FollowerCount,
                (SELECT COUNT(DISTINCT sa.song_id) FROM song_artists sa WHERE sa.artist_id = @ArtistId) as SongCount,
                (SELECT COUNT(*) FROM user_likes ul 
                 JOIN song_artists sa ON ul.song_id = sa.song_id 
                 WHERE sa.artist_id = @ArtistId) as TotalLikes,
                (SELECT COALESCE(SUM(uss.play_count), 0) FROM user_song_stats uss 
                 JOIN song_artists sa ON uss.song_id = sa.song_id 
                 WHERE sa.artist_id = @ArtistId) as TotalListens";

        return await _connection.QueryFirstAsync<Core.DTOs.ArtistStatsDto>(sql, new { ArtistId = artistId });
    }

    public async Task<Dictionary<Guid, Core.DTOs.ArtistStatsDto>> GetArtistsStatsBatchAsync(IEnumerable<Guid> artistIds)
    {
        var idList = artistIds.ToList();
        if (!idList.Any()) return new Dictionary<Guid, Core.DTOs.ArtistStatsDto>();

        var sql = @"
            SELECT 
                u.user_id as ArtistId,
                (SELECT COUNT(*) FROM user_follows uf WHERE uf.following_id = u.user_id) as FollowerCount,
                (SELECT COUNT(DISTINCT sa.song_id) FROM song_artists sa WHERE sa.artist_id = u.user_id) as SongCount,
                (SELECT COUNT(*) FROM user_likes ul 
                 JOIN song_artists sa ON ul.song_id = sa.song_id 
                 WHERE sa.artist_id = u.user_id) as TotalLikes,
                (SELECT COALESCE(SUM(uss.play_count), 0) FROM user_song_stats uss 
                 JOIN song_artists sa ON uss.song_id = sa.song_id 
                 WHERE sa.artist_id = u.user_id) as TotalListens
            FROM users u
            WHERE u.user_id IN @ArtistIds";

        var stats = await _connection.QueryAsync<Core.DTOs.ArtistStatsDto>(sql, new { ArtistIds = idList });
        return stats.ToDictionary(s => s.ArtistId, s => s);
    }
}
