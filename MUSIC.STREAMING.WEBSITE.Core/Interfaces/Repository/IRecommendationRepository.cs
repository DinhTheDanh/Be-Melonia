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
    }
}
