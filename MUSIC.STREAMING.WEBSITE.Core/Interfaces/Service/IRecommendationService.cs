using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;

namespace MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service
{
    public interface IRecommendationService
    {
        Task<List<SongDto>> GetRecommendedSongsAsync(Guid userId, int topN = 20);
        Task<List<AlbumDto>> GetRecommendedAlbumsAsync(Guid userId, int topN = 10);
        Task<Result<List<RelatedSongDto>>> GetRelatedSongsAsync(Guid songId, int limit = 6, bool excludeExplicit = false, Guid? userId = null);
    }
}
