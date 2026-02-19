using System;

namespace MUSIC.STREAMING.WEBSITE.Core.Entities
{
    public class ListeningHistory
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid SongId { get; set; }
        public DateTime ListenedAt { get; set; }
        public int DurationListened { get; set; }
        public bool Completed { get; set; }
        public string Source { get; set; }
    }
}
