using Microsoft.AspNetCore.Mvc;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace MUSIC.STREAMING.WEBSITE.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto loginDto)
        {
            try
            {
                var result = await _authService.LoginWithGoogleAsync(loginDto.IdToken);
                SetTokenCookie(result.Token);
                return Ok(new
                {
                    FullName = result.FullName,
                    Avatar = result.Avatar,
                    Role = result.Role,
                    IsNewUser = result.IsNewUser
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("set-role")]
        public async Task<IActionResult> SetUserRole([FromBody] UpdateRoleDto request)
        {
            try
            {
                // Lấy UserId từ Cookie/Token hiện tại
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
                var userId = Guid.Parse(userIdClaim);

                // Gọi Service để update DB và lấy Token mới
                var newToken = await _authService.SetUserRoleAsync(userId, request.Role);

                // Ghi đè Cookie cũ bằng Token mới (chứa quyền mới)
                SetTokenCookie(newToken);

                return Ok(new { Message = "Cập nhật vai trò thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt");
            return Ok(new { Message = "Đăng xuất thành công" });
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                var result = await _authService.RegisterAsync(registerDto);
                SetTokenCookie(result.Token);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var result = await _authService.LoginAsync(loginDto);
                SetTokenCookie(result.Token);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst("UserId")?.Value!);
                await _authService.ChangePasswordAsync(userId, request);

                return Ok(new { Message = "Đổi mật khẩu thành công. Email thông báo đã được gửi." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            try
            {
                await _authService.ForgotPasswordAsync(dto.Email);
                return Ok(new { Message = "Gửi Email thành công!!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            try
            {
                await _authService.ResetPasswordAsync(dto);
                return Ok(new { Message = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }


        private void SetTokenCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = false,

                Expires = DateTime.UtcNow.AddMinutes(30000),

                Secure = false,

                SameSite = SameSiteMode.Lax
            };

            Response.Cookies.Append("jwt", token, cookieOptions);
        }
    }
}
