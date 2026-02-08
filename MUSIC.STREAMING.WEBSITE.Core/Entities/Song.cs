using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MUSIC.STREAMING.WEBSITE.Core.Entities;

[Table("songs")]
public class Song
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid SongId { get; set; }

    public required string Title { get; set; }

    public Guid? AlbumId { get; set; }

    public required string FileUrl { get; set; }

    public string? Thumbnail { get; set; }

    public int Duration { get; set; }

    public string? Lyrics { get; set; }

    public bool IsPublic { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? FileHash { get; set; }
}
