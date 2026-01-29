using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Entities;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

namespace MUSIC.STREAMING.WEBSITE.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class InteractionController : ControllerBase
    {

        private readonly IInteractionService _interactionService;
        private readonly IMusicService _musicService;

        public InteractionController(IInteractionService interactionService, IMusicService musicService)
        {
            _interactionService = interactionService;
            _musicService = musicService;
        }

        [HttpPost("like/{songId}")]
        public async Task<IActionResult> ToggleLike(Guid songId)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst("UserId")?.Value!);
                var result = await _interactionService.ToggleLikeAsync(userId, songId);
                return Ok(new { IsLiked = result.IsLiked, Message = result.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("liked-songs")]
        public async Task<IActionResult> GetLikedSongs([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst("UserId")?.Value!);
                var songs = await _interactionService.GetLikedSongsAsync(userId, pageIndex, pageSize);
                return Ok(songs);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("playlist")]
        public async Task<IActionResult> CreatePlaylist([FromBody] CreatePlaylistDto dto)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst("UserId")?.Value!);
                var result = await _musicService.CreatePlaylistAsync(userId, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("playlist/{playlistId}/add-song/{songId}")]
        public async Task<IActionResult> AddSongToPlaylist(Guid playlistId, Guid songId)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst("UserId")?.Value!);

                await _interactionService.AddSongToPlaylistAsync(userId, playlistId, songId);

                return Ok(new { Message = "Đã thêm bài hát vào playlist thành công" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("follow/{targetUserId}")]
        public async Task<IActionResult> ToggleFollow(Guid targetUserId)
        {
            try
            {
                var currentUserId = Guid.Parse(User.FindFirst("UserId")?.Value!);

                var result = await _interactionService.ToggleFollowAsync(currentUserId, targetUserId);

                return Ok(new { IsFollowing = result.IsFollowing, Message = result.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("followings")]
        public async Task<IActionResult> GetFollowings([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var currentUserId = Guid.Parse(User.FindFirst("UserId")?.Value!);

                var result = await _interactionService.GetFollowingsAsync(currentUserId, pageIndex, pageSize);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("playlist/{playlistId}/remove-song/{songId}")]
        public async Task<IActionResult> RemoveSongFromPlaylist(Guid playlistId, Guid songId)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst("UserId")?.Value!);
                await _interactionService.RemoveSongFromPlaylistAsync(userId, playlistId, songId);
                return Ok(new { Message = "Đã xóa bài hát khỏi playlist thành công" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("playlist/{playlistId}")]
        public async Task<IActionResult> UpdatePlaylist(Guid playlistId, [FromBody] dynamic data)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst("UserId")?.Value!);
                string title = data.title;
                var result = await _interactionService.UpdatePlaylistAsync(userId, playlistId, title);
                return Ok(new { Message = "Cập nhật playlist thành công", Data = result });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("playlist/{playlistId}")]
        public async Task<IActionResult> DeletePlaylist(Guid playlistId)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst("UserId")?.Value!);
                await _interactionService.DeletePlaylistAsync(userId, playlistId);
                return Ok(new { Message = "Xóa playlist thành công" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("playlist/{playlistId}")]
        public async Task<IActionResult> GetPlaylistDetails(Guid playlistId, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _interactionService.GetPlaylistDetailsAsync(playlistId, pageIndex, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("album/{albumId}/remove-song/{songId}")]
        public async Task<IActionResult> RemoveSongFromAlbum(Guid albumId, Guid songId)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst("UserId")?.Value!);
                await _interactionService.RemoveSongFromAlbumAsync(userId, albumId, songId);
                return Ok(new { Message = "Đã xóa bài hát khỏi album thành công" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
