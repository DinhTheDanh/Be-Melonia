using System;

namespace MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

public interface IEmailService
{
    /// <summary>
    /// Gửi email
    /// </summary>
    /// <param name="toEmail">Email người nhận</param>
    /// <param name="subject">Tiêu đề email</param>
    /// <param name="body">Nội dung email</param>
    /// <returns> </returns>
    Task SendEmailAsync(string toEmail, string subject, string body);
}
