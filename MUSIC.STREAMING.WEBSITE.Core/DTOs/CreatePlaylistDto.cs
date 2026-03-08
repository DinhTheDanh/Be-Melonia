using System;

namespace MUSIC.STREAMING.WEBSITE.Core.DTOs;

public class CreatePlaylistDto
{
    public required string Title { get; set; }
    public string? Thumbnail { get; set; }
    public bool IsPublic { get; set; } = true;
}

public class UpdatePlaylistDto
{
    public required string Title { get; set; }
    public string? Thumbnail { get; set; }
    public string? Description { get; set; }
    public bool? IsPublic { get; set; }
}
