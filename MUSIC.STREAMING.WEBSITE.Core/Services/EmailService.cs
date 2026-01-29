using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;

namespace MUSIC.STREAMING.WEBSITE.Core.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }
    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var emailSettings = _config.GetSection("EmailSettings");

        var message = new MimeMessage();

        // Người gửi
        message.From.Add(new MailboxAddress(emailSettings["FromName"], emailSettings["SmtpUser"]));

        // Người nhận
        message.To.Add(new MailboxAddress("", toEmail));

        // Tiêu đề
        message.Subject = subject;

        // Nội dung (HTML)
        message.Body = new TextPart("html")
        {
            Text = body
        };

        using (var client = new SmtpClient())
        {
            // Kết nối tới Gmail SMTP
            await client.ConnectAsync(emailSettings["SmtpHost"], int.Parse(emailSettings["SmtpPort"]), SecureSocketOptions.StartTls);

            // Đăng nhập
            await client.AuthenticateAsync(emailSettings["SmtpUser"], emailSettings["SmtpPass"]);

            // Gửi mail
            await client.SendAsync(message);

            // Ngắt kết nối
            await client.DisconnectAsync(true);
        }
    }
}
