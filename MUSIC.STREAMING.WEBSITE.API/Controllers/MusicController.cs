using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MUSIC.STREAMING.WEBSITE.API.Extensions;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Helpers;
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
        public async Task<IActionResult> GetAlbums([FromQuery] string? keyword, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _musicService.GetAlbumsAsync(keyword ?? "", pageIndex, pageSize);
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

        [Authorize]
        [HttpGet("my-songs")]
        public async Task<IActionResult> GetMySongs([FromQuery] string? keyword, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userIdString = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

                var userId = Guid.Parse(userIdString);
                var result = await _musicService.GetUserSongsAsync(userId, keyword ?? "", pageIndex, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("my-albums")]
        public async Task<IActionResult> GetMyAlbums([FromQuery] string? keyword, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userIdString = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

                var userId = Guid.Parse(userIdString);
                var result = await _musicService.GetUserAlbumsAsync(userId, keyword ?? "", pageIndex, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("my-playlists")]
        public async Task<IActionResult> GetMyPlaylists([FromQuery] string? keyword, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userIdString = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

                var userId = Guid.Parse(userIdString);
                var result = await _musicService.GetUserPlaylistsAsync(userId, keyword ?? "", pageIndex, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("playlists")]
        public async Task<IActionResult> GetAllPlaylists([FromQuery] string? keyword, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _musicService.GetAllPlaylistsAsync(keyword ?? "", pageIndex, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpDelete("song/{songId}")]
        public async Task<IActionResult> DeleteSong(Guid songId)
        {
            var userIdString = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

            var artistId = Guid.Parse(userIdString);
            var result = await _musicService.DeleteSongAsync(artistId, songId);
            return result.ToActionResult();
        }

        [Authorize]
        [HttpPut("song/{songId}")]
        public async Task<IActionResult> UpdateSong(Guid songId, [FromBody] UpdateSongDto dto)
        {
            var userIdString = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

            var artistId = Guid.Parse(userIdString);
            var result = await _musicService.UpdateSongAsync(artistId, songId, dto);
            return result.ToActionResult();
        }

        [Authorize]
        [HttpDelete("album/{albumId}")]
        public async Task<IActionResult> DeleteAlbum(Guid albumId)
        {
            var userIdString = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

            var artistId = Guid.Parse(userIdString);
            var result = await _musicService.DeleteAlbumAsync(artistId, albumId);
            return result.ToActionResult();
        }

        [Authorize]
        [HttpPut("album/{albumId}")]
        public async Task<IActionResult> UpdateAlbum(Guid albumId, [FromBody] UpdateAlbumDto dto)
        {
            var userIdString = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

            var artistId = Guid.Parse(userIdString);
            var result = await _musicService.UpdateAlbumAsync(artistId, albumId, dto);
            return result.ToActionResult();
        }
    }
}
