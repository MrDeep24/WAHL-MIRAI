using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using WahlMirai.Web.Models;

namespace WahlMirai.Web.Services;

public class MailKitEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;

    public MailKitEmailSender(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.SenderDisplayName, _settings.SenderEmail));
        message.To.Add(new MailboxAddress("", toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        
        try
        {
            var secureSocketOptions = _settings.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, secureSocketOptions, ct);
            await client.AuthenticateAsync(_settings.SenderEmail, _settings.SenderPassword, ct);
            await client.SendAsync(message, ct);
        }
        finally
        {
            await client.DisconnectAsync(true, ct);
        }
    }
}
