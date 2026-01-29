using System;

namespace MUSIC.STREAMING.WEBSITE.Core.DTOs;

public class CreateAlbumDto
{
    public required string Title { get; set; }
    public string? Thumbnail { get; set; }
}
