using System;

namespace MUSIC.STREAMING.WEBSITE.Core.DTOs;

public class CreateSongDto
{
    public required string Title { get; set; }

    public required string FileUrl { get; set; }

    public required List<Guid> ArtistIds { get; set; }

    public Guid? AlbumId { get; set; } // Nếu thuộc album nào thì truyền lên

    public string? Thumbnail { get; set; } // Nếu null server sẽ tự random

    public int Duration { get; set; }

    public required List<Guid> GenreIds { get; set; }

    public string? Lyrics { get; set; }

    public string? FileHash { get; set; }
}
