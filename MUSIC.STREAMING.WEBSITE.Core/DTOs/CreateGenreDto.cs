using System;

namespace MUSIC.STREAMING.WEBSITE.Core.DTOs;

public class CreateGenreDto
{
    public required string Name { get; set; }
    public string? ImageUrl { get; set; }
}
