using System;
using System.Data;
using System.Text.Json;
using Dapper;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Entities;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;

namespace MUSIC.STREAMING.WEBSITE.Infrastructure.Repositories;

public class AlbumRepository : BaseRepository<Album>, IAlbumRepository
{
    public AlbumRepository(IDbConnection connection) : base(connection)
    {
    }

    public async Task<PagingResult<AlbumDto>> GetAlbumsWithArtistAsync(string keyword, int pageIndex, int pageSize)
    {
        var p = new DynamicParameters();
        p.Add("Keyword", string.IsNullOrEmpty(keyword) ? "" : $"%{keyword}%");

        var countSql = @"
            SELECT COUNT(1) FROM albums 
            WHERE title LIKE @Keyword OR @Keyword = ''";
        var totalRecords = await _connection.ExecuteScalarAsync<int>(countSql, p);

        int offset = (pageIndex - 1) * pageSize;
        p.Add("Off", offset);
        p.Add("Lim", pageSize);

        // Join với Users để lấy tên Artist
        var sql = @"
            SELECT a.album_id as AlbumId, a.title, a.thumbnail, a.release_date as ReleaseDate, 
                   a.created_at as CreatedAt, a.updated_at as UpdatedAt,
                   a.artist_id as ArtistId, u.full_name as ArtistName
            FROM albums a
            LEFT JOIN users u ON a.artist_id = u.user_id
            WHERE a.title LIKE @Keyword OR @Keyword = ''
            ORDER BY a.created_at DESC
            LIMIT @Lim OFFSET @Off";

        var data = await _connection.QueryAsync<AlbumDto>(sql, p);

        return new PagingResult<AlbumDto>
        {
            Data = data,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
            FromRecord = totalRecords == 0 ? 0 : offset + 1,
            ToRecord = totalRecords == 0 ? 0 : Math.Min(pageIndex * pageSize, totalRecords)
        };
    }

    public async Task<PagingResult<AlbumDto>> GetUserAlbumsAsync(Guid userId, string keyword, int pageIndex, int pageSize)
    {
        var p = new DynamicParameters();
        p.Add("UserId", userId);
        p.Add("Keyword", string.IsNullOrEmpty(keyword) ? "" : $"%{keyword}%");

        // Query đếm: Đếm album của user này
        var countSql = @"
            SELECT COUNT(1) FROM albums 
            WHERE artist_id = @UserId 
            AND (title LIKE @Keyword OR @Keyword = '')";
        var totalRecords = await _connection.ExecuteScalarAsync<int>(countSql, p);

        // Query lấy dữ liệu
        int offset = (pageIndex - 1) * pageSize;
        p.Add("Off", offset);
        p.Add("Lim", pageSize);

        var sql = @"
            SELECT a.album_id as AlbumId, a.title, a.thumbnail, a.release_date as ReleaseDate, 
                   a.created_at as CreatedAt, a.updated_at as UpdatedAt,
                   a.artist_id as ArtistId, u.full_name as ArtistName
            FROM albums a
            LEFT JOIN users u ON a.artist_id = u.user_id
            WHERE a.artist_id = @UserId 
            AND (a.title LIKE @Keyword OR @Keyword = '')
            ORDER BY a.created_at DESC
            LIMIT @Lim OFFSET @Off";

        var data = await _connection.QueryAsync<AlbumDto>(sql, p);

        return new PagingResult<AlbumDto>
        {
            Data = data,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
            FromRecord = totalRecords == 0 ? 0 : offset + 1,
            ToRecord = totalRecords == 0 ? 0 : Math.Min(pageIndex * pageSize, totalRecords)
        };
    }

    public async Task<AlbumDetailsDto?> GetAlbumDetailsAsync(Guid albumId, int pageIndex, int pageSize)
    {
        // Lấy thông tin album
        var albumSql = @"
            SELECT a.album_id as AlbumId, a.title, a.thumbnail, a.release_date as ReleaseDate,
                   a.created_at as CreatedAt, a.updated_at as UpdatedAt,
                   a.artist_id as ArtistId, u.full_name as ArtistName
            FROM albums a
            LEFT JOIN users u ON a.artist_id = u.user_id
            WHERE a.album_id = @AlbumId";
        var album = await _connection.QueryFirstOrDefaultAsync<AlbumDto>(albumSql, new { AlbumId = albumId });

        if (album == null) return null;

        // Đếm tổng số bài hát trong album
        var countSql = "SELECT COUNT(1) FROM songs WHERE album_id = @AlbumId";
        var totalRecords = await _connection.ExecuteScalarAsync<int>(countSql, new { AlbumId = albumId });

        // Lấy danh sách bài hát
        int offset = (pageIndex - 1) * pageSize;
        var songsSql = @"
            SELECT s.song_id as Id, s.title, s.thumbnail, s.file_url as FileUrl, s.duration,
                   s.created_at as CreatedAt, s.updated_at as UpdatedAt,
                   s.album_id as AlbumId, al.title as AlbumTitle,
                   GROUP_CONCAT(u.full_name SEPARATOR ', ') as ArtistNames
            FROM songs s
            LEFT JOIN song_artists sa ON s.song_id = sa.song_id
            LEFT JOIN users u ON sa.artist_id = u.user_id
            LEFT JOIN albums al ON s.album_id = al.album_id
            WHERE s.album_id = @AlbumId
            GROUP BY s.song_id, s.title, s.thumbnail, s.file_url, s.duration, s.created_at, s.updated_at, s.album_id, al.title
            ORDER BY s.created_at DESC
            LIMIT @Lim OFFSET @Off";

        var songs = await _connection.QueryAsync<SongDto>(songsSql, new { AlbumId = albumId, Off = offset, Lim = pageSize });

        return new AlbumDetailsDto
        {
            Album = album,
            Songs = new PagingResult<SongDto>
            {
                Data = songs,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
                FromRecord = totalRecords == 0 ? 0 : offset + 1,
                ToRecord = totalRecords == 0 ? 0 : Math.Min(pageIndex * pageSize, totalRecords)
            }
        };
    }

    public async Task AddSongToAlbumAsync(Guid albumId, Guid songId)
    {
        var sql = "UPDATE songs SET album_id = @AlbumId, updated_at = @UpdatedAt WHERE song_id = @SongId";
        await _connection.ExecuteAsync(sql, new { AlbumId = albumId, SongId = songId, UpdatedAt = DateTime.Now });
    }

    public async Task<bool> CheckAlbumOwnerAsync(Guid userId, Guid albumId)
    {
        var sql = "SELECT COUNT(1) FROM albums WHERE album_id = @AlbumId AND artist_id = @UserId";
        var count = await _connection.ExecuteScalarAsync<int>(sql, new { AlbumId = albumId, UserId = userId });
        return count > 0;
    }

    public async Task<List<AlbumDto>> GetAlbumsByIdsAsync(List<Guid> albumIds)
    {
        if (albumIds == null || !albumIds.Any()) return new List<AlbumDto>();

        var sql = @"
            SELECT a.album_id as AlbumId, a.title, a.thumbnail, a.release_date as ReleaseDate,
                   a.created_at as CreatedAt, a.updated_at as UpdatedAt,
                   a.artist_id as ArtistId, u.full_name as ArtistName
            FROM albums a
            LEFT JOIN users u ON a.artist_id = u.user_id
            WHERE a.album_id IN @AlbumIds
            ORDER BY a.created_at DESC";

        var data = await _connection.QueryAsync<AlbumDto>(sql, new { AlbumIds = albumIds });
        return data.AsList();
    }

    public async Task<PagingResult<PopularAlbumDto>> GetPopularAlbumsAsync(string windowType, string keyword, int pageIndex, int pageSize)
    {
        var normalizedWindow = string.IsNullOrWhiteSpace(windowType) ? "7d" : windowType.Trim().ToLower();
        var p = new DynamicParameters();
        p.Add("WindowType", normalizedWindow);
        p.Add("Keyword", string.IsNullOrEmpty(keyword) ? "" : $"%{keyword}%");

        var countSql = @"
            SELECT COUNT(1)
            FROM album_popularity_scores aps
            JOIN albums a ON aps.album_id = a.album_id
            WHERE aps.window_type = @WindowType
              AND (a.title LIKE @Keyword OR @Keyword = '')";

        var totalRecords = await _connection.ExecuteScalarAsync<int>(countSql, p);

        int offset = (pageIndex - 1) * pageSize;
        p.Add("Off", offset);
        p.Add("Lim", pageSize);

        var sql = @"
            SELECT a.album_id as AlbumId,
                   a.title,
                   a.thumbnail,
                   a.release_date as ReleaseDate,
                   a.created_at as CreatedAt,
                   a.updated_at as UpdatedAt,
                   a.artist_id as ArtistId,
                   u.full_name as ArtistName,
                   aps.score as Score,
                   aps.rank_position as `Rank`,
                   aps.streams as Streams,
                   aps.unique_listeners as UniqueListeners,
                   aps.save_count as SaveCount
            FROM album_popularity_scores aps
            JOIN albums a ON aps.album_id = a.album_id
            LEFT JOIN users u ON a.artist_id = u.user_id
            WHERE aps.window_type = @WindowType
              AND (a.title LIKE @Keyword OR @Keyword = '')
            ORDER BY aps.rank_position ASC, aps.score DESC
            LIMIT @Lim OFFSET @Off";

        var data = await _connection.QueryAsync<PopularAlbumDto>(sql, p);

        return new PagingResult<PopularAlbumDto>
        {
            Data = data,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
            FromRecord = totalRecords == 0 ? 0 : offset + 1,
            ToRecord = totalRecords == 0 ? 0 : Math.Min(pageIndex * pageSize, totalRecords)
        };
    }
}

