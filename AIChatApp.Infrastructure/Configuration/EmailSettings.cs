namespace AIChatApp.Infrastructure.Configuration;

// Make sure the property names match the JSON keys exactly (case-sensitive)
public class EmailSettings
{
    public string SmtpHost { get; set; }
    public int SmtpPort { get; set; }
    public bool EnableSsl { get; set; } // Based on your corrected JSON
    public string SmtpUser { get; set; }
    public string SmtpPass { get; set; }
    public string FromEmail { get; set; }
}