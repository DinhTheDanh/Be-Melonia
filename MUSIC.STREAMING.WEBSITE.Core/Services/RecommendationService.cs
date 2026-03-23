using StackExchange.Redis;
using System;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;
using System.Linq;

namespace MUSIC.STREAMING.WEBSITE.Core.Services
{

    public class RecommendationService : IRecommendationService
    {
        private readonly IRecommendationRepository _recommendationRepository;
        private readonly ISongRepository _songRepository;
        private readonly IAlbumRepository _albumRepository;
        private readonly IConnectionMultiplexer _redis;
        private const int MaxLimit = 20;
        private const int DefaultLimit = 6;

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

        public async Task<Result<List<RelatedSongDto>>> GetRelatedSongsAsync(Guid songId, int limit = DefaultLimit, bool excludeExplicit = false, Guid? userId = null)
        {
            if (songId == Guid.Empty)
            {
                return Result<List<RelatedSongDto>>.BadRequest("Invalid songId");
            }

            var normalizedLimit = Math.Clamp(limit <= 0 ? DefaultLimit : limit, 1, MaxLimit);

            if (!await _recommendationRepository.IsSongAvailableForRecommendationAsync(songId))
            {
                return Result<List<RelatedSongDto>>.NotFound("Song not found");
            }

            var userSegment = userId.HasValue ? $"u:{userId.Value}" : "anon";
            var cacheKey = $"related:{songId}:{normalizedLimit}:{userSegment}:{excludeExplicit}";

            try
            {
                var db = _redis.GetDatabase();
                var cached = await db.StringGetAsync(cacheKey);
                if (!cached.IsNullOrEmpty)
                {
                    var cachedData = System.Text.Json.JsonSerializer.Deserialize<List<RelatedSongDto>>(cached!);
                    if (cachedData != null)
                    {
                        return Result<List<RelatedSongDto>>.Success(cachedData);
                    }
                }

                var related = await BuildRelatedSongsAsync(songId, normalizedLimit, excludeExplicit);

                try
                {
                    await db.StringSetAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(related), TimeSpan.FromMinutes(15));
                }
                catch
                {
                    // Best effort cache write.
                }

                return Result<List<RelatedSongDto>>.Success(related);
            }
            catch (RedisException)
            {
                var related = await BuildRelatedSongsAsync(songId, normalizedLimit, excludeExplicit);
                return Result<List<RelatedSongDto>>.Success(related);
            }
        }

        private async Task<List<RelatedSongDto>> BuildRelatedSongsAsync(Guid songId, int limit, bool excludeExplicit)
        {
            var fetchSize = Math.Max(limit * 3, 15);

            var sameArtist = await _recommendationRepository.GetRelatedBySameArtistAsync(songId, fetchSize);
            var sameGenre = await _recommendationRepository.GetRelatedBySameGenreAsync(songId, fetchSize);
            var coListen = await _recommendationRepository.GetRelatedByCoListenAsync(songId, fetchSize);
            var trending = await _recommendationRepository.GetTrendingSongIdsAsync(fetchSize);
            var recent = await _recommendationRepository.GetRecentSongIdsAsync(fetchSize);

            var scoreMap = new Dictionary<Guid, double>();
            var reasonMap = new Dictionary<Guid, HashSet<string>>();

            ApplyWeightedScores(scoreMap, reasonMap, sameArtist, 0.40, "same_artist", songId);
            ApplyWeightedScores(scoreMap, reasonMap, sameGenre, 0.25, "similar_genre", songId);
            ApplyWeightedScores(scoreMap, reasonMap, coListen, 0.20, "co_listen", songId);
            ApplyWeightedScores(scoreMap, reasonMap, trending, 0.10, "trending", songId);
            ApplyWeightedScores(scoreMap, reasonMap, recent, 0.05, "fresh", songId);

            var candidateIds = scoreMap.Keys
                .Where(id => id != songId)
                .ToList();

            if (!candidateIds.Any())
            {
                return new List<RelatedSongDto>();
            }

            var baseSongs = await _songRepository.GetSongsByIdsAsync(new List<Guid> { songId });
            var baseSong = baseSongs.FirstOrDefault();

            var songDtos = await _songRepository.GetSongsByIdsAsync(candidateIds);
            songDtos = songDtos
                .Where(s => s.Id != songId && !string.IsNullOrWhiteSpace(s.FileUrl))
                .ToList();

            if (excludeExplicit)
            {
                // Current schema has no explicit flag, so this remains a no-op filter.
            }

            foreach (var song in songDtos)
            {
                if (!scoreMap.ContainsKey(song.Id))
                {
                    continue;
                }

                if (baseSong != null)
                {
                    var popularityDiff = Math.Abs(song.ListenCount - baseSong.ListenCount) + Math.Abs(song.LikeCount - baseSong.LikeCount);
                    scoreMap[song.Id] += popularityDiff < 1000 ? 0.10 : 0.03;
                }

                var releaseDate = song.PublishedAt ?? song.CreatedAt;
                if (releaseDate >= DateTime.UtcNow.AddDays(-30))
                {
                    scoreMap[song.Id] += 0.05;
                    reasonMap[song.Id].Add("fresh");
                }
            }

            var genreMap = await _recommendationRepository.GetPrimaryGenreNamesAsync(songDtos.Select(x => x.Id).ToList());

            var finalData = songDtos
                .Where(x => scoreMap.ContainsKey(x.Id))
                .OrderByDescending(x => scoreMap[x.Id])
                .ThenByDescending(x => x.ListenCount)
                .Select(x => new RelatedSongDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    ArtistNames = x.ArtistNames,
                    ArtistIds = x.ArtistIds?.Distinct().ToList() ?? new List<Guid>(),
                    Thumbnail = x.Thumbnail,
                    FileUrl = x.FileUrl,
                    Duration = x.Duration,
                    GenreName = genreMap.TryGetValue(x.Id, out var genreName) ? genreName : null,
                    LikeCount = x.LikeCount,
                    ListenCount = x.ListenCount,
                    ReleaseDate = x.PublishedAt ?? x.CreatedAt,
                    Score = Math.Round(scoreMap[x.Id], 3),
                    Reasons = reasonMap.TryGetValue(x.Id, out var reasons)
                        ? reasons.OrderBy(r => r).ToList()
                        : new List<string>()
                })
                .DistinctBy(x => x.Id)
                .Take(limit)
                .ToList();

            if (finalData.Count == 0)
            {
                return finalData;
            }

            var minimumExpected = Math.Min(3, limit);
            if (finalData.Count >= minimumExpected)
            {
                return finalData;
            }

            var fallbackIds = trending
                .Concat(recent)
                .Where(x => x != songId && finalData.All(f => f.Id != x))
                .Distinct()
                .Take(limit - finalData.Count)
                .ToList();

            if (!fallbackIds.Any())
            {
                return finalData;
            }

            var fallbackSongs = await _songRepository.GetSongsByIdsAsync(fallbackIds);
            var fallbackData = fallbackSongs
                .Where(x => !string.IsNullOrWhiteSpace(x.FileUrl) && finalData.All(f => f.Id != x.Id))
                .Select(x => new RelatedSongDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    ArtistNames = x.ArtistNames,
                    ArtistIds = x.ArtistIds?.Distinct().ToList() ?? new List<Guid>(),
                    Thumbnail = x.Thumbnail,
                    FileUrl = x.FileUrl,
                    Duration = x.Duration,
                    GenreName = genreMap.TryGetValue(x.Id, out var genreName) ? genreName : null,
                    LikeCount = x.LikeCount,
                    ListenCount = x.ListenCount,
                    ReleaseDate = x.PublishedAt ?? x.CreatedAt,
                    Score = 0.05,
                    Reasons = new List<string> { "fallback" }
                })
                .Take(limit - finalData.Count)
                .ToList();

            finalData.AddRange(fallbackData);
            return finalData.Take(limit).ToList();
        }

        private static void ApplyWeightedScores(
            Dictionary<Guid, double> scoreMap,
            Dictionary<Guid, HashSet<string>> reasonMap,
            List<Guid> ids,
            double weight,
            string reason,
            Guid excludedSongId)
        {
            foreach (var id in ids)
            {
                if (id == excludedSongId)
                {
                    continue;
                }

                if (!scoreMap.ContainsKey(id))
                {
                    scoreMap[id] = 0;
                    reasonMap[id] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                scoreMap[id] += weight;
                reasonMap[id].Add(reason);
            }
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
