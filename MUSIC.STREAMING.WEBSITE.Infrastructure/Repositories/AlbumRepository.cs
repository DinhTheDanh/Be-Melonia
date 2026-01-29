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

    public async Task<PagingResult<AlbumDto>> GetAlbumsWithArtistAsync(int pageIndex, int pageSize)
    {
        var countSql = "SELECT COUNT(1) FROM albums";
        var totalRecords = await _connection.ExecuteScalarAsync<int>(countSql);

        int offset = (pageIndex - 1) * pageSize;

        // Join với Users để lấy tên Artist
        var sql = @"
            SELECT a.album_id, a.title, a.thumbnail, a.release_date, u.full_name as ArtistName
            FROM albums a
            LEFT JOIN users u ON a.artist_id = u.user_id
            ORDER BY a.created_at DESC
            LIMIT @Lim OFFSET @Off";

        var data = await _connection.QueryAsync<AlbumDto>(sql, new { Lim = pageSize, Off = offset });

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
