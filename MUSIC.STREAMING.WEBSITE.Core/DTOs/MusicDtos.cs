using System;

namespace MUSIC.STREAMING.WEBSITE.Core.DTOs;

public class SongDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Thumbnail { get; set; }
    public string FileUrl { get; set; }
    public int Duration { get; set; }

    public string ArtistNames { get; set; }
    public List<Guid>? ArtistIds { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class AlbumDto
{
    public Guid AlbumId { get; set; }
    public string Title { get; set; }
    public string Thumbnail { get; set; }
    public string ArtistName { get; set; }
    public DateTime ReleaseDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class GenreDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? ImageUrl { get; set; }
}

public class ArtistDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; }
    public string Avatar { get; set; }
    public string Bio { get; set; }
    public string? ArtistType { get; set; }
}

public class PlaylistDto
{
    public Guid PlaylistId { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public string CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int SongCount { get; set; }
}

