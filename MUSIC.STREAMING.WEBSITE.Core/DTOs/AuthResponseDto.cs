using System;

namespace MUSIC.STREAMING.WEBSITE.Core.DTOs;

public class AuthResponseDto
{
    public string Token { get; set; }
    public string FullName { get; set; }
    public string Avatar { get; set; }
    public string Role { get; set; }
    public bool IsNewUser { get; set; }
}
