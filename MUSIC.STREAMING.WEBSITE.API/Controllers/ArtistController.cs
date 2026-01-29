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

        public ArtistController(IUserService userService, IMusicService musicService)
        {
            _userService = userService;
            _musicService = musicService;
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
    }
}
