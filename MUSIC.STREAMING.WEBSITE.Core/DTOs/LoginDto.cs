using System;

namespace MUSIC.STREAMING.WEBSITE.Core.DTOs;

public class LoginDto
{
    public required string Identifier { get; set; }
    public required string Password { get; set; }
}
