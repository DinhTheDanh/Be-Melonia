using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MUSIC.STREAMING.WEBSITE.Core.Entities;
using Dapper;
using System.Data;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;

namespace MUSIC.STREAMING.WEBSITE.Infrastructure.Repositories
{
    public class RecommendationRepository : IRecommendationRepository
    {
        private readonly IDbConnection _db;
        private const string PublicPlayableSongFilter = @"
            s.song_id <> @SongId
            AND s.is_public = 1
            AND s.file_url IS NOT NULL
            AND s.file_url <> ''
            AND (s.release_status = 'published' OR s.release_status IS NULL)";

        public RecommendationRepository(IDbConnection db)
        {
            _db = db;
        }

        public async Task<List<ListeningHistory>> GetListeningHistoryAsync(Guid userId)
        {
            var sql = "SELECT * FROM listening_history WHERE user_id = @UserId ORDER BY listened_at DESC";
            var result = await _db.QueryAsync<ListeningHistory>(sql, new { UserId = userId });
            return result.AsList();
        }

        public async Task<List<UserSongStats>> GetUserSongStatsAsync(Guid userId)
        {
            var sql = "SELECT * FROM user_song_stats WHERE user_id = @UserId ORDER BY play_count DESC";
            var result = await _db.QueryAsync<UserSongStats>(sql, new { UserId = userId });
            return result.AsList();
        }

        public async Task<List<Guid>> GetTopSongIdsAsync(Guid userId, int topN)
        {
            var sql = "SELECT song_id FROM user_song_stats WHERE user_id = @UserId ORDER BY play_count DESC LIMIT @TopN";
            var result = await _db.QueryAsync<Guid>(sql, new { UserId = userId, TopN = topN });
            return result.AsList();
        }

        public async Task<List<Guid>> GetTopArtistIdsAsync(Guid userId, int topN)
        {
            // Join qua bảng song_artists (many-to-many)
            var sql = @"
                SELECT sa.artist_id
                FROM user_song_stats uss
                JOIN song_artists sa ON uss.song_id = sa.song_id
                WHERE uss.user_id = @UserId
                GROUP BY sa.artist_id
                ORDER BY SUM(uss.play_count) DESC
                LIMIT @TopN";
            var result = await _db.QueryAsync<Guid>(sql, new { UserId = userId, TopN = topN });
            return result.AsList();
        }

        public async Task<List<Guid>> GetTopGenreIdsAsync(Guid userId, int topN)
        {
            // Join qua bảng song_genres (many-to-many)
            var sql = @"
                SELECT sg.genre_id as GenreId
                FROM user_song_stats uss
                JOIN song_genres sg ON uss.song_id = sg.song_id
                WHERE uss.user_id = @UserId
                GROUP BY sg.genre_id
                ORDER BY SUM(uss.play_count) DESC
                LIMIT @TopN";
            var result = await _db.QueryAsync<Guid>(sql, new { UserId = userId, TopN = topN });
            return result.AsList();
        }

        public async Task<List<Guid>> GetSongsByArtistIdsAsync(List<Guid> artistIds, int topN)
        {
            var sql = @"
                SELECT DISTINCT sa.song_id
                FROM song_artists sa
                WHERE sa.artist_id IN @ArtistIds
                LIMIT @TopN";
            var result = await _db.QueryAsync<Guid>(sql, new { ArtistIds = artistIds, TopN = topN });
            return result.AsList();
        }

        public async Task<List<Guid>> GetSongsByGenreIdsAsync(List<Guid> genreIds, int topN)
        {
            var sql = @"
                SELECT DISTINCT sg.song_id
                FROM song_genres sg
                WHERE sg.genre_id IN @GenreIds
                LIMIT @TopN";
            var result = await _db.QueryAsync<Guid>(sql, new { GenreIds = genreIds, TopN = topN });
            return result.AsList();
        }

        public async Task<List<Guid>> GetAlbumsByArtistIdsAsync(List<Guid> artistIds, int topN)
        {
            var sql = @"
                SELECT DISTINCT album_id
                FROM albums
                WHERE artist_id IN @ArtistIds
                LIMIT @TopN";
            var result = await _db.QueryAsync<Guid>(sql, new { ArtistIds = artistIds, TopN = topN });
            return result.AsList();
        }

        public async Task<bool> IsSongAvailableForRecommendationAsync(Guid songId)
        {
            var sql = @"
                SELECT COUNT(1)
                FROM songs s
                WHERE s.song_id = @SongId
                  AND s.is_public = 1
                  AND s.file_url IS NOT NULL
                  AND s.file_url <> ''
                  AND (s.release_status = 'published' OR s.release_status IS NULL)";
            var count = await _db.ExecuteScalarAsync<int>(sql, new { SongId = songId });
            return count > 0;
        }

        public async Task<List<Guid>> GetRelatedBySameArtistAsync(Guid songId, int topN)
        {
            var sql = $@"
                SELECT s.song_id
                FROM songs s
                JOIN song_artists sa ON s.song_id = sa.song_id
                JOIN song_artists base_sa ON sa.artist_id = base_sa.artist_id
                WHERE {PublicPlayableSongFilter}
                  AND base_sa.song_id = @SongId
                GROUP BY s.song_id
                ORDER BY COUNT(DISTINCT sa.artist_id) DESC, MAX(s.created_at) DESC
                LIMIT @TopN";
            var rows = await _db.QueryAsync<Guid>(sql, new { SongId = songId, TopN = topN });
            return rows.AsList();
        }

        public async Task<List<Guid>> GetRelatedBySameGenreAsync(Guid songId, int topN)
        {
            var sql = $@"
                SELECT s.song_id
                FROM songs s
                JOIN song_genres sg ON s.song_id = sg.song_id
                JOIN song_genres base_sg ON sg.genre_id = base_sg.genre_id
                WHERE {PublicPlayableSongFilter}
                  AND base_sg.song_id = @SongId
                GROUP BY s.song_id
                ORDER BY COUNT(DISTINCT sg.genre_id) DESC, MAX(s.created_at) DESC
                LIMIT @TopN";
            var rows = await _db.QueryAsync<Guid>(sql, new { SongId = songId, TopN = topN });
            return rows.AsList();
        }

        public async Task<List<Guid>> GetRelatedByCoListenAsync(Guid songId, int topN)
        {
            var sql = $@"
                SELECT s.song_id
                FROM songs s
                JOIN listening_history lh ON s.song_id = lh.song_id
                WHERE {PublicPlayableSongFilter}
                  AND lh.user_id IN (
                      SELECT DISTINCT user_id
                      FROM listening_history
                      WHERE song_id = @SongId
                  )
                GROUP BY s.song_id
                ORDER BY COUNT(*) DESC, MAX(lh.listened_at) DESC
                LIMIT @TopN";
            var rows = await _db.QueryAsync<Guid>(sql, new { SongId = songId, TopN = topN });
            return rows.AsList();
        }

        public async Task<List<Guid>> GetTrendingSongIdsAsync(int topN)
        {
            var sql = @"
                SELECT s.song_id
                FROM songs s
                LEFT JOIN user_song_stats uss ON s.song_id = uss.song_id
                WHERE s.is_public = 1
                  AND s.file_url IS NOT NULL
                  AND s.file_url <> ''
                  AND (s.release_status = 'published' OR s.release_status IS NULL)
                GROUP BY s.song_id
                ORDER BY COALESCE(SUM(uss.play_count), 0) DESC, MAX(s.created_at) DESC
                LIMIT @TopN";
            var rows = await _db.QueryAsync<Guid>(sql, new { TopN = topN });
            return rows.AsList();
        }

        public async Task<List<Guid>> GetRecentSongIdsAsync(int topN)
        {
            var sql = @"
                SELECT s.song_id
                FROM songs s
                WHERE s.is_public = 1
                  AND s.file_url IS NOT NULL
                  AND s.file_url <> ''
                  AND (s.release_status = 'published' OR s.release_status IS NULL)
                ORDER BY COALESCE(s.published_at, s.created_at) DESC
                LIMIT @TopN";
            var rows = await _db.QueryAsync<Guid>(sql, new { TopN = topN });
            return rows.AsList();
        }

        public async Task<Dictionary<Guid, string>> GetPrimaryGenreNamesAsync(List<Guid> songIds)
        {
            if (songIds == null || songIds.Count == 0)
            {
                return new Dictionary<Guid, string>();
            }

            var sql = @"
                SELECT sg.song_id AS SongId, MIN(g.name) AS GenreName
                FROM song_genres sg
                JOIN genres g ON g.id = sg.genre_id
                WHERE sg.song_id IN @SongIds
                GROUP BY sg.song_id";

            var rows = await _db.QueryAsync<(Guid SongId, string GenreName)>(sql, new { SongIds = songIds });
            return rows.GroupBy(x => x.SongId).ToDictionary(g => g.Key, g => g.First().GenreName);
        }
    }
}
