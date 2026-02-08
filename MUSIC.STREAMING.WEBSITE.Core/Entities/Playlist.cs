using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MUSIC.STREAMING.WEBSITE.Core.Entities;

[Table("playlists")]
public class Playlist
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid PlaylistId { get; set; }
    public required string Title { get; set; }
    public Guid UserId { get; set; }
    public string? Thumbnail { get; set; }
    public bool IsPublic { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
