using System;
using System.Data;
using Dapper;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Entities;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;

namespace MUSIC.STREAMING.WEBSITE.Infrastructure.Repositories;

public class PlaylistRepository : BaseRepository<Playlist>, IPlaylistRepository
{
    public PlaylistRepository(IDbConnection connection) : base(connection)
    {
    }

    public async Task<PagingResult<PlaylistDto>> GetUserPlaylistsAsync(Guid userId, string keyword, int pageIndex, int pageSize)
    {
        var p = new DynamicParameters();
        p.Add("UserId", userId);
        p.Add("Keyword", string.IsNullOrEmpty(keyword) ? "" : $"%{keyword}%");

        // Query đếm: Đếm playlist của user này
        var countSql = @"
            SELECT COUNT(1) FROM playlists 
            WHERE user_id = @UserId 
            AND (title LIKE @Keyword OR @Keyword = '')";
        var totalRecords = await _connection.ExecuteScalarAsync<int>(countSql, p);

        // Query lấy dữ liệu
        int offset = (pageIndex - 1) * pageSize;
        p.Add("Off", offset);
        p.Add("Lim", pageSize);

        var sql = @"
            SELECT p.playlist_id as PlaylistId, p.title, '' as Description, 
                   u.full_name as CreatedBy, p.created_at as CreatedAt, p.updated_at as UpdatedAt,
                   COUNT(ps.song_id) as SongCount
            FROM playlists p
            LEFT JOIN users u ON p.user_id = u.user_id
            LEFT JOIN playlist_songs ps ON p.playlist_id = ps.playlist_id
            WHERE p.user_id = @UserId 
            AND (p.title LIKE @Keyword OR @Keyword = '')
            GROUP BY p.playlist_id
            ORDER BY p.created_at DESC
            LIMIT @Lim OFFSET @Off";

        var data = await _connection.QueryAsync<PlaylistDto>(sql, p);

        return new PagingResult<PlaylistDto>
        {
            Data = data,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
            FromRecord = totalRecords == 0 ? 0 : offset + 1,
            ToRecord = totalRecords == 0 ? 0 : Math.Min(pageIndex * pageSize, totalRecords)
        };
    }

    public async Task<PagingResult<PlaylistDto>> GetAllPlaylistsAsync(string keyword, int pageIndex, int pageSize)
    {
        var p = new DynamicParameters();
        p.Add("Keyword", string.IsNullOrEmpty(keyword) ? "" : $"%{keyword}%");

        // Query đếm: Đếm tất cả playlist
        var countSql = @"
            SELECT COUNT(1) FROM playlists 
            WHERE (title LIKE @Keyword OR @Keyword = '')";
        var totalRecords = await _connection.ExecuteScalarAsync<int>(countSql, p);

        // Query lấy dữ liệu
        int offset = (pageIndex - 1) * pageSize;
        p.Add("Off", offset);
        p.Add("Lim", pageSize);

        var sql = @"
            SELECT p.playlist_id as PlaylistId, p.title, '' as Description, 
                   u.full_name as CreatedBy, p.created_at as CreatedAt, p.updated_at as UpdatedAt,
                   COUNT(ps.song_id) as SongCount
            FROM playlists p
            LEFT JOIN users u ON p.user_id = u.user_id
            LEFT JOIN playlist_songs ps ON p.playlist_id = ps.playlist_id
            WHERE (p.title LIKE @Keyword OR @Keyword = '')
            GROUP BY p.playlist_id
            ORDER BY p.created_at DESC
            LIMIT @Lim OFFSET @Off";

        var data = await _connection.QueryAsync<PlaylistDto>(sql, p);

        return new PagingResult<PlaylistDto>
        {
            Data = data,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
            FromRecord = totalRecords == 0 ? 0 : offset + 1,
            ToRecord = totalRecords == 0 ? 0 : Math.Min(pageIndex * pageSize, totalRecords)
        };
    }
}

