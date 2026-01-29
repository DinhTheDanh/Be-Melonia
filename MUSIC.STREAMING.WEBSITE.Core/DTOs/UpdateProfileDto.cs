using System;

namespace MUSIC.STREAMING.WEBSITE.Core.DTOs;

public class UpdateProfileDto
{
    public required string FullName { get; set; }

    public string? Bio { get; set; }

    public string? Avatar { get; set; }

    public string? Banner { get; set; }

    public string? ArtistType { get; set; }
}
