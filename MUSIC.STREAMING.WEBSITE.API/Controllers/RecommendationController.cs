using Microsoft.AspNetCore.Mvc;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;
using System;
using System.Threading.Tasks;

namespace MUSIC.STREAMING.WEBSITE.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecommendationController : ControllerBase
    {
        private readonly IRecommendationService _recommendationService;

        public RecommendationController(IRecommendationService recommendationService)
        {
            _recommendationService = recommendationService;
        }

        [HttpGet("songs/{userId}")]
        public async Task<IActionResult> GetRecommendedSongs(Guid userId, int topN = 20)
        {
            var songIds = await _recommendationService.GetRecommendedSongIdsAsync(userId, topN);
            return Ok(songIds);
        }

        [HttpGet("albums/{userId}")]
        public async Task<IActionResult> GetRecommendedAlbums(Guid userId, int topN = 10)
        {
            var albumIds = await _recommendationService.GetRecommendedAlbumIdsAsync(userId, topN);
            return Ok(albumIds);
        }
    }
}
