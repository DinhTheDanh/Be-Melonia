using System;
using System.Data;
using Dapper;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Entities;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;

namespace MUSIC.STREAMING.WEBSITE.Infrastructure.Repositories;

public class InteractionRepository : IInteractionRepository
{
    private readonly IDbConnection _connection;
    public InteractionRepository(IDbConnection connection) { _connection = connection; }

    public async Task<bool> ToggleLikeAsync(Guid userId, Guid songId)
    {
        var count = await _connection.ExecuteScalarAsync<int>(
            "SELECT count(*) FROM user_likes WHERE user_id = @U AND song_id = @S",
            new { U = userId, S = songId });

        if (count > 0)
        {
            await _connection.ExecuteAsync("DELETE FROM user_likes WHERE user_id = @U AND song_id = @S", new { U = userId, S = songId });
            return false; // Unlike
        }
        else
        {
            await _connection.ExecuteAsync("INSERT INTO user_likes (user_id, song_id, liked_at) VALUES (@U, @S, NOW())", new { U = userId, S = songId });
            return true; // Like
        }
    }

    public async Task<PagingResult<SongDto>> GetLikedSongsAsync(Guid userId, int pageIndex, int pageSize)
    {
        var parameters = new DynamicParameters();
        parameters.Add("UserId", userId);

        // 1. Đếm tổng số bài đã like
        var countSql = "SELECT COUNT(1) FROM user_likes WHERE user_id = @UserId";
        var totalRecords = await _connection.ExecuteScalarAsync<int>(countSql, parameters);

        // 2. Tính toán Offset
        int offset = (pageIndex - 1) * pageSize;
        parameters.Add("Offset", offset);
        parameters.Add("Limit", pageSize);

        // 3. Query dữ liệu có LIMIT OFFSET
        var dataSql = @"
        SELECT s.song_id as Id, s.title, s.thumbnail, s.file_url as FileUrl, s.duration,
               s.created_at as CreatedAt, s.updated_at as UpdatedAt,
               s.is_public as IsPublic, s.lyrics, s.file_hash as FileHash,
               s.album_id as AlbumId, al.title as AlbumTitle,
               GROUP_CONCAT(DISTINCT u.full_name SEPARATOR ', ') as ArtistNames,
               (SELECT COUNT(*) FROM user_likes ul2 WHERE ul2.song_id = s.song_id) as LikeCount,
               (SELECT COALESCE(SUM(uss.play_count), 0) FROM user_song_stats uss WHERE uss.song_id = s.song_id) as ListenCount
        FROM songs s 
        JOIN user_likes ul ON s.song_id = ul.song_id
        LEFT JOIN song_artists sa ON s.song_id = sa.song_id
        LEFT JOIN users u ON sa.artist_id = u.user_id
        LEFT JOIN albums al ON s.album_id = al.album_id
        WHERE ul.user_id = @UserId 
        GROUP BY s.song_id, s.title, s.thumbnail, s.file_url, s.duration, s.created_at, s.updated_at,
                 s.is_public, s.lyrics, s.file_hash, s.album_id, al.title, ul.liked_at
        ORDER BY ul.liked_at DESC
        LIMIT @Limit OFFSET @Offset";

        var items = await _connection.QueryAsync<SongDto>(dataSql, parameters);

        // 4. Tính toán trả về
        int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
        int fromRecord = totalRecords == 0 ? 0 : offset + 1;
        int toRecord = totalRecords == 0 ? 0 : Math.Min(pageIndex * pageSize, totalRecords);

        return new PagingResult<SongDto>
        {
            Data = items,
            TotalRecords = totalRecords,
            TotalPages = totalPages,
            FromRecord = fromRecord,
            ToRecord = toRecord
        };
    }

    public async Task AddSongToPlaylistAsync(Guid playlistId, Guid songId)
    {
        // Check tồn tại trước để tránh trùng
        var exists = await _connection.ExecuteScalarAsync<int>(
            "SELECT count(*) FROM playlist_songs WHERE playlist_id = @P AND song_id = @S",
            new { P = playlistId, S = songId });

        if (exists == 0)
        {
            await _connection.ExecuteAsync(
                "INSERT INTO playlist_songs (playlist_id, song_id, added_at) VALUES (@P, @S, NOW())",
                new { P = playlistId, S = songId });
        }
    }

    public async Task RemoveSongFromPlaylistAsync(Guid playlistId, Guid songId)
    {
        await _connection.ExecuteAsync(
            "DELETE FROM playlist_songs WHERE playlist_id = @P AND song_id = @S",
            new { P = playlistId, S = songId });
    }

    public async Task<bool> ToggleFollowAsync(Guid followerId, Guid followingId)
    {
        var countSql = "SELECT count(*) FROM user_follows WHERE follower_id = @F AND following_id = @T";
        var count = await _connection.ExecuteScalarAsync<int>(countSql, new { F = followerId, T = followingId });

        if (count > 0)
        {
            // Đã có -> Xóa (Unfollow)
            await _connection.ExecuteAsync("DELETE FROM user_follows WHERE follower_id = @F AND following_id = @T", new { F = followerId, T = followingId });
            return false;
        }
        else
        {
            // Chưa có -> Thêm (Follow)
            await _connection.ExecuteAsync("INSERT INTO user_follows (follower_id, following_id, followed_at) VALUES (@F, @T, NOW())", new { F = followerId, T = followingId });
            return true;
        }
    }

    public async Task<PagingResult<User>> GetFollowingsAsync(Guid followerId, int pageIndex, int pageSize)
    {
        var p = new DynamicParameters();
        p.Add("UserId", followerId);

        // Đếm số lượng người đang theo dõi
        var countSql = "SELECT COUNT(1) FROM user_follows WHERE follower_id = @UserId";
        var totalRecords = await _connection.ExecuteScalarAsync<int>(countSql, p);

        // Lấy danh sách User 
        int offset = (pageIndex - 1) * pageSize;
        p.Add("Off", offset);
        p.Add("Lim", pageSize);

        var dataSql = @"
            SELECT u.* FROM users u
            JOIN user_follows uf ON u.user_id = uf.following_id
            WHERE uf.follower_id = @UserId
            ORDER BY uf.followed_at DESC
            LIMIT @Lim OFFSET @Off";

        var users = await _connection.QueryAsync<User>(dataSql, p);

        // Trả về PagingResult 
        return new PagingResult<User>
        {
            Data = users,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
            FromRecord = totalRecords == 0 ? 0 : offset + 1,
            ToRecord = totalRecords == 0 ? 0 : Math.Min(pageIndex * pageSize, totalRecords)
        };
    }

    public async Task<PagingResult<User>> GetFollowersAsync(Guid followingId, int pageIndex, int pageSize)
    {
        var p = new DynamicParameters();
        p.Add("UserId", followingId);

        // Đếm số lượng người theo dõi
        var countSql = "SELECT COUNT(1) FROM user_follows WHERE following_id = @UserId";
        var totalRecords = await _connection.ExecuteScalarAsync<int>(countSql, p);

        // Lấy danh sách User 
        int offset = (pageIndex - 1) * pageSize;
        p.Add("Off", offset);
        p.Add("Lim", pageSize);

        var dataSql = @"
            SELECT u.* FROM users u
            JOIN user_follows uf ON u.user_id = uf.follower_id
            WHERE uf.following_id = @UserId
            ORDER BY uf.followed_at DESC
            LIMIT @Lim OFFSET @Off";

        var users = await _connection.QueryAsync<User>(dataSql, p);

        // Trả về PagingResult 
        return new PagingResult<User>
        {
            Data = users,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
            FromRecord = totalRecords == 0 ? 0 : offset + 1,
            ToRecord = totalRecords == 0 ? 0 : Math.Min(pageIndex * pageSize, totalRecords)
        };
    }

    public async Task<PlaylistDetailsDto?> GetPlaylistDetailsAsync(Guid playlistId, int pageIndex, int pageSize)
    {
        var p = new DynamicParameters();
        p.Add("PlaylistId", playlistId);

        // Lấy thông tin playlist
        var playlistSql = @"
            SELECT p.playlist_id as PlaylistId, p.title, p.thumbnail, p.description,
                   p.created_at as CreatedAt,
                   u.full_name as CreatedBy, u.user_id as CreatedById
            FROM playlists p
            LEFT JOIN users u ON p.user_id = u.user_id
            WHERE p.playlist_id = @PlaylistId";

        var playlist = await _connection.QueryFirstOrDefaultAsync<PlaylistInfoDto>(playlistSql, p);
        if (playlist == null) return null;

        // Đếm bài hát trong playlist
        var countSql = "SELECT COUNT(1) FROM playlist_songs WHERE playlist_id = @PlaylistId";
        var totalSongs = await _connection.ExecuteScalarAsync<int>(countSql, p);

        // Lấy danh sách bài hát
        int offset = (pageIndex - 1) * pageSize;
        p.Add("Offset", offset);
        p.Add("Limit", pageSize);

        var songsSql = @"
            SELECT s.song_id as Id, s.title, s.thumbnail, s.file_url as FileUrl, s.duration,
                   s.album_id as AlbumId, al.title as AlbumTitle,
                   GROUP_CONCAT(DISTINCT u.full_name SEPARATOR ', ') as ArtistNames,
                   (SELECT COUNT(*) FROM user_likes ul WHERE ul.song_id = s.song_id) as LikeCount,
                   (SELECT COALESCE(SUM(uss.play_count), 0) FROM user_song_stats uss WHERE uss.song_id = s.song_id) as ListenCount,
                   MAX(ps.added_at) as AddedAt
            FROM songs s
            LEFT JOIN song_artists sa ON s.song_id = sa.song_id
            LEFT JOIN users u ON sa.artist_id = u.user_id
            LEFT JOIN albums al ON s.album_id = al.album_id
            LEFT JOIN playlist_songs ps ON s.song_id = ps.song_id AND ps.playlist_id = @PlaylistId
            WHERE ps.playlist_id = @PlaylistId
            GROUP BY s.song_id, s.title, s.thumbnail, s.file_url, s.duration, s.album_id, al.title
            ORDER BY AddedAt DESC
            LIMIT @Limit OFFSET @Offset";

        var songs = await _connection.QueryAsync<SongDto>(songsSql, p);

        return new PlaylistDetailsDto
        {
            Playlist = playlist,
            Songs = new PagingResult<SongDto>
            {
                Data = songs,
                TotalRecords = totalSongs,
                TotalPages = (int)Math.Ceiling((double)totalSongs / pageSize),
                FromRecord = totalSongs == 0 ? 0 : offset + 1,
                ToRecord = totalSongs == 0 ? 0 : Math.Min(pageIndex * pageSize, totalSongs)
            }
        };
    }

    public async Task RemoveSongFromAlbumAsync(Guid userId, Guid albumId, Guid songId)
    {
        // Kiểm tra album tồn tại và user là chủ sở hữu
        var albumSql = "SELECT artist_id FROM albums WHERE album_id = @AlbumId";
        var artistId = await _connection.ExecuteScalarAsync<Guid?>(albumSql, new { AlbumId = albumId });

        if (!artistId.HasValue) throw new Exception("Album không tồn tại");
        if (artistId.Value != userId) throw new UnauthorizedAccessException("Bạn không có quyền xóa bài hát khỏi album này");

        // Xóa bài hát khỏi album (cập nhật album_id thành null)
        var updateSql = "UPDATE songs SET album_id = NULL WHERE song_id = @SongId AND album_id = @AlbumId";
        var result = await _connection.ExecuteAsync(updateSql, new { SongId = songId, AlbumId = albumId });

        if (result == 0) throw new Exception("Bài hát không thuộc album này");
    }

    public async Task RecordPlayAsync(Guid userId, Guid songId, int durationListened, bool completed, string? source)
    {
        // 1. Upsert user_song_stats: tăng play_count, cộng dồn total_listen_time, cập nhật last_played
        var upsertSql = @"
            INSERT INTO user_song_stats (user_id, song_id, play_count, total_listen_time, last_played, skip_count)
            VALUES (@UserId, @SongId, 1, @Duration, NOW(), @SkipIncrement)
            ON DUPLICATE KEY UPDATE
                play_count = play_count + 1,
                total_listen_time = total_listen_time + @Duration,
                last_played = NOW(),
                skip_count = skip_count + @SkipIncrement";

        int skipIncrement = completed ? 0 : 1;

        await _connection.ExecuteAsync(upsertSql, new
        {
            UserId = userId,
            SongId = songId,
            Duration = durationListened,
            SkipIncrement = skipIncrement
        });

        // 2. Insert listening_history
        var historySql = @"
            INSERT INTO listening_history (id, user_id, song_id, listened_at, duration_listened, completed, source)
            VALUES (@Id, @UserId, @SongId, NOW(), @Duration, @Completed, @Source)";

        await _connection.ExecuteAsync(historySql, new
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SongId = songId,
            Duration = durationListened,
            Completed = completed,
            Source = source
        });
    }
}


