using System;

namespace MUSIC.STREAMING.WEBSITE.Core.DTOs;

public class CreateAlbumDto
{
    public required string Title { get; set; }
    public string? Thumbnail { get; set; }
}

public class UpdateAlbumDto
{
    public required string Title { get; set; }
    public string? Thumbnail { get; set; }
    public DateTime? ReleaseDate { get; set; }
}
