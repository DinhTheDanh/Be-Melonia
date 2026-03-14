namespace MUSIC.STREAMING.WEBSITE.Core.DTOs;

// === Notification DTOs ===

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "general";
    public bool IsRead { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SendNotificationDto
{
    public Guid UserId { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public string Type { get; set; } = "general";
    public Guid? RelatedEntityId { get; set; }
}

public class UnreadCountDto
{
    public int UnreadCount { get; set; }
}

// === Admin Set Role ===

public class AdminSetRoleDto
{
    public required string Role { get; set; } // "User" | "Artist" | "ArtistPremium" | "Admin"
}

public class AdminSetRoleResponseDto
{
    public string Message { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string NewRole { get; set; } = string.Empty;
}

// === Admin Update Payment Status ===

public class AdminUpdatePaymentStatusDto
{
    public required string Status { get; set; } // "Pending" | "Success" | "Failed" | "Rejected" | "Cancelled"
}

public class AdminUpdatePaymentStatusResponseDto
{
    public string Message { get; set; } = string.Empty;
    public Guid PaymentId { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public bool RoleUpdated { get; set; }
}

// === Cancel Expired Payments ===

public class CancelExpiredPaymentsResponseDto
{
    public int CancelledCount { get; set; }
    public List<Guid> CancelledPaymentIds { get; set; } = new();
}
