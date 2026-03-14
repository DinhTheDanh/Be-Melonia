using System;
using System.Data;
using Dapper;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Entities;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;

namespace MUSIC.STREAMING.WEBSITE.Infrastructure.Repositories;

public class SongRepository : BaseRepository<Song>, ISongRepository
{
    public SongRepository(IDbConnection connection) : base(connection)
    {
    }

    public async Task<IEnumerable<Song>> GetByAlbumIdAsync(Guid albumId)
    {
        var sql = "SELECT * FROM songs WHERE album_id = @AlbumId ORDER BY created_at DESC";
        return await _connection.QueryAsync<Song>(sql, new { AlbumId = albumId });
    }

    public async Task<IEnumerable<Song>> GetByArtistIdAsync(Guid artistId)
    {
        var sql = @"
            SELECT s.* FROM songs s
            JOIN song_artists sa ON s.song_id = sa.song_id
            WHERE sa.artist_id = @ArtistId
            ORDER BY s.created_at DESC";
        return await _connection.QueryAsync<Song>(sql, new { ArtistId = artistId });
    }

    public async Task<IEnumerable<Song>> GetByPlaylistIdAsync(Guid playlistId)
    {
        var sql = @"
            SELECT s.* FROM songs s
            JOIN playlist_songs ps ON s.song_id = ps.song_id
            WHERE ps.playlist_id = @PlaylistId
            ORDER BY ps.added_at DESC";
        return await _connection.QueryAsync<Song>(sql, new { PlaylistId = playlistId });
    }

    public async Task AddArtistsToSongAsync(Guid songId, List<Guid> artistIds)
    {
        var sql = "INSERT INTO song_artists (song_id, artist_id) VALUES (@SongId, @ArtistId)";

        // Biến đổi danh sách ID thành danh sách object ẩn danh để Dapper hiểu
        var parameters = artistIds.Select(artistId => new
        {
            SongId = songId,
            ArtistId = artistId
        });

        await _connection.ExecuteAsync(sql, parameters);
    }
    public async Task AddGenresToSongAsync(Guid songId, List<Guid> genreIds)
    {
        var sql = "INSERT INTO song_genres (song_id, genre_id) VALUES (@SongId, @GenreId)";
        var parameters = genreIds.Select(genreId => new
        {
            SongId = songId,
            GenreId = genreId
        });
        await _connection.ExecuteAsync(sql, parameters);
    }

    public async Task<PagingResult<SongDto>> GetAllSongsWithArtistAsync(string keyword, int pageIndex, int pageSize, Guid? genreId = null)
    {
        var p = new DynamicParameters();
        string whereSql = "WHERE s.is_public = 1";
        string joinSql = "";

        if (!string.IsNullOrEmpty(keyword))
        {
            whereSql += " AND s.title LIKE @Key";
            p.Add("Key", $"%{keyword}%");
        }

        if (genreId.HasValue)
        {
            joinSql += " JOIN song_genres sg ON s.song_id = sg.song_id";
            whereSql += " AND sg.genre_id = @GenreId";
            p.Add("GenreId", genreId.Value);
        }

        // Đếm tổng
        var countSql = $"SELECT COUNT(1) FROM songs s {joinSql} {whereSql}";
        var totalRecords = await _connection.ExecuteScalarAsync<int>(countSql, p);

        // Lấy dữ liệu 
        int offset = (pageIndex - 1) * pageSize;
        p.Add("Off", offset);
        p.Add("Lim", pageSize);

        var sql = $@"
            SELECT s.song_id as Id, s.title, s.thumbnail, s.file_url as FileUrl, s.duration,
                   s.created_at as CreatedAt, s.updated_at as UpdatedAt,
                   s.album_id as AlbumId, al.title as AlbumTitle,
                   GROUP_CONCAT(DISTINCT u.full_name SEPARATOR ', ') as ArtistNames,
                   (SELECT COUNT(*) FROM user_likes ul WHERE ul.song_id = s.song_id) as LikeCount,
                   (SELECT COALESCE(SUM(uss.play_count), 0) FROM user_song_stats uss WHERE uss.song_id = s.song_id) as ListenCount
            FROM songs s
            {joinSql}
            LEFT JOIN song_artists sa ON s.song_id = sa.song_id
            LEFT JOIN users u ON sa.artist_id = u.user_id
            LEFT JOIN albums al ON s.album_id = al.album_id
            {whereSql}
            GROUP BY s.song_id, s.title, s.thumbnail, s.file_url, s.duration, s.created_at, s.updated_at, s.album_id, al.title
            ORDER BY s.created_at DESC
            LIMIT @Lim OFFSET @Off";

        var data = await _connection.QueryAsync<SongDto>(sql, p);

        return new PagingResult<SongDto>
        {
            Data = data,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
            FromRecord = totalRecords == 0 ? 0 : offset + 1,
            ToRecord = totalRecords == 0 ? 0 : Math.Min(pageIndex * pageSize, totalRecords)
        };
    }

    public async Task<PagingResult<SongDto>> GetSongsByArtistIdAsync(Guid artistId, int pageIndex, int pageSize)
    {
        var p = new DynamicParameters();
        p.Add("ArtistId", artistId);

        // Query đếm: Chỉ đếm những bài mà artistId này có tham gia
        var countSql = @"
            SELECT COUNT(1) FROM songs s 
            JOIN song_artists sa_check ON s.song_id = sa_check.song_id
            WHERE sa_check.artist_id = @ArtistId AND s.is_public = 1";

        var totalRecords = await _connection.ExecuteScalarAsync<int>(countSql, p);

        // Query lấy dữ liệu
        int offset = (pageIndex - 1) * pageSize;
        p.Add("Off", offset);
        p.Add("Lim", pageSize);

        var sql = $@"
            SELECT s.song_id as Id, s.title, s.thumbnail, s.file_url as FileUrl, s.duration,
                   s.created_at as CreatedAt, s.updated_at as UpdatedAt,
                   s.album_id as AlbumId, al.title as AlbumTitle,
                   GROUP_CONCAT(DISTINCT u.full_name SEPARATOR ', ') as ArtistNames,
                   (SELECT COUNT(*) FROM user_likes ul WHERE ul.song_id = s.song_id) as LikeCount,
                   (SELECT COALESCE(SUM(uss.play_count), 0) FROM user_song_stats uss WHERE uss.song_id = s.song_id) as ListenCount
            FROM songs s
            JOIN song_artists sa_check ON s.song_id = sa_check.song_id
            LEFT JOIN song_artists sa_all ON s.song_id = sa_all.song_id
            LEFT JOIN users u ON sa_all.artist_id = u.user_id
            LEFT JOIN albums al ON s.album_id = al.album_id
            WHERE sa_check.artist_id = @ArtistId AND s.is_public = 1
            GROUP BY s.song_id, s.title, s.thumbnail, s.file_url, s.duration, s.created_at, s.updated_at, s.album_id, al.title
            ORDER BY s.created_at DESC
            LIMIT @Lim OFFSET @Off";

        var data = await _connection.QueryAsync<SongDto>(sql, p);

        return new PagingResult<SongDto>
        {
            Data = data,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
            FromRecord = totalRecords == 0 ? 0 : offset + 1,
            ToRecord = totalRecords == 0 ? 0 : Math.Min(pageIndex * pageSize, totalRecords)
        };
    }

    public async Task<Song?> GetByFileHashAsync(string hash)
    {
        var sql = "SELECT * FROM songs WHERE file_hash = @Hash LIMIT 1";

        return await _connection.QueryFirstOrDefaultAsync<Song>(sql, new { Hash = hash });
    }

    public async Task<PagingResult<SongDto>> GetUserSongsAsync(Guid userId, string keyword, int pageIndex, int pageSize)
    {
        var p = new DynamicParameters();
        p.Add("UserId", userId);
        p.Add("Keyword", string.IsNullOrEmpty(keyword) ? "" : $"%{keyword}%");

        // Query đếm: Đếm bài hát của user này
        var countSql = @"
            SELECT COUNT(1) FROM songs s 
            JOIN song_artists sa ON s.song_id = sa.song_id
            WHERE sa.artist_id = @UserId 
            AND (s.title LIKE @Keyword OR @Keyword = '')";

        var totalRecords = await _connection.ExecuteScalarAsync<int>(countSql, p);

        // Query lấy dữ liệu
        int offset = (pageIndex - 1) * pageSize;
        p.Add("Off", offset);
        p.Add("Lim", pageSize);

        var sql = $@"
            SELECT s.song_id as Id, s.title, s.thumbnail, s.file_url as FileUrl, s.duration, s.created_at, s.updated_at,
                   s.album_id as AlbumId, al.title as AlbumTitle,
                   GROUP_CONCAT(DISTINCT u.full_name SEPARATOR ', ') as ArtistNames,
                   (SELECT COUNT(*) FROM user_likes ul WHERE ul.song_id = s.song_id) as LikeCount,
                   (SELECT COALESCE(SUM(uss.play_count), 0) FROM user_song_stats uss WHERE uss.song_id = s.song_id) as ListenCount
            FROM songs s
            JOIN song_artists sa ON s.song_id = sa.song_id
            LEFT JOIN users u ON sa.artist_id = u.user_id
            LEFT JOIN albums al ON s.album_id = al.album_id
            WHERE sa.artist_id = @UserId 
            AND (s.title LIKE @Keyword OR @Keyword = '')
            GROUP BY s.song_id, s.title, s.thumbnail, s.file_url, s.duration, s.created_at, s.updated_at, s.album_id, al.title
            ORDER BY s.created_at DESC
            LIMIT @Lim OFFSET @Off";

        var data = await _connection.QueryAsync<SongDto>(sql, p);

        return new PagingResult<SongDto>
        {
            Data = data,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
            FromRecord = totalRecords == 0 ? 0 : offset + 1,
            ToRecord = totalRecords == 0 ? 0 : Math.Min(pageIndex * pageSize, totalRecords)
        };
    }

    public async Task<bool> CheckSongOwnerAsync(Guid artistId, Guid songId)
    {
        var sql = @"
            SELECT COUNT(1) FROM songs s
            JOIN song_artists sa ON s.song_id = sa.song_id
            WHERE s.song_id = @SongId AND sa.artist_id = @ArtistId";

        var count = await _connection.ExecuteScalarAsync<int>(sql, new { SongId = songId, ArtistId = artistId });
        return count > 0;
    }

    public async Task RemoveGenresFromSongAsync(Guid songId)
    {
        var sql = "DELETE FROM song_genres WHERE song_id = @SongId";
        await _connection.ExecuteAsync(sql, new { SongId = songId });
    }

    public async Task<List<SongDto>> GetSongsByIdsAsync(List<Guid> songIds)
    {
        if (songIds == null || !songIds.Any()) return new List<SongDto>();

        var sql = @"
            SELECT s.song_id as Id, s.title, s.thumbnail, s.file_url as FileUrl, s.duration,
                   s.created_at as CreatedAt, s.updated_at as UpdatedAt,
                   s.album_id as AlbumId, al.title as AlbumTitle,
                   GROUP_CONCAT(DISTINCT u.full_name SEPARATOR ', ') as ArtistNames,
                   (SELECT COUNT(*) FROM user_likes ul WHERE ul.song_id = s.song_id) as LikeCount,
                   (SELECT COALESCE(SUM(uss.play_count), 0) FROM user_song_stats uss WHERE uss.song_id = s.song_id) as ListenCount
            FROM songs s
            LEFT JOIN song_artists sa ON s.song_id = sa.song_id
            LEFT JOIN users u ON sa.artist_id = u.user_id
            LEFT JOIN albums al ON s.album_id = al.album_id
            WHERE s.song_id IN @SongIds AND s.is_public = 1
            GROUP BY s.song_id, s.title, s.thumbnail, s.file_url, s.duration, s.created_at, s.updated_at, s.album_id, al.title
            ORDER BY s.created_at DESC";

        var data = await _connection.QueryAsync<SongDto>(sql, new { SongIds = songIds });
        return data.AsList();
    }
}

