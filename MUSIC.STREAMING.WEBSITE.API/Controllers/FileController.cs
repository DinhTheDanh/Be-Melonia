using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

namespace MUSIC.STREAMING.WEBSITE.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly ICloudinaryService _cloudinaryService;
        public FileController(ICloudinaryService cloudinaryService)
        {
            _cloudinaryService = cloudinaryService;
        }

        // [Authorize(Roles = "Artist,Admin")]
        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            try
            {
                // Gọi service upload lên Cloudinary
                var imageUrl = await _cloudinaryService.UploadImageAsync(file);

                // Trả về link ảnh cho Frontend
                return Ok(new { Url = imageUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
        [HttpPost("upload-audio")]
        // [Authorize(Roles = "Artist,Admin")]
        public async Task<IActionResult> UploadAudio(IFormFile file)
        {
            try
            {
                // 1. Upload lên Cloudinary
                var audioUrl = await _cloudinaryService.UploadAudioAsync(file);

                // 2. Trả về Link nhạc
                return Ok(new { Url = audioUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
        [HttpGet("signature")]
        // [Authorize]
        public IActionResult GetSignature()
        {
            try
            {
                var signData = _cloudinaryService.GetMediaUploadSignature();
                return Ok(signData);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
