using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MUSIC.STREAMING.WEBSITE.Core.Entities;

[Table("albums")]
public class Album
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid AlbumId { get; set; }

    public required string Title { get; set; }

    public Guid ArtistId { get; set; }

    public string? Thumbnail { get; set; }

    public DateTime ReleaseDate { get; set; }

    public DateTime CreatedAt { get; set; }
}
