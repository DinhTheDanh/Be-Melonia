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
            SELECT a.album_id as AlbumId, a.title, a.thumbnail, a.release_date as ReleaseDate, u.full_name as ArtistName
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
            SELECT a.album_id as AlbumId, a.title, a.thumbnail, a.release_date as ReleaseDate, u.full_name as ArtistName
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
}

