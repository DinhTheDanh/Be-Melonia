using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

namespace MUSIC.STREAMING.WEBSITE.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ArtistController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IMusicService _musicService;
        private readonly IFeatureAuthorizationService _featureAuthorizationService;

        public ArtistController(
            IUserService userService,
            IMusicService musicService,
            IFeatureAuthorizationService featureAuthorizationService)
        {
            _userService = userService;
            _musicService = musicService;
            _featureAuthorizationService = featureAuthorizationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetArtists([FromQuery] string? keyword, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _userService.GetArtistsAsync(keyword, pageIndex, pageSize);
            return Ok(result);
        }

        [HttpGet("{artistId}/songs")]
        public async Task<IActionResult> GetSongsByArtist(Guid artistId, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _musicService.GetSongsByArtistAsync(artistId, pageIndex, pageSize);
            return Ok(result);
        }

        [HttpGet("analytics/dashboard")]
        [Authorize(Roles = "Artist,ArtistPremium,Admin")]
        public async Task<IActionResult> GetAnalyticsDashboard([FromQuery] int days = 30)
        {
            var userIdString = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrWhiteSpace(userIdString))
                return Unauthorized(new { Message = "Vui lòng đăng nhập" });

            var userId = Guid.Parse(userIdString);

            var featureResult = await _featureAuthorizationService.HasAdvancedAnalyticsAsync(userId);
            if (featureResult.IsFailure)
                return BadRequest(new { Message = featureResult.Error });

            var role = User.FindFirst("Role")?.Value;
            var isAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);

            if (!isAdmin && !featureResult.Data)
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Gói hiện tại chưa hỗ trợ analytics nâng cao" });

            var data = await _userService.GetArtistAnalyticsDashboardAsync(userId, days);
            return Ok(new
            {
                Message = "Lấy dữ liệu dashboard thành công",
                Data = data
            });
        }

        [HttpGet("analytics/top-songs")]
        [Authorize(Roles = "Artist,ArtistPremium,Admin")]
        public async Task<IActionResult> GetAnalyticsTopSongs(
            [FromQuery] int days = 30,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var userIdString = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrWhiteSpace(userIdString))
                return Unauthorized(new { Message = "Vui lòng đăng nhập" });

            var userId = Guid.Parse(userIdString);

            var featureResult = await _featureAuthorizationService.HasAdvancedAnalyticsAsync(userId);
            if (featureResult.IsFailure)
                return BadRequest(new { Message = featureResult.Error });

            var role = User.FindFirst("Role")?.Value;
            var isAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);

            if (!isAdmin && !featureResult.Data)
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Gói hiện tại chưa hỗ trợ analytics nâng cao" });

            var result = await _userService.GetArtistTopSongsAsync(userId, days, pageIndex, pageSize);
            return Ok(result);
        }
    }
}
