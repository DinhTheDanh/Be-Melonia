using System;

namespace MUSIC.STREAMING.WEBSITE.Core.DTOs;

public class UserProfileDto
{
    public Guid UserId { get; set; }

    public string? Username { get; set; }

    public string? Email { get; set; }

    public string? FullName { get; set; }

    public string? Avatar { get; set; }

    public string? Bio { get; set; }

    public string? Role { get; set; }

}
