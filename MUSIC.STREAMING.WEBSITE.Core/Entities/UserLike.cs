using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MUSIC.STREAMING.WEBSITE.Core.Entities;

[Table("user_likes")]
public class UserLike
{
    [Key]
    public Guid UserId { get; set; }

    public Guid SongId { get; set; }

    public DateTime LikedAt { get; set; }
}
