using AIChatApp.Domain.Interfaces;
using AIChatApp.Infrastructure.Configuration;
using Microsoft.Extensions.Options; 
using System.Net;
using System.Net.Mail;

namespace AIChatApp.Infrastructure.Email;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;

    // 1. Inject IOptions<EmailSettings> to access configuration
    public EmailService(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
    }

    public async Task SendVerificationEmailAsync(string toEmail, string verificationLink)
    {
        try
        {
            // 2. Use the injected settings
            using var client = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
            {
                Credentials = new NetworkCredential(_emailSettings.SmtpUser, _emailSettings.SmtpPass),
                EnableSsl = _emailSettings.EnableSsl // Use the configured SSL setting
            };

            // ... (rest of mail message creation and sending logic)
            var mailMessage = new MailMessage(_emailSettings.FromEmail, toEmail)
            {
                Subject = "Verify your AIChatApp account",
                Body = $"Please click the following link to verify your email: <a href='{verificationLink}'>Verify Account</a>",
                IsBodyHtml = true
            };

            await client.SendMailAsync(mailMessage);
            Console.WriteLine($"Email successfully sent to {toEmail}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Email Error: {ex.Message}");
        }
    }
}