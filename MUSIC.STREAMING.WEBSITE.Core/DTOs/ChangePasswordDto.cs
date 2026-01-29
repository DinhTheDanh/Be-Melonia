using System;

namespace MUSIC.STREAMING.WEBSITE.Core.DTOs;

public class ChangePasswordDto
{
    public required string CurrentPassword { get; set; }
    public required string NewPassword { get; set; }
    public required string ConfirmPassword { get; set; }
}
