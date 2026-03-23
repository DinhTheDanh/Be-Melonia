using System;

namespace MUSIC.STREAMING.WEBSITE.Core.DTOs;

public class SongDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Thumbnail { get; set; }
    public string FileUrl { get; set; }
    public int Duration { get; set; }

    public Guid? AlbumId { get; set; }
    public string? AlbumTitle { get; set; }

    public string ArtistNames { get; set; }
    public List<Guid>? ArtistIds { get; set; } = new();
    public int LikeCount { get; set; }
    public int ListenCount { get; set; }
    public DateTime? ScheduledReleaseAt { get; set; }
    public string ReleaseStatus { get; set; } = "published";
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class RelatedSongDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ArtistNames { get; set; } = string.Empty;
    public List<Guid> ArtistIds { get; set; } = new();
    public string? Thumbnail { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public int Duration { get; set; }
    public string? GenreName { get; set; }
    public int LikeCount { get; set; }
    public int ListenCount { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public double Score { get; set; }
    public List<string> Reasons { get; set; } = new();
}

public class ScheduledSongQueueItemDto
{
    public Guid SongId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ArtistNames { get; set; } = string.Empty;
    public string? Thumbnail { get; set; }
    public DateTime? ScheduledReleaseAt { get; set; }
    public string ReleaseStatus { get; set; } = "pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class AlbumDto
{
    public Guid AlbumId { get; set; }
    public string Title { get; set; }
    public string Thumbnail { get; set; }
    public Guid ArtistId { get; set; }
    public string ArtistName { get; set; }
    public DateTime ReleaseDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class PopularAlbumDto : AlbumDto
{
    public double Score { get; set; }
    public int Rank { get; set; }
    public int Streams { get; set; }
    public int UniqueListeners { get; set; }
    public int SaveCount { get; set; }
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
    public string? Banner { get; set; }
    public string Bio { get; set; }
    public string? ArtistType { get; set; }
    public int FollowerCount { get; set; }
    public int SongCount { get; set; }
    public int TotalLikes { get; set; }
    public int TotalListens { get; set; }
}

public class PlaylistDto
{
    public Guid PlaylistId { get; set; }
    public string Title { get; set; }
    public string? Thumbnail { get; set; }
    public string? Description { get; set; }
    public string CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int SongCount { get; set; }
}

public class AlbumDetailsDto
{
    public AlbumDto? Album { get; set; }
    public PagingResult<SongDto>? Songs { get; set; }
}

public class PlaylistInfoDto
{
    public Guid PlaylistId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Thumbnail { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public Guid? CreatedById { get; set; }
}

public class PlaylistDetailsDto
{
    public PlaylistInfoDto? Playlist { get; set; }
    public PagingResult<SongDto>? Songs { get; set; }
}

public class ArtistStatsDto
{
    public Guid ArtistId { get; set; }
    public int FollowerCount { get; set; }
    public int SongCount { get; set; }
    public int TotalLikes { get; set; }
    public int TotalListens { get; set; }
}

public class ArtistAnalyticsSummaryDto
{
    public int TotalFollowers { get; set; }
    public int TotalListens { get; set; }
    public int TotalLikes { get; set; }
    public int TotalSongs { get; set; }
}

public class ArtistTrendPointDto
{
    public string Date { get; set; } = string.Empty;
    public int Followers { get; set; }
    public int Listens { get; set; }
    public int Likes { get; set; }
}

public class ArtistAnalyticsDashboardDto
{
    public ArtistAnalyticsSummaryDto Summary { get; set; } = new();
    public List<ArtistTrendPointDto> Trends { get; set; } = new();
}

public class ArtistDailyIncrementDto
{
    public DateTime Date { get; set; }
    public int FollowersDelta { get; set; }
    public int ListensDelta { get; set; }
    public int LikesDelta { get; set; }
}

public class ArtistTopSongDto
{
    public Guid SongId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Thumbnail { get; set; }
    public int Listens { get; set; }
    public int Likes { get; set; }
    public int FollowersGained { get; set; }
}

public class RecordPlayDto
{
    public Guid SongId { get; set; }
    public int DurationListened { get; set; }
    public bool Completed { get; set; }
    public string? Source { get; set; }
}

