using System;
using System.Data;
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
                   a.created_at as CreatedAt, a.updated_at as UpdatedAt, u.full_name as ArtistName
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
                   a.created_at as CreatedAt, a.updated_at as UpdatedAt, u.full_name as ArtistName
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

    public async Task<dynamic> GetAlbumDetailsAsync(Guid albumId, int pageIndex, int pageSize)
    {
        // Lấy thông tin album
        var albumSql = @"
            SELECT a.album_id as AlbumId, a.title, a.thumbnail, a.release_date as ReleaseDate,
                   a.created_at as CreatedAt, a.updated_at as UpdatedAt, u.full_name as ArtistName
            FROM albums a
            LEFT JOIN users u ON a.artist_id = u.user_id
            WHERE a.album_id = @AlbumId";
        var album = await _connection.QueryFirstOrDefaultAsync<AlbumDto>(albumSql, new { AlbumId = albumId });

        if (album == null) return null!;

        // Đếm tổng số bài hát trong album
        var countSql = "SELECT COUNT(1) FROM songs WHERE album_id = @AlbumId";
        var totalRecords = await _connection.ExecuteScalarAsync<int>(countSql, new { AlbumId = albumId });

        // Lấy danh sách bài hát
        int offset = (pageIndex - 1) * pageSize;
        var songsSql = @"
            SELECT s.song_id as Id, s.title, s.thumbnail, s.file_url as FileUrl, s.duration,
                   s.created_at as CreatedAt, s.updated_at as UpdatedAt,
                   GROUP_CONCAT(u.full_name SEPARATOR ', ') as ArtistNames
            FROM songs s
            LEFT JOIN song_artists sa ON s.song_id = sa.song_id
            LEFT JOIN users u ON sa.artist_id = u.user_id
            WHERE s.album_id = @AlbumId
            GROUP BY s.song_id
            ORDER BY s.created_at DESC
            LIMIT @Lim OFFSET @Off";

        var songs = await _connection.QueryAsync<SongDto>(songsSql, new { AlbumId = albumId, Off = offset, Lim = pageSize });

        return new
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
}

