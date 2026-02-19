using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service
{
    public interface IRecommendationService
    {
        Task<List<Guid>> GetRecommendedSongIdsAsync(Guid userId, int topN = 20);
        Task<List<Guid>> GetRecommendedAlbumIdsAsync(Guid userId, int topN = 10);
    }
}
