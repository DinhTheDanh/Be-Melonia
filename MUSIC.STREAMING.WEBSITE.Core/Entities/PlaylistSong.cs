using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MUSIC.STREAMING.WEBSITE.Core.Entities;

[Table("playlist_songs")]
public class PlaylistSong
{
    [Key]
    public int Id { get; set; } // Auto increment
    public Guid PlaylistId { get; set; }
    public Guid SongId { get; set; }
    public DateTime AddedAt { get; set; }
}
