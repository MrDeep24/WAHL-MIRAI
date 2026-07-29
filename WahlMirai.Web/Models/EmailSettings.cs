namespace WahlMirai.Web.Models;

public class EmailSettings
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderPassword { get; set; } = string.Empty;
    public string SenderDisplayName { get; set; } = string.Empty;
    public bool UseStartTls { get; set; } = true;
    public int RateLimitPerMinute { get; set; } = 20;
}
