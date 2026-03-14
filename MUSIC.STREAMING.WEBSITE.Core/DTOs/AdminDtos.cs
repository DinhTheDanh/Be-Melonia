namespace MUSIC.STREAMING.WEBSITE.Core.DTOs;

// === Dashboard ===
public class DashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int TotalArtists { get; set; }
    public int TotalSongs { get; set; }
    public int TotalSubscriptions { get; set; }
    public long TotalRevenue { get; set; }
    public int NewUsersToday { get; set; }
    public int NewSongsToday { get; set; }
}

// === User Management ===
public class AdminUserDto
{
    public Guid UserId { get; set; }
    public string? FullName { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = "User";
    public bool IsBanned { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ToggleBanResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsBanned { get; set; }
}

// === Artist Management ===
public class AdminArtistDto
{
    public Guid UserId { get; set; }
    public Guid ArtistId { get; set; }
    public string? FullName { get; set; }
    public string? ArtistName { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public int SongCount { get; set; }
    public int FollowerCount { get; set; }
    public int TotalListens { get; set; }
    public DateTime CreatedAt { get; set; }
}

// === Song Management ===
public class AdminSongDto
{
    public Guid Id { get; set; }
    public Guid SongId { get; set; }
    public string? Title { get; set; }
    public string? ArtistNames { get; set; }
    public string? Thumbnail { get; set; }
    public string? FileUrl { get; set; }
    public int Duration { get; set; }
    public int ListenCount { get; set; }
    public int LikeCount { get; set; }
    public string? GenreName { get; set; }
    public DateTime CreatedAt { get; set; }
}

// === Subscription Management ===
public class AdminSubscriptionDto
{
    public Guid SubscriptionId { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string? FullName { get; set; }
    public Guid PlanId { get; set; }
    public string? PlanName { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

// === Payment Management ===
public class AdminPaymentDto
{
    public Guid PaymentId { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string? FullName { get; set; }
    public long Amount { get; set; }
    public string? PlanName { get; set; }
    public Guid PlanId { get; set; }
    public string Status { get; set; } = "Pending";
    public string PaymentMethod { get; set; } = "VNPay";
    public string? TransactionId { get; set; }
    public DateTime CreatedAt { get; set; }
}

// === Paging response for Admin (matches frontend PagingResult convention) ===
public class AdminPagingResult<T>
{
    public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
    public int FromRecord { get; set; }
    public int ToRecord { get; set; }
}
