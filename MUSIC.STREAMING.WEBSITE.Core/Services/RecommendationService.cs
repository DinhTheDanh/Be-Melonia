using StackExchange.Redis;
using System;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

namespace MUSIC.STREAMING.WEBSITE.Core.Services
{

    public class RecommendationService : IRecommendationService
    {
        private readonly IRecommendationRepository _recommendationRepository;
        private readonly IConnectionMultiplexer _redis;

        public RecommendationService(IRecommendationRepository recommendationRepository, IConnectionMultiplexer redis)
        {
            _recommendationRepository = recommendationRepository;
            _redis = redis;
        }

        public async Task<List<Guid>> GetRecommendedSongIdsAsync(Guid userId, int topN = 20)
        {
            var cacheKey = $"recommendation:songs:{userId}";
            try
            {
                var db = _redis.GetDatabase();
                var cached = await db.StringGetAsync(cacheKey);
                if (!cached.IsNullOrEmpty)
                    return System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(cached!)!;

                var topSongIds = await _recommendationRepository.GetTopSongIdsAsync(userId, 10);
                var topArtistIds = await _recommendationRepository.GetTopArtistIdsAsync(userId, 5);
                var topGenreIds = await _recommendationRepository.GetTopGenreIdsAsync(userId, 5);

                var recommendedSongIds = new List<Guid>();

                if (topArtistIds.Any())
                {
                    var songsByArtist = await _recommendationRepository.GetSongsByArtistIdsAsync(topArtistIds, topN);
                    recommendedSongIds.AddRange(songsByArtist);
                }

                if (topGenreIds.Any())
                {
                    var songsByGenre = await _recommendationRepository.GetSongsByGenreIdsAsync(topGenreIds, topN);
                    recommendedSongIds.AddRange(songsByGenre);
                }

                var result = recommendedSongIds.Distinct().Except(topSongIds).Take(topN).ToList();

                try { await db.StringSetAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(result), TimeSpan.FromMinutes(10)); } catch { }
                return result;
            }
            catch (RedisException)
            {
                // Fallback: trả về dự liệu từ DB không có cache
                var topSongIds = await _recommendationRepository.GetTopSongIdsAsync(userId, 10);
                var topArtistIds = await _recommendationRepository.GetTopArtistIdsAsync(userId, 5);
                var topGenreIds = await _recommendationRepository.GetTopGenreIdsAsync(userId, 5);
                var recommendedSongIds = new List<Guid>();
                if (topArtistIds.Any()) recommendedSongIds.AddRange(await _recommendationRepository.GetSongsByArtistIdsAsync(topArtistIds, topN));
                if (topGenreIds.Any()) recommendedSongIds.AddRange(await _recommendationRepository.GetSongsByGenreIdsAsync(topGenreIds, topN));
                return recommendedSongIds.Distinct().Except(topSongIds).Take(topN).ToList();
            }
        }

        public async Task<List<Guid>> GetRecommendedAlbumIdsAsync(Guid userId, int topN = 10)
        {
            var cacheKey = $"recommendation:albums:{userId}";
            try
            {
                var db = _redis.GetDatabase();
                var cached = await db.StringGetAsync(cacheKey);
                if (!cached.IsNullOrEmpty)
                    return System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(cached!)!;

                var topArtistIds = await _recommendationRepository.GetTopArtistIdsAsync(userId, 5);
                var result = new List<Guid>();
                if (topArtistIds.Any())
                {
                    var albums = await _recommendationRepository.GetAlbumsByArtistIdsAsync(topArtistIds, topN);
                    result.AddRange(albums);
                }
                result = result.Distinct().Take(topN).ToList();

                try { await db.StringSetAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(result), TimeSpan.FromMinutes(10)); } catch { }
                return result;
            }
            catch (RedisException)
            {
                var topArtistIds = await _recommendationRepository.GetTopArtistIdsAsync(userId, 5);
                var result = new List<Guid>();
                if (topArtistIds.Any()) result.AddRange(await _recommendationRepository.GetAlbumsByArtistIdsAsync(topArtistIds, topN));
                return result.Distinct().Take(topN).ToList();
            }
        }
    }
}
