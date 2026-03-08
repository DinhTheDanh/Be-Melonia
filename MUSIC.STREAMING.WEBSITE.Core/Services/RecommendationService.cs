using StackExchange.Redis;
using System;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

namespace MUSIC.STREAMING.WEBSITE.Core.Services
{

    public class RecommendationService : IRecommendationService
    {
        private readonly IRecommendationRepository _recommendationRepository;
        private readonly ISongRepository _songRepository;
        private readonly IAlbumRepository _albumRepository;
        private readonly IConnectionMultiplexer _redis;

        public RecommendationService(
            IRecommendationRepository recommendationRepository,
            ISongRepository songRepository,
            IAlbumRepository albumRepository,
            IConnectionMultiplexer redis)
        {
            _recommendationRepository = recommendationRepository;
            _songRepository = songRepository;
            _albumRepository = albumRepository;
            _redis = redis;
        }

        public async Task<List<SongDto>> GetRecommendedSongsAsync(Guid userId, int topN = 20)
        {
            var cacheKey = $"recommendation:songs:{userId}";
            try
            {
                var db = _redis.GetDatabase();
                var cached = await db.StringGetAsync(cacheKey);
                if (!cached.IsNullOrEmpty)
                    return System.Text.Json.JsonSerializer.Deserialize<List<SongDto>>(cached!)!;

                var songDtos = await GetRecommendedSongDtosFromDb(userId, topN);

                try { await db.StringSetAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(songDtos), TimeSpan.FromMinutes(10)); } catch { }
                return songDtos;
            }
            catch (RedisException)
            {
                return await GetRecommendedSongDtosFromDb(userId, topN);
            }
        }

        private async Task<List<SongDto>> GetRecommendedSongDtosFromDb(Guid userId, int topN)
        {
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

            var resultIds = recommendedSongIds.Distinct().Except(topSongIds).Take(topN).ToList();

            if (!resultIds.Any()) return new List<SongDto>();

            return await _songRepository.GetSongsByIdsAsync(resultIds);
        }

        public async Task<List<AlbumDto>> GetRecommendedAlbumsAsync(Guid userId, int topN = 10)
        {
            var cacheKey = $"recommendation:albums:{userId}";
            try
            {
                var db = _redis.GetDatabase();
                var cached = await db.StringGetAsync(cacheKey);
                if (!cached.IsNullOrEmpty)
                    return System.Text.Json.JsonSerializer.Deserialize<List<AlbumDto>>(cached!)!;

                var albumDtos = await GetRecommendedAlbumDtosFromDb(userId, topN);

                try { await db.StringSetAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(albumDtos), TimeSpan.FromMinutes(10)); } catch { }
                return albumDtos;
            }
            catch (RedisException)
            {
                return await GetRecommendedAlbumDtosFromDb(userId, topN);
            }
        }

        private async Task<List<AlbumDto>> GetRecommendedAlbumDtosFromDb(Guid userId, int topN)
        {
            var topArtistIds = await _recommendationRepository.GetTopArtistIdsAsync(userId, 5);
            var resultIds = new List<Guid>();
            if (topArtistIds.Any())
            {
                var albums = await _recommendationRepository.GetAlbumsByArtistIdsAsync(topArtistIds, topN);
                resultIds.AddRange(albums);
            }
            resultIds = resultIds.Distinct().Take(topN).ToList();

            if (!resultIds.Any()) return new List<AlbumDto>();

            return await _albumRepository.GetAlbumsByIdsAsync(resultIds);
        }
    }
}
