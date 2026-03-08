using Microsoft.AspNetCore.Mvc;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;
using System;
using System.Threading.Tasks;

namespace MUSIC.STREAMING.WEBSITE.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
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
            var songs = await _recommendationService.GetRecommendedSongsAsync(userId, topN);
            return Ok(new { Message = "Lấy danh sách bài hát đề xuất thành công", Data = songs });
        }

        [HttpGet("albums/{userId}")]
        public async Task<IActionResult> GetRecommendedAlbums(Guid userId, int topN = 10)
        {
            var albums = await _recommendationService.GetRecommendedAlbumsAsync(userId, topN);
            return Ok(new { Message = "Lấy danh sách album đề xuất thành công", Data = albums });
        }
    }
}
