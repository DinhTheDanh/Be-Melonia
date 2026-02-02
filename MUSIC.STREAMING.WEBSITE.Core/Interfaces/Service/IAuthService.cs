using System;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;

namespace MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

public interface IAuthService
{
    /// <summary>
    /// Xử lý đăng nhập bằng Google Token
    /// </summary>
    /// <param name="idToken">Token nhận được</param>
    /// <returns>Thông tin User và JWT Token</returns>
    Task<AuthResponseDto> LoginWithGoogleAsync(string idToken);

    /// <summary>
    /// Xử lý set role
    /// </summary>
    /// <param name="userId">Id của người dùng</param>
    /// <param name="newRole">Role mới</param>
    /// <returns>Trả về token mới</returns>
    Task<string> SetUserRoleAsync(Guid userId, string newRole);

    /// <summary>
    /// Đăng ký 
    /// </summary>
    /// <param name="registerDto">DTO đăng ký</param>
    /// <returns>Thông tin User và JWT Token</returns> 

    Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);

    /// <summary>
    /// Đăng nhập
    /// </summary>
    /// <param name="loginDto">DTO đăng nhập</param>
    /// <returns>Thông tin User và JWT Token</returns>
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto);

    /// <summary>
    /// Thay đổi mật khẩu
    /// </summary>
    /// <param name="userId">ID của user</param>
    /// <param name="dto">DTO Mật khẩu</param>
    /// <returns>Gửi mail </returns>
    Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto);

    /// <summary>
    /// Quên mật khẩu
    /// </summary>
    /// <param name="email">Email </param>
    /// <returns></returns>
    Task ForgotPasswordAsync(string email);

    /// <summary>
    /// Reset lại mật khẩu
    /// </summary>
    /// <param name="dto">DTO Reset</param>
    /// <returns></returns>
    Task ResetPasswordAsync(ResetPasswordDto dto);

    /// <summary>
    /// Refresh access token bằng refresh token
    /// </summary>
    /// <param name="refreshToken">Refresh token</param>
    /// <returns>Token mới</returns>
    Task<TokenResponseDto> RefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Đăng xuất - revoke refresh token
    /// </summary>
    /// <param name="refreshToken">Refresh token cần revoke</param>
    /// <returns></returns>
    Task LogoutAsync(string refreshToken);
}
