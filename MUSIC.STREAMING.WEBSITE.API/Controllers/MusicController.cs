using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

namespace MUSIC.STREAMING.WEBSITE.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class MusicController : ControllerBase
    {

        private readonly IMusicService _musicService;

        public MusicController(IMusicService musicService, ISongRepository songRepo)
        {
            _musicService = musicService;
        }

        [HttpGet("songs")]
        public async Task<IActionResult> GetAllSongs([FromQuery] string? keyword, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _musicService.GetAllSongsAsync(keyword ?? "", pageIndex, pageSize);
            return Ok(result);
        }

        [HttpGet("genres")]
        public async Task<IActionResult> GetGenres()
        {
            var result = await _musicService.GetAllGenresAsync();
            return Ok(result);
        }

        [HttpGet("albums")]
        public async Task<IActionResult> GetAlbums([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _musicService.GetAlbumsAsync(pageIndex, pageSize);
            return Ok(result);
        }

        [HttpGet("check-hash/{hash}")]
        public async Task<IActionResult> CheckHash(string hash)
        {
            if (string.IsNullOrEmpty(hash)) return BadRequest("Hash không được để trống");

            var existingSong = await _musicService.CheckFileHashAsync(hash);

            if (existingSong != null)
            {
                return Ok(new
                {
                    Exists = true,
                    FileUrl = existingSong.FileUrl,
                    Duration = existingSong.Duration
                });
            }

            return Ok(new { Exists = false });
        }

        [HttpPost("song")]
        // [Authorize(Roles = "Artist,Admin")] // Chỉ Artist mới được up nhạc
        public async Task<IActionResult> UploadSong([FromBody] CreateSongDto dto)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst("UserId")?.Value!);
                var result = await _musicService.CreateSongAsync(userId, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
        [HttpPost("album")]
        // [Authorize(Roles = "Artist,Admin")]
        public async Task<IActionResult> CreateAlbum([FromBody] CreateAlbumDto dto)
        {
            var userId = Guid.Parse(User.FindFirst("UserId")?.Value!);
            var result = await _musicService.CreateAlbumAsync(userId, dto);
            return Ok(result);
        }

        [HttpPost("genre")]
        // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateGenre([FromBody] CreateGenreDto dto)
        {
            try
            {
                var result = await _musicService.CreateGenreAsync(dto);
                return Ok(new { Message = "Tạo thể loại thành công", Data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
