using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WahlMirai.Web.Models;

namespace WahlMirai.Web.Services;

public class EmailQueueBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailQueueBackgroundService> _logger;

    public EmailQueueBackgroundService(IServiceScopeFactory scopeFactory, IOptions<EmailSettings> settings, ILogger<EmailQueueBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delayMs = _settings.RateLimitPerMinute > 0 ? (int)(60000.0 / _settings.RateLimitPerMinute) : 3000;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueueAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred processing email queue.");
            }

            await Task.Delay(delayMs, stoppingToken);
        }
    }

    private async Task ProcessQueueAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WahlMiraiDbContext>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var passwordStore = scope.ServiceProvider.GetRequiredService<IPendingPasswordStore>();

        var pendingEmail = await dbContext.EmailQueues
            .Include(e => e.Voter)
            .Where(e => e.Status == "PENDIENTE" && e.Attempts < 3)
            .OrderBy(e => e.CreatedAt)
            .FirstOrDefaultAsync(stoppingToken);

        if (pendingEmail == null) return;

        pendingEmail.Attempts++;

        try
        {
            string subject = "";
            string htmlBody = "";
            bool shouldSend = false;

            if (pendingEmail.EmailType == "CANDIDATURA_APROBADA" || pendingEmail.EmailType == "CANDIDATURA_RECHAZADA")
            {
                subject = pendingEmail.EmailType == "CANDIDATURA_APROBADA" 
                    ? "Postulación Aprobada - Wahl Mirai" 
                    : "Postulación Rechazada - Wahl Mirai";
                
                var statusMsg = pendingEmail.EmailType == "CANDIDATURA_APROBADA" 
                    ? "Tu postulación ha sido revisada y <strong>APROBADA</strong>. Ya estás visible en el tarjetón electoral."
                    : "Tu postulación ha sido revisada y <strong>RECHAZADA</strong>. Puedes revisar los detalles en la plataforma.";

                htmlBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <h2 style='color: #2e7d32;'>Hola {pendingEmail.Voter.FullName},</h2>
                        <p>{statusMsg}</p>
                        <hr style='border: none; border-top: 1px solid #eee; margin-top: 30px;' />
                        <p style='color: #999; font-size: 0.8em;'>Este es un mensaje automático del sistema Wahl Mirai.</p>
                    </div>";
                
                shouldSend = true;
            }
            else
            {
                if (passwordStore.TryGetPassword(pendingEmail.Id, out var plainTextPassword))
                {
                    subject = "Credenciales de Acceso - Wahl Mirai";
                    var emailTypeFriendly = pendingEmail.EmailType switch
                    {
                        "CREDENCIAL_INICIAL" => "Credencial inicial",
                        "RECUPERACION_ACCESO" => "Recuperación de acceso",
                        "REASIGNACION_ADMIN" => "Reasignación por administrador",
                        _ => pendingEmail.EmailType
                    };

                    htmlBody = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                            <h2 style='color: #2e7d32;'>Hola {pendingEmail.Voter.FullName},</h2>
                            <p>Aquí tienes tu nueva contraseña para ingresar a Wahl Mirai:</p>
                            <div style='background:#f4f4f4; padding:15px; text-align:center; border-radius: 8px; margin: 20px 0;'>
                                <h3 style='margin: 0; font-family: monospace; letter-spacing: 2px;'>{plainTextPassword}</h3>
                            </div>
                            <p style='color: #666; font-size: 0.9em;'>Motivo: {emailTypeFriendly}</p>
                            <hr style='border: none; border-top: 1px solid #eee; margin-top: 30px;' />
                            <p style='color: #999; font-size: 0.8em;'>Este es un mensaje automático del sistema Wahl Mirai, por favor no respondas a este correo.</p>
                        </div>
                    ";
                    
                    shouldSend = true;
                    passwordStore.RemovePassword(pendingEmail.Id);
                }
                else
                {
                    pendingEmail.Status = "FALLIDO";
                    pendingEmail.ErrorMessage = "La contraseña en memoria se perdió (reinicio del servicio). El usuario deberá solicitarla nuevamente.";
                }
            }

            if (shouldSend)
            {
                await emailSender.SendAsync(pendingEmail.Voter.ContactEmail, subject, htmlBody, stoppingToken);
                pendingEmail.Status = "ENVIADO";
                pendingEmail.SentAt = DateTime.Now;
            }
        }
        catch (Exception ex)
        {
            if (pendingEmail.Attempts >= 3)
            {
                pendingEmail.Status = "FALLIDO";
            }
            pendingEmail.ErrorMessage = ex.Message.Length > 200 ? ex.Message.Substring(0, 200) : ex.Message;
            
            // Highly visible console output for the user
            Console.WriteLine("\n=======================================================");
            Console.WriteLine($"[ERROR DE CORREO] Falló el envío del email ID: {pendingEmail.Id}");
            Console.WriteLine($"MENSAJE DE ERROR: {ex.Message}");
            Console.WriteLine("=======================================================\n");

            _logger.LogError(ex, "Failed to send email ID {EmailId}", pendingEmail.Id);
        }

        await dbContext.SaveChangesAsync(stoppingToken);
    }
}
