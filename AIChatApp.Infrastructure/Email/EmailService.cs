using AIChatApp.Domain.Interfaces;
using AIChatApp.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace AIChatApp.Infrastructure.Email;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private SmtpClient? _smtpClient;

    public EmailService(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
    }

    // Lazy initialization of SMTP client (reused across calls)
    private SmtpClient GetSmtpClient()
    {
        _smtpClient ??= new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
        {
            Credentials = new NetworkCredential(_emailSettings.SmtpUser, _emailSettings.SmtpPass),
            EnableSsl = _emailSettings.EnableSsl
        };
        return _smtpClient;
    }

    public async Task SendVerificationEmailAsync(string toEmail, string verificationLink)
    {
        try
        {
            var client = GetSmtpClient();

            var mailMessage = new MailMessage(_emailSettings.FromEmail, toEmail)
            {
                Subject = "Verify your AIChatApp account",
                Body = $@"
                    <html>
                        <body style='font-family: Arial, sans-serif;'>
                            <h2>Email Verification</h2>
                            <p>Thank you for registering! Please click the link below to verify your email address:</p>
                            <p><a href='{verificationLink}' style='background-color: #4F46E5; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Verify Email</a></p>
                            <p>Or copy and paste this link: {verificationLink}</p>
                            <p>This link expires in 24 hours.</p>
                        </body>
                    </html>",
                IsBodyHtml = true
            };

            await client.SendMailAsync(mailMessage);
            mailMessage.Dispose();
            Console.WriteLine($"Verification email successfully sent to {toEmail}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Email Error: {ex.Message}");
            throw;
        }
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
    {
        try
        {
            var client = GetSmtpClient();

            var mailMessage = new MailMessage(_emailSettings.FromEmail, toEmail)
            {
                Subject = "Reset your AIChatApp password",
                Body = $@"
                    <html>
                        <body style='font-family: Arial, sans-serif;'>
                            <h2>Password Reset Request</h2>
                            <p>We received a request to reset your password. Click the link below to create a new password:</p>
                            <p><a href='{resetLink}' style='background-color: #4F46E5; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Reset Password</a></p>
                            <p>Or copy and paste this link: {resetLink}</p>
                            <p><strong>This link expires in 1 hour.</strong></p>
                            <p>If you didn't request a password reset, please ignore this email.</p>
                        </body>
                    </html>",
                IsBodyHtml = true
            };

            await client.SendMailAsync(mailMessage);
            mailMessage.Dispose();
            Console.WriteLine($"Password reset email successfully sent to {toEmail}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Email Error: {ex.Message}");
            throw;
        }
    }
}