using System;

namespace MUSIC.STREAMING.WEBSITE.Core.DTOs;

public class RegisterDto
{
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public string? FullName { get; set; }
    public string? Avatar { get; set; }

    public List<Guid>? FavoriteGenreIds { get; set; }
}
