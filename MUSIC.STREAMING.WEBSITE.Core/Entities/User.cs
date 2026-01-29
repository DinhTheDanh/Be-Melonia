

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MUSIC.STREAMING.WEBSITE.Core.Entities;

[Table("users")]
public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid UserId { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public required string Email { get; set; }

    public string AuthSource { get; set; } = "local";

    public string? ExternalId { get; set; }

    public string? FullName { get; set; }

    public string? Avatar { get; set; }

    public string? Bio { get; set; }

    public string Role { get; set; } = "User";

    public string? ResetToken { get; set; }

    public DateTime? ResetTokenExpiry { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Banner { get; set; }

    public string? ArtistType { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

}