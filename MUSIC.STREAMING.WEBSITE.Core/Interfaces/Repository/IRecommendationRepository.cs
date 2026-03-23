using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MUSIC.STREAMING.WEBSITE.Core.Entities;

namespace MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository
{
    public interface IRecommendationRepository
    {
        Task<List<ListeningHistory>> GetListeningHistoryAsync(Guid userId);
        Task<List<UserSongStats>> GetUserSongStatsAsync(Guid userId);
        Task<List<Guid>> GetTopSongIdsAsync(Guid userId, int topN);
        Task<List<Guid>> GetTopArtistIdsAsync(Guid userId, int topN);
        Task<List<Guid>> GetTopGenreIdsAsync(Guid userId, int topN);
        Task<List<Guid>> GetSongsByArtistIdsAsync(List<Guid> artistIds, int topN);
        Task<List<Guid>> GetSongsByGenreIdsAsync(List<Guid> genreIds, int topN);
        Task<List<Guid>> GetAlbumsByArtistIdsAsync(List<Guid> artistIds, int topN);
        Task<bool> IsSongAvailableForRecommendationAsync(Guid songId);
        Task<List<Guid>> GetRelatedBySameArtistAsync(Guid songId, int topN);
        Task<List<Guid>> GetRelatedBySameGenreAsync(Guid songId, int topN);
        Task<List<Guid>> GetRelatedByCoListenAsync(Guid songId, int topN);
        Task<List<Guid>> GetTrendingSongIdsAsync(int topN);
        Task<List<Guid>> GetRecentSongIdsAsync(int topN);
        Task<Dictionary<Guid, string>> GetPrimaryGenreNamesAsync(List<Guid> songIds);
    }
}
