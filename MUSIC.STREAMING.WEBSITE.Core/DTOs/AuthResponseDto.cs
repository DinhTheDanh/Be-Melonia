using System;

namespace MUSIC.STREAMING.WEBSITE.Core.DTOs;

public class AuthResponseDto
{
    public string Token { get; set; }
    public string? RefreshToken { get; set; }
    public string FullName { get; set; }
    public string Avatar { get; set; }
    public string Role { get; set; }
    public bool IsNewUser { get; set; }
}

public class RefreshTokenRequestDto
{
    public required string RefreshToken { get; set; }
}

public class TokenResponseDto
{
    public string Token { get; set; }
    public string RefreshToken { get; set; }
}
