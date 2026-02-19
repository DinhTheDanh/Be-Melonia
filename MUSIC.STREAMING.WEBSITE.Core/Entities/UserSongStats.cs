using System;

namespace MUSIC.STREAMING.WEBSITE.Core.Entities
{
    public class UserSongStats
    {
        public Guid UserId { get; set; }
        public Guid SongId { get; set; }
        public int PlayCount { get; set; }
        public int TotalListenTime { get; set; }
        public DateTime? LastPlayed { get; set; }
        public int SkipCount { get; set; }
    }
}
