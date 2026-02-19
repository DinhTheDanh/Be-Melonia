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
    }
}
