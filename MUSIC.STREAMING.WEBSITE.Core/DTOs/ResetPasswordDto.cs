using System;

namespace MUSIC.STREAMING.WEBSITE.Core.DTOs;

public class ResetPasswordDto
{
    public required string Token { get; set; } // Token lấy từ URL email

    public required string NewPassword { get; set; }

    public required string ConfirmPassword { get; set; }

}
